// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

internal static class Shell
{
    /// <param name="Output">Standard output and standard error, in that order: what a reader of the run is shown.</param>
    /// <param name="Stdout">Standard output alone: what a caller parses, which a warning on standard error must not reach.</param>
    public readonly record struct Result(int Exit, string Output, string Stdout);

    public static Result Run(
        string repo, string tool, string[] arguments, TimeSpan? bound = null, IReadOnlyDictionary<string, string>? environment = null)
    {
        ProcessStartInfo start = new(tool)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = repo,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        foreach ((string name, string value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[name] = value;
        }

        using Process process = Process.Start(start) ?? throw new InvalidOperationException($"could not start {tool}");

        // Both pipes drained concurrently: reading one to its end first deadlocks once the child
        // fills the other's buffer.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(bound ?? Timeout.InfiniteTimeSpan))
        {
            // Bounded, and said: a run that is killed reports that it was, which no reading can
            // mistake for a pass — there is no `Passed!` in it.
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            string partial = stdout.GetAwaiter().GetResult();
            return new Result(-1, $"{partial}{stderr.GetAwaiter().GetResult()}\nmutate: killed after {bound!.Value.TotalMinutes.ToString(CultureInfo.InvariantCulture)} minutes\n", partial);
        }

        process.WaitForExit();
        string output = stdout.GetAwaiter().GetResult();
        return new Result(process.ExitCode, output + stderr.GetAwaiter().GetResult(), output);
    }
}
