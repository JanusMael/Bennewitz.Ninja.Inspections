using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// Whether a consumer's definition holds the theme's own value, by plans/00007's decision 1: the same colour and opacity,
/// an alias of the same key, or the same element, read with each prefix as the namespace it names and an <c>x:</c> type as
/// the type it stands for, with the same attributes and content and no reference to a resource.
/// </summary>
internal static class SameValue
{
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XName Key = XName.Get("Key", XamlNamespace);

    /// <summary>Whether <paramref name="consumer"/> holds the value of <paramref name="theme"/>.</summary>
    public static bool As(ResourceDefinition consumer, ResourceDefinition theme)
    {
        if (consumer.Value is not ResourceValue.Opaque && theme.Value is not ResourceValue.Opaque)
        {
            return consumer.Value == theme.Value
                   && (consumer.Element is null || theme.Element is null || TypeOf(consumer.Element) == TypeOf(theme.Element));
        }

        return consumer.Element is { } mine && theme.Element is { } its
               && !Refers(mine) && !Refers(its)
               && Same(mine, its);
    }

    private static bool Same(XElement a, XElement b)
    {
        if (TypeOf(a) != TypeOf(b))
        {
            return false;
        }

        if (!Attributes(a).SetEquals(Attributes(b)))
        {
            return false;
        }

        if (Text(a) != Text(b))
        {
            return false;
        }

        List<XElement> mine = [.. a.Elements()];
        List<XElement> its = [.. b.Elements()];
        return mine.Count == its.Count && mine.Zip(its).All(pair => Same(pair.First, pair.Second));
    }

    /// <summary>The type an element names: an <c>x:</c> intrinsic and one in <c>System</c> alike, otherwise its namespace and name.</summary>
    private static string TypeOf(XElement element)
    {
        string space = element.Name.NamespaceName;
        bool system = space == XamlNamespace
                      || space == "using:System"
                      || space == "clr-namespace:System"
                      || space.StartsWith("clr-namespace:System;", StringComparison.Ordinal);
        return (system ? "System" : space) + "|" + element.Name.LocalName;
    }

    private static HashSet<string> Attributes(XElement element)
    {
        return new HashSet<string>(
            element.Attributes()
                   .Where(attribute => !attribute.IsNamespaceDeclaration && attribute.Name != Key)
                   .Select(attribute => attribute.Name.NamespaceName + "|" + attribute.Name.LocalName + "=" + attribute.Value.Trim()),
            StringComparer.Ordinal);
    }

    private static string Text(XElement element)
    {
        return string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value)).Trim();
    }

    /// <summary>Whether <paramref name="element"/> or anything in it refers to a resource.</summary>
    private static bool Refers(XElement element)
    {
        return element.DescendantsAndSelf().Any(e => ThemeReads.WrittenOn(e).Any());
    }
}
