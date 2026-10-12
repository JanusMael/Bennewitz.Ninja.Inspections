using Avalonia;
using Avalonia.Controls;

namespace FpProbe6;

// Round 8's cases for BNXQ1010, beside every earlier case, which run unchanged. Each "S" case measures a
// claim round 8 makes; a case ending in "c" is its control.

/// <summary>A Border that asks for 600 px of width whatever it holds.</summary>
public sealed class WideMeasure : Border
{
    protected override Size MeasureOverride(Size availableSize) => new(600, base.MeasureOverride(availableSize).Height);
}

/// <summary>A panel as wide as what it holds asks for, which arranges each child 600 px wide.</summary>
public sealed class WideArrange : Panel
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (Control child in Children)
        {
            child.Arrange(new Rect(0, 0, 600, finalSize.Height));
        }

        return finalSize;
    }
}

/// <summary>A list whose own code paints each row it prepares, which markup does not show.</summary>
public sealed class PaintingList : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is ListBoxItem row)
        {
            row.Background = Avalonia.Media.Brushes.Transparent;
        }
    }
}

/// <summary>An attached flag a row theme can bind to each row's item, for a selector to ask for.</summary>
public static class Flags
{
    public static readonly AttachedProperty<bool> OnProperty = AvaloniaProperty.RegisterAttached<Control, bool>("On", typeof(Flags));

    public static bool GetOn(Control control) => control.GetValue(OnProperty);

    public static void SetOn(Control control, bool value) => control.SetValue(OnProperty, value);
}

internal static partial class Cases
{
    /// <summary>A row theme based on nothing, Background Transparent, with <paramref name="extra"/> and the given template.</summary>
    private static string TransparentTheme(string extra, string template) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="Transparent"/>
            EXTRA
            <Setter Property="Template"><ControlTemplate>TEMPLATE</ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("EXTRA", extra).Replace("TEMPLATE", template);

    private static string RootTakingIt() => "<Border Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>";

    private const string PaintedItems =
        "<ItemsControl ItemsSource=\"{Binding Label}\"><ItemsControl.ItemTemplate><DataTemplate>"
        + "<Border Background=\"Transparent\"><TextBlock Text=\"{Binding}\" HorizontalAlignment=\"Left\"/></Border>"
        + "</DataTemplate></ItemsControl.ItemTemplate></ItemsControl>";

    public static readonly Case[] All12 =
    [
        // ── What an element's own type, not its content, makes wide ──────────────────────────────
        C("S01", "item Grid: a Left Viewbox, Height 20, Stretch Fill, scaling a 10 px Transparent Border",
            List(NullStyle, "<Grid><Viewbox HorizontalAlignment=\"Left\" Height=\"20\" Stretch=\"Fill\"><Border Width=\"10\" Height=\"10\" Background=\"Transparent\"/></Viewbox>" + Plain + "</Grid>")),
        C("S01c", "the same Viewbox, Stretch Uniform, which keeps it 20 px wide (control for S01)",
            List(NullStyle, "<Grid><Viewbox HorizontalAlignment=\"Left\" Height=\"20\"><Border Width=\"10\" Height=\"10\" Background=\"Transparent\"/></Viewbox>" + Plain + "</Grid>")),
        C("S02", "item Grid: a Left Path, Fill Transparent, whose geometry is 600 px wide",
            List(NullStyle, "<Grid><Path HorizontalAlignment=\"Left\" Fill=\"Transparent\" Data=\"M0,0 L600,0 L600,20 L0,20 Z\"/>" + Plain + "</Grid>")),
        C("S03", "item Grid: a Left Transparent WideMeasure, whose MeasureOverride asks for 600 px",
            List(NullStyle, "<Grid><p:WideMeasure HorizontalAlignment=\"Left\" Background=\"Transparent\"><TextBlock Text=\"x\"/></p:WideMeasure>" + Plain + "</Grid>")),
        C("S03b", "item Grid: a Left WideArrange, whose ArrangeOverride gives a Transparent Border 600 px",
            List(NullStyle, "<Grid><p:WideArrange HorizontalAlignment=\"Left\"><Border Background=\"Transparent\"/></p:WideArrange>" + Plain + "</Grid>")),

        // ── A template set on the element itself, to nothing ─────────────────────────────────────
        C("S04", "item root ContentControl Template={x:Null} with Background Transparent, holding a Left label",
            List(NullStyle, "<ContentControl Template=\"{x:Null}\" Background=\"Transparent\">" + Label + "</ContentControl>")),

        // ── What hides what draws from the pointer ───────────────────────────────────────────────
        C("S05", "row theme Transparent; template Panel: the Border taking it inside a Panel with IsHitTestVisible=False",
            List(TransparentTheme(string.Empty, "<Panel><Panel IsHitTestVisible=\"False\"><Border Background=\"{TemplateBinding Background}\"/></Panel>" + Presenter + "</Panel>"), Plain)),
        C("S06", "row theme Transparent and IsHitTestVisible False on the rows; root Border takes the Background",
            List(TransparentTheme("<Setter Property=\"IsHitTestVisible\" Value=\"False\"/>", RootTakingIt()), Plain)),
        C("S06b", "row theme Transparent and IsEnabled False on the rows; root Border takes the Background",
            List(TransparentTheme("<Setter Property=\"IsEnabled\" Value=\"False\"/>", RootTakingIt()), Plain)),
        C("S06c", "row theme Transparent; root Border takes the Background (control for S06)",
            List(TransparentTheme(string.Empty, RootTakingIt()), Plain)),
        C("S07", "row theme Transparent; a style in the template sets IsHitTestVisible False on the Border taking it",
            List(TransparentTheme(string.Empty, "<Panel><Panel.Styles><Style Selector=\"Border.bg\"><Setter Property=\"IsHitTestVisible\" Value=\"False\"/></Style></Panel.Styles>"
                                                + "<Border Classes=\"bg\" Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain)),

        // ── Presenters that take the row's template and not its content ──────────────────────────
        C("S08", "row Background null; the template's presenter takes the row's Tag and ContentTemplate; item template paints",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background}\"><ContentPresenter Content=\"{TemplateBinding Tag}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"/></Border>"), Painted)),
        C("S08b", "row Background null; a ContentControl in the template takes only the row's ContentTemplate; item template paints",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background}\"><ContentControl ContentTemplate=\"{TemplateBinding ContentTemplate}\"/></Border>"), Painted)),

        // ── Selectors that hold of every row in the state rows start in, and some that do not ───
        C("S09", "row theme sets no Background (null); list style 'ListBoxItem:not(:selected)' sets Transparent",
            List(UnpaintedRows() + "<ListBox.Styles><Style Selector=\"ListBoxItem:not(:selected)\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ListBox.Styles>", Plain)),
        C("S10", "row theme Transparent, root takes it; nested ^[IsEnabled=True] sets a template that draws nothing",
            List(TransparentTheme("<Style Selector=\"^[IsEnabled=True]\"><Setter Property=\"Template\"><ControlTemplate><Panel>" + Presenter + "</Panel></ControlTemplate></Setter></Style>",
                RootTakingIt()), Plain)),
        C("S11", "row theme Background null; theme binds p:Flags.On to each item's NotFlag; nested ^[(p|Flags.On)=True] sets Transparent",
            List(RowTheme("<Setter Property=\"p:Flags.On\" Value=\"{Binding NotFlag}\"/><Style Selector=\"^[(p|Flags.On)=True]\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),
        C("S11b", "the same bound to each item's Flag, which is true of every item (the declared assumption's limit)",
            List(RowTheme("<Setter Property=\"p:Flags.On\" Value=\"{Binding Flag}\"/><Style Selector=\"^[(p|Flags.On)=True]\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),

        // ── An items control in what the rows present ────────────────────────────────────────────
        C("S12", "item root ItemsControl over the label's characters, whose item template paints",
            List(NullStyle, PaintedItems)),

        // ── A list whose own code paints its rows ────────────────────────────────────────────────
        C("S13", "p:PaintingList, whose PrepareContainerForItemOverride sets each row Transparent; row theme Background null",
            Host("p:PaintingList", RowTheme(string.Empty).Replace("ListBox.ItemContainerTheme", "p:PaintingList.ItemContainerTheme"), Plain)
                .Replace("<ListBox.ItemTemplate>", "<p:PaintingList.ItemTemplate>").Replace("</ListBox.ItemTemplate>", "</p:PaintingList.ItemTemplate>")),
    ];
}
