// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says, and where plans/00009 says.
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
/// Reads a test run's log: <c>dotnet test</c>'s, or, by plans/00009, the in-process runner's that xUnit v3 builds into a
/// test assembly. ⛔ The outcome line is load-bearing and no arithmetic replaces it: an
/// aborted host prints `Failed!` with `failed: 0`, which every count-based reading calls healthy —
/// `AGENTS.md` §5 and `scripts/catch-crash`. So anything that is not an explicit `Passed!` is red, and in the
/// in-process runner's log, anything that is not a summary of more than no tests with no error and no failure.
/// </summary>
internal static partial class Judge
{
    /// <summary>The one outcome that is not a failure of the run.</summary>
    public const string Ok = "ok";

    /// <summary>
    /// The log without the terminal's escape sequences, which is how every log is read. ⛔ Measured on GitHub's
    /// runners (plans/00008): the test host colours its output where it takes the run for CI's, as
    /// <c>ESC[31mfailed ESC[m Name</c>, so a failed test went unnamed and every kill read as <c>aborted</c>.
    /// </summary>
    public static string Plain(string log) => Escapes().Replace(log, string.Empty);

    /// <summary>
    /// Whether the log is the in-process runner's: its banner, then its summary's heading (plans/00009, decision 2). A
    /// run that printed no summary is not, so a host that died before it reads as no direct run at all.
    /// </summary>
    public static bool InProcess(string log)
    {
        int banner = log.IndexOf(InProcessBanner, StringComparison.Ordinal);
        return banner >= 0 && log.IndexOf(SummaryHeading, banner, StringComparison.Ordinal) > banner;
    }

    public static Reading Read(string log)
    {
        bool inProcess = log.Contains(InProcessBanner, StringComparison.Ordinal);
        bool passed = inProcess ? InProcessPassed(log) : log.Contains("Test run summary: Passed!", StringComparison.Ordinal);
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
            Group at = m.Groups["line"].Success ? m.Groups["line"] : m.Groups["paren"];
            int number = int.Parse(at.Value, CultureInfo.InvariantCulture);
            if (!lines.Contains(number))
            {
                lines.Add(number);
            }
        }

        lines.Sort();

        List<string> failed = [];
        foreach (Match m in (inProcess ? InProcessFailedTest() : FailedTest()).Matches(log))
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

    /// <summary>The first line the in-process runner prints, before its version.</summary>
    private const string InProcessBanner = "xUnit.net v3 In-Process Runner";

    /// <summary>What the in-process runner prints before its summary lines, one for each assembly.</summary>
    private const string SummaryHeading = "=== TEST EXECUTION SUMMARY ===";

    /// <summary>
    /// ⛔ Green only on a summary, after its heading, whose every assembly line ran more than no tests with no error and no
    /// failure. Measured, 2026-11-17: a filter that matches nothing prints <c>Total: 0</c> alone and exits 0, so a
    /// reading by the exit code, or by the absence of a failure, calls it a pass.
    /// </summary>
    private static bool InProcessPassed(string log)
    {
        int heading = log.LastIndexOf(SummaryHeading, StringComparison.Ordinal);
        if (heading < 0)
        {
            return false;
        }

        string summary = log[(heading + SummaryHeading.Length)..];
        bool any = false;
        foreach (string raw in summary.Split('\n'))
        {
            if (raw.Trim().Length == 0)
            {
                continue;
            }

            Match totals = SummaryTotals().Match(raw);
            if (!totals.Success
                || int.Parse(totals.Groups["total"].Value, CultureInfo.InvariantCulture) == 0
                || totals.Groups["errors"].Value != "0"
                || totals.Groups["failed"].Value != "0")
            {
                return false;
            }

            any = true;
        }

        return any;
    }

    /// <summary>
    /// A stack frame in a source file: the line its trace stopped on, as <c>dotnet test</c> prints it,
    /// <c>in File.cs:142</c> or <c>File.cs:line 142</c>, or as the in-process runner does, <c>File.cs(142,0): at</c>.
    /// </summary>
    [GeneratedRegex(@"[^\s:(]+\.cs(?::(?:line )?(?<line>\d+)|\((?<paren>\d+),\d+\))")]
    private static partial Regex TraceLine();

    /// <summary>
    /// A failed test as Microsoft.Testing.Platform prints one: <c>failed</c>, the fully qualified
    /// name, then its duration in parentheses. The name is the last segment.
    /// </summary>
    [GeneratedRegex(@"(?m)^failed\s+\S*?\.(?<name>\w+)[\s(]")]
    private static partial Regex FailedTest();

    /// <summary>
    /// A failed test as the in-process runner prints one: the fully qualified name, a theory row's arguments after it,
    /// then <c>[FAIL]</c>. The name is the method's, the last segment before any arguments.
    /// </summary>
    [GeneratedRegex(@"(?m)^\s*\S*?\.(?<name>\w+)(?:\(.*\))?[ \t]+\[FAIL\][ \t]*\r?$")]
    private static partial Regex InProcessFailedTest();

    /// <summary>One assembly's line of the in-process runner's summary, with every count it prints for a run that ran.</summary>
    [GeneratedRegex(@"\bTotal: (?<total>\d+), Errors: (?<errors>\d+), Failed: (?<failed>\d+), Skipped: \d+, Not Run: \d+, Time: ")]
    private static partial Regex SummaryTotals();

    /// <summary>A control sequence: ESC, <c>[</c>, its parameters, and the letter that ends it.</summary>
    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex Escapes();
}
