// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says, and where plans/00009 says.
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

    /// <summary>
    /// Runs the tests, through <c>dotnet test --project</c>, or, given the built test assembly, through the in-process
    /// runner xUnit v3 builds into it, <c>dotnet &lt;assembly&gt; -class &lt;class&gt; -noColor</c> (plans/00009, decision 1).
    /// </summary>
    /// <remarks>
    /// <para>⭐ <b>Measured</b> over the smoke set's class, 2026-11-16: 5.40 s through <c>dotnet test --project</c>, which
    /// evaluates the project before the host starts, and 1.43 s run directly.</para>
    /// <para>⚠ <b>The direct run starts in the assembly's folder,</b> where <c>dotnet test</c> starts the test host, as
    /// measured on Windows, 2026-11-17, and as the runner sets its own folder anyway; a test that reads a file beside it
    /// finds what it finds under <c>dotnet test</c>. Its environment and bound are a test run's.</para>
    /// </remarks>
    /// <param name="classes">The classes to run, which add up; none for the whole project.</param>
    /// <param name="assembly">The test project's built assembly, to run it directly; null to run <c>dotnet test</c>.</param>
    public static string Test(string repo, string project, IReadOnlyList<string> classes, string? assembly = null) =>
        Judge.Plain((assembly is null
            ? Shell.Run(
                repo,
                "dotnet",
                [
                    "test", "--project", project, "--no-build", "-c", Options.Configuration, .. Options.PropertyArguments,
                    .. classes.SelectMany(c => (string[])["--filter-class", c]),
                ],
                RunBound,
                BuildEnvironment)
            : Shell.Run(
                Path.GetDirectoryName(assembly)!,
                "dotnet",
                [assembly, .. classes.SelectMany(c => (string[])["-class", c]), "-noColor"],
                RunBound,
                BuildEnvironment)).Output);

    /// <summary>A property as MSBuild evaluates the project for this run's builds: its last line of output, or null.</summary>
    public static string? Property(string repo, string project, string name)
    {
        Shell.Result result = Shell.Run(
            repo,
            "dotnet",
            ["msbuild", project, "-getProperty:" + name, "-p:Configuration=" + Options.Configuration, .. Options.PropertyArguments],
            TimeSpan.FromMinutes(5),
            BuildEnvironment);
        string? value = result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        return result.Exit == 0 && !string.IsNullOrEmpty(value) ? value : null;
    }
}
