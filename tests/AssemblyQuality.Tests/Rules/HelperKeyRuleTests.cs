using System.Reflection;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;
using Xunit;

namespace AssemblyQuality.Tests.Rules;

/// <summary>
/// <c>BNAQ1011</c>, keys through marked helpers, by plans/00011, over the <c>Keyed</c> fixture compiled optimised with
/// an embedded PDB (<c>keyed/</c>) and unoptimised with its PDB beside it (<c>keyed-debug/</c>), and over
/// <c>KeyedOdd</c>, whose marked parameters cannot be read.
/// </summary>
public sealed class HelperKeyRuleTests
{
    private const string App = "Keyed.App.";

    /// <summary>Both builds of the fixture, by their folder.</summary>
    public static TheoryData<string> Builds => new() { "keyed", "keyed-debug" };

    private static readonly Dictionary<string, Assembly> Loaded = new()
    {
        ["keyed"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "keyed", "AssemblyQuality.Fixtures.Keyed.dll")),
        ["keyed-debug"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "keyed-debug", "AssemblyQuality.Fixtures.KeyedDebug.dll")),
        ["keyed-odd"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "keyed-odd", "AssemblyQuality.Fixtures.KeyedOdd.dll")),
    };

    private static AssemblyScanContext Scan(string build) => AssemblyScanContext.Of(Loaded[build]);

    private static HelperKeyReading Read(string build) => new HelperKeyRule().Read(Scan(build));

    private static AssemblyRuleResult Analyze(string build) => new HelperKeyRule().Analyze(Scan(build));

    /// <summary>The values the one call from <paramref name="method"/> to <paramref name="helper"/> takes, by kind and text.</summary>
    private static string[] ValuesFrom(HelperKeyReading reading, string method, string helper = "Palette::Brush") =>
        [.. Assert.Single(reading.Calls, c => c.Method == App + method && c.Helper == App + helper)
            .Values.Select(v => $"{v.Kind} {v.Text.Replace(App, string.Empty, StringComparison.Ordinal)}")];

    [Theory]
    [MemberData(nameof(Builds))]
    public void Each_path_a_key_takes_is_followed(string build)
    {
        HelperKeyReading reading = Read(build);

        Assert.Equal(["Field Colours::Accent"], ValuesFrom(reading, "Painter::AConstant"));
        Assert.Equal(["Field Colours::Danger"], ValuesFrom(reading, "Painter::ALocal"));
        Assert.Equal(["Field Colours::Calm", "Field Colours::Quiet"], ValuesFrom(reading, "Painter::ATernary"));
        Assert.Equal(["Field Colours::Warning", "Field Colours::Info"], ValuesFrom(reading, "Painter::ATernaryOfTuples"));
        Assert.Equal(["Field Colours::Paired"], ValuesFrom(reading, "Painter::ATuple"));
        Assert.Equal(["Field Colours::Captured"], ValuesFrom(reading, "Painter::ACapturedLocal"));
        Assert.Equal(["Field Colours::Local"], ValuesFrom(reading, "Painter::ALocalFunction"));
        Assert.Equal(["Field Colours::Deep"], ValuesFrom(reading, "Painter::Inner"));
        Assert.Equal(["Field Colours::Eighth"], ValuesFrom(reading, "Painter::Depth8"));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_field_that_reaches_the_helper_only_through_a_parameter_is_passed(string build)
    {
        HelperKeyReading reading = Read(build);

        KeyValue value = Assert.Single(Assert.Single(reading.Calls, c => c.Method == App + "Painter::Glyph").Values);
        Assert.Equal(KeyValueKind.Field, value.Kind);
        Assert.Equal(App + "Colours::Subtle", value.Text);
        Assert.Equal(App + "Painter::AParameter", value.Method);
        Assert.DoesNotContain(Analyze(build).Findings, f => f.Subject == App + "Colours::Subtle");
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_literal_that_spells_a_fields_value_reads_as_that_field(string build)
    {
        Assert.Equal(["Field Colours::Spelt"], ValuesFrom(Read(build), "Painter::ALiteral"));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void The_attribute_is_found_by_its_full_name_here_and_in_a_referenced_assembly_each_declaring_it(string build)
    {
        HelperKeyReading reading = Read(build);

        Assert.Contains(reading.Parameters, p => p.Method == App + "Palette::Brush" && p.KeyClass == App + "Colours");
        HelperKeyCall library = Assert.Single(reading.Calls, c => c.Helper == "Keyed.Library.LibraryPalette::Paint");
        Assert.Equal("Keyed.Library.LibraryKeys", library.KeyClass);
        Assert.Equal("Keyed.Library.LibraryKeys::Frame", Assert.Single(library.Values).Text);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_key_class_nested_in_another_is_read(string build)
    {
        HelperKeyReading reading = Read(build);

        MarkedParameter shade = Assert.Single(reading.Parameters, p => p.Method == App + "Palette::Shade");
        Assert.Equal(App + "Colours+Nested", shade.KeyClass);
        Assert.Equal(["Inner"], shade.Fields);
        Assert.Equal(["Field Colours+Nested::Inner"], ValuesFrom(reading, "Painter::ANestedClass", "Palette::Shade"));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void Two_marked_helpers_naming_one_class_share_its_keys(string build)
    {
        AssemblyRuleResult result = Analyze(build);

        Assert.Equal(["Field Colours::Tinted"], ValuesFrom(Read(build), "Painter::ASecondHelper", "Palette::Tint"));
        Assert.DoesNotContain(result.Findings, f => f.Subject.StartsWith(App + "Colours::", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_const_passed_nowhere_is_reported(string build)
    {
        AssemblyFinding finding = Assert.Single(Analyze(build).Findings, f => f.Subject == App + "Faults::Unused");
        Assert.Contains("reaches no parameter marked", finding.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void Each_key_that_is_no_field_or_cannot_be_followed_is_reported(string build)
    {
        string[] reported = [.. Analyze(build).Findings
            .Where(f => f.Subject.StartsWith(App + "FaultyPalette::Brush(key) from ", StringComparison.Ordinal))
            .Select(f => f.Subject[(App + "FaultyPalette::Brush(key) from " + App).Length..])
            .Order(StringComparer.Ordinal)];

        Assert.Equal(
        [
            "Exposed::Paint", "Faulty::ADelegatesKey", "Faulty::AField", "Faulty::ALambdasParameter",
            "Faulty::AnotherClassesKey", "Faulty::Computed", "Faulty::Depth9", "Faulty::MadeIntoADelegate",
            "Faulty::NoField", "Faulty::Relay", "Overriding::Paint",
        ], reported);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void What_cannot_be_followed_says_why(string build)
    {
        HelperKeyReading reading = Read(build);

        string Why(string method) =>
            Assert.Single(Assert.Single(reading.Calls, c => c.Method == App + method && c.Kind == UseKind.Call).Values).Text;

        Assert.Equal("the field " + App + "Faulty::FromAField", Why("Faulty::AField"));
        Assert.Equal("what System.String::Concat returns", Why("Faulty::Computed"));
        Assert.Equal("what a delegate returns", Why("Faulty::ADelegatesKey"));
        Assert.StartsWith("the parameter key of a lambda", Why("Faulty::ALambdasParameter"), StringComparison.Ordinal);
        Assert.Contains("visible outside its assembly", Why("Exposed::Paint"), StringComparison.Ordinal);
        Assert.Contains("overrides or implements another", Why("Overriding::Paint"), StringComparison.Ordinal);
        Assert.Contains("of a method made into a delegate", Why("Faulty::Relay"), StringComparison.Ordinal);
        Assert.Contains($"followed through {8} callers and no further", Why("Faulty::Depth9"), StringComparison.Ordinal);
        Assert.Equal(KeyValueKind.Literal,
            Assert.Single(Assert.Single(reading.Calls, c => c.Method == App + "Faulty::NoField").Values).Kind);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_marked_helper_made_into_a_delegate_is_reported(string build)
    {
        HelperKeyCall made = Assert.Single(Read(build).Calls, c => c.Kind == UseKind.Delegate);
        Assert.Equal(App + "Faulty::MadeIntoADelegate", made.Method);
        Assert.Empty(made.Values);
        Assert.Single(Analyze(build).Findings, f => f.Message.Contains("so no call shows which keys", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_marked_parameter_of_another_class_is_reported(string build)
    {
        AssemblyFinding finding = Assert.Single(Analyze(build).Findings,
            f => f.Subject.EndsWith("Faulty::AnotherClassesKey", StringComparison.Ordinal));
        Assert.Contains("which takes keys of " + App + "Colours", finding.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_finding_says_where_the_key_arises(string build)
    {
        AssemblyFinding finding = Assert.Single(Analyze(build).Findings,
            f => f.Subject.EndsWith("Faulty::NoField", StringComparison.Ordinal));
        SourceLine source = Assert.IsType<SourceLine>(finding.Source);
        Assert.EndsWith("Keys.cs", source.File, StringComparison.Ordinal);
        Assert.True(source.Line > 0);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void It_inspects_each_call_to_a_marked_parameter_and_skips_nothing_it_can_read(string build)
    {
        HelperKeyReading reading = Read(build);

        Assert.Equal(reading.Calls.Count(c => c.Kind != UseKind.Delegate), reading.Inspected);
        Assert.Equal(25, reading.Inspected);
        Assert.Empty(reading.Skipped);
    }

    [Fact]
    public void An_attribute_whose_argument_is_no_single_resolving_type_is_skipped()
    {
        Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName("AssemblyQuality.Fixtures.Absent")));

        HelperKeyReading reading = Read("keyed-odd");

        foreach (string helper in new[] { "NoType", "NoArgument", "ANamedArgument", "Unresolved" })
        {
            Assert.Single(reading.Skipped, s => s.StartsWith($"Keyed.Odd.Helpers::{helper}(key) carries [ResourceKey] without one type",
                StringComparison.Ordinal));
        }

        Assert.DoesNotContain(reading.Calls, c => c.Helper == "Keyed.Odd.Helpers::NoType");
    }

    [Fact]
    public void A_parameter_that_is_no_string_a_class_with_no_const_string_and_two_fields_of_one_value_are_skipped()
    {
        AssemblyRuleResult result = Analyze("keyed-odd");

        Assert.Single(result.Skipped, s => s.StartsWith("Keyed.Odd.Helpers::NotAString(key) carries [ResourceKey] but takes System.Int32",
            StringComparison.Ordinal));
        Assert.Single(result.Skipped, s => s.StartsWith("Keyed.Odd.Helpers::NoConstantString(key) names Keyed.Odd.NoConstants, which holds no const string field",
            StringComparison.Ordinal));
        Assert.Single(result.Skipped, s => s.StartsWith("Keyed.Odd.Twins: First and Second hold one value", StringComparison.Ordinal));
        Assert.Equal(7, result.Skipped.Count);
        Assert.Equal(2, result.Inspected);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void With_no_marked_parameter_it_inspects_nothing()
    {
        Assembly confined = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "confined", "AssemblyQuality.Fixtures.Confined.dll"));

        AssemblyRuleResult result = new HelperKeyRule().Analyze(AssemblyScanContext.Of(confined));

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }
}
