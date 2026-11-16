namespace Testing.Mutate.Tests;

/// <summary>A mutation that edits its own set is refused before anything runs (plans/00008, decision 8).</summary>
public sealed class OwnSetTests
{
    [Fact]
    public void A_set_that_edits_itself_is_refused_before_anything_runs()
    {
        (int exitCode, string output) = Tool.Run(RepoPaths.Root, ["--set", "tests/Testing.Mutate.Tests/fixtures/set-edits-its-own-set.json", "--check"]);

        Assert.True(exitCode == 1, output);
        Assert.Contains("\"the set rewrites itself\" edits the set that declares it", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_set_that_edits_another_file_is_not_refused_for_it()
    {
        (int exitCode, string output) = Tool.Run(RepoPaths.Root, ["--set", "tests/Testing.Mutate.Tests/fixtures/set-shards.json", "--check"]);

        Assert.True(exitCode == 0, output);
        Assert.DoesNotContain("edits the set that declares it", output, StringComparison.Ordinal);
    }
}
