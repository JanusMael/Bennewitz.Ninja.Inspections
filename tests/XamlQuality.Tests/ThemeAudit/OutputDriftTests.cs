using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// <see cref="OutputDrift"/>, the drift check the tool's <c>--check</c> and a consumer's test share: a committed output
/// against what a fresh run writes.
/// </summary>
public sealed class OutputDriftTests : IDisposable
{
    private const string Fresh = "# Report\n\n| A | B |\n|---|---|\n";

    private readonly string _root = Directory.CreateTempSubdirectory("theme-drift-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private string Committed => Path.Combine(_root, "report.md");

    private string Received => Path.Combine(_root, "report.received.md");

    [Fact]
    public void An_output_that_is_what_a_fresh_run_writes_is_up_to_date_and_nothing_is_written()
    {
        File.WriteAllText(Committed, Fresh);

        Assert.Equal(DriftState.UpToDate, OutputDrift.Check(Committed, Fresh));
        Assert.False(File.Exists(Received));
    }

    /// <summary>A checkout that writes CRLF, as git does on Windows, is not drift.</summary>
    [Fact]
    public void A_CRLF_copy_of_an_LF_output_is_up_to_date()
    {
        File.WriteAllText(Committed, Fresh.Replace("\n", "\r\n", StringComparison.Ordinal));

        Assert.Equal(DriftState.UpToDate, OutputDrift.Check(Committed, Fresh));
    }

    [Fact]
    public void A_stale_output_is_reported_and_the_fresh_one_is_written_beside_it()
    {
        File.WriteAllText(Committed, Fresh + "edited\n");

        Assert.Equal(DriftState.Stale, OutputDrift.Check(Committed, Fresh));
        Assert.Equal(Fresh, File.ReadAllText(Received));
        Assert.Equal(Fresh + "edited\n", File.ReadAllText(Committed));
    }

    [Fact]
    public void A_missing_output_is_reported_and_nothing_is_written()
    {
        Assert.Equal(DriftState.Missing, OutputDrift.Check(Committed, Fresh));
        Assert.False(File.Exists(Committed));
        Assert.False(File.Exists(Received));
    }

    /// <summary>An up-to-date check removes the received copy an earlier stale check left, so none outlives the drift.</summary>
    [Fact]
    public void An_up_to_date_check_removes_a_received_copy_an_earlier_check_left()
    {
        File.WriteAllText(Committed, Fresh + "edited\n");
        OutputDrift.Check(Committed, Fresh);
        File.WriteAllText(Committed, Fresh);

        Assert.Equal(DriftState.UpToDate, OutputDrift.Check(Committed, Fresh));
        Assert.False(File.Exists(Received));
    }

    [Theory]
    [InlineData("theme-audit.md", "theme-audit.received.md")]
    [InlineData("Compat.axaml", "Compat.received.axaml")]
    [InlineData("README", "README.received")]
    public void The_received_copy_is_named_beside_the_output_with_its_extension(string name, string received)
    {
        Assert.Equal(Path.Combine(_root, received), OutputDrift.ReceivedPathOf(Path.Combine(_root, name)));
    }
}
