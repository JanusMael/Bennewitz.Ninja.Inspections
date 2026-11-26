// BNAQ1011's subject, and the subject of BNAQ1010's option that allows a use by its key: marked helpers, the key
// classes they name, and each path plans/00011 decision 6 follows a key along. Keyed compiles it optimised with its
// PDB embedded, KeyedDebug unoptimised with its PDB beside the DLL, since the compiler shapes locals differently.
#pragma warning disable CA1822, CA1052, CA1034, CA1812, IDE0060, IDE0039, IDE0042

using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Bennewitz.Ninja;
using Keyed.Library;

namespace Bennewitz.Ninja
{
    /// <summary>The key attribute, declared internal as every consumer declares it.</summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class ResourceKeyAttribute(Type keys) : Attribute
    {
        public Type Keys { get; } = keys;
    }
}

namespace Keyed.App
{
    /// <summary>The keys every clean path reaches.</summary>
    internal static class Colours
    {
        internal const string Accent = "Keyed.Accent";
        internal const string Danger = "Keyed.Danger";
        internal const string Calm = "Keyed.Calm";
        internal const string Quiet = "Keyed.Quiet";
        internal const string Warning = "Keyed.Warning";
        internal const string Info = "Keyed.Info";
        internal const string Subtle = "Keyed.Subtle";
        internal const string Deep = "Keyed.Deep";
        internal const string Eighth = "Keyed.Eighth";
        internal const string Spelt = "Keyed.Spelt";
        internal const string Tinted = "Keyed.Tinted";
        internal const string Captured = "Keyed.Captured";
        internal const string Local = "Keyed.Local";
        internal const string Paired = "Keyed.Paired";

        /// <summary>A key class nested in another.</summary>
        internal static class Nested
        {
            internal const string Inner = "Keyed.Inner";
        }
    }

    /// <summary>The marked helpers. Brush and Tint name one class, so they share its keys.</summary>
    internal static class Palette
    {
        internal static object Brush([ResourceKey(typeof(Colours))] string key) => key;

        internal static object Tint([ResourceKey(typeof(Colours))] string key) => key;

        internal static object Shade([ResourceKey(typeof(Colours.Nested))] string key) => key;
    }

    /// <summary>Each path a key takes to a helper, every one clean.</summary>
    internal sealed class Painter
    {
        internal object AConstant() => Palette.Brush(Colours.Accent);

        internal object ALocal()
        {
            string key = Colours.Danger;
            return Palette.Brush(key);
        }

        internal object ATernary(bool calm) => Palette.Brush(calm ? Colours.Calm : Colours.Quiet);

        internal int ATernaryOfTuples(bool warn)
        {
            var (key, weight) = warn ? (Colours.Warning, 1) : (Colours.Info, 2);
            Palette.Brush(key);
            return weight;
        }

        internal int ATuple()
        {
            (string Key, int Weight) pair = (Colours.Paired, 1);
            Palette.Brush(pair.Key);
            return pair.Weight;
        }

        internal object AParameter() => Glyph(Colours.Subtle);

        internal object TwoParameters() => Outer(Colours.Deep);

        internal object EightCallers() => Depth1(Colours.Eighth);

        internal object ALiteral() => Palette.Brush("Keyed.Spelt");

        internal object ASecondHelper() => Palette.Tint(Colours.Tinted);

        internal Func<object> ACapturedLocal()
        {
            string key = Colours.Captured;
            return () => Palette.Brush(key);
        }

        internal object ALocalFunction()
        {
            return Paint(Colours.Local);

            static object Paint(string key) => Palette.Brush(key);
        }

        internal object ANestedClass() => Palette.Shade(Colours.Nested.Inner);

        internal object AReferencedHelper() => LibraryPalette.Paint(LibraryKeys.Frame);

        private static object Glyph(string colour) => Palette.Brush(colour);

        private static object Outer(string outer) => Inner(outer);

        private static object Inner(string inner) => Palette.Brush(inner);

        private static object Depth1(string key) => Depth2(key);

        private static object Depth2(string key) => Depth3(key);

        private static object Depth3(string key) => Depth4(key);

        private static object Depth4(string key) => Depth5(key);

        private static object Depth5(string key) => Depth6(key);

        private static object Depth6(string key) => Depth7(key);

        private static object Depth7(string key) => Depth8(key);

        private static object Depth8(string key) => Palette.Brush(key);
    }

    /// <summary>The keys the faults name. Unused reaches no helper.</summary>
    internal static class Faults
    {
        internal const string Used = "Faults.Used";
        internal const string Unused = "Faults.Unused";
    }

    internal static class FaultyPalette
    {
        internal static object Brush([ResourceKey(typeof(Faults))] string key) => key;
    }

    /// <summary>Each key a helper must not be given, one method each.</summary>
    internal sealed class Faulty
    {
        private static readonly string FromAField = Faults.Used;

        internal object Used() => FaultyPalette.Brush(Faults.Used);

        internal object NoField() => FaultyPalette.Brush("Faults.Missing");

        internal object AField() => FaultyPalette.Brush(FromAField);

        internal object Computed(int n) => FaultyPalette.Brush("Faults." + n);

        internal object ADelegatesKey(Func<string> key) => FaultyPalette.Brush(key());

        internal Func<string, object> MadeIntoADelegate() => FaultyPalette.Brush;

        internal Func<string, object> ALambdasParameter() => key => FaultyPalette.Brush(key);

        internal object AnotherClassesKey([ResourceKey(typeof(Colours))] string key) => FaultyPalette.Brush(key);

        internal object NineCallers() => Depth1(Faults.Used);

        internal Func<string, object> AMethodMadeIntoADelegate() => Relay;

        private static object Relay(string key) => FaultyPalette.Brush(key);

        private static object Depth1(string key) => Depth2(key);

        private static object Depth2(string key) => Depth3(key);

        private static object Depth3(string key) => Depth4(key);

        private static object Depth4(string key) => Depth5(key);

        private static object Depth5(string key) => Depth6(key);

        private static object Depth6(string key) => Depth7(key);

        private static object Depth7(string key) => Depth8(key);

        private static object Depth8(string key) => Depth9(key);

        private static object Depth9(string key) => FaultyPalette.Brush(key);
    }

    /// <summary>A public method, whose callers outside the assembly the scan cannot see.</summary>
    public sealed class Exposed
    {
        public object Paint(string key) => FaultyPalette.Brush(key);
    }

    internal abstract class Painted
    {
        internal abstract object Paint(string key);
    }

    /// <summary>An override, which a call through its base reaches unseen.</summary>
    internal sealed class Overriding : Painted
    {
        internal override object Paint(string key) => FaultyPalette.Brush(key);
    }

    /// <summary>Resource lookups for BNAQ1010's key option, each allowed or not by where its key comes from.</summary>
    internal static class Lookups
    {
        private static readonly string Field = "Lookups.Field";

        internal static object? Directly(IResourceNode node, [ResourceKey(typeof(Colours))] string key) =>
            node.TryGetResource(key, null, out object? value) ? value : null;

        internal static object? ThroughAPrivateMethod(IResourceNode node, [ResourceKey(typeof(Colours))] string key) =>
            Find(node, key);

        internal static object? Resolve(IResourceNode node, [ResourceKey(typeof(Colours))] string key) =>
            FoundOrNot(node, key);

        internal static object? Refresh(IResourceNode node) => FoundOrNot(node, Field);

        internal static object? ALiteral(IResourceNode node) =>
            node.TryGetResource("Lookups.Literal", null, out object? value) ? value : null;

        internal static object? ALiteralInAMarkedMethod(IResourceNode node, [ResourceKey(typeof(Colours))] string key) =>
            node.TryGetResource("Lookups.Other", null, out object? value) ? value : null;

        public static object? AnUnmarkedParameter(IResourceNode node, string key) =>
            node.TryGetResource(key, null, out object? value) ? value : null;

        internal static object? AField(IResourceNode node) =>
            node.TryGetResource(Field, null, out object? value) ? value : null;

        internal static object? AComputedKey(IResourceNode node, int n) =>
            node.TryGetResource("Lookups." + n, null, out object? value) ? value : null;

        internal static object AConstructionByItsKey([ResourceKey(typeof(Colours))] string key) =>
            new DynamicResourceExtension(key);

        internal static object AConstructionWithALiteral() => new DynamicResourceExtension("Lookups.Made");

        private static object? Find(IResourceNode node, string key) =>
            node.TryGetResource(key, null, out object? value) ? value : null;

        private static object? FoundOrNot(IResourceNode node, string key) =>
            node.TryGetResource(key, null, out object? value) ? value : null;
    }
}
