// Two controls of one name in two namespaces, for TemplatePartRuleTests and TemplatePartReadingTests. A
// ControlTheme names its target without the namespace, so BNXQ1003 cannot say which of them a theme is for.
namespace XamlQuality.Tests.Rules.PartTwins
{
    /// <summary>One of two controls a theme cannot tell apart by name, with a part of its own.</summary>
    public sealed class TwinPanel
    {
        /// <summary>The part this one declares.</summary>
        public const string LeftPart = "PART_Left";
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
}
