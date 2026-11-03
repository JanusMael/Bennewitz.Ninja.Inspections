using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ3001: an Avalonia <c>DispatcherTimer</c>'s <c>Tick</c> handler is not wholly inside a
/// try/catch.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>One throw stops the timer for good, and nothing says so.</b> Measured on Avalonia 12.1.3 and
/// .NET 10.0.14, headless, on Windows and Linux: a 30 ms timer whose handler threw on its first tick
/// ticked once in 600 ms while <c>IsEnabled</c> read true, and
/// <c>Dispatcher.UnhandledException</c> never heard of it; the exception surfaced only as an
/// unobserved task's, at a later collection. The fix is visible in the body: one try/catch that
/// logs, around all of it.
/// </para>
/// <para>
/// ⚠ <b>Both ways a handler reaches <c>Tick</c></b>: <c>Tick +=</c>, and the constructor that takes
/// the handler as its callback. <c>DispatcherTimer.Run</c> and <c>RunOnce</c> were not measured, so
/// they are not read. What guarded means is in <see cref="Callbacks"/>.
/// </para>
/// <para>
/// ⚠ <b>On by default, though its one finding on real code was safe.</b> On the family's
/// repositories and a real application's code it reported one handler, which could not throw on real
/// input. The developer chose on 2026-11-03 to catch the shape everywhere and suppress the safe
/// sites, over plans/00005 decision 3's count.
/// </para>
/// <para>
/// ⭐ <b>An Avalonia rule, so its number is from <c>BNCQ3001</c></b>: a project that does not use
/// Avalonia never sees it, and one that does can see, and turn off, the rules about the framework
/// apart from the rest.
/// </para>
/// <para>
/// ⛔ <b>Inert where Avalonia is not referenced.</b> A compilation that cannot name
/// <c>Avalonia.Threading.DispatcherTimer</c> registers nothing.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class DispatcherTimerTickAnalyzer : ScopedAnalyzer<INamedTypeSymbol>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ3001";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "An Avalonia DispatcherTimer's Tick handler is not wholly inside a try/catch",
        messageFormat: "One throw from this Tick handler stops the timer for good, while IsEnabled reads true and Dispatcher.UnhandledException never hears of it. Put the whole body inside one try/catch that logs.",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An Avalonia DispatcherTimer whose Tick handler throws once stops ticking for good. IsEnabled still reads true, Dispatcher.UnhandledException is not raised, and the exception surfaces only as an unobserved task's at a later garbage collection. One try/catch that logs, around the whole body, is the fix.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override INamedTypeSymbol? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken) =>
        compilation.GetTypeByMetadataName("Avalonia.Threading.DispatcherTimer");

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, INamedTypeSymbol timer)
    {
        context.RegisterOperationAction(operationContext => CheckHandler(operationContext, timer), OperationKind.EventAssignment);
        context.RegisterOperationAction(operationContext => CheckCreation(operationContext, timer), OperationKind.ObjectCreation);
    }

    private static void CheckHandler(OperationAnalysisContext context, INamedTypeSymbol timer)
    {
        var assignment = (IEventAssignmentOperation)context.Operation;
        if (assignment.Adds
            && assignment.EventReference is IEventReferenceOperation { Event: { Name: "Tick" } tick }
            && SymbolEqualityComparer.Default.Equals(tick.ContainingType, timer))
        {
            Report(context, assignment.HandlerValue);
        }
    }

    private static void CheckCreation(OperationAnalysisContext context, INamedTypeSymbol timer)
    {
        var creation = (IObjectCreationOperation)context.Operation;
        if (!SymbolEqualityComparer.Default.Equals(creation.Type, timer))
        {
            return;
        }

        foreach (IArgumentOperation argument in creation.Arguments)
        {
            if (argument.Parameter?.Type.TypeKind == TypeKind.Delegate)
            {
                Report(context, argument.Value);
            }
        }
    }

    private static void Report(OperationAnalysisContext context, IOperation value)
    {
        if (Callbacks.Of(value) is { } callback && Callbacks.IsUnguarded(callback.Body))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, callback.Location));
        }
    }
}
