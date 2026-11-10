using System.Collections.Concurrent;
using System.Globalization;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// One defined resource as the inventory holds it: its analysed value, the file that defined it
/// (<c>null</c> when a code provider supplied it), and a detached copy of the defining element so
/// a generator can carry the definition verbatim into another dictionary.
/// </summary>
public sealed record ResourceDefinition(ResourceValue Value, string? File, XElement? Element)
{
    /// <summary>The line of <see cref="File"/> the defining element starts on; 0 when a code provider supplied it.</summary>
    public int Line { get; init; }
}

/// <summary>
/// Where a theme comes from and what the walker needs to read it completely: the entry file and
/// the assembly's <c>avares</c> base directory, plus the optional facts a checkout does not carry.
/// </summary>
/// <param name="EntryFile">The theme's entry — Semi's <c>Index.axaml</c>, Fluent's <c>FluentTheme.xaml</c>.</param>
/// <param name="BaseDirectory">The assembly's <c>avares</c> base, where a <c>/</c>-rooted include resolves.</param>
public sealed record ThemeSource(string EntryFile, string BaseDirectory)
{
    /// <summary>The theme's assembly, so its own <c>avares://Assembly/…</c> includes resolve.</summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// Variant display name → the display name of the variant it inherits from. Semi's Desert
    /// inherits Light and its Aquatic/Dusk/NightSky inherit Dark, so a key defined only on the
    /// parent still counts as defined on the child, exactly as Avalonia resolves it.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Inheritance { get; init; }

    /// <summary>
    /// Resource path (<c>/Strings/InvariantResources.xaml</c>, or a full <c>avares://</c> URI)
    /// → the file that supplies it, for a file the project links in from outside its directory.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Links { get; init; }

    /// <summary>
    /// Code-behind element name → the keys it supplies, each with a colour literal, an alias key,
    /// or <c>null</c> when the value is not a colour. Declares a <c>ResourceProvider</c> written in
    /// code (Fluent's <c>SystemAccentColors</c>) that no AXAML file can stand in for.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string?>>? Providers { get; init; }

    /// <summary>
    /// Further dictionaries an application holds in its resources while the theme is in its styles — a
    /// compat dictionary, a host's own overrides. Each is a root of its own, and the application's lookup
    /// searches them before the theme, the last listed first.
    /// </summary>
    public IReadOnlyList<string>? Merge { get; init; }
}

/// <summary>
/// One theme variant's resources as an application holding the theme finds them under that variant: every
/// key its lookup reaches, in the files the configuration merges and then the theme, each dictionary searched
/// for its own entries, its theme dictionary for the variant, those the variant inherits and <c>Default</c>'s,
/// then its merged dictionaries. A key resolves to a colour when it is a literal or an alias chain that ends
/// in one, each alias followed where Avalonia follows it, with brush opacities folded into the alpha; a
/// thickness, a gradient or an alias Avalonia cannot resolve is defined but not colour-scored.
/// </summary>
public sealed class VariantInventory
{
    private readonly ResourceApplication _application;
    private readonly IReadOnlyDictionary<string, Found> _effective;

    internal VariantInventory(string key, string displayName, IReadOnlyDictionary<string, string> keyNamespaces,
                              ResourceApplication application)
    {
        Key = key;
        DisplayName = displayName;
        KeyNamespaces = keyNamespaces;
        _application = application;
        _effective = application.Flatten(displayName);
    }

    /// <summary>The raw variant key (e.g. <c>Dark</c> or <c>{x:Static semi:SemiTheme.NightSky}</c>).</summary>
    public string Key { get; }

    /// <summary>The readable variant name (e.g. <c>NightSky</c>).</summary>
    public string DisplayName { get; }

    /// <summary>The XML namespace declarations (prefix → namespace) the raw <see cref="Key"/> depends on.</summary>
    public IReadOnlyDictionary<string, string> KeyNamespaces { get; }

    /// <summary>Every key the application's lookup finds under this variant.</summary>
    public IReadOnlyCollection<string> Keys => (IReadOnlyCollection<string>)_effective.Keys;

    /// <summary>Whether this variant defines <paramref name="key"/> at all.</summary>
    public bool Defines(string key) => _effective.ContainsKey(key);

    /// <summary>The definition the application's lookup finds first for <paramref name="key"/> under this variant, or <c>null</c> when undefined.</summary>
    public ResourceDefinition? Lookup(string key)
    {
        return _effective.TryGetValue(key, out Found found) ? found.Definition : null;
    }

    /// <summary>
    /// The colour <paramref name="key"/> resolves to under this variant, as Avalonia draws it: an alias chain
    /// followed to its literal where Avalonia 12.1.3 follows it, and the alpha scaled by every brush opacity on
    /// the way, an opacity a brush takes from a resource being the number that resource holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The key is looked up as the application looks it up, by plans/00006. A <c>StaticResource</c> alias then
    /// searches its own dictionaries, from the one holding it out to the root it was loaded in, then the
    /// application, in the variant of the nearest theme dictionary around it, or in <c>Default</c> alone where
    /// there is none. A <c>DynamicResource</c> one, whose dictionary's owner is the application, searches the
    /// application under this variant. An alias does not find itself, nor one it is being resolved for, so one
    /// that names its own key searches past its own entry.
    /// </para>
    /// <para>
    /// <c>null</c> when the key is undefined or opaque; when an alias's target is found nowhere it searches; when
    /// an alias in a dictionary with a class of its own, entered by its element, is not found in that dictionary,
    /// since it resolves while the dictionary is constructed; when an opacity's resource holds no number; and when a
    /// brush's <c>Color</c> names a brush, directly or through an alias, which Avalonia 12.1.3 does not load.
    /// </para>
    /// </remarks>
    public AuditColor? Resolve(string key)
    {
        return Resolve(key, 1.0);
    }

    /// <summary>
    /// The colour <paramref name="key"/> resolves to as <see cref="Resolve(string)"/> resolves it, drawn inside
    /// elements whose opacities multiply to <paramref name="opacity"/>: the brush's own opacity times
    /// <paramref name="opacity"/>, truncated once, as Avalonia's Skia renderer multiplies a brush's opacity by the
    /// opacity its elements push before it makes the alpha.
    /// </summary>
    internal AuditColor? Resolve(string key, double opacity)
    {
        return _effective.TryGetValue(key, out Found found) ? _application.Colour(found, DisplayName, opacity) : null;
    }

    /// <summary>
    /// <paramref name="color"/> at <paramref name="opacity"/>, as Avalonia's Skia renderer paints a
    /// solid brush at 12.1.3 (<c>DrawingContextImpl.CreatePaint</c>): the alpha times the opacity,
    /// truncated to a whole byte once.
    /// </summary>
    internal static AuditColor WithOpacity(AuditColor color, double opacity)
    {
        return opacity >= 1.0 ? color : color with { A = (byte)(color.A * opacity) };
    }
}

/// <summary>
/// The per-variant inventory of a theme: which keys each variant defines and what colour each
/// resolves to, built by <see cref="ThemeGraphWalker"/> walking the resource graph into the
/// dictionaries Avalonia builds from it, and looked up as an application holding the theme in its
/// styles, and the files the configuration merges in its resources, looks a key up.
/// </summary>
public sealed class ThemeInventory
{
    /// <summary>The <c>ThemeDictionaries</c> key Avalonia falls back to when a variant lacks a key.</summary>
    public const string DefaultVariant = "Default";

    private readonly IReadOnlyDictionary<string, VariantInventory> _byDisplayName;
    private readonly IReadOnlyDictionary<string, string>? _inheritance;
    private readonly VariantInventory? _baseOnly;

    private ThemeInventory(IReadOnlyList<VariantInventory> variants, VariantInventory? baseOnly,
                           IReadOnlyList<UnresolvedInclude> unresolved, IReadOnlyList<string> files,
                           IReadOnlyDictionary<string, string>? inheritance, ResourceApplication application, ThemeReading reading)
    {
        Variants = variants;
        Unresolved = unresolved;
        Files = files;
        Application = application;
        Reading = reading;
        _inheritance = inheritance;
        _baseOnly = baseOnly;
        _byDisplayName = variants
            .GroupBy(v => v.DisplayName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    /// <summary>
    /// The theme's variants, in the order they are first declared. A dictionary that declares no
    /// <c>ThemeDictionaries</c> at all has exactly one, <c>Default</c>, holding its keys.
    /// </summary>
    public IReadOnlyList<VariantInventory> Variants { get; }

    /// <summary>Every include or code-behind reference the walker could not resolve.</summary>
    public IReadOnlyList<UnresolvedInclude> Unresolved { get; }

    /// <summary>Every file the walker read, in walk order — the material a digest is taken over.</summary>
    public IReadOnlyList<string> Files { get; }

    /// <summary>The application holding the theme, and the files merged into it, as its lookup searches them.</summary>
    internal ResourceApplication Application { get; }

    /// <summary>The theme's own reading, by plans/00007: what its entry's walk reads, reaches and scopes.</summary>
    internal ThemeReading Reading { get; }

    /// <summary>
    /// The variant that applies when an application requests <paramref name="displayName"/>:
    /// the exact variant, else the nearest declared ancestor along the inheritance map, else
    /// <c>Default</c> — Avalonia's own fallback — else the keys the theme defines outside any
    /// variant, which Avalonia serves whatever variant is active. Fluent and Simple declare only
    /// <c>Default</c> and <c>Dark</c>, so their <c>Light</c> is <c>Default</c>.
    /// </summary>
    public VariantInventory ForVariant(string displayName)
    {
        HashSet<string> visiting = new(StringComparer.Ordinal);
        string? current = displayName;
        while (current is not null && visiting.Add(current))
        {
            if (_byDisplayName.TryGetValue(current, out VariantInventory? variant))
            {
                return variant;
            }

            current = _inheritance is not null && _inheritance.TryGetValue(current, out string? parent) ? parent : null;
        }

        return _byDisplayName.TryGetValue(DefaultVariant, out VariantInventory? fallback) ? fallback : _baseOnly!;
    }

    /// <summary>The variant <paramref name="displayName"/> inherits from, per the inheritance map, or <c>null</c>.</summary>
    public string? ParentOf(string displayName)
    {
        return _inheritance is not null && _inheritance.TryGetValue(displayName, out string? parent) ? parent : null;
    }

    /// <summary>
    /// Builds the inventory for the theme rooted at <paramref name="entryFile"/>.
    /// <paramref name="baseDirectory"/> is the assembly's <c>avares</c> base and
    /// <paramref name="assemblyName"/> its assembly (so its own <c>avares://</c> includes resolve).
    /// <paramref name="inheritance"/> maps a variant's display name to the display name of the
    /// variant it inherits from. See <see cref="ThemeSource"/> for the full set of inputs.
    /// </summary>
    public static ThemeInventory Build(
        string entryFile,
        string baseDirectory,
        string? assemblyName = null,
        IReadOnlyDictionary<string, string>? inheritance = null)
    {
        return Build(new ThemeSource(entryFile, baseDirectory) { AssemblyName = assemblyName, Inheritance = inheritance });
    }

    /// <summary>Builds the inventory for <paramref name="source"/>.</summary>
    public static ThemeInventory Build(ThemeSource source)
    {
        WalkResult walk = ThemeGraphWalker.Walk(source);
        IReadOnlyDictionary<string, string>? inheritance = source.Inheritance;

        // The application searches its resources, the merged files, the last listed first, before its styles,
        // the theme.
        ResourceApplication application = new([.. Enumerable.Reverse(walk.Merges), walk.Entry], inheritance);

        // A dictionary with no ThemeDictionaries still has its keys: they become its one variant,
        // Default. A theme that declares variants but no Default keeps its base keys reachable
        // through ForVariant's last fallback, without adding a variant it never declared.
        List<VariantId> variants = [.. walk.Variants];
        VariantId baseOnly = new(DefaultVariant, DefaultVariant, new Dictionary<string, string>(StringComparer.Ordinal));
        if (variants.Count == 0)
        {
            variants.Add(baseOnly);
        }

        // Each variant is what the application's lookup finds under it: a variant's own theme dictionary, then
        // those it inherits, by display name, then Default's, in each dictionary in turn.
        List<VariantInventory> inventories = variants
            .Select(v => new VariantInventory(v.Key, v.DisplayName, v.Namespaces, application))
            .ToList();

        VariantInventory? baseOnlyFallback = variants.Any(v => v.Key == DefaultVariant)
            ? null
            : new VariantInventory(baseOnly.Key, baseOnly.DisplayName, baseOnly.Namespaces, application);

        ThemeReading reading = new(walk.Reads, walk.EntryUnresolved, walk.Scoped, walk.Entry, Path.GetFullPath(source.BaseDirectory));
        return new ThemeInventory(inventories, baseOnlyFallback, walk.Unresolved, walk.Files, inheritance, application, reading);
    }
}

/// <summary>A definition the lookup found, and the dictionary it found it in.</summary>
internal readonly record struct Found(ResourceDefinition Definition, ResourceNode Node);

/// <summary>
/// What an application holds, as <c>Application.TryGetResource</c> at Avalonia 12.1.3 searches it: its resources,
/// a consumer's tokens and then the files a configuration merges, the last listed first, before its styles, the
/// theme; and the variant each display name inherits.
/// </summary>
/// <remarks>
/// ⭐ <b>Where an alias resolves is plans/00006's model, and <c>DrawnColourTests</c> holds it to what Avalonia
/// draws.</b> A key asked from outside, as a consumer's reference or a contrast pair's key is, starts here. A
/// <c>StaticResource</c> alias searches its own chain first, in the variant of the theme dictionary around it, then
/// here in that variant; a <c>DynamicResource</c> one searches here, in the variant asked.
/// </remarks>
internal sealed class ResourceApplication(IReadOnlyList<ResourceNode> roots, IReadOnlyDictionary<string, string>? inheritance)
{
    // The one thing it caches, each variant's chain, is cached concurrently: an inventory resolves for whoever shares it.
    private readonly ConcurrentDictionary<string, string[]> _searched = new(StringComparer.Ordinal);

    /// <summary>The roots the lookup searches, in order.</summary>
    public IReadOnlyList<ResourceNode> Roots { get; } = roots;

    /// <summary>This application with <paramref name="resources"/>, a consumer's tokens, searched before what it holds.</summary>
    public ResourceApplication Under(ResourceApplication resources) => new([.. resources.Roots, .. Roots], inheritance);

    /// <summary>The colour <paramref name="key"/> resolves to, asked from outside under <paramref name="variant"/>.</summary>
    public AuditColor? Resolve(string key, string variant)
    {
        return Find(key, variant, Building()) is { } found ? Colour(found, variant, 1.0) : null;
    }

    /// <summary>Every key the lookup finds under <paramref name="variant"/>, each with the definition it finds first.</summary>
    public Dictionary<string, Found> Flatten(string variant)
    {
        Dictionary<string, Found> found = new(StringComparer.Ordinal);
        foreach (ResourceNode root in Roots)
        {
            FlattenInto(root, variant, found);
        }

        return found;
    }

    /// <summary>
    /// The colour <paramref name="found"/> resolves to, asked under <paramref name="variant"/> and drawn inside elements
    /// whose opacities multiply to <paramref name="opacity"/>.
    /// </summary>
    public AuditColor? Colour(Found found, string variant, double opacity)
    {
        return Colour(found, variant, Building(), opacity, asColor: false);
    }

    /// <summary>
    /// What the application's lookup finds first for <paramref name="key"/> under <paramref name="variant"/>, passing
    /// <paramref name="past"/>, as a reference held by a definition of its own key searches past that definition.
    /// </summary>
    public Found? Find(string key, string? variant, ResourceDefinition? past)
    {
        return Find(key, variant, Past(past));
    }

    /// <summary>What <paramref name="node"/>'s own lookup finds for <paramref name="key"/> under <paramref name="variant"/>.</summary>
    public Found? FindIn(ResourceNode node, string key, string? variant)
    {
        return FindIn(node, key, variant, Building());
    }

    /// <summary>
    /// Where a theme's <paramref name="read"/> finds its key, by plans/00006: a dynamic one in the application under
    /// <paramref name="variant"/>; a static one on its own chain, from its dictionary out to its root, then, unless its
    /// root is a dictionary with a class of its own, in the application, both in the variant of the theme dictionary
    /// around it, or in <c>Default</c> alone. A read held by a definition of its own key searches past it.
    /// </summary>
    public (ReadReach Reach, Found? Found) Locate(ThemeRead read, string variant)
    {
        ResourceDefinition? past = read.Holder is { } holder && KeyOf(holder) == read.Key ? holder : null;
        if (read.Dynamic)
        {
            return (ReadReach.Dynamic, Find(read.Key, variant, Past(past)));
        }

        string? around = read.Node.ResolutionVariant;
        for (ResourceNode? node = read.Node; node is not null; node = node.Parent)
        {
            if (FindIn(node, read.Key, around, Past(past)) is { } found)
            {
                return (ReadReach.Chain, found);
            }
        }

        return read.Node.Root.BuiltAtOnce
            ? (ReadReach.BuiltAtOnce, null)
            : (ReadReach.Application, Find(read.Key, around, Past(past)));
    }

    /// <summary>
    /// Whether <paramref name="found"/>, followed alias by alias where Avalonia follows it under <paramref name="variant"/>,
    /// reads <paramref name="key"/> on the way: an alias of it, or a definition <paramref name="reads"/> says reads it, as a
    /// gradient brush whose stop names it does.
    /// </summary>
    public bool Reaches(Found found, string key, string variant, Func<Found, bool> reads)
    {
        HashSet<ResourceDefinition> building = Building();
        for (Found? step = found; step is { } current && building.Add(current.Definition);)
        {
            if (reads(current))
            {
                return true;
            }

            if (current.Definition.Value is not ResourceValue.Alias alias)
            {
                return false;
            }

            if (alias.TargetKey == key)
            {
                return true;
            }

            bool dynamic = ThemeDefinitionScanner.ReadsDynamically(current.Definition.Element, "Color");
            step = Locate(alias.TargetKey, current, dynamic, variant, building);
        }

        return false;
    }

    /// <summary>The key a definition is written under, from its element; <c>null</c> for one a provider supplied.</summary>
    public static string? KeyOf(ResourceDefinition definition)
    {
        return definition.Element?.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))?.Value;
    }

    /// <summary>The definitions being resolved, which a lookup does not find, as Avalonia's does not while it builds one.</summary>
    private static HashSet<ResourceDefinition> Building() => new(ReferenceEqualityComparer.Instance);

    /// <summary>A lookup that passes <paramref name="past"/>, where one is given.</summary>
    private static HashSet<ResourceDefinition> Past(ResourceDefinition? past)
    {
        HashSet<ResourceDefinition> building = Building();
        if (past is not null)
        {
            building.Add(past);
        }

        return building;
    }

    /// <summary>What the application's lookup finds first for <paramref name="key"/> under <paramref name="variant"/>.</summary>
    private Found? Find(string key, string? variant, HashSet<ResourceDefinition> building)
    {
        foreach (ResourceNode root in Roots)
        {
            if (FindIn(root, key, variant, building) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// What <paramref name="node"/>'s own lookup finds for <paramref name="key"/> under <paramref name="variant"/>: its own
    /// entries, its theme dictionaries, its merged dictionaries the last first, then the files its styles include the last
    /// first, each asked under the same variant.
    /// </summary>
    private Found? FindIn(ResourceNode node, string key, string? variant, HashSet<ResourceDefinition> building)
    {
        if (node.Own.TryGetValue(key, out ResourceDefinition? own) && !building.Contains(own))
        {
            return new Found(own, node);
        }

        if (node.Themes.Count > 0)
        {
            foreach (string name in Searched(variant))
            {
                if (node.Themes.TryGetValue(name, out ResourceNode? theme) && FindIn(theme, key, variant, building) is { } found)
                {
                    return found;
                }
            }
        }

        for (int i = node.Merged.Count - 1; i >= 0; i--)
        {
            if (FindIn(node.Merged[i], key, variant, building) is { } found)
            {
                return found;
            }
        }

        for (int i = node.Styles.Count - 1; i >= 0; i--)
        {
            if (FindIn(node.Styles[i], key, variant, building) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private void FlattenInto(ResourceNode node, string variant, Dictionary<string, Found> found)
    {
        foreach ((string key, ResourceDefinition definition) in node.Own)
        {
            found.TryAdd(key, new Found(definition, node));
        }

        foreach (string name in Searched(variant))
        {
            if (node.Themes.TryGetValue(name, out ResourceNode? theme))
            {
                FlattenInto(theme, variant, found);
            }
        }

        for (int i = node.Merged.Count - 1; i >= 0; i--)
        {
            FlattenInto(node.Merged[i], variant, found);
        }

        for (int i = node.Styles.Count - 1; i >= 0; i--)
        {
            FlattenInto(node.Styles[i], variant, found);
        }
    }

    /// <summary>
    /// The theme dictionaries a lookup under <paramref name="variant"/> searches, by display name: the variant and
    /// those it inherits, then <c>Default</c>; <c>Default</c> alone for none, or for <c>Default</c> itself.
    /// </summary>
    public string[] Searched(string? variant)
    {
        return _searched.GetOrAdd(variant ?? ThemeInventory.DefaultVariant, static (name, inheritance) =>
        {
            List<string> chain = [];
            for (string? current = name;
                 current is not null && current != ThemeInventory.DefaultVariant && !chain.Contains(current, StringComparer.Ordinal);
                 current = inheritance is not null && inheritance.TryGetValue(current, out string? parent) ? parent : null)
            {
                chain.Add(current);
            }

            chain.Add(ThemeInventory.DefaultVariant);
            return [.. chain];
        }, inheritance);
    }

    /// <summary>
    /// Follows <paramref name="found"/>, <paramref name="opacity"/> being the product of the opacities met on the way,
    /// and <paramref name="asColor"/> whether a brush's <c>Color</c> named it, so that it must be a colour.
    /// </summary>
    private AuditColor? Colour(Found found, string variant, HashSet<ResourceDefinition> building, double opacity, bool asColor)
    {
        ResourceDefinition definition = found.Definition;
        if (!building.Add(definition))
        {
            return null;
        }

        try
        {
            switch (definition.Value)
            {
                case ResourceValue.ColorLiteral literal when !(asColor && IsBrush(definition)):
                    return OpacityOf(literal.Opacity, literal.OpacityKey, found, variant, building) is { } own
                        ? VariantInventory.WithOpacity(literal.Color, opacity * own)
                        : null;
                case ResourceValue.Alias alias when !(asColor && alias.FromColor):
                    if (OpacityOf(alias.Opacity, alias.OpacityKey, found, variant, building) is not { } aliasing)
                    {
                        return null;
                    }

                    bool dynamic = ThemeDefinitionScanner.ReadsDynamically(definition.Element, "Color");
                    return Locate(alias.TargetKey, found, dynamic, variant, building) is { } target
                        ? Colour(target, variant, building, opacity * aliasing, asColor || alias.FromColor)
                        : null;
                default:
                    return null;
            }
        }
        finally
        {
            building.Remove(definition);
        }
    }

    /// <summary>
    /// Where the resource that <paramref name="from"/> names as <paramref name="key"/> is found: for a dynamic reference,
    /// the application under the variant asked; for a static one, <paramref name="from"/>'s own chain, from its dictionary
    /// out to its root, then the application, both in the variant of the theme dictionary around it. A dictionary with a
    /// class of its own, built at once, has no application to fall back on that the audit can see.
    /// </summary>
    private Found? Locate(string key, Found from, bool dynamic, string variant, HashSet<ResourceDefinition> building)
    {
        if (dynamic)
        {
            return Find(key, variant, building);
        }

        string? around = from.Node.ResolutionVariant;
        for (ResourceNode? node = from.Node; node is not null; node = node.Parent)
        {
            if (FindIn(node, key, around, building) is { } found)
            {
                return found;
            }
        }

        return from.Node.Root.BuiltAtOnce ? null : Find(key, around, building);
    }

    /// <summary>
    /// A brush's own opacity: <paramref name="literal"/>, or where the brush names a resource for it, the number an
    /// <c>x:Double</c> under that key holds, found where the brush's own reference finds it, clamped as a literal is;
    /// <c>null</c> when the key holds no number.
    /// </summary>
    private double? OpacityOf(double literal, string? key, Found brush, string variant, HashSet<ResourceDefinition> building)
    {
        if (key is null)
        {
            return literal;
        }

        bool dynamic = ThemeDefinitionScanner.ReadsDynamically(brush.Definition.Element, "Opacity");
        return Locate(key, brush, dynamic, variant, building) is { Definition.Element: { Name.LocalName: "Double" } number }
               && double.TryParse(number.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? Math.Clamp(value, 0, 1)
            : null;
    }

    /// <summary>Whether a definition is a brush, which a brush's <c>Color</c> cannot name.</summary>
    private static bool IsBrush(ResourceDefinition definition)
    {
        return definition.Element?.Name.LocalName.EndsWith("SolidColorBrush", StringComparison.Ordinal) == true;
    }
}
