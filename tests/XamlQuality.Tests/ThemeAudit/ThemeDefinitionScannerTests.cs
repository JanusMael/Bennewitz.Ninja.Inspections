using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

public sealed class ThemeDefinitionScannerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("theme-def-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Attributes_keys_to_their_theme_variant_and_reads_colour_values()
    {
        WriteFile("Theme.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                  <Color x:Key="PageBackground">#FFFFFF</Color>
                  <SolidColorBrush x:Key="Accent" Color="#2E7D32" />
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                  <Color x:Key="PageBackground">#1E1E1E</Color>
                  <SolidColorBrush x:Key="Accent" Color="{DynamicResource AccentColor}" />
                </ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
              <Thickness x:Key="StandardPadding">4</Thickness>
            </ResourceDictionary>
            """);

        IReadOnlyList<DefinedResource> defined = ThemeDefinitionScanner.Scan(_root);

        DefinedResource lightBackground = Single(defined, "Light", "PageBackground");
        Assert.Equal(new ResourceValue.ColorLiteral(new AuditColor(0xFF, 0xFF, 0xFF, 0xFF)), lightBackground.Value);

        DefinedResource lightAccent = Single(defined, "Light", "Accent");
        Assert.Equal(new ResourceValue.ColorLiteral(new AuditColor(0xFF, 0x2E, 0x7D, 0x32)), lightAccent.Value);

        DefinedResource darkBackground = Single(defined, "Dark", "PageBackground");
        Assert.Equal(new ResourceValue.ColorLiteral(new AuditColor(0xFF, 0x1E, 0x1E, 0x1E)), darkBackground.Value);

        // A brush's Color names a colour: the alias says so, since a brush there would not load.
        DefinedResource darkAccent = Single(defined, "Dark", "Accent");
        Assert.Equal(new ResourceValue.Alias("AccentColor") { FromColor = true }, darkAccent.Value);

        // A base resource (outside ThemeDictionaries) has no variant and is not colour-scored.
        DefinedResource padding = Single(defined, null, "StandardPadding");
        Assert.IsType<ResourceValue.Opaque>(padding.Value);
    }

    [Fact]
    public void The_variant_container_dictionaries_are_not_themselves_defined_keys()
    {
        WriteFile("Theme.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                  <Color x:Key="Only">#000000</Color>
                </ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);

        IReadOnlyList<DefinedResource> defined = ThemeDefinitionScanner.Scan(_root);
        DefinedResource only = Assert.Single(defined);
        Assert.Equal("Only", only.Key);
        Assert.Equal("Light", only.Variant);
    }

    [Fact]
    public void Reads_a_static_resource_alias_and_a_type_keyed_control_theme()
    {
        WriteFile("Aliases.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <StaticResource x:Key="ButtonBackground" ResourceKey="Accent" />
              <ControlTheme x:Key="{x:Type Button}" TargetType="Button" />
            </ResourceDictionary>
            """);

        IReadOnlyList<DefinedResource> defined = ThemeDefinitionScanner.Scan(_root);

        Assert.Equal(new ResourceValue.Alias("Accent"), Single(defined, null, "ButtonBackground").Value);
        DefinedResource controlTheme = Single(defined, null, "{x:Type Button}");
        Assert.IsType<ResourceValue.Opaque>(controlTheme.Value);
    }

    [Fact]
    public void Reads_a_brushs_Color_and_Opacity_written_as_a_resource_or_as_a_property_element()
    {
        WriteFile("Brushes.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="StaticOpacity" Color="#FFFFFF" Opacity="{StaticResource Faint}" />
              <SolidColorBrush x:Key="DynamicOpacity" Color="{DynamicResource Ink}" Opacity="{DynamicResource Faint}" />
              <SolidColorBrush x:Key="Elements">
                <SolidColorBrush.Color>#FFFFFF</SolidColorBrush.Color>
                <SolidColorBrush.Opacity>0.12</SolidColorBrush.Opacity>
              </SolidColorBrush>
              <SolidColorBrush x:Key="ElementResources">
                <SolidColorBrush.Color><StaticResource ResourceKey="Ink" /></SolidColorBrush.Color>
                <SolidColorBrush.Opacity><StaticResource ResourceKey="Faint" /></SolidColorBrush.Opacity>
              </SolidColorBrush>
            </ResourceDictionary>
            """);

        IReadOnlyList<DefinedResource> defined = ThemeDefinitionScanner.Scan(_root);

        AuditColor white = new(0xFF, 0xFF, 0xFF, 0xFF);
        Assert.Equal(new ResourceValue.ColorLiteral(white) { OpacityKey = "Faint" }, Single(defined, null, "StaticOpacity").Value);
        Assert.Equal(new ResourceValue.Alias("Ink") { OpacityKey = "Faint", FromColor = true }, Single(defined, null, "DynamicOpacity").Value);
        Assert.Equal(new ResourceValue.ColorLiteral(white, 0.12), Single(defined, null, "Elements").Value);
        Assert.Equal(new ResourceValue.Alias("Ink") { OpacityKey = "Faint", FromColor = true }, Single(defined, null, "ElementResources").Value);
    }

    [Fact]
    public void A_brush_whose_Color_or_Opacity_it_cannot_read_is_not_colour_scored()
    {
        // A binding, a resource inside another extension, and an opacity that is no number: each would
        // be a guess, and an opacity read as 1 scores a translucent brush as opaque.
        WriteFile("Brushes.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="BoundOpacity" Color="#FFFFFF" Opacity="{Binding Fade}" />
              <SolidColorBrush x:Key="ConvertedColor" Color="{Binding Ink, Converter={StaticResource ToColor}}" />
              <SolidColorBrush x:Key="WordOpacity" Color="#FFFFFF" Opacity="faint" />
            </ResourceDictionary>
            """);

        IReadOnlyList<DefinedResource> defined = ThemeDefinitionScanner.Scan(_root);

        string[] unread = ["BoundOpacity", "ConvertedColor", "WordOpacity"];
        Assert.All(unread, key => Assert.IsType<ResourceValue.Opaque>(Single(defined, null, key).Value));
    }

    private static DefinedResource Single(IReadOnlyList<DefinedResource> defined, string? variant, string key)
    {
        return defined.Single(d => d.Variant == variant && d.Key == key);
    }

    private void WriteFile(string relative, string content)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
