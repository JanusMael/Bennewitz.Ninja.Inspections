namespace XamlQuality.ThemeRuntime.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;
using Bennewitz.Ninja.XamlQuality.ThemeRuntime;
using Xunit;
using ThemeVariant = Avalonia.Styling.ThemeVariant;

/// <summary>
/// The runtime theme check, compiled from the package's files and run headless, by plans/00003: the audit
/// of <c>Fixtures/theme-audit.json</c> read against Avalonia's lookup. The fixture theme has a Default and
/// a Dark dictionary, a Contrast variant that inherits Dark and is reached through an include, a key held
/// only in a <c>ControlTheme</c>'s resources and one added in code; the second theme merges aliases into
/// it after it. Their consumer references nine keys and one written as a markup extension.
/// </summary>
public sealed class ThemeRuntimeCheckTests
{
    private const string Fixture = "Fixture";
    private const string WithAliases = "Fixture + aliases";
    private static readonly Uri AliasesUri = new("avares://XamlQuality.ThemeRuntime.Tests/Fixtures/Themes/Aliases.axaml");

    [AvaloniaFact]
    public void Where_the_audit_and_Avalonia_agree_nothing_is_reported_through_inheritance_an_include_or_an_alias()
    {
        RuntimeCheckResult result = ThemeRuntimeCheck.Run(Audit(), Map());

        // The application's own style and resources hold OnlyDark, OnlyContrast and GoodAlias, so a check
        // that did not set them aside would find them where the audit finds them undefined.
        Assert.DoesNotContain(result.Differences, d => d.Lookup.Key is "Ink" or "OnlyDefault" or "OnlyDark" or "OnlyContrast" or "GoodAlias");

        // Two themes, three variants each, and the nine keys the consumer references and does not define.
        Assert.Equal(54, result.Inspected);
        Assert.Equal(["App: {x:Type Button}"], result.Skipped.Select(s => s.Subject).ToArray());
    }

    [AvaloniaFact]
    public void A_resource_the_theme_adds_in_code_is_found_where_the_audit_finds_it_undefined()
    {
        Assert.Equal(EverySetup(RuntimeDifferenceKind.UndefinedButFound), Of(ThemeRuntimeCheck.Run(Audit(), Map()), FixtureTheme.CodeAdded));
    }

    [AvaloniaFact]
    public void A_key_held_only_in_a_ControlThemes_own_resources_is_out_of_the_applications_reach()
    {
        Assert.Equal(EverySetup(RuntimeDifferenceKind.DefinedButNotFound), Of(ThemeRuntimeCheck.Run(Audit(), Map()), "OnlyInControlTheme"));
    }

    [AvaloniaFact]
    public void An_alias_whose_target_its_variant_and_Default_lack_throws_under_that_variant_and_what_inherits_it()
    {
        RuntimeCheckResult result = ThemeRuntimeCheck.Run(Audit(), Map());

        Assert.Equal(
            [(WithAliases, "Dark", RuntimeDifferenceKind.LookupThrew), (WithAliases, "Contrast", RuntimeDifferenceKind.LookupThrew)],
            Of(result, "DarkAlias"));
        Assert.All(result.Differences.Where(d => d.Lookup.Key == "DarkAlias"), d => Assert.Contains("OnlyContrast", d.Message));
    }

    [AvaloniaFact]
    public void An_alias_outside_any_variants_dictionary_throws_under_every_variant()
    {
        Assert.Equal(
            [
                (WithAliases, "Light", RuntimeDifferenceKind.LookupThrew),
                (WithAliases, "Dark", RuntimeDifferenceKind.LookupThrew),
                (WithAliases, "Contrast", RuntimeDifferenceKind.LookupThrew),
            ],
            Of(ThemeRuntimeCheck.Run(Audit(), Map()), "TopAlias"));
    }

    [AvaloniaFact]
    public void A_variant_built_from_its_name_loses_what_it_inherits()
    {
        RuntimeCheckResult result = ThemeRuntimeCheck.Run(Audit(), Map(contrast: new ThemeVariant("Contrast", null)));

        Assert.Equal(
            [(Fixture, "Contrast", RuntimeDifferenceKind.DefinedButNotFound), (WithAliases, "Contrast", RuntimeDifferenceKind.DefinedButNotFound)],
            Of(result, "OnlyDark"));
    }

    [AvaloniaFact]
    public void What_the_map_leaves_out_is_skipped_and_never_asked()
    {
        // No Contrast for the first theme, and no factory for the second, whose variants and merged
        // dictionary the map does name.
        ThemeRuntimeMap map = new()
        {
            Themes = new Dictionary<string, Func<IStyle>> { [Fixture] = () => new FixtureTheme() },
            Merges = new Dictionary<string, Uri> { ["Themes/Aliases.axaml"] = AliasesUri },
            Variants = new Dictionary<string, IReadOnlyDictionary<string, ThemeVariant>>
            {
                [Fixture] = new Dictionary<string, ThemeVariant> { ["Light"] = ThemeVariant.Light, ["Dark"] = ThemeVariant.Dark },
                [WithAliases] = Variants(FixtureVariants.Contrast),
            },
        };

        RuntimeCheckResult result = ThemeRuntimeCheck.Run(Audit(), map);

        Assert.Equal(["Fixture, Contrast", WithAliases, "App: {x:Type Button}"], result.Skipped.Select(s => s.Subject).ToArray());
        Assert.Equal(18, result.Inspected);
    }

    [AvaloniaFact]
    public void A_merged_dictionary_that_does_not_load_is_skipped_under_every_variant_not_only_the_first()
    {
        RuntimeCheckResult result = ThemeRuntimeCheck.Run(Audit(),
            Map(aliases: new Uri("avares://XamlQuality.ThemeRuntime.Tests/Fixtures/Themes/Gone.axaml")));

        XamlSkip[] notLoaded = [.. result.Skipped.Where(s => s.Reason.StartsWith("did not load", StringComparison.Ordinal))];
        Assert.Equal(["Fixture + aliases, Light", "Fixture + aliases, Dark", "Fixture + aliases, Contrast"], notLoaded.Select(s => s.Subject).ToArray());
        Assert.All(notLoaded, s => Assert.Contains("Gone.axaml", s.Reason));
        Assert.Equal(27, result.Inspected);
    }

    [AvaloniaFact]
    public void A_theme_whose_factory_throws_is_skipped_under_every_variant()
    {
        ThemeRuntimeMap map = Map(themes: new Dictionary<string, Func<IStyle>>
        {
            [Fixture] = () => throw new InvalidOperationException("The fixture theme is broken."),
            [WithAliases] = () => new FixtureTheme(),
        });

        RuntimeCheckResult result = ThemeRuntimeCheck.Run(Audit(), map);

        XamlSkip[] notLoaded = [.. result.Skipped.Where(s => s.Reason.StartsWith("did not load", StringComparison.Ordinal))];
        Assert.Equal(["Fixture, Light", "Fixture, Dark", "Fixture, Contrast"], notLoaded.Select(s => s.Subject).ToArray());
        Assert.All(notLoaded, s => Assert.EndsWith("The fixture theme is broken.", s.Reason, StringComparison.Ordinal));
        Assert.Equal(27, result.Inspected);
    }

    [AvaloniaFact]
    public void The_applications_styles_and_resources_are_as_before_after_a_run_and_after_one_that_throws()
    {
        Application application = Application.Current!;
        object?[] before = Snapshot(application);
        Assert.Same(((TestApp)application).OwnStyle, before[0]);

        ThemeRuntimeCheck.Run(Audit(), Map());
        Assert.Equal(before, Snapshot(application));

        // The map breaks after the first theme is in place, so the run throws from the middle of its work.
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => ThemeRuntimeCheck.Run(Audit(), Map(variants: new BreakingVariants())));
        Assert.Equal(BreakingVariants.Message, thrown.Message);
        Assert.Equal(before, Snapshot(application));
    }

    [AvaloniaFact]
    public async Task Off_the_UI_thread_it_throws()
    {
        AuditResult audit = Audit();
        ThemeRuntimeMap map = Map();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => ThemeRuntimeCheck.Run(audit, map)));
    }

    private static AuditResult Audit() =>
        AuditRunner.Run(AuditConfig.Load(Path.Combine(ProjectDirectory(), "Fixtures", "theme-audit.json")));

    private static ThemeRuntimeMap Map(
        ThemeVariant? contrast = null,
        Uri? aliases = null,
        IReadOnlyDictionary<string, Func<IStyle>>? themes = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ThemeVariant>>? variants = null) => new()
    {
        Themes = themes ?? new Dictionary<string, Func<IStyle>> { [Fixture] = () => new FixtureTheme(), [WithAliases] = () => new FixtureTheme() },
        Merges = new Dictionary<string, Uri> { ["Themes/Aliases.axaml"] = aliases ?? AliasesUri },
        Variants = variants ?? new Dictionary<string, IReadOnlyDictionary<string, ThemeVariant>>
        {
            [Fixture] = Variants(contrast ?? FixtureVariants.Contrast),
            [WithAliases] = Variants(contrast ?? FixtureVariants.Contrast),
        },
    };

    private static Dictionary<string, ThemeVariant> Variants(ThemeVariant contrast) =>
        new() { ["Light"] = ThemeVariant.Light, ["Dark"] = ThemeVariant.Dark, ["Contrast"] = contrast };

    private static (string Theme, string Variant, RuntimeDifferenceKind Kind)[] Of(RuntimeCheckResult result, string key) =>
        [.. result.Differences.Where(d => d.Lookup.Key == key).Select(d => (d.Lookup.Theme, d.Lookup.DisplayName, d.Kind))];

    private static (string Theme, string Variant, RuntimeDifferenceKind Kind)[] EverySetup(RuntimeDifferenceKind kind) =>
    [
        (Fixture, "Light", kind), (Fixture, "Dark", kind), (Fixture, "Contrast", kind),
        (WithAliases, "Light", kind), (WithAliases, "Dark", kind), (WithAliases, "Contrast", kind),
    ];

    /// <summary>The application's styles, the resources its <c>Styles</c> holds and its own, and the two sentinels.</summary>
    private static object?[] Snapshot(Application application) =>
    [
        .. application.Styles,
        application.Styles.Resources,
        application.Resources,
        application.Styles.Resources[TestApp.StylesSentinel],
        application.Resources[TestApp.ApplicationSentinel],
    ];

    private static string ProjectDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "XamlQuality.ThemeRuntime.Tests.csproj")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    /// <summary>A map's variants whose indexer throws, though a plan can be made from them.</summary>
    private sealed class BreakingVariants : IReadOnlyDictionary<string, IReadOnlyDictionary<string, ThemeVariant>>
    {
        public const string Message = "The map broke while the check ran.";

        private readonly Dictionary<string, IReadOnlyDictionary<string, ThemeVariant>> _inner = new()
        {
            [Fixture] = Variants(FixtureVariants.Contrast),
            [WithAliases] = Variants(FixtureVariants.Contrast),
        };

        public IReadOnlyDictionary<string, ThemeVariant> this[string key] => throw new InvalidOperationException(Message);

        public IEnumerable<string> Keys => _inner.Keys;

        public IEnumerable<IReadOnlyDictionary<string, ThemeVariant>> Values => _inner.Values;

        public int Count => _inner.Count;

        public bool ContainsKey(string key) => _inner.ContainsKey(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, ThemeVariant> value) =>
            _inner.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, IReadOnlyDictionary<string, ThemeVariant>>> GetEnumerator() => _inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
