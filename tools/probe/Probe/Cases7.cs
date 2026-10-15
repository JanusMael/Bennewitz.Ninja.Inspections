using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace FpProbe6;

// The sixth judge's cases for BNXQ1010 round 6, added beside probe6's (which run unchanged). Each "Z" case
// aims at round 6's new reasoning or at a sibling of a round-5 defect that round 6 did not reach.

/// <summary>An enum, for content that is an enum value.</summary>
public enum Kind
{
    One,
    Two,
}

/// <summary>Static members, for content given by an x:Static.</summary>
public static class Statics
{
    public static string Text => "row";

    public static Kind KindOne => Kind.One;
}

/// <summary>A Border that borrows Border's style key, so a Border selector styles it, as StyleKeyOverride does for any control.</summary>
public class KeyedBorder : Border
{
    protected override Type StyleKeyOverride => typeof(Border);
}

/// <summary>Code that paints a control from outside its type: an extension method in a helper class.</summary>
public static class PaintHelpers
{
    public static void PaintTransparent(this TemplatedControl control) => control.Background = Brushes.Transparent;
}

/// <summary>A view whose constructor paints it through a helper in another class; its markup paints nothing.</summary>
public sealed class HelperView : UserControl
{
    public HelperView()
    {
        Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
        this.PaintTransparent();
    }
}

/// <summary>A view whose base, in the referenced library, sets its Background in a handler that captures a local.</summary>
public sealed class DerivedLambdaView : ProbeLib.LibLambdaBase
{
    public DerivedLambdaView() => Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
}

/// <summary>A view whose generic base, in the referenced library, sets its Background in its constructor.</summary>
public sealed class GenericDerivedView : ProbeLib.LibGenericBase<string>
{
    public GenericDerivedView() => Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
}

/// <summary>Walks the running application's styles and prints every style setter that may paint or size.</summary>
internal static class FrameworkStyles
{
    private static readonly HashSet<string> Wanted = new(StringComparer.Ordinal)
    {
        "Background", "Fill", "Width", "MinWidth", "Template", "ContentTemplate",
    };

    private static int _visited;

    public static void Dump()
    {
        int count = 0;
        foreach (IStyle style in Application.Current!.Styles)
        {
            Console.WriteLine($"top-level {style.GetType().FullName}");
            count += Walk(style, 0);
        }

        Console.WriteLine($"styles visited: {_visited}, setters printed: {count}");
    }

    private static int Walk(IStyle style, int depth)
    {
        int printed = 0;
        _visited++;
        switch (style)
        {
            case Style s:
                foreach (SetterBase setter in s.Setters)
                {
                    if (setter is Setter { Property: { } property } plain && Wanted.Contains(property.Name))
                    {
                        Console.WriteLine($"  style [{s.Selector}] sets {property.Name} = {Describe(plain.Value)}");
                        printed++;
                    }
                }

                foreach (IStyle child in s.Children)
                {
                    printed += Walk(child, depth + 1);
                }

                break;
            case Styles group:
                foreach (IStyle child in group)
                {
                    printed += Walk(child, depth + 1);
                }

                break;
            default:
                if (style.GetType().GetProperty("Loaded")?.GetValue(style) is IStyle loaded)
                {
                    printed += Walk(loaded, depth + 1);
                }

                break;
        }

        return printed;
    }

    private static string Describe(object? value) => value switch
    {
        null => "null",
        ISolidColorBrush brush => "brush " + brush.Color,
        _ => value.GetType().Name,
    };
}

internal static partial class Cases
{
    /// <summary>probe6's cases, then this judge's.</summary>
    public static Case[] Every => [.. All, .. All7, .. All8, .. All9, .. All10, .. All11, .. All12, .. AllJ9, .. AllJ9c, .. AllR10, .. AllJ10, .. AllR11, .. AllJ11, .. AllJ12, .. AllJ13, .. AllJ14, .. AllJ15, .. AllJ16, .. AllIS];

    private const string Label = "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>";

    private static string ListTemplates(string templates) => "<ListBox.DataTemplates>" + templates + "</ListBox.DataTemplates>";

    private static string PaintedFor(string type, string text = "{Binding}") =>
        "<DataTemplate DataType=\"" + type + "\"><Border Background=\"Transparent\"><TextBlock Text=\"" + text + "\" HorizontalAlignment=\"Left\"/></Border></DataTemplate>";

    private const string PaintedAny =
        "<DataTemplate><Border Background=\"Transparent\"><TextBlock Text=\"painted\" HorizontalAlignment=\"Left\"/></Border></DataTemplate>";

    /// <summary>A ControlTheme in the list's resources: implicit for a type, or keyed.</summary>
    private static string ResourceTheme(string key, string type, string setters) =>
        "<ListBox.Resources><ControlTheme x:Key=\"" + key + "\" TargetType=\"" + type + "\">" + setters + "</ControlTheme></ListBox.Resources>";

    /// <summary>A row theme whose Background is null, whose template's root takes it, and whose presenter binds the item template.</summary>
    private static string RowTheme(string extraSetters) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Template"><ControlTemplate><Border Background="{TemplateBinding Background}">PRESENTER</Border></ControlTemplate></Setter>
            EXTRA
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("PRESENTER", Presenter).Replace("EXTRA", extraSetters);

    private const string PaintedContentTemplateSetter =
        "<Setter Property=\"ContentTemplate\">" + PaintedAny + "</Setter>";

    private static string ChipTheme(string setters) => ThemeFor("p:Chip", setters);

    public static readonly Case[] All7 =
    [
        // ── Content that is data (F3): what round 6 now skips, and what it still decides ─────────
        C("Z01", "ContentControl Content={x:Static p:Statics.Text}; list DataTemplate sys:String paints",
            List(NullStyle + ListTemplates(PaintedFor("sys:String")), "<ContentControl Content=\"{x:Static p:Statics.Text}\"/>")),
        C("Z02", "ContentControl Content={x:Static p:Statics.Text}; no data template",
            List(NullStyle, "<ContentControl Content=\"{x:Static p:Statics.Text}\"/>")),
        C("Z03", "ContentControl Content={x:Static p:Kind.One}; list DataTemplate p:Kind paints",
            List(NullStyle + ListTemplates(PaintedFor("p:Kind")), "<ContentControl Content=\"{x:Static p:Kind.One}\"/>")),
        C("Z04", "ContentControl Content={x:Static p:Kind.One}; no data template",
            List(NullStyle, "<ContentControl Content=\"{x:Static p:Kind.One}\"/>")),
        C("Z05", "ContentControl holding <p:Kind>One</p:Kind>; list DataTemplate p:Kind paints",
            List(NullStyle + ListTemplates(PaintedFor("p:Kind")), "<ContentControl><p:Kind>One</p:Kind></ContentControl>")),
        C("Z06", "ContentPresenter Content=\"row\"; list DataTemplate sys:String paints",
            List(NullStyle + ListTemplates(PaintedFor("sys:String")), "<ContentPresenter Content=\"row\"/>")),
        C("Z07", "ContentPresenter Content=\"row\"; no data template",
            List(NullStyle, "<ContentPresenter Content=\"row\"/>")),
        C("Z08", "ContentControl Content=\"row\"; list DataTemplate for p:Vm only (no match) paints",
            List(NullStyle + PaintedVmTemplates, "<ContentControl Content=\"row\"/>")),
        C("Z09", "ContentControl holding a TextBlock; list DataTemplate with no DataType paints",
            List(NullStyle + ListTemplates(PaintedAny), "<ContentControl><TextBlock Text=\"x\" HorizontalAlignment=\"Left\"/></ContentControl>")),
        C("Z10", "ContentControl holding <sys:Int32>5</sys:Int32>; list DataTemplate sys:Int32 paints",
            List(NullStyle + ListTemplates(PaintedFor("sys:Int32")), "<ContentControl><sys:Int32>5</sys:Int32></ContentControl>")),
        C("Z11", "ContentControl Content=\"row\" in a StackPanel; list DataTemplate sys:String paints",
            List(NullStyle + ListTemplates(PaintedFor("sys:String")), "<StackPanel><ContentControl Content=\"row\"/>" + Label + "</StackPanel>")),

        // ── A theme that gives a content control its ContentTemplate ─────────────────────────────
        C("Z12", "ContentControl Content=\"row\"; implicit ContentControl theme sets a painted ContentTemplate",
            List(NullStyle + ResourceTheme("{x:Type ContentControl}", "ContentControl", PaintedContentTemplateSetter),
                "<ContentControl Content=\"row\"/>")),
        C("Z13", "ContentControl Content=\"row\" Theme={StaticResource Painted}: theme sets a painted ContentTemplate",
            List(NullStyle + ResourceTheme("Painted", "ContentControl", PaintedContentTemplateSetter),
                "<ContentControl Theme=\"{StaticResource Painted}\" Content=\"row\"/>")),
        C("Z14", "ContentControl holding a TextBlock; implicit theme sets a painted ContentTemplate",
            List(NullStyle + ResourceTheme("{x:Type ContentControl}", "ContentControl", PaintedContentTemplateSetter),
                "<ContentControl><TextBlock Text=\"x\" HorizontalAlignment=\"Left\"/></ContentControl>")),
        C("Z12b", "ContentControl Content=\"row\"; implicit theme's nested ^ style sets a painted ContentTemplate",
            List(NullStyle + ResourceTheme("{x:Type ContentControl}", "ContentControl", "<Style Selector=\"^\">" + PaintedContentTemplateSetter + "</Style>"),
                "<ContentControl Content=\"row\"/>")),
        C("Z15", "ContentControl Content=\"row\"; implicit theme with no ContentTemplate (control for Z12)",
            List(NullStyle + ResourceTheme("{x:Type ContentControl}", "ContentControl", "<Setter Property=\"Tag\" Value=\"t\"/>"),
                "<ContentControl Content=\"row\"/>")),

        // ── The rows' own theme gives them a ContentTemplate ─────────────────────────────────────
        new Case("Z16", "no item template; the row theme sets a painted ContentTemplate",
            NoItemTemplate("ListBox", RowTheme(PaintedContentTemplateSetter)), []),
        C("Z17", "plain item template; the row theme sets a painted ContentTemplate",
            List(RowTheme(PaintedContentTemplateSetter), Plain)),
        new Case("Z18", "no item template; the same row theme without ContentTemplate (control for Z16)",
            NoItemTemplate("ListBox", RowTheme(string.Empty)), []),
        C("Z17c", "plain item template; the same row theme without ContentTemplate (control for Z17)",
            List(RowTheme(string.Empty), Plain)),
        new Case("Z16b", "no item template; the implicit ListBoxItem theme in the list's resources sets a painted ContentTemplate",
            NoItemTemplate("ListBox", ResourceTheme("{x:Type ListBoxItem}", "ListBoxItem",
                "<Setter Property=\"Background\" Value=\"{x:Null}\"/><Setter Property=\"Template\"><ControlTemplate><Border Background=\"{TemplateBinding Background}\">"
                + Presenter + "</Border></ControlTemplate></Setter>" + PaintedContentTemplateSetter)), []),

        // ── Width, dock side and grid column that a theme or a style sets ───────────────────────
        C("Z19", "item Grid: Left Chip whose implicit theme sets MinWidth=600 and Transparent",
            List(NullStyle + ChipTheme("<Setter Property=\"MinWidth\" Value=\"600\"/><Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<Grid><p:Chip HorizontalAlignment=\"Left\"/>" + Plain + "</Grid>")),
        C("Z20", "item Grid: Left Chip whose implicit theme sets Width=600 and Transparent",
            List(NullStyle + ChipTheme("<Setter Property=\"Width\" Value=\"600\"/><Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<Grid><p:Chip HorizontalAlignment=\"Left\"/>" + Plain + "</Grid>")),
        C("Z19b", "Grid 16,*: Chip in the 16 px column whose implicit theme sets MinWidth=600 and Transparent",
            List(NullStyle + ChipTheme("<Setter Property=\"MinWidth\" Value=\"600\"/><Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<Grid ColumnDefinitions=\"16,*\"><p:Chip Grid.Column=\"0\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Grid>")),
        C("Z21", "item Grid: Left Chip whose theme sets Transparent, no width (control for Z19)",
            List(NullStyle + ChipTheme("<Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<Grid><p:Chip HorizontalAlignment=\"Left\"/>" + Plain + "</Grid>")),
        C("Z22", "item StackPanel Horizontal: Chip whose theme sets MinWidth=600 and Transparent",
            List(NullStyle + ChipTheme("<Setter Property=\"MinWidth\" Value=\"600\"/><Setter Property=\"Background\" Value=\"Transparent\"/>"),
                "<StackPanel Orientation=\"Horizontal\"><p:Chip/>" + Plain + "</StackPanel>")),
        C("Z23", "DockPanel: first child Border.top, a style in DockPanel.Styles docks it Top, Height 60",
            List(NullStyle, "<DockPanel><DockPanel.Styles><Style Selector=\"Border.top\"><Setter Property=\"DockPanel.Dock\" Value=\"Top\"/></Style></DockPanel.Styles>"
                            + "<Border Classes=\"top\" Height=\"60\" Background=\"Transparent\"/><TextBlock Text=\"{Binding Label}\"/></DockPanel>")),
        C("Z24", "Grid 16,*: Border.wide placed in column 1 by a style in Grid.Styles",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Grid.Styles><Style Selector=\"Border.wide\"><Setter Property=\"Grid.Column\" Value=\"1\"/></Style></Grid.Styles>"
                            + "<Border Classes=\"wide\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Grid>")),
        C("Z25", "Grid 16,*: Border.wide spanning both columns by a style in Grid.Styles",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Grid.Styles><Style Selector=\"Border.wide\"><Setter Property=\"Grid.ColumnSpan\" Value=\"2\"/></Style></Grid.Styles>"
                            + "<Border Classes=\"wide\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Grid>")),
        C("Z26", "item Grid: Left Border Width=10, a style sets Width=600 (local wins)",
            List(NullStyle, "<Grid><Grid.Styles><Style Selector=\"Border.w\"><Setter Property=\"Width\" Value=\"600\"/></Style></Grid.Styles>"
                            + "<Border Classes=\"w\" Width=\"10\" HorizontalAlignment=\"Left\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("Z27", "item Grid: Left Border Width=10, a style sets MinWidth=600",
            List(NullStyle, "<Grid><Grid.Styles><Style Selector=\"Border.w\"><Setter Property=\"MinWidth\" Value=\"600\"/></Style></Grid.Styles>"
                            + "<Border Classes=\"w\" Width=\"10\" HorizontalAlignment=\"Left\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("Z28", "nested docks: inner dock docked Top by a property element holds a Transparent Border",
            List(NullStyle, "<DockPanel><DockPanel><DockPanel.Dock>Top</DockPanel.Dock><Border Height=\"40\" Background=\"Transparent\"/></DockPanel>"
                            + "<TextBlock Text=\"{Binding Label}\"/></DockPanel>")),
        C("Z29", "nested docks: inner dock docked Left by a property element holds a Transparent Border",
            List(NullStyle, "<DockPanel><DockPanel><DockPanel.Dock>Left</DockPanel.Dock><Border Width=\"20\" Background=\"Transparent\"/></DockPanel>"
                            + "<TextBlock Text=\"{Binding Label}\"/></DockPanel>")),

        // ── A style key a control borrows ─────────────────────────────────────────────────────────
        C("Z30", "item Grid holds p:KeyedBorder (style key Border); Grid.Styles Border sets Transparent",
            List(NullStyle, "<Grid><Grid.Styles><Style Selector=\"Border\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></Grid.Styles>"
                            + "<p:KeyedBorder/>" + Plain + "</Grid>")),
        C("Z31", "item Grid holds a plain p:KeyedBorder, no style (control for Z30)",
            List(NullStyle, "<Grid><p:KeyedBorder/>" + Plain + "</Grid>")),

        // ── The rows' presenter, and the styles that reach what it presents (F10, K14e) ──────────
        C("Z32", "row presenter binds ContentTemplate to Tag (null); list DataTemplate p:Row paints",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Border Background="{TemplateBinding Background}"><ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding Tag}"/></Border>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                """ + ListTemplates(PaintedFor("p:Row", "{Binding Label}")), Plain)),
        C("Z33", "row content shown by a ContentControl beside a Tag presenter; Border.Styles on its holder paints Border.item",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Panel>
                        <ContentPresenter Content="{TemplateBinding Tag}"/>
                        <Border Background="{TemplateBinding Background}">
                          <Border.Styles><Style Selector="Border.item"><Setter Property="Background" Value="Transparent"/></Style></Border.Styles>
                          <ContentControl Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/>
                        </Border>
                      </Panel>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                """, "<Border Classes=\"item\">" + Label + "</Border>")),
        C("Z34", "the same, with no Tag presenter (control for Z33)",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Panel>
                        <Border Background="{TemplateBinding Background}">
                          <Border.Styles><Style Selector="Border.item"><Setter Property="Background" Value="Transparent"/></Style></Border.Styles>
                          <ContentControl Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/>
                        </Border>
                      </Panel>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                """, "<Border Classes=\"item\">" + Label + "</Border>")),
        C("Z35", "row template: Border.Styles on the presenter's own parent paints Border.item (control: reaches)",
            List(RowTemplateWithStyle("<Style Selector=\"Border.item\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"),
                "<Border Classes=\"item\">" + Label + "</Border>")),

        // ── Code that sets a background ──────────────────────────────────────────────────────────
        C("Z36", "item root HelperView (ctor calls an extension method in another class that sets Background)",
            List(NullStyle, "<p:HelperView/>"), ("HelperView.axaml", ViewMarkup("HelperView"))),
        C("Z37", "item root DerivedLambdaView (library base sets Background in a lambda capturing a local)",
            List(NullStyle, "<p:DerivedLambdaView/>"), ("DerivedLambdaView.axaml", ViewMarkup("DerivedLambdaView"))),
        C("Z38", "item root GenericDerivedView (generic library base sets Background in its ctor)",
            List(NullStyle, "<p:GenericDerivedView/>"), ("GenericDerivedView.axaml", ViewMarkup("GenericDerivedView"))),
        C("Z39", "item root CompiledTemplateView: compiled markup paints only an unused DataTemplate",
            List(NullStyle, "<p:CompiledTemplateView/>"), ("CompiledTemplateView.axaml", SourceView("CompiledTemplateView.axaml"))),
        C("Z40", "item root CompiledThemeView: compiled markup paints only an unused ControlTheme",
            List(NullStyle, "<p:CompiledThemeView/>"), ("CompiledThemeView.axaml", SourceView("CompiledThemeView.axaml"))),
        C("Z39c", "item root CompiledBadgeView: compiled markup paints only a right-aligned badge, through a resource DataTemplate",
            List(NullStyle, "<p:CompiledBadgeView/>"), ("CompiledBadgeView.axaml", SourceView("CompiledBadgeView.axaml"))),
        C("Z39b", "item root CompiledListView: compiled markup paints only its own empty list's item template",
            List(NullStyle, "<p:CompiledListView/>"), ("CompiledListView.axaml", SourceView("CompiledListView.axaml"))),

        // ── Tree data templates ──────────────────────────────────────────────────────────────────
        new Case("Z41", "no item template; list DataTemplates holds a painted TreeDataTemplate for p:Row",
            NoItemTemplate("ListBox", NullStyle + ListTemplates(
                "<TreeDataTemplate DataType=\"p:Row\" ItemsSource=\"{Binding Kids}\"><Border Background=\"Transparent\">" + Label + "</Border></TreeDataTemplate>")), []),
        new Case("Z42", "no item template; list DataTemplates holds an unpainted TreeDataTemplate for p:Row",
            NoItemTemplate("ListBox", NullStyle + ListTemplates(
                "<TreeDataTemplate DataType=\"p:Row\" ItemsSource=\"{Binding Kids}\">" + Label + "</TreeDataTemplate>")), []),
    ];
}
