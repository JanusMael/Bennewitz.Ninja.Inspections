using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.Rules;

namespace XamlQuality.Tests.Rules;

/// <summary>
/// <see cref="ItemContainerBackgroundRule"/> over markup written for the test, against the miniature
/// framework in <c>FocusFakes.cs</c>.
/// </summary>
/// <remarks>
/// ⭐ <b>The rule reports two things markup decides</b>: rows whose <c>Background</c> can be null where
/// their template draws it, and rows whose template draws no background at all. What the rows present,
/// an item template and what it holds, decides neither. What leaves a row taking no click was measured on
/// Avalonia 12.1.3 first, headless, under Fluent, Simple and Semi 12.1.0.1: stock rows, a style setting
/// <c>{x:Null}</c>, a binding to a null source and through a converter answering null, each with and
/// without <c>TargetNullValue</c> and with <c>FallbackValue</c> alone, a row theme not <c>BasedOn</c>
/// another with no <c>Background</c>, one that sets <c>Transparent</c> with a template that draws nothing,
/// one <c>BasedOn</c> the framework's, and <c>ComboBoxItem</c>, <c>TabItem</c> and <c>TreeViewItem</c> rows.
/// The fakes reproduce the framework's shape, not its behaviour, so these tests prove the rule's reading
/// of markup and types; the measurements are what say that reading matches the framework.
/// </remarks>
public sealed class ItemContainerBackgroundRuleTests : IDisposable
{
    private const string Header =
        "xmlns=\"https://github.com/avaloniaui\" " +
        "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

    private const string Themed = "ItemContainerTheme=\"{StaticResource RowTheme}\"";

    /// <summary>A row template's presenter as the framework's are written: it takes the row's content and the template the item template reaches it by.</summary>
    private const string Presenter = "<ContentPresenter Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\" />";

    private const string DrawingTemplate = "<Border Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>";

    private const string BareTemplate = Presenter;

    private const string FrameworkRow = "{StaticResource {x:Type ListBoxItem}}";

    private readonly string _root;

    public ItemContainerBackgroundRuleTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "xq-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) { Directory.Delete(_root, recursive: true); }
    }

    private void Write(string name, string markup)
    {
        string path = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, markup);
    }

    private XamlRuleResult Run() =>
        new ItemContainerBackgroundRule().Analyze(
            XamlScanContext.Load(_root).WithAssemblies(typeof(FocusFakes.ListBox).Assembly));

    private XamlRuleResult RunWithoutTypes() =>
        new ItemContainerBackgroundRule().Analyze(XamlScanContext.Load(_root));

    /// <summary>Writes the view and runs the rule over it, given only <paramref name="assembly"/>.</summary>
    private XamlRuleResult TestWith(System.Reflection.Assembly assembly, string view)
    {
        Write("View.axaml", view);
        return new ItemContainerBackgroundRule().Analyze(XamlScanContext.Load(_root).WithAssemblies(assembly));
    }

    /// <summary>Writes the view and runs the rule over it.</summary>
    private XamlRuleResult Test(string view)
    {
        Write("View.axaml", view);
        return Run();
    }

    private static string View(string body, string resources = "", string styles = "") =>
        "<UserControl " + Header + ">"
        + (resources.Length > 0 ? "<UserControl.Resources>" + resources + "</UserControl.Resources>" : string.Empty)
        + (styles.Length > 0 ? "<UserControl.Styles>" + styles + "</UserControl.Styles>" : string.Empty)
        + body
        + "</UserControl>";

    /// <summary>An items control bound to a source.</summary>
    private static string List(string attributes = "", string host = "ListBox") =>
        "<" + host + " ItemsSource=\"{Binding Rows}\" " + attributes + " />";

    /// <summary>A list bound to a source whose item template presents <paramref name="root"/>, with the row theme unless not <paramref name="themed"/>.</summary>
    private static string ItemTemplated(string root, bool themed = true) =>
        "<ListBox ItemsSource=\"{Binding Rows}\"" + (themed ? " " + Themed : string.Empty) + "><ListBox.ItemTemplate><DataTemplate>"
        + root + "</DataTemplate></ListBox.ItemTemplate></ListBox>";

    /// <summary>A row theme keyed <c>RowTheme</c>, with <paramref name="template"/> as its template when there is one.</summary>
    private static string Theme(string setters = "", string basedOn = "", string template = DrawingTemplate) =>
        "<ControlTheme x:Key=\"RowTheme\" TargetType=\"ListBoxItem\"" + (basedOn.Length > 0 ? " BasedOn=\"" + basedOn + "\"" : string.Empty) + ">"
        + setters
        + (template.Length > 0 ? "<Setter Property=\"Template\"><ControlTemplate>" + template + "</ControlTemplate></Setter>" : string.Empty)
        + "</ControlTheme>";

    /// <summary>The themed list and a row theme whose template is <paramref name="template"/> and whose Background is <paramref name="background"/>.</summary>
    private static string Rows(string template, string background = "Transparent", string resources = "", string styles = "") =>
        View(List(Themed), Theme(Background(background), template: template) + resources, styles);

    private static string Background(string value) => "<Setter Property=\"Background\" Value=\"" + value + "\" />";

    private static string Style(string selector, string setters) => "<Style Selector=\"" + selector + "\">" + setters + "</Style>";

    private static void AssertClean(XamlRuleResult result)
    {
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
        Assert.Equal(1, result.Inspected);
    }

    private static XamlFinding AssertReported(XamlRuleResult result)
    {
        Assert.Empty(result.Skipped);
        Assert.Equal(1, result.Inspected);
        XamlFinding finding = Assert.Single(result.Findings);
        Assert.Equal("BNXQ1010", finding.RuleId);
        return finding;
    }

    /// <summary>A finding that the rows' Background can be null where their template draws it.</summary>
    private static XamlFinding AssertNullWhereDrawn(XamlRuleResult result)
    {
        XamlFinding finding = AssertReported(result);
        Assert.Contains("can have a null Background where their template draws it", finding.Message, StringComparison.Ordinal);
        return finding;
    }

    /// <summary>A finding that the rows' template draws no background.</summary>
    private static XamlFinding AssertNoBackground(XamlRuleResult result)
    {
        XamlFinding finding = AssertReported(result);
        Assert.Contains("draw no background", finding.Message, StringComparison.Ordinal);
        return finding;
    }

    private static XamlSkip AssertSkipped(XamlRuleResult result)
    {
        Assert.Empty(result.Findings);
        Assert.Equal(0, result.Inspected);
        return Assert.Single(result.Skipped);
    }

    // ── What is a subject ──────────────────────────────────────────────────────

    /// <summary>A list whose rows are written in markup generates none from a source: nothing to inspect, with types or without.</summary>
    [Fact]
    public void MarkupWithNoItemsSource_InspectsNothing()
    {
        XamlRuleResult result = Test(View("<ListBox><ListBoxItem>One</ListBoxItem></ListBox>"));

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);

        XamlRuleResult untyped = RunWithoutTypes();
        Assert.Equal(0, untyped.Inspected);
        Assert.Empty(untyped.Skipped);
    }

    /// <summary>Which rows a control generates, and what draws them, are read from types; a list with no source is not named.</summary>
    [Fact]
    public void WithoutAssemblies_EveryItemsSourceIsNamedAsSkipped()
    {
        Write("View.axaml", View("<StackPanel>" + List() + List(host: "ComboBox") + "<ListBox><ListBoxItem>One</ListBoxItem></ListBox></StackPanel>"));

        XamlRuleResult result = RunWithoutTypes();

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Equal(2, result.Skipped.Count);
        Assert.All(result.Skipped, skip => Assert.Contains("given no assemblies", skip.Reason, StringComparison.Ordinal));
    }

    /// <summary>A tree's rows are left out: whether a null one takes the click depends on the theme's template.</summary>
    [Fact]
    public void ATreeView_IsNotASubject()
    {
        XamlRuleResult result = Test(View(List(host: "TreeView"), styles: Style("TreeViewItem", Background("{x:Null}"))));

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }

    /// <summary>An items control's presenters are not rows.</summary>
    [Fact]
    public void AnItemsControlsPresenters_AreNotSubjects()
    {
        XamlRuleResult result = Test(View(List(host: "ItemsControl")));

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }

    /// <summary>
    /// An ItemsSource written after the list's own type or a base is the list's. After another owner it is the list's
    /// only where that owner holds the list's own, and only through a binding, which is all the XAML compiler takes
    /// there: measured, a list written <c>ComboBox.ItemsSource="{Binding Rows}"</c>, or with it as a property element,
    /// generated its rows. A literal there does not load, and an owner that holds another ItemsSource, or none, gives
    /// the list no source.
    /// </summary>
    [Fact]
    public void AnItemsSourceAfterAnOwner_IsTheListsWhereThatOwnerHoldsItsOwn()
    {
        static void AssertNotASubject(XamlRuleResult result)
        {
            Assert.Equal(0, result.Inspected);
            Assert.Empty(result.Findings);
            Assert.Empty(result.Skipped);
        }

        AssertClean(Test(View("<ListBox ListBox.ItemsSource=\"{Binding Rows}\" />")));
        AssertClean(Test(View("<ListBox ItemsControl.ItemsSource=\"{Binding Rows}\" />")));

        string bare = Theme(Background("Transparent"), template: BareTemplate);
        AssertNoBackground(Test(View("<ListBox ComboBox.ItemsSource=\"{Binding Rows}\" " + Themed + " />", bare)));
        AssertNoBackground(Test(View("<ListBox " + Themed + "><ComboBox.ItemsSource><Binding Path=\"Rows\" /></ComboBox.ItemsSource></ListBox>", bare)));
        AssertNoBackground(Test(View("<ListBox " + Themed + "><ComboBox.ItemsSource><DynamicResource ResourceKey=\"Rows\" /></ComboBox.ItemsSource></ListBox>", bare)));
        AssertNoBackground(Test(View("<ListBox " + Themed + "><ComboBox.ItemsSource><RowBinding /></ComboBox.ItemsSource></ListBox>", bare)));
        AssertNoBackground(Test(View("<ListBox ComboBox.ItemsSource=\"{RowBinding}\" " + Themed + " />", bare)));

        AssertNotASubject(Test(View("<ListBox ComboBox.ItemsSource=\"Rows\" " + Themed + " />", bare)));
        AssertNotASubject(Test(View("<ListBox Wrapping.ItemsSource=\"{Binding Rows}\" />")));
        AssertNotASubject(Test(View("<ListBox Paint.ItemsSource=\"{Binding Rows}\" />")));
    }

    /// <summary>
    /// What markup cannot decide about a property written after an owner is said, wherever the rule reads one: an owner
    /// whose type does not resolve, and, after an owner outside the element's chain, a value that may or may not be a
    /// binding. Where the list's theme would give what the list does not, it is not asked.
    /// </summary>
    [Fact]
    public void AnOwnerOrAValueThatCannotBeRead_IsSkipped()
    {
        XamlSkip unresolved = AssertSkipped(Test(View("<ListBox Unknowable.ItemsSource=\"{Binding Rows}\" />")));
        Assert.Contains("whether it does could not be read", unresolved.Reason, StringComparison.Ordinal);
        XamlSkip resource = AssertSkipped(Test(View("<ListBox ComboBox.ItemsSource=\"{StaticResource Rows}\" />")));
        Assert.Contains("only if it is a binding", resource.Reason, StringComparison.Ordinal);

        AssertSkipped(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" Unknowable.Theme=\"{StaticResource Other}\" />")));
        AssertSkipped(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" Unknowable.ItemContainerTheme=\"{StaticResource Other}\" />",
            Theme(Background("Transparent")) + "<ControlTheme x:Key=\"{x:Type ListBox}\" TargetType=\"ListBox\">"
            + "<Setter Property=\"ItemContainerTheme\" Value=\"{StaticResource RowTheme}\" /></ControlTheme>")));

        AssertSkipped(Test(Rows("<Panel Unknowable.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>")));
        AssertSkipped(Test(Rows("<Panel Border.Background=\"{StaticResource Brush}\">" + Presenter + "</Panel>")));
        XamlSkip unreadable = AssertSkipped(Test(Rows("<Panel><UnreadablePanel Border.Background=\"{TemplateBinding Background}\" />" + Presenter + "</Panel>")));
        Assert.Contains("whether it does could not be read", unreadable.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(Rows("<Panel><Border Unknowable.Theme=\"{StaticResource Other}\" />" + Presenter + "</Panel>")));
        AssertSkipped(Test(Rows("<Border Background=\"{TemplateBinding Background}\" Unknowable.Theme=\"{StaticResource Other}\">" + Presenter + "</Border>")));
        AssertSkipped(Test(Rows("<Panel><Chip Unknowable.Background=\"{TemplateBinding Background}\" />" + Presenter + "</Panel>", resources: ChipTheme())));
        AssertSkipped(Test(Rows("<Panel><Chip Unknowable.Template=\"{StaticResource Other}\" />" + Presenter + "</Panel>", resources: ChipTheme())));
        AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}") + "<Setter Property=\"(Unknowable.Background)\" Value=\"Transparent\" />"))));
        AssertSkipped(Test(View(List(), styles: Style("ListBoxItem", "<Setter Property=\"(Unknowable.Background)\" Value=\"Transparent\" />"))));
        XamlSkip unreadableOwner = AssertSkipped(Test(View(List(Themed),
            Theme(Background("{x:Null}") + "<Setter Property=\"UnreadablePanel.Background\" Value=\"Transparent\" />"))));
        Assert.Contains("could not be read", unreadableOwner.Reason, StringComparison.Ordinal);
        XamlSkip unreadableOnAnElement = AssertSkipped(Test(Rows("<Panel UnreadablePanel.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>")));
        Assert.Contains("could not be read", unreadableOnAnElement.Reason, StringComparison.Ordinal);

        // A type's own markup, found by its x:Class.
        const string Class = "x:Class=\"XamlQuality.Tests.Rules.ItemFakes.RowView\"";
        foreach (string unknowable in (string[])["Unknowable.Template=\"{StaticResource Other}\"", "Unknowable.Background=\"Transparent\""])
        {
            Write("RowView.axaml", "<UserControl " + Header + " " + Class + " " + unknowable + "><TextBlock /></UserControl>");
            AssertSkipped(Test(Rows("<Panel><RowView />" + Presenter + "</Panel>")));
        }

        Write("FilledRect.axaml", "<Rectangle " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.FilledRect\" Unknowable.Fill=\"{x:Null}\" />");
        AssertSkipped(Test(Rows("<Panel><FilledRect />" + Presenter + "</Panel>")));

        // A style elsewhere, which may reach the list or not.
        Write("Other.axaml", "<Styles " + Header + "><Style Selector=\"ListBoxItem\"><Setter Property=\"(Unknowable.Background)\" Value=\"Transparent\" /></Style></Styles>");
        XamlSkip elsewhere = AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}")))));
        Assert.Contains("in no scope this markup shows reaching", elsewhere.Reason, StringComparison.Ordinal);
    }

    /// <summary>A template's <c>ItemsSource</c> sets its children's source and generates no rows itself.</summary>
    [Fact]
    public void ATreeDataTemplatesItemsSource_IsNotAHost()
    {
        XamlRuleResult result = Test(View(
            "<TreeView ItemsSource=\"{Binding Rows}\"><TreeView.ItemTemplate>"
            + "<TreeDataTemplate ItemsSource=\"{Binding Children}\"><TextBlock Text=\"{Binding Label}\" /></TreeDataTemplate>"
            + "</TreeView.ItemTemplate></TreeView>"));

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }

    // ── The framework's rows ───────────────────────────────────────────────────

    /// <summary>Stock rows are Transparent in every theme measured, and their template draws it.</summary>
    [Fact]
    public void StockRows_AreClean() =>
        AssertClean(Test(View(List())));

    /// <summary>A row theme based on the framework's keeps the framework's background and template.</summary>
    [Fact]
    public void ARowThemeBasedOnTheFrameworks_IsClean() =>
        AssertClean(Test(View(List(Themed), Theme(basedOn: FrameworkRow, template: string.Empty))));

    // ── A style that sets every row's background ──────────────────────────────

    /// <summary>A null background is reported where the template draws it, the framework's own included, with the fix.</summary>
    [Fact]
    public void AStyleSettingANullBackground_IsReported()
    {
        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem", Background("{x:Null}")))));

        Assert.Contains("the framework theme's own row template draws the row's Background", finding.Message, StringComparison.Ordinal);
        Assert.Contains("{x:Null}", finding.Message, StringComparison.Ordinal);
        Assert.Contains("TargetNullValue={x:Static Brushes.Transparent}", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A binding answers null wherever its source does, the case that arose.</summary>
    [Fact]
    public void ABoundBackgroundWithNoTargetNullValue_IsReported()
    {
        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem", Background("{Binding RowBrush}")))));

        Assert.Contains("no TargetNullValue", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A converter answering null leaves the row null too.</summary>
    [Fact]
    public void ABackgroundBoundThroughAConverter_IsReported() =>
        AssertNullWhereDrawn(Test(View(List(),
            styles: Style("ListBoxItem", Background("{Binding IsHighlighted, Converter={StaticResource NullWhenFalse}}")))));

    /// <summary>A non-null TargetNullValue answers for the null.</summary>
    [Fact]
    public void ABoundBackgroundWithATargetNullValue_IsClean() =>
        AssertClean(Test(View(List(),
            styles: Style("ListBoxItem", Background("{Binding RowBrush, TargetNullValue={x:Static Brushes.Transparent}}")))));

    /// <summary>A TargetNullValue that is itself null answers null.</summary>
    [Fact]
    public void ATargetNullValueOfNull_IsReported()
    {
        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(),
            styles: Style("ListBoxItem", Background("{Binding RowBrush, TargetNullValue={x:Null}}")))));

        Assert.Contains("itself null", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A FallbackValue covers a failed binding, not a null value: measured, it did not help.</summary>
    [Fact]
    public void AFallbackValueAlone_IsReported() =>
        AssertNullWhereDrawn(Test(View(List(),
            styles: Style("ListBoxItem", Background("{Binding RowBrush, FallbackValue={x:Static Brushes.Transparent}}")))));

    /// <summary>A literal brush is not null.</summary>
    [Fact]
    public void ALiteralBrush_IsClean() =>
        AssertClean(Test(View(List(), styles: Style("ListBoxItem", Background("Transparent")))));

    /// <summary>The setter's other spellings: a <c>Setter.Value</c> element holding <c>x:Null</c>, or a binding as its content.</summary>
    [Fact]
    public void TheSettersOtherSpellings_Count()
    {
        AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem",
            "<Setter Property=\"Background\"><Setter.Value><x:Null /></Setter.Value></Setter>"))));
        AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem",
            "<Setter Property=\"Background\"><Binding Path=\"RowBrush\" /></Setter>"))));
        AssertClean(Test(View(List(), styles: Style("ListBoxItem",
            "<Setter Property=\"Background\"><Binding Path=\"RowBrush\" TargetNullValue=\"Transparent\" /></Setter>"))));
    }

    /// <summary>
    /// A background is read in every spelling the XAML compiler takes: a setter's name in parentheses or in a
    /// Setter.Property element, after an owner that holds the row's, as a Border does, whose BackgroundProperty is the
    /// one a templated control adds itself to; an element's after its own type or a base, and not after an owner that
    /// holds another property; and a template binding's or a path's after an owner that holds the row's.
    /// </summary>
    [Fact]
    public void TheBackgroundsOtherSpellings_Count()
    {
        AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem", "<Setter Property=\"(ListBoxItem.Background)\" Value=\"{x:Null}\" />"))));
        AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem", "<Setter Value=\"{x:Null}\"><Setter.Property>Background</Setter.Property></Setter>"))));
        AssertClean(Test(View(List(Themed), Theme("<Setter Value=\"Transparent\"><Setter.Property>Background</Setter.Property></Setter>"))));
        AssertClean(Test(View(List(Themed), Theme("<Setter Property=\"(Border.Background)\" Value=\"Transparent\" />"))));

        AssertClean(Test(Rows("<Panel><Border Border.Background=\"Transparent\" />" + Presenter + "</Panel>")));
        AssertClean(Test(Rows("<Panel><XClassBorder Border.Background=\"Transparent\" />" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel><Border Wrapping.Background=\"Transparent\" />" + Presenter + "</Panel>")));
        AssertClean(Test(Rows("<Panel><XClassBorder><Border.Background>Transparent</Border.Background></XClassBorder>" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel><Border><Wrapping.Background>Transparent</Wrapping.Background></Border>" + Presenter + "</Panel>")));

        const string Part = "ListBoxItem /template/ Border#Bg";
        const string Template = "<Panel><Border Name=\"Bg\" />" + Presenter + "</Panel>";
        AssertSkipped(Test(Rows(Template, styles: Style(Part, "<Setter Property=\"(Border.Background)\" Value=\"Red\" />"))));
        AssertSkipped(Test(Rows(Template, styles: Style(Part, "<Setter Value=\"Red\"><Setter.Property>Background</Setter.Property></Setter>"))));

        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{TemplateBinding Border.Background}\">" + Presenter + "</Border>", "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{TemplateBinding (Border.Background)}\">" + Presenter + "</Border>", "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{Binding (Border.Background), RelativeSource={RelativeSource TemplatedParent}}\">"
                                       + Presenter + "</Border>", "{x:Null}")));
        AssertClean(Test(Rows("<Border Background=\"{TemplateBinding Border.Background}\">" + Presenter + "</Border>")));
    }

    /// <summary>Two styles that disagree are not ordered: which wins is not evaluated.</summary>
    [Fact]
    public void StylesThatDisagree_AreSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(View(List(),
            styles: Style("ListBoxItem", Background("{x:Null}")) + Style(":is(ListBoxItem)", Background("Transparent")))));

        Assert.Contains("which wins", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A style wins over the row theme, in either direction.</summary>
    [Fact]
    public void AStyleOverTheRowTheme_Wins()
    {
        AssertClean(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
            Style("ListBoxItem", Background("Transparent")))));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("Transparent")),
            Style("ListBoxItem", Background("{x:Null}")))));
    }

    /// <summary>A style that sets the rows' template, or the list's container theme, is not evaluated, written with a type or without one.</summary>
    [Fact]
    public void AStyleThatSetsTheRowsTemplateOrTheme_IsSkipped()
    {
        AssertSkipped(Test(View(List(),
            styles: Style("ListBoxItem", "<Setter Property=\"Template\"><ControlTemplate>" + BareTemplate + "</ControlTemplate></Setter>"))));
        AssertSkipped(Test(View(List(), Theme(Background("{x:Null}")),
            Style("ListBox", "<Setter Property=\"ItemContainerTheme\" Value=\"{StaticResource RowTheme}\" />"))));
        AssertSkipped(Test(View(List(Themed), Theme(template: "<Border Background=\"Transparent\">" + Presenter + "</Border>"),
            Style(":not(.nope)", "<Setter Property=\"Template\" Value=\"{x:Null}\" />"))));
    }

    // ── Styles that pick some rows ─────────────────────────────────────────────

    /// <summary>A style that paints the rows in a state leaves the state they start in as it was.</summary>
    [Fact]
    public void AStyleThatPaintsRowsInAState_LeavesTheStartingStateAsItWas() =>
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
            Style("ListBoxItem:pointerover", Background("Red")))));

    /// <summary>A style that clears the rows in a state can break them, as a measured <c>:pointerover</c> one did.</summary>
    [Fact]
    public void AStyleThatClearsRowsInAState_IsSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(View(List(), styles: Style("ListBoxItem:selected", Background("{x:Null}")))));

        Assert.Contains("while they are pointed at", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A style nested in one that asks for a state applies only in that state, whatever it adds.</summary>
    [Fact]
    public void AStyleNestedInAState_IsAStateStyle() =>
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
            Style("ListBoxItem:pointerover", Style("^ /template/ ContentPresenter", Background("Red"))))));

    /// <summary>A style that may clear some rows by a class is not decided.</summary>
    [Fact]
    public void AStyleThatClearsRowsWithAClass_IsSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(View(List(), styles: Style("ListBoxItem.plain", Background("{x:Null}")))));

        Assert.Contains("some of the ListBoxItem rows", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A style that paints some rows leaves the rest as they were.</summary>
    [Fact]
    public void AStyleThatPaintsRowsWithAClass_LeavesTheRestAsTheyWere() =>
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
            Style("ListBoxItem.plain", Background("Transparent")))));

    /// <summary>
    /// A style whose selector holds of every row in the state rows start in, whatever the data, sets every
    /// row's Background, however it is written: with the host alone as its context, through either
    /// combinator, or with a :not() of a class or a name. Measured, ListBox ListBoxItem, ListBox &gt;
    /// ListBoxItem and ListBoxItem:not(.nope) each painted every row. One written with no type picks the
    /// template's parts too, and outranks what the template gives them, so it leaves them undecided.
    /// </summary>
    [Fact]
    public void AStyleThatHoldsOfEveryRow_SetsEveryRowsBackground()
    {
        foreach (string selector in (string[])["ListBox ListBoxItem", "ListBox > ListBoxItem", "ListBoxItem:not(.nope)", "ListBoxItem:not(#name)"])
        {
            AssertClean(Test(View(List(Themed), Theme(), Style(selector, Background("Transparent")))));
            AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("Transparent")), Style(selector, Background("{x:Null}")))));
        }

        XamlSkip typeless = AssertSkipped(Test(View(List(Themed), Theme(), Style(":not(.nope)", Background("Transparent")))));
        Assert.Contains("outranking the template's own value", typeless.Reason, StringComparison.Ordinal);

        // Over the framework's own row template, which the rule takes as drawing, one with no type sets the rows' Background as one naming them does.
        AssertNullWhereDrawn(Test(View(List(), styles: Style(":not(.nope)", Background("{x:Null}")))));
    }

    /// <summary>
    /// A style whose selector may hold of every row, and paints, leaves a null background undecided: a
    /// property selector, a pseudo-class that is not one of the states, :nth-child(), a context other than
    /// the host alone, or a :not() of a state, which a row leaves when it is pointed at or selected, and
    /// which a list that keeps a row selected starts outside. A child of anything but the host is no row.
    /// </summary>
    [Fact]
    public void AStyleThatMayHoldOfEveryRow_LeavesANullBackgroundUndecided()
    {
        foreach (string selector in (string[])
                 [
                     "ListBoxItem[IsEnabled=True]", "ListBoxItem:nth-child(n)", "ListBoxItem:checked", "Window ListBoxItem",
                     "ListBox.compact ListBoxItem", "ListBoxItem:not([IsEnabled=False])", "ListBoxItem:not(:selected)", "ListBoxItem:not(:pointerover)",
                 ])
        {
            XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(), Style(selector, Background("Transparent")))));
            Assert.Contains("whether its selector picks them all", skip.Reason, StringComparison.Ordinal);
        }

        AssertNullWhereDrawn(Test(View(List(Themed), Theme(), Style("ScrollViewer > ListBoxItem", Background("Transparent")))));
    }

    /// <summary>
    /// A :not() of a state that clears the rows clears those out of it, which every row is until it is
    /// pointed at or selected: measured, a click after a hover missed rows that ListBoxItem:not(:pointerover)
    /// painted over a null background, so one that clears them is not decided either.
    /// </summary>
    [Fact]
    public void ANotOfAStateThatClearsTheRows_IsSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent")),
            Style("ListBoxItem:not(:pointerover)", Background("{x:Null}")))));

        Assert.Contains("the absence of a state", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A style asking for a property the rows' theme binds to each row's item picks the rows the data picks,
    /// and paints no other: measured, rows such a style did not pick missed the click. Taken as picking
    /// only some rows, not proved: data true of every item paints them all. Bound to anything but the item,
    /// the property is the same for every row, which may be all of them.
    /// </summary>
    [Fact]
    public void AStyleAskingForAPropertyBoundToEachItem_PicksOnlySomeRows()
    {
        string bound = "<Setter Property=\"Tag\" Value=\"{Binding Kind}\" />";
        string multi = "<Setter Property=\"Tag\"><MultiBinding><Binding Path=\"A\" /><Binding Path=\"B\" /></MultiBinding></Setter>";

        AssertNullWhereDrawn(Test(View(List(Themed), Theme(bound + Style("^[Tag=recent]", Background("Transparent"))))));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(bound), Style("ListBoxItem[Tag=recent]", Background("Transparent")))));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(multi + Style("^[Tag=recent]", Background("Transparent"))))));

        AssertSkipped(Test(View(List(Themed), Theme("<Setter Property=\"Tag\" Value=\"{Binding Kind, RelativeSource={RelativeSource Self}}\" />"
                                                    + Style("^[Tag=recent]", Background("Transparent"))))));
        AssertSkipped(Test(View(List(Themed), Theme("<Setter Property=\"Tag\" Value=\"{Binding $parent[ListBox].Tag}\" />"
                                                    + Style("^[Tag=recent]", Background("Transparent"))))));

        // An attached property, bound to each item and asked for after its owner, as a selector names one.
        string attached = "<Setter xmlns:p=\"using:XamlQuality.Tests.Rules.ItemFakes\" Property=\"p:Paint.Shade\" Value=\"{Binding Kind}\" />";
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(attached + Style("^[(p|Paint.Shade)=recent]", Background("Transparent"))))));
        AssertNullWhereDrawn(Test(View(List(Themed),
            Theme(attached + Style("^[(p|Paint.Shade)=True]", Style("^[(p|Paint.Shade)=recent]", Background("Transparent")))))));
    }

    /// <summary>
    /// A style that reaches into the rows' template and gives a part a theme, a template or a fill, in no
    /// state, may change what draws there, since a selector with a condition outranks what the template's
    /// markup gives the part. What it gives a part to present, its content or its content template, is what
    /// the rows present, and changes nothing.
    /// </summary>
    [Fact]
    public void AStyleThatReachesIntoTheRowsTemplate_MattersOnlyForWhatAPartDraws()
    {
        string painted = "<Setter Property=\"ContentTemplate\"><DataTemplate><Border Background=\"Transparent\" /></DataTemplate></Setter>";

        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}")), Style("ListBoxItem:not(.nope) /template/ ContentPresenter", painted))));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}")),
            Style("ListBoxItem /template/ ContentPresenter", "<Setter Property=\"Content\"><Border Background=\"Transparent\" /></Setter>"))));

        XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}")
            + Style("^ /template/ ContentPresenter", "<Setter Property=\"Theme\" Value=\"{StaticResource Painted}\" />")))));
        Assert.Contains("sets Theme on a part of the template", skip.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}")), Style("ListBoxItem /template/ Rectangle", "<Setter Property=\"Fill\" Value=\"Red\" />"))));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}")),
            Style("ListBoxItem:pointerover /template/ Rectangle", "<Setter Property=\"Fill\" Value=\"Red\" />"))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent"), template: BareTemplate),
            Style("ListBoxItem:pointerover /template/ Rectangle", "<Setter Property=\"Fill\" Value=\"Red\" />"))));
    }

    /// <summary>A state of the list is not a state of its rows: a part it paints then may draw.</summary>
    [Fact]
    public void AStateOfTheList_IsNotAStateOfTheRows() =>
        AssertSkipped(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
            Style("ListBox:pointerover ListBoxItem /template/ ContentPresenter", Background("Red")))));

    /// <summary>A state inside <c>:not()</c> is not asked for, so a part it paints may draw.</summary>
    [Fact]
    public void AStateInsideNot_IsNotAskedFor() =>
        AssertSkipped(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
            Style("ListBoxItem:not(:selected) /template/ ContentPresenter", Background("Red")))));

    /// <summary>
    /// A style that paints a part of the rows' template may draw where the rows' own Background is null, or
    /// where the template draws none, and changes nothing for rows whose template draws a Background that is
    /// not null.
    /// </summary>
    [Fact]
    public void AStyleThatPaintsAPartOfTheTemplate_MattersOnlyWhereNothingElseDraws()
    {
        const string Part = "ListBoxItem /template/ ContentPresenter";

        AssertClean(Test(View(List(), styles: Style(Part, Background("Transparent")))));
        XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")), Style(Part, Background("Transparent")))));
        Assert.Contains("a part of the template", skip.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent"), template: BareTemplate), Style(Part, Background("Transparent")))));
    }

    /// <summary>A style that clears a part of the rows' template may leave that part drawing nothing.</summary>
    [Fact]
    public void AStyleThatClearsAPartOfTheTemplate_IsSkipped() =>
        AssertSkipped(Test(View(List(), styles: Style("ListBoxItem /template/ ContentPresenter", Background("{x:Null}")))));

    /// <summary>
    /// A style that gives a part of the rows' template a binding may draw there as surely as one that gives it
    /// a brush: measured, a part given the row's Transparent background through $parent[ListBoxItem] or the
    /// templated parent drew it. So where the template draws nothing else, the rows are not decided, and
    /// where it draws the row's Transparent, the part may be left drawing nothing. A part given {x:Null}, or
    /// a binding only in a state, leaves a template that draws nothing reported.
    /// </summary>
    [Fact]
    public void AStyleThatBindsAPartOfTheTemplate_MayDrawThere()
    {
        const string Bare = "<Panel Name=\"Bg\">" + Presenter + "</Panel>";
        const string Part = "ListBoxItem /template/ Panel#Bg";

        foreach (string binding in (string[])["{Binding $parent[ListBoxItem].Background}", "{Binding Background, RelativeSource={RelativeSource TemplatedParent}}"])
        {
            XamlSkip skip = AssertSkipped(Test(Rows(Bare, styles: Style(Part, Background(binding)))));
            Assert.Contains("which may draw where the rows themselves draw nothing", skip.Reason, StringComparison.Ordinal);
        }

        AssertSkipped(Test(Rows(DrawingTemplate, styles: Style("ListBoxItem /template/ Border", Background("{Binding RowBrush}")))));
        AssertNoBackground(Test(Rows(Bare, styles: Style(Part, Background("{x:Null}")))));
        AssertNoBackground(Test(Rows(Bare, styles: Style("ListBoxItem:pointerover /template/ Panel#Bg", Background("{Binding $parent[ListBoxItem].Background}")))));

        // One in another file may reach the list or not, and may draw where it does, or leave a part drawing nothing.
        Write("Rows.axaml", "<Styles " + Header + ">" + Style(Part, Background("{Binding $parent[ListBoxItem].Background}")) + "</Styles>");
        XamlSkip elsewhere = AssertSkipped(Test(Rows(Bare)));
        Assert.Contains("no scope this markup shows reaching", elsewhere.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(Rows(DrawingTemplate)));
    }

    /// <summary>
    /// Rows that markup may disable from the start may start in :disabled, so a style that paints them while
    /// disabled may paint them from the start: measured, the rows of a list whose IsEnabled was False, and
    /// rows whose theme set it so, took a ^:disabled style's Transparent. Their list or what holds it, an
    /// element of their template, or a setter anywhere in the scan may disable them, with anything but True;
    /// where nothing does, :disabled is a state they start out of.
    /// </summary>
    [Fact]
    public void RowsThatMarkupMayDisable_MayStartInDisabled()
    {
        string disabled = Themed + " IsEnabled=\"False\"";
        string theme = Theme(Background("{x:Null}") + Style("^:disabled", Background("Transparent")));
        const string Bare = "<Panel Name=\"Bg\">" + Presenter + "</Panel>";

        XamlSkip skip = AssertSkipped(Test(View(List(disabled), theme)));
        Assert.Contains("may hold of every row", skip.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(View("<StackPanel IsEnabled=\"{Binding Ready}\">" + List(Themed) + "</StackPanel>", theme)));
        AssertSkipped(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" " + Themed + "><ListBox.IsEnabled><Binding Path=\"Ready\" /></ListBox.IsEnabled></ListBox>", theme)));
        AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}") + "<Setter Property=\"IsEnabled\" Value=\"False\" />"
                                                    + Style("^:disabled", Background("Transparent"))))));
        AssertSkipped(Test(View(List(disabled), Theme(Background("{x:Null}")), Style("ListBoxItem:disabled", Background("Transparent")))));

        XamlSkip cleared = AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent")),
            Style("ListBoxItem:disabled", Background("{x:Null}")))));
        Assert.Contains(":disabled, which they may start in, since the ListBox at", cleared.Reason, StringComparison.Ordinal);

        // A style nested in one that asks for :disabled, and what a part of their template is given while disabled.
        AssertSkipped(Test(View(List(disabled), Theme(Background("{x:Null}")), Style("ListBoxItem:disabled", Style("^", Background("Transparent"))))));
        AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent"), template: Bare),
            Style("ListBoxItem:disabled", Style("^ /template/ Panel#Bg", Background("Transparent"))))));
        AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent") + Style("^:disabled /template/ Panel#Bg", Background("Transparent")), template: Bare))));
        AssertSkipped(Test(View(List(disabled), Theme(Background("{x:Null}")), Style("ListBoxItem:disabled /template/ Rectangle", "<Setter Property=\"Fill\" Value=\"Red\" />"))));
        AssertSkipped(Test(View(List(disabled), Theme(Background("{x:Null}") + Style("^:disabled /template/ Rectangle", "<Setter Property=\"Fill\" Value=\"Red\" />")))));

        // The list's IsEnabled written after an owner, as the XAML compiler takes it, and named as the list's.
        foreach (string owner in (string[])["InputElement", "ListBox"])
        {
            XamlSkip owned = AssertSkipped(Test(View(List(Themed + " " + owner + ".IsEnabled=\"False\""), Theme(Background("Transparent")),
                Style("ListBoxItem:disabled", Background("{x:Null}")))));
            Assert.Contains(":disabled, which they may start in, since the ListBox at", owned.Reason, StringComparison.Ordinal);
        }

        AssertNullWhereDrawn(Test(View(List(Themed + " IsEnabled=\"True\""), theme)));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}") + "<Setter Property=\"IsEnabled\" Value=\"True\" />"
                                                           + Style("^:disabled", Background("Transparent"))))));
        AssertNullWhereDrawn(Test(View(List(Themed), theme)));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}")), Style("ListBoxItem:disabled", Background("Transparent")))));
    }

    /// <summary>
    /// A control disabled anywhere disables everything it holds, and markup cannot always say what that is, so
    /// any IsEnabled but True in the scan makes :disabled a state rows, and what their template holds, may
    /// start in: measured, rows started :disabled where the list's ItemsPanel, or the ItemsPresenter in its own
    /// template, was disabled, and a part did where a view's markup or a templated control's theme disabled it.
    /// </summary>
    [Fact]
    public void WhateverMarkupDisables_MayStartInDisabled()
    {
        string theme = Theme(Background("{x:Null}") + Style("^:disabled", Background("Transparent")));
        string panel = "<ListBox ItemsSource=\"{Binding Rows}\" " + Themed
                       + "><ListBox.ItemsPanel><ItemsPanelTemplate><StackPanel IsEnabled=\"False\" /></ItemsPanelTemplate></ListBox.ItemsPanel></ListBox>";
        string presenter = "<ListBox ItemsSource=\"{Binding Rows}\" " + Themed
                           + "><ListBox.Template><ControlTemplate><ItemsPresenter IsEnabled=\"False\" /></ControlTemplate></ListBox.Template></ListBox>";
        string painting = Style("Border:disabled", Background("Red"));
        string inView = Theme(Background("Transparent"), template: "<Panel><RowView />" + Presenter + "</Panel>");
        string inChip = Theme(Background("Transparent"), template: "<Panel><Chip />" + Presenter + "</Panel>");
        static string RowView(string enabled) =>
            "<UserControl " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.RowView\"><Border Name=\"Part\" Height=\"10\" IsEnabled=\"" + enabled + "\" /></UserControl>";

        AssertNullWhereDrawn(Test(View(panel.Replace("False", "True", StringComparison.Ordinal), theme)));
        AssertNullWhereDrawn(Test(View(presenter.Replace("False", "True", StringComparison.Ordinal), theme)));
        Write("RowView.axaml", RowView("True"));
        AssertNoBackground(Test(View(List(Themed), inView, painting)));
        AssertNoBackground(Test(View(List(Themed), inChip + ChipTheme(template: "<Border Name=\"ChipBg\" IsEnabled=\"True\" />"), painting)));

        XamlSkip skip = AssertSkipped(Test(View(panel, theme)));
        Assert.Contains("may hold of every row", skip.Reason, StringComparison.Ordinal);
        XamlSkip named = AssertSkipped(Test(View(panel, Theme(Background("Transparent")), Style("ListBoxItem:disabled", Background("{x:Null}")))));
        Assert.Contains(":disabled, which they may start in, since the StackPanel at", named.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(View(presenter, theme)));
        AssertSkipped(Test(View(List(Themed), inChip + ChipTheme(template: "<Border Name=\"ChipBg\" IsEnabled=\"False\" />"), painting)));
        Write("RowView.axaml", RowView("False"));
        AssertSkipped(Test(View(List(Themed), inView, painting)));

        // Markup elsewhere, which may hold the view: a command, which disables a button or a menu item while it
        // cannot execute, and IsEnabled as an attribute or a property element.
        Write("RowView.axaml", RowView("True"));
        Write("Other.axaml", "<UserControl " + Header + "><Button Command=\"{x:Null}\" /></UserControl>");
        AssertNullWhereDrawn(Test(View(List(Themed), theme)));
        Write("Other.axaml", "<UserControl " + Header + "><Button Command=\"{Binding Go}\" /></UserControl>");
        AssertSkipped(Test(View(List(Themed), theme)));
        Write("Other.axaml", "<Styles " + Header + "><Style Selector=\"Button\"><Setter Property=\"Command\" Value=\"{Binding Go}\" /></Style></Styles>");
        AssertSkipped(Test(View(List(Themed), theme)));
        Write("Other.axaml", "<UserControl " + Header + "><Button IsEnabled=\"{Binding Ready}\" /></UserControl>");
        AssertSkipped(Test(View(List(Themed), theme)));
        Write("Other.axaml", "<UserControl " + Header + "><Button><Button.IsEnabled><Binding Path=\"Ready\" /></Button.IsEnabled></Button></UserControl>");
        AssertSkipped(Test(View(List(Themed), theme)));

        // Every spelling the XAML compiler takes: after an owner, or a setter's in parentheses. An IsEnabled
        // attached by a type that takes no part in focus is a property of its own, and disables nothing; one
        // after an owner that does not resolve may be the control's.
        foreach (string disabling in (string[])
                 [
                     "<Button InputElement.IsEnabled=\"False\" />",
                     "<Button Control.IsEnabled=\"False\" />",
                     "<Button Unknowable.IsEnabled=\"False\" />",
                     "<Button Button.Command=\"{Binding Go}\" />",
                 ])
        {
            Write("Other.axaml", "<UserControl " + Header + ">" + disabling + "</UserControl>");
            AssertSkipped(Test(View(List(Themed), theme)));
        }

        foreach (string setter in (string[])
                 [
                     "Property=\"(InputElement.IsEnabled)\" Value=\"False\"",
                     "Property=\"(Unknowable.IsEnabled)\" Value=\"False\"",
                     "Property=\"(Button.Command)\" Value=\"{Binding Go}\"",
                 ])
        {
            Write("Other.axaml", "<Styles " + Header + "><Style Selector=\"Button\"><Setter " + setter + " /></Style></Styles>");
            AssertSkipped(Test(View(List(Themed), theme)));
        }

        Write("Other.axaml", "<UserControl " + Header + "><Button Wrapping.IsEnabled=\"False\" /></UserControl>");
        AssertNullWhereDrawn(Test(View(List(Themed), theme)));
        Write("Other.axaml", "<Styles " + Header + "><Style Selector=\"Button\"><Setter xmlns:p=\"using:XamlQuality.Tests.Rules.ItemFakes\" Property=\"(p:Wrapping.IsEnabled)\" Value=\"False\" /></Style></Styles>");
        AssertNullWhereDrawn(Test(View(List(Themed), theme)));

        // A setter may name its property in a Setter.Property element, which the XAML compiler takes as well, and a
        // skip names what it sets.
        foreach ((string setter, string sets) in ((string, string)[])
                 [
                     ("<Setter Value=\"False\"><Setter.Property>IsEnabled</Setter.Property></Setter>", "sets IsEnabled to False"),
                     ("<Setter Value=\"False\"><Setter.Property>(InputElement.IsEnabled)</Setter.Property></Setter>", "sets IsEnabled to False"),
                     ("<Setter Value=\"{Binding Go}\"><Setter.Property>Button.Command</Setter.Property></Setter>", "sets Command to {Binding Go}"),
                 ])
        {
            Write("Other.axaml", "<Styles " + Header + "><Style Selector=\"Button\">" + setter + "</Style></Styles>");
            XamlSkip inElement = AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent")), Style("ListBoxItem:disabled", Background("{x:Null}")))));
            Assert.Contains("since a Setter at Other.axaml", inElement.Reason, StringComparison.Ordinal);
            Assert.Contains(sets, inElement.Reason, StringComparison.Ordinal);
        }

        Write("Other.axaml", "<Styles " + Header + "><Style Selector=\"Button\"><Setter Value=\"True\"><Setter.Property>IsEnabled</Setter.Property></Setter></Style></Styles>");
        AssertNullWhereDrawn(Test(View(List(Themed), theme)));

        // The list, what holds it and their template are named before markup elsewhere that may disable them.
        Write("A.axaml", "<UserControl " + Header + "><Button IsEnabled=\"False\" /></UserControl>");
        string clearing = Style("ListBoxItem:disabled", Background("{x:Null}"));
        XamlSkip list = AssertSkipped(Test(View(List(Themed + " IsEnabled=\"False\""), Theme(Background("Transparent")), clearing)));
        Assert.Contains("since the ListBox at View.axaml", list.Reason, StringComparison.Ordinal);
        XamlSkip owned = AssertSkipped(Test(View(List(Themed + " InputElement.IsEnabled=\"False\""), Theme(Background("Transparent")), clearing)));
        Assert.Contains("since the ListBox at View.axaml", owned.Reason, StringComparison.Ordinal);
        XamlSkip holder = AssertSkipped(Test(View("<StackPanel IsEnabled=\"{Binding Ready}\">" + List(Themed) + "</StackPanel>", Theme(Background("Transparent")), clearing)));
        Assert.Contains("since the StackPanel at View.axaml", holder.Reason, StringComparison.Ordinal);
        XamlSkip template = AssertSkipped(Test(View(List(Themed),
            Theme(Background("Transparent"), template: "<Border Background=\"{TemplateBinding Background}\" IsEnabled=\"False\">" + Presenter + "</Border>"), clearing)));
        Assert.Contains("in their template sets IsEnabled to False", template.Reason, StringComparison.Ordinal);
        XamlSkip ownedInTemplate = AssertSkipped(Test(View(List(Themed),
            Theme(Background("Transparent"), template: "<Border Background=\"{TemplateBinding Background}\" InputElement.IsEnabled=\"False\">" + Presenter + "</Border>"), clearing)));
        Assert.Contains("in their template sets IsEnabled to False", ownedInTemplate.Reason, StringComparison.Ordinal);
        XamlSkip elsewhere = AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent")), clearing)));
        Assert.Contains("since the Button at A.axaml", elsewhere.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A list that always selects a row, or whose markup selects one, starts with that row in :selected, so a
    /// style that paints a part of the rows' template while they are selected may paint it from the start:
    /// measured, with SelectionMode="AlwaysSelected" or SelectedIndex="0", the first row's Border took a
    /// ListBoxItem:selected style's Red. The list, its theme, its type's default, as a TabControl's, the text an
    /// editable combo box is given, or a setter anywhere in the scan may select one; where nothing does,
    /// :selected is a state they start out of. It holds only of the rows selected, so where the rows' own
    /// Background is null, the rest still draw it null.
    /// </summary>
    [Fact]
    public void RowsThatMarkupMaySelect_MayStartInSelected()
    {
        const string Bare = "<Panel><Border Name=\"Bg\" />" + Presenter + "</Panel>";
        string painting = Style("ListBoxItem:selected /template/ Border#Bg", Background("Red"));
        string transparent = Theme(Background("Transparent"), template: Bare);

        foreach (string selecting in (string[])
                 [
                     "SelectionMode=\"AlwaysSelected\"",
                     "SelectionMode=\"Multiple,AlwaysSelected\"",
                     "SelectedIndex=\"0\"",
                     "SelectedIndex=\"{Binding Current}\"",
                     "SelectedItem=\"{Binding Current}\"",
                 ])
        {
            XamlSkip skip = AssertSkipped(Test(View(List(Themed + " " + selecting), transparent, painting)));
            Assert.Contains("paints a part of the template of the ListBoxItem rows with Red", skip.Reason, StringComparison.Ordinal);
        }

        AssertSkipped(Test(View(List(Themed), transparent, painting + Style("ListBox", "<Setter Property=\"SelectedIndex\" Value=\"0\" />"))));
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Property=\"IsSelected\" Value=\"{Binding Chosen}\" />", template: Bare), painting)));
        AssertSkipped(Test(View(List(Themed), "<ControlTheme x:Key=\"{x:Type ListBox}\" TargetType=\"ListBox\"><Setter Property=\"SelectionMode\" Value=\"AlwaysSelected\" /></ControlTheme>"
                                              + transparent, painting)));
        AssertSkipped(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" " + Themed + "><ListBox.SelectedItem><Binding Path=\"Current\" /></ListBox.SelectedItem></ListBox>",
            transparent, painting)));

        foreach (string none in (string[])["", "SelectionMode=\"Multiple\"", "SelectedIndex=\"-1\"", "SelectedItem=\"{x:Null}\""])
        {
            AssertNoBackground(Test(View(List(Themed + " " + none), transparent, painting)));
        }

        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Property=\"IsSelected\" Value=\"False\" />", template: Bare), painting)));

        // Every spelling the XAML compiler takes: after an owner the list is, a SelectionMode by number, a setter's
        // in parentheses. A property attached by a type the list is not is a property of its own.
        foreach (string selecting in (string[])
                 [
                     "ListBox.SelectedIndex=\"0\"",
                     "ItemsControl.SelectedIndex=\"0\"",
                     "Unknowable.SelectedIndex=\"0\"",
                     "ListBox.SelectionMode=\"AlwaysSelected\"",
                     "SelectionMode=\"4\"",
                     "SelectionMode=\"5\"",
                 ])
        {
            AssertSkipped(Test(View(List(Themed + " " + selecting), transparent, painting)));
        }

        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Property=\"(ListBoxItem.IsSelected)\" Value=\"True\" />", template: Bare), painting)));
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Value=\"True\"><Setter.Property>IsSelected</Setter.Property></Setter>", template: Bare), painting)));
        AssertSkipped(Test(View(List(Themed), transparent, painting + Style("ListBox", "<Setter Value=\"0\"><Setter.Property>SelectedIndex</Setter.Property></Setter>"))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Value=\"False\"><Setter.Property>IsSelected</Setter.Property></Setter>", template: Bare), painting)));
        foreach (string none in (string[])["SelectionMode=\"1\"", "SelectionMode=\"Multiple, Toggle\"", "SelectedIndex=\"-2\"", "Wrapping.SelectedIndex=\"0\""])
        {
            AssertNoBackground(Test(View(List(Themed + " " + none), transparent, painting)));
        }

        // A style that clears the rows selected names why they may start so, and a :not() of :selected may still hold of every row.
        XamlSkip cleared = AssertSkipped(Test(View(List(Themed + " SelectedIndex=\"0\""), Theme(Background("Transparent")), Style("ListBoxItem:selected", Background("{x:Null}")))));
        Assert.Contains(":selected, which some of them may start in, since the ListBox at", cleared.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(View(List(Themed + " SelectedIndex=\"0\""), Theme(Background("{x:Null}")), Style("ListBoxItem:not(:selected)", Background("Transparent")))));
        AssertSkipped(Test(View(List(Themed + " SelectedIndex=\"0\""), Theme(Background("Transparent")), Style(":selected", Background("{x:Null}")))));
        AssertSkipped(Test(View(List(Themed + " SelectedIndex=\"0\""), Theme(Background("Transparent")),
            Style(":selected", "<Setter Property=\"Template\"><ControlTemplate><Border /></ControlTemplate></Setter>"))));

        // The list's theme is named before a setter anywhere else that selects.
        XamlSkip themed = AssertSkipped(Test(View(List(Themed),
            "<ControlTheme x:Key=\"{x:Type ListBox}\" TargetType=\"ListBox\"><Setter Property=\"SelectionMode\" Value=\"AlwaysSelected\" /></ControlTheme>"
            + Theme(Background("Transparent")), Style("ListBoxItem:selected", Background("{x:Null}")))));
        Assert.Contains("since the ControlTheme at", themed.Reason, StringComparison.Ordinal);

        // A tab control always selects a tab, and an editable combo box selects the row its text names.
        string tabs = "<ControlTheme x:Key=\"RowTheme\" TargetType=\"TabItem\">" + Background("Transparent")
                      + "<Setter Property=\"Template\"><ControlTemplate>" + Bare + "</ControlTemplate></Setter></ControlTheme>";
        AssertSkipped(Test(View(List(Themed, host: "TabControl"), tabs, Style("TabItem:selected /template/ Border#Bg", Background("Red")))));
        AssertNoBackground(Test(View(List(Themed, host: "TabControl"), tabs, Style("TabItem:pointerover /template/ Border#Bg", Background("Red")))));
        string combo = Style("ComboBoxItem:selected /template/ Border#Bg", Background("Red"));
        AssertSkipped(Test(View(List(Themed + " Text=\"{Binding Name}\"", host: "ComboBox"), transparent, combo)));
        AssertSkipped(Test(View(List(Themed + " ComboBox.Text=\"{Binding Name}\"", host: "ComboBox"), transparent, combo)));
        AssertNoBackground(Test(View(List(Themed, host: "ComboBox"), transparent, combo)));

        AssertNullWhereDrawn(Test(View(List(Themed + " SelectedIndex=\"0\""), Theme(Background("{x:Null}")), Style("ListBoxItem:selected", Background("Transparent")))));
        AssertNullWhereDrawn(Test(View(List(Themed + " SelectedIndex=\"0\""), Theme(Background("{x:Null}") + Style("^:selected", Background("Transparent"))))));
    }

    /// <summary>
    /// A selection written after an owner the list is not, that holds the list's own, selects a row from the start by a
    /// binding, the one value the XAML compiler takes there: measured, a list written
    /// <c>ComboBox.SelectedIndex="{Binding Zero}"</c> started with its first row selected, which a <c>:selected</c>
    /// style then painted. A literal there does not load, and an owner that holds another selects nothing.
    /// </summary>
    [Fact]
    public void ASelectionAfterAnotherOwner_SelectsByABinding()
    {
        const string Bare = "<Panel><Border Name=\"Bg\" />" + Presenter + "</Panel>";
        string painting = Style("ListBoxItem:selected /template/ Border#Bg", Background("Red"));
        string transparent = Theme(Background("Transparent"), template: Bare);

        AssertSkipped(Test(View(List(Themed + " ComboBox.SelectedIndex=\"{Binding Zero}\""), transparent, painting)));
        AssertSkipped(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" " + Themed + "><ComboBox.SelectedIndex><Binding Path=\"Zero\" /></ComboBox.SelectedIndex></ListBox>",
            transparent, painting)));
        AssertNoBackground(Test(View(List(Themed + " ComboBox.SelectedIndex=\"0\""), transparent, painting)));
        AssertNoBackground(Test(View(List(Themed + " Paint.SelectedIndex=\"{Binding Zero}\""), transparent, painting)));

        // A copy of the property, which a consumer's list holds beside the CLR property it declares, is the same property.
        AssertSkipped(Test(View(List(Themed + " CopyingList.SelectedIndex=\"{Binding Zero}\""), transparent, painting)));
    }

    /// <summary>
    /// A style that paints a part of the rows' template while they are selected, as Fluent's own row themes do
    /// at 12.1.3, paints the rows selected and no other. So where markup may select a row, a null Background
    /// the rest draw is still reported, and rows whose template draws nothing are not decided, since the rows
    /// selected draw there. One that may paint the part in every row, for :disabled where markup may disable
    /// them, or for :not(:selected), leaves both undecided.
    /// </summary>
    [Fact]
    public void APartPaintedWhileSelected_LeavesTheOtherRowsAsTheyWere()
    {
        const string Fluent = "<ContentPresenter Name=\"PART_ContentPresenter\" Background=\"{TemplateBinding Background}\" "
                              + "Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\" />";
        const string Bare = "<ContentPresenter Name=\"PART_ContentPresenter\" Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\" />";
        const string Part = "/template/ ContentPresenter#PART_ContentPresenter";
        string selecting = Themed + " SelectedItem=\"{Binding Current}\"";
        string accent = Background("{DynamicResource SystemControlHighlightListAccentLowBrush}");
        string selected = Style("^:selected", Style("^ " + Part, accent) + Style("^:not(:focus) " + Part, accent));
        string bound = Background("{Binding RowBrush}");
        string transparent = Background("Transparent");
        string fill = "<Setter Property=\"Fill\" Value=\"Red\" />";

        // Nested in the row theme, as Fluent writes it.
        AssertNullWhereDrawn(Test(View(List(selecting), Theme(bound + selected, template: Fluent))));
        XamlSkip nested = AssertSkipped(Test(View(List(selecting), Theme(transparent + selected, template: Bare))));
        Assert.Contains("in a nested style", nested.Reason, StringComparison.Ordinal);
        AssertNoBackground(Test(View(List(Themed), Theme(transparent + selected, template: Bare))));

        // In a style that reaches the list, with a brush or with a binding.
        foreach (string value in (string[])["Red", "{Binding Accent}"])
        {
            string style = Style("ListBoxItem:selected " + Part, Background(value));
            AssertNullWhereDrawn(Test(View(List(selecting), Theme(bound, template: Fluent), style)));
            AssertSkipped(Test(View(List(selecting), Theme(transparent, template: Bare), style)));
        }

        // A fill, a theme or a template given to a part while selected.
        AssertNullWhereDrawn(Test(View(List(selecting), Theme(bound, template: Fluent), Style("ListBoxItem:selected /template/ Rectangle", fill))));
        AssertNullWhereDrawn(Test(View(List(selecting), Theme(bound + Style("^:selected /template/ Rectangle", fill), template: Fluent))));
        AssertSkipped(Test(View(List(selecting), Theme(transparent + Style("^:selected /template/ Rectangle", fill), template: Bare))));

        // Selected and disabled holds only of the rows selected; disabled alone, or not selected, may hold of every row.
        string disabled = selecting + " IsEnabled=\"False\"";
        AssertNullWhereDrawn(Test(View(List(disabled), Theme(bound + Style("^:selected:disabled " + Part, accent), template: Fluent))));
        AssertSkipped(Test(View(List(disabled), Theme(bound + Style("^:disabled " + Part, accent), template: Fluent))));
        AssertSkipped(Test(View(List(selecting), Theme(bound, template: Fluent), Style("ListBoxItem:not(:selected) " + Part, Background("Red")))));

        // One in another file, which may reach the list or not.
        Write("Rows.axaml", "<Styles " + Header + ">" + Style("ListBoxItem:selected " + Part, Background("Red"))
                            + Style("ListBoxItem:selected /template/ Rectangle", fill) + "</Styles>");
        AssertNullWhereDrawn(Test(View(List(selecting), Theme(bound, template: Fluent))));
        XamlSkip elsewhere = AssertSkipped(Test(View(List(selecting), Theme(transparent, template: Bare))));
        Assert.Contains("no scope this markup shows reaching", elsewhere.Reason, StringComparison.Ordinal);
    }

    /// <summary>A part painted only in an interaction state changes nothing, in every such state, whether the rows' Background can be null or their template draws nothing.</summary>
    [Fact]
    public void EveryInteractionState_IsAState()
    {
        foreach (string state in (string[])["disabled", "focus", "focus-visible", "focus-within", "pointerover", "pressed", "selected"])
        {
            AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")),
                Style("ListBoxItem:" + state + " /template/ ContentPresenter", Background("Red")))));
            AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent"), template: BareTemplate),
                Style("ListBoxItem:" + state + " /template/ ContentPresenter", Background("Red")))));
        }
    }

    /// <summary>A style that may reach the list from another file depends on where the view is placed, when it could change the answer.</summary>
    [Fact]
    public void AStyleInAnotherFile_IsSkipped()
    {
        Write("Rows.axaml", "<Styles " + Header + ">" + Style("ListBoxItem", Background("{x:Null}")) + "</Styles>");

        XamlSkip skip = AssertSkipped(Test(View(List())));

        Assert.Contains("no scope this markup shows reaching", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>One that may clear some rows from another file is not decided either.</summary>
    [Fact]
    public void AStyleForSomeRowsInAnotherFile_IsSkipped()
    {
        Write("Rows.axaml", "<Styles " + Header + ">" + Style("ListBoxItem.plain", Background("{x:Null}")) + "</Styles>");

        AssertSkipped(Test(View(List())));
    }

    /// <summary>One that paints the rows from another file changes nothing for rows that are clean, and may for rows whose Background can be null.</summary>
    [Fact]
    public void AStyleInAnotherFileThatPaints_MattersOnlyWhereTheBackgroundCanBeNull()
    {
        Write("Rows.axaml", "<Styles " + Header + ">" + Style("ListBoxItem", Background("Transparent")) + "</Styles>");

        AssertClean(Test(View(List())));
        AssertSkipped(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")))));
    }

    // ── The row theme ──────────────────────────────────────────────────────────

    /// <summary>A row theme's own binding with no TargetNullValue, as the one that arose.</summary>
    [Fact]
    public void ARowThemeBoundWithNoTargetNullValue_IsReported()
    {
        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{Binding RowBrush}")))));

        Assert.Contains("the ControlTheme sets it at", finding.Message, StringComparison.Ordinal);
        Assert.Contains("the Border at", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>With a non-null TargetNullValue the same theme is clean.</summary>
    [Fact]
    public void ARowThemeBoundWithATargetNullValue_IsClean() =>
        AssertClean(Test(View(List(Themed),
            Theme(Background("{Binding RowBrush, TargetNullValue={x:Static Brushes.Transparent}}")))));

    /// <summary>A theme based on nothing, with no background, leaves the property's default: null.</summary>
    [Fact]
    public void ARowThemeBasedOnNothingWithNoBackground_IsReported()
    {
        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(Themed), Theme())));

        Assert.Contains("BasedOn nothing", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Set Background to Transparent in the theme", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A theme based on nothing, with no template, leaves the row the default template a ContentControl
    /// has, whose ContentPresenter draws the row's background: measured, such a row took the click when
    /// that was Transparent.
    /// </summary>
    [Fact]
    public void ARowThemeBasedOnNothingWithNoTemplate_TakesTheDefaultTemplate()
    {
        AssertClean(Test(View(List(Themed), Theme(Background("Transparent"), template: string.Empty))));
        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}"), template: string.Empty))));
        Assert.Contains("the template ContentControl registers by default", finding.Message, StringComparison.Ordinal);

        XamlFinding byDefault = AssertNullWhereDrawn(Test(View(List(Themed), Theme(template: string.Empty))));
        Assert.Contains("BasedOn nothing", byDefault.Message, StringComparison.Ordinal);
    }

    /// <summary>A template of null presents nothing at all, whatever the row's background: measured, such a row had no height.</summary>
    [Fact]
    public void ANullTemplate_DrawsNoBackground()
    {
        XamlFinding finding = AssertNoBackground(Test(View(List(Themed),
            Theme(Background("Transparent") + "<Setter Property=\"Template\" Value=\"{x:Null}\" />", template: string.Empty))));

        Assert.Contains("is null, so they present nothing at all", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A row theme based on another in the scan takes what that one sets, and where its chain ends.</summary>
    [Fact]
    public void ARowThemeBasedOnAnotherInTheScan_IsFollowed()
    {
        static string Based(string baseSetters, string baseBasedOn) =>
            Theme(basedOn: "{StaticResource BaseRow}")
            + "<ControlTheme x:Key=\"BaseRow\" TargetType=\"ListBoxItem\"" + (baseBasedOn.Length > 0 ? " BasedOn=\"" + baseBasedOn + "\"" : string.Empty) + ">"
            + baseSetters + "</ControlTheme>";

        XamlFinding finding = AssertNullWhereDrawn(Test(View(List(Themed), Based(Background("{Binding RowBrush}"), string.Empty))));
        Assert.Contains("no TargetNullValue", finding.Message, StringComparison.Ordinal);
        AssertClean(Test(View(List(Themed), Based(string.Empty, FrameworkRow))));
    }

    /// <summary>A row theme based on an implicit theme that another view defines, and that sets the background, may reach it or not.</summary>
    [Fact]
    public void ARowThemeBasedOnAnImplicitThemeInAnotherView_IsSkipped()
    {
        Write("Other.axaml", View(string.Empty,
            "<ControlTheme x:Key=\"{x:Type ListBoxItem}\" TargetType=\"ListBoxItem\">" + Background("{x:Null}") + "</ControlTheme>"));

        XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(basedOn: FrameworkRow))));

        Assert.Contains("in no scope this markup shows reaching it", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A theme reached through the list's own theme, as a custom list's is.</summary>
    [Fact]
    public void ARowThemeTheListsThemeSets_IsFollowed() =>
        AssertNullWhereDrawn(Test(View(List("Theme=\"{StaticResource ListTheme}\""),
            Theme(Background("{Binding RowBrush}"))
            + "<ControlTheme x:Key=\"ListTheme\" TargetType=\"ListBox\" BasedOn=\"{StaticResource {x:Type ListBox}}\">"
            + "<Setter Property=\"ItemContainerTheme\" Value=\"{StaticResource RowTheme}\" /></ControlTheme>")));

    /// <summary>
    /// A row theme written after another owner that holds the list's ItemContainerTheme is the list's by a binding,
    /// which a DynamicResource is: measured, rows given one by <c>ComboBox.ItemContainerTheme="{DynamicResource ...}"</c>,
    /// or by it as a property element, took it over their implicit theme. One after an owner that holds another
    /// leaves them the implicit theme.
    /// </summary>
    [Fact]
    public void ARowThemeAfterAnotherOwner_IsTheListsByABinding()
    {
        string themes = "<ControlTheme x:Key=\"{x:Type ListBoxItem}\" TargetType=\"ListBoxItem\">" + Background("{x:Null}")
                        + "<Setter Property=\"Template\"><ControlTemplate>" + DrawingTemplate + "</ControlTemplate></Setter></ControlTheme>"
                        + Theme(Background("Transparent"));

        AssertClean(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" ComboBox.ItemContainerTheme=\"{DynamicResource RowTheme}\" />", themes)));
        AssertClean(Test(View("<ListBox ItemsSource=\"{Binding Rows}\"><ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"RowTheme\" />"
                              + "</ComboBox.ItemContainerTheme></ListBox>", themes)));
        AssertNullWhereDrawn(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" Paint.ItemContainerTheme=\"{DynamicResource RowTheme}\" />", themes)));
    }

    /// <summary>
    /// A setter sets the rows' Background after an owner only where that owner holds it, in any spelling: one for an
    /// attached Background of the same name sets another property, and the rows keep their own; measured, a row theme
    /// whose Background was null, with a setter of <c>Transparent</c> for an attached <c>(p:Paint.Background)</c>,
    /// left the rows drawing null where their template took it.
    /// </summary>
    [Fact]
    public void ASetterForAnotherOwnersBackground_LeavesTheRowsOwn()
    {
        foreach (string attached in (string[])
                 [
                     "<Setter Property=\"(Paint.Background)\" Value=\"Transparent\" />",
                     "<Setter xmlns:p=\"using:XamlQuality.Tests.Rules.ItemFakes\" Property=\"(p:Paint.Background)\" Value=\"Transparent\" />",
                     "<Setter Property=\"Paint.Background\" Value=\"Transparent\" />",
                     "<Setter Value=\"Transparent\"><Setter.Property>Paint.Background</Setter.Property></Setter>",
                 ])
        {
            AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}") + attached))));
            AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}") + Style("^", attached)))));
            AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem", Background("{x:Null}") + attached))));
        }

        AssertClean(Test(View(List(Themed), Theme(Background("{x:Null}") + "<Setter Property=\"(Border.Background)\" Value=\"Transparent\" />"))));
        AssertClean(Test(View(List(), styles: Style("ListBoxItem", Background("{x:Null}") + "<Setter Property=\"(Border.Background)\" Value=\"Transparent\" />"))));
    }

    /// <summary>A theme the scan does not hold cannot be read.</summary>
    [Fact]
    public void ARowThemeTheScanDoesNotHold_IsSkipped() =>
        AssertSkipped(Test(View(List("ItemContainerTheme=\"{StaticResource Elsewhere}\""))));

    /// <summary>An implicit row theme in another view may reach the list, depending on where the view is placed.</summary>
    [Fact]
    public void AnImplicitRowThemeInAnotherView_IsSkipped()
    {
        Write("Other.axaml", View(string.Empty,
            "<ControlTheme x:Key=\"{x:Type ListBoxItem}\" TargetType=\"ListBoxItem\">" + Background("{x:Null}") + "</ControlTheme>"));

        AssertSkipped(Test(View(List())));
    }

    /// <summary>
    /// States leave a null base as it starts, and so does a style that paints only the rows it picks by a
    /// property the theme binds to each row's item, which the data decides row by row, as a
    /// WPF-to-Avalonia port's recent-line highlight does.
    /// </summary>
    [Fact]
    public void StatesAndPropertyStyles_LeaveANullBaseNull() =>
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(
            Background("{Binding RowBrush}")
            + "<Setter Property=\"Tag\" Value=\"{Binding Kind}\" />"
            + Style("^[Tag=recent]", Style("^[IsEnabled=True]", Background("Green")))
            + Style("^:pointerover", Background("Red"))
            + Style("^:selected", Background("Blue"))))));

    /// <summary>
    /// A nested style that may hold of every row in the state rows start in may paint them all: measured, a
    /// theme whose own Background was null and whose nested ^:not(.nope) set Transparent took the click
    /// across every row. A :not() of a state holds only while a row is out of it. One that asks for a class
    /// picks no row, and leaves the rows reported.
    /// </summary>
    [Fact]
    public void ANestedStyleThatMayHoldOfEveryRow_MayPaintThem()
    {
        foreach (string selector in (string[])["^:not(.nope)", "^:not(:selected)", "^[IsEnabled=True]"])
        {
            XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}") + Style(selector, Background("Transparent"))))));
            Assert.Contains("may hold of every row", skip.Reason, StringComparison.Ordinal);
        }

        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}") + Style("^.nope", Background("Transparent"))))));
    }

    /// <summary>
    /// A nested style that may give some rows, or every row, another template or theme changes what draws
    /// them, whichever way: measured, a nested ^[IsEnabled=True] whose template drew nothing left rows whose
    /// theme's own template drew Transparent missing the click. One in a state no row starts in changes
    /// nothing this rule decides.
    /// </summary>
    [Fact]
    public void ANestedStyleThatGivesRowsAnotherTemplate_IsSkipped()
    {
        string bare = "<Setter Property=\"Template\"><ControlTemplate>" + BareTemplate + "</ControlTemplate></Setter>";

        XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + Style("^[IsEnabled=True]", bare)))));
        Assert.Contains("sets Template on some or all of", skip.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + Style("^[Tag=recent]", "<Setter Property=\"Theme\" Value=\"{StaticResource Other}\" />")))));
        AssertClean(Test(View(List(Themed), Theme(Background("Transparent") + Style("^:pointerover", bare)))));
    }

    /// <summary>A nested style that clears the background in a state can break the rows.</summary>
    [Fact]
    public void AStateThatClearsTheBackground_IsSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(
            Background("Transparent")
            + Style("^:pointerover", Background("{x:Null}"))))));

        Assert.Contains("while they are pointed at", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A nested style that may clear some rows by a class or a property is not decided.</summary>
    [Fact]
    public void ANestedStyleThatClearsSomeRows_IsSkipped()
    {
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + Style("^.plain", Background("{x:Null}"))))));
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + Style("^[Tag=plain]", Background("{Binding RowBrush}"))))));
    }

    /// <summary>A style nested in a nested style is read too.</summary>
    [Fact]
    public void AStyleNestedInANestedStyle_IsRead() =>
        AssertSkipped(Test(View(List(Themed), Theme(
            Background("Transparent")
            + Style("^[Tag=a]", Style("^[IsEnabled=True]", Background("{x:Null}")))))));

    /// <summary>A nested style whose selector is the theme alone applies in every state, and wins over the theme's own setter.</summary>
    [Fact]
    public void ANestedStyleForTheThemeAlone_IsTheBase() =>
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(
            Background("Transparent")
            + Style("^", Background("{x:Null}"))))));

    /// <summary>A theme that paints a part of its own template in a nested style may draw where the rows' Background is null.</summary>
    [Fact]
    public void ARowThemePaintingAPartOfItsTemplate_IsSkipped() =>
        AssertSkipped(Test(View(List(Themed), Theme(
            Background("{x:Null}")
            + Style("^ /template/ ContentPresenter", Background("Transparent"))))));

    /// <summary>
    /// A theme that gives a part of its template a binding or a TemplateBinding in a nested style may draw
    /// there: measured, a part given the row's Transparent background that way drew it. So a template that
    /// draws nothing else is not decided.
    /// </summary>
    [Fact]
    public void ARowThemeBindingAPartOfItsTemplate_MayDrawThere()
    {
        const string Bare = "<Panel Name=\"Bg\">" + Presenter + "</Panel>";

        foreach (string binding in (string[])["{TemplateBinding Background}", "{Binding $parent[ListBoxItem].Background}"])
        {
            XamlSkip skip = AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent") + Style("^ /template/ Panel#Bg", Background(binding)), template: Bare))));
            Assert.Contains("which may draw where the rows themselves draw nothing", skip.Reason, StringComparison.Ordinal);
        }

        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent") + Style("^ /template/ Panel#Bg", Background("{x:Null}")), template: Bare))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent") + Style("^:selected /template/ Panel#Bg", Background("{TemplateBinding Background}")), template: Bare))));
    }

    /// <summary>A theme that paints a part of its template only in a state changes nothing, whether the rows' Background can be null or their template draws nothing.</summary>
    [Fact]
    public void ARowThemePaintingAPartInAState_ChangesNothing()
    {
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(
            Background("{Binding RowBrush}")
            + Style("^:pointerover /template/ ContentPresenter", Background("Red"))))));
        AssertNoBackground(Test(View(List(Themed), Theme(
            Background("Transparent")
            + Style("^:pointerover /template/ ContentPresenter", Background("Red")), template: BareTemplate))));
    }

    /// <summary>
    /// A theme that clears a part of its own template in a nested style may leave that part drawing nothing,
    /// whether the part draws the row's background or paints its own.
    /// </summary>
    [Fact]
    public void ARowThemeClearingAPartOfItsTemplate_IsSkipped()
    {
        AssertSkipped(Test(View(List(Themed), Theme(
            Background("Transparent")
            + Style("^ /template/ Border", Background("{x:Null}"))))));
        AssertSkipped(Test(View(List(Themed), Theme(
            Style("^ /template/ Border", Background("{x:Null}")),
            template: "<Border Background=\"Transparent\"><ContentPresenter Content=\"{TemplateBinding Content}\" /></Border>"))));
    }

    /// <summary>
    /// A theme's <c>TargetType</c> is the type its prefix names, as an owner is: measured, a list's rows took nothing from
    /// a theme whose <c>TargetType</c> named, through a <c>clr-namespace:</c> prefix, a <c>ListBoxItem</c> subclass whose name
    /// another scanned type carries, since the framework applies no theme its rows' style key is not. A name that resolves
    /// to no one type is not followed either.
    /// </summary>
    [Fact]
    public void ATargetTypeIsTheTypeItsPrefixNames()
    {
        static string Targeting(string target) =>
            View(List(Themed), "<ControlTheme x:Key=\"RowTheme\" xmlns:m=\"using:XamlQuality.Tests.Rules.ItemFakes\" TargetType=\""
                + target + "\">" + Background("{x:Null}")
                + "<Setter Property=\"Template\"><ControlTemplate>" + DrawingTemplate + "</ControlTemplate></Setter></ControlTheme>");

        foreach (string target in (string[])["m:AmbiguousRow", "{x:Type m:AmbiguousRow}"])
        {
            Assert.Contains("does not apply it", AssertSkipped(Test(Targeting(target))).Reason, StringComparison.Ordinal);
        }

        Assert.Contains("does not resolve to one type", AssertSkipped(Test(Targeting("AmbiguousRow"))).Reason, StringComparison.Ordinal);
        AssertNullWhereDrawn(Test(Targeting("ListBoxItem")));
    }

    // ── The row template ───────────────────────────────────────────────────────

    /// <summary>A template that draws no background leaves even a Transparent row taking no click: measured. It is reported, with the fix.</summary>
    [Fact]
    public void ARowTemplateThatDrawsNoBackground_IsReported()
    {
        XamlFinding finding = AssertNoBackground(Test(Rows(BareTemplate)));

        Assert.Contains("no element of their template at", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Background=\"{TemplateBinding Background}\"", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A template that paints a brush of its own draws a background, as a Background or as a Fill, whatever the row's is.</summary>
    [Fact]
    public void ARowTemplateWithABrushOfItsOwn_IsClean()
    {
        AssertClean(Test(Rows("<Border Background=\"Transparent\"><ContentPresenter Content=\"{TemplateBinding Content}\" /></Border>", "{x:Null}")));
        AssertClean(Test(Rows("<Panel><Rectangle Fill=\"Transparent\" /><ContentPresenter Content=\"{TemplateBinding Content}\" /></Panel>", "{x:Null}")));

        // What a content property's element holds is the element's content, written in the template.
        AssertClean(Test(Rows("<Border><Border.Child><Border Background=\"Transparent\" /></Border.Child></Border>", "{x:Null}")));
    }

    /// <summary>A binding relative to the templated parent draws the row's background, as a TemplateBinding does, in either spelling.</summary>
    [Fact]
    public void ATemplatedParentBinding_DrawsTheRowsBackground()
    {
        foreach (string source in (string[])["{RelativeSource TemplatedParent}", "{RelativeSource Mode=TemplatedParent}"])
        {
            string byBinding = "<Border Background=\"{Binding Background, RelativeSource=" + source + "}\">" + Presenter + "</Border>";

            AssertNullWhereDrawn(Test(Rows(byBinding, "{x:Null}")));
            AssertClean(Test(Rows(byBinding)));
        }
    }

    /// <summary>
    /// A template's element takes a Background written after another owner where that owner holds its own, and only
    /// through a binding, which is all the XAML compiler takes there: measured, a <c>Panel</c> written
    /// <c>Border.Background="{TemplateBinding Background}"</c>, or given a binding to the templated parent as a
    /// property element, drew the row's background, and one written <c>Border.Background="Red"</c> did not load. An
    /// attached Background of the same name is another property, which the element does not draw.
    /// </summary>
    [Fact]
    public void ABackgroundAfterAnotherOwner_IsTheElementsOwnOnlyByABinding()
    {
        foreach (string taking in (string[])
                 [
                     "<Panel Border.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>",
                     "<Border Panel.Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>",
                     "<Panel TemplatedControl.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>",
                     "<ContentPresenter Border.Background=\"{TemplateBinding Background}\" Content=\"{TemplateBinding Content}\" />",
                     "<Panel><Border.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\" /></Border.Background>"
                     + Presenter + "</Panel>",
                 ])
        {
            AssertClean(Test(Rows(taking)));
            AssertNullWhereDrawn(Test(Rows(taking, "{x:Null}")));
        }

        AssertClean(Test(Rows("<Panel Border.Background=\"{DynamicResource Brush}\">" + Presenter + "</Panel>", "{x:Null}")));
        AssertNoBackground(Test(Rows("<Panel Border.Background=\"Transparent\">" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel Border.Background=\"{x:Null}\">" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel><Border.Background>Transparent</Border.Background>" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel><Border.Background><Run /></Border.Background>" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel Paint.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// A TemplateBinding or a path takes the row's Background after an owner only where that owner holds it: an
    /// attached Background of the same name is another property, which the row does not draw; measured, a
    /// <c>Border</c> bound to one painted its default and followed nothing the row's did.
    /// </summary>
    [Fact]
    public void ABindingToAnotherOwnersBackground_IsNotTheRows()
    {
        foreach (string other in (string[])
                 [
                     "{TemplateBinding Paint.Background}",
                     "{TemplateBinding (Paint.Background)}",
                     "{Binding (Paint.Background), RelativeSource={RelativeSource TemplatedParent}}",
                     "{Binding $parent[ListBoxItem].(Paint.Background)}",
                 ])
        {
            AssertSkipped(Test(Rows("<Border Background=\"" + other + "\">" + Presenter + "</Border>", "{x:Null}")));
        }

        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{Binding $parent[ListBoxItem].(Border.Background)}\">" + Presenter + "</Border>", "{x:Null}")));
    }

    /// <summary>
    /// An element that gives a property in two spellings that each set it, or may, decides nothing: the framework
    /// applies each in turn, in the order the document gives them, and a binding's value arrives once its data does;
    /// measured, a template's <c>Panel</c> written <c>Border.Background="{TemplateBinding Background}"</c> and then
    /// <c>TextBlock.Background="{DynamicResource Red}"</c> painted <c>Red</c>, and written the other way round drew the
    /// row's background. Where any value counts, as for what may disable the rows, each is read.
    /// </summary>
    [Fact]
    public void AnElementsPropertyInTwoSpellings_DecidesNothing()
    {
        foreach (string twice in (string[])
                 [
                     "<Panel Border.Background=\"{TemplateBinding Background}\" TextBlock.Background=\"{DynamicResource Red}\">" + Presenter + "</Panel>",
                     "<Panel TextBlock.Background=\"{DynamicResource Red}\" Border.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>",
                     "<Panel Background=\"Red\" Border.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>",
                     "<Border Background=\"Red\"><Border.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\" />"
                     + "</Border.Background>" + Presenter + "</Border>",
                 ])
        {
            XamlSkip skip = AssertSkipped(Test(Rows(twice, "{x:Null}")));
            Assert.Contains("which the framework applies in turn", skip.Reason, StringComparison.Ordinal);
        }

        AssertSkipped(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" ItemsControl.ItemsSource=\"{Binding Others}\" " + Themed + " />",
            Theme(Background("{x:Null}")))));

        // Another owner's property is not a spelling of the element's own.
        AssertNullWhereDrawn(Test(Rows("<Panel Background=\"{TemplateBinding Background}\" Paint.Background=\"Red\">" + Presenter + "</Panel>", "{x:Null}")));

        XamlSkip disabled = AssertSkipped(Test(View(List(Themed + " IsEnabled=\"True\" InputElement.IsEnabled=\"False\""),
            Theme(Background("{x:Null}") + Style("^:disabled", Background("Transparent"))))));
        Assert.Contains("may hold of every row", disabled.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// An owner outside the element's chain that declares accessors of its own names an attached property, which any
    /// value sets, and where its field holds the element's own property that value is the element's own: measured, a
    /// template's <c>Panel</c> written <c>BgAlias.Background="Red"</c>, where <c>BgAlias</c> is a static class whose field
    /// holds a control's <c>Background</c>, painted <c>Red</c>, as did one written after an object that adds itself as an
    /// owner of it and has accessors but no instance property. Only an owner with an instance property of the name takes
    /// nothing but a binding.
    /// </summary>
    [Fact]
    public void AnAttachedOwnerOfTheElementsOwnProperty_TakesAnyValue()
    {
        foreach (string owner in (string[])["BgAlias", "BgOwner"])
        {
            AssertClean(Test(Rows("<Panel " + owner + ".Background=\"Red\">" + Presenter + "</Panel>")));
            AssertClean(Test(Rows("<Panel " + owner + ".Background=\"Red\">" + Presenter + "</Panel>", "{x:Null}")));
            AssertNullWhereDrawn(Test(Rows("<Panel " + owner + ".Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>", "{x:Null}")));
        }
    }

    /// <summary>
    /// An owner names the property the field the XAML compiler takes for it holds: on an element, the one declared
    /// beside the instance CLR property it finds up the owner's chain, and in a setter, a TemplateBinding or a path, the
    /// owner's most-derived public one. Measured: a <c>Border</c> written
    /// <c>ShadowPanel.Background="{TemplateBinding Background}"</c>, where <c>ShadowPanel</c> derives from <c>Panel</c> and
    /// hides its field with a public one of another property, drew the row's background, as did one written after
    /// <c>ProtectedShadow</c>, which hides it with a protected one; and a row theme's setter for
    /// <c>ProtectedShadow.Background</c> set the row's.
    /// </summary>
    [Fact]
    public void TheFieldTheCompilerTakes_NamesTheProperty()
    {
        foreach (string owner in (string[])["ShadowPanel", "ProtectedShadow"])
        {
            AssertClean(Test(Rows("<Border " + owner + ".Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>")));
            AssertNullWhereDrawn(Test(Rows("<Border " + owner + ".Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>", "{x:Null}")));
        }

        // ProtectedShadow's public field is Panel's, which a setter, a TemplateBinding and a path take.
        AssertClean(Test(View(List(Themed), Theme("<Setter Property=\"ProtectedShadow.Background\" Value=\"Transparent\" />"))));
        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{TemplateBinding ProtectedShadow.Background}\">" + Presenter + "</Border>", "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{Binding (ProtectedShadow.Background), RelativeSource={RelativeSource TemplatedParent}}\">"
                                       + Presenter + "</Border>", "{x:Null}")));

        // ShadowPanel's public field is its own, which holds another property.
        AssertSkipped(Test(Rows("<Border Background=\"{TemplateBinding ShadowPanel.Background}\">" + Presenter + "</Border>", "{x:Null}")));
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}") + "<Setter Property=\"ShadowPanel.Background\" Value=\"Transparent\" />"))));

        // An element whose own type hides the field sets the property declared beside the CLR property it inherits.
        AssertNullWhereDrawn(Test(Rows("<ShadowPanel Border.Background=\"{TemplateBinding Background}\">" + Presenter + "</ShadowPanel>", "{x:Null}")));
    }

    /// <summary>
    /// An owner is the type its prefix's namespace holds, not whatever type its name finds: measured, a row theme's
    /// setter for <c>alt:TemplatedControl.Background</c>, a consumer's class of that name in another namespace, set
    /// another property than the row's, as one for <c>alt:Panel.Background</c> did. One written with the prefix of the
    /// framework's namespace is the framework's, found by its name, and one whose prefix nothing declares does not load.
    /// </summary>
    [Fact]
    public void AnOwnerIsTheTypeItsPrefixNames()
    {
        const string Alt = "xmlns:alt=\"using:XamlQuality.Tests.Rules.ItemFakes.Alt\"";
        foreach (string owner in (string[])["alt:TemplatedControl", "alt:Panel", "(alt:Panel)"])
        {
            string setter = owner.StartsWith('(')
                ? "<Setter " + Alt + " Property=\"(alt:Panel.Background)\" Value=\"{x:Null}\" />"
                : "<Setter " + Alt + " Property=\"" + owner + ".Background\" Value=\"{x:Null}\" />";
            AssertClean(Test(View(List(Themed), Theme(Background("Transparent") + setter))));
        }

        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}")
            + "<Setter " + Alt + " Property=\"alt:TemplatedControl.Background\" Value=\"Transparent\" />"))));
        AssertSkipped(Test(Rows("<Border " + Alt + " Background=\"{TemplateBinding alt:Panel.Background}\">" + Presenter + "</Border>", "{x:Null}")));
        AssertNoBackground(Test(Rows("<Panel " + Alt + " alt:Panel.Background=\"Red\">" + Presenter + "</Panel>")));

        AssertClean(Test(View(List(Themed), Theme(Background("{x:Null}")
            + "<Setter xmlns:fw=\"https://github.com/avaloniaui\" Property=\"fw:TemplatedControl.Background\" Value=\"Transparent\" />"))));
        AssertSkipped(Test(View(List(Themed), Theme(Background("{x:Null}") + "<Setter Property=\"nowhere:Panel.Background\" Value=\"Transparent\" />"))));

        // A clr-namespace prefix names its type as a using: one does, and a property element's owner is its prefix's too.
        const string AltClr = "xmlns:alt=\"clr-namespace:XamlQuality.Tests.Rules.ItemFakes.Alt;assembly=XamlQuality.Tests\"";
        AssertClean(Test(View(List(Themed), Theme(Background("Transparent")
            + "<Setter " + AltClr + " Property=\"alt:TemplatedControl.Background\" Value=\"{x:Null}\" />"))));
        AssertNoBackground(Test(Rows("<Panel " + Alt + "><alt:Panel.Background>Red</alt:Panel.Background>" + Presenter + "</Panel>")));

        // An owner in a URI that only an assembly's XmlnsDefinition maps to a type is not decided.
        const string Uri = "xmlns:u=\"https://example.test/alt\"";
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent")
            + "<Setter " + Uri + " Property=\"u:TemplatedControl.Background\" Value=\"{x:Null}\" />"))));
        AssertSkipped(Test(Rows("<Panel " + Uri + " u:Panel.Background=\"Red\">" + Presenter + "</Panel>")));

        // So is one in WPF's or MAUI's URI in Avalonia's markup, which neither Avalonia nor XamlX maps at 12.1.3: measured,
        // a row theme's setter for w:TemplatedControl.Background, with w mapped by a consumer's XmlnsDefinition to a type
        // of its own, set another property than the row's. In markup whose root is in that URI, it is the framework's.
        const string Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        foreach (string presentation in (string[])[Wpf, "http://schemas.microsoft.com/dotnet/2021/maui"])
        {
            AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent")
                + "<Setter xmlns:w=\"" + presentation + "\" Property=\"w:TemplatedControl.Background\" Value=\"{x:Null}\" />"))));
        }

        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("Transparent")
                + "<Setter Property=\"TemplatedControl.Background\" Value=\"{x:Null}\" />"))
            .Replace("https://github.com/avaloniaui", Wpf, StringComparison.Ordinal)));
        AssertNullWhereDrawn(Test(Rows("<Panel Border.Background=\"{TemplateBinding Background}\">" + Presenter + "</Panel>", "{x:Null}")
            .Replace("https://github.com/avaloniaui", Wpf, StringComparison.Ordinal)));
    }

    /// <summary>
    /// An owner with no prefix is the framework's type of that name, whatever consumer's type in the control's chain is
    /// called so too: measured, a row theme's setter for <c>ContentControl.Background</c>, on rows of a consumer's
    /// <c>ContentControl</c> that derives from <c>ListBoxItem</c> and hides the field with one of another property, set the
    /// rows' own <c>Background</c>. Which type the namespace names is not read, so where the two hold different properties
    /// nothing is decided.
    /// </summary>
    [Fact]
    public void AnOwnerTwoTypesInTheChainAreCalled_IsDecidedOnlyWhereTheyAgree()
    {
        static string Shadowed(string setters) =>
            View("<ContentControlList ItemsSource=\"{Binding Rows}\" " + Themed + " />",
                "<ControlTheme x:Key=\"RowTheme\" TargetType=\"ContentControl\">" + setters
                + "<Setter Property=\"Template\"><ControlTemplate>" + DrawingTemplate + "</ControlTemplate></Setter></ControlTheme>");

        AssertSkipped(TestWith(EmittedViews.Shadows, Shadowed(Background("Transparent")
            + "<Setter Property=\"ContentControl.Background\" Value=\"{x:Null}\" />")));

        // ListBoxItem names one type in the chain, the framework's, and the rows' own property.
        AssertNullWhereDrawn(TestWith(EmittedViews.Shadows, Shadowed(Background("Transparent")
            + "<Setter Property=\"ListBoxItem.Background\" Value=\"{x:Null}\" />")));
        AssertClean(TestWith(EmittedViews.Shadows, Shadowed(Background("Transparent"))));
    }

    /// <summary>
    /// A binding to the ancestor of the row's type that the row is draws the row's background: measured for
    /// <c>$parent</c> and <c>AncestorType</c>, at the nearest level, which each spells its own way, from 0 or
    /// from 1, in either tree. Past an ancestor of that type that the template puts between them, the row is
    /// the next.
    /// </summary>
    [Fact]
    public void ABindingToTheRowAsAnAncestor_DrawsItsBackground()
    {
        foreach (string background in (string[])
                 [
                     "{Binding $parent[ListBoxItem].Background}",
                     "{Binding $parent[ListBoxItem;0].Background}",
                     "{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem}}",
                     "{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, AncestorLevel=1}}",
                     "{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, Tree=Logical}}",
                     "{Binding Background, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type ListBoxItem}}}",
                 ])
        {
            string template = "<Border Background=\"" + background + "\"><ContentPresenter Content=\"{TemplateBinding Content}\" /></Border>";
            AssertNullWhereDrawn(Test(Rows(template, "{x:Null}")));
            AssertClean(Test(Rows(template)));
        }

        foreach (string background in (string[])
                 [
                     "{Binding $parent[ContentControl;1].Background}",
                     "{Binding Background, RelativeSource={RelativeSource AncestorType=ContentControl, AncestorLevel=2, Tree=Logical}}",
                 ])
        {
            string inside = "<ContentControl><Border Background=\"" + background + "\" /></ContentControl>";
            AssertNullWhereDrawn(Test(Rows(inside, "{x:Null}")));
            AssertClean(Test(Rows(inside)));
        }

        // In the logical tree, a type every element is is counted as any other.
        AssertNullWhereDrawn(Test(Rows("<Border><Border Background=\"{Binding $parent[Control;1].Background}\" />" + Presenter + "</Border>", "{x:Null}")));
    }

    /// <summary>A template held in a resource is followed.</summary>
    [Fact]
    public void ARowTemplateHeldInAResource_IsFollowed()
    {
        static string Held(string template) =>
            Theme("<Setter Property=\"Template\" Value=\"{StaticResource RowTemplate}\" />" + Background("Transparent"), template: string.Empty)
            + "<ControlTemplate x:Key=\"RowTemplate\">" + template + "</ControlTemplate>";

        AssertNoBackground(Test(View(List(Themed), Held(BareTemplate))));
        AssertClean(Test(View(List(Themed), Held(DrawingTemplate))));
    }

    /// <summary>
    /// Where something draws the row's Background, that value decides: whatever else in the template may
    /// paint, or does, leaves a null one reported and a brush clean.
    /// </summary>
    [Fact]
    public void WhereTheTemplateDrawsTheRowsBackground_ThatValueDecides()
    {
        const string Doubtful =
            "<Border Background=\"{TemplateBinding Background}\"><Border Background=\"{TemplateBinding BorderBrush}\">"
            + "<ContentPresenter Content=\"{TemplateBinding Content}\" /></Border></Border>";
        const string Painted =
            "<Panel><Border Background=\"Transparent\" /><Border Background=\"{TemplateBinding Background}\">" + Presenter + "</Border></Panel>";

        AssertNullWhereDrawn(Test(Rows(Doubtful, "{x:Null}")));
        AssertClean(Test(Rows(Doubtful)));
        AssertNullWhereDrawn(Test(Rows(Painted, "{Binding RowBrush}")));

        // A shape's Fill draws the row's Background as a Border's Background does.
        const string Filled = "<Panel><Rectangle Fill=\"{TemplateBinding Background}\" />" + Presenter + "</Panel>";
        AssertNullWhereDrawn(Test(Rows(Filled, "{x:Null}")));
        AssertClean(Test(Rows(Filled)));
    }

    /// <summary>A template whose only paint this rule cannot evaluate is not decided.</summary>
    [Fact]
    public void ATemplateWhoseOnlyPaintCannotBeEvaluated_IsSkipped() =>
        AssertSkipped(Test(Rows("<Border Background=\"{TemplateBinding BorderBrush}\"><ContentPresenter Content=\"{TemplateBinding Content}\" /></Border>")));

    /// <summary>A template element bound to a brush of the item's is not decided: the binding may answer null or a brush.</summary>
    [Fact]
    public void ATemplateElementBoundToData_IsSkipped() =>
        AssertSkipped(Test(Rows("<Border Background=\"{Binding RowBrush}\"><ContentPresenter Content=\"{TemplateBinding Content}\" /></Border>")));

    /// <summary>
    /// A template element that sets no background of its own, and that a style may pick and paint, is not
    /// decided: measured, a class style painting one took the click. By its type, a class, a conditional
    /// class or its name.
    /// </summary>
    [Fact]
    public void ATemplateElementAStyleMayPaint_IsSkipped()
    {
        foreach ((string border, string selector) in (ValueTuple<string, string>[])
                 [
                     ("<Border Classes=\"row bg\">", "Border.bg"),
                     ("<Border Classes.bg=\"{Binding IsHighlighted}\">", "Border.bg"),
                     ("<Border>", "Border"),
                     ("<Border Name=\"Bg\">", "Border#Bg"),
                     ("<Border x:Name=\"Bg\">", ":is(Control)#Bg"),
                 ])
        {
            AssertSkipped(Test(Rows(border + Presenter + "</Border>", styles: Style(selector, Background("Transparent")))));
        }

        // A setter may name the property with its owner, in parentheses.
        AssertSkipped(Test(Rows("<Border>" + Presenter + "</Border>", styles: Style("Border", "<Setter Property=\"(Border.Background)\" Value=\"Transparent\" />"))));
    }

    /// <summary>A style that cannot pick the element in its first state paints nothing there, so the rows are still reported.</summary>
    [Fact]
    public void ATemplateElementNoStylePicks_IsReported()
    {
        foreach ((string border, string selector) in (ValueTuple<string, string>[])
                 [
                     ("<Border Classes=\"row\">", "Border.bg"),
                     ("<Border>", "Border.card"),
                     ("<Border>", "Border:pointerover"),
                     ("<Border Name=\"Bg\">", "Border#Other"),
                     ("<Border>", "TextBlock"),
                     ("<Border>", "Button /template/ Border"),
                 ])
        {
            AssertNoBackground(Test(Rows(border + Presenter + "</Border>", styles: Style(selector, Background("Transparent")))));
        }

        // A nested style picks what its parent picks, and more narrowly.
        AssertNoBackground(Test(Rows("<Border>" + Presenter + "</Border>", styles: Style("Border.bg", Style("^", Background("Transparent"))))));
    }

    /// <summary>
    /// What a template's markup gives an element ranks below a style with a condition and above a style with
    /// none, by BindingPriority at 12.1.3, and below what the element's own code sets: so a value a style
    /// with a condition, a nested style of the element's theme or its code may replace is not decided, and
    /// one a plain style or a theme's own setter would replace stands.
    /// </summary>
    [Fact]
    public void WhatATemplateGivesAnElement_IsOutrankedOnlyByAConditionOrCode()
    {
        string taker = "<Border Classes=\"bg\" Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>";
        string cleared = "<Border Classes=\"bg\" Background=\"{x:Null}\">" + Presenter + "</Border>";
        static string BorderTheme(string setters) => "<ControlTheme x:Key=\"{x:Type Border}\" TargetType=\"Border\">" + setters + "</ControlTheme>";

        XamlSkip skip = AssertSkipped(Test(Rows(taker, "{x:Null}", styles: Style("Border.bg", Background("Red")))));
        Assert.Contains("outranking the template's own value", skip.Reason, StringComparison.Ordinal);
        AssertNullWhereDrawn(Test(Rows(taker, "{x:Null}", styles: Style("Border", Background("Red")))));
        AssertNullWhereDrawn(Test(Rows(taker, "{x:Null}", styles: Style(":is(Border)", Background("Red")))));
        AssertSkipped(Test(Rows(taker, "{x:Null}", styles: Style("Border.bg", Style("^", Background("Red"))))));
        AssertSkipped(Test(Rows(cleared, styles: Style("Border.bg", Background("Transparent")))));
        AssertNoBackground(Test(Rows(cleared, styles: Style("Border", Background("Transparent")))));

        AssertSkipped(Test(Rows(taker, "{x:Null}", BorderTheme(Style("^:not(.x)", Background("Red"))))));
        AssertNullWhereDrawn(Test(Rows(taker, "{x:Null}", BorderTheme(Background("Red")))));
        AssertNullWhereDrawn(Test(Rows(taker, "{x:Null}", BorderTheme(Style("^:pointerover", Background("Red"))))));

        XamlSkip code = AssertSkipped(Test(Rows("<CodePaintedBorder Background=\"{TemplateBinding Background}\">" + Presenter + "</CodePaintedBorder>", "{x:Null}")));
        Assert.Contains("its own code may set", code.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A background given to an element the framework does not draw it on, such as a run of text, is not
    /// known to be drawn, and is not decided; a TextBlock draws its own.
    /// </summary>
    [Fact]
    public void ABackgroundOnAnElementNotKnownToDrawIt_IsSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(Rows("<TextBlock><Run Background=\"{TemplateBinding Background}\" /></TextBlock>", "{x:Null}")));
        Assert.Contains("does not know a Run to draw", skip.Reason, StringComparison.Ordinal);
        AssertNullWhereDrawn(Test(Rows("<TextBlock Background=\"{TemplateBinding Background}\" />", "{x:Null}")));
    }

    /// <summary>A theme a template element names is read: one that may paint it, or that the scan does not hold, is not decided.</summary>
    [Fact]
    public void ATemplateElementsTheme_IsRead()
    {
        static string Card(string setters) =>
            "<ControlTheme x:Key=\"Card\" TargetType=\"Border\">" + setters + "</ControlTheme>";

        string named = "<Border Theme=\"{StaticResource Card}\">" + Presenter + "</Border>";
        AssertSkipped(Test(Rows(named)));
        AssertNoBackground(Test(Rows(named, resources: Card("<Setter Property=\"Padding\" Value=\"4\" />"))));
        AssertSkipped(Test(Rows(named, resources: Card(Background("Transparent")))));
        AssertSkipped(Test(Rows("<Border><Border.Theme><StaticResource ResourceKey=\"Card\" /></Border.Theme>" + Presenter + "</Border>",
            resources: Card(Background("Transparent")))));
    }

    /// <summary>A templated control in the template that the scan holds no theme for may be painted by its framework's, so the rows are not decided.</summary>
    [Fact]
    public void ATemplatedControlWithNoThemeInTheScan_IsSkipped()
    {
        XamlSkip skip = AssertSkipped(Test(Rows("<Button Content=\"{TemplateBinding Content}\" />")));
        Assert.Contains("a templated control whose own theme may paint it", skip.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(Rows("<Panel><Button />" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// A binding to an ancestor that is not a row, or to another of the row's properties, does not take the
    /// row's background; nor does one past the row by its level, which an outer row answers: measured,
    /// $parent[ListBoxItem;1] and AncestorLevel=2 drew an outer row's background, or none. Nor does one that
    /// an ancestor of its type in the template answers first, since an ancestor of a derived type counts, nor,
    /// in the visual tree, one with a templated control between, whose own template may hold one, or one to a
    /// type every element is, which a visual an element puts between it and its child may be, as a Viewbox's is.
    /// </summary>
    [Fact]
    public void ABindingToAnotherAncestor_IsNotTheRows()
    {
        AssertSkipped(Test(Rows("<Border Background=\"{Binding $parent[ListBox].Background}\">" + Presenter + "</Border>", "{x:Null}")));
        AssertSkipped(Test(Rows("<Border Background=\"{Binding $parent[ListBoxItem].BorderBrush}\">" + Presenter + "</Border>", "{x:Null}")));

        foreach (string outer in (string[])
                 [
                     "{Binding $parent[ListBoxItem;1].Background}",
                     "{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, AncestorLevel=2}}",
                     "{Binding $parent[ListBoxItem;x].Background}",
                 ])
        {
            XamlSkip skip = AssertSkipped(Test(Rows("<Border Background=\"" + outer + "\">" + Presenter + "</Border>", "{x:Null}")));
            Assert.Contains("a binding with no TargetNullValue", skip.Reason, StringComparison.Ordinal);
        }

        AssertSkipped(Test(Rows("<ContentControl><Border Background=\"{Binding $parent[ContentControl].Background}\" /></ContentControl>", "{x:Null}")));
        AssertSkipped(Test(Rows("<Unresolved><Border Background=\"{Binding $parent[ListBoxItem].Background}\" /></Unresolved>", "{x:Null}")));
        AssertSkipped(Test(Rows("<Border><Border Background=\"{Binding $parent[Control].Background}\" />" + Presenter + "</Border>", "{x:Null}")));
        AssertSkipped(Test(Rows("<ContentControl><Border Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem}}\" /></ContentControl>", "{x:Null}")));
        AssertSkipped(Test(Rows("<Border><Border Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=Control, AncestorLevel=2}}\" />"
                                + Presenter + "</Border>", "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows("<ContentControl><Border Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, Tree=Logical}}\" /></ContentControl>", "{x:Null}")));
    }

    /// <summary>
    /// A RelativeSource takes its AncestorType only when it finds an ancestor, which it does with no mode
    /// written: measured, a Border bound by Mode=Self with AncestorType=ListBoxItem bound itself, and drew
    /// nothing. One that binds the element itself or its data context does not take the row's background,
    /// whatever type it also names.
    /// </summary>
    [Fact]
    public void ARelativeSourceThatFindsNoAncestor_IsNotTheRows()
    {
        foreach (string source in (string[])
                 [
                     "{RelativeSource Mode=Self, AncestorType=ListBoxItem}",
                     "{RelativeSource Self, AncestorType=ListBoxItem}",
                     "{RelativeSource Mode=DataContext, AncestorType=ListBoxItem}",
                 ])
        {
            string template = "<Border Background=\"{Binding Background, RelativeSource=" + source + "}\">" + Presenter + "</Border>";
            XamlSkip skip = AssertSkipped(Test(Rows(template, "{x:Null}")));
            Assert.Contains("a binding with no TargetNullValue", skip.Reason, StringComparison.Ordinal);
            AssertSkipped(Test(Rows(template)));
        }

        string finding = "<Border Background=\"{Binding Background, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=ListBoxItem}}\">" + Presenter + "</Border>";
        AssertNullWhereDrawn(Test(Rows(finding, "{x:Null}")));
        AssertClean(Test(Rows(finding)));
    }

    /// <summary>
    /// What a ContentPresenter in the template holds as markup content is not where the template writes it:
    /// measured, a Border written inside one had no templated parent, so a TemplateBinding there took nothing,
    /// and inside one named PART_ContentPresenter its logical parent was the row itself, so $parent[Control;1]
    /// reached the list. Nor does what a TransitioningContentControl holds take a templated parent, since it
    /// hands its content to a presenter of its own, by its source at 12.1.3. So what either holds is not
    /// followed, and where nothing else decides, the rows are skipped. What any other ContentControl holds is
    /// its logical child, which takes the row as its templated parent.
    /// </summary>
    [Fact]
    public void WhatAPresenterHoldsAsMarkupContent_IsNotFollowed()
    {
        foreach (string held in (string[])
                 [
                     "<Panel><ContentPresenter><Border Background=\"{TemplateBinding Background}\" /></ContentPresenter>" + Presenter + "</Panel>",
                     "<Panel><ContentPresenter><Border Background=\"{Binding Background, RelativeSource={RelativeSource TemplatedParent}}\" /></ContentPresenter>" + Presenter + "</Panel>",
                     "<ContentPresenter Name=\"PART_ContentPresenter\"><Border Background=\"{Binding $parent[Control;1].Background}\" /></ContentPresenter>",
                     "<ContentPresenter Name=\"PART_ContentPresenter\"><ContentPresenter.Content><Border Background=\"{TemplateBinding Background}\" /></ContentPresenter.Content></ContentPresenter>",
                     "<Panel><ScrollContentPresenter><Grid><Border Background=\"{TemplateBinding Background}\" /></Grid></ScrollContentPresenter>" + Presenter + "</Panel>",
                 ])
        {
            XamlSkip skip = AssertSkipped(Test(Rows(held, "{x:Null}")));
            Assert.Contains("as the content of the", skip.Reason, StringComparison.Ordinal);
            AssertSkipped(Test(Rows(held)));
        }

        // A TransitioningContentControl whose own theme draws nothing, holding a Border in either spelling.
        string transitioning = "<ControlTheme x:Key=\"{x:Type TransitioningContentControl}\" TargetType=\"TransitioningContentControl\">"
                               + "<Setter Property=\"Template\"><ControlTemplate>" + Presenter + "</ControlTemplate></Setter></ControlTheme>";
        foreach (string held in (string[])
                 [
                     "<Panel><TransitioningContentControl><Border Background=\"{TemplateBinding Background}\" /></TransitioningContentControl>" + Presenter + "</Panel>",
                     "<Panel><TransitioningContentControl><TransitioningContentControl.Content><Border Background=\"{TemplateBinding Background}\" />"
                     + "</TransitioningContentControl.Content></TransitioningContentControl>" + Presenter + "</Panel>",
                 ])
        {
            XamlSkip skip = AssertSkipped(Test(Rows(held, "{x:Null}", transitioning)));
            Assert.Contains("as the content of the TransitioningContentControl", skip.Reason, StringComparison.Ordinal);
            AssertSkipped(Test(Rows(held, resources: transitioning)));
        }

        AssertNullWhereDrawn(Test(Rows("<Panel><ContentControl><Border Background=\"{TemplateBinding Background}\" /></ContentControl>" + Presenter + "</Panel>", "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{TemplateBinding Background}\"><ContentPresenter><TextBlock /></ContentPresenter></Border>", "{x:Null}")));
    }

    /// <summary>
    /// What the rows' template holds starts disabled with them, so where markup may disable the rows, a style
    /// that paints an element of their template while it is disabled may paint it from the start, and so may
    /// one in that element's own theme, which outranks what the template gives it. So may one that paints a
    /// part the template itself disables. Where nothing disables them, both are passed over.
    /// </summary>
    [Fact]
    public void WhatTheRowsTemplateHolds_MayStartDisabledWithThem()
    {
        string disabled = Themed + " IsEnabled=\"False\"";
        string border = "<Border>" + Presenter + "</Border>";
        string painting = Style("Border:disabled", Background("Transparent"));

        AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent"), template: border), painting)));
        AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent"), template: border), Style("Border:disabled", Style("^", Background("Transparent"))))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent"), template: border), painting)));

        string taker = "<Border Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>";
        string borderTheme = "<ControlTheme x:Key=\"{x:Type Border}\" TargetType=\"Border\">" + Style("^:disabled", Background("Red")) + "</ControlTheme>";
        XamlSkip skip = AssertSkipped(Test(View(List(disabled), Theme(Background("{x:Null}"), template: taker) + borderTheme)));
        Assert.Contains("outranking the template's own value", skip.Reason, StringComparison.Ordinal);
        AssertNullWhereDrawn(Test(View(List(Themed), Theme(Background("{x:Null}"), template: taker) + borderTheme)));

        string disabledPart = "<Border IsEnabled=\"False\">" + Presenter + "</Border>";
        AssertSkipped(Test(View(List(Themed), Theme(Background("Transparent"), template: disabledPart), Style("ListBoxItem /template/ Border:disabled", Background("Transparent")))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent"), template: border), Style("ListBoxItem /template/ Border:disabled", Background("Transparent")))));
    }

    /// <summary>
    /// A style declared in the template, in the Styles of an element or of what holds it, may paint that
    /// element: measured, a Border painted by a style in its own Border.Styles took the click. One declared
    /// on an element beside it does not reach it.
    /// </summary>
    [Fact]
    public void AStyleDeclaredInTheTemplate_MayPaintWhatItReaches()
    {
        const string Painting = "<Style Selector=\"Border\"><Setter Property=\"Background\" Value=\"Transparent\" /></Style>";

        AssertSkipped(Test(Rows("<Border><Border.Styles>" + Painting + "</Border.Styles>" + Presenter + "</Border>")));
        AssertSkipped(Test(Rows("<Panel><Panel.Styles>" + Painting + "</Panel.Styles><Border>" + Presenter + "</Border></Panel>")));
        AssertNoBackground(Test(Rows("<Panel><StackPanel><StackPanel.Styles>" + Painting + "</StackPanel.Styles></StackPanel><Border>" + Presenter + "</Border></Panel>")));
    }

    /// <summary>
    /// What a tooltip, a flyout or a menu holds is not what a template presents, and what a Popup holds is
    /// shown in a layer of its own, only while it is open: a background drawn inside one leaves the rows as
    /// they were.
    /// </summary>
    [Fact]
    public void WhatAPropertyElementOrAPopupHolds_IsNotPresented()
    {
        const string Tip = "<ToolTip.Tip><Border Background=\"Transparent\"><TextBlock /></Border></ToolTip.Tip>";

        AssertNullWhereDrawn(Test(Rows("<Border Background=\"{TemplateBinding Background}\">" + Tip + Presenter + "</Border>", "{Binding RowBrush}")));
        AssertNoBackground(Test(Rows("<ContentPresenter Content=\"{TemplateBinding Content}\">" + Tip + "</ContentPresenter>")));
        AssertNoBackground(Test(Rows("<Panel><Popup><Border Background=\"Transparent\" /></Popup>" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel><Popup><Popup.Child><Border Background=\"{TemplateBinding Background}\" /></Popup.Child></Popup>" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// A template's binding to the row's Background with a TargetNullValue that is not null paints whatever
    /// the row's is, in either spelling, and one through a converter, or with anything else this rule does
    /// not evaluate, is not decided: measured, rows whose Background was null took the click across their
    /// width through a TemplatedParent or $parent[ListBoxItem] binding with a TargetNullValue of
    /// Transparent, and through a TemplateBinding whose converter turned null into Transparent.
    /// </summary>
    [Fact]
    public void WhatATemplatesBindingDoesWithANull_Decides()
    {
        static string Root(string background) => "<Border Background=\"" + background + "\">" + Presenter + "</Border>";

        AssertClean(Test(Rows(Root("{Binding Background, RelativeSource={RelativeSource TemplatedParent}, TargetNullValue=Transparent}"), "{x:Null}")));
        AssertClean(Test(Rows(Root("{Binding $parent[ListBoxItem].Background, TargetNullValue={x:Static Brushes.Transparent}}"), "{x:Null}")));
        AssertClean(Test(Rows("<Border><Border.Background><Binding Path=\"Background\" "
            + "RelativeSource=\"{RelativeSource TemplatedParent}\" TargetNullValue=\"Transparent\" /></Border.Background>" + Presenter + "</Border>", "{x:Null}")));

        foreach (string changed in (string[])[
                     "{TemplateBinding Background, Converter={StaticResource NullToTransparent}}",
                     "{Binding Background, RelativeSource={RelativeSource TemplatedParent}, StringFormat=x}",
                     "{Binding Background, RelativeSource={RelativeSource TemplatedParent}, Mode=OneWayToSource}"])
        {
            XamlSkip skip = AssertSkipped(Test(Rows(Root(changed), "{x:Null}")));
            Assert.Contains("which this rule does not evaluate", skip.Reason, StringComparison.Ordinal);
        }

        AssertNullWhereDrawn(Test(Rows(Root(
            "{Binding Background, RelativeSource={RelativeSource TemplatedParent}, TargetNullValue={x:Null}, Mode=OneWay, FallbackValue=Red}"), "{x:Null}")));
    }

    /// <summary>
    /// What hides an element from the pointer decides nothing: an element that draws the row's Background
    /// is taken to draw it, and one that paints is taken to paint, whatever their IsVisible, IsHitTestVisible
    /// or IsEnabled, or what binds them.
    /// </summary>
    [Fact]
    public void WhatHidesWhatDraws_DecidesNothing()
    {
        foreach (string hidden in (string[])["IsHitTestVisible=\"False\"", "IsVisible=\"False\"", "IsEnabled=\"{Binding Enabled}\""])
        {
            string template = "<Panel><Border Background=\"{TemplateBinding Background}\" " + hidden + " />" + Presenter + "</Panel>";
            AssertClean(Test(Rows(template)));
            AssertNullWhereDrawn(Test(Rows(template, "{x:Null}")));
        }

        AssertClean(Test(Rows("<Panel><Border Background=\"Transparent\" IsHitTestVisible=\"False\" />" + Presenter + "</Panel>", "{x:Null}")));
        AssertClean(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Property=\"IsHitTestVisible\" Value=\"False\" />"))));
    }

    // ── Templated controls and views in the row template ──────────────────────

    /// <summary>A Chip theme whose template draws the Chip's own Background, with <paramref name="setters"/> before it.</summary>
    private static string ChipTheme(string setters = "", string basedOn = "", string template = "<Border Background=\"{TemplateBinding Background}\"><TextBlock /></Border>") =>
        "<ControlTheme x:Key=\"{x:Type Chip}\" TargetType=\"Chip\"" + (basedOn.Length > 0 ? " BasedOn=\"" + basedOn + "\"" : string.Empty) + ">"
        + setters
        + (template.Length > 0 ? "<Setter Property=\"Template\"><ControlTemplate>" + template + "</ControlTemplate></Setter>" : string.Empty)
        + "</ControlTheme>";

    /// <summary>
    /// A templated control in the row template is read by its own theme when the scan holds it, as a row's
    /// is: one whose template draws a background nothing sets paints nothing; one whose theme sets a brush
    /// paints, however little of the row it covers; one whose theme may paint it, or reaches a framework
    /// theme, and one the scan holds no theme for, are not decided.
    /// </summary>
    [Fact]
    public void ATemplatedControlsOwnTheme_IsRead()
    {
        const string Beside = "<Panel><Chip />" + Presenter + "</Panel>";

        AssertNoBackground(Test(Rows(Beside, resources: ChipTheme())));
        AssertNoBackground(Test(Rows(Beside, resources: ChipTheme(Style("^:pointerover", Background("Red"))))));
        AssertClean(Test(Rows(Beside, "{x:Null}", ChipTheme(Background("Transparent")))));

        // A theme the control names is read as surely as its implicit one.
        AssertClean(Test(Rows("<Panel><Chip Theme=\"{StaticResource Named}\" />" + Presenter + "</Panel>", "{x:Null}",
            ChipTheme(Background("Transparent")).Replace("x:Key=\"{x:Type Chip}\"", "x:Key=\"Named\"", StringComparison.Ordinal))));

        AssertSkipped(Test(Rows(Beside)));
        AssertSkipped(Test(Rows(Beside, resources: ChipTheme(Style("^.accent", Background("Red"))))));
        AssertSkipped(Test(Rows(Beside, resources: ChipTheme(basedOn: "{StaticResource {x:Type Chip}}"))));
        AssertSkipped(Test(Rows(Beside, resources: ChipTheme(basedOn: "{StaticResource {x:Type Chip}}", template: string.Empty))));
        AssertSkipped(Test(Rows(Beside, resources: ChipTheme(), styles: Style("Chip", "<Setter Property=\"Theme\" Value=\"{StaticResource Other}\" />"))));
        AssertSkipped(Test(Rows(Beside, resources: ChipTheme(),
            styles: Style("Chip /template/ Border", "<Setter Property=\"Background\" Value=\"Red\" />"))));

        // A theme whose template holds what it themes is read once, not forever, and the reason says what it leaves undecided once.
        XamlSkip again = AssertSkipped(Test(Rows(Beside, resources: ChipTheme(template: "<Border Background=\"{TemplateBinding Background}\"><Chip /></Border>"))));
        Assert.Contains("holds what it themes, so whether their template draws a background is not decided from markup.", again.Reason, StringComparison.Ordinal);
        Assert.Equal(again.Reason.IndexOf("not decided", StringComparison.Ordinal), again.Reason.LastIndexOf("not decided", StringComparison.Ordinal));
    }

    /// <summary>
    /// A templated control whose Background takes the row's draws it only where its own template draws its
    /// Background: then a null one is reported, and a brush is clean; a template that ignores it draws
    /// nothing of the row's, and one that paints a brush of its own paints. With no theme in the scan, a
    /// ContentControl takes the default template, which draws it.
    /// </summary>
    [Fact]
    public void ATemplatedControlTakingTheRowsBackground_DrawsItOnlyThroughItsOwnTemplate()
    {
        const string Taker = "<Panel><Chip Background=\"{TemplateBinding Background}\" />" + Presenter + "</Panel>";

        XamlFinding finding = AssertNullWhereDrawn(Test(Rows(Taker, "{x:Null}", ChipTheme())));
        Assert.Contains("the Chip at", finding.Message, StringComparison.Ordinal);
        Assert.Contains("whose own template draws its Background with the Border at", finding.Message, StringComparison.Ordinal);
        AssertClean(Test(Rows(Taker, resources: ChipTheme())));

        AssertNoBackground(Test(Rows(Taker, resources: ChipTheme(template: "<Border><TextBlock /></Border>"))));
        AssertClean(Test(Rows(Taker, "{x:Null}", ChipTheme(template: "<Border Background=\"Transparent\" />"))));
        AssertSkipped(Test(Rows(Taker, "{x:Null}")));

        const string Content = "<ContentControl Background=\"{TemplateBinding Background}\" Content=\"{TemplateBinding Content}\" />";
        AssertNullWhereDrawn(Test(Rows(Content, "{x:Null}")));
        AssertClean(Test(Rows(Content)));

        // A binding whose TargetNullValue answers for the null paints, through a control's template as through a Border.
        AssertClean(Test(Rows("<ContentControl Background=\"{Binding Background, RelativeSource={RelativeSource TemplatedParent}, TargetNullValue=Transparent}\" "
                              + "Content=\"{TemplateBinding Content}\" />", "{x:Null}")));
    }

    /// <summary>
    /// A UserControl or a ContentControl with no theme in the scan paints only its own background, which its
    /// default template draws: one that sets none paints nothing, one given a brush paints, and one given a
    /// binding may paint. What it holds is read where it is written.
    /// </summary>
    [Fact]
    public void AContentControlOrAUserControl_DrawsOnlyItsOwnBackground()
    {
        AssertNoBackground(Test(Rows("<UserControl><TextBlock Text=\"{TemplateBinding Tag}\" /></UserControl>")));
        AssertNoBackground(Test(Rows("<ContentControl Content=\"row\" />")));
        AssertClean(Test(Rows("<UserControl Background=\"Transparent\"><TextBlock /></UserControl>", "{x:Null}")));
        AssertClean(Test(Rows("<ContentControl><Border Background=\"Transparent\" /></ContentControl>", "{x:Null}")));

        XamlSkip skip = AssertSkipped(Test(Rows("<UserControl Background=\"{Binding RowBrush}\" />")));
        Assert.Contains("a UserControl at", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A view in the row template is read from its own markup, found by its x:Class: the Background its root
    /// gives it, and what that markup holds. One whose markup the scan does not hold once, or that holds
    /// itself, is not decided; one whose Background takes the row's draws it through its default template.
    /// </summary>
    [Fact]
    public void AViewInTheRowTemplate_IsReadFromItsOwnMarkup()
    {
        const string Class = "x:Class=\"XamlQuality.Tests.Rules.ItemFakes.RowView\"";
        string rows = Rows("<Panel><RowView />" + Presenter + "</Panel>");

        XamlSkip missing = AssertSkipped(Test(rows));
        Assert.Contains("x:Class", missing.Reason, StringComparison.Ordinal);

        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><Grid><TextBlock Text=\"{Binding Label}\" /></Grid></UserControl>");
        AssertNoBackground(Test(rows));
        AssertNullWhereDrawn(Test(Rows("<RowView Background=\"{TemplateBinding Background}\" />", "{x:Null}")));

        Write("RowView.axaml", "<UserControl " + Header + " " + Class + " Background=\"Transparent\"><TextBlock /></UserControl>");
        AssertClean(Test(rows));

        // A view whose markup gives it a theme or a template of its own draws what that draws, which is not followed.
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + " Theme=\"{StaticResource Elsewhere}\"><TextBlock /></UserControl>");
        XamlSkip themed = AssertSkipped(Test(rows));
        Assert.Contains("gives it a Template or a Theme", themed.Reason, StringComparison.Ordinal);

        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><Grid><Border Background=\"Transparent\" /></Grid></UserControl>");
        AssertClean(Test(rows));

        // A view whose markup holds it again is read once, not forever.
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><Grid><RowView /></Grid></UserControl>");
        AssertSkipped(Test(rows));

        // Two files that declare one class leave which is the view's own undecided.
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><Grid><TextBlock Text=\"{Binding Label}\" /></Grid></UserControl>");
        Write("RowViewCopy.axaml", "<UserControl " + Header + " " + Class + "><Grid><TextBlock Text=\"{Binding Label}\" /></Grid></UserControl>");
        AssertSkipped(Test(rows));
    }

    /// <summary>
    /// In a template, a view's own Background outranks the one the template gives it, which the XAML compiler
    /// sets at BindingPriority.Template: measured, a view whose root was Transparent drew it, given the row's
    /// null background or {x:Null} by the template. Where the view's root sets none, the template's stands, and
    /// so does a binding, which the compiler sets as a local value even there, after the view's own.
    /// </summary>
    [Fact]
    public void AViewsOwnBackground_OutranksTheTemplates()
    {
        const string Class = "x:Class=\"XamlQuality.Tests.Rules.ItemFakes.RowView\"";
        const string Taking = "<Panel><RowView Background=\"{TemplateBinding Background}\" />" + Presenter + "</Panel>";
        const string Clearing = "<Panel><RowView Background=\"{x:Null}\" />" + Presenter + "</Panel>";
        const string Binding = "<Panel><RowView Background=\"{Binding $parent[ListBoxItem].Background}\" />" + Presenter + "</Panel>";
        const string ElementBinding = "<Panel><RowView><RowView.Background><Binding Path=\"$parent[ListBoxItem].Background\" /></RowView.Background></RowView>"
                                      + Presenter + "</Panel>";

        Write("RowView.axaml", "<UserControl " + Header + " " + Class + " Background=\"Transparent\"><TextBlock /></UserControl>");
        AssertClean(Test(Rows(Taking, "{x:Null}")));
        AssertClean(Test(Rows(Clearing)));
        AssertNullWhereDrawn(Test(Rows(Binding, "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows(ElementBinding, "{x:Null}")));
        AssertClean(Test(Rows(Binding)));

        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><TextBlock /></UserControl>");
        AssertNullWhereDrawn(Test(Rows(Taking, "{x:Null}")));
        AssertNoBackground(Test(Rows(Clearing)));

        // Outside a template, what a view's markup gives a view it holds is set after that view's own, and wins.
        const string Holding = "<Panel><RowView />" + Presenter + "</Panel>";
        Write("InnerView.axaml", "<UserControl " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.InnerView\" Background=\"Transparent\"><TextBlock /></UserControl>");
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><Grid><InnerView Background=\"{x:Null}\" /></Grid></UserControl>");
        AssertNoBackground(Test(Rows(Holding)));
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + "><Grid><InnerView /></Grid></UserControl>");
        AssertClean(Test(Rows(Holding)));
    }

    /// <summary>
    /// Any type with markup of its own, found by its x:Class, is read from it as a view is, whatever it
    /// derives from: measured, a Border subclass and a Panel subclass whose own markup was Transparent drew it
    /// in a row's template, and so did a subclass of each with no markup of its own, onto which its base's
    /// constructor loads the base's. In a template, what its root gives it outranks what the template gives it,
    /// but for a binding; elsewhere, what markup gives it replaces it. What that markup holds paints as well.
    /// </summary>
    [Fact]
    public void ATypesOwnMarkup_IsReadWhateverItDerivesFrom()
    {
        const string Border = "x:Class=\"XamlQuality.Tests.Rules.ItemFakes.XClassBorder\"";
        const string Beside = "<Panel><XClassBorder />" + Presenter + "</Panel>";
        const string Taking = "<XClassBorder Background=\"{TemplateBinding Background}\" />";

        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + " Background=\"Transparent\" />");
        Write("XClassPanel.axaml", "<Panel " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.XClassPanel\" Background=\"Transparent\" />");
        AssertClean(Test(Rows(Beside)));
        AssertClean(Test(Rows("<Panel><XClassPanel />" + Presenter + "</Panel>")));
        AssertClean(Test(Rows(Taking, "{x:Null}")));
        AssertNullWhereDrawn(Test(Rows("<XClassBorder Background=\"{Binding $parent[ListBoxItem].Background}\" />", "{x:Null}")));

        // A subclass with no markup of its own carries its base's, which the base's constructor loads onto it.
        const string Derived = "<Panel><DerivedXClassBorder />" + Presenter + "</Panel>";
        AssertClean(Test(Rows(Derived)));
        AssertClean(Test(Rows("<Panel><DerivedXClassPanel />" + Presenter + "</Panel>")));
        AssertClean(Test(Rows("<DerivedXClassBorder Background=\"{TemplateBinding Background}\" />", "{x:Null}")));

        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + " Background=\"{x:Null}\" />");
        AssertNoBackground(Test(Rows(Taking)));

        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + "><Border Background=\"Transparent\" /></Border>");
        AssertClean(Test(Rows(Beside)));

        // What its own markup holds that this rule cannot read leaves it undecided.
        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + "><Unknowable /></Border>");
        AssertSkipped(Test(Rows(Beside)));

        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + " />");
        AssertNoBackground(Test(Rows(Beside)));
        AssertNullWhereDrawn(Test(Rows(Taking, "{x:Null}")));

        // What its own markup gives it is a local value, which a style with a condition does not outrank.
        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + " Background=\"Transparent\" />");
        AssertClean(Test(Rows("<Panel><XClassBorder Classes=\"x\" />" + Presenter + "</Panel>", styles: Style("XClassBorder.x", Background("{x:Null}")))));

        // A templated control that is not a view is read from its own markup too, where its theme's template draws its Background.
        const string Chip = "x:Class=\"XamlQuality.Tests.Rules.ItemFakes.XClassChip\"";
        string chipTheme = "<ControlTheme x:Key=\"{x:Type XClassChip}\" TargetType=\"XClassChip\"><Setter Property=\"Template\"><ControlTemplate>"
                           + "<Border Background=\"{TemplateBinding Background}\" /></ControlTemplate></Setter></ControlTheme>";
        Write("XClassChip.axaml", "<TemplatedControl " + Header + " " + Chip + " Background=\"Transparent\" />");
        AssertClean(Test(Rows("<Panel><XClassChip />" + Presenter + "</Panel>", resources: chipTheme)));
        Write("XClassChip.axaml", "<TemplatedControl " + Header + " " + Chip + " />");
        AssertNoBackground(Test(Rows("<Panel><XClassChip />" + Presenter + "</Panel>", resources: chipTheme)));

        // Outside a template, what markup gives it replaces its own.
        Write("XClassBorder.axaml", "<Border " + Header + " " + Border + " Background=\"Transparent\" />");
        Write("RowView.axaml", "<UserControl " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.RowView\"><Grid><XClassBorder Background=\"{x:Null}\" /></Grid></UserControl>");
        AssertNoBackground(Test(Rows("<Panel><RowView />" + Presenter + "</Panel>")));

        // Markup of its own beside its base's, each loaded in turn, is not combined, and a base's says nothing
        // of a subclass's; a class two files declare leaves which is its own undecided, as a view's does.
        Write("DerivedXClassBorder.axaml", "<Border " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.DerivedXClassBorder\" Background=\"Transparent\" />");
        AssertSkipped(Test(Rows(Derived)));
        AssertClean(Test(Rows(Beside)));
        Write("XClassBorderCopy.axaml", "<Border " + Header + " " + Border + " Background=\"Transparent\" />");
        AssertSkipped(Test(Rows(Beside)));
        Write("XClassChipCopy.axaml", "<TemplatedControl " + Header + " " + Chip + " Background=\"Transparent\" />");
        XamlSkip chipTwice = AssertSkipped(Test(Rows("<Panel><XClassChip />" + Presenter + "</Panel>", resources: chipTheme)));
        Assert.Contains("or a base's, found by its x:Class, is not in the scan once", chipTwice.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A templated control in the rows' template starts disabled with them, so where markup may disable the
    /// rows, a nested style of its own theme, or a style that reaches into its template, that paints it while
    /// it is disabled may paint it from the start. Where nothing disables them, both are passed over.
    /// </summary>
    [Fact]
    public void ATemplatedControlInRowsThatMayStartDisabled_MayBePainted()
    {
        const string Beside = "<Panel><Chip />" + Presenter + "</Panel>";
        string disabled = Themed + " IsEnabled=\"False\"";
        string nested = ChipTheme(Style("^:disabled", Background("Red")));
        string reaching = Style("Chip:disabled /template/ Border", Background("Red"));

        AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent"), template: Beside) + nested)));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent"), template: Beside) + nested)));
        AssertSkipped(Test(View(List(disabled), Theme(Background("Transparent"), template: Beside) + ChipTheme(), reaching)));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent"), template: Beside) + ChipTheme(), reaching)));
    }

    /// <summary>
    /// An element's own properties are read after a base of its type as after the type itself, as the XAML compiler
    /// takes them: the Background a templated control's template draws, the Theme it names, a Template of its own,
    /// and what a view's root gives the view, whose type there is its x:Class's, by XamlCompilerTaskExecutor at 12.1.3.
    /// </summary>
    [Fact]
    public void AnElementsPropertiesAfterABaseOfItsType_Count()
    {
        AssertClean(Test(Rows("<Panel><Chip TemplatedControl.Background=\"Transparent\" />" + Presenter + "</Panel>", resources: ChipTheme())));

        string painted = "<ControlTheme x:Key=\"Painted\" TargetType=\"Chip\"><Setter Property=\"Template\"><ControlTemplate>"
                         + "<Border Background=\"Transparent\" /></ControlTemplate></Setter></ControlTheme>";
        AssertClean(Test(Rows("<Panel><Chip TemplatedControl.Theme=\"{StaticResource Painted}\" />" + Presenter + "</Panel>", resources: painted)));
        AssertSkipped(Test(Rows("<Panel><Chip TemplatedControl.Template=\"{StaticResource Other}\" />" + Presenter + "</Panel>",
            resources: ChipTheme(template: "<Border />"))));
        AssertSkipped(Test(Rows("<Panel><Border Control.Theme=\"{StaticResource Bordered}\" />" + Presenter + "</Panel>",
            resources: "<ControlTheme x:Key=\"Bordered\" TargetType=\"Border\"><Setter Property=\"Background\" Value=\"Transparent\" /></ControlTheme>")));

        const string Class = "x:Class=\"XamlQuality.Tests.Rules.ItemFakes.RowView\"";
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + " TemplatedControl.Background=\"Transparent\"><TextBlock /></UserControl>");
        AssertClean(Test(Rows("<Panel><RowView />" + Presenter + "</Panel>")));
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + " TemplatedControl.Template=\"{StaticResource Other}\"><TextBlock /></UserControl>");
        AssertSkipped(Test(Rows("<Panel><RowView />" + Presenter + "</Panel>")));
        Write("RowView.axaml", "<UserControl " + Header + " " + Class + " RowView.Background=\"Transparent\"><TextBlock /></UserControl>");
        AssertClean(Test(Rows("<Panel><RowView />" + Presenter + "</Panel>")));

        Write("DerivedXClassBorder.axaml", "<Border " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.DerivedXClassBorder\" XClassBorder.Background=\"Transparent\" />");
        AssertClean(Test(Rows("<Panel><DerivedXClassBorder />" + Presenter + "</Panel>")));
    }

    /// <summary>A Theme of {x:Null} leaves the element its implicit theme, by StyledElement.GetEffectiveTheme at 12.1.3.</summary>
    [Fact]
    public void ANullTheme_LeavesTheImplicitOne() =>
        AssertClean(Test(Rows("<Panel><Chip Theme=\"{x:Null}\" />" + Presenter + "</Panel>", "{x:Null}", ChipTheme(Background("Transparent")))));

    /// <summary>
    /// A templated element's own Template outranks its theme's and draws whatever it draws, which this rule
    /// does not follow: measured, a ContentControl, a UserControl and a templated control given one that
    /// drew a Transparent Border each took the click. One given {x:Null} draws nothing, whatever its
    /// Background: measured, it missed.
    /// </summary>
    [Fact]
    public void ALocalTemplate_IsNotFollowed()
    {
        string template = "<ControlTemplate><Border Background=\"Transparent\"><ContentPresenter Content=\"{TemplateBinding Content}\" /></Border></ControlTemplate>";

        XamlSkip skip = AssertSkipped(Test(Rows("<ContentControl Content=\"{TemplateBinding Content}\"><ContentControl.Template>" + template
                                                + "</ContentControl.Template></ContentControl>", "{x:Null}")));
        Assert.Contains("a Template of its own", skip.Reason, StringComparison.Ordinal);
        AssertSkipped(Test(Rows("<UserControl><UserControl.Template>" + template + "</UserControl.Template></UserControl>")));
        AssertSkipped(Test(Rows("<ContentControl Template=\"{StaticResource Painted}\" />",
            resources: template.Replace("<ControlTemplate>", "<ControlTemplate x:Key=\"Painted\">", StringComparison.Ordinal))));
        AssertSkipped(Test(Rows("<Panel><Chip><Chip.Template>" + template + "</Chip.Template></Chip>" + Presenter + "</Panel>")));

        AssertNoBackground(Test(Rows("<ContentControl Template=\"{x:Null}\" Background=\"Transparent\"><TextBlock /></ContentControl>")));

        // An element whose type does not resolve is taken as templated when it is given a template.
        AssertSkipped(Test(Rows("<Panel><Unresolved Template=\"{StaticResource Painted}\" />" + Presenter + "</Panel>",
            resources: template.Replace("<ControlTemplate>", "<ControlTemplate x:Key=\"Painted\">", StringComparison.Ordinal))));
        AssertNoBackground(Test(Rows("<Panel><Unresolved Template=\"{x:Null}\" />" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// An items control in the row template is drawn by its own theme, and what it presents through its
    /// item template, as what any presenter presents, decides nothing.
    /// </summary>
    [Fact]
    public void AnItemsControlInTheRowTemplate_IsReadByItsOwnTheme()
    {
        string theme = "<ControlTheme x:Key=\"{x:Type ItemsControl}\" TargetType=\"ItemsControl\"><Setter Property=\"Template\"><ControlTemplate>"
                       + "<ItemsPresenter /></ControlTemplate></Setter></ControlTheme>";
        string items = "<Panel><ItemsControl ItemsSource=\"{Binding Children}\"><ItemsControl.ItemTemplate><DataTemplate><Border Background=\"Transparent\" />"
                       + "</DataTemplate></ItemsControl.ItemTemplate></ItemsControl>" + Presenter + "</Panel>";

        AssertNoBackground(Test(Rows(items, resources: theme)));
        AssertSkipped(Test(Rows(items)));
    }

    // ── What types and code paint ──────────────────────────────────────────────

    /// <summary>
    /// A background no markup sets is the default the type registers, which its static constructor can
    /// make a brush: measured, a templated control and a view that did took the click across the row. So
    /// an element of such a type paints, rows of such a type are clean, and the same markup with a type
    /// that registers no brush is reported.
    /// </summary>
    [Fact]
    public void ADefaultBackgroundTheTypeRegisters_Paints()
    {
        static string ThemeFor(string type) =>
            "<ControlTheme x:Key=\"{x:Type " + type + "}\" TargetType=\"" + type + "\"><Setter Property=\"Template\"><ControlTemplate>"
            + "<Border Background=\"{TemplateBinding Background}\"><TextBlock /></Border></ControlTemplate></Setter></ControlTheme>";

        AssertClean(Test(Rows("<Panel><PaintedChip />" + Presenter + "</Panel>", "{x:Null}", ThemeFor("PaintedChip"))));
        AssertNoBackground(Test(Rows("<Panel><Chip />" + Presenter + "</Panel>", resources: ThemeFor("Chip"))));

        AssertClean(Test(Rows("<Panel><PaintedPanel />" + Presenter + "</Panel>", "{x:Null}")));
        AssertNoBackground(Test(Rows("<Panel><StackPanel />" + Presenter + "</Panel>")));

        Write("PaintedView.axaml", "<UserControl " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes.PaintedView\"><TextBlock /></UserControl>");
        AssertClean(Test(Rows("<Panel><PaintedView />" + Presenter + "</Panel>", "{x:Null}")));

        AssertClean(Test(View(List(Themed, host: "PaintedRowList"), Theme())));
    }

    /// <summary>A default that cannot be read, because the type's static constructor fails, decides nothing.</summary>
    [Fact]
    public void ADefaultBackgroundThatCannotBeRead_IsSkipped()
    {
        AssertSkipped(Test(Rows("<Panel><UnreadablePanel />" + Presenter + "</Panel>")));
        AssertSkipped(Test(Rows("<Panel><UnreadableRow />" + Presenter + "</Panel>",
            resources: "<ControlTheme x:Key=\"{x:Type UnreadableRow}\" TargetType=\"UnreadableRow\"><Setter Property=\"Template\"><ControlTemplate>"
                       + "<Border Background=\"{TemplateBinding Background}\"><TextBlock /></Border></ControlTemplate></Setter></ControlTheme>")));

        XamlSkip skip = AssertSkipped(Test(View(List(Themed, host: "UnreadableRowList"), Theme())));
        Assert.Contains("could not be read", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A background the type's own code sets is a local value, which outranks every style and theme and
    /// which markup does not show: measured, a view whose code-behind set Transparent took the click. So an
    /// element of such a type, rows of such a type, and the rows of a list whose own code sets one are not
    /// decided.
    /// </summary>
    [Fact]
    public void ABackgroundSetInCode_IsSkipped()
    {
        // In a constructor, a method, a static method, a closure over a local, or after an await: measured for the last two.
        foreach (string painted in (string[])["CodePaintedView", "HandlerPaintedView", "StaticPaintedView", "LambdaPaintedView", "AsyncPaintedView"])
        {
            Write(painted + ".axaml", "<UserControl " + Header + " x:Class=\"XamlQuality.Tests.Rules.ItemFakes." + painted + "\"><TextBlock /></UserControl>");
            XamlSkip code = AssertSkipped(Test(Rows("<Panel><" + painted + " />" + Presenter + "</Panel>")));
            Assert.Contains("whose own code sets its Background", code.Reason, StringComparison.Ordinal);
        }

        XamlSkip rows = AssertSkipped(Test(View(List(host: "CodePaintedRowList"))));
        Assert.Contains("The code of CodePaintedRow sets Background", rows.Reason, StringComparison.Ordinal);

        // Measured: rows a list's own code made Transparent as it prepared them took the click. A framework's
        // own list does not, though its code may set a fill elsewhere, as Avalonia's ComboBox does.
        XamlSkip list = AssertSkipped(Test(View(List(Themed, host: "CodePaintingList"), Theme(Background("{x:Null}")))));
        Assert.Contains("The code of CodePaintingList sets a Background", list.Reason, StringComparison.Ordinal);
        AssertNullWhereDrawn(TestWith(EmittedViews.Framework, View(List(Themed, host: "FrameworkList"), Theme(Background("{x:Null}")))));
    }

    /// <summary>
    /// Code is read wherever the type's chain takes it, the assemblies the scan was not given included:
    /// measured, a view whose base in a referenced library set its background took the click. Compiled
    /// markup is not code, whether a method on the view or a closure XamlX nests in it for a template: it
    /// sets what the markup says, which is read as markup, so a compiled view whose markup paints nothing
    /// paints nothing; and markup compiled into a type, or a base, whose file the scan does not hold leaves it
    /// undecided.
    /// </summary>
    [Fact]
    public void CodeOutsideTheScan_IsRead_AndCompiledMarkup_IsReadAsMarkup()
    {
        Write("DerivedCodeView.axaml", "<UserControl " + Header + " x:Class=\"Fixtures.DerivedCodeView\"><TextBlock /></UserControl>");
        Write("CompiledView.axaml", "<UserControl " + Header + " x:Class=\"Fixtures.CompiledView\"><Grid><TextBlock /></Grid></UserControl>");

        XamlSkip derived = AssertSkipped(TestWith(EmittedViews.Views, Rows("<Panel><DerivedCodeView />" + Presenter + "</Panel>")));
        Assert.Contains("whose own code sets its Background", derived.Reason, StringComparison.Ordinal);

        AssertNoBackground(TestWith(EmittedViews.Views, Rows("<Panel><CompiledView />" + Presenter + "</Panel>")));

        // Markup compiled into a type, or a base, whose file the scan does not hold is not read, whatever the type.
        XamlSkip elsewhere = AssertSkipped(TestWith(EmittedViews.Views, Rows("<Panel><CompiledBorder />" + Presenter + "</Panel>")));
        Assert.Contains("is compiled into its assembly and is not in the scan", elsewhere.Reason, StringComparison.Ordinal);
        AssertSkipped(TestWith(EmittedViews.Views, Rows("<Panel><DerivedCompiledBorder />" + Presenter + "</Panel>")));
        Write("CompiledBorder.axaml", "<Border " + Header + " x:Class=\"Fixtures.CompiledBorder\" Background=\"Transparent\" />");
        AssertClean(TestWith(EmittedViews.Views, Rows("<Panel><CompiledBorder />" + Presenter + "</Panel>")));
        AssertClean(TestWith(EmittedViews.Views, Rows("<Panel><DerivedCompiledBorder />" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// A control that draws itself in a Render of its own may paint its bounds: measured, one that filled
    /// them with Transparent took the click across the row. The framework's own draw what sets their
    /// background or fill, which is read, so one in an assembly named as the framework's is not taken to.
    /// </summary>
    [Fact]
    public void AControlThatDrawsItself_MayPaint()
    {
        XamlSkip skip = AssertSkipped(Test(Rows("<Panel><FillRender />" + Presenter + "</Panel>")));
        Assert.Contains("draws itself", skip.Reason, StringComparison.Ordinal);
        AssertNoBackground(TestWith(EmittedViews.Framework, Rows("<Panel><FrameworkDrawn />" + Presenter + "</Panel>")));
        AssertNoBackground(Test(Rows("<Panel><Border />" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// A fill no markup sets is the default the shape's type registers, or one its own code sets: measured,
    /// a rectangle whose static constructor made its Fill Transparent, and one whose constructor set it,
    /// each took the click across the row; a plain rectangle missed it.
    /// </summary>
    [Fact]
    public void AFillATypeOrItsCodeGives_Paints()
    {
        AssertClean(Test(Rows("<Panel><FilledRect />" + Presenter + "</Panel>", "{x:Null}")));
        XamlSkip code = AssertSkipped(Test(Rows("<Panel><CodeFilledRect />" + Presenter + "</Panel>")));
        Assert.Contains("whose own code sets its Background or its Fill", code.Reason, StringComparison.Ordinal);
        AssertNoBackground(Test(Rows("<Panel><Rectangle />" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// Any element takes the implicit theme its style key names, one with no template as much as a control:
    /// measured, an empty Border an implicit theme made Transparent took the click. So such an element is not
    /// decided, and an implicit theme that paints nothing changes nothing.
    /// </summary>
    [Fact]
    public void AnImplicitThemeReachesAnElementWithNoTemplate()
    {
        const string Holding = "<Panel><Border />" + Presenter + "</Panel>";
        static string BorderTheme(string setters) => "<ControlTheme x:Key=\"{x:Type Border}\" TargetType=\"Border\">" + setters + "</ControlTheme>";

        XamlSkip skip = AssertSkipped(Test(Rows(Holding, resources: BorderTheme(Background("Transparent")))));
        Assert.Contains("its implicit theme", skip.Reason, StringComparison.Ordinal);
        AssertNoBackground(Test(Rows(Holding, resources: BorderTheme("<Setter Property=\"Margin\" Value=\"2\" />"))));
        AssertNoBackground(Test(Rows(Holding)));

        // A Theme of {x:Null} leaves the element its implicit one, by StyledElement.GetEffectiveTheme at 12.1.3.
        const string Cleared = "<Panel><Border Theme=\"{x:Null}\" />" + Presenter + "</Panel>";
        AssertSkipped(Test(Rows(Cleared, resources: BorderTheme(Background("Transparent")))));
        AssertNoBackground(Test(Rows(Cleared, resources: BorderTheme("<Setter Property=\"Margin\" Value=\"2\" />"))));
    }

    /// <summary>
    /// An element of the template whose type neither the scanned assemblies nor those they reference
    /// define may draw anything, so the rows are not decided where nothing draws their Background.
    /// </summary>
    [Fact]
    public void AnElementWhoseTypeDoesNotResolve_MayPaint()
    {
        XamlSkip skip = AssertSkipped(Test(Rows("<Panel><Unresolved />" + Presenter + "</Panel>")));
        Assert.Contains("define once, so what it draws is not read", skip.Reason, StringComparison.Ordinal);
        AssertNullWhereDrawn(Test(Rows("<Panel><Unresolved /><Border Background=\"{TemplateBinding Background}\" />" + Presenter + "</Panel>", "{x:Null}")));

        // A Template of {x:Null} presents nothing, written after its own name as alone.
        AssertNoBackground(Test(Rows("<Panel><Unresolved Unresolved.Template=\"{x:Null}\" />" + Presenter + "</Panel>")));
    }

    /// <summary>
    /// A type selector matches an element's style key, as Avalonia's does: measured, a Border style painted a
    /// control whose StyleKeyOverride borrows Border's, and a selector naming the control's own type did not.
    /// </summary>
    [Fact]
    public void ATypeSelector_MatchesTheStyleKey()
    {
        static string Holding(string selector) =>
            "<Panel><Panel.Styles>" + Style(selector, Background("Transparent")) + "</Panel.Styles><KeyedBorder />" + Presenter + "</Panel>";

        AssertSkipped(Test(Rows(Holding("Border"))));
        AssertNoBackground(Test(Rows(Holding("KeyedBorder"))));
    }

    // ── What the rows present decides nothing ──────────────────────────────────

    /// <summary>
    /// An item template that paints may take the click over each row's content, and the rule does not ask:
    /// rows whose Background can be null where their template draws it, and rows whose template draws no
    /// background, are reported whatever their item template, the list's own, its theme's, one chosen by the
    /// item's type, or one bound to the item's brush.
    /// </summary>
    [Fact]
    public void AnItemTemplateThatPaints_DecidesNothing()
    {
        const string Painted = "<DataTemplate><Border Background=\"Transparent\"><TextBlock Text=\"{Binding Label}\" /></Border></DataTemplate>";
        string bare = Theme(Background("Transparent"), template: BareTemplate);

        AssertNoBackground(Test(View("<ListBox ItemsSource=\"{Binding Rows}\" " + Themed + "><ListBox.ItemTemplate>" + Painted + "</ListBox.ItemTemplate></ListBox>", bare)));
        AssertNullWhereDrawn(Test(View("<ListBox ItemsSource=\"{Binding Rows}\"><ListBox.ItemTemplate>" + Painted + "</ListBox.ItemTemplate></ListBox>",
            styles: Style("ListBoxItem", Background("{x:Null}")))));
        AssertNoBackground(Test(View(List("Theme=\"{StaticResource ListTheme}\""),
            bare
            + "<ControlTheme x:Key=\"ListTheme\" TargetType=\"ListBox\" BasedOn=\"{StaticResource {x:Type ListBox}}\">"
            + "<Setter Property=\"ItemContainerTheme\" Value=\"{StaticResource RowTheme}\" />"
            + "<Setter Property=\"ItemTemplate\">" + Painted + "</Setter></ControlTheme>")));
        AssertNoBackground(Test(View(ItemTemplated("<Border Background=\"{Binding RowBrush}\" />"), bare)));

        Write("Templates.axaml", "<UserControl " + Header + "><UserControl.DataTemplates>"
                                 + Painted.Replace("<DataTemplate>", "<DataTemplate DataType=\"x:String\">", StringComparison.Ordinal)
                                 + "</UserControl.DataTemplates></UserControl>");
        AssertNoBackground(Test(View(List(Themed), bare)));
    }

    /// <summary>
    /// A style or a theme that gives the rows, or the list, something to present, content, a content
    /// template or an item template, changes what the rows present and nothing the rule asks.
    /// </summary>
    [Fact]
    public void WhatAStyleOrAThemeGivesTheRowsToPresent_DecidesNothing()
    {
        const string Painted = "<DataTemplate><Border Background=\"Transparent\" /></DataTemplate>";
        string blank = Style("ListBoxItem", Background("{x:Null}"));

        AssertNullWhereDrawn(Test(View(List(), styles: blank + Style("ListBoxItem", "<Setter Property=\"ContentTemplate\">" + Painted + "</Setter>"))));
        AssertNullWhereDrawn(Test(View(List(), styles: blank + Style("ListBox", "<Setter Property=\"ItemTemplate\">" + Painted + "</Setter>"))));
        AssertNullWhereDrawn(Test(View(List(), styles: blank + Style(":is(ListBoxItem)", "<Setter Property=\"Content\"><Border Background=\"Transparent\" /></Setter>"))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Property=\"ContentTemplate\">" + Painted + "</Setter>", template: BareTemplate))));
        AssertNoBackground(Test(View(List(Themed), Theme(Background("Transparent") + "<Setter Property=\"Content\" Value=\"row\" />", template: BareTemplate))));
    }

    /// <summary>
    /// A tab presents the item as its header, through the item template, which decides nothing either: a tab
    /// template that holds only what presents the header draws no background, whatever the item template paints.
    /// </summary>
    [Fact]
    public void ATabsHeader_DecidesNothing()
    {
        static string TabTheme(string background, string template) =>
            "<ControlTheme x:Key=\"TabTheme\" TargetType=\"TabItem\">" + Background(background)
            + "<Setter Property=\"Template\"><ControlTemplate>" + template + "</ControlTemplate></Setter></ControlTheme>";
        const string Header = "<ContentPresenter Content=\"{TemplateBinding Header}\" ContentTemplate=\"{TemplateBinding HeaderTemplate}\" />";
        const string Tabs =
            "<TabControl ItemsSource=\"{Binding Pages}\" ItemContainerTheme=\"{StaticResource TabTheme}\">"
            + "<TabControl.ItemTemplate><DataTemplate><Border Background=\"Transparent\" /></DataTemplate></TabControl.ItemTemplate></TabControl>";

        AssertNoBackground(Test(View(Tabs, TabTheme("Transparent", Header))));
        AssertNoBackground(Test(View(Tabs, TabTheme("Transparent", "<ContentPresenter Content=\"{TemplateBinding Content}\" />"))));
        AssertNullWhereDrawn(Test(View(Tabs, TabTheme("{x:Null}", "<Border Background=\"{TemplateBinding Background}\">" + Header + "</Border>"))));
    }

    /// <summary>A view locator or a template that chooses in code chooses what the rows present, which decides nothing.</summary>
    [Fact]
    public void WhatChoosesInCodeWhatTheRowsPresent_DecidesNothing()
    {
        Write("App.axaml", "<Application " + Header + " xmlns:local=\"using:Sample\"><Application.DataTemplates><local:ViewLocator />"
                           + "</Application.DataTemplates></Application>");
        string bare = Theme(Background("Transparent"), template: BareTemplate);

        AssertNullWhereDrawn(Test(View(List(), styles: Style("ListBoxItem", Background("{x:Null}")))));
        AssertNoBackground(Test(View(List(Themed), bare)));
        AssertNoBackground(Test(View(List(Themed + " ItemTemplate=\"{StaticResource Chooser}\""),
            bare + "<RowChooser x:Key=\"Chooser\"><DataTemplate><Border Background=\"Transparent\" /></DataTemplate></RowChooser>")));
    }

    /// <summary>
    /// What a presenter in the row template shows through a content template or a data template, the row's
    /// content, the item it is bound to, or content it is given, decides nothing: a presenter draws only its
    /// own Background, and a content control only what its own template draws.
    /// </summary>
    [Fact]
    public void WhatAPresenterInTheTemplateShows_DecidesNothing()
    {
        const string Painted = "<DataTemplate><Border Background=\"Transparent\" /></DataTemplate>";
        Write("Templates.axaml", "<UserControl " + Header + "><UserControl.DataTemplates>"
                                 + Painted.Replace("<DataTemplate>", "<DataTemplate DataType=\"x:String\">", StringComparison.Ordinal)
                                 + "</UserControl.DataTemplates></UserControl>");

        foreach (string template in (string[])
                 [
                     "<ContentControl Content=\"row\" ContentTemplate=\"{StaticResource Painted}\" />",
                     "<ContentPresenter Content=\"{Binding}\"><ContentPresenter.ContentTemplate>" + Painted + "</ContentPresenter.ContentTemplate></ContentPresenter>",
                     "<ContentPresenter Content=\"{TemplateBinding DataContext}\" />",
                     "<ContentControl Content=\"{Binding}\" />",
                     "<ContentPresenter Content=\"row\" />",
                 ])
        {
            AssertNoBackground(Test(Rows(template, resources: Painted.Replace("<DataTemplate>", "<DataTemplate x:Key=\"Painted\">", StringComparison.Ordinal))));
        }
    }

    // ── Both spellings, and other rows ─────────────────────────────────────────

    /// <summary>ItemsSource, the container theme and a template element's background, each written as a property element.</summary>
    [Fact]
    public void ThePropertyElementSpellings_Count()
    {
        const string Host =
            "<ListBox><ListBox.ItemsSource><Binding Path=\"Rows\" /></ListBox.ItemsSource>"
            + "<ListBox.ItemContainerTheme><StaticResource ResourceKey=\"RowTheme\" /></ListBox.ItemContainerTheme></ListBox>";
        const string Template =
            "<Border><Border.Background><TemplateBinding Property=\"Background\" /></Border.Background>"
            + "<ContentPresenter Content=\"{TemplateBinding Content}\" /></Border>";

        AssertNullWhereDrawn(Test(View(Host, Theme(Background("{Binding RowBrush}")))));
        AssertNullWhereDrawn(Test(Rows(Template, "{x:Null}")));
        AssertClean(Test(Rows(Template)));
    }

    /// <summary>A drop-down's rows are list rows too.</summary>
    [Fact]
    public void AComboBoxsRows_AreChecked() =>
        AssertNullWhereDrawn(Test(View(List(host: "ComboBox"), styles: Style("ComboBoxItem", Background("{x:Null}")))));

    /// <summary>A tab control's tabs are rows too.</summary>
    [Fact]
    public void ATabControlsTabs_AreChecked() =>
        AssertNullWhereDrawn(Test(View(List(host: "TabControl"), styles: Style("TabItem", Background("{x:Null}")))));
}
