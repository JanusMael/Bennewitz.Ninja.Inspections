using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ1009: a callback no caller catches has a body that is not wholly inside one try/catch.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Nothing above these callbacks catches.</b> Measured on Windows and under WSL 2 on .NET 10: a
/// throw ended the process from a <c>System.Threading.Timer</c> callback, a <c>Thread</c>'s start, a
/// <c>ThreadPool.QueueUserWorkItem</c> callback, a <c>Process</c> <c>OutputDataReceived</c>,
/// <c>ErrorDataReceived</c> or <c>Exited</c> handler, and a finalizer. A <c>FileSystemWatcher</c>
/// <c>Created</c>, <c>Changed</c>, <c>Deleted</c> or <c>Renamed</c> handler's throw ended it on
/// Windows and silenced the watcher on Linux. A <c>System.Timers.Timer</c> swallowed an
/// <c>Elapsed</c> handler's throw, and nothing ever reported it. The fix is visible in the body: one
/// try/catch that logs, around all of it.
/// </para>
/// <para>
/// ⛔ <b>Only the measured callbacks.</b> Another joins when it is measured to end the process or
/// swallow the throw. A <c>DispatcherTimer</c>'s <c>Tick</c> is Avalonia's, and <c>BNCQ3001</c>'s.
/// </para>
/// <para>
/// ⚠ <b>The body is judged as written.</b> A body that only posts to a dispatcher, or calls a
/// method that catches for itself, is still reported: the rule cannot see what a call throws.
/// An empty body is not reported. What guarded means is in <see cref="Callbacks"/>.
/// </para>
/// <para>
/// ⚠ <b>On by default, though most findings on real code were safe.</b> Judged on the family's
/// repositories and a real application's code, 12 of its 13 findings could not throw on real input,
/// and the one that could was a probe that throws on purpose. The developer chose on 2026-11-03 to
/// catch the shape everywhere and suppress the safe sites, over plans/00005 decision 3's count.
/// </para>
/// <para>
/// ⛔ <b>Inert where none of the types is in reach.</b> A compilation that can name none of
/// <c>System.Threading.Timer</c>, <c>Thread</c>, <c>ThreadPool</c>, <c>System.Timers.Timer</c>,
/// <c>FileSystemWatcher</c> and <c>Process</c> registers nothing, finalizers included.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UnguardedCallbackAnalyzer : ScopedAnalyzer<UnguardedCallbackAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ1009";

    private const string EndsTheProcess = "a throw there ends the process";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A callback no caller catches is not wholly inside a try/catch",
        messageFormat: "{0} runs where no caller catches, and {1}. Put the whole body inside one try/catch that logs.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nothing above a System.Threading.Timer callback, a thread's start, a thread-pool work item, a Process event handler or a finalizer catches: a throw there ends the process. A FileSystemWatcher handler's throw ends it on Windows and silences the watcher on Linux, and a System.Timers.Timer swallows an Elapsed handler's throw for good. One try/catch that logs, around the whole body, is the fix.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        Scope scope = new(
            compilation.GetTypeByMetadataName("System.Threading.Timer"),
            compilation.GetTypeByMetadataName("System.Threading.Thread"),
            compilation.GetTypeByMetadataName("System.Threading.ThreadPool"),
            compilation.GetTypeByMetadataName("System.Timers.Timer"),
            compilation.GetTypeByMetadataName("System.IO.FileSystemWatcher"),
            compilation.GetTypeByMetadataName("System.Diagnostics.Process"));

        return scope.IsEmpty ? null : scope;
    }

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope)
    {
        context.RegisterOperationAction(operationContext => CheckCreation(operationContext, scope), OperationKind.ObjectCreation);
        context.RegisterOperationAction(operationContext => CheckWorkItem(operationContext, scope), OperationKind.Invocation);
        context.RegisterOperationAction(operationContext => CheckHandler(operationContext, scope), OperationKind.EventAssignment);
        context.RegisterSyntaxNodeAction(CheckFinalizer, SyntaxKind.DestructorDeclaration);
    }

    private static void CheckCreation(OperationAnalysisContext context, Scope scope)
    {
        var creation = (IObjectCreationOperation)context.Operation;
        string? what =
            Is(creation.Type, scope.ThreadingTimer) ? "This System.Threading.Timer callback"
            : Is(creation.Type, scope.Thread) ? "This thread's start"
            : null;

        if (what is null)
        {
            return;
        }

        foreach (IArgumentOperation argument in creation.Arguments)
        {
            if (argument.Parameter?.Type.TypeKind == TypeKind.Delegate)
            {
                Report(context, argument.Value, what, EndsTheProcess);
            }
        }
    }

    private static void CheckWorkItem(OperationAnalysisContext context, Scope scope)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != "QueueUserWorkItem" || !Is(invocation.TargetMethod.ContainingType, scope.ThreadPool))
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter?.Type.TypeKind == TypeKind.Delegate)
            {
                Report(context, argument.Value, "This thread-pool work item", EndsTheProcess);
            }
        }
    }

    private static void CheckHandler(OperationAnalysisContext context, Scope scope)
    {
        var assignment = (IEventAssignmentOperation)context.Operation;
        if (!assignment.Adds || assignment.EventReference is not IEventReferenceOperation { Event: { } handled })
        {
            return;
        }

        INamedTypeSymbol type = handled.ContainingType;
        string name = handled.Name;
        (string What, string Outcome)? found =
            Is(type, scope.TimersTimer) && name == "Elapsed"
                ? ("This System.Timers.Timer Elapsed handler", "the timer swallows a throw there for good, and nothing ever reports it")
            : Is(type, scope.Watcher) && name is "Created" or "Changed" or "Deleted" or "Renamed"
                ? ($"This FileSystemWatcher {name} handler", "a throw there ends the process on Windows, and silences the watcher on Linux")
            : Is(type, scope.Process) && name is "OutputDataReceived" or "ErrorDataReceived" or "Exited"
                ? ($"This Process {name} handler", EndsTheProcess)
            : null;

        if (found is { } callback)
        {
            Report(context, assignment.HandlerValue, callback.What, callback.Outcome);
        }
    }

    private static void CheckFinalizer(SyntaxNodeAnalysisContext context)
    {
        var finalizer = (DestructorDeclarationSyntax)context.Node;
        SyntaxNode? body = finalizer.Body ?? (SyntaxNode?)finalizer.ExpressionBody?.Expression;
        if (Callbacks.IsUnguarded(body))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                finalizer.Identifier.GetLocation(),
                "This finalizer",
                "a throw there ends the process at the collection that runs it"));
        }
    }

    private static void Report(OperationAnalysisContext context, IOperation value, string what, string outcome)
    {
        if (Callbacks.Of(value) is { } callback && Callbacks.IsUnguarded(callback.Body))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, callback.Location, what, outcome));
        }
    }

    private static bool Is(ITypeSymbol? type, INamedTypeSymbol? expected) =>
        type is not null && expected is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, expected);

    /// <summary>The types whose callbacks the rule reads, as this compilation sees them; any may be missing.</summary>
    internal sealed class Scope
    {
        public Scope(INamedTypeSymbol? threadingTimer, INamedTypeSymbol? thread, INamedTypeSymbol? threadPool, INamedTypeSymbol? timersTimer, INamedTypeSymbol? watcher, INamedTypeSymbol? process)
        {
            ThreadingTimer = threadingTimer;
            Thread = thread;
            ThreadPool = threadPool;
            TimersTimer = timersTimer;
            Watcher = watcher;
            Process = process;
        }

        public INamedTypeSymbol? ThreadingTimer { get; }

        public INamedTypeSymbol? Thread { get; }

        public INamedTypeSymbol? ThreadPool { get; }

        public INamedTypeSymbol? TimersTimer { get; }

        public INamedTypeSymbol? Watcher { get; }

        public INamedTypeSymbol? Process { get; }

        public bool IsEmpty =>
            ThreadingTimer is null && Thread is null && ThreadPool is null && TimersTimer is null && Watcher is null && Process is null;
    }
}
