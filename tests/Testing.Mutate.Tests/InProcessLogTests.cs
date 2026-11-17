namespace Testing.Mutate.Tests;

/// <summary>
/// The in-process runner's log, which a direct run prints (plans/00009, step 2), read as its <c>dotnet test</c> twin is:
/// each pair under <c>fixtures/runs/</c> is one run of the same tests captured both ways, on xUnit v3 3.2.2.
/// </summary>
/// <remarks>
/// <para>⭐ <b>A pair must read the same,</b> verdict, message, trace lines and failed names, and judge the same, so a
/// set's verdicts do not depend on which way its run took.</para>
/// <para>⛔ <b>Two of the pairs are what a count-based reading gets wrong:</b> a filter that matches nothing prints
/// <c>Total: 0</c> and exits 0, and a class fixture whose cleanup throws fails no test by name and counts an error.</para>
/// </remarks>
public sealed class InProcessLogTests
{
    private static string Run(string name, string way) =>
        RepoPaths.Source(Path.Combine("tests", "Testing.Mutate.Tests", "fixtures", "runs", $"{name}.{way}.log"));

    [Theory]
    [InlineData("killed", "red", "MarkupUnderObj_IsNotScanned", "Expected: 0", "ok")]
    [InlineData("killed", "red", "Some_test_that_never_ran", null, "wrong-killer")]
    [InlineData("killed", "red", "MarkupUnderObj_IsNotScanned", "words no assertion said", "wrong-assertion")]
    [InlineData("theory", "red", "A_row_fails", "Actual:   1.5", "ok")]
    [InlineData("host-died", "red", null, null, "aborted")]
    [InlineData("host-died", "green", null, null, "unexpected-red")]
    [InlineData("cleanup-error", "red", null, null, "aborted")]
    [InlineData("cleanup-error", "green", null, null, "unexpected-red")]
    [InlineData("green", "green", null, null, "ok")]
    [InlineData("green", "red", null, null, "survived")]
    [InlineData("no-tests", "green", null, null, "unexpected-red")]
    [InlineData("no-tests", "red", null, null, "aborted")]
    public void A_direct_run_is_judged_as_its_dotnet_test_twin_is(string name, string expect, string? killer, string? says, string outcome)
    {
        (int directExit, string direct) = Judge(Run(name, "direct"), expect, killer, says);
        (int twinExit, string twin) = Judge(Run(name, "dotnet-test"), expect, killer, says);

        Assert.Equal(twin, direct);
        Assert.Equal(twinExit, directExit);
        Assert.Equal(outcome, direct.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[^1]);
        Assert.Equal(outcome == "ok" ? 0 : 1, directExit);
    }

    [Fact]
    public void A_direct_kill_names_the_failed_test_its_message_and_where_its_trace_stopped()
    {
        (int exitCode, string output) = Judge(Run("killed", "direct"), "red", null, null);

        Assert.True(exitCode == 0, output);
        Assert.Contains("red Assert.Equal() Failure: Values differ at 142 [MarkupUnderObj_IsNotScanned]", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_failed_theory_row_is_named_by_its_method()
    {
        (int exitCode, string output) = Judge(Run("theory", "direct"), "red", null, null);

        Assert.True(exitCode == 0, output);
        Assert.Contains("at 10 [A_row_fails]", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_summary_that_prints_every_count_for_no_tests_is_red()
    {
        // The captured empty filter prints `Total: 0` alone, which a summary missing a count already reads as red; this is
        // the green capture with its summary line given every count at none, so the floor on `Total` alone decides it.
        string green = File.ReadAllText(Run("green", "direct"));
        string line = green.Split('\n').Single(l => l.Contains("Total: 1, Errors: 0, Failed: 0,", StringComparison.Ordinal));
        string log = Path.Combine(Directory.CreateTempSubdirectory("mutate-log-").FullName, "no-tests-counted.log");
        File.WriteAllText(log, green.Replace(line, line.Replace("Total: 1,", "Total: 0,", StringComparison.Ordinal), StringComparison.Ordinal));
        try
        {
            (int exitCode, string output) = Judge(log, "green", null, null);

            Assert.Equal(1, exitCode);
            Assert.EndsWith("unexpected-red", output.TrimEnd(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(log)!, recursive: true);
        }
    }

    private static (int ExitCode, string Output) Judge(string log, string expect, string? killer, string? says)
    {
        List<string> arguments = ["--judge", log, expect];
        if (killer is not null)
        {
            arguments.AddRange(["--killer", killer]);
        }

        if (says is not null)
        {
            arguments.AddRange(["--says", says]);
        }

        (int exitCode, string output) = Tool.Run(RepoPaths.Root, arguments);
        return (exitCode, output.ReplaceLineEndings("\n"));
    }
}
