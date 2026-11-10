namespace XamlQuality.ThemeRuntime.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;
using Xunit;
using ThemeVariant = Avalonia.Styling.ThemeVariant;

/// <summary>
/// <para>
/// The verdicts of plans/00007's check held to what Avalonia draws, by its decision 8. Each case audits the theme compiled
/// under <c>Fixtures/Unseen</c> with a consumer that redefines one key and references what applies the read of it, then
/// draws that reference where an application holds the theme in its styles, after Simple, and the consumer's definitions
/// in its resources. A redefinition the check finds must leave the theme's colour drawn, one it does not find must draw
/// the consumer's, and one it does not decide is drawn as it was measured.
/// </para>
/// <para>
/// Each case is drawn with the consumer's resources held before the theme is built and after, since an application may
/// hold the two in either order, and each kind of read carries its kind as the trait <c>kind</c>, as
/// <c>ThemeRead.Kind</c> writes it, so a local run can choose kinds by the runner's trait filter.
/// </para>
/// </summary>
public sealed class UnseenRedefinitionDrawnTests : IDisposable
{
    private const string Namespaces = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:XamlQuality.ThemeRuntime.Tests\"";
    private const string Themes = "#404040";
    private const string Consumers = "#B0B0B0";
    private static readonly Uri Fixtures = new("avares://XamlQuality.ThemeRuntime.Tests/Fixtures/Unseen/");

    private readonly string _root = Directory.CreateTempSubdirectory("unseen-drawn-").FullName;

    /// <summary>What the check says of a redefinition under one variant.</summary>
    public enum Verdict
    {
        /// <summary>A finding: no read of the theme's sees it.</summary>
        Found,

        /// <summary>Examined, or not, and not a finding: something the theme draws sees it, or nothing reads it.</summary>
        Seen,

        /// <summary>Not decided.</summary>
        NotDecided,
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [AvaloniaFact]
    [Trait("kind", "alias entry>MergeResourceInclude>theme-inline")]
    public void An_alias_in_an_inline_theme_dictionary_of_a_merged_file()
    {
        AssertDrawnAsChecked(Inside("B1 aliases K1", "K1", BrushOf("K1"), Painted("B1"), "Light"));
    }

    [AvaloniaFact]
    [Trait("kind", "alias entry>ResourceInclude>ResourceInclude")]
    public void An_alias_in_a_file_two_resource_includes_reach()
    {
        AssertDrawnAsChecked(
            Inside("B2 aliases K2", "K2", BrushOf("K2"), Painted("B2"), "Light"),
            Reaching("B2A aliases K2A, which only Dark defines", "K2A", BrushOf("K2A"), Painted("B2A")));
    }

    [AvaloniaFact]
    [Trait("kind", "alias entry>theme-ResourceInclude>ResourceInclude")]
    public void An_alias_in_a_file_merged_into_a_theme_dictionarys_include()
    {
        AssertDrawnAsChecked(Inside("B3 aliases K3", "K3", BrushOf("K3"), Painted("B3"), "Dark"));
    }

    [AvaloniaFact]
    [Trait("kind", "brush entry>MergeResourceInclude")]
    public void A_brush_in_a_merged_file()
    {
        AssertDrawnAsChecked(
            Inside("B4's Color reads K4", "K4", ColourOf("K4"), Painted("B4"), "Light"),
            Reaching("B4A's Color reads K4A, which only Dark defines", "K4A", ColourOf("K4A"), Painted("B4A")));
    }

    [AvaloniaFact]
    [Trait("kind", "brush entry>MergeResourceInclude>theme-inline")]
    public void A_brush_in_an_inline_theme_dictionary_of_a_merged_file()
    {
        AssertDrawnAsChecked(Inside("B5's Color reads K5", "K5", ColourOf("K5"), Painted("B5"), "Light"));
    }

    [AvaloniaFact]
    [Trait("kind", "brush entry>ResourceInclude>theme-ResourceInclude")]
    public void A_brush_in_a_theme_dictionarys_include_in_an_included_file()
    {
        AssertDrawnAsChecked(Inside("B6's Color reads K6", "K6", ColourOf("K6"), Painted("B6"), "Dark"));
    }

    [AvaloniaFact]
    [Trait("kind", "brush entry>ResourceInclude>theme-class")]
    public void A_brush_in_a_palette_held_by_its_class_in_a_theme_dictionary()
    {
        AssertDrawnAsChecked(Inside("B7's Color reads K7, in its own palette", "K7", ColourOf("K7"), Painted("B7"), "Light"));
    }

    [AvaloniaFact]
    [Trait("kind", "brush entry>theme-ResourceInclude>ResourceInclude")]
    public void A_brush_in_a_file_merged_into_a_theme_dictionarys_include()
    {
        AssertDrawnAsChecked(Inside("B8's Color reads K8", "K8", ColourOf("K8"), Painted("B8"), "Dark"));
    }

    [AvaloniaFact]
    [Trait("kind", "entry entry>ResourceInclude>ResourceInclude")]
    public void A_control_themes_BasedOn_in_a_file_two_resource_includes_reach()
    {
        AssertDrawnAsChecked(
            Inside("T9 is based on K9", "K9", ThemeOf("K9"), Themed("T9"), "Light"),
            Reaching("T9A is based on K9A, which only Dark defines", "K9A", ThemeOf("K9A"), Themed("T9A")));
    }

    [AvaloniaFact]
    [Trait("kind", "entry entry>StyleInclude>MergeResourceInclude")]
    public void A_control_themes_BasedOn_in_a_file_a_style_includes_merge_reaches()
    {
        AssertDrawnAsChecked(
            Inside("T10 is based on K10", "K10", ThemeOf("K10"), Themed("T10"), "Light"),
            Reaching("T10A is based on K10A, which only Dark defines", "K10A", ThemeOf("K10A"), Themed("T10A")));
    }

    [AvaloniaFact]
    [Trait("kind", "entry entry>theme-ResourceInclude>ResourceInclude")]
    public void A_control_themes_BasedOn_in_a_file_merged_into_a_theme_dictionarys_include()
    {
        AssertDrawnAsChecked(
            Inside("T11 is based on K11, and the consumer applies T11 dynamically", "K11", ThemeOf("K11"), "<Border Theme=\"{DynamicResource T11}\" />", "Dark"),
            new Case("T11 is based on K11, and the consumer's static reference, outside every theme dictionary, finds no T11 under Default",
                "K11", ThemeOf("K11"), Themed("T11"), "Dark", Verdict.Seen, Drawing: "#000000"));
    }

    [AvaloniaFact]
    [Trait("kind", "setter entry>ResourceInclude>ResourceInclude")]
    public void A_setter_in_a_file_two_resource_includes_reach()
    {
        AssertDrawnAsChecked(
            Inside("T12's setter reads K12, and only the consumer's reference applies T12", "K12", BrushOf("K12"), Themed("T12"), "Light"),
            Reaching("T12A's setter reads K12A, which only Dark defines", "K12A", BrushOf("K12A"), Themed("T12A")),
            new Case("T12A's setter reads K12A, which the consumer holds only under Dark, and searches the application in Default, finding nothing",
                "K12A", Slot("Dark", BrushOf("K12A")), Themed("T12A"), "Dark", Verdict.Found, Drawing: "threw KeyNotFoundException: Static resource 'K12A' not found."));
    }

    [AvaloniaFact]
    [Trait("kind", "setter entry>StyleInclude>MergeResourceInclude")]
    public void A_setter_in_a_file_a_style_includes_merge_reaches()
    {
        AssertDrawnAsChecked(
            Inside("T13's setter reads K13", "K13", BrushOf("K13"), Themed("T13"), "Light"),
            Reaching("T13A's setter reads K13A, which only Dark defines", "K13A", BrushOf("K13A"), Themed("T13A")));
    }

    [AvaloniaFact]
    [Trait("kind", "template entry>ResourceInclude>ResourceInclude")]
    public void A_template_in_a_file_two_resource_includes_reach()
    {
        AssertDrawnAsChecked(
            Inside("T14's template reads K14", "K14", BrushOf("K14"), Templated("T14"), "Light"),
            Reaching("T14A's template reads K14A, which only Dark defines", "K14A", BrushOf("K14A"), Templated("T14A")));
    }

    [AvaloniaFact]
    [Trait("kind", "template entry>StyleInclude>MergeResourceInclude")]
    public void A_template_in_a_file_a_style_includes_merge_reaches()
    {
        AssertDrawnAsChecked(
            Inside("T15's template reads K15", "K15", BrushOf("K15"), Templated("T15"), "Light"),
            Reaching("T15A's template reads K15A, which only Dark defines", "K15A", BrushOf("K15A"), Templated("T15A")));
    }

    [AvaloniaFact]
    public void A_finding_leaves_the_themes_colour_and_the_key_it_names_to_redefine_instead_draws_the_consumers()
    {
        AssertDrawnAsChecked(
            Inside("Token aliases Base, and a style reads Token dynamically", "Base", BrushOf("Base"), "<Border Classes=\"token\" />", "Light"),
            new Case("the style reads Token dynamically", "Token", BrushOf("Token"), "<Border Classes=\"token\" />", "Light", Verdict.Seen));
        Assert.Equal(["Token"], Check(Inside("Base", "Base", BrushOf("Base"), "<Border Classes=\"token\" />", "Light")).Instead);
    }

    [AvaloniaFact]
    public void A_dynamic_read_in_a_dictionary_draws_the_consumers_colour()
    {
        AssertDrawnAsChecked(new Case("DynBrush's Color reads DynColour dynamically", "DynColour", ColourOf("DynColour"), Painted("DynBrush"), "Light", Verdict.Seen));
    }

    [AvaloniaFact]
    public void An_alias_of_its_own_key_draws_the_themes_colour_inside_the_theme_and_the_consumers_past_it()
    {
        AssertDrawnAsChecked(
            Inside("Self passes itself to the theme's merged Self", "Self", BrushOf("Self"), Painted("SelfUser"), "Light"),
            new Case("SelfOut passes itself out to the application", "SelfOut", BrushOf("SelfOut"), Painted("SelfOutUser"), "Light", Verdict.Seen));
    }

    [AvaloniaFact]
    public void A_consumers_implicit_theme_hides_the_themes_and_one_based_on_it_under_its_own_key_applies_it()
    {
        const string Probe = "<local:UnseenProbe />";
        string own = "<ControlTheme x:Key=\"{x:Type local:UnseenProbe}\" TargetType=\"local:UnseenProbe\"><Setter Property=\"Background\" Value=\"{DynamicResource Hidden}\" />"
                     + "<Setter Property=\"Template\"><ControlTemplate><Border Background=\"{TemplateBinding Background}\" /></ControlTemplate></Setter></ControlTheme>";
        string based = "<ControlTheme x:Key=\"{x:Type local:UnseenProbe}\" TargetType=\"local:UnseenProbe\" BasedOn=\"{StaticResource {x:Type local:UnseenProbe}}\" />";
        AssertDrawnAsChecked(
            new Case("the consumer's implicit theme hides the theme's, the one read of Hidden", "Hidden", BrushOf("Hidden") + own, Probe, "Light", Verdict.Seen),
            new Case("the consumer's implicit theme is based on the theme's", "Hidden", BrushOf("Hidden") + based, Probe, "Light", Verdict.Found, BothOrders: false));
    }

    [AvaloniaFact]
    [Trait("kind", "brush entry>ResourceInclude>theme-class")]
    public void A_read_in_a_palette_held_by_its_class_whose_chain_lacks_the_key_is_not_decided()
    {
        AssertDrawnAsChecked(new Case("BC's Color reads OutsideC, outside its palette", "OutsideC", ColourOf("OutsideC"), Painted("BC"), "Light", Verdict.NotDecided,
            Before: Consumers, After: "threw KeyNotFoundException: Static resource 'OutsideC' not found.", Entry: "ClassTheme.axaml"));
    }

    /// <summary>
    /// The port's case on Semi itself: its <c>SolidButton</c> paints <c>ButtonSolidPrimaryBackground</c>, which aliases
    /// <c>SemiColorPrimary</c> in its Dark theme dictionary. Redefining the palette key leaves Semi's dark blue,
    /// <c>SemiBlue5Color</c>, <c>#54A9FF</c> at <c>Tokens/Palette/Dark.axaml:63</c> in Semi's source at 12.1.0.1; redefining
    /// the token draws the consumer's colour. The check's verdicts on Semi's source are step 2's, which no test reads.
    /// </summary>
    [AvaloniaFact]
    public void Semis_SolidButton_keeps_its_dark_blue_where_its_palette_key_is_redefined_and_takes_the_consumers_colour_where_its_token_is()
    {
        const string Button = "<Button Theme=\"{StaticResource SolidButton}\" Width=\"40\" Height=\"40\" />";
        string palette = Drawn(() => [new Semi.Avalonia.SemiTheme()], ColourOf("SemiColorPrimary"), Button, "Dark", consumerFirst: false);
        string token = Drawn(() => [new Semi.Avalonia.SemiTheme()], ColourOf("ButtonSolidPrimaryBackground"), Button, "Dark", consumerFirst: false);

        Assert.Equal(("#54A9FF", Consumers), (palette, token));
    }

    private static Case Inside(string what, string key, string consumer, string view, string variant) => new(what, key, consumer, view, variant, Verdict.Found);

    private static Case Reaching(string what, string key, string consumer, string view) => new(what, key, consumer, view, "Dark", Verdict.Seen);

    private static string BrushOf(string key) => "<SolidColorBrush x:Key=\"" + key + "\" Color=\"" + Consumers + "\" />";

    private static string ColourOf(string key) => "<Color x:Key=\"" + key + "\">" + Consumers + "</Color>";

    private static string ThemeOf(string key) =>
        "<ControlTheme x:Key=\"" + key + "\" TargetType=\"Border\"><Setter Property=\"Background\" Value=\"" + Consumers + "\" /></ControlTheme>";

    private static string Slot(string variant, string resources) =>
        "<ResourceDictionary.ThemeDictionaries><ResourceDictionary x:Key=\"" + variant + "\">" + resources + "</ResourceDictionary></ResourceDictionary.ThemeDictionaries>";

    private static string Painted(string key) => "<Border Background=\"{DynamicResource " + key + "}\" />";

    private static string Themed(string key) => "<Border Theme=\"{StaticResource " + key + "}\" />";

    private static string Templated(string key) => "<ContentControl Theme=\"{StaticResource " + key + "}\" />";

    /// <summary>
    /// Each case's verdict must be the one it expects, and its drawing must agree: the theme's colour for a finding, the
    /// consumer's where the check finds none, and, for one not decided, the colours measured, with the consumer's resources
    /// held before the theme is built and after.
    /// </summary>
    private void AssertDrawnAsChecked(params Case[] cases)
    {
        List<string> wrong = [];
        foreach (Case @case in cases)
        {
            Checked check = Check(@case);
            string after = Drawn(@case, consumerFirst: false);
            string? before = @case.BothOrders ? Drawn(@case, consumerFirst: true) : null;
            string measured = @case.What + ": the check says " + check.Verdict + (check.Why is { } why ? " (" + why + ")" : string.Empty)
                              + "; drawn " + (before is null ? string.Empty : before + " with the consumer's resources held first, ") + after + " with them held after";

            if (check.Verdict != @case.Expected)
            {
                wrong.Add(measured + ", where the case expects " + @case.Expected);
                continue;
            }

            string[] wanted = @case.Expected switch
            {
                _ when @case.Drawing is { } drawing => [drawing],
                Verdict.Found => [Themes],
                Verdict.Seen => [Consumers],
                _ => [@case.Before ?? "?", @case.After ?? "?"],
            };

            bool agrees = @case.Expected == Verdict.NotDecided && @case.Drawing is null
                ? before == wanted[0] && after == wanted[1]
                : after == wanted[0] && (before is null || before == wanted[0]);
            if (!agrees)
            {
                wrong.Add(measured);
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>What the check says of the case's key under its variant, the consumer written to a folder of its own.</summary>
    private Checked Check(Case @case)
    {
        string directory = Directory.CreateDirectory(Path.Combine(_root, Guid.NewGuid().ToString("N"))).FullName;
        string app = Directory.CreateDirectory(Path.Combine(directory, "app")).FullName;
        File.WriteAllText(Path.Combine(app, "App.axaml"),
            "<Application " + Namespaces + "><Application.Resources><ResourceDictionary>" + @case.Consumer + "</ResourceDictionary></Application.Resources></Application>");
        File.WriteAllText(Path.Combine(app, "MainView.axaml"), "<UserControl " + Namespaces + ">" + @case.View + "</UserControl>");

        string project = ProjectDirectory().Replace('\\', '/');
        AuditConfig config = AuditConfig.Parse(
            "{ \"themes\": [ { \"name\": \"Unseen\", \"entry\": \"" + project + "/Fixtures/Unseen/" + @case.Entry + "\", \"baseDirectory\": \"" + project
            + "\", \"assembly\": \"XamlQuality.ThemeRuntime.Tests\", \"variants\": [\"Light\", \"Dark\"] } ], \"consumers\": [ { \"name\": \"App\", \"paths\": [\"app\"] } ] }",
            directory);
        ConsumerThemeFindings findings = Assert.Single(AuditRunner.Run(config).Findings);

        bool Matches(UnseenRedefinition redefinition) => redefinition.Key == @case.Key && redefinition.Variants.Contains(@case.Variant);
        if (findings.Unseen.FirstOrDefault(Matches) is { } found)
        {
            return new Checked(Verdict.Found, null, found.RedefineInstead);
        }

        return findings.UnseenNotDecided.FirstOrDefault(Matches) is { } open
            ? new Checked(Verdict.NotDecided, open.Why, [])
            : new Checked(Verdict.Seen, null, []);
    }

    /// <summary>
    /// The colour the case's view is drawn in over black, or what Avalonia threw: the theme in the application's styles after
    /// Simple, the consumer's resources in its resources, held before the theme is built or after.
    /// </summary>
    private static string Drawn(Case @case, bool consumerFirst) =>
        Drawn(() => [new SimpleTheme(), (IStyle)AvaloniaXamlLoader.Load(new Uri(Fixtures, @case.Entry))], @case.Consumer, @case.View, @case.Variant, consumerFirst);

    /// <summary>
    /// The colour <paramref name="view"/> is drawn in over black under <paramref name="variant"/>, or what Avalonia threw:
    /// <paramref name="themes"/> in the application's styles, <paramref name="consumer"/> in its resources, held before the
    /// themes are built or after. The application's own styles and resources are put back afterwards.
    /// </summary>
    private static string Drawn(Func<IStyle[]> themes, string consumer, string view, string variant, bool consumerFirst)
    {
        Application application = Application.Current!;
        List<IStyle> styles = [.. application.Styles];
        IResourceDictionary stylesResources = application.Styles.Resources;
        IResourceDictionary resources = application.Resources;
        ThemeVariant? requested = application.RequestedThemeVariant;
        try
        {
            application.Styles.Clear();
            application.Styles.Resources = new ResourceDictionary();
            application.Resources = new ResourceDictionary();
            application.RequestedThemeVariant = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
            if (consumerFirst)
            {
                application.Resources = Consumer(consumer);
            }

            foreach (IStyle theme in themes())
            {
                application.Styles.Add(theme);
            }

            if (!consumerFirst)
            {
                application.Resources = Consumer(consumer);
            }

            string markup = "<Border " + Namespaces + " Background=\"#000000\">" + view + "</Border>";
            AuditColor drawn = DrawnColourTests.Paint((Control)AvaloniaRuntimeXamlLoader.Load(markup, typeof(UnseenRedefinitionDrawnTests).Assembly), variant: null);
            return "#" + drawn.R.ToString("X2") + drawn.G.ToString("X2") + drawn.B.ToString("X2");
        }
        catch (Exception thrown)
        {
            Exception cause = thrown.GetBaseException();
            return "threw " + cause.GetType().Name + ": " + cause.Message;
        }
        finally
        {
            application.RequestedThemeVariant = requested;
            application.Styles.Clear();
            foreach (IStyle style in styles)
            {
                application.Styles.Add(style);
            }

            application.Styles.Resources = stylesResources;
            application.Resources = resources;
        }
    }

    private static ResourceDictionary Consumer(string resources) =>
        (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("<ResourceDictionary " + Namespaces + ">" + resources + "</ResourceDictionary>", typeof(UnseenRedefinitionDrawnTests).Assembly);

    private static string ProjectDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "XamlQuality.ThemeRuntime.Tests.csproj")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    /// <summary>One redefinition, checked and drawn.</summary>
    /// <param name="What">The case, as a failure names it.</param>
    /// <param name="Key">The key the consumer redefines.</param>
    /// <param name="Consumer">The consumer's resources, its redefinition among them.</param>
    /// <param name="View">The consumer's view, which references what applies the read, drawn over black.</param>
    /// <param name="Variant">The variant it is checked and drawn under, <c>Light</c> or <c>Dark</c>.</param>
    /// <param name="Expected">The verdict the case expects.</param>
    /// <param name="BothOrders">Whether it is drawn with the consumer's resources held before the theme is built as well as after.</param>
    /// <param name="Before">For a case not decided, the colour measured with the consumer's resources held first.</param>
    /// <param name="After">For a case not decided, the colour measured with them held after.</param>
    /// <param name="Entry">The theme's entry file under <c>Fixtures/Unseen</c>.</param>
    /// <param name="Drawing">
    /// What is drawn in either order where the verdict does not say it by a colour, as where the theme's read never runs
    /// or, by plans/00006's model too, finds nothing.
    /// </param>
    private sealed record Case(
        string What,
        string Key,
        string Consumer,
        string View,
        string Variant,
        Verdict Expected,
        bool BothOrders = true,
        string? Before = null,
        string? After = null,
        string Entry = "Theme.axaml",
        string? Drawing = null);

    private sealed record Checked(Verdict Verdict, string? Why, IReadOnlyList<string> Instead);
}
