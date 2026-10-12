using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FpProbe6;

/// <summary>Judge 13: an attached property named Background that is not Border's, Red unless set.</summary>
public sealed class Paint13 : AvaloniaObject
{
    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<Paint13, Control, IBrush?>("Background", Brushes.Red);

    public static IBrush? GetBackground(Control control) => control.GetValue(BackgroundProperty);

    public static void SetBackground(Control control, IBrush? value) => control.SetValue(BackgroundProperty, value);
}

// Judge 13's cases, aimed at the changes made since judge 12: ThemeReader.Local's owner rule (an element's own
// property read after its type or a base, not after another owner), Decide's drop of an ItemsSource written after
// another owner, HasText's owners, and PropertyNamed's and PathProperty's owners. The XAML compiler's
// AvaloniaXamlIlTransformInstanceAttachedProperties takes Owner.Name, where the owner is not a base, as the owner's
// instance property set through its static NameProperty field, and AvaloniaXamlIlAvaloniaPropertyResolver gives that
// property a BindingSetter: a binding, a TemplateBinding or a DynamicResource then binds the field's property on the
// element. A case ending in "c" is a control; "b" is the property-element spelling, which the rule read before the
// change; "x" holds its list in a view compiled at build time.
internal static partial class Cases
{
    private const string Plain13 = "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>";

    /// <summary>A list whose plain ItemsSource attribute is replaced by <paramref name="source"/> (an attribute, or nothing).</summary>
    private static string SourcedBy(string markup, string source) =>
        markup.Replace("ItemsSource=\"{Binding Rows}\"", source);

    /// <summary>A row theme Background Transparent whose template is the presenter alone, which draws nothing.</summary>
    private static string PresenterOnly() => TransparentTheme(string.Empty, Presenter);

    /// <summary>A list whose item template declares x:DataType p:Row and holds <paramref name="root"/>.</summary>
    private static string TypedList(string attributes, string fragment, string root) => """
        <ListBox NAMESPACES
                 Width="300" Height="200" ATTRIBUTES>
          FRAGMENT
          <ListBox.ItemTemplate><DataTemplate x:DataType="p:Row">ROOT</DataTemplate></ListBox.ItemTemplate>
        </ListBox>
        """.Replace("NAMESPACES", Namespaces).Replace("ATTRIBUTES", attributes).Replace("FRAGMENT", fragment).Replace("ROOT", root);

    private const string UnnamedRoot = "<StackPanel><TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></StackPanel>";

    /// <summary>A row theme template: a Border that takes the row's Background around the presenter.</summary>
    private static string BorderTemplate() =>
        "<Setter Property=\"Template\"><ControlTemplate><Border Background=\"{TemplateBinding Background}\">" + Presenter + "</Border></ControlTemplate></Setter>";

    private static string KeyedTheme(string key, string setters) =>
        "<ControlTheme x:Key=\"" + key + "\" TargetType=\"ListBoxItem\">" + setters + "</ControlTheme>";

    private static string ImplicitTheme(string setters) =>
        "<ControlTheme x:Key=\"{x:Type ListBoxItem}\" TargetType=\"ListBoxItem\">" + setters + "</ControlTheme>";

    private const string KeyBindings13 =
        "<ListBox.KeyBindings><KeyBinding Gesture=\"F5\" Command=\"{x:Static p:Diag13Commands.Count}\"/></ListBox.KeyBindings>";

    /// <summary>A row template element written as given around the presenter, in a row theme whose Background is <paramref name="background"/>.</summary>
    private static string ElementTheme(string background, string open, string close) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="BACKGROUND"/>
            <Setter Property="Template"><ControlTemplate>OPENPRESENTERCLOSE</ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("BACKGROUND", background).Replace("OPEN", open).Replace("PRESENTER", Presenter).Replace("CLOSE", close);

    public static readonly Case[] AllJ13 =
    [
        // ── A: ItemsSource written after an owner that is not a base (Decide's drop) ────────────────────
        C("N01", "row theme Transparent, template the presenter alone (draws nothing); the list's source written ComboBox.ItemsSource=\"{Binding Rows}\"",
            SourcedBy(List(PresenterOnly(), Plain13), "ComboBox.ItemsSource=\"{Binding Rows}\"")),
        C("N01t", "the same written TreeView.ItemsSource=\"{Binding Rows}\"",
            SourcedBy(List(PresenterOnly(), Plain13), "TreeView.ItemsSource=\"{Binding Rows}\"")),
        C("N01c", "the same written ItemsSource=\"{Binding Rows}\" (control)",
            List(PresenterOnly(), Plain13)),
        C("N02b", "the same, the source a property element <ComboBox.ItemsSource><Binding Path=\"Rows\"/></ComboBox.ItemsSource>",
            SourcedBy(List(PresenterOnly() + "<ComboBox.ItemsSource><Binding Path=\"Rows\"/></ComboBox.ItemsSource>", Plain13), string.Empty)),
        C("N03x", "N01's list in N13SourceView, compiled at build time",
            ViewRoot("N13SourceView"), ("N13SourceView.axaml", SourceView("N13SourceView.axaml"))),
        C("N04", "BNXQ1009: item template x:DataType p:Row, root a StackPanel naming nothing; the source written ComboBox.ItemsSource=\"{Binding Rows}\"",
            TypedList("ComboBox.ItemsSource=\"{Binding Rows}\"", string.Empty, UnnamedRoot)),
        C("N04c", "the same written ItemsSource=\"{Binding Rows}\" (control)",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, UnnamedRoot)),
        C("N05b", "N04 with the source a property element <ComboBox.ItemsSource><Binding Path=\"Rows\"/></ComboBox.ItemsSource>",
            TypedList(string.Empty, "<ComboBox.ItemsSource><Binding Path=\"Rows\"/></ComboBox.ItemsSource>", UnnamedRoot)),

        // ── B: Background written after an owner that is not a base, in the rows' template (ThemeReader.Local) ──
        C("N10", "row theme Transparent; template <Panel Border.Background=\"{TemplateBinding Background}\"> around the presenter",
            List(ElementTheme("Transparent", "<Panel Border.Background=\"{TemplateBinding Background}\">", "</Panel>"), Plain13)),
        C("N10c", "the same Panel written Background=\"{TemplateBinding Background}\" (control)",
            List(ElementTheme("Transparent", "<Panel Background=\"{TemplateBinding Background}\">", "</Panel>"), Plain13)),
        C("N11", "row theme {x:Null}; template <Panel Border.Background=\"{TemplateBinding Background}\"> around the presenter",
            List(ElementTheme("{x:Null}", "<Panel Border.Background=\"{TemplateBinding Background}\">", "</Panel>"), Plain13)),
        C("N12b", "row theme Transparent; template Panel given <Border.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Border.Background>",
            List(ElementTheme("Transparent",
                "<Panel><Border.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Border.Background>", "</Panel>"), Plain13)),
        C("N12c", "the same written <Panel.Background>, its own type (control)",
            List(ElementTheme("Transparent",
                "<Panel><Panel.Background><Binding Path=\"Background\" RelativeSource=\"{RelativeSource TemplatedParent}\"/></Panel.Background>", "</Panel>"), Plain13)),
        C("N13", "row theme Transparent; template <Border Panel.Background=\"{TemplateBinding Background}\"> around the presenter",
            List(ElementTheme("Transparent", "<Border Panel.Background=\"{TemplateBinding Background}\">", "</Border>"), Plain13)),
        C("N14", "row theme Transparent; template the presenter alone, written Border.Background=\"{TemplateBinding Background}\"",
            List(ElementTheme("Transparent", string.Empty, string.Empty).Replace(
                "Name=\"PART_ContentPresenter\"", "Name=\"PART_ContentPresenter\" Border.Background=\"{TemplateBinding Background}\""), Plain13)),
        C("N15", "row theme Transparent; template <Panel Border.Background=\"{DynamicResource N15Brush}\">, N15Brush Red in the list's resources",
            List("<ListBox.Resources><SolidColorBrush x:Key=\"N15Brush\" Color=\"Red\"/></ListBox.Resources>"
                 + ElementTheme("Transparent", "<Panel Border.Background=\"{DynamicResource N15Brush}\">", "</Panel>"), Plain13)),
        C("N16", "row theme Transparent; template <Panel Border.Background=\"Red\">, a literal (the remark's measurement)",
            List(ElementTheme("Transparent", "<Panel Border.Background=\"Red\">", "</Panel>"), Plain13)),
        C("N17", "row theme Transparent; template <Panel TemplatedControl.Background=\"{TemplateBinding Background}\">",
            List(ElementTheme("Transparent", "<Panel TemplatedControl.Background=\"{TemplateBinding Background}\">", "</Panel>"), Plain13)),
        C("N18x", "N10's list in N13PanelView, compiled at build time",
            ViewRoot("N13PanelView"), ("N13PanelView.axaml", SourceView("N13PanelView.axaml"))),

        // ── C: ItemContainerTheme written after an owner that is not a base (ThemeReader.ItemContainerThemeOf) ──
        C("N20", "implicit row theme {x:Null}, Border takes it; the list written ComboBox.ItemContainerTheme=\"{DynamicResource N20Painted}\": Transparent, Border takes it",
            SourcedBy(List("<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Background\" Value=\"{x:Null}\"/>" + BorderTemplate())
                           + KeyedTheme("N20Painted", "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>", Plain13),
                "ItemsSource=\"{Binding Rows}\" ComboBox.ItemContainerTheme=\"{DynamicResource N20Painted}\"")),
        C("N20c", "the same without the ComboBox.ItemContainerTheme (control)",
            List("<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Background\" Value=\"{x:Null}\"/>" + BorderTemplate())
                 + KeyedTheme("N20Painted", "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>", Plain13)),
        C("N20b", "N20 with the theme a property element <ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"N20Painted\"/></ComboBox.ItemContainerTheme>",
            List("<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Background\" Value=\"{x:Null}\"/>" + BorderTemplate())
                 + KeyedTheme("N20Painted", "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>"
                 + "<ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"N20Painted\"/></ComboBox.ItemContainerTheme>", Plain13)),
        C("N21", "BNXQ1005: an F5 key binding; list Focusable False; implicit row theme Focusable False; the list written ComboBox.ItemContainerTheme=\"{DynamicResource N21Focusable}\", Focusable True",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\" ComboBox.ItemContainerTheme=\"{DynamicResource N21Focusable}\"",
                "<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Focusable\" Value=\"False\"/>" + BorderTemplate())
                + KeyedTheme("N21Focusable", "<Setter Property=\"Focusable\" Value=\"True\"/>" + BorderTemplate()) + "</ListBox.Resources>" + KeyBindings13, Plain13)),
        C("N21c", "the same without the ComboBox.ItemContainerTheme (control)",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\"",
                "<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Focusable\" Value=\"False\"/>" + BorderTemplate())
                + KeyedTheme("N21Focusable", "<Setter Property=\"Focusable\" Value=\"True\"/>" + BorderTemplate()) + "</ListBox.Resources>" + KeyBindings13, Plain13)),
        C("N21b", "N21 with the theme a property element <ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"N21Focusable\"/></ComboBox.ItemContainerTheme>",
            TypedList("ItemsSource=\"{Binding Rows}\" Focusable=\"False\"",
                "<ListBox.Resources>" + ImplicitTheme("<Setter Property=\"Focusable\" Value=\"False\"/>" + BorderTemplate())
                + KeyedTheme("N21Focusable", "<Setter Property=\"Focusable\" Value=\"True\"/>" + BorderTemplate()) + "</ListBox.Resources>" + KeyBindings13
                + "<ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"N21Focusable\"/></ComboBox.ItemContainerTheme>", Plain13)),
        C("N22", "BNXQ1009: item template root a StackPanel naming nothing; the list written ComboBox.ItemContainerTheme=\"{DynamicResource N22Named}\", which names each row by its Label",
            TypedList("ItemsSource=\"{Binding Rows}\" ComboBox.ItemContainerTheme=\"{DynamicResource N22Named}\"",
                "<ListBox.Resources>" + KeyedTheme("N22Named", "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\"/>"
                    + "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>", UnnamedRoot)),
        C("N22c", "the same without the ComboBox.ItemContainerTheme (control)",
            TypedList("ItemsSource=\"{Binding Rows}\"",
                "<ListBox.Resources>" + KeyedTheme("N22Named", "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\"/>"
                    + "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>", UnnamedRoot)),
        C("N22b", "N22 with the theme a property element <ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"N22Named\"/></ComboBox.ItemContainerTheme>",
            TypedList("ItemsSource=\"{Binding Rows}\"",
                "<ListBox.Resources>" + KeyedTheme("N22Named", "<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding Label}\"/>"
                    + "<Setter Property=\"Background\" Value=\"Transparent\"/>" + BorderTemplate()) + "</ListBox.Resources>"
                + "<ComboBox.ItemContainerTheme><DynamicResource ResourceKey=\"N22Named\"/></ComboBox.ItemContainerTheme>", UnnamedRoot)),

        // ── D: HasText's owners (BNXQ1009) ─────────────────────────────────────────────────────────────
        C("N23", "BNXQ1009: item template root <TextBlock SelectableTextBlock.Text=\"{Binding Label}\"/>, x:DataType p:Row",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock SelectableTextBlock.Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>")),
        C("N23c", "the same written Text=\"{Binding Label}\" (control)",
            TypedList("ItemsSource=\"{Binding Rows}\"", string.Empty, "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>")),

        // ── E: PropertyNamed and PathProperty after an owner whose Background is not Border's ──────────
        C("N30", "row theme {x:Null}; template Border Background=\"{TemplateBinding p:Paint13.Background}\", an attached Background, Red by default",
            List(ElementTheme("{x:Null}", "<Border Background=\"{TemplateBinding p:Paint13.Background}\">", "</Border>"), Plain13)),
        C("N30c", "the same written {TemplateBinding Border.Background} (control: the remark's case)",
            List(ElementTheme("{x:Null}", "<Border Background=\"{TemplateBinding Border.Background}\">", "</Border>"), Plain13)),
        C("N31", "row theme {x:Null}; template Border Background=\"{Binding (p:Paint13.Background), RelativeSource={RelativeSource TemplatedParent}}\"",
            List(ElementTheme("{x:Null}", "<Border Background=\"{Binding (p:Paint13.Background), RelativeSource={RelativeSource TemplatedParent}}\">", "</Border>"), Plain13)),
        C("N31c", "the same written (Border.Background) (control)",
            List(ElementTheme("{x:Null}", "<Border Background=\"{Binding (Border.Background), RelativeSource={RelativeSource TemplatedParent}}\">", "</Border>"), Plain13)),
        C("N32", "row theme {x:Null}; template Border Background=\"{Binding $parent[ListBoxItem].(p:Paint13.Background)}\"",
            List(ElementTheme("{x:Null}", "<Border Background=\"{Binding $parent[ListBoxItem].(p:Paint13.Background)}\">", "</Border>"), Plain13)),

        // ── F: SetterProperty's parentheses and Setter.Property element, read by ThemeReader.SetsProperty ──
        C("N40", "row theme {x:Null}, Border takes it; one more setter (p:Paint13.Background) Transparent, the attached property, in parentheses",
            List(RowTheme("<Setter Property=\"(p:Paint13.Background)\" Value=\"Transparent\"/>"), Plain13)),
        C("N40c", "the same setter written p:Paint13.Background, no parentheses (read alike before the change)",
            List(RowTheme("<Setter Property=\"p:Paint13.Background\" Value=\"Transparent\"/>"), Plain13)),
        C("N41", "the same setter naming p:Paint13.Background in a Setter.Property element",
            List(RowTheme("<Setter Value=\"Transparent\"><Setter.Property>p:Paint13.Background</Setter.Property></Setter>"), Plain13)),

        // ── G: outside the changes since judge 12 (OwnedBy, F2's): a selection after another owner, bound ─────
        C("N50", "EL02's list (row theme Transparent, presenter drawing nothing, nested '^:selected' paints it Red); the list written ComboBox.SelectedIndex=\"{Binding Zero}\"",
            ListWith(SelectedPartList(string.Empty), "ComboBox.SelectedIndex=\"{Binding Zero}\"")),
        C("N50c", "the same written SelectedIndex=\"{Binding Zero}\" (control)",
            ListWith(SelectedPartList(string.Empty), "SelectedIndex=\"{Binding Zero}\"")),
    ];
}
