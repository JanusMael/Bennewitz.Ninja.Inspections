using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace FpProbe6;

internal static partial class Facts9
{
    /// <summary>The visuals of a row's own template, as Facts.cs reads them.</summary>
    private static IEnumerable<Visual> TemplateVisuals(Visual row) =>
        row.GetVisualChildren().SelectMany(AndBelow);

    private static IEnumerable<Visual> AndBelow(Visual visual)
    {
        yield return visual;
        if (visual is ItemsPresenter || (visual is ContentPresenter presenter && presenter.Content is not Control))
        {
            yield break;
        }

        foreach (Visual below in visual.GetVisualChildren().SelectMany(AndBelow))
        {
            yield return below;
        }
    }

    private static string? Painted(Visual visual) => visual switch
    {
        Shape => "Fill",
        Border or Panel or ContentPresenter or TextPresenter or TextBlock => "Background",
        _ => null,
    };

    private static IBrush? Read(Visual visual) => visual switch
    {
        Shape shape => shape.Fill,
        Border border => border.Background,
        Panel panel => panel.Background,
        ContentPresenter presenter => presenter.Background,
        TextPresenter text => text.Background,
        TextBlock block => block.Background,
        _ => null,
    };

    private static string Shown(IBrush? brush) => brush switch
    {
        null => "null",
        ISolidColorBrush solid => solid.Color.A == 0 ? "Transparent" : solid.Color.ToString(),
        _ => brush.GetType().Name,
    };

    /// <summary>Whether the rendered frame shows the marker at three quarters of the visual's width and half its height.</summary>
    private static string Pixel(WriteableBitmap? frame, Visual visual, TopLevel top)
    {
        if (frame is null)
        {
            return "(no frame)";
        }

        if (visual.TranslatePoint(new Point(visual.Bounds.Width * 0.75, visual.Bounds.Height / 2), top) is not { } point)
        {
            return "(not placed)";
        }

        using ILockedFramebuffer buffer = frame.Lock();
        int x = (int)point.X;
        int y = (int)point.Y;
        if (x < 0 || y < 0 || x >= buffer.Size.Width || y >= buffer.Size.Height)
        {
            return "(off frame)";
        }

        uint pixel = (uint)Marshal.ReadInt32(buffer.Address + (y * buffer.RowBytes) + (x * 4));
        bool marker = pixel == 0xFF010203u || pixel == 0xFF030201u;
        return marker ? "(drawn)" : $"(not drawn: {pixel:X8})";
    }
}
