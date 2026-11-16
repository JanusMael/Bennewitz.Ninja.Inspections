// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>This harness again, as the binary running now — so that no child rebuilds the script it runs from.</summary>
internal static class Self
{
    /// <summary>
    /// The binary to start and what to hand it first. Under the dotnet host that is the entry assembly beside
    /// <see cref="AppContext.BaseDirectory"/>; as an apphost, the apphost itself.
    /// </summary>
    public static (string File, string[] Launcher) Binary()
    {
        string self = Environment.ProcessPath ?? "dotnet";
        string[] launcher = string.Equals(Path.GetFileNameWithoutExtension(self), "dotnet", StringComparison.OrdinalIgnoreCase)
            ? [Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll")]
            : [];
        return (self, launcher);
    }

    /// <summary>
    /// Several sets one after another in this checkout, each a run of this harness judged on its own; answers
    /// 2 when any set was refused, 1 when any was not as declared, else 0.
    /// </summary>
    public static int EachSet(string repo, IReadOnlyList<string> sets, IReadOnlyList<string> passed)
    {
        (string self, string[] launcher) = Binary();
        List<int> codes = [];
        foreach (string set in sets)
        {
            Console.WriteLine($"=== {set}");
            ProcessStartInfo start = new(self) { UseShellExecute = false, WorkingDirectory = repo };
            foreach (string argument in (string[])[.. launcher, "--set", set, .. passed])
            {
                start.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(start) ?? throw new InvalidOperationException("could not start this harness again");
            process.WaitForExit();
            codes.Add(process.ExitCode);
            Console.WriteLine($"=== exit {process.ExitCode.ToString(CultureInfo.InvariantCulture)}");
        }

        int failed = codes.Count(c => c != 0);
        Console.WriteLine($"{codes.Count.ToString(CultureInfo.InvariantCulture)} set(s), {failed.ToString(CultureInfo.InvariantCulture)} not as declared");
        return codes.Contains(2) ? 2 : failed > 0 ? 1 : 0;
    }
}
