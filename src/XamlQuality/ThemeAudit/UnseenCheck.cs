namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>What the check found for one consumer under one theme: the findings, what it could not decide, and how many redefinitions it examined, each a key under a variant.</summary>
internal sealed record UnseenResult(IReadOnlyList<UnseenRedefinition> Found, IReadOnlyList<UnseenRedefinition> NotDecided, int Examined);

/// <summary>
/// The check of plans/00007: a key a consumer's application holds under a variant, outside the theme's files, that the
/// theme defines with another value and reads under that variant, where no read finds the redefinition.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>It reads plans/00006's lookup, never a second model of it.</b> The application holds the consumer's definitions
/// in its resources, before the files the configuration merges and the theme; a read counts where something applies or
/// reads the entry holding it, and resolves where <see cref="ResourceApplication.Locate(ThemeRead, string)"/> says.
/// </para>
/// <para>
/// ⚠ <b>What it cannot decide, it names, and never guesses</b>, by decision 5: a static read in a dictionary with a class
/// of its own whose chain lacks the key, one that finds it first among scoped resources, one of a kind no drawing holds
/// the lookup to, a definition under the resources of an element in an <c>Application</c>'s styles, and a theme whose walk
/// left something unresolved.
/// </para>
/// </remarks>
internal static class UnseenCheck
{
    /// <summary>
    /// The kinds of static read whose lookup is drawn, by decision 8: what holds the read innermost and the route to its
    /// dictionary, as <see cref="ThemeRead.Kind"/> writes them. Plans/00006's <c>DrawnColourTests</c> drew the first four;
    /// the rest are every kind Fluent, Simple and Semi hold at their pins with a read that counts. A read of another kind is
    /// not decided.
    /// </summary>
    internal static readonly IReadOnlySet<string> DrawnKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "alias entry",
        "alias entry>class",
        "alias entry>theme-inline",
        "brush entry",
        "alias entry>MergeResourceInclude>theme-inline",
        "alias entry>ResourceInclude>ResourceInclude",
        "alias entry>theme-ResourceInclude>ResourceInclude",
        "brush entry>MergeResourceInclude",
        "brush entry>MergeResourceInclude>theme-inline",
        "brush entry>ResourceInclude>theme-ResourceInclude",
        "brush entry>ResourceInclude>theme-class",
        "brush entry>theme-ResourceInclude>ResourceInclude",
        "entry entry>ResourceInclude>ResourceInclude",
        "entry entry>StyleInclude>MergeResourceInclude",
        "entry entry>theme-ResourceInclude>ResourceInclude",
        "setter entry>ResourceInclude>ResourceInclude",
        "setter entry>StyleInclude>MergeResourceInclude",
        "template entry>ResourceInclude>ResourceInclude",
        "template entry>StyleInclude>MergeResourceInclude",
    };

    /// <summary>
    /// The kinds of static read that reach the application whose drawing differs with the redefinition held before the
    /// theme is built and after, by decision 8; a read of one that reaches the application is not decided.
    /// </summary>
    internal static readonly IReadOnlySet<string> OrderDependentKinds = new HashSet<string>(StringComparer.Ordinal);

    internal const string BuiltAtOnceWhy = "a static read in a dictionary with a class of its own, whose chain lacks the key, resolves against whatever the application holds when that dictionary is built";
    internal const string OrderWhy = "a static read of a kind whose drawing differs with the redefinition held before the theme is built and after";
    internal const string ScopedWhy = "a static read finds the key first among the resources a control theme, a style or a template holds, which Avalonia scopes to what they style";
    internal const string UndrawnWhy = "a static read is of a kind no drawing holds the lookup to";
    internal const string ApplicationStylesWhy = "the consumer defines it under the resources of an element in an application's styles, which may be the theme's own";
    internal const string UnresolvedWhy = "the theme's walk left an include or a class unresolved, and what it could not read may read the key dynamically";

    /// <summary>Checks <paramref name="consumer"/> under <paramref name="theme"/>, naming each definition's directory where <paramref name="nameDirectories"/>.</summary>
    public static UnseenResult Run(ConsumerReading consumer, ThemeTarget theme, bool nameDirectories)
    {
        ThemeInventory inventory = theme.Inventory;
        ThemeReading reading = inventory.Reading;
        Pair pair = new(consumer, inventory);
        ResourceApplication application = pair.Application;
        ResourceNode held = pair.Held;
        HashSet<ResourceNode> consumerNodes = pair.ConsumerNodes;
        Dictionary<ResourceDefinition, ConsumerDefinition> byDefinition = pair.ByDefinition;
        string[] keys = [.. byDefinition.Values.Select(definition => definition.Key).Where(key => !key.StartsWith('{')).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

        List<(string Key, string Variant, Verdict Verdict)> verdicts = [];
        int examined = 0;
        foreach (string variant in theme.Variants)
        {
            Dictionary<ThemeRead, (ReadReach Reach, Found? Found)> taken = pair.Counted(variant);
            ILookup<string, ThemeRead> readsOf = taken.Keys.ToLookup(read => read.Key, StringComparer.Ordinal);

            foreach (string key in keys)
            {
                if (application.FindIn(held, key, variant) is not { } found
                    || application.FindIn(reading.Entry, key, variant) is not { } own
                    || SameValue.As(found.Definition, own.Definition))
                {
                    continue;
                }

                examined++;
                ThemeRead[] reads = [.. readsOf[key]];
                if (reads.Length == 0)
                {
                    continue;
                }

                List<string> why = [];
                bool sees = false;
                foreach (ThemeRead read in reads)
                {
                    (ReadReach reach, Found? target) = taken[read];
                    bool consumers = target is { } t && consumerNodes.Contains(t.Node);
                    if (!read.Dynamic && !DrawnKinds.Contains(read.Kind))
                    {
                        why.Add(UndrawnWhy + ": " + read.Kind);
                        continue;
                    }

                    switch (reach)
                    {
                        case ReadReach.Dynamic or ReadReach.Application when consumers:
                            if (reach == ReadReach.Application && OrderDependentKinds.Contains(read.Kind))
                            {
                                why.Add(OrderWhy);
                            }
                            else
                            {
                                sees = true;
                            }

                            break;
                        case ReadReach.Chain when target is { } scoped && reading.Scoped.Contains(scoped.Definition):
                            why.Add(ScopedWhy);
                            break;
                        case ReadReach.BuiltAtOnce:
                            why.Add(BuiltAtOnceWhy);
                            break;
                    }
                }

                if (sees)
                {
                    continue;
                }

                ConsumerDefinition defined = byDefinition[found.Definition];
                if (why.Count == 0)
                {
                    if (defined.InApplicationStyles)
                    {
                        why.Add(ApplicationStylesWhy);
                    }

                    if (inventory.Unresolved.Count > 0)
                    {
                        why.Add(UnresolvedWhy);
                    }
                }

                string[] instead = [.. taken.Keys
                    .Where(read => read.Dynamic && read.Key != key)
                    .Select(read => read.Key)
                    .Distinct(StringComparer.Ordinal)
                    .Where(other => inventory.Application.FindIn(reading.Entry, other, variant) is { } definition
                                    && inventory.Application.Reaches(definition, key, variant))
                    .Order(StringComparer.Ordinal)];

                string[] readAt = [.. reads
                    .Select(read => (Path: Path.GetRelativePath(reading.BaseDirectory, read.File).Replace('\\', '/'), read.Line))
                    .Distinct()
                    .OrderBy(at => at.Path, StringComparer.Ordinal)
                    .ThenBy(at => at.Line)
                    .Select(at => at.Path + ":" + at.Line.ToString(System.Globalization.CultureInfo.InvariantCulture))];

                string? reason = why.Count == 0 ? null : string.Join("; ", why.Distinct(StringComparer.Ordinal));
                verdicts.Add((key, variant, new Verdict(reason, readAt, instead, Shown(defined, nameDirectories))));
            }
        }

        List<UnseenRedefinition> records = Grouped(verdicts, theme.Variants);
        return new UnseenResult([.. records.Where(record => record.Why is null)], [.. records.Where(record => record.Why is not null)], examined);
    }

    /// <summary>The theme's reads that count under <paramref name="variant"/> for <paramref name="consumer"/>, each with where it finds its key.</summary>
    internal static IReadOnlyDictionary<ThemeRead, (ReadReach Reach, Found? Found)> ReadsThatCount(ConsumerReading consumer, ThemeInventory inventory, string variant)
    {
        return new Pair(consumer, inventory).Counted(variant);
    }

    /// <summary>
    /// A consumer and a theme as the check reads them: the application holding the consumer's definitions in its resources,
    /// before the files the configuration merges and the theme, and the theme's reads by the entry holding them.
    /// </summary>
    private sealed class Pair
    {
        private readonly ThemeInventory _inventory;
        private readonly HashSet<ResourceNode> _themeNodes;
        private readonly ILookup<(ResourceDefinition, ResourceNode), ThemeRead> _byHolder;
        private readonly string[] _implicitThemes;
        private readonly ConsumerReference[] _references;

        public Pair(ConsumerReading consumer, ThemeInventory inventory)
        {
            _inventory = inventory;
            HashSet<string> themeFiles = new(inventory.Files.Select(Path.GetFullPath), StringComparer.Ordinal);

            // The application holds the consumer's definitions in its resources, a theme dictionary for each variant they are held under.
            foreach (ConsumerDefinition definition in consumer.Definitions)
            {
                if (!definition.Held || definition.Definition.File is not { } file || themeFiles.Contains(Path.GetFullPath(file)))
                {
                    continue;
                }

                foreach (string? variant in definition.Variants)
                {
                    ResourceNode into = Held;
                    if (variant is not null)
                    {
                        if (!Held.Themes.TryGetValue(variant, out ResourceNode? slot))
                        {
                            slot = new ResourceNode(Held, variant, builtAtOnce: false) { Route = "consumer>theme-inline" };
                            Held.Themes[variant] = slot;
                            ConsumerNodes.Add(slot);
                        }

                        into = slot;
                    }

                    into.Own.TryAdd(definition.Key, definition.Definition);
                }

                ByDefinition[definition.Definition] = definition;
            }

            ConsumerNodes.Add(Held);
            Application = inventory.Application.Under(new ResourceApplication([Held], null));
            _themeNodes = NodesFrom(inventory.Reading.Entry);
            _byHolder = inventory.Reading.Reads
                .Where(read => read.Holder is not null)
                .ToLookup(read => (read.Holder!, read.Node), new HolderComparer());
            _implicitThemes = [.. _themeNodes
                .SelectMany(node => node.Own.Keys)
                .Where(key => key.StartsWith("{x:Type", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
            _references = [.. consumer.References.Where(reference => !themeFiles.Contains(Path.GetFullPath(reference.File)))];
        }

        /// <summary>The dictionary the application holds the consumer's definitions in.</summary>
        public ResourceNode Held { get; } = new(parent: null, variant: null, builtAtOnce: false) { Route = "consumer" };

        /// <summary>That dictionary and its theme dictionaries.</summary>
        public HashSet<ResourceNode> ConsumerNodes { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>Each definition the application holds, with what the consumer's reading says of it.</summary>
        public Dictionary<ResourceDefinition, ConsumerDefinition> ByDefinition { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>The application, the consumer's definitions in its resources.</summary>
        public ResourceApplication Application { get; }

        /// <summary>
        /// The reads that count under <paramref name="variant"/>, each with where it finds its key, by decision 2: those in an
        /// implicit control theme the application finds for its type, those no keyed entry holds, and those in an entry that
        /// a read that counts, or one of the consumer's references, finds.
        /// </summary>
        public Dictionary<ThemeRead, (ReadReach Reach, Found? Found)> Counted(string variant)
        {
            Dictionary<ThemeRead, (ReadReach Reach, Found? Found)> taken = new(ReferenceEqualityComparer.Instance);
            HashSet<(ResourceDefinition, ResourceNode)> counted = new(new HolderComparer());
            Queue<(ResourceDefinition, ResourceNode)> queue = [];

            void Count(Found? found)
            {
                if (found is { } entry && _themeNodes.Contains(entry.Node) && counted.Add((entry.Definition, entry.Node)))
                {
                    queue.Enqueue((entry.Definition, entry.Node));
                }
            }

            void Take(ThemeRead read)
            {
                if (!taken.ContainsKey(read))
                {
                    (ReadReach reach, Found? found) = Application.Locate(read, variant);
                    taken[read] = (reach, found);
                    Count(found);
                }
            }

            foreach (string key in _implicitThemes)
            {
                Count(Application.Find(key, variant, past: null));
            }

            foreach (ThemeRead read in _inventory.Reading.Reads.Where(read => read.Holder is null))
            {
                Take(read);
            }

            foreach (ConsumerReference reference in _references)
            {
                Count(Application.Find(reference.Key, variant, reference.Past));
            }

            while (queue.TryDequeue(out (ResourceDefinition, ResourceNode) entry))
            {
                foreach (ThemeRead read in _byHolder[entry])
                {
                    Take(read);
                }
            }

            return taken;
        }
    }

    /// <summary>Every dictionary the walk built from <paramref name="entry"/>, a dictionary with a class of its own entered from it included.</summary>
    private static HashSet<ResourceNode> NodesFrom(ResourceNode entry)
    {
        HashSet<ResourceNode> nodes = new(ReferenceEqualityComparer.Instance);
        Stack<ResourceNode> pending = new([entry]);
        while (pending.TryPop(out ResourceNode? node))
        {
            if (nodes.Add(node))
            {
                foreach (ResourceNode next in node.Themes.Values.Concat(node.Merged).Concat(node.Styles))
                {
                    pending.Push(next);
                }
            }
        }

        return nodes;
    }

    /// <summary>Where the consumer defines a key, as the report writes it.</summary>
    private static string Shown(ConsumerDefinition definition, bool nameDirectory)
    {
        string[] variants = [.. definition.Variants.OfType<string>()];
        string shown = nameDirectory ? definition.Directory + ": " + definition.Shown : definition.Shown;
        return variants.Length == 0 ? shown : shown + " (" + string.Join(", ", variants) + ")";
    }

    /// <summary>One record per key and set of variants sharing a verdict, reads and keys to redefine instead; by key, then first variant.</summary>
    private static List<UnseenRedefinition> Grouped(List<(string Key, string Variant, Verdict Verdict)> verdicts, IReadOnlyList<string> order)
    {
        List<UnseenRedefinition> records = [];
        foreach (IGrouping<string, (string Key, string Variant, Verdict Verdict)> byKey in verdicts.GroupBy(v => v.Key, StringComparer.Ordinal))
        {
            foreach (IGrouping<Verdict, (string Key, string Variant, Verdict Verdict)> shared in byKey.GroupBy(v => v.Verdict, new SharedVerdict()))
            {
                Verdict verdict = shared.First().Verdict;
                string[] variants = [.. shared.Select(v => v.Variant).Distinct(StringComparer.Ordinal)];
                string[] definedAt = [.. shared.Select(v => v.Verdict.DefinedAt).Distinct(StringComparer.Ordinal)];
                records.Add(new UnseenRedefinition(byKey.Key, variants, definedAt, verdict.ReadAt.Count, verdict.Instead, verdict.Why)
                {
                    ReadAt = verdict.ReadAt,
                });
            }
        }

        return [.. records
            .OrderBy(record => record.Key, StringComparer.Ordinal)
            .ThenBy(record => IndexIn(order, record.Variants[0]))];
    }

    private static int IndexIn(IReadOnlyList<string> order, string variant)
    {
        for (int i = 0; i < order.Count; i++)
        {
            if (order[i] == variant)
            {
                return i;
            }
        }

        return order.Count;
    }

    /// <summary>One redefinition's answer under one variant.</summary>
    private sealed record Verdict(string? Why, IReadOnlyList<string> ReadAt, IReadOnlyList<string> Instead, string DefinedAt);

    /// <summary>What variants must share to be one record: the verdict, the reads and the keys to redefine instead.</summary>
    private sealed class SharedVerdict : IEqualityComparer<Verdict>
    {
        public bool Equals(Verdict? x, Verdict? y)
        {
            return x is not null && y is not null
                   && string.Equals(x.Why, y.Why, StringComparison.Ordinal)
                   && x.ReadAt.SequenceEqual(y.ReadAt, StringComparer.Ordinal)
                   && x.Instead.SequenceEqual(y.Instead, StringComparer.Ordinal);
        }

        public int GetHashCode(Verdict obj)
        {
            HashCode hash = new();
            hash.Add(obj.Why, StringComparer.Ordinal);
            foreach (string read in obj.ReadAt)
            {
                hash.Add(read, StringComparer.Ordinal);
            }

            foreach (string key in obj.Instead)
            {
                hash.Add(key, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>A definition in one dictionary, compared by reference.</summary>
    private sealed class HolderComparer : IEqualityComparer<(ResourceDefinition Definition, ResourceNode Node)>
    {
        public bool Equals((ResourceDefinition Definition, ResourceNode Node) x, (ResourceDefinition Definition, ResourceNode Node) y)
        {
            return ReferenceEquals(x.Definition, y.Definition) && ReferenceEquals(x.Node, y.Node);
        }

        public int GetHashCode((ResourceDefinition Definition, ResourceNode Node) obj)
        {
            return HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Definition),
                                    System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Node));
        }
    }
}
