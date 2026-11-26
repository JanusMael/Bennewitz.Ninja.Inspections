// An assembly Keyed references, with its own declaration of the key attribute, internal as every consumer's is, a
// key class and a marked helper: BNAQ1011 finds the attribute through the reference by its full name alone.
#pragma warning disable CA1052

namespace Bennewitz.Ninja
{
    /// <summary>The key attribute, declared again here, as a second project of a consumer's would.</summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter)]
    internal sealed class ResourceKeyAttribute(System.Type keys) : System.Attribute
    {
        public System.Type Keys { get; } = keys;
    }
}

namespace Keyed.Library
{
    /// <summary>A key class in a referenced assembly.</summary>
    public static class LibraryKeys
    {
        public const string Frame = "Library.Frame";
        public const string Shadow = "Library.Shadow";
    }

    /// <summary>A marked helper in a referenced assembly.</summary>
    public static class LibraryPalette
    {
        public static object Paint([Bennewitz.Ninja.ResourceKey(typeof(LibraryKeys))] string key) => key;
    }
}
