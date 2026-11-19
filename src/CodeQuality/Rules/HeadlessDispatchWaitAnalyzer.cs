using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ3002: a wait blocks on the task <c>HeadlessUnitTestSession.Dispatch</c> returns.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The wait can wait on itself, and the run never ends.</b> Read in Avalonia's source at 12.1.3,
/// unchanged at 12.1.6: the session completes a dispatch's task on its one consumer thread, through a
/// <c>TaskCompletionSource</c> made without <c>RunContinuationsAsynchronously</c>, so the rest of a test
/// that awaited a dispatch, and xUnit's start of the next test, can run on that thread. A wait there for
/// another dispatch waits on the only thread that could run it. Measured on 12.1.3, twice on Windows 11
/// and under WSL 2: after an awaited dispatch, every wait this rule reads hung, <c>Wait</c> with a
/// timeout returned false, and the same waits with no await first, or after <c>Task.Yield</c>, returned.
/// </para>
/// <para>
/// ⚠ <b>The waits it reads</b>: the task's <c>Wait</c> in any overload, its <c>Result</c>, and
/// <c>GetAwaiter().GetResult()</c>, that one through <c>ConfigureAwait</c> too, and <c>Task.WaitAll</c>
/// or <c>Task.WaitAny</c> given the task. The task is a call to any <c>Dispatch</c> overload, or a local
/// that holds one, read as <see cref="Locals"/> reads a local, followed through up to eight locals. A
/// task held in a field, a parameter or a return value is not followed.
/// </para>
/// <para>
/// ⚠ <b>Reported wherever it is, a helper or a dispatched action included.</b> Whether a wait deadlocks
/// depends on test order, which the code does not decide, and a wait inside a dispatched action blocks
/// the consumer on itself every time. A wait in a delegate that <c>Task.Run</c> runs, before any await
/// in it, never runs on the consumer thread, and a <c>Result</c> read after the task was awaited does not
/// block: each is reported all the same, and suppressed where it is, with that reason.
/// </para>
/// <para>
/// ⛔ <b>Inert where <c>Avalonia.Headless</c> is not referenced.</b> A compilation that cannot name
/// <c>Avalonia.Headless.HeadlessUnitTestSession</c> registers nothing, Avalonia itself referenced or not.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class HeadlessDispatchWaitAnalyzer : ScopedAnalyzer<HeadlessDispatchWaitAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ3002";

    /// <summary>How many locals a task is followed through, one holding the next, before the rule stops.</summary>
    private const int MaxLocalHops = 8;

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A wait blocks on the task a headless dispatch returns",
        messageFormat: "This wait can hang the test run: Avalonia's headless session completes a dispatch on its one thread, where the code after an awaited dispatch runs, so a wait there waits on itself. Await the dispatch, or return its task.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Avalonia's HeadlessUnitTestSession completes the task Dispatch returns on its one consumer thread, without RunContinuationsAsynchronously, so the rest of a test that awaited a dispatch, and the start of the next test, can run on that thread. A wait there for another dispatch, by Wait, Result, GetAwaiter().GetResult(), Task.WaitAll or Task.WaitAny, waits on the only thread that could run it, and the run never ends. Await the dispatch, or return its task, and make the test async.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        INamedTypeSymbol? session = compilation.GetTypeByMetadataName("Avalonia.Headless.HeadlessUnitTestSession");
        INamedTypeSymbol? task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        INamedTypeSymbol? taskOf = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        return session is null || task is null || taskOf is null ? null : new Scope(session, task, taskOf);
    }

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope)
    {
        context.RegisterOperationAction(operationContext => CheckCall(operationContext, scope), OperationKind.Invocation);
        context.RegisterOperationAction(operationContext => CheckResult(operationContext, scope), OperationKind.PropertyReference);
    }

    private static void CheckCall(OperationAnalysisContext context, Scope scope)
    {
        var call = (IInvocationOperation)context.Operation;
        IMethodSymbol method = call.TargetMethod;
        switch (method.Name)
        {
            case "GetResult" when call.Instance is IInvocationOperation { TargetMethod.Name: "GetAwaiter" } awaiter
                && awaiter.Instance is { } awaited:
                Report(context, scope, call, Configured(awaited));
                break;
            case "Wait" when !method.IsStatic && Is(method.ContainingType, scope.Task) && call.Instance is { } waited:
                Report(context, scope, call, waited);
                break;
            case "WaitAll" or "WaitAny" when method.IsStatic && Is(method.ContainingType, scope.Task):
                foreach (IArgumentOperation argument in call.Arguments)
                {
                    foreach (IOperation element in Elements(argument.Value))
                    {
                        if (IsDispatch(element, scope, 0))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, Calls.NameOf(call)));
                            return;
                        }
                    }
                }

                break;
        }
    }

    private static void CheckResult(OperationAnalysisContext context, Scope scope)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        if (reference.Property.Name == "Result"
            && Is(reference.Property.ContainingType, scope.TaskOf)
            && reference.Instance is { } held
            && IsDispatch(held, scope, 0))
        {
            Location location = reference.Syntax is MemberAccessExpressionSyntax member
                ? member.Name.Identifier.GetLocation()
                : reference.Syntax.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }

    private static void Report(OperationAnalysisContext context, Scope scope, IInvocationOperation call, IOperation task)
    {
        if (IsDispatch(task, scope, 0))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, Calls.NameOf(call)));
        }
    }

    /// <summary>The task a <c>ConfigureAwait</c> call is made on, or the value itself where it is no such call.</summary>
    private static IOperation Configured(IOperation awaited) =>
        Unconverted(awaited) is IInvocationOperation { TargetMethod.Name: "ConfigureAwait", Instance: { } task } ? task : awaited;

    /// <summary>Whether <paramref name="value"/> is a call to a <c>Dispatch</c> overload, or a local that holds one.</summary>
    private static bool IsDispatch(IOperation value, Scope scope, int hops)
    {
        IOperation unwrapped = Unconverted(value);
        if (unwrapped is IInvocationOperation call)
        {
            return call.TargetMethod.Name == "Dispatch"
                && SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, scope.Session);
        }

        return hops < MaxLocalHops
            && unwrapped is ILocalReferenceOperation held
            && Locals.OnlyValueOf(held) is { } initial
            && IsDispatch(initial, scope, hops + 1);
    }

    /// <summary>The tasks an argument of <c>WaitAll</c> or <c>WaitAny</c> passes: an array's or a collection's elements, or itself.</summary>
    private static IEnumerable<IOperation> Elements(IOperation value)
    {
        switch (Unconverted(value))
        {
            case IArrayCreationOperation { Initializer: { } initializer }:
                return initializer.ElementValues;
            case ICollectionExpressionOperation collection:
                return collection.Elements;
            default:
                return new[] { value };
        }
    }

    private static IOperation Unconverted(IOperation value)
    {
        IOperation current = value;
        while (current is IConversionOperation conversion)
        {
            current = conversion.Operand;
        }

        return current;
    }

    private static bool Is(INamedTypeSymbol? type, INamedTypeSymbol expected) =>
        type is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, expected);

    /// <summary>The types the rule compares against, resolved once per compilation.</summary>
    internal sealed class Scope
    {
        public Scope(INamedTypeSymbol session, INamedTypeSymbol task, INamedTypeSymbol taskOf)
        {
            Session = session;
            Task = task;
            TaskOf = taskOf;
        }

        /// <summary><c>Avalonia.Headless.HeadlessUnitTestSession</c>.</summary>
        public INamedTypeSymbol Session { get; }

        /// <summary><c>System.Threading.Tasks.Task</c>, which declares <c>Wait</c>, <c>WaitAll</c> and <c>WaitAny</c>.</summary>
        public INamedTypeSymbol Task { get; }

        /// <summary><c>System.Threading.Tasks.Task&lt;TResult&gt;</c>, which declares <c>Result</c>.</summary>
        public INamedTypeSymbol TaskOf { get; }
    }
}
