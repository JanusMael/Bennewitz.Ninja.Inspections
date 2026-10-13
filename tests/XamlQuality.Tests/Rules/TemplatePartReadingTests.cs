using System.Text.RegularExpressions;
using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.Rules;

namespace XamlQuality.Tests.Rules;

/// <summary>Looks its part up in its own <c>OnApplyTemplate</c>, which its template is applied through.</summary>
public sealed class OwnLookupHost : HandedTemplatedControl
{
    /// <inheritdoc />
    protected override void OnApplyTemplate(HandedTemplateArgs e) => _ = e.NameScope.Find("PART_Own");
}

/// <summary>Looks its part up in a lambda its <c>OnApplyTemplate</c> declares, which compiles into a closure.</summary>
public sealed class LambdaApplyHost : HandedTemplatedControl
{
    /// <inheritdoc />
    protected override void OnApplyTemplate(HandedTemplateArgs e)
    {
        Func<object?> find = () => e.NameScope.Find("PART_Deferred");
        _ = find();
    }
}

/// <summary>
/// Declares and looks its parts up out of their names' order, looks one up again in a method of its own, and hands its
/// template to a helper that looks the other up again, so that its reading has an order to keep by part, by type and by
/// method.
/// </summary>
public sealed class UnorderedLookupHost : HandedTemplatedControl
{
    /// <summary>Declared first, and last by name.</summary>
    public const string Last = "PART_Zulu";

    /// <summary>Declared second, and first by name.</summary>
    public const string First = "PART_Alpha";

    /// <inheritdoc />
    protected override void OnApplyTemplate(HandedTemplateArgs e)
    {
        _ = e.NameScope.Find("PART_Zulu");
        _ = e.NameScope.Find("PART_Alpha");
        AlphaFinder.Seek(e);
        Again(e);
    }

    /// <summary>Looks a part up again, in a method declared after <c>OnApplyTemplate</c> and named before it.</summary>
    private static void Again(HandedTemplateArgs e) => _ = e.NameScope.Find("PART_Zulu");
}

/// <summary>
/// Finds a part that the control handing it the template looks up too, in a type named before that control's and a
/// method named after its <c>OnApplyTemplate</c>.
/// </summary>
public static class AlphaFinder
{
    /// <summary>Finds the part.</summary>
    /// <param name="e">A control's applied template.</param>
    public static void Seek(HandedTemplateArgs e) => _ = e.NameScope.Find("PART_Alpha");
}

/// <summary>A generic base class of the consumer's own that looks its subclasses' parts up.</summary>
/// <typeparam name="T">Anything: only that the base is generic matters.</typeparam>
public abstract class GenericLookingBase<T> : HandedTemplatedControl
{
    /// <inheritdoc />
    protected override void OnApplyTemplate(HandedTemplateArgs e) => _ = e.NameScope.Find("PART_Generic");
}

/// <summary>Derives from a construction of a generic base, whose <c>OnApplyTemplate</c> runs on this control's template.</summary>
public sealed class GenericInheritingHost : GenericLookingBase<int>;

/// <summary>Hands its template to a helper it nests, which is a type of its own.</summary>
public sealed class NestedHelperHost : HandedTemplatedControl
{
    /// <inheritdoc />
    protected override void OnApplyTemplate(HandedTemplateArgs e) => Wirer.Wire(e);

    /// <summary>Finds a part on the template it is handed.</summary>
    private static class Wirer
    {
        /// <summary>Finds the part.</summary>
        /// <param name="e">The control's applied template.</param>
        public static void Wire(HandedTemplateArgs e) => _ = e.NameScope.Find("PART_Nested");
    }
}

/// <summary>A base class that hands its subclasses' template to a helper it nests.</summary>
public abstract class NestingBase : HandedTemplatedControl
{
    /// <inheritdoc />
    protected override void OnApplyTemplate(HandedTemplateArgs e) => Inner.Wire(e);

    /// <summary>Finds a part on the template it is handed.</summary>
    private static class Inner
    {
        /// <summary>Finds the part.</summary>
        /// <param name="e">A control's applied template.</param>
        public static void Wire(HandedTemplateArgs e) => _ = e.NameScope.Find("PART_InnerWired");
    }
}

/// <summary>Inherits its base's <c>OnApplyTemplate</c>, and so the helper the base nests.</summary>
public sealed class NestingBaseHost : NestingBase;

/// <summary>Nests a type whose lookup no control hands a template to.</summary>
public sealed class NestedFinderPanel
{
    /// <summary>Looks a part up on a name scope of its own.</summary>
    private static class Finder
    {
        /// <summary>Finds the part.</summary>
        /// <returns>Whatever the lookup returns.</returns>
        public static object? Locate() => FakeNameScope.Find("PART_Located");
    }
}

/// <summary>
/// <see cref="TemplatePartRule.ReadParts"/> over markup and types written for the test.
/// </summary>
/// <remarks>
/// ⭐ The reading is what the rule checks, so <see cref="TheRulesFindings_AreThePartsTheReadingSaysEachThemeLacks"/>
/// holds the rule to it: every finding is a part the reading says a theme lacks, and every part a theme lacks is a
/// finding.
/// </remarks>
public sealed class TemplatePartReadingTests : IDisposable
{
    private const string Header =
        "xmlns=\"https://github.com/avaloniaui\" " +
        "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
        "xmlns:t=\"clr-namespace:XamlQuality.Tests.Rules\"";

    private static readonly string TestAssembly = typeof(ProxyPanel).Assembly.GetName().Name!;

    private static readonly Regex Finding = new("^(?<control>\\S+) looks up the template part '(?<part>[^']+)'");

    private readonly string _root = Directory.CreateTempSubdirectory("xq-").FullName;

    public void Dispose()
    {
        if (Directory.Exists(_root)) { Directory.Delete(_root, recursive: true); }
    }

    private void Write(string name, string markup) =>
        File.WriteAllText(Path.Combine(_root, name), markup);

    private XamlScanContext Scan() =>
        XamlScanContext.Load(_root).WithAssemblies(typeof(ProxyPanel).Assembly);

    private TemplatePartReading Read() => TemplatePartRule.ReadParts(Scan());

    private static ControlTemplateParts Entry(TemplatePartReading reading, string name) =>
        Assert.Single(reading.Controls, control => control.Name == name);

    private static string FullName(Type type) => type.FullName!;

    /// <summary>A <c>ControlTheme</c> for <paramref name="target"/> whose template holds <paramref name="inner"/>, on one line.</summary>
    private static string ThemeOf(string target, string inner) =>
        $"<ControlTheme x:Key=\"k\" TargetType=\"{target}\">"
        + $"<Setter Property=\"Template\"><ControlTemplate>{inner}</ControlTemplate></Setter>"
        + "</ControlTheme>";

    private static string Dictionary(params string[] lines) =>
        string.Join("\n", [$"<ResourceDictionary {Header}>", .. lines, "</ResourceDictionary>"]);

    // ── Where each lookup comes from ────────────────────────────────────────────

    /// <summary>
    /// A lookup the control's own code makes is its own, through the <c>OnApplyTemplate</c> its template is applied
    /// through and in a method no control hands a template to alike.
    /// </summary>
    [Fact]
    public void ALookupInTheControlsOwnCode_IsOwn()
    {
        TemplatePartReading reading = Read();

        Assert.Equal(
            [new TemplatePartLookup("PART_Own", TemplatePartOrigin.Own, FullName(typeof(OwnLookupHost)), "OnApplyTemplate", TestAssembly)],
            Entry(reading, nameof(OwnLookupHost)).Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_Inline", TemplatePartOrigin.Own, FullName(typeof(InlineLookupPanel)), "ApplyTemplate", TestAssembly)],
            Entry(reading, nameof(InlineLookupPanel)).Lookups);
    }

    /// <summary>
    /// A lambda the control declares is its own code, named by the method the compiler gives it, which tells two
    /// lambdas in one method apart, and by the control rather than the closure the compiler nests in it.
    /// </summary>
    [Fact]
    public void ALookupInALambdaTheControlDeclares_IsOwn_UnderItsCompiledName()
    {
        TemplatePartLookup lookup = Assert.Single(Entry(Read(), nameof(LambdaApplyHost)).Lookups);

        Assert.Equal("PART_Deferred", lookup.Part);
        Assert.Equal(TemplatePartOrigin.Own, lookup.Origin);
        Assert.Equal(FullName(typeof(LambdaApplyHost)), lookup.Type);
        Assert.StartsWith("<OnApplyTemplate>b__", lookup.Method, StringComparison.Ordinal);
        Assert.Equal(TestAssembly, lookup.Assembly);
    }

    /// <summary>
    /// A base class's <c>OnApplyTemplate</c> runs on its subclass's template, so its lookup is the subclass's, as the
    /// base class's code; the base class's own entry has it as its own.
    /// </summary>
    [Fact]
    public void ALookupInABaseClassesOnApplyTemplate_IsTheBaseClasses()
    {
        TemplatePartReading reading = Read();

        Assert.Equal(
            [new TemplatePartLookup("PART_FromBase", TemplatePartOrigin.BaseClass, FullName(typeof(LookingBase)), "OnApplyTemplate", TestAssembly)],
            Entry(reading, nameof(InheritingHost)).Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_FromBase", TemplatePartOrigin.Own, FullName(typeof(LookingBase)), "OnApplyTemplate", TestAssembly)],
            Entry(reading, nameof(LookingBase)).Lookups);
    }

    /// <summary>
    /// A generic base class is a base of the control derived from a construction of it, and is named as the generic
    /// type it declares.
    /// </summary>
    [Fact]
    public void ALookupInAGenericBaseClass_IsTheBaseClasses()
    {
        Assert.Equal(
            [new TemplatePartLookup("PART_Generic", TemplatePartOrigin.BaseClass, FullName(typeof(GenericLookingBase<>)), "OnApplyTemplate", TestAssembly)],
            Entry(Read(), nameof(GenericInheritingHost)).Lookups);
    }

    /// <summary>
    /// ⭐ DiffView's case: a control derives from a library's, whose <c>OnApplyTemplate</c> looks its parts up. The
    /// lookup names the library's assembly, which is what tells a consumer the part is the library's to keep.
    /// </summary>
    [Fact]
    public void ALookupInABaseClassInAnotherAssembly_NamesThatAssembly()
    {
        TemplatePartReading reading = TemplatePartRule.ReadParts(
            XamlScanContext.Load(_root).WithAssemblies(EmittedTemplateBases.Bases, EmittedTemplateBases.Hosts));

        Assert.Empty(reading.Skipped);
        Assert.Equal(
            [new TemplatePartLookup("PART_Emitted", TemplatePartOrigin.BaseClass, "Fixtures.EmittedLookingBase", "OnApplyTemplate", "PartBases")],
            Entry(reading, "EmittedHost").Lookups);
    }

    /// <summary>
    /// A lookup a helper makes on a template the control hands it, whole or as its name scope, down a chain or into a
    /// constructor, is the control's, as the helper's code.
    /// </summary>
    [Fact]
    public void ALookupInAHelperTheControlHandsItsTemplate_IsTheHelpers()
    {
        TemplatePartReading reading = Read();
        string helper = FullName(typeof(PartLookupHelper));

        Assert.Equal(
            [
                new TemplatePartLookup("PART_Handed", TemplatePartOrigin.Helper, helper, "Apply", TestAssembly),
                new TemplatePartLookup("PART_Shared", TemplatePartOrigin.Helper, helper, "Apply", TestAssembly),
            ],
            Entry(reading, nameof(HandingHost)).Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_Scoped", TemplatePartOrigin.Helper, FullName(typeof(ScopeHelper)), "Apply", TestAssembly)],
            Entry(reading, nameof(ScopeHandingHost)).Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_Chained", TemplatePartOrigin.Helper, FullName(typeof(ChainSecond)), "Apply", TestAssembly)],
            Entry(reading, nameof(ChainHost)).Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_Constructed", TemplatePartOrigin.Helper, FullName(typeof(ConstructedHelper)), ".ctor", TestAssembly)],
            Entry(reading, nameof(ConstructingHost)).Lookups);
    }

    /// <summary>
    /// A type the control nests, or a base class nests, is a type of its own: a lookup it makes on the template the
    /// control hands it is a helper's, named as that type, which declares the method.
    /// </summary>
    [Fact]
    public void ALookupInATypeTheControlNests_IsAHelpers_UnderThatTypesName()
    {
        TemplatePartReading reading = Read();

        Assert.Equal(
            [new TemplatePartLookup("PART_Nested", TemplatePartOrigin.Helper, FullName(typeof(NestedHelperHost)) + "+Wirer", "Wire", TestAssembly)],
            Entry(reading, nameof(NestedHelperHost)).Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_InnerWired", TemplatePartOrigin.Helper, FullName(typeof(NestingBase)) + "+Inner", "Wire", TestAssembly)],
            Entry(reading, nameof(NestingBaseHost)).Lookups);
    }

    /// <summary>
    /// A lookup in a nested type that no control hands a template to is kept as the outermost type's own, as the rule
    /// keeps it, and is named as the nested type, which declares the method.
    /// </summary>
    [Fact]
    public void ALookupInANestedTypeNoControlHandsItsTemplate_IsTheOutermostTypesOwn_UnderTheNestedTypesName()
    {
        Assert.Equal(
            [new TemplatePartLookup("PART_Located", TemplatePartOrigin.Own, FullName(typeof(NestedFinderPanel)) + "+Finder", "Locate", TestAssembly)],
            Entry(Read(), nameof(NestedFinderPanel)).Lookups);
    }

    /// <summary>
    /// A method no control hands its template to keeps its lookups as its own type's, which is then an entry of its
    /// own; what the controls hand it stays theirs.
    /// </summary>
    [Fact]
    public void ALookupNoControlHandsItsTemplate_StaysItsOwnTypes()
    {
        TemplatePartReading reading = Read();

        ControlTemplateParts helper = Entry(reading, nameof(PartLookupHelper));
        Assert.Empty(helper.Constants);
        Assert.Equal(
            [new TemplatePartLookup("PART_Unhanded", TemplatePartOrigin.Own, FullName(typeof(PartLookupHelper)), "OnOtherTemplateApplied", TestAssembly)],
            helper.Lookups);
        Assert.Equal(
            [new TemplatePartLookup("PART_Described", TemplatePartOrigin.Own, FullName(typeof(Describer)), "Describe", TestAssembly)],
            Entry(reading, nameof(Describer)).Lookups);
    }

    // ── What a control declares, and what its themes declare ────────────────────

    /// <summary>
    /// ⚠ A constant is not a lookup: a part only a constant names is in <c>Constants</c> and in no lookup, beside a
    /// control whose constant and lookup name different parts.
    /// </summary>
    [Fact]
    public void AConstantNothingLooksUp_IsAConstantAndNoLookup()
    {
        TemplatePartReading reading = Read();

        ControlTemplateParts proxy = Entry(reading, nameof(ProxyPanel));
        Assert.Equal(["PART_Footer", "PART_Header"], proxy.Constants);
        Assert.Empty(proxy.Lookups);

        ControlTemplateParts mixed = Entry(reading, nameof(MixedLookupPanel));
        Assert.Equal(["PART_Constant"], mixed.Constants);
        Assert.Equal(
            [new TemplatePartLookup("PART_Literal", TemplatePartOrigin.Own, FullName(typeof(MixedLookupPanel)), "ApplyTemplate", TestAssembly)],
            mixed.Lookups);
    }

    /// <summary>
    /// A theme's parts are its elements named <c>PART_…</c>, in either naming spelling, in the order it declares them,
    /// each with its element's local name and the automation id it declares. A name that is not a part is left out, and
    /// an element that gives one name in both spellings is one part.
    /// </summary>
    [Fact]
    public void AThemesDeclaredParts_AreItsPartElements_InTheOrderItDeclaresThem()
    {
        Write("Proxy.axaml", Dictionary(
            "<ControlTheme x:Key=\"k\" TargetType=\"t:ProxyPanel\">",
            "<Setter Property=\"Template\"><ControlTemplate><StackPanel>",
            "<ScrollViewer x:Name=\"PART_Header\" AutomationProperties.AutomationId=\"Header\" />",
            "<Border Name=\"Decor\" />",
            "<Border Name=\"PART_Footer\" />",
            "<Border Name=\"PART_Twice\" x:Name=\"PART_Twice\" />",
            "</StackPanel></ControlTemplate></Setter>",
            "</ControlTheme>"));

        ThemeParts theme = Assert.Single(Entry(Read(), nameof(ProxyPanel)).Themes);

        Assert.Equal("Proxy.axaml", theme.File);
        Assert.Equal(2, theme.Line);
        Assert.Equal(
            [
                new DeclaredPart("PART_Header", "ScrollViewer", "Header"),
                new DeclaredPart("PART_Footer", "Border", null),
                new DeclaredPart("PART_Twice", "Border", null),
            ],
            theme.Declared);
    }

    /// <summary>
    /// A part's automation id is read in every spelling: the attached property's attribute and property element, and
    /// MAUI's plain attribute. A blank one is kept, since it hides the id the framework would derive; a bound one in a
    /// property element is named by its binding's type.
    /// </summary>
    [Fact]
    public void ADeclaredPartsAutomationId_IsReadInEverySpelling()
    {
        Write("Ids.axaml", Dictionary(ThemeOf(
            "t:ProxyPanel",
            "<StackPanel>"
            + "<TextBox Name=\"PART_Header\"><AutomationProperties.AutomationId> Header </AutomationProperties.AutomationId></TextBox>"
            + "<Border Name=\"PART_Footer\" AutomationProperties.AutomationId=\"\" />"
            + "<Border Name=\"PART_Bound\"><AutomationProperties.AutomationId><Binding Path=\"Id\" /></AutomationProperties.AutomationId></Border>"
            + "<Border Name=\"PART_Maui\" AutomationId=\"Plain\" />"
            + "<Border Name=\"PART_None\" />"
            + "</StackPanel>")));

        ThemeParts theme = Assert.Single(Entry(Read(), nameof(ProxyPanel)).Themes);

        Assert.Equal(
            [
                new DeclaredPart("PART_Header", "TextBox", "Header"),
                new DeclaredPart("PART_Footer", "Border", ""),
                new DeclaredPart("PART_Bound", "Border", "{Binding}"),
                new DeclaredPart("PART_Maui", "Border", "Plain"),
                new DeclaredPart("PART_None", "Border", null),
            ],
            theme.Declared);
    }

    /// <summary>A control themed twice lists both themes, by file, then line, each with what it declares.</summary>
    [Fact]
    public void AControlThemedTwice_ListsBothThemes_ByFileThenLine()
    {
        Write("B.axaml", Dictionary(
            ThemeOf("t:ProxyPanel", "<Border Name=\"PART_Header\" />"),
            ThemeOf("t:ProxyPanel", "<Border Name=\"PART_Footer\" />")));
        Write("A.axaml", Dictionary(ThemeOf("t:ProxyPanel", "<Border Name=\"PART_Header\" /><Border Name=\"PART_Footer\" />")));

        IReadOnlyList<ThemeParts> themes = Entry(Read(), nameof(ProxyPanel)).Themes;

        Assert.Equal(["A.axaml:2", "B.axaml:2", "B.axaml:3"], themes.Select(theme => $"{theme.File}:{theme.Line}"));
        Assert.Equal(["PART_Header", "PART_Footer"], themes[0].Declared.Select(part => part.Name));
        Assert.Equal(["PART_Header"], themes[1].Declared.Select(part => part.Name));
        Assert.Equal(["PART_Footer"], themes[2].Declared.Select(part => part.Name));
    }

    /// <summary>
    /// A themed control that declares no part has no entry, since the rule credits it nothing, and a control with no
    /// theme in the scan has an entry with no themes.
    /// </summary>
    [Fact]
    public void AnEntryIsATypeWithParts_WhetherOrNotItIsThemed()
    {
        Write("Partless.axaml", Dictionary(ThemeOf("t:PartlessPanel", "<Border Name=\"PART_Header\" />")));

        TemplatePartReading reading = Read();

        Assert.DoesNotContain(reading.Controls, control => control.Name == nameof(PartlessPanel));
        Assert.Empty(Entry(reading, nameof(ProxyPanel)).Themes);
    }

    // ── What could not be read ──────────────────────────────────────────────────

    /// <summary>
    /// ⛔ Build output whose dependency does not load: the control read only in part is named with what stopped the
    /// rest, and keeps the constant that was read; the type that did not load is named with the assembly that defines
    /// it. Neither throws, and they are ordered by name.
    /// </summary>
    [Fact]
    public void WhatCouldNotBeRead_IsNamedWithWhy()
    {
        TemplatePartReading reading = TemplatePartRule.ReadParts(
            XamlScanContext.Load(_root).WithAssemblies(UnloadableAssembly.Consumer));

        Assert.Equal(["Plain", "Thing"], reading.Skipped.Select(skip => skip.Subject));
        Assert.Contains("Part of its code could not be read: Dep could not be found", reading.Skipped[0].Reason, StringComparison.Ordinal);
        Assert.Contains(
            "Consumer, which the scan was given, defines it, but it could not be loaded: Dep could not be found",
            reading.Skipped[1].Reason,
            StringComparison.Ordinal);
        Assert.Equal(["PART_Plain"], Entry(reading, "Plain").Constants);
        Assert.DoesNotContain(reading.Controls, control => control.Name == "Thing");
    }

    /// <summary>
    /// ⛔ A scan given no assemblies reads nothing, and says so once, so an empty reading cannot pass for a library
    /// with no parts.
    /// </summary>
    [Fact]
    public void WithoutAssemblies_NothingIsRead_AndOneSkipSaysSo()
    {
        Write("Bad.axaml", Dictionary(ThemeOf("t:ProxyPanel", "<Border Name=\"PART_Header\" />")));

        TemplatePartReading reading = TemplatePartRule.ReadParts(XamlScanContext.Load(_root));

        Assert.Empty(reading.Controls);
        XamlSkip skip = Assert.Single(reading.Skipped);
        Assert.Contains("given no assemblies", skip.Reason, StringComparison.Ordinal);
        Assert.Contains("WithAssemblies", skip.Reason, StringComparison.Ordinal);
    }

    // ── The reading and the rule ────────────────────────────────────────────────

    /// <summary>
    /// The reading keeps the order that lets two readings compare as text: controls, constants, and lookups by part,
    /// then type, then method, all ordinal, each once.
    /// </summary>
    [Fact]
    public void TheReading_IsInTheOrderItComparesIn()
    {
        TemplatePartReading reading = Read();

        ControlTemplateParts unordered = Entry(reading, nameof(UnorderedLookupHost));
        string host = FullName(typeof(UnorderedLookupHost));
        Assert.Equal(["PART_Alpha", "PART_Zulu"], unordered.Constants);
        Assert.Equal(
            [
                new TemplatePartLookup("PART_Alpha", TemplatePartOrigin.Helper, FullName(typeof(AlphaFinder)), "Seek", TestAssembly),
                new TemplatePartLookup("PART_Alpha", TemplatePartOrigin.Own, host, "OnApplyTemplate", TestAssembly),
                new TemplatePartLookup("PART_Zulu", TemplatePartOrigin.Own, host, "Again", TestAssembly),
                new TemplatePartLookup("PART_Zulu", TemplatePartOrigin.Own, host, "OnApplyTemplate", TestAssembly),
            ],
            unordered.Lookups);

        string[] names = [.. reading.Controls.Select(control => control.Name)];
        Assert.True(names.Length > 10, $"Only {names.Length} entries were read from the test assembly.");
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        foreach (ControlTemplateParts control in reading.Controls)
        {
            Assert.Equal(control.Constants.Distinct().Order(StringComparer.Ordinal), control.Constants);
            Assert.Equal(
                control.Lookups.Distinct()
                    .OrderBy(lookup => lookup.Part, StringComparer.Ordinal)
                    .ThenBy(lookup => lookup.Type, StringComparer.Ordinal)
                    .ThenBy(lookup => lookup.Method, StringComparer.Ordinal),
                control.Lookups);
        }
    }

    /// <summary>
    /// ⭐ The rule's findings are exactly the parts the reading says each theme lacks: for each theme of a control, its
    /// constants and looked-up parts that the theme does not declare. And what the rule inspected is each theme's
    /// count of them.
    /// </summary>
    [Fact]
    public void TheRulesFindings_AreThePartsTheReadingSaysEachThemeLacks()
    {
        Write("Hosts.axaml", Dictionary(
            ThemeOf("t:HandingHost", "<Border Name=\"PART_Handed\" />"),
            ThemeOf("t:InheritingHost", "<Border Name=\"PART_Other\" />"),
            ThemeOf("t:ScopeHandingHost", "<Border Name=\"PART_Scoped\" />"),
            ThemeOf("t:UnorderedLookupHost", "<Border Name=\"PART_Zulu\" />")));
        Write("Panels.axaml", Dictionary(
            ThemeOf("t:ProxyPanel", "<Border Name=\"PART_Header\" />"),
            ThemeOf("t:MixedLookupPanel", "<Border Name=\"PART_Constant\" />"),
            ThemeOf("t:LambdaApplyHost", "<Border />"),
            ThemeOf("t:PartlessPanel", "<Border />"),
            ThemeOf("t:SomethingElse", "<Border />")));
        Write("Again.axaml", Dictionary(
            ThemeOf("t:ProxyPanel", "<Border Name=\"PART_Header\" /><Border Name=\"PART_Footer\" />")));
        XamlScanContext scan = Scan();

        TemplatePartReading reading = TemplatePartRule.ReadParts(scan);
        XamlRuleResult result = new TemplatePartRule().Analyze(scan);

        string[] lacking =
        [
            .. from control in reading.Controls
               from theme in control.Themes
               from part in control.Constants.Concat(control.Lookups.Select(lookup => lookup.Part)).Distinct()
               where !theme.Declared.Any(declared => declared.Name == part)
               select $"{theme.File}:{theme.Line} {control.Name} {part}",
        ];
        string[] found =
        [
            .. result.Findings
                .Select(finding =>
                {
                    Match match = Finding.Match(finding.Message);
                    Assert.True(match.Success, finding.Message);
                    return $"{finding.RelativePath}:{finding.Line} {match.Groups["control"].Value} {match.Groups["part"].Value}";
                })
                .Order(StringComparer.Ordinal),
        ];

        Assert.True(lacking.Length >= 5, $"Only {lacking.Length} lacking parts, so the comparison proves little.");
        Assert.Equal(lacking.Order(StringComparer.Ordinal), found);
        Assert.Equal(
            reading.Controls.Sum(control =>
                control.Themes.Count * control.Constants.Concat(control.Lookups.Select(lookup => lookup.Part)).Distinct().Count()),
            result.Inspected);
    }
}
