// Marked parameters BNAQ1011 cannot read, each named in Skipped, never checked: plans/00011 decision 6. The
// attribute here takes an object, so it can be given anything; Absent, which one names, is never beside this DLL.
#pragma warning disable CA1052, CA1812, IDE0060

using System;
using Bennewitz.Ninja;

namespace Bennewitz.Ninja
{
    /// <summary>A key attribute declared with looser arguments than the documented one.</summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class ResourceKeyAttribute : Attribute
    {
        public ResourceKeyAttribute()
        {
        }

        public ResourceKeyAttribute(object keys) => Keys = keys;

        public object? Keys { get; set; }
    }
}

namespace Keyed.Odd
{
    internal static class OddKeys
    {
        internal const string One = "Odd.One";
    }

    /// <summary>A class with no const string field.</summary>
    internal static class NoConstants
    {
        internal static readonly string NotConstant = "Odd.NotConstant";
        internal const int Number = 1;
    }

    /// <summary>Two fields that hold one value.</summary>
    internal static class Twins
    {
        internal const string First = "Odd.Twin";
        internal const string Second = "Odd.Twin";
        internal const string Single = "Odd.Single";
    }

    internal static class Helpers
    {
        internal static object NotAString([ResourceKey(typeof(OddKeys))] int key) => key;

        internal static object NoType([ResourceKey("OddKeys")] string key) => key;

        internal static object NoArgument([ResourceKey] string key) => key;

        internal static object ANamedArgument([ResourceKey(Keys = typeof(OddKeys))] string key) => key;

        internal static object Unresolved([ResourceKey(typeof(global::Absent.Gone))] string key) => key;

        internal static object NoConstantString([ResourceKey(typeof(NoConstants))] string key) => key;

        internal static object TwinKeys([ResourceKey(typeof(Twins))] string key) => key;
    }

    internal static class Callers
    {
        internal static object NotAString() => Helpers.NotAString(1);

        internal static object NoType() => Helpers.NoType(OddKeys.One);

        internal static object Twin() => Helpers.TwinKeys(Twins.First);

        internal static object Single() => Helpers.TwinKeys(Twins.Single);
    }
}
