using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FpProbe6;

/// <summary>Judge 15: values a binding's Source can name.</summary>
public static class J15Values
{
    public static bool Yes { get; } = true;

    public static bool No { get; } = false;
}

/// <summary>Judge 15's class constructor that fails, as one that needs what a scan's process lacks does.</summary>
internal static class J15Fail
{
    public static void Now(string type)
    {
        if (Environment.GetEnvironmentVariable("J15_CCTOR_OK") is null)
        {
            throw new InvalidOperationException($"Judge 15: the class constructor of {type} fails.");
        }
    }
}

/// <summary>Judge 15: a Border subclass whose class constructor fails; its CLR Background is Border's.</summary>
public class BadBorder15 : Border
{
    static BadBorder15() => J15Fail.Now(nameof(BadBorder15));
}

/// <summary>Judge 15: a Button subclass whose class constructor fails; its CLR Focusable and IsEnabled are InputElement's.</summary>
public class BadButton15 : Button
{
    static BadButton15() => J15Fail.Now(nameof(BadButton15));
}

/// <summary>Judge 15: a SelectableTextBlock subclass whose class constructor fails; its CLR Text is TextBlock's.</summary>
public class BadSelectable15 : SelectableTextBlock
{
    static BadSelectable15() => J15Fail.Now(nameof(BadSelectable15));
}

/// <summary>Judge 15: a static class with XAML attached accessors over Border's BackgroundProperty whose class constructor fails.</summary>
public static class BadAlias15
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty;

    static BadAlias15() => J15Fail.Now(nameof(BadAlias15));

    public static IBrush? GetBackground(AvaloniaObject target) => target.GetValue(BackgroundProperty);

    public static void SetBackground(AvaloniaObject target, IBrush? value) => target.SetValue(BackgroundProperty, value);
}

/// <summary>Judge 15: a ListBox whose own public FocusableProperty field hides InputElement's with another property; its CLR Focusable is InputElement's.</summary>
public class ShadowList15 : ListBox
{
    public new static readonly StyledProperty<bool> FocusableProperty =
        AvaloniaProperty.Register<ShadowList15, bool>("Focusable");

    protected override Type StyleKeyOverride => typeof(ListBox);
}

/// <summary>
/// Judge 15: ShadowList15 without a StyleKeyOverride, so a ControlTheme that targets it applies (ControlTheme.TryAttach
/// asks TargetType.IsAssignableFrom(target.StyleKey) at 12.1.3).
/// </summary>
public class ShadowList15b : ListBox
{
    public new static readonly StyledProperty<bool> FocusableProperty =
        AvaloniaProperty.Register<ShadowList15b, bool>("Focusable");
}

// Judge 15's cases, aimed at the changes made since judge 14: an owner's namespace (ThemeReader.Owner, OwnershipOf by
// name for every namespace but using: and clr-namespace:), BNXQ1009's IsAutomationProperty filter, the compiler's three
// routes (ElementTypes.OwnerPropertyOn and its failure), the field a setter's owner names (PublicPropertyOf against
// the in-chain shortcut), and Local's two spellings. A case ending in "c" is a control.
internal static partial class Cases
{
    private const string J15Namespaces =
        "xmlns:u=\"https://judge15.example/alt\" xmlns:a15=\"clr-namespace:FpProbe6.Alt15;assembly=FpProbe6\" "
        + "xmlns:au=\"using:FpProbe6.Alt15\" xmlns:an=\"clr-namespace:FpProbe6.Alt15\" xmlns:av=\"https://github.com/avaloniaui\" xmlns:p=";

    /// <summary>Markup whose root also declares judge 15's prefixes u, a15, au, an and av.</summary>
    private static string With15(string markup) => markup.Replace("xmlns:p=", J15Namespaces);

    /// <summary>A list whose plain ItemsSource attribute is followed by <paramref name="attributes"/>.</summary>
    private static string Also15(string markup, string attributes) =>
        markup.Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" " + attributes);

    /// <summary>The list's theme U5List (ListBox's, BasedOn the framework's) with <paramref name="setters"/>, and N21c's unfocusable rows.</summary>
    private static string ListTheme15(string setters, string extra = "") =>
        UnfocusableRows14(extra + "<ControlTheme x:Key=\"U5List\" TargetType=\"ListBox\" BasedOn=\"{StaticResource {x:Type ListBox}}\">"
                          + setters + "</ControlTheme>");

    private const string UnfocusableList15 = "<Setter Property=\"Focusable\" Value=\"False\"/>";
    private const string FalseValue15 = "<sys:Boolean x:Key=\"U5False\">False</sys:Boolean>";

    /// <summary>A list of <paramref name="type"/> whose theme U5S1List (BasedOn ListBox's) has <paramref name="setters"/>, with N21c's unfocusable rows and the F5 key binding.</summary>
    private static string ShadowListMarkup15(string setters, string type = "ShadowList15") => """
        <p:TYPE NAMESPACES
                 Width="300" Height="200" ItemsSource="{Binding Rows}" Theme="{DynamicResource U5S1List}">
          <p:TYPE.Resources>
            IMPLICIT
            <ControlTheme x:Key="U5S1List" TargetType="p:TYPE" BasedOn="{StaticResource {x:Type ListBox}}">SETTERS</ControlTheme>
          </p:TYPE.Resources>
          <p:TYPE.KeyBindings><KeyBinding Gesture="F5" Command="{x:Static p:Diag13Commands.Count}"/></p:TYPE.KeyBindings>
          <p:TYPE.ItemTemplate><DataTemplate x:DataType="p:Row">PLAIN</DataTemplate></p:TYPE.ItemTemplate>
        </p:TYPE>
        """.Replace("NAMESPACES", Namespaces)
            .Replace("IMPLICIT", ImplicitTheme("<Setter Property=\"Focusable\" Value=\"False\"/>" + BorderTemplate()))
            .Replace("SETTERS", setters.Replace("TYPE", type))
            .Replace("PLAIN", Plain13)
            .Replace("TYPE", type);

    /// <summary>A row theme {x:Null} whose template is a Border taking the row's Background, with a nested style that paints the rows while disabled.</summary>
    private const string DisabledPaintsTheme15 =
        "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\"><Setter Property=\"Background\" Value=\"{x:Null}\"/>"
        + "<Setter Property=\"Template\"><ControlTemplate><Border Background=\"{TemplateBinding Background}\">" + Presenter + "</Border></ControlTemplate></Setter>"
        + "<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"
        + "</ControlTheme></ListBox.ItemContainerTheme>";

    public static readonly Case[] AllJ15 =
    [
        // ── X: an owner in a consumer's URI namespace (XmlnsDefinition), whose local name is a framework type's ──
        C("U5X1", "row theme Background Transparent, Border takes it; then a setter u:TemplatedControl.Background {x:Null} (a consumer's attached property, URI namespace)",
            With15(List(RowTheme7Transparent("<Setter Property=\"u:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U5X1b", "the same setter with a binding, {Binding Missing15}",
            With15(List(RowTheme7Transparent("<Setter Property=\"u:TemplatedControl.Background\" Value=\"{Binding Missing15}\"/>"), Plain13))),
        C("U5X1a", "the same setter written a15:TemplatedControl.Background (clr-namespace with assembly; control)",
            With15(List(RowTheme7Transparent("<Setter Property=\"a15:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U5X1u", "the same setter written au:TemplatedControl.Background (using:; control)",
            With15(List(RowTheme7Transparent("<Setter Property=\"au:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U5X1n", "the same setter written an:TemplatedControl.Background (clr-namespace without assembly; control)",
            With15(List(RowTheme7Transparent("<Setter Property=\"an:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U5X1v", "the same setter written av:TemplatedControl.Background (the framework's URI with a prefix; control: the row's own)",
            With15(List(RowTheme7Transparent("<Setter Property=\"av:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U5X1c", "the same setter written TemplatedControl.Background (unprefixed; control: the row's own)",
            With15(List(RowTheme7Transparent("<Setter Property=\"TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U5X2", "row theme {x:Null}; template <Panel u:Panel.Background=\"Red\"> (a consumer's attached property, URI namespace)",
            With15(Element14("{x:Null}", "<Panel u:Panel.Background=\"Red\">", "</Panel>"))),
        C("U5X2b", "row theme {x:Null}; template <Panel u:Panel.Background=\"{TemplateBinding Background}\">",
            With15(Element14("{x:Null}", "<Panel u:Panel.Background=\"{TemplateBinding Background}\">", "</Panel>"))),
        C("U5X2d", "row theme {x:Null}; template <Panel u:Panel.Background=\"{DynamicResource U4Red}\">",
            With15(Element14("{x:Null}", "<Panel u:Panel.Background=\"{DynamicResource U4Red}\">", "</Panel>"))),
        C("U5X2a", "row theme {x:Null}; template <Panel a15:Panel.Background=\"Red\"> (clr-namespace; control)",
            With15(Element14("{x:Null}", "<Panel a15:Panel.Background=\"Red\">", "</Panel>"))),
        C("U5X2c", "row theme {x:Null}; template <Panel Panel.Background=\"{TemplateBinding Background}\"> (control: the Panel's own)",
            With15(Element14("{x:Null}", "<Panel Panel.Background=\"{TemplateBinding Background}\">", "</Panel>"))),
        C("U5X3", "row theme Transparent; template <Border Background=\"{TemplateBinding u:TemplatedControl.Background}\">",
            With15(Element14("Transparent", "<Border Background=\"{TemplateBinding u:TemplatedControl.Background}\">", "</Border>"))),
        C("U5X3n", "the same with row theme {x:Null}",
            With15(Element14("{x:Null}", "<Border Background=\"{TemplateBinding u:TemplatedControl.Background}\">", "</Border>"))),
        C("U5X3p", "row theme Transparent; template Border bound to the templated parent's (u:TemplatedControl.Background) by a path",
            With15(Element14("Transparent", "<Border><Border.Background><Binding Path=\"(u:TemplatedControl.Background)\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Border.Background>", "</Border>"))),
        C("U5X3c", "row theme Transparent; template <Border Background=\"{TemplateBinding TemplatedControl.Background}\"> (control: the row's own)",
            With15(Element14("Transparent", "<Border Background=\"{TemplateBinding TemplatedControl.Background}\">", "</Border>"))),

        // ── X4: BNXQ1009's AutomationProperties filter, IsAutomationProperty, against a consumer's AutomationProperties ──
        C("U5X4", "BNXQ1009: row theme sets u:AutomationProperties.Name {Binding Label} only (a consumer's attached Name, URI namespace); root names nothing, x:DataType p:Row",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14("<Setter Property=\"u:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U5X4a", "the same written a15:AutomationProperties.Name (clr-namespace with assembly)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14("<Setter Property=\"a15:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U5X4r", "BNXQ1009: row theme sets AutomationProperties.Name {Binding Label}, then u:AutomationProperties.Name {x:Null}",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(NameByLabel + "<Setter Property=\"u:AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),
        C("U5X4ra", "the same written a15:AutomationProperties.Name {x:Null}",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(NameByLabel + "<Setter Property=\"a15:AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),
        C("U5X4c", "row theme sets AutomationProperties.Name {Binding Label} alone (control)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(NameByLabel), UnnamedRoot))),
        C("U5X4n", "row theme sets nothing that names (control: the finding)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(string.Empty), UnnamedRoot))),

        // ── X5: BNXQ1009's HasText through Local, an owner in a URI namespace on the item template's root ──
        C("U5X5", "BNXQ1009: item template root <TextBlock u:TextBlock.Text=\"Named\"/> (a consumer's attached Text, URI namespace)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock u:TextBlock.Text=\"Named\" HorizontalAlignment=\"Left\"/>"))),
        C("U5X5b", "the same with a binding, u:TextBlock.Text=\"{Binding Label}\"",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock u:TextBlock.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>"))),
        C("U5X5a", "the same written a15:TextBlock.Text=\"Named\" (clr-namespace; control)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock a15:TextBlock.Text=\"Named\" HorizontalAlignment=\"Left\"/>"))),

        // ── X6: BNXQ1005's Local on the list, an owner in a URI namespace ──
        C("U5X6", "BNXQ1005: the list's theme sets Focusable False, rows Focusable False, F5 key binding; the list written u:InputElement.Focusable=\"True\" (a consumer's accessors, URI namespace)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\" u:InputElement.Focusable=\"True\"", ListTheme15(UnfocusableList15), Plain13))),
        C("U5X6b", "the same written u:Control.Focusable=\"{DynamicResource U4True}\" (a consumer's attached property, which takes a binding)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\" u:Control.Focusable=\"{DynamicResource U4True}\"", ListTheme15(UnfocusableList15, TrueValue14), Plain13))),
        C("U5X6c", "the same list with neither (control: the finding)",
            With15(TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\"", ListTheme15(UnfocusableList15), Plain13))),

        // ── B: an owner outside the chain whose class constructor fails in the rule's process, never run by the markup ──
        C("U5B1", "row theme {x:Null}; template <Panel p:BadBorder15.Background=\"{TemplateBinding Background}\">, BadBorder15 : Border with a failing class constructor",
            Element14("{x:Null}", "<Panel p:BadBorder15.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U5B1t", "the same with row theme Transparent",
            Element14("Transparent", "<Panel p:BadBorder15.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U5B1d", "row theme Transparent; template <Panel p:BadBorder15.Background=\"{DynamicResource U4Red}\">",
            Element14("Transparent", "<Panel p:BadBorder15.Background=\"{DynamicResource U4Red}\">", "</Panel>")),
        C("U5B1l", "row theme Transparent; template <Panel p:BadBorder15.Background=\"Red\"> (a literal: the instance-attached route does not load it)",
            Element14("Transparent", "<Panel p:BadBorder15.Background=\"Red\">", "</Panel>")),
        C("U5B1c", "row theme {x:Null}; template <Panel Border.Background=\"{TemplateBinding Background}\"> (control: Border reads)",
            Element14("{x:Null}", "<Panel Border.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U5B2", "BNXQ1005: the list's theme sets Focusable False, rows Focusable False, F5; the list written p:BadButton15.Focusable=\"{DynamicResource U4True}\"",
            TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\" p:BadButton15.Focusable=\"{DynamicResource U4True}\"", ListTheme15(UnfocusableList15, TrueValue14), Plain13)),
        C("U5B2b", "the same written p:BadButton15.Focusable=\"{Binding Source={x:Static p:J15Values.Yes}}\"",
            TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\" p:BadButton15.Focusable=\"{Binding Source={x:Static p:J15Values.Yes}}\"", ListTheme15(UnfocusableList15), Plain13)),
        C("U5B2l", "the same written p:BadButton15.Focusable=\"True\" (a literal: does not load)",
            TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\" p:BadButton15.Focusable=\"True\"", ListTheme15(UnfocusableList15), Plain13)),
        C("U5B2c", "the same written Button.Focusable=\"{DynamicResource U4True}\" (control: Button reads)",
            TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U5List}\" Button.Focusable=\"{DynamicResource U4True}\"", ListTheme15(UnfocusableList15, TrueValue14), Plain13)),
        C("U5B3", "BNXQ1009: item template root <TextBlock p:BadSelectable15.Text=\"{Binding Label}\"/>, x:DataType p:Row",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock p:BadSelectable15.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>")),
        C("U5B3c", "the same written SelectableTextBlock.Text=\"{Binding Label}\" (control)",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock SelectableTextBlock.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>")),
        C("U5B4", "BNXQ1010 start state: row theme {x:Null}, Border takes it, ^:disabled paints Transparent; the list written p:BadButton15.IsEnabled=\"{DynamicResource U5False}\"",
            Also15(List("<ListBox.Resources>" + FalseValue15 + "</ListBox.Resources>" + DisabledPaintsTheme15, Plain13), "p:BadButton15.IsEnabled=\"{DynamicResource U5False}\"")),
        C("U5B4c", "the same written Button.IsEnabled=\"{DynamicResource U5False}\" (control)",
            Also15(List("<ListBox.Resources>" + FalseValue15 + "</ListBox.Resources>" + DisabledPaintsTheme15, Plain13), "Button.IsEnabled=\"{DynamicResource U5False}\"")),
        C("U5B4n", "the same list without IsEnabled (control: enabled rows, null)",
            List("<ListBox.Resources>" + FalseValue15 + "</ListBox.Resources>" + DisabledPaintsTheme15, Plain13)),
        C("U5B5", "row theme Transparent; template <Panel p:BadAlias15.Background=\"Red\">, a static class with accessors whose class constructor fails (record)",
            Element14("Transparent", "<Panel p:BadAlias15.Background=\"Red\">", "</Panel>")),

        // ── S: a setter's owner in the control's chain whose most-derived public field holds another property ──
        C("U5S1", "BNXQ1005: a ShadowList15 whose theme sets Focusable True, then p:ShadowList15.Focusable False (ShadowList15's public field: another property); rows Focusable False, F5",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"True\"/><Setter Property=\"p:ShadowList15.Focusable\" Value=\"False\"/>")),
        C("U5S1t", "the same theme with Focusable True alone (control: the list takes focus)",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"True\"/>")),
        C("U5S1c", "the same theme with Focusable False alone (control: the finding)",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"False\"/>")),
        C("U5S2", "BNXQ1005: a ShadowList15b (no StyleKeyOverride) whose theme sets Focusable True, then p:ShadowList15b.Focusable False; rows Focusable False, F5",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"True\"/><Setter Property=\"p:TYPE.Focusable\" Value=\"False\"/>", "ShadowList15b")),
        C("U5S2b", "the same theme with p:ShadowList15b.Focusable given a binding, {Binding Source={x:Static p:J15Values.No}}",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"True\"/><Setter Property=\"p:TYPE.Focusable\" Value=\"{Binding Source={x:Static p:J15Values.No}}\"/>", "ShadowList15b")),
        C("U5S2t", "the same theme with Focusable True alone (control: the list takes focus)",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"True\"/>", "ShadowList15b")),
        C("U5S2c", "the same theme with Focusable False alone (control: the finding)",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"False\"/>", "ShadowList15b")),
        C("U5S2p", "the same theme with Focusable True, then ListBox.Focusable False (control: ListBox's field is InputElement's; a duplicate setter)",
            ShadowListMarkup15("<Setter Property=\"Focusable\" Value=\"True\"/><Setter Property=\"ListBox.Focusable\" Value=\"False\"/>", "ShadowList15b")),

        // ── T: two and three spellings on one element, literal and binding, each order (records: a skip claims nothing) ──
        C("U5T1", "row theme {x:Null}; template <Panel Background=\"Red\" Panel.Background=\"{TemplateBinding Background}\">",
            Element14("{x:Null}", "<Panel Background=\"Red\" Panel.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U5T1r", "the same two attributes in the other order",
            Element14("{x:Null}", "<Panel Panel.Background=\"{TemplateBinding Background}\" Background=\"Red\">", "</Panel>")),
        C("U5T2", "row theme {x:Null}; template <Panel Background=\"Red\" Border.Background=\"{TemplateBinding Background}\"> with <Panel.Background><SolidColorBrush Color=\"Blue\"/></Panel.Background>",
            Element14("{x:Null}", "<Panel Background=\"Red\" Border.Background=\"{TemplateBinding Background}\"><Panel.Background><SolidColorBrush Color=\"Blue\"/></Panel.Background>", "</Panel>")),
        C("U5T3", "BNXQ1005: the list written Focusable=\"False\" InputElement.Focusable=\"{DynamicResource U4True}\", rows Focusable False, F5",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\" InputElement.Focusable=\"{DynamicResource U4True}\"", UnfocusableRows14(TrueValue14), Plain13)),
        C("U5T4", "BNXQ1009: root <TextBlock Text=\"{Binding Label}\"><Run Text=\"x\"/></TextBlock> (a Text beside inline content)",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"><Run Text=\"x\"/></TextBlock>")),

        // ── N: a prefix declared below the root (record: XamlX takes xmlns on the root alone) ──
        C("U5N1", "row theme Transparent; a setter u2:TemplatedControl.Background {x:Null} whose prefix u2 the ControlTheme declares",
            List(RowTheme7Transparent("<Setter Property=\"u2:TemplatedControl.Background\" Value=\"{x:Null}\"/>")
                .Replace("<ControlTheme TargetType=\"ListBoxItem\">", "<ControlTheme TargetType=\"ListBoxItem\" xmlns:u2=\"https://judge15.example/alt\">"), Plain13)),
    ];
}
