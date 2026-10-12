using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FpProbe6;

// The seventh judge's cases for BNXQ1010 round 7, beside every earlier case, which run unchanged. Each "Q"
// case aims at round 7's reasoning or at a sibling of one of its fixes; a case ending in "c" is a control.

/// <summary>Converters a template's binding can name, for a value markup cannot evaluate.</summary>
public static class Converters
{
    /// <summary>Hands on a brush, and Transparent for null.</summary>
    public static readonly IValueConverter NullToTransparent =
        new FuncValueConverter<object?, object?>(value => value ?? Brushes.Transparent);
}

internal static partial class Cases
{
    private const string PaintedCcTemplate =
        "<ControlTemplate><Border Background=\"Transparent\"><ContentPresenter Content=\"{TemplateBinding Content}\"/></Border></ControlTemplate>";

    /// <summary>
    /// The same markup as Program.cs's UnpaintedTheme: a row theme based on nothing that sets no Background,
    /// whose template takes the row's. A method, because a static field in another partial file may not be
    /// initialised yet when All9 is.
    /// </summary>
    private static string UnpaintedRows() => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Template"><ControlTemplate><Border Background="{TemplateBinding Background}">PRESENTER</Border></ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("PRESENTER", Presenter);

    public static readonly Case[] All9 =
    [
        // ── The row template's own binding of the row's Background ───────────────────────────────
        C("Q01", "row Background null; template Border binds it through TemplatedParent with TargetNullValue Transparent",
            List(NullRowTemplate("<Border Background=\"{Binding Background, RelativeSource={RelativeSource TemplatedParent}, TargetNullValue={x:Static Brushes.Transparent}}\">"
                                 + Presenter + "</Border>"), Plain)),
        C("Q01c", "the same binding with no TargetNullValue (control for Q01)",
            List(NullRowTemplate("<Border Background=\"{Binding Background, RelativeSource={RelativeSource TemplatedParent}}\">" + Presenter + "</Border>"), Plain)),
        C("Q02", "row Background null; template Border binds $parent[ListBoxItem].Background with TargetNullValue Transparent",
            List(NullRowTemplate("<Border Background=\"{Binding $parent[ListBoxItem].Background, TargetNullValue={x:Static Brushes.Transparent}}\">"
                                 + Presenter + "</Border>"), Plain)),
        C("Q03", "row Background null; template Border TemplateBinding Background through a converter null->Transparent",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background, Converter={x:Static p:Converters.NullToTransparent}}\">"
                                 + Presenter + "</Border>"), Plain)),

        // ── A template set on the element itself ──────────────────────────────────────────────────
        C("Q04", "item root ContentControl with a local Template whose root Border is Transparent; content a Left label",
            List(NullStyle, "<ContentControl><ContentControl.Template>" + PaintedCcTemplate + "</ContentControl.Template>" + Label + "</ContentControl>")),
        C("Q04b", "item root UserControl with a local Template whose root Border is Transparent; content a Left label",
            List(NullStyle, "<UserControl><UserControl.Template>" + PaintedCcTemplate + "</UserControl.Template>" + Label + "</UserControl>")),
        C("Q04c", "item root ContentControl holding a Left label, no local Template (control for Q04)",
            List(NullStyle, "<ContentControl>" + Label + "</ContentControl>")),
        C("Q04d", "item root ContentControl Template={StaticResource} from the list's resources, root Border Transparent",
            List(NullStyle + "<ListBox.Resources><ControlTemplate x:Key=\"PaintedCc\" TargetType=\"ContentControl\"><Border Background=\"Transparent\"><ContentPresenter Content=\"{TemplateBinding Content}\"/></Border></ControlTemplate></ListBox.Resources>",
                "<ContentControl Template=\"{StaticResource PaintedCc}\">" + Label + "</ContentControl>")),
        C("Q05", "row Background null; the row template hosts the item in a ContentControl with a local painted Template",
            List(NullRowTemplate("<ContentControl Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"><ContentControl.Template><ControlTemplate>"
                                 + "<Border Background=\"Transparent\"><ContentPresenter Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"/></Border>"
                                 + "</ControlTemplate></ContentControl.Template></ContentControl>"), Plain)),
        C("Q05c", "the same ContentControl with no local Template (control for Q05)",
            List(NullRowTemplate("<ContentControl Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"/>"), Plain)),
        C("Q06", "item Grid: a Chip with a local Template drawing a Transparent Border; its implicit theme binds a background nothing sets",
            List(NullStyle + ChipTheme(string.Empty),
                "<Grid><p:Chip><p:Chip.Template><ControlTemplate><Border Background=\"Transparent\"/></ControlTemplate></p:Chip.Template></p:Chip>" + Plain + "</Grid>")),

        // ── A row theme's nested styles in the state rows start in ────────────────────────────────
        C("Q07", "row theme Background null, template binds it; nested ^:not(.nope) sets Transparent",
            List(RowTheme("<Style Selector=\"^:not(.nope)\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),
        C("Q07b", "row theme Background null; nested ^[IsEnabled=True] sets a Template whose root Border is Transparent",
            List(RowTheme("<Style Selector=\"^[IsEnabled=True]\"><Setter Property=\"Template\"><ControlTemplate><Border Background=\"Transparent\">"
                          + Presenter + "</Border></ControlTemplate></Setter></Style>"), Plain)),
        C("Q07c", "row theme Background null; nested ^:not(:selected) sets Transparent",
            List(RowTheme("<Style Selector=\"^:not(:selected)\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),
        C("Q07d", "row theme Background null; nested ^.nope sets Transparent, which no row carries (control for Q07)",
            List(RowTheme("<Style Selector=\"^.nope\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),

        // ── A list style that paints every row, written with a context or a qualifier ─────────────
        C("Q08", "row theme sets no Background (null); list style 'ListBox ListBoxItem' sets Transparent",
            List(UnpaintedRows() +"<ListBox.Styles><Style Selector=\"ListBox ListBoxItem\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ListBox.Styles>", Plain)),
        C("Q08b", "row theme sets no Background (null); list style 'ListBox > ListBoxItem' sets Transparent",
            List(UnpaintedRows() +"<ListBox.Styles><Style Selector=\"ListBox &gt; ListBoxItem\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ListBox.Styles>", Plain)),
        C("Q08c", "row theme sets no Background (null); list style 'ListBoxItem:not(.nope)' sets Transparent",
            List(UnpaintedRows() +"<ListBox.Styles><Style Selector=\"ListBoxItem:not(.nope)\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ListBox.Styles>", Plain)),
        C("Q08d", "row theme sets no Background (null); list style 'ListBoxItem.nope' sets Transparent (control)",
            List(UnpaintedRows() +"<ListBox.Styles><Style Selector=\"ListBoxItem.nope\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ListBox.Styles>", Plain)),

        // ── A layout transform, a render transform's sibling ─────────────────────────────────────
        C("Q09", "item Grid: a Left LayoutTransformControl scaling a content-sized Transparent Border 40 times",
            List(NullStyle, "<Grid><LayoutTransformControl HorizontalAlignment=\"Left\"><LayoutTransformControl.LayoutTransform><ScaleTransform ScaleX=\"40\" ScaleY=\"1\"/></LayoutTransformControl.LayoutTransform>"
                            + "<Border Background=\"Transparent\"><TextBlock Text=\"x\"/></Border></LayoutTransformControl>" + Plain + "</Grid>")),
        C("Q09c", "the same LayoutTransformControl with no transform (control for Q09)",
            List(NullStyle, "<Grid><LayoutTransformControl HorizontalAlignment=\"Left\">"
                            + "<Border Background=\"Transparent\"><TextBlock Text=\"x\"/></Border></LayoutTransformControl>" + Plain + "</Grid>")),

        // ── What takes the row's background but takes no click ───────────────────────────────────
        C("Q10", "row theme Transparent; template Panel: a Border taking it with IsHitTestVisible=False, beside the presenter",
            List(Theme("<Panel><Border Background=\"{TemplateBinding Background}\" IsHitTestVisible=\"False\"/>" + Presenter + "</Panel>"), Plain)),
        C("Q10b", "row theme Transparent; template root Border takes it with IsHitTestVisible=False and holds the presenter",
            List(Theme("<Border Background=\"{TemplateBinding Background}\" IsHitTestVisible=\"False\">" + Presenter + "</Border>"), Plain)),
        C("Q10c", "row theme Transparent; template Panel: a Border taking it with IsVisible=False, beside the presenter",
            List(Theme("<Panel><Border Background=\"{TemplateBinding Background}\" IsVisible=\"False\"/>" + Presenter + "</Panel>"), Plain)),
        C("Q10d", "row theme Transparent; template Panel: a Border taking it, beside the presenter (control for Q10)",
            List(Theme("<Panel><Border Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain)),
        C("Q10e", "row theme null; template Panel: a Transparent Border of its own with IsHitTestVisible=False, beside the presenter",
            List(NullRowTemplate("<Panel><Border Background=\"Transparent\" IsHitTestVisible=\"False\"/>" + Presenter + "</Panel>"), Plain)),

        // ── A literal padding or margin that widens an aligned element ───────────────────────────
        C("Q11", "item Grid: a Left Transparent Border with Padding 0,0,250,0 around the label",
            List(NullStyle, "<Grid><Border HorizontalAlignment=\"Left\" Background=\"Transparent\" Padding=\"0,0,250,0\"><TextBlock Text=\"{Binding Label}\"/></Border></Grid>")),
        C("Q11b", "item Grid: a Left Transparent Border around a label with Margin 0,0,250,0",
            List(NullStyle, "<Grid><Border HorizontalAlignment=\"Left\" Background=\"Transparent\"><TextBlock Text=\"{Binding Label}\" Margin=\"0,0,250,0\"/></Border></Grid>")),

        // ── A presenter given the row's template and not its content ─────────────────────────────
        C("Q12", "row Background null; the template's presenter binds ContentTemplate but not Content; item template paints",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background}\"><ContentPresenter ContentTemplate=\"{TemplateBinding ContentTemplate}\"/></Border>"), Painted)),
        C("Q12c", "the same presenter binding neither (control for Q12)",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background}\"><ContentPresenter/></Border>"), Painted)),
    ];
}
