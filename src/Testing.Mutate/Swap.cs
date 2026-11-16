namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>
/// The assembly swap, <c>--swap &lt;project&gt;</c> (plans/00008, decision 5): a mutation in a project named so builds that
/// project alone and copies its assembly over the one in the test project's output, rather than building the test
/// project.
/// </summary>
/// <remarks>
/// <para>⭐ <b>Measured:</b> over Inspections' smoke set, nine mutations of its rules library each way, on a machine
/// other builds kept busy, took 14.0 s on average swapped and 17.9 s built, 2026-11-16. Most of what is left is the
/// test run, which <c>dotnet test --project</c> starts by evaluating the project.</para>
/// <para>⚠ <b>Only the swapped project's own build checks the mutation:</b> one that changes what the project shows the
/// projects referencing it is not compiled against them, and can fail at run time where a build would have refused it.
/// Swap a project whose mutations stay inside it.</para>
/// <para>⚠ <b>A mutation to what restore reads is never swapped, nor the one after it</b>
/// (<see cref="Mutation.RestoresAfter"/>): each builds the test project, restoring.</para>
/// </remarks>
/// <param name="Project">The swapped project, as <c>--swap</c> named it.</param>
/// <param name="Folder">The project's folder, ending in a separator: a mutation of a file under it is swapped.</param>
/// <param name="Assembly">The project's built assembly.</param>
/// <param name="TestOutput">The test project's output folder, which holds a copy of the assembly.</param>
internal sealed record Swap(string Project, string Folder, string Assembly, string TestOutput)
{
    private static StringComparison Paths => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>Whether the mutation's file is under the swapped project's folder.</summary>
    public bool Takes(string repo, Mutation mutation) =>
        Path.GetFullPath(Path.Combine(repo, mutation.File.Replace('/', Path.DirectorySeparatorChar))).StartsWith(Folder, Paths);

    /// <summary>Builds the project alone, without a restore, and copies its assembly, and its symbols where it has them, into the test output.</summary>
    public bool Build(string repo, out string log)
    {
        if (!Dotnet.Build(repo, Project, out log, restore: false))
        {
            return false;
        }

        foreach (string built in (string[])[Assembly, Path.ChangeExtension(Assembly, ".pdb")])
        {
            if (File.Exists(built))
            {
                File.Copy(built, Path.Combine(TestOutput, Path.GetFileName(built)), overwrite: true);
            }
        }

        return true;
    }

    /// <summary>
    /// Reads from MSBuild, once the baseline has built both, where the project's assembly is and where the test project
    /// writes its output; null, with the fault, for a project that is the test project, builds no single assembly, or
    /// whose assembly the test output does not hold, since its tests would never run what was swapped.
    /// </summary>
    public static Swap? Resolve(string repo, string testProject, string project, out string fault)
    {
        fault = string.Empty;
        string full = Path.GetFullPath(Path.Combine(repo, project));
        if (string.Equals(full, Path.GetFullPath(Path.Combine(repo, testProject)), Paths))
        {
            fault = $"--swap {project}: that is the test project, which every mutation builds anyway";
            return null;
        }

        string? assembly = Property(repo, project, "TargetPath");
        string? output = Property(repo, testProject, "TargetDir");
        if (assembly is null || output is null)
        {
            fault = $"--swap {project}: MSBuild names no single assembly for it, or no output for {testProject}; a project that targets several frameworks is not swapped";
            return null;
        }

        if (!File.Exists(Path.Combine(output, Path.GetFileName(assembly))))
        {
            fault = $"--swap {project}: {testProject}'s output, {output}, holds no {Path.GetFileName(assembly)}, so its tests would never run it";
            return null;
        }

        return new Swap(project, Path.GetDirectoryName(full)! + Path.DirectorySeparatorChar, assembly, output);
    }

    /// <summary>A property as MSBuild evaluates the project for this run's builds: its last line of output, or null.</summary>
    private static string? Property(string repo, string project, string name)
    {
        Shell.Result result = Shell.Run(
            repo,
            "dotnet",
            ["msbuild", project, "-getProperty:" + name, "-p:Configuration=" + Options.Configuration, .. Options.PropertyArguments],
            TimeSpan.FromMinutes(5),
            Dotnet.BuildEnvironment);
        string? value = result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        return result.Exit == 0 && !string.IsNullOrEmpty(value) ? value : null;
    }
}
