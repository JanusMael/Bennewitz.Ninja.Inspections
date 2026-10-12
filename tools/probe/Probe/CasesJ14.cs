using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace FpProbe6;

/// <summary>Judge 14: an attached property named Name that is not AutomationProperties'.</summary>
public sealed class Tag14 : AvaloniaObject
{
    public static readonly AttachedProperty<string?> NameProperty =
        AvaloniaProperty.RegisterAttached<Tag14, Control, string?>("Name");

    public static string? GetName(Control control) => control.GetValue(NameProperty);

    public static void SetName(Control control, string? value) => control.SetValue(NameProperty, value);
}

/// <summary>Judge 14: XAML attached accessors, on a static class, over Border's own BackgroundProperty.</summary>
public static class BgAlias14
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty;

    public static IBrush? GetBackground(AvaloniaObject target) => target.GetValue(BackgroundProperty);

    public static void SetBackground(AvaloniaObject target, IBrush? value) => target.SetValue(BackgroundProperty, value);
}

/// <summary>Judge 14: an AvaloniaObject that adds itself as an owner of Border's BackgroundProperty and exposes it through XAML attached accessors, with no instance property.</summary>
public sealed class BgOwner14 : AvaloniaObject
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<BgOwner14>();

    public static IBrush? GetBackground(AvaloniaObject target) => target.GetValue(BackgroundProperty);

    public static void SetBackground(AvaloniaObject target, IBrush? value) => target.SetValue(BackgroundProperty, value);
}

/// <summary>Judge 14: XAML attached accessors, on a static class, over TextBlock's own TextProperty.</summary>
public static class TextAlias14
{
    public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty;

    public static string? GetText(AvaloniaObject target) => target.GetValue(TextProperty);

    public static void SetText(AvaloniaObject target, string? value) => target.SetValue(TextProperty, value);
}

/// <summary>Judge 14: XAML attached accessors, on a static class, over InputElement's own FocusableProperty.</summary>
public static class FocusAlias14
{
    public static readonly StyledProperty<bool> FocusableProperty = InputElement.FocusableProperty;

    public static bool GetFocusable(AvaloniaObject target) => target.GetValue(FocusableProperty);

    public static void SetFocusable(AvaloniaObject target, bool value) => target.SetValue(FocusableProperty, value);
}

/// <summary>Judge 14: a Panel whose own BackgroundProperty field hides Panel's with another property; its CLR Background is still Panel's.</summary>
public class ShadowPanel14 : Panel
{
    public new static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ShadowPanel14, IBrush?>("Background");
}

/// <summary>Judge 14: a Panel whose own protected BackgroundProperty field hides Panel's public one with another property.</summary>
public class ProtectedShadow14 : Panel
{
    protected new static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ProtectedShadow14, IBrush?>("Background");
}

/// <summary>Judge 14: a TextBlock whose own TextProperty field hides TextBlock's with another property; its CLR Text is still TextBlock's.</summary>
public class ShadowTextBlock14 : TextBlock
{
    public new static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ShadowTextBlock14, string?>("Text");
}

// Judge 14's cases, aimed at the changes made since judge 13: property identity (HoldsSameProperty, OwnershipOf),
// ThemeReader.Local's order and Givings, Binds, HasText, BNXQ1009's anyOwner read of AutomationProperties, and the
// owner read without its prefix. A case ending in "c" is a control.
internal static partial class Cases
{
    private const string AltNamespace = "xmlns:alt=\"clr-namespace:FpProbe6.Alt;assembly=FpProbe6\" xmlns:p=";

    /// <summary>Markup that may also name the FpProbe6.Alt namespace with the prefix alt.</summary>
    private static string WithAlt(string markup) => markup.Replace("xmlns:p=", AltNamespace);

    /// <summary>Markup whose root declares the design-time namespace d, ignorable.</summary>
    private static string WithDesign(string markup) => markup.Replace("xmlns:p=",
        "xmlns:d=\"http://schemas.microsoft.com/expression/blend/2008\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" mc:Ignorable=\"d\" xmlns:p=");

    /// <summary>A row theme of the given setters, its template a Border taking the row's Background around the presenter.</summary>
    private static string RowTheme14(string setters, string basedOn = "") =>
        "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\"" + basedOn + ">" + setters
        + "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate() + "</ControlTheme></ListBox.ItemContainerTheme>";

    private const string NameByLabel = "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\"/>";
    private const string TagName = "<Setter Property=\"p:Tag14.Name\" Value=\"tag\"/>";
    private const string RedBrush14 = "<SolidColorBrush x:Key=\"U4Red\" Color=\"Red\"/>";
    private const string TrueValue14 = "<sys:Boolean x:Key=\"U4True\">True</sys:Boolean>";

    /// <summary>N21c's resources: an implicit row theme Focusable False whose template is a Border around the presenter.</summary>
    private static string UnfocusableRows14(string extra = "") =>
        "<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Focusable\" Value=\"False\"/>" + BorderTemplate()) + extra + "</ListBox.Resources>" + KeyBindings13;

    /// <summary>A row theme Background {x:Null} or Transparent whose template is <paramref name="open"/>, the presenter, <paramref name="close"/>, with a red brush in the list's resources.</summary>
    private static string Element14(string background, string open, string close) =>
        List("<ListBox.Resources>" + RedBrush14 + "</ListBox.Resources>" + ElementTheme(background, open, close), Plain13);

    public static readonly Case[] AllJ14 =
    [
        // ── O: BNXQ1009 reads AutomationProperties.Name in the row theme after any owner, then tells the owner apart ──
        C("U4O1", "BNXQ1009: row theme sets AutomationProperties.Name {Binding Label}, then p:Tag14.Name (another attached Name); item template root a StackPanel naming nothing, x:DataType p:Row",
            TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(NameByLabel + TagName), UnnamedRoot)),
        C("U4O1c", "the same without the p:Tag14.Name setter (control)",
            TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(NameByLabel), UnnamedRoot)),
        C("U4O1r", "the same with p:Tag14.Name first and AutomationProperties.Name last",
            TypedList("ItemsSource=\"{Binding Rows}\"", RowTheme14(TagName + NameByLabel), UnnamedRoot)),
        C("U4O2", "BNXQ1009: row theme BasedOn a keyed theme that sets AutomationProperties.Name {Binding Label}; the row theme itself sets p:Tag14.Name",
            TypedList("ItemsSource=\"{Binding Rows}\"",
                "<ListBox.Resources>" + KeyedTheme("U4O2Base", NameByLabel + "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>"
                + "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\" BasedOn=\"{StaticResource U4O2Base}\">" + TagName + "</ControlTheme></ListBox.ItemContainerTheme>",
                UnnamedRoot)),

        // ── D: one element giving its own property twice; ThemeReader.Local answers the first spelling it reads ──
        C("U4D1", "row theme {x:Null}; template <Panel Border.Background=\"{TemplateBinding Background}\" TextBlock.Background=\"{DynamicResource U4Red}\">",
            Element14("{x:Null}", "<Panel Border.Background=\"{TemplateBinding Background}\" TextBlock.Background=\"{DynamicResource U4Red}\">", "</Panel>")),
        C("U4D1r", "the same two attributes in the other order",
            Element14("{x:Null}", "<Panel TextBlock.Background=\"{DynamicResource U4Red}\" Border.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U4D2", "row theme {x:Null}; template <Panel Background=\"Red\" Border.Background=\"{TemplateBinding Background}\">",
            Element14("{x:Null}", "<Panel Background=\"Red\" Border.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U4D2r", "the same two attributes in the other order",
            Element14("{x:Null}", "<Panel Border.Background=\"{TemplateBinding Background}\" Background=\"Red\">", "</Panel>")),
        C("U4D3", "row theme {x:Null}; template <Border Background=\"Red\"> with <Border.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Border.Background>",
            Element14("{x:Null}", "<Border Background=\"Red\"><Border.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Border.Background>", "</Border>")),
        C("U4D4", "BNXQ1009: item template root <TextBlock Text=\"\" SelectableTextBlock.Text=\"{Binding Label}\"/>, x:DataType p:Row",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock Text=\"\" SelectableTextBlock.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>")),
        C("U4D4r", "the same two attributes in the other order",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock SelectableTextBlock.Text=\"{Binding Label}\" Text=\"\" HorizontalAlignment=\"Left\"/>")),
        C("U4D4b", "BNXQ1009: root <TextBlock Text=\"\"> with <SelectableTextBlock.Text><Binding Path=\"Label\"/></SelectableTextBlock.Text>",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock Text=\"\" HorizontalAlignment=\"Left\"><SelectableTextBlock.Text><Binding Path=\"Label\"/></SelectableTextBlock.Text></TextBlock>")),
        C("U4D4p", "BNXQ1009: root <TextBlock Text=\"\"> with <TextBlock.Text><Binding Path=\"Label\"/></TextBlock.Text>",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock Text=\"\" HorizontalAlignment=\"Left\"><TextBlock.Text><Binding Path=\"Label\"/></TextBlock.Text></TextBlock>")),
        C("U4D5", "BNXQ1005: N21c's list (F5 key binding, rows Focusable False) written Focusable=\"False\" InputElement.Focusable=\"True\"",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\" InputElement.Focusable=\"True\"", UnfocusableRows14(), Plain13)),
        C("U4D5r", "the same two attributes in the other order",
            TypedList("ItemsSource=\"{Binding Rows}\" InputElement.Focusable=\"True\" Focusable=\"False\"", UnfocusableRows14(), Plain13)),
        C("U4D5b", "BNXQ1005: the same list written Focusable=\"False\" Button.Focusable=\"{DynamicResource U4True}\", U4True a sys:Boolean True",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\" Button.Focusable=\"{DynamicResource U4True}\"", UnfocusableRows14(TrueValue14), Plain13)),
        C("U4D5c", "the list written Focusable=\"False\" alone (control)",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\"", UnfocusableRows14(), Plain13)),

        // ── A: an owner outside the chain whose field holds the element's property, exposed as an attached property ──
        C("U4A1", "row theme Transparent; template <Panel p:BgAlias14.Background=\"Red\">, a static class whose field holds Border's BackgroundProperty",
            Element14("Transparent", "<Panel p:BgAlias14.Background=\"Red\">", "</Panel>")),
        C("U4A1n", "the same with row theme {x:Null}",
            Element14("{x:Null}", "<Panel p:BgAlias14.Background=\"Red\">", "</Panel>")),
        C("U4A1b", "row theme {x:Null}; template <Panel p:BgAlias14.Background=\"{TemplateBinding Background}\"> (control: a binding)",
            Element14("{x:Null}", "<Panel p:BgAlias14.Background=\"{TemplateBinding Background}\">", "</Panel>")),
        C("U4A2", "BNXQ1009: item template root <TextBlock p:TextAlias14.Text=\"Named\"/>, x:DataType p:Row",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock p:TextAlias14.Text=\"Named\" HorizontalAlignment=\"Left\"/>")),
        C("U4A3", "BNXQ1005: the list's Theme (DynamicResource) sets Focusable False, rows Focusable False, F5 key binding; the list written p:FocusAlias14.Focusable=\"True\"",
            TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U4A3List}\" p:FocusAlias14.Focusable=\"True\"",
                UnfocusableRows14("<ControlTheme x:Key=\"U4A3List\" TargetType=\"ListBox\" BasedOn=\"{StaticResource {x:Type ListBox}}\"><Setter Property=\"Focusable\" Value=\"False\"/></ControlTheme>"), Plain13)),
        C("U4A3c", "the same without p:FocusAlias14.Focusable (control)",
            TypedList("ItemsSource=\"{Binding Rows}\" Theme=\"{DynamicResource U4A3List}\"",
                UnfocusableRows14("<ControlTheme x:Key=\"U4A3List\" TargetType=\"ListBox\" BasedOn=\"{StaticResource {x:Type ListBox}}\"><Setter Property=\"Focusable\" Value=\"False\"/></ControlTheme>"), Plain13)),

        // ── F: an owner whose own field hides the one its CLR property's declaring type holds ──
        C("U4F1", "row theme Transparent; template <Border p:ShadowPanel14.Background=\"{TemplateBinding Background}\">",
            Element14("Transparent", "<Border p:ShadowPanel14.Background=\"{TemplateBinding Background}\">", "</Border>")),
        C("U4F1n", "the same with row theme {x:Null}",
            Element14("{x:Null}", "<Border p:ShadowPanel14.Background=\"{TemplateBinding Background}\">", "</Border>")),
        C("U4F2", "BNXQ1009: item template root <TextBlock p:ShadowTextBlock14.Text=\"{Binding Label}\"/>, x:DataType p:Row",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock p:ShadowTextBlock14.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>")),

        // ── P: an owner whose own protected field hides the public one a setter's or a TemplateBinding's property is read from ──
        C("U4P1", "row theme Background Transparent, Border takes it; then a setter p:ProtectedShadow14.Background {x:Null}",
            List(RowTheme7Transparent("<Setter Property=\"p:ProtectedShadow14.Background\" Value=\"{x:Null}\"/>"), Plain13)),
        C("U4P1c", "the same setter written Panel.Background (control)",
            List(RowTheme7Transparent("<Setter Property=\"Panel.Background\" Value=\"{x:Null}\"/>"), Plain13)),
        C("U4P2", "row theme Transparent; template <Border p:ProtectedShadow14.Background=\"{TemplateBinding Background}\">",
            Element14("Transparent", "<Border p:ProtectedShadow14.Background=\"{TemplateBinding Background}\">", "</Border>")),
        C("U4P3", "row theme {x:Null}; template <Border Background=\"{TemplateBinding p:ProtectedShadow14.Background}\">",
            Element14("{x:Null}", "<Border Background=\"{TemplateBinding p:ProtectedShadow14.Background}\">", "</Border>")),
        C("U4P4", "row theme BasedOn nothing whose only Background setter is p:ProtectedShadow14.Background Transparent; template a Border taking the row's",
            List("<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\"><Setter Property=\"p:ProtectedShadow14.Background\" Value=\"Transparent\"/>"
                 + BorderTemplate() + "</ControlTheme></ListBox.ItemContainerTheme>", Plain13)),
        C("U4P4c", "the same setter written Panel.Background (control)",
            List("<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\"><Setter Property=\"Panel.Background\" Value=\"Transparent\"/>"
                 + BorderTemplate() + "</ControlTheme></ListBox.ItemContainerTheme>", Plain13)),
        C("U4A4", "row theme Transparent; template <Panel p:BgOwner14.Background=\"Red\">, an AvaloniaObject owner of Border's BackgroundProperty with attached accessors",
            Element14("Transparent", "<Panel p:BgOwner14.Background=\"Red\">", "</Panel>")),

        // ── R: an owner read without its prefix, whose local name is a framework type's ──
        C("U4R1", "row theme Background Transparent, Border takes it; then a setter alt:TemplatedControl.Background {x:Null}, a consumer's attached property",
            WithAlt(List(RowTheme7Transparent("<Setter Property=\"alt:TemplatedControl.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U4R2", "the same setter written alt:Panel.Background",
            WithAlt(List(RowTheme7Transparent("<Setter Property=\"alt:Panel.Background\" Value=\"{x:Null}\"/>"), Plain13))),
        C("U4R1c", "the same setter written p:Paint13.Background (control: no framework name)",
            List(RowTheme7Transparent("<Setter Property=\"p:Paint13.Background\" Value=\"{x:Null}\"/>"), Plain13)),

        // ── G: an attribute of another namespace whose local name is the property's ──
        C("U4G1", "row theme {x:Null}; template <Panel d:Background=\"Red\" Background=\"{TemplateBinding Background}\">, d ignorable",
            WithDesign(Element14("{x:Null}", "<Panel d:Background=\"Red\" Background=\"{TemplateBinding Background}\">", "</Panel>"))),

        // ── B: each value kind after an owner outside the chain (records; a skip claims nothing) ──
        C("U4B1", "row theme {x:Null}; template <Panel Border.Background=\"{ReflectionBinding Background, RelativeSource={RelativeSource TemplatedParent}}\">",
            Element14("{x:Null}", "<Panel Border.Background=\"{ReflectionBinding Background, RelativeSource={RelativeSource TemplatedParent}}\">", "</Panel>")),
        C("U4B2", "row theme Transparent; template <Panel Border.Background=\"{StaticResource U4Red}\">",
            Element14("Transparent", "<Panel Border.Background=\"{StaticResource U4Red}\">", "</Panel>")),
        C("U4B3", "row theme Transparent; template <Panel Border.Background=\"{x:Static Brushes.Red}\">",
            Element14("Transparent", "<Panel Border.Background=\"{x:Static Brushes.Red}\">", "</Panel>")),
        C("U4B4", "row theme Transparent; template <Panel Border.Background=\"{x:Null}\">",
            Element14("Transparent", "<Panel Border.Background=\"{x:Null}\">", "</Panel>")),
        C("U4B5", "row theme Transparent; template <Panel> with <Border.Background><SolidColorBrush Color=\"Red\"/></Border.Background>",
            Element14("Transparent", "<Panel><Border.Background><SolidColorBrush Color=\"Red\"/></Border.Background>", "</Panel>")),
    ];

    /// <summary>A row theme Background Transparent, its template a Border taking it around the presenter, with <paramref name="extra"/> after.</summary>
    private static string RowTheme7Transparent(string extra) =>
        "<ListBox.ItemContainerTheme><ControlTheme TargetType=\"ListBoxItem\"><Setter Property=\"Background\" Value=\"Transparent\"/>"
        + BorderTemplate() + extra + "</ControlTheme></ListBox.ItemContainerTheme>";
}
