// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

internal static class Dotnet
{
    /// <summary>How long one test run may take before it is killed and reported, rather than waited on for ever.</summary>
    private static readonly TimeSpan RunBound = TimeSpan.FromMinutes(10);

    /// <summary>
    /// What every build and test run this harness starts is given over the environment it inherits.
    /// Avalonia's build telemetry off (DECISIONS.md, 2026-10-22): the collector its build task starts
    /// before each compile outlived a build by three quarters of an hour and kept the worktree it ran in.
    /// MSBuild's node reuse off: a worker node outlives its build and is handed to the next, whichever
    /// session starts it, so anything that ends a node ends the build holding it, and one run of the gates
    /// lost three mutations to MSB4166 that way. One build timestamp: AutoVersioning otherwise takes one afresh
    /// at each evaluation and hands it to the compiler, so every build recompiles every project however
    /// little changed.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildEnvironment { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AVALONIA_TELEMETRY_OPTOUT"] = "1",
            ["MSBUILDDISABLENODEREUSE"] = "1",
            ["BuildTimestamp"] = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
        };

    /// <param name="restore">False once a build has restored and nothing restore reads has changed since:
    /// measured, a mutation's build takes 4.5 s without it where it took 6.4.</param>
    public static bool Build(string repo, string project, out string log, bool restore = true)
    {
        Shell.Result result = Shell.Run(
            repo,
            "dotnet",
            restore
                ? ["build", project, "-c", Options.Configuration, "--nologo", "-v", "q", .. Options.PropertyArguments]
                : ["build", project, "-c", Options.Configuration, "--nologo", "-v", "q", "--no-restore", .. Options.PropertyArguments],
            RunBound,
            BuildEnvironment);
        log = result.Output;
        return result.Exit == 0;
    }

    /// <param name="classes">The classes to run, each its own <c>--filter-class</c>, which add up; none for the whole project.</param>
    public static string Test(string repo, string project, IReadOnlyList<string> classes) =>
        Shell.Run(
            repo,
            "dotnet",
            [
                "test", "--project", project, "--no-build", "-c", Options.Configuration, .. Options.PropertyArguments,
                .. classes.SelectMany(c => (string[])["--filter-class", c]),
            ],
            RunBound,
            BuildEnvironment).Output;
}
