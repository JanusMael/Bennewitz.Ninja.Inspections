using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ1007: a producer built by <c>Observable.Create</c> calls <c>OnNext</c> inside a try whose
/// catch carries on.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The catch talks to a dead observer.</b> A subscriber's throw comes out of <c>OnNext</c>
/// after Rx has disposed the producer's subscription, so a catch around the call swallows the
/// subscriber's failure and carries on producing for nobody. Measured on Windows with Rx 6.1.0: a
/// polling producer shared by two subscribers, one throwing on poll 3, caught it and polled no more;
/// the other subscriber saw nothing after 3. The fix is visible at the call: catch only the
/// producer's own work, outside the <c>OnNext</c> call.
/// </para>
/// <para>
/// ⚠ <b>Only the observer <c>Observable.Create</c> hands its delegate.</b> A catch around a
/// <c>Subject</c>'s <c>OnNext</c> is a guard: measured, the subject's other subscribers kept every
/// later value. The observer is the delegate's first parameter, followed into any lambda or local
/// function written inside it; a method group is not opened.
/// </para>
/// <para>
/// ⚠ <b>A catch that cannot carry on is not reported</b>, judged with Roslyn's control-flow analysis:
/// one whose end cannot be reached, because it throws, returns or breaks, is fine, and one whose end
/// can be reached, or that continues a loop, is reported.
/// </para>
/// <para>
/// ⛔ <b>Inert where System.Reactive is not referenced.</b> A compilation that cannot name
/// <c>System.Reactive.Linq.Observable</c> cannot call <c>Observable.Create</c>, so nothing is
/// registered.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class RxProducerCatchAnalyzer : ScopedAnalyzer<RxProducerCatchAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ1007";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A producer built by Observable.Create catches what OnNext throws",
        messageFormat: "This OnNext sits inside a try whose catch carries on, so a subscriber's throw is swallowed after Rx has already disposed the producer's subscription, and the producer goes on talking to a dead observer. Catch only the producer's own work, outside the OnNext call.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A subscriber's throw comes out of OnNext after Rx has disposed the producer's subscription. A catch around the OnNext of the observer Observable.Create hands its delegate swallows that failure, and the producer carries on for nobody: every subscriber of a shared source stops, and nothing says why.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken) =>
        compilation.GetTypeByMetadataName(Reactive.Observable) is { } observable
            && compilation.GetTypeByMetadataName("System.IObserver`1") is { } observer
            ? new Scope(observable, observer)
            : null;

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope) =>
        context.RegisterOperationAction(operationContext => Check(operationContext, scope), OperationKind.Invocation);

    private static void Check(OperationAnalysisContext context, Scope scope)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != "Create" || !Reactive.IsDeclaredBy(invocation.TargetMethod, scope.Observable))
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (Reactive.Function(argument.Value) is not { } producer
                || producer.Symbol.Parameters.Length == 0
                || !SymbolEqualityComparer.Default.Equals(producer.Symbol.Parameters[0].Type.OriginalDefinition, scope.Observer))
            {
                continue;
            }

            IParameterSymbol observer = producer.Symbol.Parameters[0];
            foreach (IOperation operation in producer.Body.Descendants())
            {
                if (operation is IInvocationOperation { TargetMethod.Name: "OnNext", Instance: IParameterReferenceOperation reference } onNext
                    && SymbolEqualityComparer.Default.Equals(reference.Parameter, observer)
                    && CaughtAndCarriedOn(onNext, producer.Syntax))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, Reactive.NameOf(onNext)));
                }
            }
        }
    }

    /// <summary>
    /// Whether <paramref name="onNext"/> sits in the try block of a try statement inside the
    /// producer, one of whose catch clauses can carry on.
    /// </summary>
    private static bool CaughtAndCarriedOn(IInvocationOperation onNext, SyntaxNode producer)
    {
        SemanticModel? model = onNext.SemanticModel;
        if (model is null)
        {
            return false;
        }

        SyntaxNode child = onNext.Syntax;
        for (SyntaxNode? node = child.Parent; node is not null && child != producer; child = node, node = node.Parent)
        {
            if (node is TryStatementSyntax tryStatement && child == tryStatement.Block)
            {
                foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
                {
                    if (CarriesOn(model, catchClause.Block))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Whether control can leave the catch other than by throwing, returning or breaking out.</summary>
    private static bool CarriesOn(SemanticModel model, BlockSyntax block)
    {
        ControlFlowAnalysis? flow = model.AnalyzeControlFlow(block);
        if (flow is null || !flow.Succeeded)
        {
            return false;
        }

        if (flow.EndPointIsReachable)
        {
            return true;
        }

        foreach (SyntaxNode exit in flow.ExitPoints)
        {
            if (exit is ContinueStatementSyntax)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>System.Reactive's operators and the observer type, as this compilation sees them.</summary>
    internal sealed class Scope
    {
        public Scope(INamedTypeSymbol observable, INamedTypeSymbol observer)
        {
            Observable = observable;
            Observer = observer;
        }

        public INamedTypeSymbol Observable { get; }

        /// <summary><c>System.IObserver&lt;T&gt;</c>, unconstructed.</summary>
        public INamedTypeSymbol Observer { get; }
    }
}
