using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// Where the audit resolves an alias, by plans/00006: a <c>StaticResource</c> in its own dictionaries first, out to the
/// root it was loaded in, in the variant of the theme dictionary around it, then in the application, whose resources, a
/// consumer's tokens and the merged files, come before its styles, the theme; a <c>DynamicResource</c> in the application,
/// under the variant asked. <c>DrawnColourTests</c>, in the runtime check's test project, holds each rule to what Avalonia
/// draws.
/// </summary>
public sealed class AliasScopeTests : IDisposable
{
    private const string Namespaces = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:Fixture\"";
    private static readonly AuditColor Dim = new(0xFF, 0x40, 0x40, 0x40);
    private static readonly AuditColor Pale = new(0xFF, 0xB0, 0xB0, 0xB0);
    private readonly string _root = Directory.CreateTempSubdirectory("alias-scope-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void An_alias_reads_its_own_dictionaries_before_a_merged_file_that_redefines_its_target()
    {
        ThemeInventory theme = Build(Colour("Target", "#404040") + Alias("B", "Target"), Colour("Target", "#B0B0B0"));

        Assert.Equal(Dim, theme.ForVariant("Light").Resolve("B"));
        Assert.Equal(Pale, theme.ForVariant("Light").Resolve("Target"));
    }

    [Fact]
    public void Past_its_own_dictionaries_an_alias_reads_the_merged_files_the_last_first_then_the_theme()
    {
        ThemeInventory theme = Build(Colour("Target", "#404040") + Alias("C", "Elsewhere"),
            Colour("Target", "#101010"), Colour("Target", "#B0B0B0"), Alias("B", "Target"), Colour("Elsewhere", "#B0B0B0"));

        Assert.Equal(Pale, theme.ForVariant("Light").Resolve("B"));
        Assert.Equal(Pale, theme.ForVariant("Light").Resolve("C"));
    }

    [Fact]
    public void A_consumer_token_aliasing_a_theme_key_resolves_to_the_themes_colour_and_one_a_merged_file_redefines_to_its()
    {
        ThemeInventory theme = Build(Colour("Target", "#404040") + Colour("Merged", "#404040") + Colour("Shared", "#404040"),
            Colour("Merged", "#B0B0B0") + Colour("Shared", "#404040"));
        string file = Write("Tokens.axaml", Dictionary(Alias("Token", "Target") + Alias("Other", "Merged") + Colour("Shared", "#B0B0B0")));
        ThemeInventory tokens = ThemeInventory.Build(new ThemeSource(file, _root));

        IReadOnlyList<ContrastFinding> findings = ContrastAudit.Score(
            [new ContrastPair("Token", "Target", 1), new ContrastPair("Other", "Target", 1), new ContrastPair("Shared", "Target", 1)],
            theme, ["Light"], tokens);

        Assert.Equal([Dim, Pale, Pale], findings.Select(finding => finding.Foreground));
    }

    [Fact]
    public void An_alias_outside_every_theme_dictionary_reads_Default_alone_whatever_the_variant_asked()
    {
        ThemeInventory theme = Build(
            Slots(("Default", Colour("Target", "#404040")), ("Dark", Colour("Target", "#B0B0B0") + Colour("OnlyDark", "#B0B0B0")))
            + Alias("B", "Target") + Alias("C", "OnlyDark"));

        Assert.Equal(Dim, theme.ForVariant("Dark").Resolve("B"));
        Assert.Null(theme.ForVariant("Dark").Resolve("C"));
        Assert.True(theme.ForVariant("Dark").Defines("C"));
    }

    [Fact]
    public void An_alias_in_a_theme_dictionary_reads_that_variant_and_Default_under_a_variant_that_inherits_it()
    {
        ThemeInventory theme = Build(
            Slots(
                ("Default", Colour("Target", "#404040")),
                ("Dark", Colour("Target", "#B0B0B0") + Alias("B", "Target") + Alias("C", "OnlyContrast")),
                ("Contrast", Colour("OnlyContrast", "#FFFF00"))),
            inheritance: new Dictionary<string, string> { ["Contrast"] = "Dark" });

        Assert.Equal(Pale, theme.ForVariant("Contrast").Resolve("B"));
        Assert.Null(theme.ForVariant("Contrast").Resolve("C"));
        Assert.Null(theme.ForVariant("Dark").Resolve("C"));
    }

    [Fact]
    public void A_dictionarys_own_entry_comes_before_its_theme_dictionaries()
    {
        ThemeInventory theme = Build(Colour("Target", "#404040") + Slots(("Dark", Colour("Target", "#B0B0B0"))));

        Assert.Equal(Dim, theme.ForVariant("Dark").Resolve("Target"));
    }

    [Fact]
    public void A_dictionary_a_theme_holds_by_its_class_is_a_root_of_its_own_and_reads_no_application()
    {
        Write("Palette.axaml",
            "<ResourceDictionary " + Namespaces + " x:Class=\"Fixture.Palette\">"
            + Colour("Inside", "#B0B0B0") + Alias("A", "Inside") + Alias("B", "Outside") + "</ResourceDictionary>");
        ThemeInventory theme = Build(
            "<ResourceDictionary.MergedDictionaries><local:Palette /></ResourceDictionary.MergedDictionaries>" + Colour("Outside", "#404040"),
            Colour("Outside", "#B0B0B0"));

        Assert.Empty(theme.Unresolved);
        Assert.Equal(Pale, theme.ForVariant("Light").Resolve("A"));
        Assert.Null(theme.ForVariant("Light").Resolve("B"));
        Assert.True(theme.ForVariant("Light").Defines("B"));
    }

    [Fact]
    public void An_alias_of_its_own_key_reads_past_its_own_entry_and_a_cycle_with_nothing_past_it_has_no_colour()
    {
        ThemeInventory reexport = Build(
            "<ResourceDictionary.MergedDictionaries><ResourceDictionary>" + Colour("B", "#404040")
            + "</ResourceDictionary></ResourceDictionary.MergedDictionaries>" + Alias("B", "B"));
        Assert.Equal(Dim, reexport.ForVariant("Light").Resolve("B"));

        ThemeInventory cycle = Build(Alias("A", "C") + Alias("C", "A"));
        Assert.Null(cycle.ForVariant("Light").Resolve("A"));
    }

    [Fact]
    public void A_dynamic_reference_reads_the_application_under_the_variant_asked()
    {
        ThemeInventory theme = Build(
            Slots(("Default", Colour("Target", "#404040")), ("Dark", Colour("Target", "#B0B0B0")))
            + "<SolidColorBrush x:Key=\"B\" Color=\"{DynamicResource Target}\" />"
            + Colour("Redefined", "#404040") + "<SolidColorBrush x:Key=\"C\" Color=\"{DynamicResource Redefined}\" />",
            Colour("Redefined", "#B0B0B0"));

        Assert.Equal(Pale, theme.ForVariant("Dark").Resolve("B"));
        Assert.Equal(Dim, theme.ForVariant("Light").Resolve("B"));
        Assert.Equal(Pale, theme.ForVariant("Light").Resolve("C"));
    }

    [Fact]
    public void A_brushs_opacity_resource_is_found_where_its_own_reference_finds_it()
    {
        ThemeInventory theme = Build(
            "<x:Double x:Key=\"Fade\">0.5</x:Double>"
            + "<SolidColorBrush x:Key=\"Static\" Color=\"#FFFFFF\" Opacity=\"{StaticResource Fade}\" />"
            + "<SolidColorBrush x:Key=\"Dynamic\" Color=\"#FFFFFF\" Opacity=\"{DynamicResource Fade}\" />",
            "<x:Double x:Key=\"Fade\">0.25</x:Double>");

        Assert.Equal(new AuditColor(127, 0xFF, 0xFF, 0xFF), theme.ForVariant("Light").Resolve("Static"));
        Assert.Equal(new AuditColor(63, 0xFF, 0xFF, 0xFF), theme.ForVariant("Light").Resolve("Dynamic"));
    }

    private static string Colour(string key, string colour) => "<Color x:Key=\"" + key + "\">" + colour + "</Color>";

    private static string Alias(string key, string target) => "<StaticResource x:Key=\"" + key + "\" ResourceKey=\"" + target + "\" />";

    private static string Slots(params (string Key, string Resources)[] slots) =>
        "<ResourceDictionary.ThemeDictionaries>"
        + string.Concat(slots.Select(slot => "<ResourceDictionary x:Key=\"" + slot.Key + "\">" + slot.Resources + "</ResourceDictionary>"))
        + "</ResourceDictionary.ThemeDictionaries>";

    private static string Dictionary(string resources) => "<ResourceDictionary " + Namespaces + ">" + resources + "</ResourceDictionary>";

    /// <summary>A theme whose entry holds <paramref name="theme"/>, with each of <paramref name="merges"/> merged after it.</summary>
    private ThemeInventory Build(string theme, params string[] merges) => Build(theme, null, merges);

    private ThemeInventory Build(string theme, IReadOnlyDictionary<string, string>? inheritance, params string[] merges)
    {
        string entry = Write("Theme.axaml", "<Styles " + Namespaces + "><Styles.Resources><ResourceDictionary>" + theme
                                            + "</ResourceDictionary></Styles.Resources></Styles>");
        List<string> merged = [.. merges.Select((merge, i) => Write("Merge" + i + ".axaml", Dictionary(merge)))];
        return ThemeInventory.Build(new ThemeSource(entry, _root) { Merge = merged, Inheritance = inheritance });
    }

    private string Write(string relative, string content)
    {
        string path = Path.Combine(_root, relative);
        File.WriteAllText(path, content);
        return path;
    }
}
