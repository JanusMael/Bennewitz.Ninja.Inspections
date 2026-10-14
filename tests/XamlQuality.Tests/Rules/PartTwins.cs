// Types of one name in two namespaces, for TemplatePartRuleTests and TemplatePartReadingTests. BNXQ1003 reads a
// ControlTheme's target by its name alone, so it cannot say which of them a theme is for.
namespace XamlQuality.Tests.Rules.PartTwins
{
    /// <summary>One of two controls a theme cannot tell apart by name, with a part of its own.</summary>
    public sealed class TwinPanel
    {
        /// <summary>The part this one declares.</summary>
        public const string LeftPart = "PART_Left";
    }

    /// <summary>One of two types of one name whose parts are lookups alone, made on no control's template.</summary>
    public static class LookingTwin
    {
        /// <summary>Looks a part up on a name scope of its own.</summary>
        /// <returns>Whatever the lookup returns.</returns>
        public static object? Seek() => FakeNameScope.Find("PART_Sought");
    }
}

namespace XamlQuality.Tests.Rules.PartTwins.Elsewhere
{
    /// <summary>The other control named <c>TwinPanel</c>, with a part the first does not declare.</summary>
    public sealed class TwinPanel
    {
        /// <summary>The part this one declares.</summary>
        public const string RightPart = "PART_Right";
    }

    /// <summary>The other type named <c>LookingTwin</c>, whose part is a lookup too.</summary>
    public static class LookingTwin
    {
        /// <summary>Looks a part up on a name scope of its own.</summary>
        /// <returns>Whatever the lookup returns.</returns>
        public static object? Seek() => FakeNameScope.Find("PART_Found");
    }
}
