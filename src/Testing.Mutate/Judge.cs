// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>What a mutation says must happen to the run it is judged by.</summary>
/// <param name="Expect">`red` or `green`.</param>
/// <param name="Killer">For a red mutation, the test that must be among the failures, or null.</param>
/// <param name="Says">Text the run must contain, or null.</param>
internal sealed record Declared(string Expect, string? Killer, string? Says);

/// <summary>What a run of the filtered tests said: the verdict, the tests that failed, and where the first trace stopped.</summary>
internal readonly record struct Reading(string Verdict, string Message, IReadOnlyList<int> Lines, IReadOnlyList<string> Failed)
{
    public static Reading NoOp(int matches) =>
        new("no-op", $"matched {matches.ToString(CultureInfo.InvariantCulture)} times, not once", [], []);

    public static Reading Unbuildable(string log) =>
        new("build", log.Length > 400 ? log[^400..] : log, [], []);

    public override string ToString()
    {
        string failed = Failed.Count == 0 ? string.Empty : $" [{string.Join(", ", Failed)}]";
        return Lines.Count == 0
            ? $"{Verdict} {Message}{failed}".TrimEnd()
            : $"{Verdict} {Message} at {string.Join(',', Lines)}{failed}".Trim();
    }
}

/// <summary>
/// Reads a `dotnet test` log. ⛔ The outcome line is load-bearing and no arithmetic replaces it: an
/// aborted host prints `Failed!` with `failed: 0`, which every count-based reading calls healthy —
/// `AGENTS.md` §5 and `scripts/catch-crash`. So anything that is not an explicit `Passed!` is red.
/// </summary>
internal static partial class Judge
{
    /// <summary>The one outcome that is not a failure of the run.</summary>
    public const string Ok = "ok";

    public static Reading Read(string log)
    {
        bool passed = log.Contains("Test run summary: Passed!", StringComparison.Ordinal);
        string message = string.Empty;
        foreach (string raw in log.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("Assert.", StringComparison.Ordinal))
            {
                message = line;
                break;
            }
        }

        List<int> lines = [];
        foreach (Match m in TraceLine().Matches(log))
        {
            int at = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            if (!lines.Contains(at))
            {
                lines.Add(at);
            }
        }

        lines.Sort();

        List<string> failed = [];
        foreach (Match m in FailedTest().Matches(log))
        {
            string name = m.Groups["name"].Value;
            if (!failed.Contains(name))
            {
                failed.Add(name);
            }
        }

        return new Reading(passed ? "green" : "red", message, lines, failed);
    }

    /// <summary>
    /// Compares a reading against what was declared. ⛔ Every outcome but <see cref="Ok"/> fails the
    /// run, including the three that look like a kill: a red with no test failed by name is a host
    /// that died, a red that did not fail the declared killer was some other test's, and a killer
    /// that failed without the declared words failed on some other assertion.
    /// </summary>
    public static string Outcome(Reading read, Declared declared, string log)
    {
        if (declared.Expect == "green")
        {
            return read.Verdict == "green" ? Ok : "unexpected-red";
        }

        if (read.Verdict == "green")
        {
            return "survived";
        }

        if (read.Failed.Count == 0)
        {
            return "aborted";
        }

        if (declared.Killer is { } killer && !read.Failed.Contains(killer))
        {
            return "wrong-killer";
        }

        return declared.Says is { } says && !log.Contains(says, StringComparison.Ordinal)
            ? "wrong-assertion"
            : Ok;
    }

    /// <summary>A stack frame in a source file: the line its trace stopped on.</summary>
    [GeneratedRegex(@"[^\s:(]+\.cs:(?:line )?(\d+)")]
    private static partial Regex TraceLine();

    /// <summary>
    /// A failed test as Microsoft.Testing.Platform prints one: <c>failed</c>, the fully qualified
    /// name, then its duration in parentheses. The name is the last segment.
    /// </summary>
    [GeneratedRegex(@"(?m)^failed\s+\S*?\.(?<name>\w+)[\s(]")]
    private static partial Regex FailedTest();
}
