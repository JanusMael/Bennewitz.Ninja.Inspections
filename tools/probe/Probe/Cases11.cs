using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace FpProbe6;

// The seventh judge's second batch: siblings of what the first found.

/// <summary>A rectangle whose Fill defaults to Transparent, set in its static constructor.</summary>
public class FilledRect : Rectangle
{
    static FilledRect() => FillProperty.OverrideDefaultValue<FilledRect>(Brushes.Transparent);
}

/// <summary>A rectangle whose own code sets its Fill through the property's setter.</summary>
public class CodeFilledRect : Rectangle
{
    public CodeFilledRect() => Fill = Brushes.Transparent;
}

internal static partial class Cases
{
    private const string PaintedPresenterStyle =
        "<Setter Property=\"ContentTemplate\"><DataTemplate><Border Background=\"Transparent\"><TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Border></DataTemplate></Setter>";

    public static readonly Case[] All11 =
    [
        // ── A fill that a type's default or its code gives ───────────────────────────────────────
        C("Q13", "item Grid: a stretched FilledRect (Fill default Transparent from its static constructor)",
            List(NullStyle, "<Grid><p:FilledRect/>" + Plain + "</Grid>")),
        C("Q13b", "item Grid: a stretched CodeFilledRect (its constructor sets Fill Transparent)",
            List(NullStyle, "<Grid><p:CodeFilledRect/>" + Plain + "</Grid>")),
        C("Q13c", "item Grid: a stretched Rectangle with no Fill (control for Q13)",
            List(NullStyle, "<Grid><Rectangle/>" + Plain + "</Grid>")),

        // ── Opacity and IsEnabled on what takes the row's background ─────────────────────────────
        C("Q14", "row theme Transparent; template Panel: a Border taking it with Opacity 0, beside the presenter",
            List(Theme("<Panel><Border Background=\"{TemplateBinding Background}\" Opacity=\"0\"/>" + Presenter + "</Panel>"), Plain)),
        C("Q17", "row theme Transparent; template Panel: a Border taking it with IsEnabled=False, beside the presenter",
            List(Theme("<Panel><Border Background=\"{TemplateBinding Background}\" IsEnabled=\"False\"/>" + Presenter + "</Panel>"), Plain)),

        // ── A list style that reaches into the row template and gives its presenter a template ───
        C("Q15", "row Background null; list style 'ListBoxItem:not(.nope) /template/ ContentPresenter' sets a painted ContentTemplate",
            List(RowTheme(string.Empty) + "<ListBox.Styles><Style Selector=\"ListBoxItem:not(.nope) /template/ ContentPresenter#PART_ContentPresenter\">"
                 + PaintedPresenterStyle + "</Style></ListBox.Styles>", Plain)),
        C("Q15c", "the same with 'ListBoxItem /template/ ContentPresenter', no activator (control for Q15)",
            List(RowTheme(string.Empty) + "<ListBox.Styles><Style Selector=\"ListBoxItem /template/ ContentPresenter#PART_ContentPresenter\">"
                 + PaintedPresenterStyle + "</Style></ListBox.Styles>", Plain)),
        C("Q16", "row Background null; the row theme's nested '^:not(.nope) /template/ ContentPresenter' sets a painted ContentTemplate",
            List(RowTheme("<Style Selector=\"^:not(.nope) /template/ ContentPresenter#PART_ContentPresenter\">" + PaintedPresenterStyle + "</Style>"), Plain)),
    ];
}
