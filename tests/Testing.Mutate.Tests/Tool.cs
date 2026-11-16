using System.Diagnostics;

namespace Testing.Mutate.Tests;

/// <summary>The tool as this test project's build holds it, run through its real command line.</summary>
internal static class Tool
{
    /// <summary>Runs the tool in <paramref name="directory"/>, bounded, and answers its exit code and everything it printed.</summary>
    public static (int ExitCode, string Output) Run(string directory, IEnumerable<string> arguments, TimeSpan? bound = null)
    {
        ProcessStartInfo start = new()
        {
            FileName = "dotnet",
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Testing.Mutate.dll"));
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the tool.");

        // Both pipes drained concurrently: one read to its end first deadlocks once the child fills the other.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(bound ?? TimeSpan.FromMinutes(10)))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return (-1, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult() + "\n(killed: the tool ran past its bound)");
        }

        process.WaitForExit();
        return (process.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
    }
}
