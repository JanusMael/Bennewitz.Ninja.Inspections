using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// One definition a consumer writes, by plans/00007's decision 3: its key, the definition, where it is, the variants of
/// the theme dictionaries holding it, and what holds it.
/// </summary>
/// <param name="Key">The key it is written under.</param>
/// <param name="Definition">The definition, as the audit reads a theme's.</param>
/// <param name="Shown">Its file and line as the report writes them: the path below the directory it was read from, with <c>/</c>.</param>
/// <param name="Directory">The directory it was read from, as the configuration names it.</param>
/// <param name="Variants">
/// The display name of the nearest theme dictionary around it, in its own file or along the consumer's files that include
/// it; <c>null</c> where none holds it. A file included under two is read under both.
/// </param>
/// <param name="InSlot">Whether a theme dictionary holds it, in its own file or along the files that include it.</param>
/// <param name="Held">
/// Whether an application holding the consumer's files holds it: not among the resources of a control theme, a style a
/// control theme holds or a template, and not inside a <c>Design.*</c> element.
/// </param>
/// <param name="InApplicationStyles">Whether it is under the resources of an element in an <c>Application</c>'s styles.</param>
internal sealed record ConsumerDefinition(
    string Key,
    ResourceDefinition Definition,
    string Shown,
    string Directory,
    IReadOnlyList<string?> Variants,
    bool InSlot,
    bool Held,
    bool InApplicationStyles);

/// <summary>One reference a consumer writes outside a preview, and the definition of its own key holding it, which it searches past.</summary>
internal sealed record ConsumerReference(string Key, string File, ResourceDefinition? Past);

/// <summary>A consumer's definitions and references, by plans/00007's decision 3.</summary>
internal sealed record ConsumerReading(IReadOnlyList<ConsumerDefinition> Definitions, IReadOnlyList<ConsumerReference> References)
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly string[] IncludeElements = ["ResourceInclude", "MergeResourceInclude"];

    /// <summary>
    /// Reads <paramref name="files"/>, each with the directory it was read from and that directory as the configuration
    /// names it, following the includes among them, by a <c>ResourceInclude</c> or by a class's element, for the variant
    /// of the theme dictionary that includes a file.
    /// </summary>
    /// <exception cref="InvalidDataException">A file is not well-formed XML.</exception>
    public static ConsumerReading Read(IReadOnlyList<(string File, string Directory, string Named)> files)
    {
        List<(string File, string Directory, string Named, XDocument Document)> documents = [.. files.Select(f => (f.File, f.Directory, f.Named, Load(f.File)))];
        Dictionary<string, IReadOnlySet<string?>> variants = VariantsOf(documents);

        List<ConsumerDefinition> definitions = [];
        Dictionary<XElement, ResourceDefinition> byElement = [];
        foreach ((string file, string directory, string named, XDocument document) in documents)
        {
            string shown = Path.GetRelativePath(directory, file).Replace('\\', '/');
            foreach (XElement element in document.Descendants())
            {
                if (element.Attribute(XamlNamespace + "Key") is not { } key || ThemeDefinitionScanner.IsThemeDictionarySlotChild(element))
                {
                    continue;
                }

                int line = ThemeReads.LineOf(element);
                ResourceDefinition definition = new(ThemeDefinitionScanner.ValueOf(element), file, new XElement(element)) { Line = line };
                byElement[element] = definition;

                string? slot = SlotOf(element);
                IReadOnlyList<string?> held = slot is not null ? [slot] : [.. variants[file]];
                definitions.Add(new ConsumerDefinition(key.Value, definition, shown + ":" + line, named, held,
                    InSlot: slot is not null || held.Any(variant => variant is not null),
                    Held: !IsScoped(element) && !ThemeReads.InPreview(element),
                    InApplicationStyles: InApplicationStyles(element)));
            }
        }

        List<ConsumerReference> references = [];
        foreach ((string file, _, _, XDocument document) in documents)
        {
            foreach (XElement element in document.Descendants())
            {
                bool preview = false;
                bool first = true;
                XElement? holder = null;
                foreach ((_, string key) in ThemeReads.WrittenOn(element))
                {
                    if (first)
                    {
                        preview = ThemeReads.InPreview(element);
                        holder = ThemeReads.HolderOf(element);
                        first = false;
                    }

                    if (preview)
                    {
                        break;
                    }

                    ResourceDefinition? past = holder is not null && holder.Attribute(XamlNamespace + "Key")?.Value == key
                        ? byElement.GetValueOrDefault(holder)
                        : null;
                    references.Add(new ConsumerReference(key, file, past));
                }
            }
        }

        return new ConsumerReading(definitions, references);
    }

    private static XDocument Load(string file)
    {
        try
        {
            return XDocument.Load(file, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"{file}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// The variants each file is read under: those of the theme dictionaries that include it, by a <c>ResourceInclude</c>
    /// or a class's element, or of the files that include it outside one; <c>null</c> for a file nothing includes.
    /// </summary>
    private static Dictionary<string, IReadOnlySet<string?>> VariantsOf(IReadOnlyList<(string File, string Directory, string Named, XDocument Document)> documents)
    {
        Dictionary<string, string> byClass = new(StringComparer.Ordinal);
        HashSet<string> ambiguous = new(StringComparer.Ordinal);
        foreach ((string file, _, _, XDocument document) in documents)
        {
            if (document.Root?.Attribute(XamlNamespace + "Class")?.Value is { Length: > 0 } className)
            {
                string simple = className[(className.LastIndexOf('.') + 1)..];
                if (!byClass.TryAdd(simple, file))
                {
                    ambiguous.Add(simple);
                }
            }
        }

        HashSet<string> known = new(documents.Select(d => d.File), StringComparer.Ordinal);
        List<(string From, string To, string? Variant)> edges = [];
        foreach ((string file, string directory, _, XDocument document) in documents)
        {
            foreach (XElement element in document.Descendants())
            {
                if (element.Parent is not { } parent
                    || !(parent.Name.LocalName.EndsWith("MergedDictionaries", StringComparison.Ordinal)
                         || parent.Name.LocalName.EndsWith("ThemeDictionaries", StringComparison.Ordinal)))
                {
                    continue;
                }

                string? target = null;
                if (IncludeElements.Contains(element.Name.LocalName, StringComparer.Ordinal))
                {
                    if (element.Attribute("Source")?.Value.Trim() is { Length: > 0 } source
                        && ResourceIncludeResolver.TryResolveSource(source, file, directory, null, out string resolved, out _))
                    {
                        target = resolved;
                    }
                }
                else if (!ambiguous.Contains(element.Name.LocalName) && byClass.TryGetValue(element.Name.LocalName, out string? classFile))
                {
                    target = classFile;
                }

                if (target is not null && known.Contains(target))
                {
                    string? slot = parent.Name.LocalName.EndsWith("ThemeDictionaries", StringComparison.Ordinal)
                        && element.Attribute(XamlNamespace + "Key")?.Value is { } variantKey
                        ? ThemeVariant.DisplayNameOf(variantKey)
                        : SlotOf(element);
                    edges.Add((file, target, slot));
                }
            }
        }

        Dictionary<string, HashSet<string?>> variants = documents.ToDictionary(d => d.File, _ => new HashSet<string?>(), StringComparer.Ordinal);
        HashSet<string> included = new(edges.Select(edge => edge.To), StringComparer.Ordinal);
        foreach ((string file, _, _, _) in documents.Where(d => !included.Contains(d.File)))
        {
            variants[file].Add(null);
        }

        for (bool changed = true; changed;)
        {
            changed = false;
            foreach ((string from, string to, string? slot) in edges)
            {
                IEnumerable<string?> reached = slot is not null ? [slot] : variants[from];
                foreach (string? variant in reached.ToList())
                {
                    changed |= variants[to].Add(variant);
                }
            }
        }

        // A file only a cycle includes is read as nothing includes it.
        return variants.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlySet<string?>)(kv.Value.Count > 0 ? kv.Value : new HashSet<string?> { null }),
            StringComparer.Ordinal);
    }

    /// <summary>The display name of the nearest theme dictionary around <paramref name="element"/> in its own file, or <c>null</c>.</summary>
    private static string? SlotOf(XElement element)
    {
        foreach (XElement ancestor in element.Ancestors())
        {
            if (ThemeDefinitionScanner.IsThemeDictionarySlotChild(ancestor) && ancestor.Attribute(XamlNamespace + "Key")?.Value is { } key)
            {
                return ThemeVariant.DisplayNameOf(key);
            }
        }

        return null;
    }

    /// <summary>Whether a control theme, a style a control theme holds, or a template holds <paramref name="element"/>, which an application does not.</summary>
    private static bool IsScoped(XElement element)
    {
        foreach (XElement ancestor in element.Ancestors())
        {
            string localName = ancestor.Name.LocalName;
            if (localName == "ControlTheme"
                || (localName.EndsWith("Template", StringComparison.Ordinal) && !localName.Contains('.', StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether <paramref name="element"/> is under the resources of an element in an <c>Application</c>'s styles.</summary>
    private static bool InApplicationStyles(XElement element)
    {
        foreach (XElement ancestor in element.Ancestors())
        {
            if (ancestor.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal)
                && ancestor.Parent?.Parent?.Name.LocalName == "Application.Styles")
            {
                return true;
            }
        }

        return false;
    }
}
