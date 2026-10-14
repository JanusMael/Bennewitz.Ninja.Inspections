using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// The runtime theme check's library half, by plans/00003: the plan worked out from the audit's result
/// and a map's names, and the comparison of Avalonia's answers with the audit's. Over the committed
/// fixture under <c>Fixtures/audit</c>: a host theme that drops <c>HostAccent</c> under Dark, a compat
/// theme that extends it and merges a dictionary closing that gap, a consumer audited under both, and
/// one restricted to the host.
/// </summary>
public sealed class RuntimeCheckPlanTests : IDisposable
{
    private static readonly IReadOnlySet<string> Compat = new HashSet<string>(["host/Compat.axaml"]);

    private readonly string _root = Directory.CreateTempSubdirectory("theme-run-").FullName;
    private readonly AuditResult _audit = AuditRunner.Run(AuditConfig.Load(Path.Combine(FixturePaths.Fixture("audit"), "theme-audit.json")));

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void The_plan_sets_up_each_mapped_theme_and_variant_with_its_merges_and_the_audits_answer_for_each_key()
    {
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(_audit, Everything(), Compat);

        Assert.Equal(
            [("Host", "Light", ""), ("Host", "Dark", ""), ("Host + compat", "Light", "host/Compat.axaml"), ("Host + compat", "Dark", "host/Compat.axaml")],
            plan.Setups.Select(s => (s.Theme, s.DisplayName, string.Join(",", s.Merges))).ToArray());

        // Both consumers define AppOwn themselves, so it is never asked; HostAccent is missing under Dark only.
        Assert.Equal(
            [("App", "HostAccent", true), ("App", "NoSuchKey", false), ("Host-only", "HostAccent", true), ("Host-only", "NoSuchKey", false)],
            Lookups(plan, "Host", "Light"));
        Assert.Equal(
            [("App", "HostAccent", false), ("App", "NoSuchKey", false), ("Host-only", "HostAccent", false), ("Host-only", "NoSuchKey", false)],
            Lookups(plan, "Host", "Dark"));

        // The merged dictionary closes the gap; the consumer restricted to the host is not asked about the compat theme.
        Assert.Equal([("App", "HostAccent", true), ("App", "NoSuchKey", false)], Lookups(plan, "Host + compat", "Dark"));
        Assert.Empty(plan.Skipped);
    }

    [Fact]
    public void What_the_map_leaves_out_is_skipped_and_never_set_up()
    {
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(_audit,
            new Dictionary<string, IReadOnlySet<string>> { ["Host"] = new HashSet<string>(["Light"]) },
            new HashSet<string>());

        Assert.Equal([("Host", "Light")], plan.Setups.Select(s => (s.Theme, s.DisplayName)).ToArray());
        Assert.Equal(["Host, Dark", "Host + compat"], plan.Skipped.Select(s => s.Subject).ToArray());
        Assert.Contains("no ThemeVariant", plan.Skipped[0].Reason);
        Assert.Contains("no factory", plan.Skipped[1].Reason);
    }

    [Fact]
    public void A_theme_whose_merged_dictionary_the_map_does_not_name_is_not_checked_without_it()
    {
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(_audit, Everything(), new HashSet<string>());

        Assert.DoesNotContain(plan.Setups, s => s.Theme == "Host + compat");
        XamlSkip skip = Assert.Single(plan.Skipped);
        Assert.Equal("Host + compat", skip.Subject);
        Assert.Contains("host/Compat.axaml", skip.Reason);
    }

    [Fact]
    public void A_key_written_as_a_markup_extension_is_skipped_once_for_each_consumer_that_references_it()
    {
        Directory.CreateDirectory(Path.Combine(_root, "theme"));
        Directory.CreateDirectory(Path.Combine(_root, "app"));
        File.WriteAllText(Path.Combine(_root, "theme", "Theme.axaml"),
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light"><SolidColorBrush x:Key="Ink" Color="#000000" /></ResourceDictionary>
                <ResourceDictionary x:Key="Dark"><SolidColorBrush x:Key="Ink" Color="#FFFFFF" /></ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);
        File.WriteAllText(Path.Combine(_root, "app", "A.axaml"),
            """
            <Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style Selector="Border"><Setter Property="Background" Value="{DynamicResource Ink}" /></Style>
              <ControlTheme x:Key="Plain" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}" />
            </Styles>
            """);
        File.WriteAllText(Path.Combine(_root, "app", "B.axaml"),
            """
            <Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ControlTheme x:Key="Other" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}" />
            </Styles>
            """);
        AuditResult audit = AuditRunner.Run(AuditConfig.Parse(
            """
            {
              "themes": [ { "name": "Theme", "entry": "theme/Theme.axaml", "baseDirectory": "theme", "variants": ["Light", "Dark"] } ],
              "consumers": [ { "name": "App", "paths": ["app"] } ]
            }
            """, _root));

        RuntimeCheckPlan plan = RuntimeCheckPlan.For(audit,
            new Dictionary<string, IReadOnlySet<string>> { ["Theme"] = new HashSet<string>(["Light", "Dark"]) },
            new HashSet<string>());

        XamlSkip skip = Assert.Single(plan.Skipped);
        Assert.Equal("App: {x:Type Button}", skip.Subject);
        Assert.Equal("app/A.axaml", skip.RelativePath);
        Assert.Equal(3, skip.Line);
        Assert.All(plan.Setups, s => Assert.Equal(["Ink"], s.Lookups.Select(l => l.Key).ToArray()));
    }

    [Fact]
    public void A_theme_no_consumer_is_audited_under_is_neither_set_up_nor_named()
    {
        Directory.CreateDirectory(Path.Combine(_root, "theme"));
        Directory.CreateDirectory(Path.Combine(_root, "app"));
        File.WriteAllText(Path.Combine(_root, "theme", "Theme.axaml"),
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light"><SolidColorBrush x:Key="Ink" Color="#000000" /></ResourceDictionary>
                <ResourceDictionary x:Key="Dark"><SolidColorBrush x:Key="Ink" Color="#FFFFFF" /></ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);
        File.WriteAllText(Path.Combine(_root, "app", "A.axaml"),
            """
            <Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style Selector="Border"><Setter Property="Background" Value="{DynamicResource Ink}" /></Style>
            </Styles>
            """);
        AuditResult audit = AuditRunner.Run(AuditConfig.Parse(
            """
            {
              "themes": [
                { "name": "Theme", "entry": "theme/Theme.axaml", "baseDirectory": "theme", "variants": ["Light", "Dark"] },
                { "name": "Unused", "entry": "theme/Theme.axaml", "baseDirectory": "theme", "variants": ["Light", "Dark"] }
              ],
              "consumers": [ { "name": "App", "paths": ["app"], "themes": ["Theme"] } ]
            }
            """, _root));

        // The map names no factory for the theme the consumer is not audited under, and that is no skip:
        // there is nothing to ask under it.
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(audit,
            new Dictionary<string, IReadOnlySet<string>> { ["Theme"] = new HashSet<string>(["Light", "Dark"]) },
            new HashSet<string>());

        Assert.Equal([("Theme", "Light"), ("Theme", "Dark")], plan.Setups.Select(s => (s.Theme, s.DisplayName)).ToArray());
        Assert.Empty(plan.Skipped);
    }

    [Fact]
    public void Compare_names_each_kind_of_difference_in_the_plans_order_and_counts_the_answers()
    {
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(_audit, Everything(), Compat);
        List<RuntimeAnswer> answers = [];
        foreach (RuntimeLookup lookup in plan.Setups.SelectMany(s => s.Lookups))
        {
            // Avalonia agrees everywhere but in three places, each of a different kind.
            answers.Add((lookup.Consumer, lookup.Theme, lookup.DisplayName, lookup.Key) switch
            {
                ("App", "Host", "Dark", "HostAccent") => new RuntimeAnswer(lookup, Found: true),
                ("App", "Host + compat", "Dark", "HostAccent") => new RuntimeAnswer(lookup, Found: false),
                ("Host-only", "Host", "Light", "NoSuchKey") => new RuntimeAnswer(lookup, Found: false, "Static resource 'NoSuchKey' not found."),
                _ => new RuntimeAnswer(lookup, lookup.Defined),
            });
        }

        RuntimeCheckResult result = plan.Compare(answers, new Dictionary<RuntimeSetup, string>());

        Assert.Equal(
            [
                ("Host-only", "Host", "Light", "NoSuchKey", RuntimeDifferenceKind.LookupThrew),
                ("App", "Host", "Dark", "HostAccent", RuntimeDifferenceKind.UndefinedButFound),
                ("App", "Host + compat", "Dark", "HostAccent", RuntimeDifferenceKind.DefinedButNotFound),
            ],
            result.Differences.Select(d => (d.Lookup.Consumer, d.Lookup.Theme, d.Lookup.DisplayName, d.Lookup.Key, d.Kind)).ToArray());
        Assert.Equal("Static resource 'NoSuchKey' not found.", result.Differences[0].Message);
        Assert.Null(result.Differences[1].Message);
        Assert.Equal(answers.Count, result.Inspected);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void A_setup_that_did_not_load_or_left_a_lookup_unanswered_is_named_in_Skipped_and_counts_nothing()
    {
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(_audit, Everything(), Compat);
        RuntimeSetup notLoaded = plan.Setups[0];
        RuntimeSetup partly = plan.Setups[1];

        // Answers for the setup that did not load are not counted, and one lookup of the next goes unanswered.
        List<RuntimeAnswer> answers =
        [
            .. plan.Setups.SelectMany(s => s.Lookups).Where(l => l != partly.Lookups[0]).Select(l => new RuntimeAnswer(l, l.Defined)),
        ];

        RuntimeCheckResult result = plan.Compare(answers,
            new Dictionary<RuntimeSetup, string> { [notLoaded] = "The resource avares://Fixture/Gone.axaml could not be found.\nSecond line." });

        Assert.Empty(result.Differences);
        Assert.Equal(plan.Setups.Skip(1).Sum(s => s.Lookups.Count) - 1, result.Inspected);
        Assert.Equal(
            [
                "Host, Light: did not load, so none of its 4 lookups was made: The resource avares://Fixture/Gone.axaml could not be found. Second line.",
                "Host, Dark: 1 of its 4 lookups were not answered",
            ],
            result.Skipped.Select(s => s.ToString()).ToArray());
    }

    [Fact]
    public void A_skip_of_the_plan_reaches_the_result_ahead_of_the_setups_own()
    {
        RuntimeCheckPlan plan = RuntimeCheckPlan.For(_audit,
            new Dictionary<string, IReadOnlySet<string>> { ["Host"] = new HashSet<string>(["Light", "Dark"]) },
            new HashSet<string>());

        RuntimeCheckResult result = plan.Compare([], new Dictionary<RuntimeSetup, string>());

        Assert.Equal(0, result.Inspected);
        Assert.Equal(["Host + compat", "Host, Light", "Host, Dark"], result.Skipped.Select(s => s.Subject).ToArray());
    }

    [Fact]
    public void A_difference_reads_as_one_line()
    {
        RuntimeLookup lookup = new("App", "Semi", "Aquatic", "SemiColorWindow", Defined: true);

        Assert.Equal(
            "App under Semi, Aquatic: SemiColorWindow: the audit finds it defined, and Avalonia does not find it",
            new RuntimeDifference(lookup, RuntimeDifferenceKind.DefinedButNotFound).ToString());
        Assert.Equal(
            "App under Semi, Aquatic: SemiColorWindow: the audit finds it undefined, and Avalonia finds it",
            new RuntimeDifference(lookup with { Defined = false }, RuntimeDifferenceKind.UndefinedButFound).ToString());
        Assert.Equal(
            "App under Semi, Aquatic: SemiColorWindow: the lookup threw: Static resource 'X' not found. At line 3.",
            new RuntimeDifference(lookup, RuntimeDifferenceKind.LookupThrew, "Static resource 'X' not found.\r\nAt line 3.").ToString());
    }

    private static Dictionary<string, IReadOnlySet<string>> Everything() => new()
    {
        ["Host"] = new HashSet<string>(["Light", "Dark"]),
        ["Host + compat"] = new HashSet<string>(["Light", "Dark"]),
    };

    private static (string Consumer, string Key, bool Defined)[] Lookups(RuntimeCheckPlan plan, string theme, string variant) =>
        [.. plan.Setups.Single(s => s.Theme == theme && s.DisplayName == variant).Lookups.Select(l => (l.Consumer, l.Key, l.Defined))];
}
