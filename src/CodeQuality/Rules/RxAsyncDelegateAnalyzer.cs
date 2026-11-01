using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ1006: an async delegate given to a System.Reactive operator that does not await it.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>An async lambda given to <c>Subscribe</c> is async void.</b> <c>Subscribe</c>, <c>Do</c> and
/// <c>Finally</c> take an <see cref="Action"/>, so an async lambda given to one returns nothing to wait
/// on, and its exception is rethrown on the thread it resumed on. Measured on Windows with Rx 6.1.0:
/// off a UI thread, the process ended. An async <c>Select</c> turns each value into a task nobody
/// awaits, whose failure was unobserved until a forced collection. The fix is visible at the call:
/// <c>SelectMany</c> with <c>Observable.FromAsync</c>, which awaits, or a synchronous handler.
/// </para>
/// <para>
/// ⚠ <b>A <c>Select</c> whose tasks go straight to an operator that awaits them is not reported.</b>
/// <c>Select(async …).Concat()</c>, <c>.Merge()</c> and <c>.Switch()</c> take an observable of tasks
/// and await each, which is the idiom for running them in order or at once. Read from the parameter
/// the result is passed to, so any System.Reactive operator that takes an observable of tasks counts.
/// </para>
/// <para>
/// ⚠ <b>Only System.Reactive's operators.</b> An async lambda given to any other void delegate is
/// the ground of <c>Microsoft.VisualStudio.Threading.Analyzers</c>' VSTHRD101, and two analyzers
/// reporting one site under two IDs costs every consumer a suppression. A method group passed to an
/// <see cref="Action"/> is an async void method, reported where it is declared by VSTHRD100.
/// </para>
/// <para>
/// ⛔ <b>Inert where System.Reactive is not referenced.</b> A compilation that cannot name
/// <c>System.Reactive.Linq.Observable</c> cannot call its operators, so nothing is registered.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class RxAsyncDelegateAnalyzer : ScopedAnalyzer<RxAsyncDelegateAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ1006";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "An async delegate is given to a System.Reactive operator that does not await it",
        messageFormat: "{0}. Use SelectMany with Observable.FromAsync, which awaits the work and carries its failure to onError, or a synchronous handler.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An async lambda given to Subscribe, Do or Finally is async void: its exception is rethrown on the thread it resumed on, which off a UI thread ends the process. An async Select makes each value a task nobody awaits, whose failure goes unobserved. SelectMany with Observable.FromAsync awaits the work.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken) =>
        compilation.GetTypeByMetadataName(Reactive.Observable) is { } observable
            ? new Scope(
                observable,
                compilation.GetTypeByMetadataName(Reactive.ObservableExtensions),
                compilation.GetTypeByMetadataName("System.IObservable`1"),
                compilation.GetTypeByMetadataName("System.Threading.Tasks.Task"),
                compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1"))
            : null;

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope) =>
        context.RegisterOperationAction(operationContext => Check(operationContext, scope), OperationKind.Invocation);

    private static void Check(OperationAnalysisContext context, Scope scope)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (!Reactive.IsDeclaredBy(method, scope.Observable) && !Reactive.IsDeclaredBy(method, scope.ObservableExtensions))
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter?.Type is INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.ReturnsVoid: true }
                && Reactive.Function(argument.Value) is { Symbol.IsAsync: true } function)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    AsyncKeyword(function),
                    $"An async delegate given to '{method.Name}' is async void: its exception is rethrown on the thread it resumed on, which off a UI thread ends the process"));
            }
        }

        if (method.Name == "Select"
            && Reactive.IsDeclaredBy(method, scope.Observable)
            && IsObservableOfTasks(method.ReturnType, scope)
            && !IsAwaitedBy(invocation.Parent, scope))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                Reactive.NameOf(invocation),
                "This 'Select' makes each value a task nobody awaits, so a failure in one goes unobserved and the subscription never hears of it"));
        }
    }

    /// <summary>
    /// Whether the <c>Select</c>'s result is passed straight to a System.Reactive operator whose
    /// parameter takes an observable of tasks, such as <c>Concat</c>, <c>Merge</c> or <c>Switch</c>,
    /// which await them.
    /// </summary>
    private static bool IsAwaitedBy(IOperation? parent, Scope scope)
    {
        while (parent is IConversionOperation conversion)
        {
            parent = conversion.Parent;
        }

        return parent is IArgumentOperation { Parameter: { } parameter, Parent: IInvocationOperation consumer }
            && Reactive.IsDeclaredBy(consumer.TargetMethod, scope.Observable)
            && IsObservableOfTasks(parameter.Type, scope);
    }

    private static bool IsObservableOfTasks(ITypeSymbol type, Scope scope) =>
        type is INamedTypeSymbol { IsGenericType: true } observable
        && scope.IObservable is not null
        && SymbolEqualityComparer.Default.Equals(observable.OriginalDefinition, scope.IObservable)
        && IsTask(observable.TypeArguments[0], scope);

    private static bool IsTask(ITypeSymbol type, Scope scope) =>
        (scope.Task is not null && SymbolEqualityComparer.Default.Equals(type, scope.Task))
        || (scope.TaskOfT is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, scope.TaskOfT));

    /// <summary>The <c>async</c> keyword of a lambda or anonymous method, where the finding belongs.</summary>
    private static Location AsyncKeyword(IAnonymousFunctionOperation function) =>
        function.Syntax is AnonymousFunctionExpressionSyntax { AsyncKeyword.RawKind: not 0 } syntax
            ? syntax.AsyncKeyword.GetLocation()
            : function.Syntax.GetLocation();

    /// <summary>System.Reactive's operator classes, and the observable and task types, as this compilation sees them.</summary>
    internal sealed class Scope
    {
        public Scope(INamedTypeSymbol observable, INamedTypeSymbol? observableExtensions, INamedTypeSymbol? iobservable, INamedTypeSymbol? task, INamedTypeSymbol? taskOfT)
        {
            Observable = observable;
            ObservableExtensions = observableExtensions;
            IObservable = iobservable;
            Task = task;
            TaskOfT = taskOfT;
        }

        /// <summary><c>System.Reactive.Linq.Observable</c>, which declares the operators.</summary>
        public INamedTypeSymbol Observable { get; }

        /// <summary><c>System.ObservableExtensions</c>, which declares <c>Subscribe</c> with delegates.</summary>
        public INamedTypeSymbol? ObservableExtensions { get; }

        /// <summary><c>System.IObservable&lt;T&gt;</c>, unconstructed.</summary>
        public INamedTypeSymbol? IObservable { get; }

        public INamedTypeSymbol? Task { get; }

        /// <summary><c>System.Threading.Tasks.Task&lt;T&gt;</c>, unconstructed.</summary>
        public INamedTypeSymbol? TaskOfT { get; }
    }
}
