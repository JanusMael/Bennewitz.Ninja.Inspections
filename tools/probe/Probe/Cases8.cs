using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace FpProbe6;

// Round 7's cases for BNXQ1010, beside probe6's and the sixth judge's, which run unchanged. Each "R" case
// measures a claim round 7 makes; a case ending in "c" is its control.

/// <summary>A consumer's templated control whose MinWidth defaults to 600, set in its static constructor.</summary>
public sealed class WideChip : TemplatedControl
{
    static WideChip() => MinWidthProperty.OverrideDefaultValue<WideChip>(600);
}

/// <summary>A control with no template and no background that fills its bounds in its own Render.</summary>
public sealed class FillRender : Control
{
    public override void Render(DrawingContext context) => context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
}

internal static partial class Cases
{
    /// <summary>A row theme whose Background is null and whose template is <paramref name="template"/>.</summary>
    private static string NullRowTemplate(string template) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Template"><ControlTemplate>TEMPLATE</ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("TEMPLATE", template);

    public static readonly Case[] All8 =
    [
        // ── What presents a row's item: the row template's hosts ─────────────────────────────────
        C("R01", "row template presents no content (a 30 px Border taking the row's null Background); item template paints",
            List(NullRowTemplate("<Border Height=\"30\" Background=\"{TemplateBinding Background}\"/>"), Painted)),
        C("R01c", "the same with a presenter binding Content and ContentTemplate (control for R01)",
            List(NullRowTemplate("<Border Height=\"30\" Background=\"{TemplateBinding Background}\">" + Presenter + "</Border>"), Painted)),
        C("R02", "row presenter Content={Binding} (the row's data context), ContentTemplate bound; item template paints",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background}\"><ContentPresenter Content=\"{Binding}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"/></Border>"), Painted)),
        C("R03", "row presenter's ContentTemplate is a painted DataTemplate of its own; plain item template",
            List(NullRowTemplate("<Border Background=\"{TemplateBinding Background}\"><ContentPresenter Content=\"{TemplateBinding Content}\"><ContentPresenter.ContentTemplate>"
                                 + PaintedAny + "</ContentPresenter.ContentTemplate></ContentPresenter></Border>"), Plain)),
        C("R04", "item template DataType p:Vm (no match) plain; list DataTemplate p:Row paints",
            List(NullStyle + ListTemplates(PaintedFor("p:Row", "{Binding Label}")), Plain).Replace("<DataTemplate>" + Plain, "<DataTemplate DataType=\"p:Vm\">" + Plain)),
        C("R04c", "item template with no DataType, plain; list DataTemplate p:Row paints (control for R04)",
            List(NullStyle + ListTemplates(PaintedFor("p:Row", "{Binding Label}")), Plain)),

        // ── A theme that reaches an element that is not templated ────────────────────────────────
        C("R06", "item Grid holds an empty Border; an implicit Border theme in the list's resources sets Transparent",
            List(NullStyle + ResourceTheme("{x:Type Border}", "Border", "<Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<Grid><Border/>" + Label + "</Grid>")),
        C("R06c", "item Grid holds an empty Border, no theme (control for R06)",
            List(NullStyle, "<Grid><Border/>" + Label + "</Grid>")),

        // ── A width the type registers ───────────────────────────────────────────────────────────
        C("R07", "item Grid: Left WideChip (MinWidth 600 by default, from its static constructor), theme sets Transparent",
            List(NullStyle + ThemeFor("p:WideChip", "<Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<Grid><p:WideChip HorizontalAlignment=\"Left\"/>" + Plain + "</Grid>")),

        // ── Layout that carries an element past its slot ─────────────────────────────────────────
        C("R08", "item WrapPanel ItemWidth=600 holds only an empty Transparent Border 30 high",
            List(NullStyle, "<WrapPanel ItemWidth=\"600\"><Border Height=\"30\" Background=\"Transparent\"/></WrapPanel>")),
        C("R08c", "item WrapPanel with no ItemWidth, the same (control for R08)",
            List(NullStyle, "<WrapPanel><Border Height=\"30\" Background=\"Transparent\"/></WrapPanel>")),
        C("R09", "item Grid 16,*: a Transparent Border in the 16 px column with Margin 0,0,-400,0",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Background=\"Transparent\" Margin=\"0,0,-400,0\"/>"
                            + "<TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Grid>")),
        C("R09c", "item Grid 16,*: the same Border with no margin (control for R09)",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Background=\"Transparent\"/>"
                            + "<TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Grid>")),
        C("R10", "item Grid: a 10 px Left Transparent Border scaled 40 times from its left edge",
            List(NullStyle, "<Grid><Border Width=\"10\" HorizontalAlignment=\"Left\" Background=\"Transparent\" RenderTransformOrigin=\"0%,0%\" RenderTransform=\"scale(40, 1)\"/>"
                            + Plain + "</Grid>")),
        C("R10c", "item Grid: the same Border, not scaled (control for R10)",
            List(NullStyle, "<Grid><Border Width=\"10\" HorizontalAlignment=\"Left\" Background=\"Transparent\"/>" + Plain + "</Grid>")),

        C("R12", "item Grid: a Left StackPanel holding a Transparent Border Width=600 Height=20",
            List(NullStyle, "<Grid><StackPanel HorizontalAlignment=\"Left\"><Border Width=\"600\" Height=\"20\" Background=\"Transparent\"/></StackPanel>" + Plain + "</Grid>")),
        C("R12c", "item Grid: the same with the Border Width=10 (control for R12)",
            List(NullStyle, "<Grid><StackPanel HorizontalAlignment=\"Left\"><Border Width=\"10\" Height=\"20\" Background=\"Transparent\"/></StackPanel>" + Plain + "</Grid>")),
        C("R13", "item Grid 16,*: a Panel in the 16 px column holding a Left Transparent Border Width=600 Height=20",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Panel><Border Width=\"600\" Height=\"20\" HorizontalAlignment=\"Left\" Background=\"Transparent\"/></Panel>"
                            + "<TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Grid>")),

        // ── What a presenter's own template, and the row theme's, outrank ────────────────────────
        C("R03c", "row presenter takes the row's Background, its own ContentTemplate is plain; item template paints",
            List(NullRowTemplate("<ContentPresenter Background=\"{TemplateBinding Background}\" Content=\"{TemplateBinding Content}\"><ContentPresenter.ContentTemplate>"
                                 + "<DataTemplate>" + Plain + "</DataTemplate></ContentPresenter.ContentTemplate></ContentPresenter>"), Painted)),
        C("R14", "row theme sets a plain ContentTemplate; item template paints",
            List(RowTheme("<Setter Property=\"ContentTemplate\"><DataTemplate>" + Plain + "</DataTemplate></Setter>"), Painted)),
        C("R16", "item Grid: Chip Theme={x:Null}, its implicit theme sets Transparent and binds it",
            List(NullStyle + ChipTheme("<Setter Property=\"Background\" Value=\"Transparent\"/>"), "<Grid><p:Chip Theme=\"{x:Null}\"/>" + Plain + "</Grid>")),
        C("R17", "item Grid holds p:KeyedBorder (style key Border); Grid.Styles p|KeyedBorder sets Transparent",
            List(NullStyle, "<Grid><Grid.Styles><Style Selector=\"p|KeyedBorder\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></Grid.Styles>"
                            + "<p:KeyedBorder/>" + Plain + "</Grid>")),

        // ── What a view's own markup, or a control's own template, holds ─────────────────────────
        C("R18", "item Grid: a Left WideView, whose own markup holds a Transparent Border 600 wide",
            List(NullStyle, "<Grid><p:WideView HorizontalAlignment=\"Left\"/>" + Plain + "</Grid>"), ("WideView.axaml", SourceView("WideView.axaml"))),
        C("R19", "item Grid: a Left Chip whose implicit theme's template holds a Transparent Border 600 wide",
            List(NullStyle + """
                <ListBox.Resources>
                  <ControlTheme x:Key="{x:Type p:Chip}" TargetType="p:Chip">
                    <Setter Property="Template"><ControlTemplate><Border Width="600" Height="20" Background="Transparent"/></ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.Resources>
                """, "<Grid><p:Chip HorizontalAlignment=\"Left\"/>" + Plain + "</Grid>")),
        C("R20", "row content shown by a ContentControl a style gives a template that draws a Transparent Border; plain item template",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Border Background="{TemplateBinding Background}">
                        <Border.Styles><Style Selector="ContentControl.cell"><Setter Property="Template"><ControlTemplate>
                          <Border Background="Transparent"><ContentPresenter Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/></Border>
                        </ControlTemplate></Setter></Style></Border.Styles>
                        <ContentControl Classes="cell" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/>
                      </Border>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                """, Plain)),

        C("R20b", "the same, with the ContentControl taking the row's Background and its holder taking none",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Border>
                        <Border.Styles><Style Selector="ContentControl.cell"><Setter Property="Template"><ControlTemplate>
                          <Border Background="Transparent"><ContentPresenter Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/></Border>
                        </ControlTemplate></Setter></Style></Border.Styles>
                        <ContentControl Classes="cell" Background="{TemplateBinding Background}" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/>
                      </Border>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                """, Plain)),

        C("R21", "item Grid: a Left Border with Padding 0,0,-400,0 holding a Transparent Border 20 high",
            List(NullStyle, "<Grid><Border HorizontalAlignment=\"Left\" Padding=\"0,0,-400,0\"><Border Height=\"20\" Background=\"Transparent\"/></Border>" + Plain + "</Grid>")),
        C("R21c", "item Grid: the same with no padding (control for R21)",
            List(NullStyle, "<Grid><Border HorizontalAlignment=\"Left\"><Border Height=\"20\" Background=\"Transparent\"/></Border>" + Plain + "</Grid>")),

        // ── A control that draws itself ──────────────────────────────────────────────────────────
        C("R11", "item Grid: a stretched FillRender, which fills its bounds Transparent in Render",
            List(NullStyle, "<Grid><p:FillRender/>" + Plain + "</Grid>")),
    ];
}
