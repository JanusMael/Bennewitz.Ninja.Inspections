using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.Rules;

namespace XamlQuality.Tests.Rules;

/// <summary>
/// <see cref="ItemContainerNameRule"/> over markup written for the test, against the miniature
/// framework in <c>FocusFakes.cs</c> and the peers and item types in <c>ContainerNameFakes.cs</c>.
/// </summary>
/// <remarks>
/// ⭐ <b>What names a row was measured on Avalonia 12.1.3 first</b>, in a headless window under
/// Fluent, by asking each generated container's automation peer for its name: each container's
/// fallback, each shape of template root, <c>DisplayMemberBinding</c>, a class, record, struct, enum,
/// string and subclass as the item, a type selector against <c>:is()</c>, a state and a context
/// selector, and both ways of naming a tree. The rest, such as a blank name, a record struct, a class
/// selector, or a style or theme placed elsewhere, follows from the framework's source or from how
/// markup is read. The fakes reproduce the framework's shape, not its behaviour, so what these tests
/// prove is the rule's reading of markup and types; the measurements are what say that reading
/// matches the framework.
/// </remarks>
public sealed class ItemContainerNameRuleTests : IDisposable
{
    private const string Header =
        "xmlns=\"https://github.com/avaloniaui\" " +
        "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
        "xmlns:m=\"using:XamlQuality.Tests.Rules.ItemFakes\"";

    private const string PanelRoot = "<StackPanel><TextBlock Text=\"{Binding Label}\" /></StackPanel>";

    private const string TextRoot = "<TextBlock Text=\"{Binding Label}\" />";

    private readonly string _root;

    public ItemContainerNameRuleTests()
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
        new ItemContainerNameRule().Analyze(
            XamlScanContext.Load(_root).WithAssemblies(typeof(FocusFakes.ListBox).Assembly));

    private XamlRuleResult RunWithoutTypes() =>
        new ItemContainerNameRule().Analyze(XamlScanContext.Load(_root));

    private static string View(string body, string resources = "") =>
        $"<UserControl {Header}>{resources}{body}</UserControl>";

    /// <summary>An items control bound to a source, with <paramref name="inner"/> inside it.</summary>
    private static string Items(string inner, string attributes = "", string host = "ListBox") =>
        $"<{host} ItemsSource=\"{{Binding Rows}}\" {attributes}>{inner}</{host}>";

    private static string Template(string root, string dataType = "m:PlainItem", string host = "ListBox") =>
        $"<{host}.ItemTemplate><DataTemplate x:DataType=\"{dataType}\">{root}</DataTemplate></{host}.ItemTemplate>";

    /// <summary>An items control whose item template has <paramref name="root"/> at its root.</summary>
    private static string List(string root = PanelRoot, string dataType = "m:PlainItem", string attributes = "", string host = "ListBox") =>
        Items(Template(root, dataType, host), attributes, host);

    /// <summary>A style that names what its selector picks from the item's label.</summary>
    private static string NamingStyle(string selector) =>
        $"<Style Selector=\"{selector}\"><Setter Property=\"AutomationProperties.Name\" Value=\"{{Binding Label}}\" /></Style>";

    private static void AssertNamed(XamlRuleResult result)
    {
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
        Assert.Equal(1, result.Inspected);
    }

    private static XamlFinding AssertUnnamed(XamlRuleResult result)
    {
        Assert.Empty(result.Skipped);
        Assert.Equal(1, result.Inspected);
        XamlFinding finding = Assert.Single(result.Findings);
        Assert.Equal("BNXQ1009", finding.RuleId);
        return finding;
    }

    private static XamlSkip AssertSkipped(XamlRuleResult result)
    {
        Assert.Empty(result.Findings);
        Assert.Equal(0, result.Inspected);
        return Assert.Single(result.Skipped);
    }

    // ── What is a subject ──────────────────────────────────────────────────────

    /// <summary>A list whose items are written in markup generates no rows from a source: nothing to inspect.</summary>
    [Fact]
    public void MarkupWithNoItemsSource_InspectsNothing()
    {
        Write("View.axaml", View("<ListBox><ListBoxItem>One</ListBoxItem></ListBox>"));

        XamlRuleResult result = Run();

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }

    /// <summary>Which rows a control generates, and how they are named, are read from types.</summary>
    [Fact]
    public void WithoutAssemblies_EveryItemsSourceIsNamedAsSkipped()
    {
        Write("View.axaml", View($"<StackPanel>{List()}{List(TextRoot)}</StackPanel>"));

        XamlRuleResult result = RunWithoutTypes();

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Equal(2, result.Skipped.Count);
        Assert.All(result.Skipped, skip =>
        {
            Assert.Equal("ListBox", skip.Subject);
            Assert.Contains("The scan was given no assemblies", skip.Reason, StringComparison.Ordinal);
        });
    }

    /// <summary>An items control's containers are presenters, whose peer no search reaches: not rows.</summary>
    [Fact]
    public void AnItemsControlsPresenters_AreNotSubjects()
    {
        Write("View.axaml", View(List(host: "ItemsControl")));

        XamlRuleResult result = Run();

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }

    /// <summary>A tree template's source is its children's, not a control's.</summary>
    [Fact]
    public void ATreeDataTemplatesItemsSource_IsNotAHost()
    {
        Write("View.axaml", View(Items(
            "<TreeView.ItemTemplate><TreeDataTemplate x:DataType=\"m:NamedItem\" ItemsSource=\"{Binding Children}\">"
            + TextRoot + "</TreeDataTemplate></TreeView.ItemTemplate>",
            host: "TreeView")));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Contains("TreeViewItem", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A consumer's row whose peer writes its own name is named by code this rule does not read.</summary>
    [Fact]
    public void RowsWhosePeerNamesThemItself_AreSkipped()
    {
        Write("View.axaml", View(List(host: "m:SelfNamingList")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Equal("SelfNamingList", skip.Subject);
        Assert.Contains("SelfNamingPeer", skip.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AControlNoScannedAssemblyDefines_IsSkipped()
    {
        Write("View.axaml", View(List(host: "MysteryList")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("MysteryList is not a type", skip.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void TheItemsSourcePropertyElementSpelling_Counts()
    {
        Write("View.axaml", View(
            $"<ListBox><ListBox.ItemsSource><Binding Path=\"Rows\" /></ListBox.ItemsSource>{Template(PanelRoot)}</ListBox>"));

        AssertUnnamed(Run());
    }

    // ── Named by an item's ToString(), which its type does not write ───────────

    /// <summary>
    /// ⭐ The case the rule exists for: a panel at the template's root, over a view model with no
    /// <c>ToString()</c>. Measured: every row named with the type's full name.
    /// </summary>
    [Fact]
    public void APanelRootOverAViewModelWithNoToString_IsReported()
    {
        Write("View.axaml", View(List()));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Equal("View.axaml", finding.RelativePath);
        Assert.Equal(1, finding.Line);
        Assert.Contains("ListBoxItem containers this ListBox", finding.Message, StringComparison.Ordinal);
        Assert.Contains("a StackPanel, declares no name", finding.Message, StringComparison.Ordinal);
        Assert.Contains("\"XamlQuality.Tests.Rules.ItemFakes.PlainItem\"", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A record's compiler-written <c>ToString()</c> names a row with every property. Measured: "Rec { Label = Alpha }".</summary>
    [Theory]
    [InlineData("m:RecordItem")]
    [InlineData("m:RecordStructItem")]
    public void ARecord_IsReported_BecauseItsToStringListsEveryProperty(string dataType)
    {
        Write("View.axaml", View(List(dataType: dataType)));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Contains("is a record", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A struct gets <c>ValueType</c>'s, and an unsealed class nothing derives from gets <c>object</c>'s.</summary>
    [Theory]
    [InlineData("m:StructItem", "XamlQuality.Tests.Rules.ItemFakes.StructItem")]
    [InlineData("m:OpenItem", "XamlQuality.Tests.Rules.ItemFakes.OpenItem")]
    public void OtherTypesThatWriteNoToString_AreReported(string dataType, string fullName)
    {
        Write("View.axaml", View(List(dataType: dataType)));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Contains($"\"{fullName}\"", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A <c>ToString()</c> written in code names the row, and so do an enum's and a string's. Measured: "Alpha", "First".</summary>
    [Theory]
    [InlineData("m:NamedItem")]
    [InlineData("m:NamedRecordItem")]
    [InlineData("m:ModeItem")]
    [InlineData("x:String")]
    public void AnItemTypeWhoseToStringNamesIt_IsClean(string dataType)
    {
        Write("View.axaml", View(List(dataType: dataType)));

        AssertNamed(Run());
    }

    [Fact]
    public void TheDataTypeAttributeInItsXTypeSpelling_IsRead()
    {
        Write("View.axaml", View(Items(
            $"<ListBox.ItemTemplate><DataTemplate DataType=\"{{x:Type m:PlainItem}}\">{PanelRoot}</DataTemplate></ListBox.ItemTemplate>")));

        AssertUnnamed(Run());
    }

    /// <summary>Two types of one name are told apart by the namespace the prefix names.</summary>
    [Theory]
    [InlineData("using:XamlQuality.Tests.Rules.ItemFakes", false)]
    [InlineData("using:XamlQuality.Tests.Rules.ItemFakes.Elsewhere", true)]
    public void AnItemTypeIsFoundInItsOwnNamespace(string xmlns, bool named)
    {
        Write("View.axaml", $"<UserControl {Header} xmlns:n=\"{xmlns}\">" + List(dataType: "n:PlainItem") + "</UserControl>");

        XamlRuleResult result = Run();

        if (named)
        {
            AssertNamed(result);
        }
        else
        {
            AssertUnnamed(result);
        }
    }

    [Fact]
    public void AClrNamespaceXmlns_IsRead()
    {
        Write("View.axaml",
            $"<UserControl {Header} xmlns:c=\"clr-namespace:XamlQuality.Tests.Rules.ItemFakes;assembly=XamlQuality.Tests\">"
            + List(dataType: "c:PlainItem") + "</UserControl>");

        AssertUnnamed(Run());
    }

    /// <summary>What the items' concrete types are, and so their <c>ToString()</c>, is not in the markup.</summary>
    [Theory]
    [InlineData("m:AbstractItem", "abstract")]
    [InlineData("m:IItem", "an interface")]
    [InlineData("x:Object", "object, which every item is")]
    public void AnItemTypeThatSaysNothingOfTheItems_IsSkipped(string dataType, string why)
    {
        Write("View.axaml", View(List(dataType: dataType)));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains(why, skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>Items of a subclass that writes a <c>ToString()</c> are named by it. Measured: "Alpha".</summary>
    [Fact]
    public void ASubclassThatWritesToString_IsSkipped()
    {
        Write("View.axaml", View(List(dataType: "m:BaseItem")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("DerivedItem derives from BaseItem", skip.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ControlsAsItems_AreSkipped()
    {
        Write("View.axaml", View(List(dataType: "m:ControlItem")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("The items are controls", skip.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AnItemTemplateWithNoDataType_IsSkipped()
    {
        Write("View.axaml", View(Items(
            $"<ListBox.ItemTemplate><DataTemplate>{PanelRoot}</DataTemplate></ListBox.ItemTemplate>")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("declares no x:DataType", skip.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ADataTypeNoScannedAssemblyDefines_IsSkipped()
    {
        Write("View.axaml", View(List(dataType: "m:Nowhere")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("m:Nowhere", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>With no item template, a data template chosen by type presents each item, and the type is not in the markup.</summary>
    [Fact]
    public void NoItemTemplate_IsSkipped()
    {
        Write("View.axaml", View(Items("")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("No ItemTemplate is declared", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An empty template presents nothing, so the item's <c>ToString()</c> names the row. Measured.</summary>
    [Fact]
    public void AnEmptyItemTemplate_LeavesTheRowsToToString()
    {
        Write("View.axaml", View(List(root: "")));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Contains("is empty", finding.Message, StringComparison.Ordinal);
    }

    // ── Named by what the row presents ─────────────────────────────────────────

    /// <summary>A <c>TextBlock</c> at the root names the row with its text, in any spelling of it. Measured: "Alpha".</summary>
    [Theory]
    [InlineData("<TextBlock Text=\"{Binding Label}\" />")]
    [InlineData("<SelectableTextBlock Text=\"{Binding Label}\" />")]
    [InlineData("<TextBlock><Run Text=\"{Binding Label}\" /></TextBlock>")]
    [InlineData("<TextBlock><TextBlock.Text><Binding Path=\"Label\" /></TextBlock.Text></TextBlock>")]
    [InlineData("<TextBlock><TextBlock.Text>Label</TextBlock.Text></TextBlock>")]
    public void ATextBlockRootWithText_NamesTheRows(string root)
    {
        Write("View.axaml", View(List(root)));

        AssertNamed(Run());
    }

    /// <summary>
    /// ⭐ A <c>TextBlock</c>'s own automation name does not count, because its peer reads its text
    /// alone. Measured: a named <c>TextBlock</c> with no text left the row to <c>ToString()</c>.
    /// </summary>
    [Theory]
    [InlineData("<TextBlock Text=\"\" />")]
    [InlineData("<TextBlock Text=\"{x:Null}\" />")]
    [InlineData("<TextBlock AutomationProperties.Name=\"{Binding Label}\" />")]
    [InlineData("<TextBlock Text=\" \" AutomationProperties.Name=\"{Binding Label}\" />")]
    public void ATextBlockRootWithNoText_LeavesTheRowsToToString(string root)
    {
        Write("View.axaml", View(List(root)));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Contains("a TextBlock with no text", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Any other root that declares a name names the row, in either spelling. Measured: "Alpha".</summary>
    [Theory]
    [InlineData("<StackPanel AutomationProperties.Name=\"{Binding Label}\"><TextBlock Text=\"{Binding Label}\" /></StackPanel>")]
    [InlineData("<StackPanel><AutomationProperties.Name><Binding Path=\"Label\" /></AutomationProperties.Name><TextBlock Text=\"{Binding Label}\" /></StackPanel>")]
    [InlineData("<StackPanel AutomationProperties.LabeledBy=\"{Binding #Caption}\"><TextBlock Name=\"Caption\" Text=\"{Binding Label}\" /></StackPanel>")]
    [InlineData("<Button AutomationProperties.Name=\"{Binding Label}\" />")]
    public void ARootThatDeclaresAName_NamesTheRows(string root)
    {
        Write("View.axaml", View(List(root)));

        AssertNamed(Run());
    }

    /// <summary>An empty name is not a name, in either spelling.</summary>
    [Theory]
    [InlineData("<StackPanel AutomationProperties.Name=\"\"><TextBlock Text=\"{Binding Label}\" /></StackPanel>")]
    [InlineData("<StackPanel><AutomationProperties.Name></AutomationProperties.Name><TextBlock Text=\"{Binding Label}\" /></StackPanel>")]
    public void ABlankNameOnTheRoot_IsNotAName(string root)
    {
        Write("View.axaml", View(List(root)));

        AssertUnnamed(Run());
    }

    /// <summary>A root with a peer of its own may name the row from what it holds. Measured: a Button holding a TextBlock named it.</summary>
    [Fact]
    public void ARootWithAPeerOfItsOwn_IsSkipped()
    {
        Write("View.axaml", View(List("<Button><TextBlock Text=\"{Binding Label}\" /></Button>")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("is a Button", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>Each row presents a <c>TextBlock</c> bound to the member. Measured: "Alpha".</summary>
    [Fact]
    public void DisplayMemberBinding_NamesTheRows()
    {
        Write("View.axaml", View(Items("", "DisplayMemberBinding=\"{Binding Label}\"")));

        AssertNamed(Run());
    }

    [Fact]
    public void AnItemTemplateNamedByResource_IsFollowed()
    {
        Write("View.axaml", View(Items("", "ItemTemplate=\"{StaticResource Row}\""),
            $"<UserControl.Resources><DataTemplate x:Key=\"Row\" x:DataType=\"m:PlainItem\">{PanelRoot}</DataTemplate></UserControl.Resources>"));

        AssertUnnamed(Run());
    }

    /// <summary>An item template the list's theme sets, written as the setter's content, as Avalonia's themes write theirs.</summary>
    [Fact]
    public void AnItemTemplateTheListsThemeSets_IsFollowed()
    {
        Write("View.axaml", View(Items("", "Theme=\"{StaticResource Rows}\""),
            "<UserControl.Resources><ControlTheme x:Key=\"Rows\" TargetType=\"ListBox\"><Setter Property=\"ItemTemplate\">"
            + $"<DataTemplate x:DataType=\"m:PlainItem\">{PanelRoot}</DataTemplate></Setter></ControlTheme></UserControl.Resources>"));

        AssertUnnamed(Run());
    }

    // ── Named by the row itself: a style or its theme ──────────────────────────

    /// <summary>⭐ The fix the finding asks for. Measured: "Alpha".</summary>
    [Fact]
    public void AStyleForTheRowTypeInTheListsStyles_NamesThem()
    {
        Write("View.axaml", View(Items(Template(PanelRoot) + $"<ListBox.Styles>{NamingStyle("ListBoxItem")}</ListBox.Styles>")));

        AssertNamed(Run());
    }

    [Fact]
    public void AStyleInAnAncestorsStyles_NamesThem()
    {
        Write("View.axaml", View($"<Border>{List()}</Border>", $"<UserControl.Styles>{NamingStyle("ListBoxItem")}</UserControl.Styles>"));

        AssertNamed(Run());
    }

    [Fact]
    public void AStyleInTheApplication_NamesThem()
    {
        Write("App.axaml", $"<Application {Header}><Application.Styles>{NamingStyle("ListBoxItem")}</Application.Styles></Application>");
        Write("View.axaml", View(List()));

        AssertNamed(Run());
    }

    [Fact]
    public void AStyleTheApplicationIncludes_NamesThem()
    {
        Write("App.axaml", $"<Application {Header}><Application.Styles><StyleInclude Source=\"/Styles/Rows.axaml\" /></Application.Styles></Application>");
        Write("Styles/Rows.axaml", $"<Styles {Header}>{NamingStyle("ListBoxItem")}</Styles>");
        Write("View.axaml", View(List()));

        AssertNamed(Run());
    }

    /// <summary>A type selector matches exactly, so one for the base row misses a derived one. Measured: N1, N2 and N3.</summary>
    [Theory]
    [InlineData(":is(ListBoxItem)", true)]
    [InlineData("ComboBoxItem", true)]
    [InlineData("ListBoxItem", false)]
    public void AStyleNamesADerivedRowOnlyWhenItsSelectorPicksIt(string selector, bool named)
    {
        Write("View.axaml", View(Items(Template(PanelRoot, host: "ComboBox") + $"<ComboBox.Styles>{NamingStyle(selector)}</ComboBox.Styles>", host: "ComboBox")));

        XamlRuleResult result = Run();

        if (named)
        {
            AssertNamed(result);
        }
        else
        {
            Assert.Contains("ComboBoxItem containers", AssertUnnamed(result).Message, StringComparison.Ordinal);
        }
    }

    /// <summary>A selector with a state, a class or a context may name some rows, or rows in some places. Measured: ":selected" named only the selected row.</summary>
    [Theory]
    [InlineData("ListBoxItem:selected")]
    [InlineData("ListBoxItem.special")]
    [InlineData("ListBox > ListBoxItem")]
    [InlineData("ListBox.people ListBoxItem")]
    public void AStyleThatPicksTheRowsOnlySometimes_IsSkipped(string selector)
    {
        Write("View.axaml", View(Items(Template(PanelRoot) + $"<ListBox.Styles>{NamingStyle(selector)}</ListBox.Styles>")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("only in some state or some places", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>One alternative that is the row type alone is enough.</summary>
    [Fact]
    public void AnAlternativeThatIsTheRowTypeAlone_NamesThem()
    {
        Write("View.axaml", View(Items(Template(PanelRoot) + $"<ListBox.Styles>{NamingStyle("TabItem.special, ListBoxItem")}</ListBox.Styles>")));

        AssertNamed(Run());
    }

    /// <summary>A nested style's <c>^</c> is its parent's selector, and a state on it applies sometimes.</summary>
    [Fact]
    public void ANestedStyleWithAState_IsSkipped()
    {
        Write("View.axaml", View(Items(Template(PanelRoot)
            + "<ListBox.Styles><Style Selector=\"ListBoxItem\"><Style Selector=\"^:selected\">"
            + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></Style></Style></ListBox.Styles>")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("only in some state or some places", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An empty name is not a name. Measured: a style setting "" left the row to <c>ToString()</c>.</summary>
    [Fact]
    public void AStyleSettingABlankName_DoesNotNameThem()
    {
        Write("View.axaml", View(Items(Template(PanelRoot)
            + "<ListBox.Styles><Style Selector=\"ListBoxItem\"><Setter Property=\"AutomationProperties.Name\" Value=\"\" /></Style></ListBox.Styles>")));

        AssertUnnamed(Run());
    }

    /// <summary>The setter's other spellings count: the property in parentheses or in a Setter.Property element, and the value as content.</summary>
    [Theory]
    [InlineData("<Setter Property=\"(AutomationProperties.Name)\" Value=\"{Binding Label}\" />")]
    [InlineData("<Setter Value=\"{Binding Label}\"><Setter.Property>AutomationProperties.Name</Setter.Property></Setter>")]
    [InlineData("<Setter Property=\"AutomationProperties.Name\"><Binding Path=\"Label\" /></Setter>")]
    [InlineData("<Setter Property=\"AutomationProperties.Name\"><Setter.Value><Binding Path=\"Label\" /></Setter.Value></Setter>")]
    [InlineData("<Setter Property=\"AutomationProperties.LabeledBy\" Value=\"{Binding #Caption}\" />")]
    public void TheSettersOtherSpellings_Count(string setter)
    {
        Write("View.axaml", View(Items(Template(PanelRoot) + $"<ListBox.Styles><Style Selector=\"ListBoxItem\">{setter}</Style></ListBox.Styles>")));

        AssertNamed(Run());
    }

    /// <summary>A style in another view reaches this one only if that view hosts it, which markup does not show.</summary>
    [Fact]
    public void AStyleInAnotherView_IsSkipped()
    {
        Write("Shell.axaml", View("<ContentControl />", $"<UserControl.Styles>{NamingStyle("ListBoxItem")}</UserControl.Styles>"));
        Write("View.axaml", View(List()));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("Shell.axaml", skip.Reason, StringComparison.Ordinal);
        Assert.Contains("depends on where the view is placed", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>In one file, containment is visible: a style beside the list does not reach it.</summary>
    [Fact]
    public void AStyleBesideTheListInTheSameFile_DoesNotReachIt()
    {
        Write("View.axaml", View($"<StackPanel><Border><Border.Styles>{NamingStyle("ListBoxItem")}</Border.Styles></Border>{List()}</StackPanel>"));

        AssertUnnamed(Run());
    }

    /// <summary>A style that changes the list's template or the rows' theme is not evaluated.</summary>
    [Theory]
    [InlineData("<Style Selector=\"ListBox\"><Setter Property=\"ItemTemplate\" Value=\"{StaticResource Other}\" /></Style>", "ItemTemplate")]
    [InlineData("<Style Selector=\"ListBoxItem\"><Setter Property=\"Theme\" Value=\"{StaticResource Other}\" /></Style>", "Theme")]
    public void AStyleThatChangesWhatAppliesToTheRows_IsSkipped(string style, string property)
    {
        Write("View.axaml", View($"<StackPanel><StackPanel.Styles>{style}</StackPanel.Styles>{List()}</StackPanel>"));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains($"sets {property}", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>The container theme names them. Measured: "Alpha".</summary>
    [Fact]
    public void AnItemContainerTheme_NamesThem()
    {
        Write("View.axaml", View(Items(Template(PanelRoot)
            + "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\" BasedOn=\"{StaticResource {x:Type ListBoxItem}}\">"
            + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme></ListBox.ItemContainerTheme>")));

        AssertNamed(Run());
    }

    /// <summary>A container theme names them in the setter's other spellings too: the property in parentheses, or in a Setter.Property element.</summary>
    [Theory]
    [InlineData("<Setter Property=\"(AutomationProperties.Name)\" Value=\"{Binding Label}\" />")]
    [InlineData("<Setter Value=\"{Binding Label}\"><Setter.Property>AutomationProperties.Name</Setter.Property></Setter>")]
    public void AnItemContainerThemeInTheSettersOtherSpellings_NamesThem(string setter)
    {
        Write("View.axaml", View(Items(Template(PanelRoot)
            + "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\" BasedOn=\"{StaticResource {x:Type ListBoxItem}}\">"
            + setter + "</ControlTheme></ListBox.ItemContainerTheme>")));

        AssertNamed(Run());
    }

    /// <summary>
    /// The list's own properties, and a <c>TextBlock</c>'s text, are read after its type or a base, as the XAML
    /// compiler takes them, and after another owner only where that owner holds the same property, through a binding,
    /// the one value the compiler takes there: measured, a list written <c>ComboBox.ItemsSource="{Binding Rows}"</c>,
    /// or with it as a property element, generated rows that its item template's root named nothing on, one given
    /// <c>ComboBox.ItemContainerTheme="{DynamicResource Named}"</c>, either way, took the name that theme's setter
    /// bound, and rows whose item template's root was <c>&lt;TextBlock SelectableTextBlock.Text="{Binding Label}"/&gt;</c>
    /// were named by their labels. After an owner that holds another property they are that owner's.
    /// </summary>
    [Fact]
    public void PropertiesWrittenAfterAnOwner_CountWhereThatOwnerHoldsThem()
    {
        static void AssertNotASubject(XamlRuleResult result)
        {
            Assert.Equal(0, result.Inspected);
            Assert.Empty(result.Findings);
            Assert.Empty(result.Skipped);
        }

        Write("View.axaml", View($"<ListBox ComboBox.ItemsSource=\"{{Binding Rows}}\">{Template(PanelRoot)}</ListBox>"));
        AssertUnnamed(Run());
        Write("View.axaml", View($"<ListBox><ComboBox.ItemsSource><Binding Path=\"Rows\" /></ComboBox.ItemsSource>{Template(PanelRoot)}</ListBox>"));
        AssertUnnamed(Run());
        Write("View.axaml", View($"<ListBox ComboBox.ItemsSource=\"Rows\">{Template(PanelRoot)}</ListBox>"));
        AssertNotASubject(Run());

        const string Named = "<UserControl.Resources><ControlTheme x:Key=\"Named\" TargetType=\"ListBoxItem\">"
                             + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme></UserControl.Resources>";
        Write("View.axaml", View(List(attributes: "ComboBox.ItemContainerTheme=\"{DynamicResource Named}\""), Named));
        AssertNamed(Run());
        Write("View.axaml", View(Items(Template(PanelRoot) + "<ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"Named\" /></ComboBox.ItemContainerTheme>"), Named));
        AssertNamed(Run());
        Write("View.axaml", View(List(attributes: "Paint.ItemContainerTheme=\"{DynamicResource Named}\""), Named));
        AssertUnnamed(Run());

        Write("View.axaml", View(List("<TextBlock SelectableTextBlock.Text=\"{Binding Label}\" />")));
        AssertNamed(Run());
        Write("View.axaml", View(List("<TextBlock><SelectableTextBlock.Text><Binding Path=\"Label\" /></SelectableTextBlock.Text></TextBlock>")));
        AssertNamed(Run());
        Write("View.axaml", View(List("<TextBlock SelectableTextBlock.Text=\"Label\" />")));
        AssertUnnamed(Run());
        Write("View.axaml", View(List("<TextBlock><Wrapping.Text>Label</Wrapping.Text></TextBlock>")));
        AssertUnnamed(Run());
        Write("View.axaml", View(List("<TextBlock Unknowable.Text=\"{Binding Label}\" />")));
        Assert.Contains("whether it does could not be read", AssertSkipped(Run()).Reason, StringComparison.Ordinal);

        Write("View.axaml", View("<ListBox ItemsControl.ItemsSource=\"{Binding Rows}\" ItemsControl.DisplayMemberBinding=\"{Binding Label}\" />"));
        AssertNamed(Run());

        Write("View.axaml", View(List("<TextBlock TextBlock.Text=\"{Binding Label}\" />")));
        AssertNamed(Run());

        Write("View.axaml", View(Items("", "Control.Theme=\"{StaticResource Rows}\""),
            "<UserControl.Resources><ControlTheme x:Key=\"Rows\" TargetType=\"ListBox\"><Setter Property=\"ItemTemplate\">"
            + $"<DataTemplate x:DataType=\"m:PlainItem\">{PanelRoot}</DataTemplate></Setter></ControlTheme></UserControl.Resources>"));
        AssertUnnamed(Run());

        Write("View.axaml", View("<ListBox ItemsSource=\"{Binding Rows}\" Wrapping.DisplayMemberBinding=\"{Binding Label}\" />"));
        AssertSkipped(Run());
        Write("View.axaml", View("<ListBox ItemsSource=\"{Binding Rows}\"><Wrapping.DisplayMemberBinding><Binding Path=\"Label\" /></Wrapping.DisplayMemberBinding></ListBox>"));
        AssertSkipped(Run());

        Write("View.axaml", View("<ListBox Wrapping.ItemsSource=\"{Binding Rows}\" DisplayMemberBinding=\"{Binding Label}\" />"));
        AssertNotASubject(Run());
    }

    /// <summary>
    /// A property written after an owner whose type does not resolve may be the list's own, wherever the rule reads one:
    /// its source, and what names its rows where the list's theme would give it otherwise. Each is said, not guessed.
    /// </summary>
    [Fact]
    public void AnOwnerThatCannotBeRead_IsSkipped()
    {
        Write("View.axaml", View("<ListBox Unknowable.ItemsSource=\"{Binding Rows}\" />"));
        AssertSkipped(Run());

        static string ListTheme(string setter) =>
            $"<UserControl.Resources><ControlTheme x:Key=\"{{x:Type ListBox}}\" TargetType=\"ListBox\">{setter}</ControlTheme></UserControl.Resources>";
        Write("View.axaml", View("<ListBox ItemsSource=\"{Binding Rows}\" Unknowable.DisplayMemberBinding=\"{Binding Label}\" />",
            ListTheme("<Setter Property=\"DisplayMemberBinding\" Value=\"{Binding Label}\" />")));
        AssertSkipped(Run());
        Write("View.axaml", View("<ListBox ItemsSource=\"{Binding Rows}\" Unknowable.ItemTemplate=\"{StaticResource Other}\" />",
            ListTheme($"<Setter Property=\"ItemTemplate\"><DataTemplate x:DataType=\"m:PlainItem\">{TextRoot}</DataTemplate></Setter>")));
        AssertSkipped(Run());

        // A Text after an owner whose class constructor fails in the rule's process.
        Write("View.axaml", View(List("<TextBlock UnreadableTextBlock.Text=\"{Binding Label}\" />")));
        Assert.Contains("could not be read", AssertSkipped(Run()).Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A <c>TextBlock</c> that gives its Text two ways, or its Text beside inline content, decides nothing: the
    /// framework applies each in turn; measured, rows whose item template's root was
    /// <c>&lt;TextBlock Text="" SelectableTextBlock.Text="{Binding Label}"/&gt;</c> were named by their labels.
    /// </summary>
    [Theory]
    [InlineData("<TextBlock Text=\"\" SelectableTextBlock.Text=\"{Binding Label}\" />")]
    [InlineData("<TextBlock SelectableTextBlock.Text=\"{Binding Label}\" Text=\"\" />")]
    [InlineData("<TextBlock Text=\"\"><SelectableTextBlock.Text><Binding Path=\"Label\" /></SelectableTextBlock.Text></TextBlock>")]
    [InlineData("<TextBlock Text=\"\"><TextBlock.Text><Binding Path=\"Label\" /></TextBlock.Text></TextBlock>")]
    [InlineData("<TextBlock Text=\"\">Label</TextBlock>")]
    public void ATextGivenTwoWays_DecidesNothing(string root)
    {
        Write("View.axaml", View(List(root)));
        Assert.Contains("which the framework applies in turn", AssertSkipped(Run()).Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The row theme names the rows by the last of <c>AutomationProperties</c>' own setters, whatever another owner's
    /// attached <c>Name</c> is set after it: measured, rows whose theme set <c>AutomationProperties.Name</c> and then a
    /// consumer's <c>NameTag.Name</c> were named by their labels, as were rows whose theme set only the latter, over a
    /// base that set the former.
    /// </summary>
    [Fact]
    public void AnotherOwnersNameAfterTheAutomationName_DoesNotHideIt()
    {
        const string Naming = "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" />";
        const string Tagging = "<Setter Property=\"NameTag.Name\" Value=\"tag\" />";
        Write("View.axaml", View(Items(Template(PanelRoot)
            + $"<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\">{Naming}{Tagging}</ControlTheme></ListBox.ItemContainerTheme>")));
        AssertNamed(Run());

        Write("View.axaml", View(Items(Template(PanelRoot)
                + $"<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\" BasedOn=\"{{StaticResource Base}}\">{Tagging}</ControlTheme></ListBox.ItemContainerTheme>"),
            $"<UserControl.Resources><ControlTheme x:Key=\"Base\" TargetType=\"ListBoxItem\">{Naming}</ControlTheme></UserControl.Resources>"));
        AssertNamed(Run());

        Write("View.axaml", View(Items(Template(PanelRoot)
            + $"<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\">{Tagging}</ControlTheme></ListBox.ItemContainerTheme>")));
        AssertUnnamed(Run());

        // An owner is the type its prefix names: AutomationProperties in another namespace is another type's, one in a
        // URI only an assembly's XmlnsDefinition maps is not decided, and Avalonia's through using: is Avalonia's.
        static string RowTheme(string setters) =>
            $"<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\">{setters}</ControlTheme></ListBox.ItemContainerTheme>";
        const string Theirs = "xmlns:alt=\"using:XamlQuality.Tests.Rules.ItemFakes.Alt\" Property=\"alt:AutomationProperties.Name\"";
        Write("View.axaml", View(Items(Template(PanelRoot) + RowTheme(Naming + "<Setter " + Theirs + " Value=\"{x:Null}\" />"))));
        AssertNamed(Run());
        Write("View.axaml", View(Items(Template(PanelRoot) + RowTheme("<Setter " + Theirs + " Value=\"{Binding Label}\" />"))));
        AssertUnnamed(Run());
        Write("View.axaml", View(Items(Template(PanelRoot)
            + RowTheme("<Setter xmlns:u=\"https://example.test/alt\" Property=\"u:AutomationProperties.Name\" Value=\"{Binding Label}\" />"))));
        AssertSkipped(Run());
        Write("View.axaml", View(Items(Template(PanelRoot)
            + RowTheme("<Setter xmlns:a=\"using:Avalonia.Automation\" Property=\"a:AutomationProperties.Name\" Value=\"{Binding Label}\" />"))));
        AssertNamed(Run());

        // So it is in a style for the row type: measured, a Style for ListBoxItem in the list's Styles that set a consumer's
        // AutomationProperties.Name, in a URI its XmlnsDefinition maps, left every row named by its item's ToString().
        static string RowStyle(string setter) => "<ListBox.Styles><Style Selector=\"ListBoxItem\">" + setter + "</Style></ListBox.Styles>";
        foreach (string value in (string[])["{Binding Label}", "Named"])
        {
            Write("View.axaml", View(Items(Template(PanelRoot)
                + RowStyle("<Setter xmlns:u=\"https://example.test/alt\" Property=\"u:AutomationProperties.Name\" Value=\"" + value + "\" />"))));
            AssertSkipped(Run());
        }

        Write("View.axaml", View(Items(Template(PanelRoot) + RowStyle(Naming))));
        AssertNamed(Run());

        // Beside Avalonia's, whatever else such a style sets, the rows are named.
        Write("View.axaml", View(Items(Template(PanelRoot)
            + RowStyle("<Setter xmlns:u=\"https://example.test/alt\" Property=\"u:AutomationProperties.Name\" Value=\"{Binding Label}\" />" + Naming))));
        AssertNamed(Run());
    }

    /// <summary>
    /// A Text written after an owner is the <c>TextBlock</c>'s own where the field the XAML compiler takes for it holds
    /// the <c>TextBlock</c>'s: measured, rows whose item template's root was <c>&lt;TextBlock TextAlias.Text="Named"/&gt;</c>,
    /// where <c>TextAlias</c> is a static class whose field holds a <c>TextBlock</c>'s Text, with accessors, were named
    /// <c>Named</c>, and rows whose root was <c>&lt;TextBlock ShadowTextBlock.Text="{Binding Label}"/&gt;</c>, where
    /// <c>ShadowTextBlock</c> derives from <c>TextBlock</c> and hides its field with one of another property, were named by
    /// their labels.
    /// </summary>
    [Fact]
    public void ATextAfterAnOwner_IsTheTextBlocksWhereTheCompilersFieldHoldsIt()
    {
        Write("View.axaml", View(List("<TextBlock TextAlias.Text=\"Named\" />")));
        AssertNamed(Run());
        Write("View.axaml", View(List("<TextBlock ShadowTextBlock.Text=\"{Binding Label}\" />")));
        AssertNamed(Run());
    }

    [Fact]
    public void AnItemContainerThemeNamedByResource_NamesThem()
    {
        Write("View.axaml", View(List(attributes: "ItemContainerTheme=\"{StaticResource Row}\""),
            "<UserControl.Resources><ControlTheme x:Key=\"Row\" TargetType=\"ListBoxItem\">"
            + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme></UserControl.Resources>"));

        AssertNamed(Run());
    }

    [Fact]
    public void AnImplicitRowThemeInAnAncestor_NamesThem()
    {
        Write("View.axaml", View(List(),
            "<UserControl.Resources><ControlTheme x:Key=\"{x:Type ListBoxItem}\" TargetType=\"ListBoxItem\">"
            + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme></UserControl.Resources>"));

        AssertNamed(Run());
    }

    [Fact]
    public void ANameInheritedThroughBasedOn_Counts()
    {
        Write("View.axaml", View(List(attributes: "ItemContainerTheme=\"{StaticResource Derived}\""),
            "<UserControl.Resources>"
            + "<ControlTheme x:Key=\"Base\" TargetType=\"ListBoxItem\"><Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme>"
            + "<ControlTheme x:Key=\"Derived\" TargetType=\"ListBoxItem\" BasedOn=\"{StaticResource Base}\"><Setter Property=\"Padding\" Value=\"2\" /></ControlTheme>"
            + "</UserControl.Resources>"));

        AssertNamed(Run());
    }

    /// <summary>A name the theme sets in a nested style applies in some state only.</summary>
    [Fact]
    public void AContainerThemeThatNamesThemOnlyInAState_IsSkipped()
    {
        Write("View.axaml", View(Items(Template(PanelRoot)
            + "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\"><Style Selector=\"^:selected\">"
            + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></Style></ControlTheme></ListBox.ItemContainerTheme>")));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("applies only in some state", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An implicit row theme in another view reaches this one only if that view hosts it.</summary>
    [Fact]
    public void AnImplicitRowThemeInAnotherView_IsSkipped()
    {
        Write("Shell.axaml", View("<ContentControl />",
            "<UserControl.Resources><ControlTheme x:Key=\"{x:Type ListBoxItem}\" TargetType=\"ListBoxItem\">"
            + "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme></UserControl.Resources>"));
        Write("View.axaml", View(List()));

        XamlSkip skip = AssertSkipped(Run());

        Assert.Contains("depends on where the view is placed", skip.Reason, StringComparison.Ordinal);
    }

    // ── Trees and tabs ─────────────────────────────────────────────────────────

    /// <summary>
    /// ⭐ A tree row's peer reads its name and nothing else, so a text root and an item that writes
    /// <c>ToString()</c> leave it unnamed. Measured: 12 of 12 shapes gave an empty name.
    /// </summary>
    [Fact]
    public void ATreeViewsRows_AreReported_WhateverTheyPresent()
    {
        Write("View.axaml", View(List(TextRoot, "m:NamedItem", host: "TreeView")));

        XamlFinding finding = AssertUnnamed(Run());

        Assert.Contains("TreeViewItem containers this TreeView", finding.Message, StringComparison.Ordinal);
        Assert.Contains("reads AutomationProperties.Name and nothing else", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Either way of naming a tree row names every level. Measured: level 1 and level 2.</summary>
    [Theory]
    [InlineData("<TreeView.Styles><Style Selector=\"TreeViewItem\"><Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></Style></TreeView.Styles>")]
    [InlineData("<TreeView.ItemContainerTheme><ControlTheme TargetType=\"TreeViewItem\"><Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\" /></ControlTheme></TreeView.ItemContainerTheme>")]
    public void ATreeViewWithNamedRows_IsClean(string naming)
    {
        Write("View.axaml", View(Items(Template(TextRoot, "m:NamedItem", "TreeView") + naming, host: "TreeView")));

        AssertNamed(Run());
    }

    /// <summary>A tab control's item template presents each tab's header, and names it as a list's does. Measured.</summary>
    [Theory]
    [InlineData(PanelRoot, false)]
    [InlineData(TextRoot, true)]
    public void ATabControlsTabs_AreNamedByItsItemTemplate(string root, bool named)
    {
        Write("View.axaml", View(List(root, host: "TabControl")));

        XamlRuleResult result = Run();

        if (named)
        {
            AssertNamed(result);
        }
        else
        {
            Assert.Contains("TabItem containers this TabControl", AssertUnnamed(result).Message, StringComparison.Ordinal);
        }
    }

    // ── Counting ───────────────────────────────────────────────────────────────

    /// <summary>A named list and a reported one are inspected; a skipped one is not.</summary>
    [Fact]
    public void EachDecidedControl_CountsOnceInInspected()
    {
        Write("View.axaml", View($"<StackPanel>{List(TextRoot)}{List()}{Items("")}</StackPanel>"));

        XamlRuleResult result = Run();

        Assert.Equal(2, result.Inspected);
        Assert.Single(result.Findings);
        Assert.Single(result.Skipped);
    }
}
