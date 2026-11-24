// Every kind of use BNAQ1010 reads, in every kind of body the compiler makes, named for what each shows.
// Compiled twice: optimised with an embedded PDB (Confined) and unoptimised with one beside it (ConfinedDebug).
#pragma warning disable CA1822, CA1052, CA1034, CA1031

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Confined.App
{
    // A window the assembly declares: its constructor chains to Window's, which constructs nothing.
    public class DialogWindow : Window
    {
    }

    public static class Opener
    {
        public static Window OpenDeclared() => new DialogWindow();

        public static Window OpenForeign() => new Window();

        public static void ShowIt(Window window) => window.Show();
    }

    // The one type allowed to make colours and brushes, and the type nested in it.
    public static class Palette
    {
        public static Color Parsed() => Color.Parse("#112233");

        public static IBrush Brush() => new ImmutableSolidColorBrush(Color.Parse("#445566"));

        public static IBrush None() => Brushes.Transparent;

        public static class Nested
        {
            public static Color Red() => Colors.Red;
        }
    }

    // Colours made outside the palette, one in each kind of body.
    public class Outside
    {
        private static readonly Color Initialised = new(1, 2, 3, 4);

        public Color InAMethod() => new(1, 1, 1, 1);

        public Func<Color> InALambda() => () => new Color(2, 2, 2, 2);

        public Func<int, Color> InACapturingLambda(byte alpha) => n => new Color(alpha, (byte)n, 0, 0);

        public Color InALocalFunction()
        {
            return Make();

            static Color Make() => new(3, 3, 3, 3);
        }

        public async Task<Color> InAnAsyncMethod()
        {
            await Task.Yield();
            return new Color(4, 4, 4, 4);
        }

        public IEnumerable<Color> InAnIterator()
        {
            yield return new Color(5, 5, 5, 5);
        }

        public Color InAGetter => new(6, 6, 6, 6);

        public static Color FromTheInitialiser => Initialised;

        public Color ADefault() => default;

        public Func<string, Color> AMethodGroup() => Color.Parse;

        public Color ANamedColor() => Colors.Red;
    }

    public static class Lookups
    {
        public static object Brush(string key) => new DynamicResourceExtension(key);

        public static bool ThroughTheClass(Application app, string key) => app.TryGetResource(key, null, out _);

        public static bool ThroughTheInterface(IResourceNode node, string key) => node.TryGetResource(key, null, out _);
    }
}
