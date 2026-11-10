namespace XamlQuality.ThemeRuntime.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;
using Xunit;
using ThemeVariant = Avalonia.Styling.ThemeVariant;

/// <summary>
/// <para>
/// The audit's colours held to what Avalonia draws, by plans/00004's decision 4. Each case keys a brush or
/// a colour <c>B</c> in a cell's resources and paints the cell with it over a known ground, headless with
/// Skia, and reads the pixel at the cell's centre. A case Avalonia does not load must be one the audit
/// gives no colour. The cases paint brushes, which no theme changes, so Simple's window template alone
/// presents them.
/// </para>
/// <para>
/// The pixel must be what Skia's CPU raster draws for the colour the audit resolves, to the level. The
/// raster blends in integers of its own, the same on every CPU, which fall short of the exact blend the
/// audit composites by up to two levels or exceed it by one, so the test predicts the pixel with the
/// raster's arithmetic, <see cref="Rastered"/>, rather than with the audit's composite, by the developer's
/// decision of 2026-10-15.
/// </para>
/// <para>
/// The brand output's stacks are held the same way: a brush painted by an element at an opacity of its own must be
/// drawn at the alpha the brand output makes for that stack, its opacity times the element's, truncated once.
/// </para>
/// <para>
/// Where an alias resolves is held the same way, by plans/00006's decision 6. Those cases are drawn where an
/// application holds a theme: the theme in its styles, the files a configuration merges and a consumer's tokens in its
/// resources, each a dictionary of its own, and a control painted with <c>{DynamicResource B}</c> under a variant.
/// Where Avalonia draws nothing, the lookup having thrown, the audit must give no colour.
/// </para>
/// </summary>
public sealed class DrawnColourTests : IDisposable
{
    private const string Namespaces = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
    private const string Black = "#000000";
    private const string Ink = "<Color x:Key=\"Ink\">#FFFFFF</Color>";
    private const string Faint = "<x:Double x:Key=\"Faint\">0.12</x:Double>";
    private const string Half = "<SolidColorBrush x:Key=\"Half\" Color=\"#FFFFFF\" Opacity=\"0.5\" />";
    private const string Local = " xmlns:local=\"using:XamlQuality.ThemeRuntime.Tests\"";
    private const string Dim = "#404040";
    private const string Pale = "#B0B0B0";
    private static readonly AuditColor BlackGround = new(255, 0, 0, 0);

    private readonly string _root = Directory.CreateTempSubdirectory("drawn-colour-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [AvaloniaFact]
    public void A_brush_at_an_opacity_is_drawn_at_the_alpha_the_audit_resolves()
    {
        AssertDrawnAsRastered(
            new Cell("white at 0.1", Black, Brush("#FFFFFF", "0.1")),
            new Cell("white at 0.12", Black, Brush("#FFFFFF", "0.12")),
            new Cell("white at 0.5", Black, Brush("#FFFFFF", "0.5")),
            new Cell("white at 0.6", Black, Brush("#FFFFFF", "0.6")),
            new Cell("white at 0.62", Black, Brush("#FFFFFF", "0.62")),
            new Cell("#80FFFFFF at 0.7", Black, Brush("#80FFFFFF", "0.7")));
    }

    [AvaloniaFact]
    public void A_brush_based_on_another_resource_is_drawn_as_the_audit_resolves_it_in_each_spelling()
    {
        AssertDrawnAsRastered(
            new Cell("an alias element of a brush at 0.12", Black,
                Brush("#FFFFFF", "0.12", "Base") + "<StaticResource x:Key=\"B\" ResourceKey=\"Base\" />"),
            new Cell("an alias element of a brush at 0.5, used as a dynamic resource", Black,
                Brush("#FFFFFF", "0.5", "Base") + "<StaticResource x:Key=\"B\" ResourceKey=\"Base\" />", Dynamic: true),
            new Cell("a DynamicResource alias element of a brush at 0.12", Black,
                Brush("#FFFFFF", "0.12", "Base") + "<DynamicResource x:Key=\"B\" ResourceKey=\"Base\" />"),
            new Cell("a brush at 0.12 whose Color is a static resource of a colour", Black,
                Ink + "<SolidColorBrush x:Key=\"B\" Color=\"{StaticResource Ink}\" Opacity=\"0.12\" />"),
            new Cell("a brush at 0.12 whose Color is a dynamic resource of a colour", Black,
                Ink + "<SolidColorBrush x:Key=\"B\" Color=\"{DynamicResource Ink}\" Opacity=\"0.12\" />", Dynamic: true),
            new Cell("a brush at 0.12 whose Color is a static resource of a colour's alias", Black,
                Ink + "<StaticResource x:Key=\"InkAlias\" ResourceKey=\"Ink\" /><SolidColorBrush x:Key=\"B\" Color=\"{StaticResource InkAlias}\" Opacity=\"0.12\" />"),
            new Cell("a brush at 0.12 whose Color is a StaticResource property element", Black,
                Ink + "<SolidColorBrush x:Key=\"B\" Opacity=\"0.12\"><SolidColorBrush.Color><StaticResource ResourceKey=\"Ink\" /></SolidColorBrush.Color></SolidColorBrush>"),
            new Cell("a brush at 0.12 whose Color is a literal property element", Black,
                "<SolidColorBrush x:Key=\"B\" Opacity=\"0.12\"><SolidColorBrush.Color>#FFFFFF</SolidColorBrush.Color></SolidColorBrush>"),
            new Cell("a brush whose Color is a colour resource of #1EFFFFFF", Black,
                "<Color x:Key=\"Translucent\">#1EFFFFFF</Color><SolidColorBrush x:Key=\"B\" Color=\"{StaticResource Translucent}\" />"),
            new Cell("a colour resource of #1EFFFFFF as the background itself", Black,
                "<Color x:Key=\"B\">#1EFFFFFF</Color>"));
    }

    [AvaloniaFact]
    public void An_opacity_a_brush_takes_from_a_resource_is_drawn_as_the_audit_resolves_it_in_each_spelling()
    {
        AssertDrawnAsRastered(
            new Cell("Opacity a static resource of 0.12", Black,
                Faint + "<SolidColorBrush x:Key=\"B\" Color=\"#FFFFFF\" Opacity=\"{StaticResource Faint}\" />"),
            new Cell("Opacity a dynamic resource of 0.12", Black,
                Faint + "<SolidColorBrush x:Key=\"B\" Color=\"#FFFFFF\" Opacity=\"{DynamicResource Faint}\" />"),
            new Cell("Opacity a StaticResource property element of 0.12", Black,
                Faint + "<SolidColorBrush x:Key=\"B\" Color=\"#FFFFFF\"><SolidColorBrush.Opacity><StaticResource ResourceKey=\"Faint\" /></SolidColorBrush.Opacity></SolidColorBrush>"),
            new Cell("Opacity a literal property element of 0.12", Black,
                "<SolidColorBrush x:Key=\"B\" Color=\"#FFFFFF\"><SolidColorBrush.Opacity>0.12</SolidColorBrush.Opacity></SolidColorBrush>"),
            new Cell("Color and Opacity both literal property elements", Black,
                "<SolidColorBrush x:Key=\"B\"><SolidColorBrush.Color>#FFFFFF</SolidColorBrush.Color><SolidColorBrush.Opacity>0.12</SolidColorBrush.Opacity></SolidColorBrush>"));
    }

    [AvaloniaFact]
    public void A_brush_whose_Color_names_a_brush_does_not_load_and_the_audit_gives_it_no_colour()
    {
        AssertNotLoadedAndUnresolved(
            new Cell("Color a static resource of a brush", Black,
                Half + "<SolidColorBrush x:Key=\"B\" Color=\"{StaticResource Half}\" Opacity=\"0.5\" />"),
            new Cell("Color a dynamic resource of a brush", Black,
                Half + "<SolidColorBrush x:Key=\"B\" Color=\"{DynamicResource Half}\" Opacity=\"0.5\" />", Dynamic: true),
            new Cell("Color a static resource of a brush's alias", Black,
                Half + "<StaticResource x:Key=\"HalfAlias\" ResourceKey=\"Half\" /><SolidColorBrush x:Key=\"B\" Color=\"{StaticResource HalfAlias}\" />"),
            new Cell("Color a static resource of a brush whose own Color is a resource", Black,
                Ink + "<SolidColorBrush x:Key=\"InkBrush\" Color=\"{StaticResource Ink}\" /><SolidColorBrush x:Key=\"B\" Color=\"{StaticResource InkBrush}\" />"),
            new Cell("Color a StaticResource property element of a brush", Black,
                Half + "<SolidColorBrush x:Key=\"B\"><SolidColorBrush.Color><StaticResource ResourceKey=\"Half\" /></SolidColorBrush.Color></SolidColorBrush>"));
    }

    [AvaloniaFact]
    public void A_brush_blended_over_a_ground_is_drawn_as_Skia_rasters_the_audits_colour()
    {
        AssertDrawnAsRastered(
            new Cell("white at 0.12, over #454545", "#454545", Brush("#FFFFFF", "0.12")),
            new Cell("white at 0.12, over #4CAF50", "#4CAF50", Brush("#FFFFFF", "0.12")),
            new Cell("#F9F9F9 at 0.6, over #454545, a level below the exact blend", "#454545", Brush("#F9F9F9", "0.6")),
            new Cell("#191919 at 0.506, over #C2C2C2, two levels below the exact blend", "#C2C2C2", Brush("#191919", "0.506")),
            new Cell("#C0C0C0 at 0.25, over #C8C8C8, a level above the exact blend", "#C8C8C8", Brush("#C0C0C0", "0.25")),
            new Cell("#1C1F23 at 0.62, over white", "#FFFFFF", Brush("#1C1F23", "0.62")),
            new Cell("#2E7D32 at 0.5 whose Opacity is a resource, over #F5F5F5", "#F5F5F5",
                "<x:Double x:Key=\"Fade\">0.5</x:Double><SolidColorBrush x:Key=\"B\" Color=\"#2E7D32\" Opacity=\"{StaticResource Fade}\" />"));
    }

    /// <summary>
    /// Plans/00004's measured cases with an element's own opacity, the brand output's stacks: the brush's opacity times
    /// the element's, truncated once, drew 85, 30 and 126 over black where truncating each in turn gives 85, 29 and 125;
    /// and one over a ground where Skia's raster and the exact blend part.
    /// </summary>
    [AvaloniaFact]
    public void A_brush_inside_an_element_at_an_opacity_is_drawn_at_the_alpha_the_brand_output_makes()
    {
        AssertDrawnAtTheBrandOutputsAlpha(
            new Cell("a brush at 0.6 inside an element at 0.56", Black, Brush("#FFFFFF", "0.6"), Opacity: "0.56"),
            new Cell("a brush at 0.12 inside an element at 0.99", Black, Brush("#FFFFFF", "0.12"), Opacity: "0.99"),
            new Cell("a brush at 0.5 inside an element at 0.99", Black, Brush("#FFFFFF", "0.5"), Opacity: "0.99"),
            new Cell("#F9F9F9 at 0.6 inside an element at 0.56, over #5B5B5B", "#5B5B5B", Brush("#F9F9F9", "0.6"), Opacity: "0.56"));
    }

    [AvaloniaFact]
    public void An_alias_reads_its_own_dictionaries_first_then_the_applications_resources_then_its_styles()
    {
        AssertHeldAsResolved(
            new Held("a theme alias whose target a merged file redefines", "Light",
                Colour("Target", Dim) + Alias("B", "Target"), [Colour("Target", Pale)]),
            new Held("a theme brush whose Color is a static resource a merged file redefines", "Light",
                Colour("Target", Dim) + "<SolidColorBrush x:Key=\"B\" Color=\"{StaticResource Target}\" />", [Colour("Target", Pale)]),
            new Held("a merged file's alias whose target only the theme holds", "Light",
                Colour("Target", Dim), [Alias("B", "Target")]),
            new Held("a merged file's alias whose target another merged file and the theme hold", "Light",
                Colour("Target", Dim), [Colour("Target", Pale), Alias("B", "Target")]),
            new Held("a consumer token aliasing a theme key", "Light",
                Colour("Target", Dim), [], Tokens: Alias("B", "Target")),
            new Held("a consumer token aliasing a key a merged file and the theme hold", "Light",
                Colour("Target", Dim), [Colour("Target", Pale)], Tokens: Alias("B", "Target")));
    }

    [AvaloniaFact]
    public void An_alias_resolves_in_the_variant_of_the_theme_dictionary_around_it_or_in_Default_alone()
    {
        AssertHeldAsResolved(
            new Held("an alias outside every theme dictionary, under Dark, whose target Default and Dark hold", "Dark",
                Variants(Colour("Target", Dim), Colour("Target", Pale)) + Alias("B", "Target"), []),
            new Held("an alias in Dark's dictionary, under Dark, whose target Default and Dark hold", "Dark",
                Variants(Colour("Target", Dim), Colour("Target", Pale) + Alias("B", "Target")), []),
            new Held("an alias outside every theme dictionary, under Dark, whose target only Dark holds", "Dark",
                Variants(string.Empty, Colour("Target", Pale)) + Alias("B", "Target"), []),
            new Held("a merged file's alias in its Dark dictionary whose target the theme's Default and Dark hold", "Dark",
                Variants(Colour("Target", Dim), Colour("Target", Pale)), [Variants(string.Empty, Alias("B", "Target"))]),
            new Held("a dictionary's own entry and its Dark dictionary's, under Dark", "Dark",
                Colour("B", Dim) + Variants(string.Empty, Colour("B", Pale)), []));
    }

    [AvaloniaFact]
    public void An_alias_in_a_dictionary_a_theme_holds_by_its_class_resolves_in_that_dictionary_when_it_is_built()
    {
        const string Palette = "<ResourceDictionary.MergedDictionaries><local:XClassPalette /></ResourceDictionary.MergedDictionaries>";
        AssertHeldAsResolved(
            new Held("its target in the theme and in a merged file", "Light", Palette + Colour("Outside", Dim), [Colour("Outside", Pale)]),
            new Held("its target in the theme alone", "Light", Palette + Colour("Outside", Dim), []));
    }

    [AvaloniaFact]
    public void An_alias_of_its_own_key_is_not_found_in_its_own_dictionary_while_it_builds()
    {
        AssertHeldAsResolved(
            new Held("an alias of its own key over a merged dictionary that holds it", "Light",
                "<ResourceDictionary.MergedDictionaries><ResourceDictionary>" + Colour("B", Dim)
                + "</ResourceDictionary></ResourceDictionary.MergedDictionaries>" + Alias("B", "B"), []));
    }

    [AvaloniaFact]
    public void A_dynamic_alias_in_a_theme_is_looked_up_from_the_application()
    {
        AssertHeldAsResolved(
            new Held("a theme brush whose Color is a dynamic resource a merged file redefines", "Light",
                Colour("Target", Dim) + "<SolidColorBrush x:Key=\"B\" Color=\"{DynamicResource Target}\" />", [Colour("Target", Pale)]),
            new Held("a theme brush outside every theme dictionary, under Dark, whose Color is a dynamic resource Default and Dark hold", "Dark",
                Variants(Colour("Target", Dim), Colour("Target", Pale)) + "<SolidColorBrush x:Key=\"B\" Color=\"{DynamicResource Target}\" />", []));
    }

    private static string Brush(string color, string opacity, string key = "B") =>
        "<SolidColorBrush x:Key=\"" + key + "\" Color=\"" + color + "\" Opacity=\"" + opacity + "\" />";

    private static string Colour(string key, string colour) => "<Color x:Key=\"" + key + "\">" + colour + "</Color>";

    private static string Alias(string key, string target) => "<StaticResource x:Key=\"" + key + "\" ResourceKey=\"" + target + "\" />";

    /// <summary>A dictionary's theme dictionaries, holding <paramref name="byDefault"/> in Default's and <paramref name="dark"/> in Dark's.</summary>
    private static string Variants(string byDefault, string dark) =>
        "<ResourceDictionary.ThemeDictionaries><ResourceDictionary x:Key=\"Default\">" + byDefault
        + "</ResourceDictionary><ResourceDictionary x:Key=\"Dark\">" + dark + "</ResourceDictionary></ResourceDictionary.ThemeDictionaries>";

    private static string ThemeMarkup(string resources) =>
        "<Styles " + Namespaces + Local + "><Styles.Resources><ResourceDictionary>" + resources + "</ResourceDictionary></Styles.Resources></Styles>";

    private static string DictionaryMarkup(string resources) => "<ResourceDictionary " + Namespaces + Local + ">" + resources + "</ResourceDictionary>";

    /// <summary>
    /// Each case, held where an application holds it, must be drawn as Skia's CPU raster draws the colour the audit
    /// resolves, to the level; and where Avalonia draws nothing, its lookup having thrown or found nothing, the audit must
    /// give no colour.
    /// </summary>
    private void AssertHeldAsResolved(params Held[] cases)
    {
        List<string> wrong = [];
        foreach (Held held in cases)
        {
            AuditColor? resolved = Resolve(held);
            AuditColor drawn = BlackGround;
            Exception? thrown = Record.Exception(() => drawn = Draw(held));
            if (thrown is not null)
            {
                if (resolved is not null)
                {
                    wrong.Add(held.What + ": Avalonia threw " + thrown.GetType().Name + ": " + thrown.Message + ", where the audit resolves " + resolved);
                }
            }
            else if (resolved is { } colour ? Rastered(colour, BlackGround) != drawn : drawn != BlackGround)
            {
                wrong.Add(held.What + ": drawn " + drawn + ", where the audit " + (resolved is null ? "gives no colour" : "resolves " + resolved));
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>Each cell must be drawn as Skia's CPU raster draws the colour the audit resolves, to the level.</summary>
    private void AssertDrawnAsRastered(params Cell[] cells)
    {
        List<string> wrong = [];
        foreach (Cell cell in cells)
        {
            AuditColor drawn = Draw(cell);
            AuditColor? resolved = Resolve(cell);
            if (resolved is not { } colour || Rastered(colour, Ground(cell)) != drawn)
            {
                wrong.Add(cell.What + ": drawn " + drawn + ", where the audit "
                          + (resolved is null ? "gives no colour" : "resolves " + resolved + ", rastered as " + Rastered(resolved.Value, Ground(cell))));
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>
    /// Each cell, its <c>B</c> drawn inside an element at its opacity, must be drawn as Skia's CPU raster draws <c>B</c>'s
    /// colour at the alpha the brand output makes for the stack <c>B@opacity</c>, to the level.
    /// </summary>
    private void AssertDrawnAtTheBrandOutputsAlpha(params Cell[] cells)
    {
        List<string> wrong = [];
        foreach (Cell cell in cells)
        {
            AuditColor drawn = Draw(cell);
            AuditColor colour = Resolve(cell)!.Value with { A = BrandAlpha(cell) };
            if (Rastered(colour, Ground(cell)) != drawn)
            {
                wrong.Add(cell.What + ": drawn " + drawn + ", where the brand output's alpha makes " + colour + ", rastered as " + Rastered(colour, Ground(cell)));
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>
    /// The alpha the brand output makes for the stack <c>B@opacity</c>, read through its public face: its exact blend of a
    /// colour at an alpha <c>a</c> over white is, in every channel, <c>255 - a</c> above the same over black.
    /// </summary>
    private byte BrandAlpha(Cell cell)
    {
        string directory = Directory.CreateDirectory(Path.Combine(_root, Guid.NewGuid().ToString("N"))).FullName;
        File.WriteAllText(Path.Combine(directory, "Cell.axaml"), "<ResourceDictionary " + Namespaces + ">" + cell.Resources + "</ResourceDictionary>");
        File.WriteAllText(Path.Combine(directory, "Brand.template.md"), "{{mix B@" + cell.Opacity + "/#000000}}\n{{mix B@" + cell.Opacity + "/#FFFFFF}}\n");
        AuditConfig config = AuditConfig.Parse(
            """
            {
              "themes": [ { "name": "Cell", "entry": "Cell.axaml", "baseDirectory": "." } ],
              "brand": [ { "template": "Brand.template.md", "output": "Brand.md", "theme": "Cell" } ]
            }
            """,
            directory);
        BrandResult result = BrandRunner.Fill(config, config.Brand[0]);
        Assert.True(result.Problems.Count == 0, string.Join(Environment.NewLine, result.Problems));
        string[] lines = result.Markdown.Split('\n');
        Assert.True(AuditColor.TryParse(lines[1], out AuditColor overBlack), result.Markdown);
        Assert.True(AuditColor.TryParse(lines[2], out AuditColor overWhite), result.Markdown);
        return (byte)(255 - overWhite.R + overBlack.R);
    }

    private void AssertNotLoadedAndUnresolved(params Cell[] cells)
    {
        List<string> wrong = [];
        foreach (Cell cell in cells)
        {
            Exception? thrown = Record.Exception(() => Draw(cell));
            if (!Chain(thrown).Any(e => e is InvalidCastException))
            {
                wrong.Add(cell.What + ": " + (thrown is null ? "loaded and drew" : "threw " + thrown.GetType().Name + ": " + thrown.Message)
                          + ", where loading it was measured to throw an InvalidCastException");
            }

            if (Resolve(cell) is { } resolved)
            {
                wrong.Add(cell.What + ": the audit resolves " + resolved);
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>
    /// What Skia's CPU raster draws for <paramref name="colour"/> over an opaque <paramref name="ground"/>, in the
    /// Skia that SkiaSharp 3.119.4 carries. A translucent solid colour takes <c>SkARGB32_Blitter</c>, which premultiplies each
    /// channel as <c>(c * (a + 1)) &gt;&gt; 8</c>, and <c>blit_row_color32</c> blends it over the ground as
    /// <c>(d * inv + (s &lt;&lt; 8) + 128) &gt;&gt; 8</c>, <c>inv</c> being <c>255 - a</c> plus its top bit, in
    /// integers, so alike on every CPU. An alpha of 0 draws nothing, and one of 255 the colour itself.
    /// </summary>
    private static AuditColor Rastered(AuditColor colour, AuditColor ground)
    {
        int a = colour.A;
        if (a == 0)
        {
            return ground;
        }

        if (a == 255)
        {
            return colour;
        }

        int inv = 255 - a + ((255 - a) >> 7);
        byte Channel(byte c, byte d) => (byte)(((d * inv) + (((c * (a + 1)) >> 8) << 8) + 128) >> 8);
        return new AuditColor(255, Channel(colour.R, ground.R), Channel(colour.G, ground.G), Channel(colour.B, ground.B));
    }

    private static IEnumerable<Exception> Chain(Exception? exception)
    {
        for (Exception? e = exception; e is not null; e = e.InnerException)
        {
            yield return e;
        }
    }

    /// <summary>The colour the cell's centre is drawn in, read from the frame Skia renders.</summary>
    private static AuditColor Draw(Cell cell)
    {
        if (!Application.Current!.Styles.OfType<SimpleTheme>().Any())
        {
            Application.Current.Styles.Add(new SimpleTheme());
        }

        string use = cell.Dynamic ? "{DynamicResource B}" : "{StaticResource B}";
        string opacity = cell.Opacity is { } own ? " Opacity=\"" + own + "\"" : string.Empty;
        string markup = "<Border " + Namespaces + " Background=\"" + cell.Ground + "\"><Border.Resources>" + cell.Resources
                        + "</Border.Resources><Border" + opacity + " Background=\"" + use + "\" /></Border>";
        return Paint((Control)AvaloniaRuntimeXamlLoader.Load(markup, typeof(DrawnColourTests).Assembly), variant: null);
    }

    /// <summary>
    /// The colour the case's control is drawn in, its theme in the application's styles after Simple, and its merged files
    /// and then its tokens in the application's resources, each loaded as a dictionary of its own. The application's own
    /// styles and resources are put back afterwards.
    /// </summary>
    private static AuditColor Draw(Held held)
    {
        Application application = Application.Current!;
        List<IStyle> styles = [.. application.Styles];
        IResourceDictionary stylesResources = application.Styles.Resources;
        IResourceDictionary resources = application.Resources;
        ThemeVariant? requested = application.RequestedThemeVariant;
        try
        {
            Styles theme =(Styles)AvaloniaRuntimeXamlLoader.Load(ThemeMarkup(held.Theme), typeof(DrawnColourTests).Assembly);
            ResourceDictionary merged = new();
            foreach (string merge in held.Merges)
            {
                merged.MergedDictionaries.Add((ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(DictionaryMarkup(merge), typeof(DrawnColourTests).Assembly));
            }

            if (held.Tokens is { } tokens)
            {
                merged.MergedDictionaries.Add((ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(DictionaryMarkup(tokens), typeof(DrawnColourTests).Assembly));
            }

            application.Styles.Clear();
            application.Styles.Resources = new ResourceDictionary();
            application.Styles.Add(new SimpleTheme());
            application.Styles.Add(theme);
            application.Resources = merged;
            application.RequestedThemeVariant = held.Variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
            string markup = "<Border " + Namespaces + " Background=\"" + Black + "\"><Border Background=\"{DynamicResource B}\" /></Border>";
            return Paint((Control)AvaloniaRuntimeXamlLoader.Load(markup, typeof(DrawnColourTests).Assembly), variant: null);
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

    /// <summary>The colour the centre of a window holding <paramref name="content"/> is drawn in, read from the frame Skia renders.</summary>
    internal static AuditColor Paint(Control content, ThemeVariant? variant)
    {
        Window window = new() { Width = 40, Height = 40, Content = content };
        if (variant is not null)
        {
            window.RequestedThemeVariant = variant;
        }

        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            using WriteableBitmap frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Skia rendered no frame.");
            using ILockedFramebuffer buffer = frame.Lock();
            byte[] pixel = new byte[4];
            Marshal.Copy(buffer.Address + (20 * buffer.RowBytes) + (20 * 4), pixel, 0, 4);
            return buffer.Format.ToString() switch
            {
                "Rgba8888" => new AuditColor(pixel[3], pixel[0], pixel[1], pixel[2]),
                "Bgra8888" => new AuditColor(pixel[3], pixel[2], pixel[1], pixel[0]),
                string other => throw new InvalidOperationException("The frame is " + other + ", which this test does not read."),
            };
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>What the audit resolves the cell's <c>B</c> to, reading its resources as a theme.</summary>
    private AuditColor? Resolve(Cell cell)
    {
        string directory = Directory.CreateDirectory(Path.Combine(_root, Guid.NewGuid().ToString("N"))).FullName;
        string file = Path.Combine(directory, "Cell.axaml");
        File.WriteAllText(file, "<ResourceDictionary " + Namespaces + ">" + cell.Resources + "</ResourceDictionary>");
        return ThemeInventory.Build(file, directory).ForVariant(ThemeInventory.DefaultVariant).Resolve("B");
    }

    /// <summary>
    /// What the audit resolves the case's <c>B</c> to under its variant, as it resolves a contrast pair's key: the theme
    /// with its merged files, and the tokens as a consumer's. The class-backed palette is written beside them, where the
    /// audit finds a dictionary a theme holds by its element.
    /// </summary>
    private AuditColor? Resolve(Held held)
    {
        string directory = Directory.CreateDirectory(Path.Combine(_root, Guid.NewGuid().ToString("N"))).FullName;
        File.Copy(Path.Combine(ProjectDirectory(), "Fixtures", "Drawn", "XClassPalette.axaml"), Path.Combine(directory, "XClassPalette.axaml"));
        string theme = Path.Combine(directory, "Theme.axaml");
        File.WriteAllText(theme, ThemeMarkup(held.Theme));
        List<string> merges = [];
        foreach (string merge in held.Merges)
        {
            string file = Path.Combine(directory, "Merge" + merges.Count + ".axaml");
            File.WriteAllText(file, DictionaryMarkup(merge));
            merges.Add(file);
        }

        ThemeInventory? tokens = null;
        if (held.Tokens is { } body)
        {
            string file = Path.Combine(directory, "Tokens.axaml");
            File.WriteAllText(file, DictionaryMarkup(body));
            tokens = ThemeInventory.Build(new ThemeSource(file, directory));
        }

        ThemeInventory inventory = ThemeInventory.Build(new ThemeSource(theme, directory) { Merge = merges });
        return ContrastAudit.Score([new ContrastPair("B", "B", 1)], inventory, [held.Variant], tokens)[0].Foreground;
    }

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

    private static AuditColor Ground(Cell cell)
    {
        Assert.True(AuditColor.TryParse(cell.Ground, out AuditColor ground), cell.Ground);
        return ground;
    }

    /// <summary>A cell painted over <paramref name="Ground"/> by the resource <c>B</c> among <paramref name="Resources"/>.</summary>
    /// <param name="What">The case, as a failure names it.</param>
    /// <param name="Ground">The opaque colour under the cell.</param>
    /// <param name="Resources">The cell's resources, which the audit reads as a theme.</param>
    /// <param name="Dynamic">Whether the cell takes <c>B</c> as a dynamic resource rather than a static one.</param>
    /// <param name="Opacity">The opacity of the element <c>B</c> paints, which pushes it; none when <c>null</c>.</param>
    private sealed record Cell(string What, string Ground, string Resources, bool Dynamic = false, string? Opacity = null);

    /// <summary>A case drawn where an application holds it: a control over black painted with <c>{DynamicResource B}</c>.</summary>
    /// <param name="What">The case, as a failure names it.</param>
    /// <param name="Variant">The variant the control is drawn in, <c>Light</c> or <c>Dark</c>, and the one the audit resolves <c>B</c> under.</param>
    /// <param name="Theme">The resources of the theme in the application's styles, which the audit reads as a theme's entry.</param>
    /// <param name="Merges">The dictionaries a configuration merges after the theme, in the application's resources, the last searched first.</param>
    /// <param name="Tokens">A consumer's tokens, in the application's resources and searched before the merged files; none when <c>null</c>.</param>
    private sealed record Held(string What, string Variant, string Theme, string[] Merges, string? Tokens = null);
}
