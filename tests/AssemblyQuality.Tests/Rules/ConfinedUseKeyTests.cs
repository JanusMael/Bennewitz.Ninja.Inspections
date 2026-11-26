using System.Reflection;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;
using Xunit;

namespace AssemblyQuality.Tests.Rules;

/// <summary>
/// <c>BNAQ1010</c>'s option that allows a use by its key, plans/00011 decision 5, over the <c>Keyed</c> fixture's
/// <c>Lookups</c>, in both builds.
/// </summary>
public sealed class ConfinedUseKeyTests
{
    private const string Lookups = "Keyed.App.Lookups::";

    /// <summary>Both builds of the fixture, by their folder.</summary>
    public static TheoryData<string> Builds => new() { "keyed", "keyed-debug" };

    private static readonly Dictionary<string, Assembly> Loaded = new()
    {
        ["keyed"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "keyed", "AssemblyQuality.Fixtures.Keyed.dll")),
        ["keyed-debug"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "keyed-debug", "AssemblyQuality.Fixtures.KeyedDebug.dll")),
    };

    private static readonly ConfinedUse Lookup =
        ConfinedUse.Member("Avalonia.Controls.IResourceNode", "TryGetResource") with { IncludingSubtypes = true, KeyParameter = 0 };

    private static readonly ConfinedUse Extension =
        ConfinedUse.Constructing("Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension") with { KeyParameter = 0 };

    private static ConfinedUseReading Read(string build, params ConfinedUse[] confinements) =>
        new ConfinedUseRule(confinements).Read(AssemblyScanContext.Of(Loaded[build]));

    private static ConfinedUseMatch In(ConfinedUseReading reading, string method) =>
        Assert.Single(reading.Uses, u => u.Method == Lookups + method);

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_lookup_whose_key_traces_to_a_marked_parameter_directly_or_through_a_private_method_is_allowed(string build)
    {
        ConfinedUseReading reading = Read(build, Lookup, Extension);

        foreach (string method in new[] { "Directly", "Find", "AConstructionByItsKey" })
        {
            ConfinedUseMatch use = In(reading, method);
            Assert.True(use.Allowed, method);
            Assert.All(use.Keys, k => Assert.Equal(KeyValueKind.MarkedParameter, k.Kind));
        }

        Assert.Equal(Lookups + "ThroughAPrivateMethod(key)", Assert.Single(In(reading, "Find").Keys).Text);
        Assert.Empty(reading.Skipped);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_lookup_given_a_literal_an_unmarked_parameter_a_field_or_a_computed_key_is_reported_in_a_marked_method_or_not(string build)
    {
        ConfinedUseReading reading = Read(build, Lookup, Extension);

        Assert.Equal(KeyValueKind.Literal, Assert.Single(In(reading, "ALiteral").Keys).Kind);
        Assert.Equal(KeyValueKind.Literal, Assert.Single(In(reading, "ALiteralInAMarkedMethod").Keys).Kind);
        Assert.Equal(KeyValueKind.Literal, Assert.Single(In(reading, "AConstructionWithALiteral").Keys).Kind);
        Assert.Equal(KeyValueKind.Unknown, Assert.Single(In(reading, "AField").Keys).Kind);
        Assert.Equal(KeyValueKind.Unknown, Assert.Single(In(reading, "AComputedKey").Keys).Kind);
        Assert.Empty(In(reading, "AnUnmarkedParameter").Keys);
        foreach (string method in new[]
        {
            "ALiteral", "ALiteralInAMarkedMethod", "AConstructionWithALiteral", "AField", "AComputedKey", "AnUnmarkedParameter",
        })
        {
            Assert.False(In(reading, method).Allowed, method);
        }
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_lookup_given_a_marked_key_and_another_is_reported_naming_the_other(string build)
    {
        ConfinedUseMatch use = In(Read(build, Lookup), "FoundOrNot");
        Assert.False(use.Allowed);
        Assert.Equal([KeyValueKind.MarkedParameter, KeyValueKind.Unknown], use.Keys.Select(k => k.Kind).Order());

        AssemblyFinding finding = Assert.Single(new ConfinedUseRule([Lookup]).Analyze(AssemblyScanContext.Of(Loaded[build])).Findings,
            f => f.Subject.EndsWith(Lookups + "FoundOrNot", StringComparison.Ordinal));
        Assert.Contains("the field " + Lookups + "Field, in " + Lookups + "Refresh", finding.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Resolve(key)", finding.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void Without_the_option_no_key_is_read_and_none_allows_a_use(string build)
    {
        ConfinedUseReading reading = Read(build, Lookup with { KeyParameter = null });

        Assert.All(reading.Uses, u => Assert.Empty(u.Keys));
        Assert.All(reading.Uses, u => Assert.False(u.Allowed));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_place_allows_a_use_its_key_does_not(string build)
    {
        ConfinedUseReading reading = Read(build, Lookup with { AllowedInTypes = ["Keyed.App.Lookups"] });

        Assert.All(reading.Uses, u => Assert.True(u.Allowed));
        Assert.Equal(KeyValueKind.Literal, Assert.Single(In(reading, "ALiteral").Keys).Kind);
    }

    [Fact]
    public void A_key_parameter_is_never_negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Lookup with { KeyParameter = -1 });
    }
}
