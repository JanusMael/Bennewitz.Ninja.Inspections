namespace FpProbe6;

/// <summary>Judge 11: a Border subclass with no markup of its own, whose base's x:Class markup sets Background Transparent.</summary>
public class DerivedXClassBorder : XClassBorder
{
}

/// <summary>Judge 11: a Panel subclass with no markup of its own, whose base's x:Class markup sets Background Transparent.</summary>
public class DerivedXClassPanel : XClassPanel
{
}

// Judge 11's cases for BNXQ1010 round 11, aimed at round 11's own fixes: the spellings DisablingIn and
// SelectedAtStart read, and an x:Class type's subclass. A case ending in "c" is a control.
internal static partial class Cases
{
    /// <summary>The list in <paramref name="markup"/> given one more attribute.</summary>
    private static string ListWith(string markup, string attribute) =>
        markup.Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" " + attribute);

    /// <summary>The list's own Styles, holding each style given.</summary>
    private static string ListStyles(params string[] styles) => "<ListBox.Styles>" + string.Concat(styles) + "</ListBox.Styles>";

    private static string StyleOf(string selector, string property, string value) =>
        "<Style Selector=\"" + selector + "\"><Setter Property=\"" + property + "\" Value=\"" + value + "\"/></Style>";

    private const string DisabledPartRed = "<Style Selector=\"ListBoxItem:disabled /template/ Border#Bg\"><Setter Property=\"Background\" Value=\"Red\"/></Style>";

    /// <summary>A button holding the list, named Inner, whose markup is <paramref name="list"/>; <paramref name="attributes"/> and <paramref name="inside"/> as given.</summary>
    private static string ButtonHolding(string attributes, string inside, string list) => """
        <Button NAMESPACES ATTRIBUTES Width="320" Height="220">
          INSIDE
          LIST
        </Button>
        """.Replace("NAMESPACES", Namespaces).Replace("ATTRIBUTES", attributes).Replace("INSIDE", inside)
        .Replace("LIST", list.Replace(Namespaces, string.Empty).Replace("<ListBox ", "<ListBox Name=\"Inner\" "));

    /// <summary>EL02's list: row theme Transparent, template PART_ContentPresenter drawing nothing, nested '^:selected' painting it Red.</summary>
    private static string SelectedPartList(string extra) =>
        List(NestedTheme("Transparent", Presenter, SelectedPresenterRed) + extra, Plain);

    public static readonly Case[] AllJ11 =
    [
        // ── F1/F2: IsEnabled written in a spelling Avalonia takes ───────────────────────────────────
        C("G01", "J03's rows and part style; the list's IsEnabled written InputElement.IsEnabled=\"False\"",
            ListWith(List(BgPanelTheme() + ListStyles(DisabledPartRed), Plain), "InputElement.IsEnabled=\"False\"")),
        C("G01b", "the same written ListBox.IsEnabled=\"False\"",
            ListWith(List(BgPanelTheme() + ListStyles(DisabledPartRed), Plain), "ListBox.IsEnabled=\"False\"")),
        C("G01c", "the same with IsEnabled=\"False\" (control: the spelling the rule reads)",
            ListWith(List(BgPanelTheme() + ListStyles(DisabledPartRed), Plain), "IsEnabled=\"False\"")),
        C("G01d", "the same with the list enabled (control)",
            List(BgPanelTheme() + ListStyles(DisabledPartRed), Plain)),
        C("G02", "J03's rows and part style; a list style 'ListBox' setting (InputElement.IsEnabled) False",
            List(BgPanelTheme() + ListStyles(StyleOf("ListBox", "(InputElement.IsEnabled)", "False"), DisabledPartRed), Plain)),
        C("G02c", "the same setter written InputElement.IsEnabled, no parentheses (control)",
            List(BgPanelTheme() + ListStyles(StyleOf("ListBox", "InputElement.IsEnabled", "False"), DisabledPartRed), Plain)),
        C("G03", "row theme {x:Null}, Border takes it, nested '^:disabled' Transparent; the list's IsEnabled written InputElement.IsEnabled=\"False\"",
            ListWith(List(RowTheme(DisabledTransparent), Plain), "InputElement.IsEnabled=\"False\"")),
        C("G03c", "the same with IsEnabled=\"False\" (control)",
            ListWith(List(RowTheme(DisabledTransparent), Plain), "IsEnabled=\"False\"")),
        C("G04", "EL05 with the button's command written Button.Command, a command that never executes",
            ButtonHolding("Button.Command=\"{x:Static p:R11Commands.Never}\"", string.Empty, List(RowTheme(DisabledTransparent), Plain))),
        C("G04b", "the same given by a button style '(Button.Command)'",
            ButtonHolding(string.Empty,
                "<Button.Styles><Style Selector=\"Button\"><Setter Property=\"(Button.Command)\" Value=\"{x:Static p:R11Commands.Never}\"/></Style></Button.Styles>",
                List(RowTheme(DisabledTransparent), Plain))),
        C("G04c", "the same written Command= (control: EL05)",
            ButtonHolding("Command=\"{x:Static p:R11Commands.Never}\"", string.Empty, List(RowTheme(DisabledTransparent), Plain))),

        // ── F6: a selection written in a spelling Avalonia takes ────────────────────────────────────
        C("G05", "EL02's rows; the list's SelectedIndex written SelectingItemsControl.SelectedIndex=\"0\"",
            ListWith(SelectedPartList(string.Empty), "SelectingItemsControl.SelectedIndex=\"0\"")),
        C("G05b", "the same written ListBox.SelectedIndex=\"0\"",
            ListWith(SelectedPartList(string.Empty), "ListBox.SelectedIndex=\"0\"")),
        C("G05c", "the same written SelectedIndex=\"0\" (control: EL02)",
            ListWith(SelectedPartList(string.Empty), "SelectedIndex=\"0\"")),
        C("G06", "EL02's rows; a list style 'ListBox' setting (ListBox.SelectionMode) AlwaysSelected",
            SelectedPartList(ListStyles(StyleOf("ListBox", "(ListBox.SelectionMode)", "AlwaysSelected")))),
        C("G06c", "the same setter written ListBox.SelectionMode, no parentheses (control)",
            SelectedPartList(ListStyles(StyleOf("ListBox", "ListBox.SelectionMode", "AlwaysSelected")))),
        C("G07", "EL02's rows; the list's SelectionMode written ListBox.SelectionMode=\"AlwaysSelected\"",
            ListWith(SelectedPartList(string.Empty), "ListBox.SelectionMode=\"AlwaysSelected\"")),
        C("G08", "EL02's rows; SelectionMode=\"alwaysSelected\", in lower case",
            ListWith(SelectedPartList(string.Empty), "SelectionMode=\"alwaysSelected\"")),
        C("G08c", "EL02's rows; SelectionMode=\"AlwaysSelected\" (control)",
            ListWith(SelectedPartList(string.Empty), "SelectionMode=\"AlwaysSelected\"")),
        C("G09", "EL02's rows; SelectionMode=\"4\", AlwaysSelected's value",
            ListWith(SelectedPartList(string.Empty), "SelectionMode=\"4\"")),
        C("G15", "EL02's row theme with one more setter, (ListBoxItem.IsSelected) True",
            List(NestedTheme("Transparent", Presenter, "<Setter Property=\"(ListBoxItem.IsSelected)\" Value=\"True\"/>" + SelectedPresenterRed), Plain)),
        C("G15c", "the same setter written IsSelected (control)",
            List(NestedTheme("Transparent", Presenter, "<Setter Property=\"IsSelected\" Value=\"True\"/>" + SelectedPresenterRed), Plain)),

        // ── F5: a subclass of a type whose x:Class markup paints ────────────────────────────────────
        C("G10", "row theme Transparent; template Panel: p:DerivedXClassBorder beside the presenter, a subclass with no markup of XClassBorder, whose x:Class markup is Transparent",
            List(TransparentTheme(string.Empty, "<Panel><p:DerivedXClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),
        C("G10b", "the same with p:DerivedXClassPanel, a subclass of XClassPanel",
            List(TransparentTheme(string.Empty, "<Panel><p:DerivedXClassPanel Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassPanel.axaml", SourceView("XClassPanel.axaml"))),
        C("G10c", "the same with p:XClassBorder itself (control: J06)",
            List(TransparentTheme(string.Empty, "<Panel><p:XClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),
        C("G11", "row theme {x:Null}; template Panel: p:DerivedXClassBorder given {TemplateBinding Background}, beside the presenter",
            List(NullRowTemplate("<Panel><p:DerivedXClassBorder Height=\"10\" Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),
        C("G11c", "the same with p:XClassBorder itself (control: EL07c)",
            List(NullRowTemplate("<Panel><p:XClassBorder Height=\"10\" Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),

        // ── F5: an x:Class that two files in the scan declare ───────────────────────────────────────
        C("G12", "J06 (row theme Transparent; template Panel: p:XClassBorder beside the presenter) with XClassBorder's markup in the scan twice",
            List(TransparentTheme(string.Empty, "<Panel><p:XClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml")), ("XClassBorder.Copy.axaml", SourceView("XClassBorder.axaml"))),
        C("G12c", "the same with p:TintView, a view, whose markup is in the scan twice (control: a view's twice-declared markup)",
            List(TransparentTheme(string.Empty, "<Panel><p:TintView Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("TintView.axaml", SourceView("TintView.axaml")), ("TintView.Copy.axaml", SourceView("TintView.axaml"))),

        // ── Outside round 11's fixes: the same two spellings for Background ─────────────────────────
        C("G16", "row theme Transparent; template Panel: a Border whose Background is written Border.Background=\"Transparent\", beside the presenter",
            List(TransparentTheme(string.Empty, "<Panel><Border Height=\"10\" Border.Background=\"Transparent\"/>" + Presenter + "</Panel>"), Plain)),
        C("G17", "J09c's rows (template Panel: Border#Bg beside the presenter, nothing selected); list style 'ListBoxItem /template/ Border#Bg' setting (Border.Background) Red",
            List(BgPanelTheme() + ListStyles(StyleOf("ListBoxItem /template/ Border#Bg", "(Border.Background)", "Red")), Plain)),

        // ── F1: stock rows that start disabled, with no row theme in the scan ───────────────────────
        C("G13", "a disabled ListBox with no row theme in the scan: the framework's own rows",
            ListWith(List(string.Empty, Plain), "IsEnabled=\"False\"")),
        C("G13b", "a disabled TabControl with no row theme in the scan: the framework's own tabs",
            TabHost(string.Empty, Plain).Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" IsEnabled=\"False\"")),
    ];
}
