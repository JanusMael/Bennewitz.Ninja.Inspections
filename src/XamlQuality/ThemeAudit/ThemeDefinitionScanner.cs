using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>What a defined resource resolves to, as far as the audit needs.</summary>
public abstract record ResourceValue
{
    /// <summary>
    /// A literal colour: <c>&lt;Color&gt;#..&lt;/Color&gt;</c> or a brush's literal <c>Color</c>.
    /// <paramref name="Opacity"/> is the brush's own <c>Opacity</c> (1 when absent), folded into
    /// the alpha when the colour is scored.
    /// </summary>
    public sealed record ColorLiteral(AuditColor Color, double Opacity = 1.0) : ResourceValue
    {
        /// <summary>
        /// The key of the resource the brush's <c>Opacity</c> names, as either markup extension or a
        /// <c>&lt;StaticResource&gt;</c> property element, in place of a literal <see cref="Opacity"/>;
        /// the colour is scored at the number that resource holds. <c>null</c> when the opacity is a
        /// literal or absent.
        /// </summary>
        public string? OpacityKey { get; init; }
    }

    /// <summary>
    /// An alias to another key: a brush's <c>Color="{DynamicResource X}"</c>, its <c>Color</c> as a
    /// <c>&lt;StaticResource&gt;</c> property element, or <c>ResourceKey="X"</c>.
    /// <paramref name="Opacity"/> is the aliasing brush's own <c>Opacity</c> (1 when absent),
    /// multiplied along the chain when the colour is scored.
    /// </summary>
    public sealed record Alias(string TargetKey, double Opacity = 1.0) : ResourceValue
    {
        /// <summary>The key of the resource the aliasing brush's <c>Opacity</c> names, as <see cref="ColorLiteral.OpacityKey"/>.</summary>
        public string? OpacityKey { get; init; }

        /// <summary>
        /// Whether the alias is a brush's <c>Color</c>, which must name a colour: Avalonia 12.1.3 does
        /// not load a brush whose <c>Color</c> names a brush, directly or through an alias, so the
        /// audit gives such a brush no colour.
        /// </summary>
        public bool FromColor { get; init; }
    }

    /// <summary>A defined key that is not a resolvable colour (a thickness, a gradient, a control theme).
    /// It still counts as defined for the undefined-key finding; it is just not colour-scored.</summary>
    public sealed record Opaque : ResourceValue;
}

/// <summary>
/// One defined resource: the file and line, the theme variant it belongs to (the
/// <c>ThemeDictionaries</c> child's <c>x:Key</c>, or <c>null</c> for a base resource that applies
/// to every variant), the key, and its value.
/// </summary>
public sealed record DefinedResource(string File, string? Variant, string Key, ResourceValue Value, int Line);

/// <summary>
/// Reads the resource keys a theme <em>defines</em>, per variant and with their colour values, so
/// the inventory can be compared against what consumers reference and scored for contrast. The XML
/// is parsed, not line-matched. A key inside a <c>ResourceDictionary.ThemeDictionaries</c> child is
/// attributed to that child's <c>x:Key</c> variant; a key outside one is a base resource
/// (<c>Variant == null</c>).
/// </summary>
public static class ThemeDefinitionScanner
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Scans every AXAML/XAML file under a directory, leaving out <paramref name="excludedDirectories"/>.</summary>
    /// <exception cref="InvalidDataException">A file is not well-formed XML.</exception>
    public static IReadOnlyList<DefinedResource> Scan(string directory, IReadOnlyList<string>? excludedDirectories = null)
    {
        List<DefinedResource> defined = [];
        foreach (string file in XamlFiles.Enumerate(directory, excludedDirectories))
        {
            defined.AddRange(ScanFile(file));
        }

        return defined;
    }

    /// <summary>Scans one AXAML/XAML file — the unit the inventory composes per contributing file.</summary>
    /// <exception cref="InvalidDataException">The file is not well-formed XML.</exception>
    public static IReadOnlyList<DefinedResource> ScanFile(string file)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(file, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"{file}: {ex.Message}", ex);
        }

        List<DefinedResource> defined = [];
        foreach (XElement element in document.Descendants())
        {
            XAttribute? key = element.Attribute(XamlNamespace + "Key");
            if (key is null || IsThemeDictionarySlotChild(element))
            {
                continue;
            }

            int line = ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : 0;
            defined.Add(new DefinedResource(file, VariantOf(element), key.Value, ValueOf(element), line));
        }

        return defined;
    }

    /// <summary>
    /// A direct child of a <c>ThemeDictionaries</c> slot is a variant container or reference — an
    /// inline <c>ResourceDictionary</c>, a <c>ResourceInclude</c>, or a code-behind dictionary
    /// element — not a defined resource, so its <c>x:Key</c> names a variant, not a resource.
    /// </summary>
    internal static bool IsThemeDictionarySlotChild(XElement element)
    {
        return element.Parent is { } parent
               && parent.Name.LocalName.EndsWith("ThemeDictionaries", StringComparison.Ordinal);
    }

    private static string? VariantOf(XElement element)
    {
        foreach (XElement ancestor in element.Ancestors())
        {
            if (ancestor.Name.LocalName == "ResourceDictionary"
                && ancestor.Attribute(XamlNamespace + "Key") is { } variantKey
                && ancestor.Parent is { } parent
                && parent.Name.LocalName.EndsWith("ThemeDictionaries", StringComparison.Ordinal))
            {
                return variantKey.Value;
            }
        }

        return null;
    }

    /// <summary>The value a keyed resource element resolves to (literal colour, alias, or opaque).</summary>
    internal static ResourceValue ValueOf(XElement element)
    {
        string localName = element.Name.LocalName;

        // <StaticResource x:Key="A" ResourceKey="B" /> — an alias.
        if (localName is "StaticResource" or "DynamicResource"
            && element.Attribute("ResourceKey")?.Value is { } aliasTarget && aliasTarget.Length > 0)
        {
            return new ResourceValue.Alias(aliasTarget);
        }

        // <Color x:Key="A">#RRGGBB</Color>
        if (localName == "Color")
        {
            string text = element.Value.Trim();
            return AuditColor.TryParse(text, out AuditColor literal)
                ? new ResourceValue.ColorLiteral(literal)
                : new ResourceValue.Opaque();
        }

        // <SolidColorBrush x:Key="A" Color="#.." />, its Color a literal or a colour's key, and an
        // optional Opacity, a literal or a number's key, that scales the colour as seen.
        if (localName.EndsWith("SolidColorBrush", StringComparison.Ordinal))
        {
            return BrushValue(element);
        }

        return new ResourceValue.Opaque();
    }

    /// <summary>
    /// A brush's <c>Color</c> and <c>Opacity</c>, each written as an attribute, as a whole
    /// <c>{StaticResource}</c> or <c>{DynamicResource}</c>, or as a property element holding a literal
    /// or a <c>&lt;StaticResource&gt;</c>, which are the spellings Avalonia 12.1.3 was measured to
    /// draw. Either written any other way, a binding, a resource inside another extension, or a word
    /// that is no number, leaves the brush unscored: read as opaque, a translucent brush would be
    /// scored as what it does not draw.
    /// </summary>
    private static ResourceValue BrushValue(XElement brush)
    {
        Written opacity = WrittenProperty(brush, "Opacity");
        double literalOpacity = 1.0;
        if (opacity.Literal is { } opacityText)
        {
            if (!double.TryParse(opacityText, NumberStyles.Float, CultureInfo.InvariantCulture, out literalOpacity))
            {
                return new ResourceValue.Opaque();
            }

            literalOpacity = Math.Clamp(literalOpacity, 0, 1);
        }
        else if (opacity.Unreadable)
        {
            return new ResourceValue.Opaque();
        }

        Written color = WrittenProperty(brush, "Color");
        if (color.Literal is { } colorText)
        {
            return AuditColor.TryParse(colorText, out AuditColor literal)
                ? new ResourceValue.ColorLiteral(literal, literalOpacity) { OpacityKey = opacity.Key }
                : new ResourceValue.Opaque();
        }

        return color.Key is { } colorKey
            ? new ResourceValue.Alias(colorKey, literalOpacity) { OpacityKey = opacity.Key, FromColor = true }
            : new ResourceValue.Opaque();
    }

    /// <summary>How a brush writes one of its properties: a literal, a resource's key, unreadable, or not at all.</summary>
    private readonly record struct Written(string? Literal, string? Key, bool Unreadable);

    private static Written WrittenProperty(XElement brush, string property)
    {
        if (brush.Attribute(property)?.Value.Trim() is { } attribute)
        {
            if (!attribute.StartsWith('{'))
            {
                return new Written(attribute, null, false);
            }

            return ResourceReferenceScanner.TryReadWholeReference(attribute, out string key)
                ? new Written(null, key, false)
                : new Written(null, null, true);
        }

        XElement? element = brush.Elements().FirstOrDefault(e => e.Name.LocalName.EndsWith("." + property, StringComparison.Ordinal));
        if (element is null)
        {
            return default;
        }

        List<XElement> held = [.. element.Elements()];
        if (held.Count == 0)
        {
            string text = element.Value.Trim();
            return text.Length > 0 ? new Written(text, null, false) : new Written(null, null, true);
        }

        return held is [{ Name.LocalName: "StaticResource" } reference]
               && reference.Attribute("ResourceKey")?.Value.Trim() is { Length: > 0 } resourceKey
            ? new Written(null, resourceKey, false)
            : new Written(null, null, true);
    }
}
