using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>What holds a reference innermost, up to the keyed entry around it, by plans/00007.</summary>
internal enum ReadHolder
{
    /// <summary>A <c>Setter</c>, its value or what it holds.</summary>
    Setter,

    /// <summary>A template: a <c>ControlTemplate</c>, a <c>DataTemplate</c> or another element named <c>…Template</c>.</summary>
    Template,

    /// <summary>A <c>Style</c>'s own attributes, or what it holds outside a setter or a template.</summary>
    Style,

    /// <summary>A <c>&lt;StaticResource&gt;</c> or <c>&lt;DynamicResource&gt;</c> alias element.</summary>
    Alias,

    /// <summary>A <c>SolidColorBrush</c>, its <c>Color</c> or its <c>Opacity</c>.</summary>
    Brush,

    /// <summary>Another keyed entry, its own attributes or what it holds, as a control theme's <c>BasedOn</c>.</summary>
    Entry,

    /// <summary>Nothing above: no keyed entry, setter, template or style holds it.</summary>
    Other,
}

/// <summary>
/// One reference in a theme's files, as <see cref="ResourceReferenceScanner"/> reads a consumer's, in the dictionary the
/// walk built around it: its key, whether it is dynamic, where it is written, the keyed entry holding it, and its kind.
/// </summary>
/// <param name="Key">The key it names.</param>
/// <param name="Dynamic">Whether it is a <c>DynamicResource</c>.</param>
/// <param name="File">The file it is written in.</param>
/// <param name="Line">The line its element starts on.</param>
/// <param name="Node">The dictionary the walk built around it.</param>
/// <param name="Holder">The keyed entry holding it, itself included, as the walk defined it; <c>null</c> for none.</param>
/// <param name="Holds">What holds it innermost.</param>
internal sealed record ThemeRead(string Key, bool Dynamic, string File, int Line, ResourceNode Node, ResourceDefinition? Holder, ReadHolder Holds)
{
    /// <summary>
    /// Its kind, by plans/00007's decision 3: what holds it innermost, with the whole chain of dictionaries that reaches its
    /// file from the root it was loaded in, as <see cref="ResourceNode.Route"/> writes it.
    /// </summary>
    public string Kind => Holds.ToString().ToLowerInvariant() + " " + Node.Route;
}

/// <summary>
/// A theme's own reading, by plans/00007: every reference its entry's walk met outside a preview, what that walk left
/// unresolved, which a file the configuration merges does not add to, the definitions a control theme, a style or a
/// template holds, the dictionary its entry is, and its base directory, below which the report writes its files.
/// </summary>
internal sealed record ThemeReading(
    IReadOnlyList<ThemeRead> Reads,
    IReadOnlyList<UnresolvedInclude> Unresolved,
    IReadOnlySet<ResourceDefinition> Scoped,
    ResourceNode Entry,
    string BaseDirectory);

/// <summary>Where a theme's read finds its key, by plans/00006's lookup.</summary>
internal enum ReadReach
{
    /// <summary>A dynamic read, which searches the application under the variant asked.</summary>
    Dynamic,

    /// <summary>A static read, found on its own chain, from its dictionary out to its root.</summary>
    Chain,

    /// <summary>A static read in a dictionary with a class of its own, whose chain lacks the key: it resolves when built.</summary>
    BuiltAtOnce,

    /// <summary>A static read whose chain lacks the key, which searches the application in its own variant.</summary>
    Application,
}

/// <summary>How the walk reads the references an element writes, and what holds them.</summary>
internal static class ThemeReads
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Each reference <paramref name="element"/> itself writes: its element form, then each attribute's, in order.</summary>
    public static IEnumerable<(bool Dynamic, string Key)> WrittenOn(XElement element)
    {
        string localName = element.Name.LocalName;
        if (localName is "StaticResource" or "DynamicResource"
            && element.Attribute("ResourceKey")?.Value.Trim() is { Length: > 0 } resourceKey)
        {
            yield return (localName == "DynamicResource", resourceKey);
        }

        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration || attribute.Name == XamlNamespace + "Key")
            {
                continue;
            }

            foreach ((ReferenceKind kind, string key) in ResourceReferenceScanner.ExtractMarkupReferences(attribute.Value))
            {
                yield return (kind == ReferenceKind.Dynamic, key);
            }
        }
    }

    /// <summary>The innermost keyed element holding <paramref name="element"/>, itself included, inside its dictionary; <c>null</c> for none.</summary>
    public static XElement? HolderOf(XElement element)
    {
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            string localName = current.Name.LocalName;
            if (localName.EndsWith("ThemeDictionaries", StringComparison.Ordinal)
                || localName.EndsWith("MergedDictionaries", StringComparison.Ordinal))
            {
                return null;
            }

            if (current.Attribute(XamlNamespace + "Key") is not null && !ThemeDefinitionScanner.IsThemeDictionarySlotChild(current))
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>What holds a reference on <paramref name="element"/> innermost, up to and including <paramref name="holder"/>.</summary>
    public static ReadHolder HoldsOf(XElement element, XElement? holder)
    {
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            string localName = current.Name.LocalName;
            if (IsTemplate(localName))
            {
                return ReadHolder.Template;
            }

            if (localName == "Setter")
            {
                return ReadHolder.Setter;
            }

            if (localName == "Style")
            {
                return ReadHolder.Style;
            }

            if (current == holder)
            {
                break;
            }
        }

        return holder?.Name.LocalName switch
        {
            null => ReadHolder.Other,
            "StaticResource" or "DynamicResource" => ReadHolder.Alias,
            { } name when name.EndsWith("SolidColorBrush", StringComparison.Ordinal) => ReadHolder.Brush,
            _ => ReadHolder.Entry,
        };
    }

    /// <summary>Whether <paramref name="element"/> is a preview, or inside one: a <c>Design.*</c> element, whose controls no application draws.</summary>
    public static bool InPreview(XElement element)
    {
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            if (current.Name.LocalName.StartsWith("Design.", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a keyed <paramref name="element"/> is among the resources a control theme, a style or a template holds, which
    /// Avalonia scopes to what they style and the walk reads as their dictionary's own.
    /// </summary>
    public static bool IsScoped(XElement element)
    {
        foreach (XElement ancestor in element.Ancestors())
        {
            string localName = ancestor.Name.LocalName;
            if (localName is "ControlTheme" or "Style" || IsTemplate(localName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The line <paramref name="element"/> starts on, or 0 where the document kept none.</summary>
    public static int LineOf(XElement element)
    {
        return ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : 0;
    }

    /// <summary>A template element: a name ending in <c>Template</c> that is no property element.</summary>
    private static bool IsTemplate(string localName)
    {
        return localName.EndsWith("Template", StringComparison.Ordinal) && !localName.Contains('.', StringComparison.Ordinal);
    }
}
