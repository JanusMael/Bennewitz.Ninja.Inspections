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
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;
using Xunit;

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
/// </summary>
public sealed class DrawnColourTests : IDisposable
{
    private const string Namespaces = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
    private const string Black = "#000000";
    private const string Ink = "<Color x:Key=\"Ink\">#FFFFFF</Color>";
    private const string Faint = "<x:Double x:Key=\"Faint\">0.12</x:Double>";
    private const string Half = "<SolidColorBrush x:Key=\"Half\" Color=\"#FFFFFF\" Opacity=\"0.5\" />";

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

    private static string Brush(string color, string opacity, string key = "B") =>
        "<SolidColorBrush x:Key=\"" + key + "\" Color=\"" + color + "\" Opacity=\"" + opacity + "\" />";

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
        Control content = (Control)AvaloniaRuntimeXamlLoader.Load(markup, typeof(DrawnColourTests).Assembly);
        Window window = new() { Width = 40, Height = 40, Content = content };
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
}
