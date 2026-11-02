using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ1008: a gate is released outside a <c>finally</c> clause, in a body that takes it.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>One throw between taking and releasing leaves the gate shut.</b> Measured on Windows: one
/// throw between <c>WaitAsync</c> and <c>Release</c> left a <c>SemaphoreSlim</c> held, the next call
/// had not completed half a second later, and <c>CurrentCount</c> read 0. The fix is visible in the
/// body: <c>try { … } finally { gate.Release(); }</c>.
/// </para>
/// <para>
/// ⚠ <b>The gates:</b> <c>SemaphoreSlim</c> (<c>Wait</c>, <c>WaitAsync</c>, <c>Release</c>),
/// <c>Semaphore</c> (<c>WaitOne</c>, <c>Release</c>), <c>Mutex</c> (<c>WaitOne</c>,
/// <c>ReleaseMutex</c>), <c>Monitor</c> (<c>Enter</c>, <c>TryEnter</c>, <c>Exit</c>, on the object
/// passed) and <c>ReaderWriterLockSlim</c> (its <c>Enter</c>, <c>TryEnter</c> and <c>Exit</c>
/// methods). A gate is a field, a local or a parameter, and a release is reported only where the
/// same body takes the same one.
/// </para>
/// <para>
/// ⚠ <b>Each lambda, anonymous method and local function is a body of its own</b>, so a gate taken
/// in a method and released in a continuation it schedules is not reported: there the release
/// belongs to the continuation, and nothing in the method could put it in a <c>finally</c>.
/// </para>
/// <para>
/// ⛔ <b>Inert where none of the gate types is in reach.</b>
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class GateReleaseAnalyzer : ScopedAnalyzer<GateReleaseAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ1008";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A gate taken in a body is released outside a finally clause",
        messageFormat: "'{0}' releases '{1}' outside a finally clause, so one throw after it is taken leaves it shut and every later caller waits. Release it in the finally clause of a try that starts once it is taken.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A SemaphoreSlim, Semaphore, Mutex, Monitor or ReaderWriterLockSlim taken and then released outside a finally clause stays taken when anything between the two throws, and every later caller waits for good. Release it in a finally.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        Scope scope = new(
            compilation.GetTypeByMetadataName("System.Threading.SemaphoreSlim"),
            compilation.GetTypeByMetadataName("System.Threading.Semaphore"),
            compilation.GetTypeByMetadataName("System.Threading.Mutex"),
            compilation.GetTypeByMetadataName("System.Threading.Monitor"),
            compilation.GetTypeByMetadataName("System.Threading.ReaderWriterLockSlim"));

        return scope.IsEmpty ? null : scope;
    }

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope) =>
        context.RegisterOperationAction(operationContext => Check(operationContext, scope), OperationKind.Invocation);

    private static void Check(OperationAnalysisContext context, Scope scope)
    {
        var release = (IInvocationOperation)context.Operation;
        if (Classify(release, scope) is not (Use.Release, { } gate))
        {
            return;
        }

        IOperation body = Body(release);
        if (InFinally(release.Syntax, body.Syntax))
        {
            return;
        }

        foreach (IOperation operation in body.Descendants())
        {
            if (operation is IInvocationOperation acquire
                && Classify(acquire, scope) is (Use.Acquire, { } taken)
                && SymbolEqualityComparer.Default.Equals(taken, gate)
                && Body(acquire) == body)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, Calls.NameOf(release), release.TargetMethod.Name, gate.Name));
                return;
            }
        }
    }

    /// <summary>Whether the call takes or releases a gate, and which field, local or parameter it is.</summary>
    private static (Use Use, ISymbol? Gate) Classify(IInvocationOperation call, Scope scope)
    {
        IMethodSymbol method = call.TargetMethod;
        string name = method.Name;

        if (Is(method.ContainingType, scope.Monitor))
        {
            Use use = name is "Enter" or "TryEnter" ? Use.Acquire : name == "Exit" ? Use.Release : Use.None;
            return (use, call.Arguments.Length > 0 ? Gate(call.Arguments[0].Value) : null);
        }

        ITypeSymbol? receiver = call.Instance?.Type;
        if (receiver is null)
        {
            return (Use.None, null);
        }

        Use found =
            Is(receiver, scope.SemaphoreSlim) ? (name is "Wait" or "WaitAsync" ? Use.Acquire : name == "Release" ? Use.Release : Use.None)
            : Is(receiver, scope.Semaphore) ? (name == "WaitOne" ? Use.Acquire : name == "Release" ? Use.Release : Use.None)
            : Is(receiver, scope.Mutex) ? (name == "WaitOne" ? Use.Acquire : name == "ReleaseMutex" ? Use.Release : Use.None)
            : Is(receiver, scope.ReaderWriterLock)
                ? (name.StartsWith("Enter", StringComparison.Ordinal) || name.StartsWith("TryEnter", StringComparison.Ordinal) ? Use.Acquire
                    : name.StartsWith("Exit", StringComparison.Ordinal) ? Use.Release
                    : Use.None)
            : Use.None;

        return (found, found == Use.None ? null : Gate(call.Instance!));
    }

    /// <summary>The field, local or parameter a gate is read from; null for anything else.</summary>
    private static ISymbol? Gate(IOperation value)
    {
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }

        return value switch
        {
            IFieldReferenceOperation field => field.Field,
            ILocalReferenceOperation local => local.Local,
            IParameterReferenceOperation parameter => parameter.Parameter,
            _ => null,
        };
    }

    /// <summary>The body a call belongs to: its nearest lambda, anonymous method or local function, or the member's.</summary>
    private static IOperation Body(IOperation operation)
    {
        IOperation current = operation;
        while (current.Parent is { } parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return parent;
            }

            current = parent;
        }

        return current;
    }

    private static bool InFinally(SyntaxNode call, SyntaxNode body)
    {
        for (SyntaxNode? node = call.Parent; node is not null && node != body; node = node.Parent)
        {
            if (node is FinallyClauseSyntax)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Is(ITypeSymbol? type, INamedTypeSymbol? expected) =>
        type is not null && expected is not null && SymbolEqualityComparer.Default.Equals(type, expected);

    private enum Use
    {
        None,
        Acquire,
        Release,
    }

    /// <summary>The gate types, as this compilation sees them; any may be missing.</summary>
    internal sealed class Scope
    {
        public Scope(INamedTypeSymbol? semaphoreSlim, INamedTypeSymbol? semaphore, INamedTypeSymbol? mutex, INamedTypeSymbol? monitor, INamedTypeSymbol? readerWriterLock)
        {
            SemaphoreSlim = semaphoreSlim;
            Semaphore = semaphore;
            Mutex = mutex;
            Monitor = monitor;
            ReaderWriterLock = readerWriterLock;
        }

        public INamedTypeSymbol? SemaphoreSlim { get; }

        public INamedTypeSymbol? Semaphore { get; }

        public INamedTypeSymbol? Mutex { get; }

        public INamedTypeSymbol? Monitor { get; }

        public INamedTypeSymbol? ReaderWriterLock { get; }

        public bool IsEmpty => SemaphoreSlim is null && Semaphore is null && Mutex is null && Monitor is null && ReaderWriterLock is null;
    }
}
