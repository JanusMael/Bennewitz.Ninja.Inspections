using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// The check of plans/00007: a key a consumer redefines that the theme's own reads never see. Each test writes a theme laid
/// out as Semi is, its theme dictionaries and its controls each included by a <c>ResourceInclude</c> of an index that includes
/// the file, and its palettes held by their class, into a temporary folder of its own with a consumer beside it, and runs
/// the audit over them.
/// </summary>
public sealed class UnseenRedefinitionTests : IDisposable
{
    private const string Namespaces = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:Fixture\"";
    private const string ButtonTheme = "<ControlTheme x:Key=\"{x:Type Button}\" TargetType=\"Button\">\n<Setter Property=\"Background\" Value=\"{DynamicResource ButtonBackground}\" />\n</ControlTheme>";
    private const string LightPalette = "<Color x:Key=\"Blue\">#0064FA</Color>\n<SolidColorBrush x:Key=\"Primary\" Color=\"{StaticResource Blue}\" />\n<Color x:Key=\"Unused\">#123456</Color>";
    private const string DarkPalette = "<Color x:Key=\"Blue\">#54A9FF</Color>\n<SolidColorBrush x:Key=\"Primary\" Color=\"{StaticResource Blue}\" />\n<Color x:Key=\"Unused\">#654321</Color>";
    private const string ButtonAlias = "<StaticResource x:Key=\"ButtonBackground\" ResourceKey=\"Primary\" />";
    private readonly string _root = Directory.CreateTempSubdirectory("unseen-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Each_read_keeps_what_holds_it_and_the_route_to_its_file_and_a_previews_is_no_read()
    {
        Theme(
            controls: ButtonTheme
                      + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\" BasedOn=\"{StaticResource {x:Type Button}}\">"
                      + "\n<Setter Property=\"Template\"><ControlTemplate><Border Background=\"{StaticResource Primary}\" /></ControlTemplate></Setter>"
                      + "\n<Style Selector=\"^:pointerover\"><Style.Animations><Animation Duration=\"{StaticResource Quick}\" /></Style.Animations></Style>"
                      + "\n</ControlTheme>"
                      + "\n<Design.PreviewWith><Button Background=\"{StaticResource Primary}\" /></Design.PreviewWith>",
            light: ButtonAlias);

        ThemeInventory inventory = ThemeInventory.Build(AuditRunner.ToSource(Config(), Config().Themes[0]));

        Assert.Equal(
            [
                "Controls/Button.axaml:3 ButtonBackground dynamic setter entry>ResourceInclude>ResourceInclude",
                "Controls/Button.axaml:5 {x:Type Button} static entry entry>ResourceInclude>ResourceInclude",
                "Controls/Button.axaml:6 Primary static template entry>ResourceInclude>ResourceInclude",
                "Controls/Button.axaml:7 Quick static style entry>ResourceInclude>ResourceInclude",
                "Themes/Light/Button.axaml:2 Primary static alias entry>theme-ResourceInclude>ResourceInclude",
                "Tokens/Light.axaml:3 Blue static brush entry>ResourceInclude>theme-class",
            ],
            inventory.Reading.Reads
                .Where(read => read.File.Contains("Light", StringComparison.Ordinal) || read.File.Contains("Controls", StringComparison.Ordinal))
                .Select(read => Path.GetRelativePath(Path.Combine(_root, "theme"), read.File).Replace('\\', '/') + ":" + read.Line + " "
                                + read.Key + " " + (read.Dynamic ? "dynamic" : "static") + " " + read.Kind)
                .Distinct()
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_redefinition_that_every_read_resolves_inside_the_theme_is_found_with_what_to_redefine_instead()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        Consumer("<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />");

        ConsumerThemeFindings findings = Run();

        Assert.Equal(2, findings.UnseenExamined);
        Assert.Empty(findings.UnseenNotDecided);
        Assert.Collection(findings.Unseen,
            light => AssertFinding(light, "Primary", ["Light"], ["App.axaml:3"], ["Themes/Light/Button.axaml:2"], ["ButtonBackground"]),
            dark => AssertFinding(dark, "Primary", ["Dark"], ["App.axaml:3"], ["Themes/Dark/Button.axaml:2"], ["ButtonBackground"]));
    }

    [Fact]
    public void A_key_read_dynamically_or_by_no_read_is_examined_and_not_found()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        Consumer("<SolidColorBrush x:Key=\"ButtonBackground\" Color=\"#FFAA0000\" />", "<Color x:Key=\"Unused\">#FF000000</Color>");

        ConsumerThemeFindings findings = Run();

        Assert.Equal(4, findings.UnseenExamined);
        Assert.Empty(findings.Unseen);
        Assert.Empty(findings.UnseenNotDecided);
    }

    [Fact]
    public void A_static_read_finds_a_redefinition_the_application_holds_in_its_variant_and_misses_one_held_only_under_another()
    {
        string controls = ButtonTheme + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<Setter Property=\"BorderBrush\" Value=\"{StaticResource DarkOnly}\" />\n</ControlTheme>";
        string dark = "<SolidColorBrush x:Key=\"DarkOnly\" Color=\"#111111\" />";
        string apply = "<Button Theme=\"{StaticResource Fancy}\" />";

        Theme(controls: controls, dark: dark);
        Consumer(["<SolidColorBrush x:Key=\"DarkOnly\" Color=\"#FFAA0000\" />"], view: apply);
        ConsumerThemeFindings everyVariant = Run();
        Assert.Equal(1, everyVariant.UnseenExamined);
        Assert.Empty(everyVariant.Unseen);

        Consumer([Slots(("Dark", "<SolidColorBrush x:Key=\"DarkOnly\" Color=\"#FFAA0000\" />"))], view: apply);
        ConsumerThemeFindings darkOnly = Run();
        UnseenRedefinition finding = Assert.Single(darkOnly.Unseen);
        AssertFinding(finding, "DarkOnly", ["Dark"], ["App.axaml:6 (Dark)"], ["Controls/Button.axaml:6"], []);
    }

    [Fact]
    public void A_definition_with_the_themes_own_value_is_no_redefinition_colour_alias_and_element_alike()
    {
        Theme(
            controls: ButtonTheme + "\n<ControlTheme x:Key=\"Rounded\" TargetType=\"Button\">\n<Setter Property=\"Tag\" Value=\"{StaticResource Radius}\" />\n</ControlTheme>",
            light: ButtonAlias + "\n<x:Double x:Key=\"Radius\">4</x:Double>",
            dark: ButtonAlias + "\n<x:Double x:Key=\"Radius\">4</x:Double>");
        Consumer(
            [
                "<Color x:Key=\"Blue\">#0064FA</Color>",
                "<SolidColorBrush x:Key=\"Primary\" Color=\"{StaticResource Blue}\" />",
                "<sys:Double xmlns:sys=\"clr-namespace:System;assembly=System.Runtime\" x:Key=\"Radius\"> 4 </sys:Double>",
            ],
            view: "<Button Theme=\"{StaticResource Rounded}\" />");

        ConsumerThemeFindings findings = Run();

        // Blue is the theme's own under Light alone; under Dark it is examined, and the palette reads it inside the theme.
        Assert.Equal(1, findings.UnseenExamined);
        UnseenRedefinition blue = Assert.Single(findings.Unseen);
        Assert.Equal(("Blue", "Dark"), (blue.Key, Assert.Single(blue.Variants)));
    }

    [Fact]
    public void What_a_control_theme_or_a_preview_holds_is_no_redefinition_and_what_a_Styles_document_or_its_style_holds_is()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        const string Redefined = "<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />";
        Consumer(["<ControlTheme x:Key=\"Mine\" TargetType=\"Button\">\n<ControlTheme.Resources>\n" + Redefined + "\n</ControlTheme.Resources>\n</ControlTheme>"],
            view: "<Design.PreviewWith>\n<Border>\n<Border.Resources>\n" + Redefined + "\n</Border.Resources>\n</Border>\n</Design.PreviewWith>");
        Assert.Equal(0, Run().UnseenExamined);

        Consumer();
        Write("app/Styles.axaml", "<Styles " + Namespaces + ">\n<Styles.Resources>\n" + Redefined + "\n</Styles.Resources>\n</Styles>");
        Assert.Equal(["Styles.axaml:3"], Run().Unseen.SelectMany(finding => finding.DefinedAt).Distinct());

        Consumer();
        Write("app/Styles.axaml", "<Styles " + Namespaces + ">\n<Style Selector=\"Button\">\n<Style.Resources>\n" + Redefined + "\n</Style.Resources>\n</Style>\n</Styles>");
        Assert.Equal(["Styles.axaml:4"], Run().Unseen.SelectMany(finding => finding.DefinedAt).Distinct());
    }

    [Fact]
    public void A_theme_file_read_as_a_consumer_and_a_merged_file_redefine_nothing_and_a_merged_files_reads_are_not_the_themes()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        Write("compat/Compat.axaml", Dictionary("<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />\n<SolidColorBrush x:Key=\"Extra\" Color=\"{StaticResource Unused}\" />"));
        Consumer(["<Color x:Key=\"Unused\">#FF000000</Color>"], view: "<Border Background=\"{StaticResource Extra}\" />");
        const string Merge = ", \"merge\": [\"compat/Compat.axaml\"]";

        Assert.Equal(0, Run(Merge, paths: "\"theme\", \"compat\"").UnseenExamined);

        ConsumerThemeFindings app = Run(Merge);
        Assert.Equal(2, app.UnseenExamined);
        Assert.Empty(app.Unseen);
    }

    [Fact]
    public void A_key_a_provider_supplies_is_redefined_where_its_colour_differs()
    {
        Theme(
            controls: ButtonTheme,
            light: "<StaticResource x:Key=\"ButtonBackground\" ResourceKey=\"Accent\" />",
            dark: "<StaticResource x:Key=\"ButtonBackground\" ResourceKey=\"Accent\" />",
            entry: "<ResourceDictionary.MergedDictionaries>\n<local:Accents />\n</ResourceDictionary.MergedDictionaries>");
        const string Provider = ", \"providers\": { \"Accents\": { \"Accent\": \"#FF0078D7\" } }";

        Consumer("<Color x:Key=\"Accent\">#FF0078D7</Color>");
        Assert.Equal(0, Run(Provider).UnseenExamined);

        Consumer("<Color x:Key=\"Accent\">#FF00AA00</Color>");
        Assert.Equal(["Light", "Dark"], Run(Provider).Unseen.SelectMany(finding => finding.Variants));
    }

    [Fact]
    public void A_consumers_implicit_control_theme_is_never_examined_and_hides_the_themes_and_one_based_on_it_under_its_own_key_applies_it()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        const string Redefined = "<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />";

        Consumer(Redefined, "<ControlTheme x:Key=\"{x:Type Button}\" TargetType=\"Button\">\n<Setter Property=\"Padding\" Value=\"4\" />\n</ControlTheme>");
        ConsumerThemeFindings hidden = Run();
        Assert.Equal(2, hidden.UnseenExamined);
        Assert.Empty(hidden.Unseen);

        Consumer(Redefined, "<ControlTheme x:Key=\"{x:Type Button}\" TargetType=\"Button\" BasedOn=\"{StaticResource {x:Type Button}}\">\n<Setter Property=\"Padding\" Value=\"4\" />\n</ControlTheme>");
        Assert.Equal(["Light", "Dark"], Run().Unseen.SelectMany(finding => finding.Variants));
    }

    [Fact]
    public void A_consumers_definition_that_hides_the_themes_entry_holding_a_read_takes_that_read_away()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        Consumer("<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />", "<SolidColorBrush x:Key=\"ButtonBackground\" Color=\"#FFAA0000\" />");

        ConsumerThemeFindings findings = Run();

        Assert.Equal(4, findings.UnseenExamined);
        Assert.Empty(findings.Unseen);
    }

    [Fact]
    public void An_entry_nothing_applies_reads_nothing_and_one_the_consumers_own_reference_applies_does_and_a_previews_reference_does_not()
    {
        Theme(
            controls: ButtonTheme + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<Setter Property=\"Background\" Value=\"{StaticResource Unused}\" />\n</ControlTheme>",
            light: ButtonAlias,
            dark: ButtonAlias);
        const string Redefined = "<Color x:Key=\"Unused\">#FF000000</Color>";

        Consumer(Redefined);
        Assert.Empty(Run().Unseen);

        Consumer([Redefined], view: "<Design.PreviewWith>\n<Button Theme=\"{StaticResource Fancy}\" />\n</Design.PreviewWith>");
        Assert.Empty(Run().Unseen);

        Consumer([Redefined], view: "<Button Theme=\"{StaticResource Fancy}\" />");
        Assert.Collection(Run().Unseen,
            light => AssertFinding(light, "Unused", ["Light", "Dark"], ["App.axaml:3"], ["Controls/Button.axaml:6"], []));
    }

    [Fact]
    public void A_themes_alias_of_its_own_key_searches_past_itself_out_to_the_application_and_sees_the_redefinition()
    {
        Theme(
            controls: ButtonTheme + "\n<StaticResource x:Key=\"Glow\" ResourceKey=\"Glow\" />"
                      + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<Setter Property=\"Background\" Value=\"{StaticResource Glow}\" />\n</ControlTheme>",
            light: ButtonAlias,
            dark: ButtonAlias);
        Consumer(["<SolidColorBrush x:Key=\"Glow\" Color=\"#FFAA0000\" />"], view: "<Button Theme=\"{StaticResource Fancy}\" />");

        ConsumerThemeFindings findings = Run();

        Assert.Equal(2, findings.UnseenExamined);
        Assert.Empty(findings.Unseen);
        Assert.Empty(findings.UnseenNotDecided);
    }

    [Fact]
    public void A_redefinitions_answer_may_differ_by_variant()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: "<SolidColorBrush x:Key=\"ButtonBackground\" Color=\"{DynamicResource Primary}\" />");
        Consumer("<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />");

        ConsumerThemeFindings findings = Run();

        Assert.Equal(2, findings.UnseenExamined);
        Assert.Equal(["Light"], Assert.Single(findings.Unseen).Variants);
    }

    [Fact]
    public void A_consumer_file_included_as_a_theme_dictionary_or_by_one_is_read_under_that_variant()
    {
        Theme(
            controls: ButtonTheme + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<Setter Property=\"BorderBrush\" Value=\"{StaticResource DarkOnly}\" />\n</ControlTheme>",
            dark: "<SolidColorBrush x:Key=\"DarkOnly\" Color=\"#111111\" />");
        Consumer(["<ResourceDictionary>\n<ResourceDictionary.ThemeDictionaries>\n<ResourceInclude x:Key=\"Dark\" Source=\"DarkColors.axaml\" />\n</ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>"],
            view: "<Button Theme=\"{StaticResource Fancy}\" />");
        Write("app/DarkColors.axaml", Dictionary(Merged("More.axaml")));
        Write("app/More.axaml", Dictionary("<SolidColorBrush x:Key=\"DarkOnly\" Color=\"#FFAA0000\" />"));

        UnseenRedefinition finding = Assert.Single(Run().Unseen);

        AssertFinding(finding, "DarkOnly", ["Dark"], ["More.axaml:2 (Dark)"], ["Controls/Button.axaml:6"], []);
    }

    [Fact]
    public void A_read_in_an_entry_the_variants_lookup_does_not_find_is_no_read_under_that_variant()
    {
        const string DefaultEntry = "<ResourceDictionary.ThemeDictionaries>\n<ResourceDictionary x:Key=\"Default\">\n"
                                    + "<StaticResource x:Key=\"ButtonBackground\" ResourceKey=\"Unused\" />\n</ResourceDictionary>\n</ResourceDictionary.ThemeDictionaries>";
        Consumer("<Color x:Key=\"Unused\">#FF000000</Color>");

        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias, entry: DefaultEntry);
        Assert.Empty(Run().Unseen);

        Theme(controls: ButtonTheme, dark: ButtonAlias, entry: DefaultEntry);
        AssertFinding(Assert.Single(Run().Unseen), "Unused", ["Light"], ["App.axaml:3"], ["Theme.axaml:12"], ["ButtonBackground"]);
    }

    [Fact]
    public void A_read_the_walk_meets_in_two_dictionaries_is_one_read()
    {
        Theme(
            controls: ButtonTheme + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<Setter Property=\"BorderBrush\" Value=\"{StaticResource Primary}\" />\n</ControlTheme>",
            light: ButtonAlias,
            dark: ButtonAlias);
        Consumer(["<Color x:Key=\"Blue\">#FF000000</Color>"], view: "<Button Theme=\"{StaticResource Fancy}\" />");

        ConsumerThemeFindings findings = Run();
        ThemeInventory inventory = findings.Theme.Inventory;

        // Fancy's setter reads Default's palette, and the button's alias Light's: the palette's one brush, in two dictionaries.
        Assert.Equal(2, UnseenCheck.ReadsThatCount(findings.Consumer.Reading, inventory, "Light").Keys.Count(read => read.Key == "Blue"));
        UnseenRedefinition light = findings.Unseen.Single(finding => finding.Variants.Contains("Light"));
        Assert.Equal(["Tokens/Light.axaml:3"], light.ReadAt);
        Assert.Equal(1, light.Reads);
    }

    [Fact]
    public void The_section_names_each_pair_its_findings_their_reads_and_what_to_redefine_instead_the_same_under_either_directory()
    {
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        const string Paths = "[\"one/app\", \"two/app\"]";

        Consumer(["<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />"], view: null, directory: "one/app");
        string first = Section(MarkdownReport.Render(AuditRunner.Run(Config(paths: Paths))));
        Directory.Delete(Path.Combine(_root, "one"), recursive: true);
        Consumer(["<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />"], view: null, directory: "two/app");
        string second = Section(MarkdownReport.Render(AuditRunner.Run(Config(paths: Paths))));

        Assert.Equal(first, second);
        Assert.Contains(
            "| Consumer | Theme | Examined | Found | Not decided |\n|---|---|---:|---:|---:|\n| App | Theme | 2 | 2 | 0 |\n\n"
            + "### App under Theme\n\n| Key | Defined at | Reads | Variants |\n|---|---|---:|---|\n"
            + "| `Primary` | `App.axaml:3` | 1 | Light |\n| `Primary` | `App.axaml:3` | 1 | Dark |\n\n"
            + "- `Primary` under Light, read at `Themes/Light/Button.axaml:2`; redefine instead `ButtonBackground`.\n"
            + "- `Primary` under Dark, read at `Themes/Dark/Button.axaml:2`; redefine instead `ButtonBackground`.\n\n",
            first);
        Assert.Contains("a consumer's below the directory it was read from", MarkdownReport.Render(AuditRunner.Run(Config(paths: Paths))));
    }

    [Fact]
    public void A_static_read_in_a_dictionary_with_a_class_of_its_own_whose_chain_lacks_the_key_is_not_decided()
    {
        Theme(
            controls: ButtonTheme,
            light: ButtonAlias + "\n<Color x:Key=\"Outside\">#101010</Color>",
            dark: ButtonAlias,
            lightPalette: "<SolidColorBrush x:Key=\"Primary\" Color=\"{StaticResource Outside}\" />");
        Consumer("<Color x:Key=\"Outside\">#FF000000</Color>");

        ConsumerThemeFindings findings = Run();

        Assert.Empty(findings.Unseen);
        UnseenRedefinition notDecided = Assert.Single(findings.UnseenNotDecided);
        Assert.Equal(("Outside", "Light", UnseenCheck.BuiltAtOnceWhy), (notDecided.Key, Assert.Single(notDecided.Variants), notDecided.Why));
    }

    [Fact]
    public void A_static_read_that_finds_the_key_first_among_a_control_themes_resources_is_not_decided()
    {
        Theme(
            controls: ButtonTheme + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<ControlTheme.Resources>\n<SolidColorBrush x:Key=\"Glyph\" Color=\"#000000\" />\n"
                      + "</ControlTheme.Resources>\n<Setter Property=\"Foreground\" Value=\"{StaticResource Glyph}\" />\n</ControlTheme>",
            light: ButtonAlias,
            dark: ButtonAlias);
        Consumer(["<SolidColorBrush x:Key=\"Glyph\" Color=\"#FFFFFFFF\" />"], view: "<Button Theme=\"{StaticResource Fancy}\" />");

        UnseenRedefinition notDecided = Assert.Single(Run().UnseenNotDecided);

        Assert.Equal(("Glyph", UnseenCheck.ScopedWhy), (notDecided.Key, notDecided.Why));
        Assert.Equal(["Light", "Dark"], notDecided.Variants);
    }

    [Fact]
    public void A_static_read_of_a_kind_no_drawing_holds_is_not_decided()
    {
        Theme(
            controls: ButtonTheme + "\n<ControlTheme x:Key=\"Fancy\" TargetType=\"Button\">\n<Style Selector=\"^:pointerover\">\n<Style.Animations>\n"
                      + "<Animation Duration=\"{StaticResource Quick}\" />\n</Style.Animations>\n</Style>\n</ControlTheme>",
            light: ButtonAlias + "\n<x:TimeSpan x:Key=\"Quick\">0:0:0.1</x:TimeSpan>",
            dark: ButtonAlias + "\n<x:TimeSpan x:Key=\"Quick\">0:0:0.1</x:TimeSpan>");
        Consumer(["<x:TimeSpan x:Key=\"Quick\">0:0:0.2</x:TimeSpan>"], view: "<Button Theme=\"{StaticResource Fancy}\" />");

        UnseenRedefinition notDecided = Assert.Single(Run().UnseenNotDecided);

        Assert.Equal(UnseenCheck.UndrawnWhy + ": style entry>ResourceInclude>ResourceInclude", notDecided.Why);
    }

    [Fact]
    public void A_redefinition_under_an_application_styles_element_or_under_a_theme_whose_walk_left_something_unresolved_is_not_decided()
    {
        const string Redefined = "<SolidColorBrush x:Key=\"Primary\" Color=\"#FF00AA00\" />";
        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias);
        Consumer();
        Write("app/App.axaml", "<Application " + Namespaces + ">\n<Application.Styles>\n<local:SomeTheme>\n<local:SomeTheme.Resources>\n" + Redefined
                               + "\n</local:SomeTheme.Resources>\n</local:SomeTheme>\n</Application.Styles>\n</Application>");
        ConsumerThemeFindings styles = Run();
        Assert.Empty(styles.Unseen);
        Assert.All(styles.UnseenNotDecided, notDecided => Assert.Equal(UnseenCheck.ApplicationStylesWhy, notDecided.Why));
        Assert.Equal(["Light", "Dark"], styles.UnseenNotDecided.SelectMany(notDecided => notDecided.Variants));

        Theme(controls: ButtonTheme, light: ButtonAlias, dark: ButtonAlias,
            entry: "<ResourceDictionary.MergedDictionaries>\n<ResourceInclude Source=\"/Missing.axaml\" />\n</ResourceDictionary.MergedDictionaries>");
        Consumer(Redefined);
        ConsumerThemeFindings unresolved = Run();
        Assert.Empty(unresolved.Unseen);
        Assert.All(unresolved.UnseenNotDecided, notDecided => Assert.Equal(UnseenCheck.UnresolvedWhy, notDecided.Why));
        Assert.Equal(2, unresolved.UnseenNotDecided.Count);
    }

    /// <summary>The report's section on redefinitions, from its heading to the next section's.</summary>
    private static string Section(string report)
    {
        int start = report.IndexOf("## Redefinitions the theme does not see", StringComparison.Ordinal);
        return report[start..report.IndexOf("## Contrast", start, StringComparison.Ordinal)];
    }

    private static void AssertFinding(UnseenRedefinition finding, string key, string[] variants, string[] definedAt, string[] reads, string[] instead)
    {
        Assert.Equal(key, finding.Key);
        Assert.Equal(variants, finding.Variants);
        Assert.Equal(definedAt, finding.DefinedAt);
        Assert.Equal(reads, finding.ReadAt);
        Assert.Equal(reads.Length, finding.Reads);
        Assert.Equal(instead, finding.RedefineInstead);
        Assert.Null(finding.Why);
    }

    /// <summary>
    /// A theme laid out as Semi is: its entry includes, by theme dictionary, <c>Themes/{Light,Dark}/_index.axaml</c>, which
    /// include each variant's <c>Button.axaml</c>; and merges <c>Controls/_index.axaml</c>, which includes
    /// <c>Controls/Button.axaml</c>, and <c>Tokens/_index.axaml</c>, which holds the palettes by their class under
    /// <c>Default</c>, <c>Light</c> and <c>Dark</c>. Every piece is written one element to a line.
    /// </summary>
    private void Theme(string controls = "", string light = "", string dark = "", string entry = "", string lightPalette = LightPalette, string darkPalette = DarkPalette)
    {
        Write("theme/Theme.axaml", Dictionary(
            "<ResourceDictionary.ThemeDictionaries>\n<ResourceInclude x:Key=\"Light\" Source=\"/Themes/Light/_index.axaml\" />\n"
            + "<ResourceInclude x:Key=\"Dark\" Source=\"/Themes/Dark/_index.axaml\" />\n</ResourceDictionary.ThemeDictionaries>\n"
            + "<ResourceDictionary.MergedDictionaries>\n<ResourceInclude Source=\"/Controls/_index.axaml\" />\n"
            + "<ResourceInclude Source=\"/Tokens/_index.axaml\" />\n</ResourceDictionary.MergedDictionaries>" + (entry.Length > 0 ? "\n" + entry : string.Empty)));
        Write("theme/Themes/Light/_index.axaml", Dictionary(Merged("/Themes/Light/Button.axaml")));
        Write("theme/Themes/Light/Button.axaml", Dictionary(light));
        Write("theme/Themes/Dark/_index.axaml", Dictionary(Merged("/Themes/Dark/Button.axaml")));
        Write("theme/Themes/Dark/Button.axaml", Dictionary(dark));
        Write("theme/Controls/_index.axaml", Dictionary(Merged("/Controls/Button.axaml")));
        Write("theme/Controls/Button.axaml", Dictionary(controls));
        Write("theme/Tokens/_index.axaml", Dictionary(
            "<ResourceDictionary.ThemeDictionaries>\n<local:LightPalette x:Key=\"Default\" />\n<local:LightPalette x:Key=\"Light\" />\n"
            + "<local:DarkPalette x:Key=\"Dark\" />\n</ResourceDictionary.ThemeDictionaries>"));
        Write("theme/Tokens/Light.axaml", "<ResourceDictionary " + Namespaces + " x:Class=\"Fixture.LightPalette\">\n" + lightPalette + "\n</ResourceDictionary>");
        Write("theme/Tokens/Dark.axaml", "<ResourceDictionary " + Namespaces + " x:Class=\"Fixture.DarkPalette\">\n" + darkPalette + "\n</ResourceDictionary>");
    }

    /// <summary>A consumer whose <c>App.axaml</c> holds <paramref name="resources"/> in the application's resources, one to a line from line 3.</summary>
    private void Consumer(params string[] resources) => Consumer(resources, view: null);

    private void Consumer(string[] resources, string? view, string directory = "app")
    {
        if (Directory.Exists(Path.Combine(_root, directory)))
        {
            Directory.Delete(Path.Combine(_root, directory), recursive: true);
        }

        Write(directory + "/App.axaml", "<Application " + Namespaces + ">\n<Application.Resources>\n" + string.Join("\n", resources) + "\n</Application.Resources>\n</Application>");
        if (view is not null)
        {
            Write(directory + "/MainView.axaml", "<UserControl " + Namespaces + ">\n" + view + "\n</UserControl>");
        }
    }

    private AuditConfig Config(string theme = "", string consumer = "", string paths = "\"app\"")
    {
        return AuditConfig.Parse(
            "{ \"themes\": [ { \"name\": \"Theme\", \"entry\": \"theme/Theme.axaml\", \"baseDirectory\": \"theme\", \"variants\": [\"Light\", \"Dark\"]" + theme + " } ],"
            + " \"consumers\": [ { \"name\": \"App\", \"paths\": [" + paths + "]" + consumer + " } ] }",
            _root);
    }

    private ConsumerThemeFindings Run(string theme = "", string consumer = "", string paths = "\"app\"")
    {
        return Assert.Single(AuditRunner.Run(Config(theme, consumer, paths)).Findings);
    }

    private static string Slots(params (string Key, string Resources)[] slots) =>
        "<ResourceDictionary>\n<ResourceDictionary.ThemeDictionaries>\n"
        + string.Concat(slots.Select(slot => "<ResourceDictionary x:Key=\"" + slot.Key + "\">\n" + slot.Resources + "\n</ResourceDictionary>\n"))
        + "</ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>";

    private static string Merged(string source) =>
        "<ResourceDictionary.MergedDictionaries>\n<ResourceInclude Source=\"" + source + "\" />\n</ResourceDictionary.MergedDictionaries>";

    private static string Dictionary(string resources) => "<ResourceDictionary " + Namespaces + ">\n" + resources + "\n</ResourceDictionary>";

    private void Write(string relative, string content)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
