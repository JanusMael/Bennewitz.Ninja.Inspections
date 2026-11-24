// Stand-ins for the Avalonia types AppKit's sweeps confine, under Avalonia's own names, so BNAQ1010 is proved
// on real compiled types with no Avalonia in the tests (plans/00011 decision 11). Only their names and shapes
// matter: the rule matches by full name.
#pragma warning disable CA1822, CA1052, CA1716, CA1720, CA1034

namespace Avalonia.Controls
{
    public class Window
    {
        public void Show() { }
    }

    public interface IResourceNode
    {
        bool TryGetResource(object key, object? theme, out object? value);
    }

    public class Application : IResourceNode
    {
        public bool TryGetResource(object key, object? theme, out object? value)
        {
            value = null;
            return false;
        }
    }
}

namespace Avalonia.Media
{
    public struct Color
    {
        public Color(byte a, byte r, byte g, byte b) { A = a; R = r; G = g; B = b; }

        public byte A { get; }

        public byte R { get; }

        public byte G { get; }

        public byte B { get; }

        public static Color Parse(string text) => new(255, (byte)text.Length, 0, 0);
    }

    public interface IBrush { }

    public interface ISolidColorBrush : IBrush
    {
        Color Color { get; }
    }

    // Implements IBrush through another interface and derives from no brush class, as Avalonia's does.
    public sealed class ImmutableSolidColorBrush(Color color) : ISolidColorBrush
    {
        public Color Color { get; } = color;
    }

    public static class Colors
    {
        public static Color Red => new(255, 255, 0, 0);

        // An internal type's member read where it is visible, its own assembly, as Avalonia's Colors reads KnownColors.
        public static Color Black => KnownColors.Black;
    }

    public static class Brushes
    {
        public static IBrush Transparent { get; } = new ImmutableSolidColorBrush(default);
    }

    internal static class KnownColors
    {
        internal static readonly Color Black = new(255, 0, 0, 0);
    }
}

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public sealed class DynamicResourceExtension(object key)
    {
        public object Key { get; } = key;
    }
}
