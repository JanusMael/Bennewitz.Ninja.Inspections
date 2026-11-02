using Microsoft.CodeAnalysis;

namespace Bennewitz.Ninja.CodeQuality;

/// <summary>
/// What the System.Reactive rules share: its types, found by metadata name.
/// </summary>
/// <remarks>
/// ⛔ <b>No reference to System.Reactive.</b> The analyzer ships with no dependency, so the types are
/// looked up in the consumer's compilation. One that cannot name <see cref="Observable"/> does not use
/// Rx, and the rules register nothing there.
/// </remarks>
internal static class Reactive
{
    /// <summary>The class that declares the operators.</summary>
    public const string Observable = "System.Reactive.Linq.Observable";

    /// <summary>The class that declares the <c>Subscribe</c> overloads taking delegates.</summary>
    public const string ObservableExtensions = "System.ObservableExtensions";

    /// <summary>What an operator that waits is given to wait on.</summary>
    public const string Scheduler = "System.Reactive.Concurrency.IScheduler";

    /// <summary>Whether <paramref name="method"/> is declared by <paramref name="type"/>.</summary>
    public static bool IsDeclaredBy(IMethodSymbol method, INamedTypeSymbol? type) =>
        type is not null && SymbolEqualityComparer.Default.Equals(method.ContainingType, type);
}
