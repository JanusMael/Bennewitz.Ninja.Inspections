namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>
/// What every build and test run of this process is given, and what a worktree's run is handed so it is given
/// the same: the configuration, the MSBuild properties, the swapped projects and the budget (plans/00008, decisions 3,
/// 4 and 5).
/// </summary>
internal static class Options
{
    /// <summary>The budget a run is held to unless <c>--budget</c> names another: the family's rule of 2026-10-09.</summary>
    public static readonly TimeSpan DefaultBudget = TimeSpan.FromMinutes(30);

    /// <summary>The configuration every build and test run uses: <c>Debug</c> unless <c>--configuration</c> says otherwise.</summary>
    public static string Configuration { get; set; } = "Debug";

    /// <summary>Each <c>--property name=value</c>, in the order given.</summary>
    public static List<string> Properties { get; } = [];

    /// <summary>How long a run is estimated to take before it is refused.</summary>
    public static TimeSpan Budget { get; set; } = DefaultBudget;

    /// <summary>Each <c>--swap &lt;project&gt;</c>, relative to the repository: the projects whose mutations are swapped (<see cref="Swap"/>).</summary>
    public static List<string> Swaps { get; } = [];

    /// <summary>The properties as <c>dotnet build</c> and <c>dotnet test</c> take them.</summary>
    public static IEnumerable<string> PropertyArguments => Properties.Select(p => "-p:" + p);

    /// <summary>These options as a run in a worktree is handed them, so it builds and tests as this one would.</summary>
    public static IEnumerable<string> Forwarded =>
    [
        "--configuration", Configuration,
        .. Properties.SelectMany(p => (string[])["--property", p]),
        .. Swaps.SelectMany(s => (string[])["--swap", s]),
        "--budget", Budget.TotalMinutes.ToString(CultureInfo.InvariantCulture),
    ];
}
