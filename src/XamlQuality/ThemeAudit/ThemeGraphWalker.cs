using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// A variant discovered while walking a theme: its raw key, readable name, and the XML namespace
/// declarations the raw key depends on (<c>{x:Static semi:SemiTheme.Aquatic}</c> needs <c>x</c>
/// and <c>semi</c>), so a generated dictionary can declare the same key.
/// </summary>
internal sealed record VariantId(string Key, string DisplayName, IReadOnlyDictionary<string, string> Namespaces);

/// <summary>
/// The raw material a <see cref="ThemeInventory"/> is built from: the variants the theme declares, the
/// dictionary its entry is and each one the configuration merges after it, any include or code-behind
/// reference that could not be resolved, and every file walked; and, by plans/00007, the theme's own reading:
/// every reference the entry's walk met outside a preview, the files it reached, and the definitions a control
/// theme, a style or a template holds.
/// </summary>
internal sealed record WalkResult(
    IReadOnlyList<VariantId> Variants,
    ResourceNode Entry,
    IReadOnlyList<ResourceNode> Merges,
    IReadOnlyList<UnresolvedInclude> Unresolved,
    IReadOnlyList<string> Files,
    IReadOnlyList<ThemeRead> Reads,
    IReadOnlySet<string> EntryFiles,
    IReadOnlySet<ResourceDefinition> Scoped);

/// <summary>
/// One dictionary as Avalonia 12.1.3 holds it, which its lookup, <c>ResourceDictionary.TryGetResource</c>,
/// searches in this order: its own entries; its theme dictionary for the variant asked, those that variant
/// inherits, then <c>Default</c>'s; its merged dictionaries, the last first; and, for a <c>Styles</c>, the
/// files its <c>StyleInclude</c>s name, the last first.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A node keeps where it was loaded, which a flattened map cannot.</b> A <c>StaticResource</c> alias
/// searches the dictionaries from the one holding it out to its root, then the application, by plans/00006;
/// <see cref="Parent"/> is that chain. A dictionary's own elements that are not dictionaries, a
/// <c>ControlTheme</c>'s or a <c>Style</c>'s resources among them, are read as its own entries.
/// </para>
/// <para>
/// ⚠ <b>A dictionary with a class of its own, entered by its element, is a root, and is built at once.</b>
/// Measured headless on 2026-10-20: an alias in it does not see the dictionary that holds it, and resolves
/// while the class is constructed, not at its first lookup, when the application holds only what it held
/// before the theme was built.
/// </para>
/// </remarks>
internal sealed class ResourceNode(ResourceNode? parent, string? variant, bool builtAtOnce)
{
    /// <summary>The dictionary around this one in its root's chain; <c>null</c> for a root.</summary>
    public ResourceNode? Parent { get; } = parent;

    /// <summary>The display name of the theme dictionary this one is, which an alias in it resolves in; <c>null</c> for none.</summary>
    public string? Variant { get; } = variant;

    /// <summary>Whether this root is a dictionary with a class of its own, whose aliases resolve when it is constructed.</summary>
    public bool BuiltAtOnce { get; } = builtAtOnce;

    /// <summary>The keys it defines itself, the last of one key in it winning.</summary>
    public Dictionary<string, ResourceDefinition> Own { get; } = new(StringComparer.Ordinal);

    /// <summary>Its theme dictionaries, by variant display name, the first of a name declared winning.</summary>
    public Dictionary<string, ResourceNode> Themes { get; } = new(StringComparer.Ordinal);

    /// <summary>Its merged dictionaries, in the order declared; searched last to first.</summary>
    public List<ResourceNode> Merged { get; } = [];

    /// <summary>The files its <c>StyleInclude</c>s name, in the order declared; searched after <see cref="Merged"/>, last to first.</summary>
    public List<ResourceNode> Styles { get; } = [];

    /// <summary>
    /// How it was reached from the root it was loaded in, hop by hop, each joined by <c>&gt;</c>: <c>entry</c> or
    /// <c>merge</c> for a root, then for each dictionary the hop that built it, an inline <c>ResourceDictionary</c>, a
    /// <c>ResourceInclude</c>, a <c>MergeResourceInclude</c>, a <c>StyleInclude</c>, a <c>class</c>'s element or a
    /// <c>provider</c>, prefixed <c>theme-</c> where it is keyed as a theme dictionary. A dictionary with a class of its own
    /// continues the route of the one whose element entered it, though it is a root of its own.
    /// </summary>
    public string Route { get; init; } = "entry";

    /// <summary>The references its own entries and elements write, by plans/00007; a merged file's are not the theme's.</summary>
    public List<ThemeRead> Reads { get; } = [];

    /// <summary>The root of this node's chain.</summary>
    public ResourceNode Root
    {
        get
        {
            ResourceNode node = this;
            while (node.Parent is not null)
            {
                node = node.Parent;
            }

            return node;
        }
    }

    /// <summary>The variant an alias in this node resolves in: the nearest theme dictionary's on its chain, else <c>null</c>.</summary>
    public string? ResolutionVariant
    {
        get
        {
            for (ResourceNode? node = this; node is not null; node = node.Parent)
            {
                if (node.Variant is not null)
                {
                    return node.Variant;
                }
            }

            return null;
        }
    }
}

/// <summary>
/// Walks a theme's resource graph into the dictionaries Avalonia builds from it, carrying a variant
/// context, so a key is attributed to the variant whose <c>ThemeDictionaries</c> slot it was reached
/// through — even when that slot is in a file included lower down (Semi selects its Light/Dark palettes
/// inside a shared <c>Tokens/_index</c>, not the entry). Follows <c>ResourceInclude</c>,
/// <c>StyleInclude</c> (Fluent and Simple reach their control themes that way), inline dictionaries, and
/// code-behind dictionary elements resolved by <see cref="XClassIndex"/> or supplied by a declared
/// provider (a <c>ResourceProvider</c> implemented in code, such as Fluent's accent colours). The entry
/// and each merged file are roots of their own, as an application holds them.
/// </summary>
internal static class ThemeGraphWalker
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly string[] IncludeElements = ["ResourceInclude", "MergeResourceInclude", "StyleInclude"];

    public static WalkResult Walk(ThemeSource source)
    {
        string root = Path.GetFullPath(source.BaseDirectory);
        XClassIndex classIndex = XClassIndex.Build(root);

        Walker walker = new(root, source, classIndex);
        ResourceNode entry = walker.WalkRoot(Path.GetFullPath(source.EntryFile), "entry");
        List<ResourceNode> merges = [.. (source.Merge ?? []).Select(merged => walker.WalkRoot(Path.GetFullPath(merged), "merge"))];
        return walker.ToResult(entry, merges);
    }

    private sealed class Walker(string root, ThemeSource source, XClassIndex classIndex)
    {
        private readonly Dictionary<string, VariantId> _variants = new(StringComparer.Ordinal);
        private readonly List<UnresolvedInclude> _unresolved = [];
        private readonly List<string> _files = [];
        private readonly Dictionary<string, (XDocument? Document, string? Failure)> _documents = new(StringComparer.Ordinal);
        private readonly Dictionary<(XElement Element, string Context), ResourceDefinition> _definitions = [];
        private readonly HashSet<string> _walked = new(StringComparer.Ordinal);
        private readonly List<string> _chain = [];
        private readonly List<ThemeRead> _reads = [];
        private readonly HashSet<string> _entryFiles = new(StringComparer.Ordinal);
        private readonly HashSet<ResourceDefinition> _scoped = new(ReferenceEqualityComparer.Instance);
        private bool _inEntry;
        private int _quiet;

        /// <summary>
        /// A file walked as a root of its own, as an application holds a theme or a dictionary it loads; the theme's own
        /// reading is taken from the <c>entry</c>'s walk alone, since a merged file is the configuration's.
        /// </summary>
        public ResourceNode WalkRoot(string file, string route)
        {
            ResourceNode node = new(parent: null, variant: null, builtAtOnce: false) { Route = route };
            _inEntry = route == "entry";
            WalkFile(file, node, context: null);
            _inEntry = false;
            return node;
        }

        /// <summary>
        /// Walks <paramref name="file"/> into <paramref name="node"/>, the dictionary it is. A file is walked again
        /// wherever it is included, since each include builds a dictionary of its own; what it cannot resolve is
        /// reported once for each variant it is walked under, and a file that includes itself stops there.
        /// </summary>
        private void WalkFile(string file, ResourceNode node, VariantId? context)
        {
            if (_chain.Contains(file, StringComparer.Ordinal))
            {
                return;
            }

            bool repeated = !_walked.Add(file + "\0" + (context?.Key ?? string.Empty));
            _quiet += repeated ? 1 : 0;
            try
            {
                (XDocument? document, string? failure) = Load(file);
                if (failure is not null)
                {
                    Report(new UnresolvedInclude(file, file, failure));
                }

                if (document?.Root is { } documentRoot)
                {
                    if (_inEntry)
                    {
                        _entryFiles.Add(file);
                    }

                    _chain.Add(file);
                    Process(documentRoot, file, node, context);
                    _chain.RemoveAt(_chain.Count - 1);
                }
            }
            finally
            {
                _quiet -= repeated ? 1 : 0;
            }
        }

        private (XDocument? Document, string? Failure) Load(string file)
        {
            if (_documents.TryGetValue(file, out (XDocument? Document, string? Failure) loaded))
            {
                return loaded;
            }

            if (!File.Exists(file))
            {
                loaded = (null, "file not found");
            }
            else
            {
                try
                {
                    loaded = (XDocument.Load(file, LoadOptions.SetLineInfo), null);
                    _files.Add(file);
                }
                catch (XmlException ex)
                {
                    loaded = (null, $"not well-formed XML: {ex.Message}");
                }
            }

            _documents[file] = loaded;
            return loaded;
        }

        private void Report(UnresolvedInclude unresolved)
        {
            if (_quiet == 0)
            {
                _unresolved.Add(unresolved);
            }
        }

        private void Process(XElement element, string file, ResourceNode node, VariantId? context)
        {
            foreach (XElement child in element.Elements())
            {
                string localName = child.Name.LocalName;

                if (localName.EndsWith("ThemeDictionaries", StringComparison.Ordinal))
                {
                    foreach (XElement slotChild in child.Elements())
                    {
                        XAttribute? key = slotChild.Attribute(XamlNamespace + "Key");
                        if (key is null)
                        {
                            continue;
                        }

                        VariantId variant = Register(key.Value, slotChild);
                        if (Descend(slotChild, file, node, variant, variant.DisplayName) is { } slot)
                        {
                            node.Themes.TryAdd(variant.DisplayName, slot);
                        }
                    }
                }
                else if (localName.EndsWith("MergedDictionaries", StringComparison.Ordinal))
                {
                    foreach (XElement slotChild in child.Elements())
                    {
                        if (Descend(slotChild, file, node, context, variant: null) is { } merged)
                        {
                            node.Merged.Add(merged);
                        }
                    }
                }
                else if (localName == "StyleInclude")
                {
                    if (Include(child, file, node, context, variant: null, hop: "StyleInclude") is { } included)
                    {
                        node.Styles.Add(included);
                    }
                }
                else
                {
                    if (child.Attribute(XamlNamespace + "Key") is { } keyed)
                    {
                        node.Own[keyed.Value] = DefinitionOf(child, file, context);
                    }

                    Record(child, file, node, context);
                    Process(child, file, node, context);
                }
            }
        }

        /// <summary>
        /// Records each reference <paramref name="element"/> itself writes, in the entry's walk and outside a preview, with
        /// the keyed entry holding it as this walk defined it.
        /// </summary>
        private void Record(XElement element, string file, ResourceNode node, VariantId? context)
        {
            if (!_inEntry)
            {
                return;
            }

            bool read = false;
            XElement? holder = null;
            foreach ((bool dynamic, string key) in ThemeReads.WrittenOn(element))
            {
                if (!read)
                {
                    if (ThemeReads.InPreview(element))
                    {
                        return;
                    }

                    holder = ThemeReads.HolderOf(element);
                    read = true;
                }

                ResourceDefinition? held = holder is null ? null : _definitions.GetValueOrDefault((holder, context?.Key ?? string.Empty));
                ThemeRead reference = new(key, dynamic, file, ThemeReads.LineOf(element), node, held, ThemeReads.HoldsOf(element, holder));
                node.Reads.Add(reference);
                _reads.Add(reference);
            }
        }

        /// <summary>
        /// The dictionary a <c>ThemeDictionaries</c> or <c>MergedDictionaries</c> child builds, inside
        /// <paramref name="parent"/>; <paramref name="variant"/> is the theme dictionary's display name, for a slot.
        /// </summary>
        private ResourceNode? Descend(XElement element, string includingFile, ResourceNode parent, VariantId? context, string? variant)
        {
            string localName = element.Name.LocalName;
            string slot = variant is null ? string.Empty : "theme-";

            if (IncludeElements.Contains(localName, StringComparer.Ordinal))
            {
                return Include(element, includingFile, parent, context, variant, slot + localName);
            }

            if (localName == "ResourceDictionary")
            {
                ResourceNode inline = new(parent, variant, builtAtOnce: false) { Route = parent.Route + ">" + slot + "inline" };
                Process(element, includingFile, inline, context); // inline dictionary; its keys take this context
                return inline;
            }

            if (classIndex.TryResolve(localName, out string file, out string reason))
            {
                // A code-behind dictionary element (e.g. <semi:Light/>), by x:Class: constructed with new, it is a root
                // of its own, and a dictionary built at once.
                bool dictionary = Load(file).Document?.Root?.Name.LocalName == "ResourceDictionary";
                ResourceNode own = new(parent: null, variant: null, builtAtOnce: dictionary) { Route = parent.Route + ">" + slot + "class" };
                WalkFile(file, own, context);
                return own;
            }

            if (source.Providers is not null && source.Providers.TryGetValue(localName, out IReadOnlyDictionary<string, string?>? provided))
            {
                ResourceNode supplied = new(parent, variant, builtAtOnce: false) { Route = parent.Route + ">" + slot + "provider" };
                foreach ((string key, string? literal) in provided)
                {
                    supplied.Own[key] = new ResourceDefinition(ProvidedValue(literal), null, null);
                }

                return supplied;
            }

            Report(new UnresolvedInclude(includingFile, localName, reason));
            return null;
        }

        /// <summary>The dictionary an include builds inside <paramref name="parent"/>, whose chain it continues, reached by <paramref name="hop"/>.</summary>
        private ResourceNode? Include(XElement element, string includingFile, ResourceNode parent, VariantId? context, string? variant, string hop)
        {
            string? includeSource = element.Attribute("Source")?.Value;
            if (string.IsNullOrWhiteSpace(includeSource))
            {
                return null;
            }

            if (ResourceIncludeResolver.TryResolveSource(includeSource.Trim(), includingFile, root, source.AssemblyName,
                                                         out string resolved, out string reason, source.Links))
            {
                ResourceNode included = new(parent, variant, builtAtOnce: false) { Route = parent.Route + ">" + hop };
                WalkFile(resolved, included, context);
                return included;
            }

            Report(new UnresolvedInclude(includingFile, includeSource.Trim(), reason));
            return null;
        }

        /// <summary>
        /// One definition for each element and variant it is walked under, however often its file is included, so
        /// that a key two variants reach through one walk is one definition to both, and through two walks two.
        /// </summary>
        private ResourceDefinition DefinitionOf(XElement element, string file, VariantId? context)
        {
            if (!_definitions.TryGetValue((element, context?.Key ?? string.Empty), out ResourceDefinition? definition))
            {
                definition = new ResourceDefinition(ThemeDefinitionScanner.ValueOf(element), file, new XElement(element))
                {
                    Line = ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : 0,
                };
                _definitions[(element, context?.Key ?? string.Empty)] = definition;
                if (ThemeReads.IsScoped(element))
                {
                    _scoped.Add(definition);
                }
            }

            return definition;
        }

        /// <summary>A provider's declared value: a colour literal, an alias to another key, or opaque.</summary>
        private static ResourceValue ProvidedValue(string? literal)
        {
            if (string.IsNullOrWhiteSpace(literal))
            {
                return new ResourceValue.Opaque();
            }

            return AuditColor.TryParse(literal, out AuditColor color)
                ? new ResourceValue.ColorLiteral(color)
                : new ResourceValue.Alias(literal.Trim());
        }

        private VariantId Register(string key, XElement declaringElement)
        {
            if (!_variants.TryGetValue(key, out VariantId? variant))
            {
                variant = new VariantId(key, ThemeVariant.DisplayNameOf(key), NamespacesOf(key, declaringElement));
                _variants[key] = variant;
            }

            return variant;
        }

        /// <summary>
        /// The namespace declarations a raw variant key needs: every <c>prefix:</c> inside a
        /// markup-extension key, resolved through the declaring element's scope.
        /// </summary>
        private static IReadOnlyDictionary<string, string> NamespacesOf(string key, XElement element)
        {
            Dictionary<string, string> namespaces = new(StringComparer.Ordinal);
            if (!key.StartsWith('{'))
            {
                return namespaces;
            }

            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] != ':')
                {
                    continue;
                }

                int start = i;
                while (start > 0 && (char.IsLetterOrDigit(key[start - 1]) || key[start - 1] == '_'))
                {
                    start--;
                }

                if (start == i)
                {
                    continue;
                }

                string prefix = key[start..i];
                XNamespace? ns = element.GetNamespaceOfPrefix(prefix);
                if (ns is not null && !namespaces.ContainsKey(prefix))
                {
                    namespaces[prefix] = ns.NamespaceName;
                }
            }

            return namespaces;
        }

        public WalkResult ToResult(ResourceNode entry, IReadOnlyList<ResourceNode> merges)
        {
            return new WalkResult([.. _variants.Values], entry, merges, _unresolved, _files, _reads, _entryFiles, _scoped);
        }
    }
}
