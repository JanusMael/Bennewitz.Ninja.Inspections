using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// The audit's scans leave out the drift check's received copies, <c>name.received.ext</c>, which a stale check writes
/// beside a committed output and which would otherwise read as one more file of the theme or consumer they sit in.
/// </summary>
public sealed class ReceivedCopyTests : IDisposable
{
    private const string Palette =
        "<ResourceDictionary xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\""
        + " x:Class=\"Fixtures.Palette\"><Color x:Key=\"Accent\">#123456</Color></ResourceDictionary>";

    private readonly string _root = Directory.CreateTempSubdirectory("theme-received-").FullName;

    public ReceivedCopyTests()
    {
        File.WriteAllText(Path.Combine(_root, "Palette.axaml"), Palette);
        File.WriteAllText(Path.Combine(_root, "Palette.received.axaml"), Palette);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void A_consumers_files_leave_out_a_received_copy()
    {
        Assert.Equal([Path.Combine(_root, "Palette.axaml")], XamlFiles.Enumerate(_root));
    }

    [Fact]
    public void A_key_scan_leaves_out_a_received_copy()
    {
        ResourceKey key = Assert.Single(ResourceKeyScanner.Scan(_root));
        Assert.Equal(Path.Combine(_root, "Palette.axaml"), key.File);
    }

    /// <summary>A received copy of a compiled dictionary would make its <c>x:Class</c> ambiguous, and a theme that includes it unreadable.</summary>
    [Fact]
    public void An_x_Class_index_leaves_out_a_received_copy()
    {
        Assert.True(XClassIndex.Build(_root).TryResolve("Palette", out string file, out string reason), reason);
        Assert.Equal(Path.Combine(_root, "Palette.axaml"), file);
    }

    [Fact]
    public void A_received_copy_is_named_as_the_drift_check_names_one()
    {
        string[] names = ["Compat.received.axaml", "theme-audit.received.md", "README.received", "Compat.axaml", "received.axaml", "Compat.Received.axaml"];

        Assert.Equal([true, true, true, false, false, false], names.Select(name => OutputDrift.IsReceived(Path.Combine(_root, name))));
    }
}
