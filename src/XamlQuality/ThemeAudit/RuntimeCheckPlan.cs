namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// What the runtime theme check asks Avalonia, worked out from the audit's result: one setup for each
/// theme and variant a map names, the keys to look up under it with the audit's answer for each, and
/// what the check cannot ask.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The audit is a model of Avalonia's resource lookup, and this plan is how the model is read
/// against the real one.</b> The source package <c>Bennewitz.Ninja.XamlQuality.ThemeRuntime</c>, compiled
/// into the project that adopts it, builds each setup in a running application, asks
/// <c>Application.TryGetResource</c> for each lookup, and hands the answers to <see cref="Compare"/>.
/// What needs no running framework is here, in the library, by plans/00003.
/// </para>
/// <para>
/// ⚠ <b>A theme, merge path or variant the map does not name is skipped, never checked against a bare
/// theme.</b> A variant built from its name loses what its <c>InheritVariant</c> gives it, and a theme
/// checked without a dictionary the configuration merges reads every key that dictionary supplies as
/// missing: either would be the map's error reported as the audit's.
/// </para>
/// <para>
/// ⓘ <b>A member added to these types later is an <c>init</c> property, never a new positional
/// parameter.</b> The source half an adopting project compiles may be older than the library it runs
/// against, which the package's minimum allows, and a positional parameter added here would break its
/// build.
/// </para>
/// </remarks>
/// <param name="Setups">
/// One for each mapped theme and variant: the configuration's themes in order, and each theme's
/// variants in the audit's order.
/// </param>
/// <param name="Skipped">
/// What the map leaves out, theme by theme, then each key written as a markup extension, once for each
/// consumer that references it.
/// </param>
public sealed record RuntimeCheckPlan(IReadOnlyList<RuntimeSetup> Setups, IReadOnlyList<XamlSkip> Skipped)
{
    /// <summary>The plan for <paramref name="audit"/>, given the names a map holds.</summary>
    /// <remarks>
    /// A theme no consumer is audited under is left out, since there is nothing to ask under it. A key a
    /// consumer defines itself is never looked up: it may live in a control's own resources, where no
    /// lookup at the application can see it.
    /// </remarks>
    /// <param name="audit">The audit's result, from <see cref="AuditRunner.Run"/> in the same run.</param>
    /// <param name="variants">
    /// Each theme the map names, by its name in the configuration, with the display names of the
    /// variants the map gives it.
    /// </param>
    /// <param name="merges">The merge paths the map names, as the configuration writes them.</param>
    public static RuntimeCheckPlan For(
        AuditResult audit,
        IReadOnlyDictionary<string, IReadOnlySet<string>> variants,
        IReadOnlySet<string> merges)
    {
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(variants);
        ArgumentNullException.ThrowIfNull(merges);

        List<RuntimeSetup> setups = [];
        List<XamlSkip> skipped = [];

        foreach (ThemeTarget theme in audit.Themes)
        {
            string name = theme.Config.Name;
            ConsumerThemeFindings[] findings = [.. audit.Findings.Where(finding => ReferenceEquals(finding.Theme, theme))];
            if (findings.Length == 0)
            {
                continue;
            }

            if (!variants.TryGetValue(name, out IReadOnlySet<string>? mapped))
            {
                skipped.Add(new XamlSkip(name, "the map names no factory for this theme, so none of its variants is checked"));
                continue;
            }

            string[] themeMerges = [.. theme.Config.Merge ?? []];
            string[] unmapped = [.. themeMerges.Where(path => !merges.Contains(path))];
            if (unmapped.Length > 0)
            {
                foreach (string path in unmapped)
                {
                    skipped.Add(new XamlSkip(name,
                        $"the map names no URI for the merged dictionary {path}, and the theme is not checked without it"));
                }

                continue;
            }

            foreach (string variant in theme.Variants)
            {
                if (!mapped.Contains(variant))
                {
                    skipped.Add(new XamlSkip($"{name}, {variant}",
                        "the map names no ThemeVariant for this variant, and one built from its name would lose what it inherits"));
                    continue;
                }

                List<RuntimeLookup> lookups = [];
                foreach (ConsumerThemeFindings finding in findings)
                {
                    HashSet<string> undefined = new(
                        finding.Undefined.Where(gap => gap.DisplayName == variant).Select(gap => gap.Key),
                        StringComparer.Ordinal);
                    foreach (string key in KeysOf(finding.Consumer))
                    {
                        lookups.Add(new RuntimeLookup(finding.Consumer.Config.Name, name, variant, key, !undefined.Contains(key)));
                    }
                }

                setups.Add(new RuntimeSetup(name, variant, themeMerges, lookups));
            }
        }

        foreach (ConsumerScan consumer in audit.Consumers)
        {
            if (!audit.Findings.Any(finding => ReferenceEquals(finding.Consumer, consumer)))
            {
                continue;
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (ResourceReference reference in consumer.References
                         .Where(reference => IsMarkupExtension(reference.Key) && !consumer.OwnKeys.Contains(reference.Key))
                         .OrderBy(reference => reference.Key, StringComparer.Ordinal))
            {
                if (seen.Add(reference.Key))
                {
                    skipped.Add(new XamlSkip($"{consumer.Config.Name}: {reference.Key}",
                        "a key written as a markup extension: the audit holds its text, and Avalonia's key is the object it builds",
                        audit.Config.Relative(reference.File), reference.Line > 0 ? reference.Line : null));
                }
            }
        }

        return new RuntimeCheckPlan(setups, skipped);
    }

    /// <summary>Compares Avalonia's answers with the audit's, setup by setup.</summary>
    /// <remarks>
    /// ⚠ <b>Nothing planned leaves the result unnamed.</b> A setup in <paramref name="notLoaded"/>, and one
    /// with a lookup that no answer covers, are named in <see cref="RuntimeCheckResult.Skipped"/>, so a
    /// lookup the source half never made cannot pass for one that agreed. An answer to a lookup the plan
    /// does not hold, or to one in a setup that did not load, is not counted.
    /// </remarks>
    /// <param name="answers">Avalonia's answer to each lookup it was asked, in any order.</param>
    /// <param name="notLoaded">
    /// The setups of this plan whose theme or merged dictionaries did not load, each with the exception's
    /// message.
    /// </param>
    public RuntimeCheckResult Compare(IReadOnlyList<RuntimeAnswer> answers, IReadOnlyDictionary<RuntimeSetup, string> notLoaded)
    {
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(notLoaded);

        Dictionary<RuntimeLookup, RuntimeAnswer> byLookup = [];
        foreach (RuntimeAnswer answer in answers)
        {
            byLookup.TryAdd(answer.Lookup, answer);
        }

        List<RuntimeDifference> differences = [];
        List<XamlSkip> skipped = [.. Skipped];
        int inspected = 0;

        foreach (RuntimeSetup setup in Setups)
        {
            string subject = $"{setup.Theme}, {setup.DisplayName}";
            if (notLoaded.TryGetValue(setup, out string? message))
            {
                skipped.Add(new XamlSkip(subject,
                    $"did not load, so none of its {setup.Lookups.Count} lookups was made: {OneLine(message)}"));
                continue;
            }

            int unanswered = 0;
            foreach (RuntimeLookup lookup in setup.Lookups)
            {
                if (!byLookup.TryGetValue(lookup, out RuntimeAnswer? answer))
                {
                    unanswered++;
                    continue;
                }

                inspected++;
                if (KindOf(lookup, answer) is { } kind)
                {
                    differences.Add(new RuntimeDifference(lookup, kind, answer.Message));
                }
            }

            if (unanswered > 0)
            {
                skipped.Add(new XamlSkip(subject, $"{unanswered} of its {setup.Lookups.Count} lookups were not answered"));
            }
        }

        return new RuntimeCheckResult(differences, inspected, skipped);
    }

    /// <summary>The keys a consumer references and does not define itself, markup extensions aside, ordinally.</summary>
    private static IEnumerable<string> KeysOf(ConsumerScan consumer) =>
        consumer.References
            .Select(reference => reference.Key)
            .Where(key => !IsMarkupExtension(key) && !consumer.OwnKeys.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    /// <summary>A key the audit holds as the text of a markup extension, such as <c>{x:Type Button}</c>.</summary>
    private static bool IsMarkupExtension(string key) => key.StartsWith('{');

    private static RuntimeDifferenceKind? KindOf(RuntimeLookup lookup, RuntimeAnswer answer) =>
        answer.Message is not null ? RuntimeDifferenceKind.LookupThrew
        : lookup.Defined && !answer.Found ? RuntimeDifferenceKind.DefinedButNotFound
        : !lookup.Defined && answer.Found ? RuntimeDifferenceKind.UndefinedButFound
        : null;

    internal static string OneLine(string text) => text.ReplaceLineEndings(" ");
}

/// <summary>
/// One theme and variant the runtime check sets up afresh: the theme built anew, its merged dictionaries
/// loaded anew, and the lookups to make under it.
/// </summary>
/// <param name="Theme">The theme's name in the configuration.</param>
/// <param name="DisplayName">
/// The variant's display name, as <see cref="UndefinedKeyFinding.DisplayName"/> carries it; that
/// record's <c>Variant</c> is the key the variant resolves to.
/// </param>
/// <param name="Merges">
/// The dictionaries the configuration merges after the theme, by path as it writes them, a parent's
/// first where the theme extends one.
/// </param>
/// <param name="Lookups">The keys to look up under it: consumers in the configuration's order, and each one's keys ordinally.</param>
public sealed record RuntimeSetup(string Theme, string DisplayName, IReadOnlyList<string> Merges, IReadOnlyList<RuntimeLookup> Lookups);

/// <summary>One key a consumer references and does not define itself, under one theme and variant, with the audit's answer.</summary>
/// <param name="Consumer">The consumer's name in the configuration.</param>
/// <param name="Theme">The theme's name in the configuration.</param>
/// <param name="DisplayName">The variant's display name.</param>
/// <param name="Key">The resource key.</param>
/// <param name="Defined">Whether the audit finds the key defined under that theme and variant.</param>
public sealed record RuntimeLookup(string Consumer, string Theme, string DisplayName, string Key, bool Defined);

/// <summary>Avalonia's answer to one lookup.</summary>
/// <param name="Lookup">The lookup answered.</param>
/// <param name="Found">Whether the application's lookup found the key; it says nothing where <paramref name="Message"/> is set.</param>
/// <param name="Message">The exception's message where the lookup threw; otherwise <c>null</c>.</param>
public sealed record RuntimeAnswer(RuntimeLookup Lookup, bool Found, string? Message = null);

/// <summary>What the runtime check found: where Avalonia's answer differs from the audit's, how many lookups were made, and what was not checked.</summary>
/// <remarks>
/// ⭐ <b>An adopting test asserts all three.</b> <see cref="Differences"/> empty; <see cref="Inspected"/>
/// above zero, since a check that asked nothing agrees with everything; and <see cref="Skipped"/> equal to
/// the list it expects, so that a skip that appears, a theme or a dictionary that stops loading among
/// them, fails it.
/// </remarks>
/// <param name="Differences">Each lookup where Avalonia's answer differs from the audit's, in the plan's order.</param>
/// <param name="Inspected">The lookups made and answered.</param>
/// <param name="Skipped">
/// The plan's skips, then each setup that did not load and each one with a lookup left unanswered, in
/// the plan's order.
/// </param>
public sealed record RuntimeCheckResult(IReadOnlyList<RuntimeDifference> Differences, int Inspected, IReadOnlyList<XamlSkip> Skipped);

/// <summary>One lookup where Avalonia's answer differs from the audit's.</summary>
/// <param name="Lookup">The lookup.</param>
/// <param name="Kind">How the answers differ.</param>
/// <param name="Message">For a lookup that threw, the exception's message; otherwise <c>null</c>.</param>
public sealed record RuntimeDifference(RuntimeLookup Lookup, RuntimeDifferenceKind Kind, string? Message = null)
{
    /// <summary>A single line suitable for a report or an assertion's message.</summary>
    public override string ToString()
    {
        string what = Kind switch
        {
            RuntimeDifferenceKind.DefinedButNotFound => "the audit finds it defined, and Avalonia does not find it",
            RuntimeDifferenceKind.UndefinedButFound => "the audit finds it undefined, and Avalonia finds it",
            _ => "the lookup threw",
        };

        string line = $"{Lookup.Consumer} under {Lookup.Theme}, {Lookup.DisplayName}: {Lookup.Key}: {what}";
        return Message is null ? line : $"{line}: {RuntimeCheckPlan.OneLine(Message)}";
    }
}

/// <summary>How Avalonia's answer to a lookup differs from the audit's.</summary>
public enum RuntimeDifferenceKind
{
    /// <summary>The audit finds the key defined, and Avalonia does not find it.</summary>
    DefinedButNotFound,

    /// <summary>The audit finds the key undefined, and Avalonia finds it.</summary>
    UndefinedButFound,

    /// <summary>
    /// The lookup threw, as it does where an alias's target is missing under the variant the alias is
    /// resolved in, whatever the audit's answer.
    /// </summary>
    LookupThrew,
}
