using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FpProbe6;

/// <summary>Judge 16: an object a binding reads a null brush from.</summary>
public sealed class J16Holder
{
    public IBrush? Brush => null;
}

/// <summary>Judge 16: values a binding's Source can name.</summary>
public static class J16Values
{
    public static J16Holder Holder { get; } = new();
}

/// <summary>Judge 16's class constructor that fails in a scan's process, where J16_CCTOR_OK is unset, and not in the probe's.</summary>
internal static class J16Fail
{
    public static void Now(string type)
    {
        if (Environment.GetEnvironmentVariable("J16_CCTOR_OK") is null)
        {
            throw new InvalidOperationException($"Judge 16: the class constructor of {type} fails.");
        }
    }
}

/// <summary>Judge 16: a ListBoxItem subclass, the one type of its name, which a ListBox's containers are not.</summary>
public class SoloRow16 : ListBoxItem;

/// <summary>Judge 16: a ListBox subclass, the one type of its name, which a ListBox is not.</summary>
public class SoloList16 : ListBox;

/// <summary>Judge 16: a list whose StyleKeyOverride returns ListBox through a field, so its getter holds no typeof.</summary>
public class FieldKeyList16 : ListBox
{
    private static readonly Type Key = typeof(ListBox);

    protected override Type StyleKeyOverride => Key;
}

/// <summary>Judge 16: a list whose StyleKeyOverride returns typeof(ListBox) (control).</summary>
public class TypeofKeyList16 : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);
}

/// <summary>Judge 16: a row whose StyleKeyOverride returns ListBoxItem through a field, so its getter holds no typeof.</summary>
public class FieldKeyRow16 : ListBoxItem
{
    private static readonly Type Key = typeof(ListBoxItem);

    protected override Type StyleKeyOverride => Key;
}

/// <summary>Judge 16: a row whose StyleKeyOverride returns typeof(ListBoxItem) (control).</summary>
public class TypeofKeyRow16 : ListBoxItem
{
    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}

/// <summary>Judge 16: a list, ListBox's style key, whose rows are FieldKeyRow16.</summary>
public class FieldKeyRowList16 : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new FieldKeyRow16();
}

/// <summary>Judge 16: a list, ListBox's style key, whose rows are TypeofKeyRow16 (control).</summary>
public class TypeofKeyRowList16 : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new TypeofKeyRow16();
}

/// <summary>Judge 16: a ListBox subclass whose class constructor fails in a scan's process.</summary>
public class BadList16 : ListBox
{
    static BadList16() => J16Fail.Now(nameof(BadList16));
}

/// <summary>Judge 16: a ListBox subclass that hides InputElement's FocusableProperty with a protected field of another property.</summary>
public class ProtShadowList16 : ListBox
{
    protected new static readonly StyledProperty<bool> FocusableProperty =
        AvaloniaProperty.Register<ProtShadowList16, bool>("Focusable");
}

// Judge 16's cases, aimed at the changes made since judge 15: BNXQ1009's AutomationOwner and NamingSetter (A, I), the
// framework URIs Owner.IsFramework names (B), ThemeReader.Applied (C: a TargetType that does not resolve; E: a style key
// StyleKeyOf does not read; F: a BasedOn theme), OwnershipOf's in-chain reading through ElementTypes.InChain (D, J), and
// an owner whose class constructor fails (G). A case ending in "c", "k", "t" or "p" is a control.
internal static partial class Cases
{
    private const string J16Namespaces =
        "xmlns:w=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:m=\"http://schemas.microsoft.com/dotnet/2021/maui\" "
        + "xmlns:ja=\"clr-namespace:FpProbe6.J16A;assembly=FpProbe6\" xmlns:jb=\"clr-namespace:FpProbe6.J16B;assembly=FpProbe6\" "
        + "xmlns:p16=\"clr-namespace:FpProbe6.Alt16;assembly=FpProbe6\" xmlns:ag=\"using:Avalonia.Automation\" "
        + "xmlns:agc=\"clr-namespace:Avalonia.Automation;assembly=Avalonia.Controls\" xmlns:p=";

    /// <summary>Markup whose root declares judge 15's prefixes and judge 16's: w, m, ja, jb, p16, ag and agc.</summary>
    private static string With16(string markup) => With15(markup).Replace("xmlns:p=", J16Namespaces);

    private const string TypedSource = "ItemsSource=\"{Binding Rows}\"";

    /// <summary>A Style in the list's own Styles with one setter.</summary>
    private static string RowStyle16(string selector, string setter) =>
        "<ListBox.Styles><Style Selector=\"" + selector + "\">" + setter + "</Style></ListBox.Styles>";

    /// <summary>An inline ItemContainerTheme of <paramref name="target"/> with <paramref name="setters"/> and a Border taking the row's Background.</summary>
    private static string ContainerTheme16(string target, string setters, string host = "ListBox") =>
        "<" + host + ".ItemContainerTheme><ControlTheme TargetType=\"" + target + "\">" + setters + BorderTemplate()
        + "</ControlTheme></" + host + ".ItemContainerTheme>";

    /// <summary>An inline ItemContainerTheme of <paramref name="target"/> with <paramref name="setters"/> and no template (BNXQ1009).</summary>
    private static string NamingTheme16(string target, string setters, string host = "ListBox") =>
        "<" + host + ".ItemContainerTheme><ControlTheme TargetType=\"" + target + "\">" + setters
        + "</ControlTheme></" + host + ".ItemContainerTheme>";

    /// <summary>
    /// A host of type <paramref name="host"/> (a qualified element name) with ItemsSource bound, Theme <paramref name="theme"/>
    /// when given, <paramref name="resources"/> in its Resources, the F5 key binding when <paramref name="keys"/>, and an
    /// item template of x:DataType p:Row holding <paramref name="root"/>, after <paramref name="fragment"/>.
    /// </summary>
    private static string Hosted16(string host, string resources, string fragment, string root, string theme = "", bool keys = false) => """
        <HOST NAMESPACES
              Width="300" Height="200" ItemsSource="{Binding Rows}"THEME>
          <HOST.Resources>RESOURCES</HOST.Resources>
          KEYS
          FRAGMENT
          <HOST.ItemTemplate><DataTemplate x:DataType="p:Row">ROOT</DataTemplate></HOST.ItemTemplate>
        </HOST>
        """.Replace("NAMESPACES", Namespaces)
            .Replace("THEME", theme.Length == 0 ? string.Empty : " Theme=\"{DynamicResource " + theme + "}\"")
            .Replace("RESOURCES", resources)
            .Replace("KEYS", keys ? "<HOST.KeyBindings><KeyBinding Gesture=\"F5\" Command=\"{x:Static p:Diag13Commands.Count}\"/></HOST.KeyBindings>" : string.Empty)
            .Replace("FRAGMENT", fragment)
            .Replace("ROOT", root)
            .Replace("HOST", host);

    /// <summary>An implicit row theme, keyed ListBoxItem, Focusable False, its template a Border around the presenter.</summary>
    private static string UnfocusableRowTheme16() =>
        ImplicitTheme("<Setter Property=\"Focusable\" Value=\"False\"/>" + BorderTemplate());

    /// <summary>A keyed list theme of <paramref name="target"/> with <paramref name="setters"/>, BasedOn <paramref name="basedOn"/>.</summary>
    private static string ListTheme16(string key, string target, string setters, string basedOn = "{StaticResource {x:Type ListBox}}") =>
        "<ControlTheme x:Key=\"" + key + "\" TargetType=\"" + target + "\" BasedOn=\"" + basedOn + "\">" + setters + "</ControlTheme>";

    private const string FocusTrue16 = "<Setter Property=\"Focusable\" Value=\"True\"/>";
    private const string FocusTrueBound16 = "<Setter Property=\"Focusable\" Value=\"{Binding Source={x:Static p:J15Values.Yes}}\"/>";
    private const string NullBackground16 = "<Setter Property=\"Background\" Value=\"{x:Null}\"/>";
    private const string NullBoundBackground16 = "<Setter Property=\"Background\" Value=\"{Binding Brush, Source={x:Static p:J16Values.Holder}}\"/>";
    private const string NameLabel16 = "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\"/>";

    /// <summary>A list of <paramref name="host"/> (BNXQ1005) whose Theme U6T is BasedOn U6B (TargetType ListBox, Focusable True), with unfocusable rows and F5.</summary>
    private static string FocusChain16(string host, string target, string topSetters) =>
        Hosted16(host,
            UnfocusableRowTheme16() + ListTheme16("U6B", "ListBox", FocusTrue16) + ListTheme16("U6T", target, topSetters, "{StaticResource U6B}"),
            string.Empty, Plain13, theme: "U6T", keys: true);

    /// <summary>D2: a ContentControlList whose row theme (BasedOn U6RB, Transparent) has <paramref name="setters"/> and <paramref name="template"/>.</summary>
    private static string ShadowRows16(string setters, string template) =>
        Hosted16("p16:ContentControlList",
            "<ControlTheme x:Key=\"U6RB\" TargetType=\"p16:ContentControl\"><Setter Property=\"Background\" Value=\"Transparent\"/></ControlTheme>",
            "<p16:ContentControlList.ItemContainerTheme><ControlTheme TargetType=\"p16:ContentControl\" BasedOn=\"{StaticResource U6RB}\">"
            + setters + "<Setter Property=\"Template\"><ControlTemplate>" + template + "</ControlTemplate></Setter>"
            + "</ControlTheme></p16:ContentControlList.ItemContainerTheme>",
            Plain13);

    private static string BorderAround16(string background) => "<Border Background=\"" + background + "\">" + Presenter + "</Border>";

    public static readonly Case[] AllJ16 =
    [
        // ── A: BNXQ1009, a Style whose selector is the row type alone, with an owner in a consumer's URI (NamingSetter) ──
        C("U6A1", "BNXQ1009: a Style ListBoxItem in the list's Styles sets u:AutomationProperties.Name {Binding Label} (a consumer's Name, URI namespace); root names nothing, x:DataType p:Row",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem", "<Setter Property=\"u:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6A1l", "the same with a literal, Value=\"Named\"",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem", "<Setter Property=\"u:AutomationProperties.Name\" Value=\"Named\"/>"), UnnamedRoot))),
        C("U6A1w", "the same setter written w:AutomationProperties.Name (WPF's presentation URI, mapped by the consumer)",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem", "<Setter Property=\"w:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6A1a", "the same setter written a15:AutomationProperties.Name (clr-namespace; control: another type's)",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem", "<Setter Property=\"a15:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6A1c", "the same setter written AutomationProperties.Name (control: Avalonia's)",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem", NameLabel16), UnnamedRoot))),
        C("U6A1g", "the same setter written ag:AutomationProperties.Name (using:Avalonia.Automation; control: Avalonia's)",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem", "<Setter Property=\"ag:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6A1n", "no style (control: the finding)",
            With16(TypedList(TypedSource, string.Empty, UnnamedRoot))),
        C("U6A2", "the u: setter in a Style ListBoxItem:selected (control: picks some rows, a skip)",
            With16(TypedList(TypedSource, RowStyle16("ListBoxItem:selected", "<Setter Property=\"u:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),

        // ── B: the presentation URIs of WPF and MAUI, mapped by the consumer, where Owner.IsFramework finds by name ──
        C("U6B1", "row theme Background Transparent, Border takes it; then a setter w:TemplatedControl.Background {x:Null} (WPF's URI, the consumer's attached property)",
            With16(List(RowTheme7Transparent("<Setter Property=\"w:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U6B1b", "the same setter with a binding, {Binding Brush, Source={x:Static p:J16Values.Holder}}",
            With16(List(RowTheme7Transparent("<Setter Property=\"w:TemplatedControl.Background\" Value=\"{Binding Brush, Source={x:Static p:J16Values.Holder}}\"/>"), Plain13))),
        C("U6B1m", "the same setter written m:TemplatedControl.Background {x:Null} (MAUI's URI)",
            With16(List(RowTheme7Transparent("<Setter Property=\"m:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U6B2", "row theme {x:Null}; template <Panel w:Panel.Background=\"Red\">",
            With16(Element14("{x:Null}", "<Panel w:Panel.Background=\"Red\">", "</Panel>"))),
        C("U6B2d", "row theme {x:Null}; template <Panel w:Panel.Background=\"{DynamicResource U4Red}\">",
            With16(Element14("{x:Null}", "<Panel w:Panel.Background=\"{DynamicResource U4Red}\">", "</Panel>"))),
        C("U6B2b", "row theme {x:Null}; template <Panel w:Panel.Background=\"{TemplateBinding Background}\">",
            With16(Element14("{x:Null}", "<Panel w:Panel.Background=\"{TemplateBinding Background}\">", "</Panel>"))),
        C("U6B3", "BNXQ1009: item template root <TextBlock w:TextBlock.Text=\"Named\"/>",
            With16(TypedList(TypedSource, string.Empty, "<TextBlock w:TextBlock.Text=\"Named\" HorizontalAlignment=\"Left\"/>"))),
        C("U6B3b", "the same with a binding, w:TextBlock.Text=\"{Binding Label}\"",
            With16(TypedList(TypedSource, string.Empty, "<TextBlock w:TextBlock.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>"))),
        C("U6B4", "BNXQ1009: row theme sets w:AutomationProperties.Name {Binding Label} alone; root names nothing",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"w:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6B4l", "the same with a literal, Value=\"Named\"",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"w:AutomationProperties.Name\" Value=\"Named\"/>"), UnnamedRoot))),
        C("U6B4r", "BNXQ1009: row theme sets AutomationProperties.Name {Binding Label}, then w:AutomationProperties.Name {x:Null}",
            With16(TypedList(TypedSource, RowTheme14(NameLabel16 + "<Setter Property=\"w:AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),
        C("U6B5", "BNXQ1005: the list's theme sets Focusable False, rows Focusable False, F5; the list written w:InputElement.Focusable=\"True\" (the consumer's accessors)",
            With16(TypedList(TypedSource + " Theme=\"{DynamicResource U5List}\" w:InputElement.Focusable=\"True\"", ListTheme15(UnfocusableList15), Plain13))),
        C("U6B5b", "the same written w:InputElement.Focusable=\"{Binding Source={x:Static p:J15Values.Yes}}\"",
            With16(TypedList(TypedSource + " Theme=\"{DynamicResource U5List}\" w:InputElement.Focusable=\"{Binding Source={x:Static p:J15Values.Yes}}\"", ListTheme15(UnfocusableList15), Plain13))),

        // ── C: ThemeReader.Applied, a TargetType that does not resolve (two scanned types carry its name) ──
        C("U6C1", "BNXQ1010: ItemContainerTheme TargetType ja:AmbRow16 (a ListBoxItem subclass whose name two types carry), Background {x:Null}, Border takes it",
            With16(List(ContainerTheme16("ja:AmbRow16", NullBackground16), Plain13))),
        C("U6C1x", "the same written TargetType=\"{x:Type ja:AmbRow16}\"",
            With16(List(ContainerTheme16("{x:Type ja:AmbRow16}", NullBackground16), Plain13))),
        C("U6C1b", "the same with Background {Binding Brush, Source={x:Static p:J16Values.Holder}}",
            With16(List(ContainerTheme16("ja:AmbRow16", NullBoundBackground16), Plain13))),
        C("U6C1t", "control: TargetType p:SoloRow16 (one type of the name, not ListBoxItem's style key or a base)",
            With16(List(ContainerTheme16("p:SoloRow16", NullBackground16), Plain13))),
        C("U6C1k", "control: TargetType ListBoxItem",
            With16(List(ContainerTheme16("ListBoxItem", NullBackground16), Plain13))),
        C("U6C2", "BNXQ1005: the list's Theme U6L, TargetType ja:AmbList16 (BasedOn ListBox's), sets Focusable True; rows Focusable False; F5",
            With16(Hosted16("ListBox", UnfocusableRowTheme16() + ListTheme16("U6L", "ja:AmbList16", FocusTrue16), string.Empty, Plain13, theme: "U6L", keys: true))),
        C("U6C2b", "the same with Focusable {Binding Source={x:Static p:J15Values.Yes}}",
            With16(Hosted16("ListBox", UnfocusableRowTheme16() + ListTheme16("U6L", "ja:AmbList16", FocusTrueBound16), string.Empty, Plain13, theme: "U6L", keys: true))),
        C("U6C2t", "control: TargetType p:SoloList16",
            With16(Hosted16("ListBox", UnfocusableRowTheme16() + ListTheme16("U6L", "p:SoloList16", FocusTrue16), string.Empty, Plain13, theme: "U6L", keys: true))),
        C("U6C3", "BNXQ1009: ItemContainerTheme TargetType ja:AmbRow16 sets AutomationProperties.Name {Binding Label}; root names nothing",
            With16(TypedList(TypedSource, NamingTheme16("ja:AmbRow16", NameLabel16), UnnamedRoot))),
        C("U6C3l", "the same with a literal, Value=\"Named\"",
            With16(TypedList(TypedSource, NamingTheme16("ja:AmbRow16", "<Setter Property=\"AutomationProperties.Name\" Value=\"Named\"/>"), UnnamedRoot))),
        C("U6C3t", "control: TargetType p:SoloRow16",
            With16(TypedList(TypedSource, NamingTheme16("p:SoloRow16", NameLabel16), UnnamedRoot))),

        // ── D: OwnershipOf in a setter, a TemplateBinding or a path: ElementTypes.InChain takes the most-derived type of the name ──
        C("U6D1", "BNXQ1005: a p16:SelectingItemsControl (a ListBox named as its base, hiding Focusable's field) whose Theme, BasedOn U6B (Focusable True), sets SelectingItemsControl.Focusable False; rows Focusable False; F5",
            With16(FocusChain16("p16:SelectingItemsControl", "p16:SelectingItemsControl", "<Setter Property=\"SelectingItemsControl.Focusable\" Value=\"False\"/>"))),
        C("U6D1b", "the same setter with a binding, {Binding Source={x:Static p:J15Values.No}}",
            With16(FocusChain16("p16:SelectingItemsControl", "p16:SelectingItemsControl", "<Setter Property=\"SelectingItemsControl.Focusable\" Value=\"{Binding Source={x:Static p:J15Values.No}}\"/>"))),
        C("U6D1c", "control: the setter written ListBox.Focusable False",
            With16(FocusChain16("p16:SelectingItemsControl", "p16:SelectingItemsControl", "<Setter Property=\"ListBox.Focusable\" Value=\"False\"/>"))),
        C("U6D1p", "control: the setter written p16:SelectingItemsControl.Focusable False (the consumer's own field: another property)",
            With16(FocusChain16("p16:SelectingItemsControl", "p16:SelectingItemsControl", "<Setter Property=\"p16:SelectingItemsControl.Focusable\" Value=\"False\"/>"))),
        C("U6D1n", "control: no Focusable setter in the Theme (the base's True)",
            With16(FocusChain16("p16:SelectingItemsControl", "p16:SelectingItemsControl", string.Empty))),
        C("U6D2", "BNXQ1010: p16:ContentControl rows (a ListBoxItem named as its base, hiding Background's field); row theme BasedOn U6RB (Transparent) sets ContentControl.Background {x:Null}; Border takes {TemplateBinding Background}",
            With16(ShadowRows16("<Setter Property=\"ContentControl.Background\" Value=\"{x:Null}\"/>", BorderAround16("{TemplateBinding Background}")))),
        C("U6D2b", "the same setter with a binding, {Binding Brush, Source={x:Static p:J16Values.Holder}}",
            With16(ShadowRows16("<Setter Property=\"ContentControl.Background\" Value=\"{Binding Brush, Source={x:Static p:J16Values.Holder}}\"/>", BorderAround16("{TemplateBinding Background}")))),
        C("U6D2c", "control: the setter written TemplatedControl.Background {x:Null}",
            With16(ShadowRows16("<Setter Property=\"TemplatedControl.Background\" Value=\"{x:Null}\"/>", BorderAround16("{TemplateBinding Background}")))),
        C("U6D2t", "BNXQ1010: the same rows; row theme sets Background {x:Null}; Border takes {TemplateBinding ContentControl.Background}",
            With16(ShadowRows16(NullBackground16, BorderAround16("{TemplateBinding ContentControl.Background}")))),
        C("U6D2tc", "control: Border takes {TemplateBinding TemplatedControl.Background}",
            With16(ShadowRows16(NullBackground16, BorderAround16("{TemplateBinding TemplatedControl.Background}")))),
        C("U6D2tp", "control: Border takes {TemplateBinding p16:ContentControl.Background} (the consumer's own field: another property)",
            With16(ShadowRows16(NullBackground16, BorderAround16("{TemplateBinding p16:ContentControl.Background}")))),
        C("U6D2pa", "BNXQ1010: the same rows, row theme Background {x:Null}; Border bound to the templated parent's (ContentControl.Background) by a path",
            With16(ShadowRows16(NullBackground16, "<Border><Border.Background><Binding Path=\"(ContentControl.Background)\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Border.Background>" + Presenter + "</Border>"))),

        // ── E: ThemeReader.Applied against ElementTypes.StyleKeyOf, a StyleKeyOverride returned through a field ──
        C("U6E1", "BNXQ1005: a p:FieldKeyList16 (style key ListBox, returned through a field) whose Theme U6L, TargetType p:FieldKeyList16, sets Focusable True; rows Focusable False; F5",
            With16(Hosted16("p:FieldKeyList16", UnfocusableRowTheme16() + ListTheme16("U6L", "p:FieldKeyList16", FocusTrue16), string.Empty, Plain13, theme: "U6L", keys: true))),
        C("U6E1b", "the same with Focusable {Binding Source={x:Static p:J15Values.Yes}}",
            With16(Hosted16("p:FieldKeyList16", UnfocusableRowTheme16() + ListTheme16("U6L", "p:FieldKeyList16", FocusTrueBound16), string.Empty, Plain13, theme: "U6L", keys: true))),
        C("U6E1c", "control: a p:TypeofKeyList16 (style key typeof(ListBox)) with the same theme for its own type",
            With16(Hosted16("p:TypeofKeyList16", UnfocusableRowTheme16() + ListTheme16("U6L", "p:TypeofKeyList16", FocusTrue16), string.Empty, Plain13, theme: "U6L", keys: true))),
        C("U6E2", "BNXQ1010: a p:FieldKeyRowList16, rows p:FieldKeyRow16 (style key ListBoxItem through a field); ItemContainerTheme TargetType p:FieldKeyRow16, Background {x:Null}, Border takes it",
            With16(Hosted16("p:FieldKeyRowList16", string.Empty, ContainerTheme16("p:FieldKeyRow16", NullBackground16, "p:FieldKeyRowList16"), Plain13))),
        C("U6E2b", "the same with Background {Binding Brush, Source={x:Static p:J16Values.Holder}}",
            With16(Hosted16("p:FieldKeyRowList16", string.Empty, ContainerTheme16("p:FieldKeyRow16", NullBoundBackground16, "p:FieldKeyRowList16"), Plain13))),
        C("U6E2c", "control: a p:TypeofKeyRowList16, rows p:TypeofKeyRow16 (typeof), ItemContainerTheme TargetType p:TypeofKeyRow16",
            With16(Hosted16("p:TypeofKeyRowList16", string.Empty, ContainerTheme16("p:TypeofKeyRow16", NullBackground16, "p:TypeofKeyRowList16"), Plain13))),
        C("U6E3", "BNXQ1009: a p:FieldKeyRowList16; ItemContainerTheme TargetType p:FieldKeyRow16 sets AutomationProperties.Name {Binding Label}; root names nothing",
            With16(Hosted16("p:FieldKeyRowList16", string.Empty, NamingTheme16("p:FieldKeyRow16", NameLabel16, "p:FieldKeyRowList16"), UnnamedRoot))),
        C("U6E3l", "the same with a literal, Value=\"Named\"",
            With16(Hosted16("p:FieldKeyRowList16", string.Empty, NamingTheme16("p:FieldKeyRow16", "<Setter Property=\"AutomationProperties.Name\" Value=\"Named\"/>", "p:FieldKeyRowList16"), UnnamedRoot))),
        C("U6E3c", "control: a p:TypeofKeyRowList16 with the same theme for p:TypeofKeyRow16",
            With16(Hosted16("p:TypeofKeyRowList16", string.Empty, NamingTheme16("p:TypeofKeyRow16", NameLabel16, "p:TypeofKeyRowList16"), UnnamedRoot))),

        // ── F: a BasedOn theme whose TargetType is not the style key or a base (record) ──
        C("U6F1", "BNXQ1005: the list's Theme U6T (TargetType ListBox) is BasedOn U6S (TargetType p:SoloList16, BasedOn ListBox's, Focusable True); rows Focusable False; F5",
            With16(Hosted16("ListBox", UnfocusableRowTheme16() + ListTheme16("U6S", "p:SoloList16", FocusTrue16)
                                       + ListTheme16("U6T", "ListBox", "<Setter Property=\"Padding\" Value=\"0\"/>", "{StaticResource U6S}"), string.Empty, Plain13, theme: "U6T", keys: true))),
        C("U6F2", "BNXQ1010: ItemContainerTheme TargetType ListBoxItem BasedOn U6R (TargetType p:SoloRow16, BasedOn ListBoxItem's, Background {x:Null}, Border takes it)",
            With16(Hosted16("ListBox",
                "<ControlTheme x:Key=\"U6R\" TargetType=\"p:SoloRow16\" BasedOn=\"{StaticResource {x:Type ListBoxItem}}\">" + NullBackground16 + BorderTemplate() + "</ControlTheme>",
                "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\" BasedOn=\"{StaticResource U6R}\"><Setter Property=\"Padding\" Value=\"0\"/></ControlTheme></ListBox.ItemContainerTheme>",
                Plain13))),
        C("U6F3", "BNXQ1009: ItemContainerTheme TargetType ListBoxItem BasedOn U6R (TargetType p:SoloRow16, BasedOn ListBoxItem's, AutomationProperties.Name {Binding Label}); root names nothing",
            With16(Hosted16("ListBox",
                "<ControlTheme x:Key=\"U6R\" TargetType=\"p:SoloRow16\" BasedOn=\"{StaticResource {x:Type ListBoxItem}}\">" + NameLabel16 + "</ControlTheme>",
                "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\" BasedOn=\"{StaticResource U6R}\"><Setter Property=\"Padding\" Value=\"0\"/></ControlTheme></ListBox.ItemContainerTheme>",
                UnnamedRoot))),

        // ── G: an owner whose class constructor fails in the scan's process ──
        C("U6G1", "BNXQ1010: row theme sets p:BadBorder15.Background {x:Null} alone (out of the chain; its constructor fails); Border takes it",
            With16(List(ContainerTheme16("ListBoxItem", "<Setter Property=\"p:BadBorder15.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U6G1b", "the same with {Binding Brush, Source={x:Static p:J16Values.Holder}}",
            With16(List(ContainerTheme16("ListBoxItem", "<Setter Property=\"p:BadBorder15.Background\" Value=\"{Binding Brush, Source={x:Static p:J16Values.Holder}}\"/>"), Plain13))),
        C("U6G2", "BNXQ1005: a p:BadList16 (its constructor fails in the scan's process) whose Theme, BasedOn U6B (Focusable True), sets p:BadList16.Focusable False; rows Focusable False; F5",
            With16(FocusChain16("p:BadList16", "p:BadList16", "<Setter Property=\"p:BadList16.Focusable\" Value=\"False\"/>"))),
        C("U6G2f", "the same with the setter written ListBox.Focusable False",
            With16(FocusChain16("p:BadList16", "p:BadList16", "<Setter Property=\"ListBox.Focusable\" Value=\"False\"/>"))),
        C("U6G2l", "BNXQ1005: a ListBox whose Theme U6T, TargetType ListBox BasedOn U6B, sets p:BadButton15.Focusable False (out of the chain; its constructor fails)",
            With16(FocusChain16("ListBox", "ListBox", "<Setter Property=\"p:BadButton15.Focusable\" Value=\"False\"/>"))),
        C("U6G3", "BNXQ1009: row theme sets p:BadButton15.Tag {Binding Label}, then AutomationProperties.Name {Binding Label} (control: Avalonia's names)",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"p:BadButton15.Tag\" Value=\"{Binding Label}\"/>" + NameLabel16), UnnamedRoot))),

        // ── H: an owner whose prefix is declared nowhere (record) ──
        C("U6H1", "row theme Background Transparent, Border takes it; then a setter zz:TemplatedControl.Background {x:Null}, zz declared nowhere",
            With16(List(RowTheme7Transparent("<Setter Property=\"zz:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),

        // ── I: AutomationProperties through each kind of namespace in the row theme, before and after Avalonia's ──
        C("U6I1", "BNXQ1009: row theme sets ag:AutomationProperties.Name {Binding Label} (using:Avalonia.Automation) alone",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"ag:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6I2", "the same written agc:AutomationProperties.Name (clr-namespace:Avalonia.Automation;assembly=Avalonia.Controls)",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"agc:AutomationProperties.Name\" Value=\"{Binding Label}\"/>"), UnnamedRoot))),
        C("U6I3", "row theme sets AutomationProperties.Name {Binding Label}, then a15:AutomationProperties.Name {x:Null}",
            With16(TypedList(TypedSource, RowTheme14(NameLabel16 + "<Setter Property=\"a15:AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),
        C("U6I4", "row theme sets a15:AutomationProperties.Name {Binding Label}, then AutomationProperties.Name {x:Null}",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"a15:AutomationProperties.Name\" Value=\"{Binding Label}\"/><Setter Property=\"AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),
        C("U6I5", "row theme sets u:AutomationProperties.Name {Binding Label}, then AutomationProperties.Name {Binding Label}",
            With16(TypedList(TypedSource, RowTheme14("<Setter Property=\"u:AutomationProperties.Name\" Value=\"{Binding Label}\"/>" + NameLabel16), UnnamedRoot))),
        C("U6I6", "row theme sets AutomationProperties.Name {Binding Label}, then u:AutomationProperties.Name {x:Null}",
            With16(TypedList(TypedSource, RowTheme14(NameLabel16 + "<Setter Property=\"u:AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),
        C("U6I7", "row theme sets AutomationProperties.Name {Binding Label}, then ag:AutomationProperties.Name {x:Null} (record: one property twice)",
            With16(TypedList(TypedSource, RowTheme14(NameLabel16 + "<Setter Property=\"ag:AutomationProperties.Name\" Value=\"{x:Null}\"/>"), UnnamedRoot))),

        // ── J: setters after the control's own type or a base that hides the field, publicly or not ──
        C("U6J1", "BNXQ1005: a p:ProtShadowList16 (hides Focusable's field with a protected one) whose Theme, BasedOn U6B (True), sets p:ProtShadowList16.Focusable False",
            With16(FocusChain16("p:ProtShadowList16", "p:ProtShadowList16", "<Setter Property=\"p:ProtShadowList16.Focusable\" Value=\"False\"/>"))),
        C("U6J2", "BNXQ1005: a p:ShadowList15b (public hiding field) whose Theme, BasedOn U6B (True), sets p:ShadowList15b.Focusable False",
            With16(FocusChain16("p:ShadowList15b", "p:ShadowList15b", "<Setter Property=\"p:ShadowList15b.Focusable\" Value=\"False\"/>"))),
        C("U6J3", "the same list with the setter written ListBox.Focusable False",
            With16(FocusChain16("p:ShadowList15b", "p:ShadowList15b", "<Setter Property=\"ListBox.Focusable\" Value=\"False\"/>"))),
    ];
}
