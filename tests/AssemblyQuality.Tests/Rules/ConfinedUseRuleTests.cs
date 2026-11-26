using System.Reflection;
using System.Runtime.Loader;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;
using Xunit;

namespace AssemblyQuality.Tests.Rules;

/// <summary>
/// <c>BNAQ1010</c>, confined use, by plans/00011, over the <c>Confined</c> fixture: one source compiled optimised
/// with an embedded PDB (<c>confined/</c>) and unoptimised with its PDB beside it (<c>confined-debug/</c>).
/// </summary>
public sealed class ConfinedUseRuleTests
{
    private const string Color = "Avalonia.Media.Color";
    private const string Window = "Avalonia.Controls.Window";
    private const string Outside = "Confined.App.Outside";

    /// <summary>Both builds of the fixture, by their folder.</summary>
    public static TheoryData<string> Builds => new() { "confined", "confined-debug" };

    private static readonly Dictionary<string, Assembly> Loaded = new()
    {
        ["confined"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "confined", "AssemblyQuality.Fixtures.Confined.dll")),
        ["confined-debug"] = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "confined-debug", "AssemblyQuality.Fixtures.ConfinedDebug.dll")),
    };

    private static AssemblyScanContext Scan(string build) => AssemblyScanContext.Of(Loaded[build]);

    private static ConfinedUseReading Read(string build, params ConfinedUse[] confinements) =>
        new ConfinedUseRule(confinements).Read(Scan(build));

    [Theory]
    [MemberData(nameof(Builds))]
    public void Every_kind_of_body_is_read_and_credited_to_the_method_the_source_wrote(string build)
    {
        ConfinedUseReading reading = Read(build, ConfinedUse.Constructing(Color));

        string[] methods = [.. reading.Uses
            .Where(u => u.Method.StartsWith(Outside + "::", StringComparison.Ordinal) && u.Kind == UseKind.Construction)
            .Select(u => u.Method["Confined.App.Outside::".Length..])
            .Distinct()
            .Order(StringComparer.Ordinal)];

        Assert.Equal(
            [".cctor", "ADefault", "InACapturingLambda", "InALambda", "InALocalFunction", "InAMethod", "InAnAsyncMethod",
             "InAnIterator", "InAnIteratorsFinally", "get_InAGetter"],
            methods);
        Assert.All(reading.Uses.Where(u => u.Method.StartsWith(Outside, StringComparison.Ordinal)),
            u => Assert.EndsWith("Uses.cs", u.Source?.File ?? "(none)", StringComparison.Ordinal));
        Assert.Empty(reading.Skipped);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_use_inside_a_lambda_is_one_use(string build)
    {
        ConfinedUseReading reading = Read(build, ConfinedUse.Constructing(Color));

        Assert.Single(reading.Uses, u => u.Method == Outside + "::InALambda");
        Assert.Single(reading.Uses, u => u.Method == Outside + "::InAnAsyncMethod");
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_base_constructor_chain_is_listed_and_never_reported(string build)
    {
        var confinement = ConfinedUse.Constructing(Window) with { IncludingSubtypes = true, ExceptTypesTheScanDeclares = true };
        ConfinedUseReading reading = new ConfinedUseRule([confinement]).Read(Scan(build));
        AssemblyRuleResult result = new ConfinedUseRule([confinement]).Analyze(Scan(build));

        ConfinedUseMatch chain = Assert.Single(reading.Uses, u => u.Kind == UseKind.Chain);
        Assert.Equal("Confined.App.DialogWindow::.ctor", chain.Method);
        Assert.True(chain.Allowed);
        AssemblyFinding finding = Assert.Single(result.Findings);
        Assert.Equal("Avalonia.Controls.Window::.ctor in Confined.App.Opener::OpenForeign", finding.Subject);
        Assert.True(result.Inspected > 0);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void The_subtype_and_declared_type_options_each_change_what_is_matched(string build)
    {
        string[] Constructed(ConfinedUse confinement) => [.. Read(build, confinement).Uses
            .Where(u => u.Kind == UseKind.Construction)
            .Select(u => u.Method)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(["Confined.App.Opener::OpenForeign"], Constructed(ConfinedUse.Constructing(Window)));
        Assert.Equal(["Confined.App.Opener::OpenDeclared", "Confined.App.Opener::OpenForeign"],
            Constructed(ConfinedUse.Constructing(Window) with { IncludingSubtypes = true }));
        Assert.Equal(["Confined.App.Opener::OpenForeign"],
            Constructed(ConfinedUse.Constructing(Window) with { IncludingSubtypes = true, ExceptTypesTheScanDeclares = true }));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void An_interface_reaches_what_implements_it_only_with_the_subtype_option(string build)
    {
        string[] Brushes(bool subtypes) => [.. Read(build, ConfinedUse.Constructing("Avalonia.Media.IBrush") with { IncludingSubtypes = subtypes })
            .Uses.Select(u => u.Method).Distinct().Order(StringComparer.Ordinal)];

        Assert.Empty(Brushes(subtypes: false));
        Assert.Contains("Confined.App.Palette::Brush", Brushes(subtypes: true));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_call_on_a_type_confined_by_its_construction_is_no_use(string build)
    {
        ConfinedUseReading reading = Read(build, ConfinedUse.Constructing(Window));

        Assert.DoesNotContain(reading.Uses, u => u.Method == "Confined.App.Opener::ShowIt");
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_member_named_on_an_interface_matches_a_class_that_implements_it_only_with_the_subtype_option(string build)
    {
        string[] Lookups(bool subtypes) => [.. Read(build,
                ConfinedUse.Member("Avalonia.Controls.IResourceNode", "TryGetResource") with { IncludingSubtypes = subtypes })
            .Uses.Select(u => u.Method).Order(StringComparer.Ordinal)];

        Assert.Equal(["Confined.App.Lookups::ThroughTheInterface"], Lookups(subtypes: false));
        Assert.Equal(["Confined.App.Lookups::ThroughTheClass", "Confined.App.Lookups::ThroughTheInterface"], Lookups(subtypes: true));
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void Each_kind_of_use_is_read(string build)
    {
        Assembly standIns = Assembly.LoadFrom(
            Path.Combine(AppContext.BaseDirectory, build, "AssemblyQuality.Fixtures.ConfinedStandIns.dll"));
        ConfinedUseReading reading = new ConfinedUseRule(
        [
            ConfinedUse.AnyMemberOf("Avalonia.Media.Colors"),
            ConfinedUse.AnyMemberOf("Avalonia.Media.KnownColors"),
            ConfinedUse.Member(Color, "Parse"),
        ]).Read(AssemblyScanContext.Of(Loaded[build], standIns));

        Assert.Contains(reading.Uses, u => u.Method == Outside + "::ANamedColor" && u.Kind == UseKind.Read
            && u.Member == "Avalonia.Media.Colors::Red");
        Assert.Contains(reading.Uses, u => u.Method == "Avalonia.Media.Colors::get_Black" && u.Kind == UseKind.Read
            && u.Member == "Avalonia.Media.KnownColors::Black");
        Assert.Contains(reading.Uses, u => u.Method == Outside + "::AMethodGroup" && u.Kind == UseKind.Delegate
            && u.Member == "Avalonia.Media.Color::Parse");
        Assert.Contains(reading.Uses, u => u.Method == "Confined.App.Palette::Parsed" && u.Kind == UseKind.Call);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_use_is_allowed_by_its_type_and_the_types_nested_in_it_by_its_method_and_by_its_file(string build)
    {
        string[] palette = ["Confined.App.Palette"];
        AssemblyRuleResult byType = new ConfinedUseRule(
        [
            ConfinedUse.AnyMemberOf("Avalonia.Media.Colors") with { AllowedInTypes = palette },
        ]).Analyze(Scan(build));
        AssemblyRuleResult byMethod = new ConfinedUseRule(
        [
            ConfinedUse.Constructing("Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension")
                with { AllowedInMethods = ["Confined.App.Lookups::Brush"] },
        ]).Analyze(Scan(build));
        AssemblyRuleResult byFile = new ConfinedUseRule(
        [
            ConfinedUse.Constructing("Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension")
                with { AllowedInMarkup = ["uses.cs"] },
        ]).Analyze(Scan(build));

        Assert.Equal([Outside + "::ANamedColor"],
            byType.Findings.Select(f => f.Subject[(f.Subject.IndexOf(" in ", StringComparison.Ordinal) + 4)..]).Distinct());
        Assert.Empty(byMethod.Findings);
        Assert.Empty(byFile.Findings);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void A_finding_says_where_in_the_source_it_is(string build)
    {
        AssemblyRuleResult result = new ConfinedUseRule([ConfinedUse.Constructing(Window)]).Analyze(Scan(build));

        AssemblyFinding finding = Assert.Single(result.Findings);
        SourceLine source = Assert.IsType<SourceLine>(finding.Source);
        Assert.EndsWith("Uses.cs", source.File, StringComparison.Ordinal);
        Assert.True(source.Line > 0);
        Assert.Contains($"({source})", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Unconfigured_it_inspects_nothing()
    {
        AssemblyRuleResult result = new ConfinedUseRule().Analyze(Scan("confined"));

        Assert.Empty(result.Findings);
        Assert.Equal(0, result.Inspected);
    }

    [Fact]
    public void A_name_that_resolves_nowhere_is_skipped_once_however_many_assemblies_are_scanned()
    {
        AssemblyScanContext both = AssemblyScanContext.Of(Loaded["confined"], Loaded["confined-debug"]);
        AssemblyRuleResult result = new ConfinedUseRule(
        [
            ConfinedUse.Constructing("Avalonia.Media.Colour"),
            ConfinedUse.Member(Color, "Parsed"),
        ]).Analyze(both);

        Assert.Single(result.Skipped, s => s.Contains("Avalonia.Media.Colour resolves to no type", StringComparison.Ordinal));
        Assert.Single(result.Skipped, s => s.Contains("declares no member named Parsed", StringComparison.Ordinal));
        Assert.Equal(2, result.Skipped.Count);
    }

    [Fact]
    public void A_reference_a_match_needs_that_does_not_resolve_is_skipped()
    {
        Assembly orphan = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "orphan", "AssemblyQuality.Fixtures.Orphan.dll"));
        Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName("AssemblyQuality.Fixtures.Absent")));

        AssemblyRuleResult result = new ConfinedUseRule(
        [
            ConfinedUse.Constructing("Orphan.Absent.Readable") with { IncludingSubtypes = true },
        ]).Analyze(AssemblyScanContext.Of(orphan));

        Assert.Contains(result.Skipped, s => s.Contains("could not be decided", StringComparison.Ordinal)
            && s.Contains("Absent.Base", StringComparison.Ordinal));
    }

    [Fact]
    public void An_assembly_with_no_file_is_skipped()
    {
        var context = new AssemblyLoadContext("no-file");
        using FileStream image = File.OpenRead(Loaded["confined"].Location);
        Assembly fromBytes = context.LoadFromStream(image);

        AssemblyRuleResult result = new ConfinedUseRule([ConfinedUse.Constructing(Window)]).Analyze(AssemblyScanContext.Of(fromBytes));

        Assert.Contains(result.Skipped, s => s.Contains("has no file to read", StringComparison.Ordinal));
        Assert.Equal(0, result.Inspected);
    }
}
