using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

public sealed class ThemeInventoryTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("theme-inv-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Composes_per_variant_keys_from_the_include_graph_and_resolves_alias_chains()
    {
        // Semi-shaped: an entry with ThemeDictionaries include-variants and a shared Tokens include.
        WriteFile("Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceInclude x:Key="Light" Source="/Themes/Light/_index.axaml" />
                <ResourceInclude x:Key="Dark" Source="/Themes/Dark/_index.axaml" />
              </ResourceDictionary.ThemeDictionaries>
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="/Tokens/_index.axaml" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);
        // Shared token: a base colour every variant inherits.
        WriteFile("Tokens/_index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Color x:Key="BaseInk">#111111</Color>
            </ResourceDictionary>
            """);
        // Light: a literal plus a brush that aliases a token defined in this same variant.
        WriteFile("Themes/Light/_index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="Button.axaml" />
              </ResourceDictionary.MergedDictionaries>
              <Color x:Key="AccentColor">#2E7D32</Color>
              <SolidColorBrush x:Key="AccentBrush" Color="{DynamicResource AccentColor}" />
            </ResourceDictionary>
            """);
        WriteFile("Themes/Light/Button.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="ButtonBackground" Color="#FFFFFF" />
            </ResourceDictionary>
            """);
        WriteFile("Themes/Dark/_index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Color x:Key="AccentColor">#4CAF50</Color>
              <SolidColorBrush x:Key="AccentBrush" Color="{DynamicResource AccentColor}" />
            </ResourceDictionary>
            """);

        ThemeInventory inventory = ThemeInventory.Build(Path.Combine(_root, "Index.axaml"), _root);
        Assert.Empty(inventory.Unresolved);

        VariantInventory light = inventory.Variants.Single(v => v.DisplayName == "Light");
        VariantInventory dark = inventory.Variants.Single(v => v.DisplayName == "Dark");

        // Shared base key is present in both.
        Assert.Equal(new AuditColor(0xFF, 0x11, 0x11, 0x11), light.Resolve("BaseInk"));
        Assert.Equal(new AuditColor(0xFF, 0x11, 0x11, 0x11), dark.Resolve("BaseInk"));

        // Variant-specific literal, and the alias brush resolving to it, differ per variant.
        Assert.Equal(new AuditColor(0xFF, 0x2E, 0x7D, 0x32), light.Resolve("AccentBrush"));
        Assert.Equal(new AuditColor(0xFF, 0x4C, 0xAF, 0x50), dark.Resolve("AccentBrush"));

        // A key from a nested include on one side is present there and absent on the other.
        Assert.True(light.Defines("ButtonBackground"));
        Assert.False(dark.Defines("ButtonBackground"));
        Assert.Equal(new AuditColor(0xFF, 0xFF, 0xFF, 0xFF), light.Resolve("ButtonBackground"));
    }

    [Fact]
    public void A_high_contrast_variant_inherits_its_parents_keys()
    {
        WriteFile("Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:semi="https://irihi.tech/semi">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceInclude x:Key="Dark" Source="/Dark.axaml" />
                <ResourceInclude x:Key="{x:Static semi:SemiTheme.NightSky}" Source="/HighContrast.axaml" />
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);
        WriteFile("Dark.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Color x:Key="OnlyInDark">#222222</Color>
              <Color x:Key="Shared">#333333</Color>
            </ResourceDictionary>
            """);
        WriteFile("HighContrast.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Color x:Key="Shared">#000000</Color>
            </ResourceDictionary>
            """);

        Dictionary<string, string> inheritance = new(StringComparer.Ordinal) { ["NightSky"] = "Dark" };
        ThemeInventory inventory = ThemeInventory.Build(Path.Combine(_root, "Index.axaml"), _root, inheritance: inheritance);

        VariantInventory nightSky = inventory.Variants.Single(v => v.DisplayName == "NightSky");
        // Inherited from Dark.
        Assert.Equal(new AuditColor(0xFF, 0x22, 0x22, 0x22), nightSky.Resolve("OnlyInDark"));
        // Overridden by the high-contrast variant.
        Assert.Equal(new AuditColor(0xFF, 0x00, 0x00, 0x00), nightSky.Resolve("Shared"));
    }

    [Fact]
    public void An_undefined_or_opaque_key_resolves_to_no_colour()
    {
        WriteFile("Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                  <Thickness x:Key="Pad">4</Thickness>
                  <SolidColorBrush x:Key="Dangling" Color="{DynamicResource NoSuchKey}" />
                </ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);

        ThemeInventory inventory = ThemeInventory.Build(Path.Combine(_root, "Index.axaml"), _root);
        VariantInventory light = inventory.Variants.Single(v => v.DisplayName == "Light");

        Assert.True(light.Defines("Pad"));
        Assert.Null(light.Resolve("Pad"));           // opaque
        Assert.Null(light.Resolve("Dangling"));       // alias to a missing key
        Assert.Null(light.Resolve("NoSuchKey"));      // undefined
        Assert.False(light.Defines("NoSuchKey"));
    }

    [Fact]
    public void An_opacity_scales_the_alpha_truncated_once_as_Avalonia_draws_it()
    {
        // Headless on Avalonia 12.1.3 with Skia, white at 0.12 drew an alpha of 30 (30.6), at 0.5 of
        // 127 and #80FFFFFF at 0.7 of 89 (89.6), where rounding gave 31, 128 and 90.
        WriteFile("Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="Faint" Color="#FFFFFF" Opacity="0.12" />
              <SolidColorBrush x:Key="Half" Color="#FFFFFF" Opacity="0.5" />
              <SolidColorBrush x:Key="HalfAgain" Color="#80FFFFFF" Opacity="0.7" />
              <Color x:Key="Translucent">#1EFFFFFF</Color>
              <SolidColorBrush x:Key="OfTranslucent" Color="{StaticResource Translucent}" />
            </ResourceDictionary>
            """);

        VariantInventory only = Only();

        Assert.Equal(new AuditColor(30, 0xFF, 0xFF, 0xFF), only.Resolve("Faint"));
        Assert.Equal(new AuditColor(127, 0xFF, 0xFF, 0xFF), only.Resolve("Half"));
        Assert.Equal(new AuditColor(89, 0xFF, 0xFF, 0xFF), only.Resolve("HalfAgain"));
        Assert.Equal(new AuditColor(30, 0xFF, 0xFF, 0xFF), only.Resolve("OfTranslucent")); // the colour's own alpha
    }

    [Fact]
    public void An_opacity_a_brush_takes_from_a_resource_is_the_number_that_resource_holds()
    {
        WriteFile("Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <x:Double x:Key="Faint">0.12</x:Double>
              <Color x:Key="Ink">#FFFFFF</Color>
              <SolidColorBrush x:Key="Static" Color="#FFFFFF" Opacity="{StaticResource Faint}" />
              <SolidColorBrush x:Key="Dynamic" Color="{DynamicResource Ink}" Opacity="{DynamicResource Faint}" />
              <SolidColorBrush x:Key="Element">
                <SolidColorBrush.Color>#FFFFFF</SolidColorBrush.Color>
                <SolidColorBrush.Opacity><StaticResource ResourceKey="Faint" /></SolidColorBrush.Opacity>
              </SolidColorBrush>
              <SolidColorBrush x:Key="Undefined" Color="#FFFFFF" Opacity="{StaticResource NoSuchKey}" />
              <SolidColorBrush x:Key="NotANumber" Color="#FFFFFF" Opacity="{StaticResource Ink}" />
              <x:Int32 x:Key="Whole">1</x:Int32>
              <SolidColorBrush x:Key="NotADouble" Color="#FFFFFF" Opacity="{StaticResource Whole}" />
            </ResourceDictionary>
            """);

        VariantInventory only = Only();

        Assert.Equal(new AuditColor(30, 0xFF, 0xFF, 0xFF), only.Resolve("Static"));
        Assert.Equal(new AuditColor(30, 0xFF, 0xFF, 0xFF), only.Resolve("Dynamic"));
        Assert.Equal(new AuditColor(30, 0xFF, 0xFF, 0xFF), only.Resolve("Element"));
        Assert.Null(only.Resolve("Undefined"));
        Assert.Null(only.Resolve("NotANumber"));
        Assert.Null(only.Resolve("NotADouble")); // only an x:Double is read as an opacity
    }

    [Fact]
    public void A_brush_whose_Color_names_a_brush_has_no_colour_since_it_does_not_load()
    {
        // Avalonia 12.1.3 throws an InvalidCastException loading such a brush, whether it names the
        // brush directly or through an alias.
        WriteFile("Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Color x:Key="Ink">#FFFFFF</Color>
              <StaticResource x:Key="InkAlias" ResourceKey="Ink" />
              <SolidColorBrush x:Key="InkBrush" Color="#FFFFFF" Opacity="0.5" />
              <SolidColorBrush x:Key="NamedInkBrush" Color="{StaticResource Ink}" />
              <StaticResource x:Key="InkBrushAlias" ResourceKey="InkBrush" />
              <SolidColorBrush x:Key="OfBrush" Color="{StaticResource InkBrush}" Opacity="0.5" />
              <SolidColorBrush x:Key="OfDynamicBrush" Color="{DynamicResource InkBrush}" />
              <SolidColorBrush x:Key="OfNamingBrush" Color="{StaticResource NamedInkBrush}" />
              <SolidColorBrush x:Key="OfBrushAlias" Color="{StaticResource InkBrushAlias}" />
              <SolidColorBrush x:Key="OfBrushElement">
                <SolidColorBrush.Color><StaticResource ResourceKey="InkBrush" /></SolidColorBrush.Color>
              </SolidColorBrush>
              <SolidColorBrush x:Key="OfColorAlias" Color="{StaticResource InkAlias}" />
            </ResourceDictionary>
            """);

        VariantInventory only = Only();

        string[] refused = ["OfBrush", "OfDynamicBrush", "OfNamingBrush", "OfBrushAlias", "OfBrushElement"];
        Assert.All(refused, key => Assert.Null(only.Resolve(key)));

        // A colour reached through an alias is still a colour, and an alias of a brush is that brush.
        Assert.Equal(new AuditColor(0xFF, 0xFF, 0xFF, 0xFF), only.Resolve("OfColorAlias"));
        Assert.Equal(new AuditColor(0xFF, 0xFF, 0xFF, 0xFF), only.Resolve("NamedInkBrush"));
        Assert.Equal(new AuditColor(127, 0xFF, 0xFF, 0xFF), only.Resolve("InkBrushAlias"));
    }

    private VariantInventory Only()
    {
        return Assert.Single(ThemeInventory.Build(Path.Combine(_root, "Index.axaml"), _root).Variants);
    }

    private void WriteFile(string relative, string content)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
