namespace FpProbe6;

/// <summary>Judge 12: two subclasses down from XClassBorder, with no markup of their own.</summary>
public class DerivedDerivedXClassBorder : DerivedXClassBorder
{
}

/// <summary>Judge 12: a subclass, with no markup of its own, of the view TintView, whose markup is Transparent.</summary>
public class DerivedTintView : TintView
{
}

// Judge 12's cases for BNXQ1010, aimed at the fixes made since judge 11: the spellings DisablingIn,
// DisabledAtStart and SelectedAtStart read (F1, F2), and the markup a type's chain carries (F3). A case ending in
// "c" is a control; one ending in "x" holds its list in a view compiled at build time rather than at run time.
internal static partial class Cases
{
    /// <summary>A Setter whose Property is written as a property element, a spelling the setter transformer takes.</summary>
    private static string ElementSetter(string property, string value) =>
        "<Setter Value=\"" + value + "\"><Setter.Property>" + property + "</Setter.Property></Setter>";

    private static string StyleWith(string selector, string setters) =>
        "<Style Selector=\"" + selector + "\">" + setters + "</Style>";

    /// <summary>
    /// A Border holding a compiled view. The view is not the root: the run-time loader replaces the compiled markup
    /// of the x:Class type it loads as a root with the markup it is given, through its !XamlIlPopulateOverride.
    /// </summary>
    private static string ViewRoot(string type) => "<Border " + Namespaces + "><p:" + type + "/></Border>";

    public static readonly Case[] AllJ12 =
    [
        // ── F1: IsEnabled and Command given by a Setter whose Property is a property element ──────────
        C("M01", "J03's rows and part style; a list style 'ListBox' whose Setter names IsEnabled in a Setter.Property element, Value False",
            List(BgPanelTheme() + ListStyles(StyleWith("ListBox", ElementSetter("IsEnabled", "False")), DisabledPartRed), Plain)),
        C("M01c", "the same setter written Property=\"IsEnabled\" (control)",
            List(BgPanelTheme() + ListStyles(StyleOf("ListBox", "IsEnabled", "False"), DisabledPartRed), Plain)),
        C("M01x", "M01's list in SetterElementView, a view compiled at build time",
            ViewRoot("SetterElementView"), ("SetterElementView.axaml", SourceView("SetterElementView.axaml"))),
        C("M02", "row theme {x:Null}, Border takes it, nested '^:disabled' Transparent; a list style 'ListBox' whose Setter names IsEnabled in a Setter.Property element, Value False",
            List(RowTheme(DisabledTransparent) + ListStyles(StyleWith("ListBox", ElementSetter("IsEnabled", "False"))), Plain)),
        C("M02c", "the same setter written Property=\"IsEnabled\" (control)",
            List(RowTheme(DisabledTransparent) + ListStyles(StyleOf("ListBox", "IsEnabled", "False")), Plain)),
        C("M03", "EL05's rows in a Button whose own style's Setter names Command in a Setter.Property element, a command that never executes",
            ButtonHolding(string.Empty,
                "<Button.Styles>" + StyleWith("Button", ElementSetter("Command", "{x:Static p:R11Commands.Never}")) + "</Button.Styles>",
                List(RowTheme(DisabledTransparent), Plain))),
        C("M03c", "the same setter written Property=\"Command\" (control)",
            ButtonHolding(string.Empty,
                "<Button.Styles>" + StyleOf("Button", "Command", "{x:Static p:R11Commands.Never}") + "</Button.Styles>",
                List(RowTheme(DisabledTransparent), Plain))),
        C("M10", "row theme {x:Null}, Border takes it, nested '^:disabled' Transparent, and a setter naming IsEnabled in a Setter.Property element, Value False",
            List(RowTheme(ElementSetter("IsEnabled", "False") + DisabledTransparent), Plain)),
        C("M10c", "the same setter written Property=\"IsEnabled\" (control)",
            List(RowTheme("<Setter Property=\"IsEnabled\" Value=\"False\"/>" + DisabledTransparent), Plain)),

        // ── F2: a selection given by a Setter whose Property is a property element ──────────────────
        C("M04", "EL02's row theme with one more setter naming IsSelected in a Setter.Property element, Value True",
            List(NestedTheme("Transparent", Presenter, ElementSetter("IsSelected", "True") + SelectedPresenterRed), Plain)),
        C("M04c", "the same setter written Property=\"IsSelected\" (control: G15c)",
            List(NestedTheme("Transparent", Presenter, "<Setter Property=\"IsSelected\" Value=\"True\"/>" + SelectedPresenterRed), Plain)),
        C("M04x", "M04's list in SetterElementSelectView, a view compiled at build time",
            ViewRoot("SetterElementSelectView"), ("SetterElementSelectView.axaml", SourceView("SetterElementSelectView.axaml"))),
        C("M05", "EL02's rows; a list style 'ListBox' whose Setter names SelectedIndex in a Setter.Property element, Value 0",
            SelectedPartList(ListStyles(StyleWith("ListBox", ElementSetter("SelectedIndex", "0"))))),
        C("M05c", "the same setter written Property=\"SelectedIndex\" (control)",
            SelectedPartList(ListStyles(StyleOf("ListBox", "SelectedIndex", "0")))),
        C("M11", "EL02's rows; the list's SelectedIndex given as a property element after a base, SelectingItemsControl.SelectedIndex, 0",
            SelectedPartList("<SelectingItemsControl.SelectedIndex>0</SelectingItemsControl.SelectedIndex>")),
        C("M12", "EL02's rows; the list's SelectedIndex written after an owner that is not a base, ComboBox.SelectedIndex=\"0\"",
            ListWith(SelectedPartList(string.Empty), "ComboBox.SelectedIndex=\"0\"")),
        C("M12b", "the same written TabControl.SelectedIndex=\"0\"",
            ListWith(SelectedPartList(string.Empty), "TabControl.SelectedIndex=\"0\"")),

        // ── F1: IsEnabled after an owner that resolves to a type that takes no part in focus ───────────
        C("M13", "row theme {x:Null}, Border takes it, nested '^:disabled' Transparent; the list's IsEnabled written IInputElement.IsEnabled=\"False\"",
            ListWith(List(RowTheme(DisabledTransparent), Plain), "IInputElement.IsEnabled=\"False\"")),
        C("M13b", "the same written TextBlock.IsEnabled=\"False\", an owner that takes part in focus and is not a base",
            ListWith(List(RowTheme(DisabledTransparent), Plain), "TextBlock.IsEnabled=\"False\"")),

        // ── Outside the fixes: the same Setter.Property spelling for Background, as G16 and G17 are ────────
        C("M14", "J09c's rows (template Panel: Border#Bg beside the presenter, nothing selected); list style 'ListBoxItem /template/ Border#Bg' whose Setter names Background in a Setter.Property element, Red",
            List(BgPanelTheme() + ListStyles(StyleWith("ListBoxItem /template/ Border#Bg", ElementSetter("Background", "Red"))), Plain)),
        C("M15", "a row theme whose Background is Transparent through a Setter naming it in a Setter.Property element; template: Border taking it around the presenter",
            List("<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\">" + ElementSetter("Background", "Transparent")
                 + "<Setter Property=\"Template\"><ControlTemplate><Border Background=\"{TemplateBinding Background}\">" + Presenter
                 + "</Border></ControlTemplate></Setter></ControlTheme></ListBox.ItemContainerTheme>", Plain)),

        // ── F3: the markup a type's chain carries ───────────────────────────────────────────────────
        C("M07", "J06 (template Panel: p:XClassBorder beside the presenter) with XClassBorder's markup not in the scan, compiled into the probe",
            List(TransparentTheme(string.Empty, "<Panel><p:XClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain)),
        C("M07b", "G10 (p:DerivedXClassBorder) with XClassBorder's markup not in the scan",
            List(TransparentTheme(string.Empty, "<Panel><p:DerivedXClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain)),
        C("M08", "G10 with p:DerivedDerivedXClassBorder, two subclasses down from XClassBorder, whose markup is in the scan",
            List(TransparentTheme(string.Empty, "<Panel><p:DerivedDerivedXClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),
        C("M09", "row theme Transparent; template Panel: p:DerivedTintView, a subclass of the view TintView, beside the presenter; TintView's markup in the scan",
            List(TransparentTheme(string.Empty, "<Panel><p:DerivedTintView Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("TintView.axaml", SourceView("TintView.axaml"))),
        C("M09b", "M09 with TintView's markup not in the scan",
            List(TransparentTheme(string.Empty, "<Panel><p:DerivedTintView Height=\"10\"/>" + Presenter + "</Panel>"), Plain)),
    ];
}
