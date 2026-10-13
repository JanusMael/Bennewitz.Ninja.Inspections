using Bennewitz.Ninja.XamlQuality.Rules;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// <see cref="PartsRunner"/>: the configured parts report's reading, from assemblies loaded apart from the tool's own and
/// the themes of every markup folder.
/// </summary>
public sealed class PartsRunnerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("theme-parts-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private void Theme(string folder, string part)
    {
        Directory.CreateDirectory(Path.Combine(_root, folder));
        File.WriteAllText(
            Path.Combine(_root, folder, "Gauge.axaml"),
            "<ResourceDictionary xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\""
            + " xmlns:f=\"clr-namespace:Fixtures\">\n"
            + $"<ControlTheme x:Key=\"k\" TargetType=\"f:Gauge\"><Setter Property=\"Template\"><ControlTemplate><Border Name=\"{part}\" />"
            + "</ControlTemplate></Setter></ControlTheme>\n</ResourceDictionary>");
    }

    private AuditConfig Config(params string[] markup) => AuditConfig.Parse(
        $$"""
        {
          "parts": {
            "report": "docs/template-parts.md",
            "markup": [{{string.Join(", ", markup.Select(folder => $"\"{folder}\""))}}],
            "assemblies": ["{{EmittedParts.Assembly.Replace('\\', '/')}}"]
          }
        }
        """,
        _root);

    /// <summary>
    /// The code is read once, and the themes of every markup folder are the control's, each file relative to the
    /// configuration, by file, then line.
    /// </summary>
    [Fact]
    public void EveryMarkupFoldersThemes_AreRead_RelativeToTheConfiguration()
    {
        Theme("second/themes", "PART_Dial");
        Theme("first/themes", "PART_Needle");

        TemplatePartReading reading = PartsRunner.Read(Config("second", "first"));

        ControlTemplateParts gauge = Assert.Single(reading.Controls);
        Assert.Equal("Gauge", gauge.Name);
        Assert.Equal(["PART_Needle"], gauge.Constants);
        Assert.Equal(
            [new TemplatePartLookup("PART_Dial", TemplatePartOrigin.Own, "Fixtures.Gauge", "Apply", "PartsFixture")],
            gauge.Lookups);
        Assert.Equal(["first/themes/Gauge.axaml:2", "second/themes/Gauge.axaml:2"], gauge.Themes.Select(theme => $"{theme.File}:{theme.Line}"));
        Assert.Equal(["PART_Needle"], gauge.Themes[0].Declared.Select(part => part.Name));
        Assert.Empty(reading.Skipped);
    }

    /// <summary>A folder inside another the configuration names, listed before it or after, or one named twice, is read once.</summary>
    [Fact]
    public void AFolderInsideAnotherOrNamedTwice_IsReadOnce()
    {
        Theme("first", "PART_Dial");
        Theme("first/themes", "PART_Needle");

        TemplatePartReading reading = PartsRunner.Read(Config("first/themes", "first", "first/", "first/themes/"));

        Assert.Equal(
            ["first/Gauge.axaml:2", "first/themes/Gauge.axaml:2"],
            Assert.Single(reading.Controls).Themes.Select(theme => $"{theme.File}:{theme.Line}"));
    }

    [Fact]
    public void AConfigurationWithNoPartsReport_IsAConfigurationError()
    {
        AuditConfigException error = Assert.Throws<AuditConfigException>(() => PartsRunner.Read(AuditConfig.Parse("{}", _root)));

        Assert.Contains("no parts report", error.Message, StringComparison.Ordinal);
    }
}
