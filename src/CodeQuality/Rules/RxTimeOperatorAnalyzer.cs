using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ1005: a System.Reactive operator that waits is called through an overload with no scheduler.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A time operator given no scheduler delivers on Rx's default scheduler, where a throw has no
/// caller.</b> Measured on Windows with Rx 6.1.0: a throwing handler behind each operator named here
/// ended the process, and did so with an <c>onError</c> given too. <c>Timeout</c> with no
/// <c>onError</c> ended it with no user code throwing at all, its own <c>TimeoutException</c>
/// rethrown on the timer's thread. The fix is visible at the call: pass a scheduler.
/// </para>
/// <para>
/// ⛔ <b>Only the measured operators, and only by time.</b> An operator joins the list when it is
/// measured to deliver on the scheduler. <c>Replay</c>, <c>Skip</c> and <c>Take</c> by time deliver
/// their values as they arrive and were not measured, so they are not here. An overload counts as by
/// time when it takes a <see cref="TimeSpan"/> or a <see cref="DateTimeOffset"/>: <c>Buffer</c> by
/// count, <c>Sample</c> by a sampler and <c>Throttle</c> by a selector wait on no timer.
/// </para>
/// <para>
/// ⚠ <b>A scheduler someone chose is not reported</b>, <c>Scheduler.Default</c> included. It ends
/// the process on a throw too, and docs/exception-resilience.md says so, but a rule against
/// choosing one would be a policy no measurement makes.
/// </para>
/// <para>
/// ⚠ <b>Off until a consumer turns it on</b>, with <c>dotnet_diagnostic.BNCQ1005.severity = warning</c>
/// in <c>.editorconfig</c>. The finding is the shape, and whether a throw can follow is decided by
/// what runs downstream. Judged on a real application's code, three of its five sites had nothing
/// downstream that could throw on real input, and two ended the process; plans/00005 decision 3
/// ships a rule whose findings are mostly safe off by default.
/// </para>
/// <para>
/// ⛔ <b>Inert where System.Reactive is not referenced.</b> A compilation that cannot name
/// <c>System.Reactive.Linq.Observable</c> cannot call its operators, so nothing is registered.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class RxTimeOperatorAnalyzer : ScopedAnalyzer<RxTimeOperatorAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ1005";

    /// <summary>The operators measured to deliver on the default scheduler when given none.</summary>
    /// <remarks>
    /// ⛔ An array, not a params list: C# 13 binds a params list to the <c>ReadOnlySpan</c> overload,
    /// which makes the assembly reference System.Memory, and the compiler host does not load it beside
    /// an analyzer. <c>AnalyzerShapeTests</c> caught it.
    /// </remarks>
    private static readonly ImmutableHashSet<string> Operators = ImmutableHashSet.Create(
        StringComparer.Ordinal, new[] { "Throttle", "Sample", "Interval", "Timer", "Delay", "Buffer", "Window", "Timeout" });

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A System.Reactive time operator is given no scheduler",
        messageFormat: "'{0}' is given no scheduler, so it delivers on Rx's default scheduler, where a throw in anything after it ends the process, an onError notwithstanding{1}. Pass an IScheduler: the UI scheduler for work that touches the view, a pool scheduler otherwise.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Throttle, Sample, Interval, Timer, Delay, Buffer and Window by time, and Timeout, called with no IScheduler, deliver on Rx's default scheduler. A throw there has no caller to go back to, so it ends the process, and an onError does not stop that. Timeout with no onError ends it with no user code throwing at all. Off by default: on a real application's code most findings had nothing downstream that could throw.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken) =>
        compilation.GetTypeByMetadataName(Reactive.Observable) is { } observable
            ? new Scope(
                observable,
                compilation.GetTypeByMetadataName(Reactive.Scheduler),
                compilation.GetTypeByMetadataName("System.TimeSpan"),
                compilation.GetTypeByMetadataName("System.DateTimeOffset"))
            : null;

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope) =>
        context.RegisterOperationAction(operationContext => Check(operationContext, scope), OperationKind.Invocation);

    private static void Check(OperationAnalysisContext context, Scope scope)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (!Reactive.IsDeclaredBy(method, scope.Observable) || !Operators.Contains(method.Name))
        {
            return;
        }

        bool byTime = false;
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (Is(parameter.Type, scope.Scheduler))
            {
                return;
            }

            byTime |= Is(parameter.Type, scope.TimeSpan) || Is(parameter.Type, scope.DateTimeOffset);
        }

        if (byTime)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                Reactive.NameOf(invocation),
                method.Name,
                method.Name == "Timeout" ? ", and with no onError so does Timeout's own TimeoutException" : string.Empty));
        }
    }

    private static bool Is(ITypeSymbol type, INamedTypeSymbol? expected) =>
        expected is not null && SymbolEqualityComparer.Default.Equals(type, expected);

    /// <summary>System.Reactive's operators and scheduler, and the time types, as this compilation sees them.</summary>
    internal sealed class Scope
    {
        public Scope(INamedTypeSymbol observable, INamedTypeSymbol? scheduler, INamedTypeSymbol? timeSpan, INamedTypeSymbol? dateTimeOffset)
        {
            Observable = observable;
            Scheduler = scheduler;
            TimeSpan = timeSpan;
            DateTimeOffset = dateTimeOffset;
        }

        public INamedTypeSymbol Observable { get; }

        public INamedTypeSymbol? Scheduler { get; }

        public INamedTypeSymbol? TimeSpan { get; }

        public INamedTypeSymbol? DateTimeOffset { get; }
    }
}
