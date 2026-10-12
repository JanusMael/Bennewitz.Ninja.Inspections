using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Semi.Avalonia;

namespace FpProbe6;

// The fifth judge's probe for BNXQ1010 round 5. Every case of the fourth judge's probe5 is carried
// over verbatim (X and J), so round 5's claims about them are measured again; the K cases are new and
// aim at round 5's own reasoning. Each case is clicked on a fresh window at a quarter of the first
// row's width, at its centre and 3 px inside its right edge. "dump <folder>" writes each case's
// markup, and the extra files the rule's scan needs, so the rule reads exactly what was clicked.
internal static class Program
{
    private static void Main(string[] args)
    {
        // Load the referenced control library before any markup names it.
        _ = typeof(ProbeLib.LibCodeBorder).Assembly;

        if (args.Length >= 2 && args[0] == "dump")
        {
            Dump(args[1]);
            return;
        }

        ProbeApp.Theme = args.Length > 0 ? args[0] : "fluent";
        string? only = args.Length > 1 ? args[1] : null;
        AppBuilder.Configure<ProbeApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        Console.WriteLine("=== theme: " + ProbeApp.Theme + "   sync context: " + (SynchronizationContext.Current?.GetType().Name ?? "none"));
        if (only == "styles")
        {
            FrameworkStyles.Dump();
            return;
        }

        if (only == "tree")
        {
            TreeDump.Run();
            return;
        }

        if (only == "facts")
        {
            Facts.Run(args.Length > 2 ? args[2] : null);
            return;
        }

        if (only == "diag10")
        {
            Diag10.Run(args.Length > 2 ? args[2] : null);
            return;
        }

        if (only == "facts9")
        {
            Facts9.Run(args.Length > 2 ? args[2] : null);
            return;
        }

        if (only == "facts11")
        {
            Facts9.RunRows(args.Length > 2 ? args[2] : null);
            return;
        }

        if (only == "diag13")
        {
            Diag13.Run(args.Length > 2 ? args[2] : null);
            return;
        }

        if (only == "diag12")
        {
            Diag12.Run(args.Length > 2 ? args[2] : null);
            return;
        }

        if (only == "themes9")
        {
            Facts9.Themes();
            return;
        }

        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only || (only.EndsWith('*') && c.Id.StartsWith(only.TrimEnd('*'), StringComparison.Ordinal))))
        {
            // A list or combo row is clicked at a quarter, the centre and 3 px inside its right edge, at half
            // its height; a tab, whose content is its header, 3 px inside its left edge at half its height, at
            // its centre 3 px below its top, and 3 px inside its right edge: each beside the header.
            string quarter = ClickOnce(c, 0);
            string centre = ClickOnce(c, 1);
            string edge = ClickOnce(c, 2);
            Console.WriteLine($"{c.Id,-5} {c.Name,-78} quarter {quarter}   centre {centre}   edge {edge}");
        }
    }

    private static void Dump(string folder)
    {
        foreach (Case c in Cases.Every)
        {
            string directory = Path.Combine(folder, c.Id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "View.axaml"), c.Markup);
            foreach ((string name, string text) in c.Extra)
            {
                File.WriteAllText(Path.Combine(directory, name), text);
            }
        }

        Console.WriteLine($"dumped {Cases.Every.Length} cases to {folder}");
    }

    private static string ClickOnce(Case c, int which)
    {
        IDataTemplate? added = c.AppTemplate switch
        {
            "locator" => new ViewLocator(),
            "template" => new FuncDataTemplate<Row>((row, _) => new Border
            {
                Background = Brushes.Transparent,
                Child = new TextBlock { Text = row.Label, HorizontalAlignment = HorizontalAlignment.Left },
            }),
            _ => null,
        };

        if (added is not null)
        {
            Application.Current!.DataTemplates.Add(added);
        }

        try
        {
            Control root;
            try
            {
                root = (Control)AvaloniaRuntimeXamlLoader.Load(c.Markup, typeof(Program).Assembly);
            }
            catch (Exception e)
            {
                return $"load threw {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message.Split('\n')[0]}";
            }

            return root switch
            {
                ComboBox combo => ClickCombo(combo, which),
                TabControl tabs => ClickTab(tabs, which),
                ListBox list => ClickList(list, which),
                _ => $"unknown host {root.GetType().Name}",
            };
        }
        finally
        {
            if (added is not null)
            {
                Application.Current!.DataTemplates.Remove(added);
            }
        }
    }

    private static Row[] Rows() => [new Row("Alpha"), new Row("Beta"), new Row("Gamma")];

    private static double Across(int which, double width) => which switch { 0 => width / 4, 1 => width / 2, _ => width - 3 };

    /// <summary>The first row of a list, at a quarter, the centre or the edge: selected when it takes the click.</summary>
    private static string ClickList(ListBox list, int which)
    {
        list.ItemsSource = Rows();
        Window window = new() { Width = 400, Height = 300, Content = list };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        ListBoxItem row = (ListBoxItem)list.ContainerFromIndex(0)!;
        Point point = row.TranslatePoint(new Point(Across(which, row.Bounds.Width), Math.Max(1, row.Bounds.Height / 2)), window)!.Value;
        window.CaptureRenderedFrame();
        string hit = Chain(window.InputHitTest(point));
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        string result = (list.SelectedIndex == 0 ? "SELECTS" : "misses ") + $" ({hit}) w {row.Bounds.Width:0} h {row.Bounds.Height:0}";
        window.Close();
        Dispatcher.UIThread.RunJobs();
        return result;
    }

    /// <summary>
    /// The first row of a combo's open drop-down, clicked in the popup's own top level at a quarter, the
    /// centre or the edge: selected when it takes the click.
    /// </summary>
    private static string ClickCombo(ComboBox combo, int which)
    {
        combo.ItemsSource = Rows();
        Window window = new() { Width = 400, Height = 300, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        try
        {
            if (combo.ContainerFromIndex(0) is not ComboBoxItem row || TopLevel.GetTopLevel(row) is not { } top)
            {
                return "no row realised in the drop-down";
            }

            Point point = row.TranslatePoint(new Point(Across(which, row.Bounds.Width), Math.Max(1, row.Bounds.Height / 2)), top)!.Value;
            top.CaptureRenderedFrame();
            string hit = Chain(top.InputHitTest(point));
            top.MouseDown(point, MouseButton.Left);
            top.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            return (combo.SelectedIndex == 0 ? "SELECTS" : "misses ") + $" ({hit}) w {row.Bounds.Width:0} h {row.Bounds.Height:0} in {top.GetType().Name}";
        }
        finally
        {
            combo.IsDropDownOpen = false;
            Dispatcher.UIThread.RunJobs();
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// The second tab, unselected, clicked beside its header: 3 px inside its left edge, 3 px below its top
    /// at its centre, or 3 px inside its right edge: selected when it takes the click.
    /// </summary>
    private static string ClickTab(TabControl tabs, int which)
    {
        tabs.ItemsSource = Rows();
        Window window = new() { Width = 400, Height = 300, Content = tabs };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            if (tabs.ContainerFromIndex(1) is not TabItem tab)
            {
                return "no second tab realised";
            }

            double width = tab.Bounds.Width;
            double height = tab.Bounds.Height;
            Point local = which switch { 0 => new Point(3, height / 2), 1 => new Point(width / 2, 3), _ => new Point(width - 3, height / 2) };
            Point point = tab.TranslatePoint(local, window)!.Value;
            window.CaptureRenderedFrame();
            string hit = Chain(window.InputHitTest(point));
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            return (tabs.SelectedIndex == 1 ? "SELECTS" : "misses ") + $" ({hit}) w {width:0} h {height:0}";
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static string Chain(IInputElement? hit)
    {
        List<string> names = [];
        for (Visual? visual = hit as Visual; visual is not null && names.Count < 2; visual = visual.GetVisualParent())
        {
            names.Add(visual.GetType().Name);
        }

        return names.Count == 0 ? "nothing" : string.Join(" < ", names);
    }
}

public sealed class Row(string label)
{
    public string Label { get; } = label;

    /// <summary>Always true, for a class bound as the gotchas entry binds one.</summary>
    public bool Flag => true;

    /// <summary>Always false, for a LastChildFill bound to false.</summary>
    public bool NotFlag => false;

    /// <summary>A side a dock reads from a binding: Left.</summary>
    public Dock Side => Dock.Left;

    /// <summary>A side a dock reads from a binding: Top.</summary>
    public Dock TopSide => Dock.Top;

    /// <summary>A width a binding supplies: wider than the 300 px row.</summary>
    public double Wide => 600;
}

/// <summary>A view model written as literal element content, presented by a data template.</summary>
public sealed class Vm
{
    public string Label { get; set; } = "vm";
}

/// <summary>A view locator, as Avalonia's MVVM template writes one: what presents a Row is chosen in code.</summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param) => new Border
    {
        Background = Brushes.Transparent,
        Child = new TextBlock { Text = (param as Row)?.Label ?? "?", HorizontalAlignment = HorizontalAlignment.Left },
    };

    public bool Match(object? data) => data is Row;
}

/// <summary>A consumer's templated control, drawn by the theme the case gives it.</summary>
public sealed class Chip : TemplatedControl;

/// <summary>A consumer's templated control whose Background defaults to Transparent, set in its static constructor.</summary>
public sealed class Chip2 : TemplatedControl
{
    static Chip2() => BackgroundProperty.OverrideDefaultValue<Chip2>(Brushes.Transparent);
}

/// <summary>A view whose markup paints nothing, as PlainView.axaml writes it.</summary>
public sealed class PlainView : UserControl
{
    public PlainView() => Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
}

/// <summary>A view whose code-behind sets its Background after its markup, which paints nothing.</summary>
public sealed class CodeView : UserControl
{
    public CodeView()
    {
        Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
        Background = Brushes.Transparent;
    }
}

/// <summary>A view whose Background defaults to Transparent, set in its static constructor.</summary>
public sealed class DefaultView : UserControl
{
    static DefaultView() => BackgroundProperty.OverrideDefaultValue<DefaultView>(Brushes.Transparent);

    public DefaultView() => Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
}

/// <summary>A view whose code-behind sets its Background in a handler that captures a local, as code-behind often does.</summary>
public sealed class LambdaView : UserControl
{
    public LambdaView()
    {
        Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
        IBrush brush = Brushes.Transparent;
        AttachedToVisualTree += (_, _) => Background = brush;
    }
}

/// <summary>A view whose code-behind sets its Background after an await, as an async handler does.</summary>
public sealed class AsyncView : UserControl
{
    public AsyncView()
    {
        Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
        _ = PaintLater();
    }

    private async Task PaintLater()
    {
        await Task.Yield();
        Background = Brushes.Transparent;
    }
}

/// <summary>A Border whose own code sets its Background, in the assembly the scan is given.</summary>
public sealed class CodeBorder : Border
{
    public CodeBorder() => Background = Brushes.Transparent;
}

/// <summary>A panel, not templated, whose static constructor makes its Background default Transparent.</summary>
public sealed class PaintedPanel : StackPanel
{
    static PaintedPanel() => BackgroundProperty.OverrideDefaultValue<PaintedPanel>(Brushes.Transparent);
}

/// <summary>A row whose static constructor makes its Background default Transparent.</summary>
public sealed class PaintedRow : ListBoxItem
{
    static PaintedRow() => BackgroundProperty.OverrideDefaultValue<PaintedRow>(Brushes.Transparent);

    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}

public sealed class PaintedRowList : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new PaintedRow();
}

/// <summary>A row whose code sets its Background through the property's setter.</summary>
public sealed class CodeRow : ListBoxItem
{
    public CodeRow() => Background = Brushes.Transparent;

    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}

public sealed class CodeRowList : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new CodeRow();
}

/// <summary>A row whose code sets its Background through SetValue.</summary>
public sealed class SetValueRow : ListBoxItem
{
    public SetValueRow() => SetValue(BackgroundProperty, Brushes.Transparent);

    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}

public sealed class SetValueRowList : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SetValueRow();
}

/// <summary>A row whose code sets its Background through the setter, in a handler that captures a local.</summary>
public sealed class LambdaRow : ListBoxItem
{
    public LambdaRow()
    {
        IBrush brush = Brushes.Transparent;
        AttachedToVisualTree += (_, _) => Background = brush;
    }

    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}

public sealed class LambdaRowList : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new LambdaRow();
}

public sealed record Case(string Id, string Name, string Markup, (string File, string Text)[] Extra, string? AppTemplate = null);

internal static partial class Cases
{
    private const string Presenter = """
        <ContentPresenter Name="PART_ContentPresenter"
                          Content="{TemplateBinding Content}"
                          ContentTemplate="{TemplateBinding ContentTemplate}"
                          Padding="{TemplateBinding Padding}"
                          HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                          VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"/>
        """;

    private const string Plain = "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>";
    private const string Painted = "<Border Background=\"Transparent\"><TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Border>";

    private const string NullSetter = "<Style Selector=\"ListBoxItem\"><Setter Property=\"Background\" Value=\"{x:Null}\"/></Style>";
    private const string NullStyle = "<ListBox.Styles>" + NullSetter + "</ListBox.Styles>";

    private static string Theme(string template) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Template"><ControlTemplate>TEMPLATE</ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("TEMPLATE", template);

    private const string NoTemplateTheme = """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="Transparent"/>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """;

    /// <summary>A row theme based on nothing that sets no Background, whose template takes the row's.</summary>
    private static readonly string UnpaintedTheme = """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Template"><ControlTemplate><Border Background="{TemplateBinding Background}">PRESENTER</Border></ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("PRESENTER", Presenter);

    private static string ThemeFor(string type, string setters) => """
        <ListBox.Resources>
          <ControlTheme x:Key="{x:Type TYPE}" TargetType="TYPE">
            SETTERS
            <Setter Property="Template">
              <ControlTemplate>
                <Border Background="{TemplateBinding Background}"><TextBlock Text="chip" HorizontalAlignment="Left"/></Border>
              </ControlTemplate>
            </Setter>
          </ControlTheme>
        </ListBox.Resources>
        """.Replace("TYPE", type).Replace("SETTERS", setters);

    private const string Namespaces = """
        xmlns="https://github.com/avaloniaui"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:p="clr-namespace:FpProbe6;assembly=FpProbe6"
                 xmlns:l="clr-namespace:ProbeLib;assembly=ProbeLib"
                 xmlns:sys="clr-namespace:System;assembly=System.Runtime"
        """;

    private static string List(string fragment, string itemRoot) => Host("ListBox", fragment, itemRoot);

    private static string Host(string host, string fragment, string itemRoot) => """
        <HOST NAMESPACES
                 Width="300" Height="200" ItemsSource="{Binding Rows}">
          FRAGMENT
          <ListBox.ItemTemplate><DataTemplate>ITEM</DataTemplate></ListBox.ItemTemplate>
        </HOST>
        """.Replace("HOST", host).Replace("NAMESPACES", Namespaces).Replace("FRAGMENT", fragment).Replace("ITEM", itemRoot);

    private static string NoItemTemplate(string host, string fragment) => """
        <HOST NAMESPACES
                 Width="300" Height="200" ItemsSource="{Binding Rows}">
          FRAGMENT
        </HOST>
        """.Replace("HOST", host).Replace("NAMESPACES", Namespaces).Replace("FRAGMENT", fragment);

    private static string ViewMarkup(string type, string clrNamespace = "FpProbe6") => """
        <UserControl xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     x:Class="NS.TYPE">
          <TextBlock Text="view" HorizontalAlignment="Left"/>
        </UserControl>
        """.Replace("NS", clrNamespace).Replace("TYPE", type);

    private const string AppWithLocator = """
        <Application xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:p="clr-namespace:FpProbe6;assembly=FpProbe6">
          <Application.DataTemplates>
            <p:ViewLocator/>
          </Application.DataTemplates>
        </Application>
        """;

    private const string AppWithDataTemplate = """
        <Application xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:p="clr-namespace:FpProbe6;assembly=FpProbe6">
          <Application.DataTemplates>
            <DataTemplate DataType="p:Row"><Border Background="Transparent"><TextBlock Text="{Binding Label}" HorizontalAlignment="Left"/></Border></DataTemplate>
          </Application.DataTemplates>
        </Application>
        """;

    private static Case C(string id, string name, string markup, params (string File, string Text)[] extra) => new(id, name, markup, extra);

    /// <summary>A compiled view's markup, which Probe.csproj copies beside the build output, so the dump is what was compiled.</summary>
    private static string SourceView(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "views", file));

    private const string Wide = "<Grid ColumnDefinitions=\"16,*\">CHILD</Grid>";

    private static readonly string RowTemplateStyled = """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Template"><ControlTemplate>
              <Border Background="{TemplateBinding Background}">
                <Border.Styles><Style Selector="Border.item"><Setter Property="Background" Value="Transparent"/></Style></Border.Styles>
                PRESENTER
              </Border>
            </ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("PRESENTER", Presenter);

    /// <summary>A row theme whose Background is null and whose template's root Border declares one style.</summary>
    private static string RowTemplateWithStyle(string style) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Template"><ControlTemplate>
              <Border Background="{TemplateBinding Background}">
                <Border.Styles>STYLE</Border.Styles>
                PRESENTER
              </Border>
            </ControlTemplate></Setter>
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("STYLE", style).Replace("PRESENTER", Presenter);

    private const string PaintedVmTemplates =
        "<ListBox.DataTemplates><DataTemplate DataType=\"p:Vm\"><Border Background=\"Transparent\"><TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/></Border></DataTemplate></ListBox.DataTemplates>";

    public static readonly Case[] All =
    [
        // Round 3's fp-probe3, verbatim.
        C("X5", "bare presenter; item Grid holds a Transparent Border",
            List(Theme(Presenter), "<Grid><Border Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("X6", "stock rows, style {x:Null}; item root is a UserControl",
            List(NullStyle, "<UserControl>" + Plain + "</UserControl>")),
        C("X7", "theme sets no Template; item root paints",
            List(NoTemplateTheme, Painted)),
        C("X7b", "theme sets no Template; plain item template",
            List(NoTemplateTheme, Plain)),
        C("X9", "theme sets Template {x:Null}; plain item template",
            List(NoTemplateTheme.Replace("</ControlTheme>", "<Setter Property=\"Template\" Value=\"{x:Null}\"/></ControlTheme>"), Plain)),
        C("X8", "template Border styled by its own Border.Styles",
            List(Theme("<Border><Border.Styles><Style Selector=\"Border\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></Border.Styles>"
                       + Presenter + "</Border>"), Plain)),

        // The author's fp-probe4, verbatim but for the probe's namespace.
        C("X10", "item root is a ContentControl",
            List(NullStyle, "<ContentControl Content=\"{Binding Label}\" HorizontalContentAlignment=\"Left\"/>")),
        C("X11", "item Grid: a right-aligned label with a Transparent background",
            List(NullStyle, "<Grid>" + Plain + "<TextBlock Text=\"gap\" HorizontalAlignment=\"Right\" Background=\"Transparent\"/></Grid>")),
        C("X12", "item Grid 16,*: a Transparent Border in the * column",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Grid.Column=\"1\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("X13", "item Grid 16,*: a Chip whose theme binds a background nothing sets",
            List(NullStyle + ThemeFor("p:Chip", string.Empty), Wide.Replace("CHILD", "<p:Chip Grid.Column=\"1\"/>"))),
        C("X14", "item Grid 16,*: a Chip whose theme sets Transparent",
            List(NullStyle + ThemeFor("p:Chip", "<Setter Property=\"Background\" Value=\"Transparent\"/>"), Wide.Replace("CHILD", "<p:Chip Grid.Column=\"1\"/>"))),
        C("X15", "item Grid 16,Auto,*: a 14 px Button in the 16 px column",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,Auto,*\"><Button Grid.Column=\"0\" Width=\"14\"/><TextBlock Grid.Column=\"2\" Text=\"{Binding Label}\"/></Grid>")),

        // probe5's J cases, verbatim.
        C("J1", "DockPanel: last child Dock=Left, Transparent (LastChildFill)",
            List(NullStyle, "<DockPanel><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><Border DockPanel.Dock=\"Left\" Background=\"Transparent\"/></DockPanel>")),
        C("J1b", "DockPanel: last child Dock=Right, Transparent (LastChildFill)",
            List(NullStyle, "<DockPanel><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><Border DockPanel.Dock=\"Right\" Background=\"Transparent\"/></DockPanel>")),
        C("J1c", "DockPanel LastChildFill=False: last child Dock=Left",
            List(NullStyle, "<DockPanel LastChildFill=\"False\"><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><Border DockPanel.Dock=\"Left\" Background=\"Transparent\"/></DockPanel>")),
        C("J1d", "DockPanel: last child with no Dock, Transparent",
            List(NullStyle, "<DockPanel><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><Border Background=\"Transparent\"/></DockPanel>")),
        C("J2", "DockPanel: first child with no Dock (docks Left), empty Transparent",
            List(NullStyle, "<DockPanel><Border Background=\"Transparent\"/><TextBlock Text=\"{Binding Label}\"/></DockPanel>")),
        C("J3", "item Grid: Transparent Border MaxWidth=600",
            List(NullStyle, "<Grid><Border MaxWidth=\"600\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("J3b", "item Grid: Transparent Border MaxWidth=100",
            List(NullStyle, "<Grid><Border MaxWidth=\"100\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("J4", "item Grid: Transparent Border Width=600",
            List(NullStyle, "<Grid><Border Width=\"600\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("J5", "item Grid: Transparent Border Left-aligned, MinWidth=600",
            List(NullStyle, "<Grid><Border HorizontalAlignment=\"Left\" MinWidth=\"600\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("J6", "ContentControl HorizontalContentAlignment=Left holds a painted Border",
            List(NullStyle, "<ContentControl HorizontalContentAlignment=\"Left\"><Border Background=\"Transparent\"><TextBlock Text=\"{Binding Label}\"/></Border></ContentControl>")),
        C("J7", "ContentControl, ContentTemplate element whose root paints",
            List(NullStyle, "<ContentControl Content=\"{Binding}\"><ContentControl.ContentTemplate><DataTemplate><Border Background=\"Transparent\">"
                            + Plain + "</Border></DataTemplate></ContentControl.ContentTemplate></ContentControl>")),
        C("J7b", "ContentControl, ContentTemplate={StaticResource} whose root paints",
            List(NullStyle + "<ListBox.Resources><DataTemplate x:Key=\"Painted\"><Border Background=\"Transparent\">" + Plain + "</Border></DataTemplate></ListBox.Resources>",
                "<ContentControl Content=\"{Binding}\" ContentTemplate=\"{StaticResource Painted}\"/>")),
        C("J7c", "UserControl Content={Binding}, its own DataTemplates paint",
            List(NullStyle, "<UserControl Content=\"{Binding}\"><UserControl.DataTemplates><DataTemplate DataType=\"p:Row\"><Border Background=\"Transparent\">"
                            + Plain + "</Border></DataTemplate></UserControl.DataTemplates></UserControl>")),
        C("J7d", "ContentControl Content={Binding}, the list's DataTemplates paint",
            List(NullStyle + "<ListBox.DataTemplates><DataTemplate DataType=\"p:Row\"><Border Background=\"Transparent\">" + Plain + "</Border></DataTemplate></ListBox.DataTemplates>",
                "<ContentControl Content=\"{Binding}\"/>")),
        C("J8", "item Grid 16,*: Chip2 (Background default Transparent in code), theme binds it",
            List(NullStyle + ThemeFor("p:Chip2", string.Empty), Wide.Replace("CHILD", "<p:Chip2 Grid.Column=\"1\"/>"))),
        C("J8r", "item root is Chip2 (Background default Transparent in code), theme binds it",
            List(NullStyle + ThemeFor("p:Chip2", string.Empty), "<p:Chip2/>")),
        C("J9", "view whose code-behind sets Background Transparent",
            List(NullStyle, "<p:CodeView/>"), ("CodeView.axaml", ViewMarkup("CodeView"))),
        C("J9b", "view whose Background defaults to Transparent in code",
            List(NullStyle, "<p:DefaultView/>"), ("DefaultView.axaml", ViewMarkup("DefaultView"))),
        C("J9c", "view that paints nothing anywhere",
            List(NullStyle, "<p:PlainView/>"), ("PlainView.axaml", ViewMarkup("PlainView"))),
        C("J10", "Chip whose theme binds Background, a list style p|Chip sets Transparent",
            List("<ListBox.Styles>" + NullSetter + "<Style Selector=\"p|Chip\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ListBox.Styles>"
                 + ThemeFor("p:Chip", string.Empty), Wide.Replace("CHILD", "<p:Chip Grid.Column=\"1\"/>"))),
        C("J11", "Chip theme, nested ^:not(:pointerover) sets Transparent",
            List(NullStyle + ThemeFor("p:Chip", "<Style Selector=\"^:not(:pointerover)\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"),
                Wide.Replace("CHILD", "<p:Chip Grid.Column=\"1\"/>"))),
        C("J7e", "ContentPresenter, ContentTemplate element whose root paints",
            List(NullStyle, "<ContentPresenter Content=\"{Binding}\"><ContentPresenter.ContentTemplate><DataTemplate><Border Background=\"Transparent\">"
                            + Plain + "</Border></DataTemplate></ContentPresenter.ContentTemplate></ContentPresenter>")),
        C("J20", "row template: bare presenter and a 20 px Chip whose theme paints Transparent",
            List(Theme("<Grid>" + Presenter + "<p:Chip HorizontalAlignment=\"Right\" Width=\"20\"/></Grid>")
                 + ThemeFor("p:Chip", "<Setter Property=\"Background\" Value=\"Transparent\"/>"), Plain)),
        C("J16", "row template's Border.Styles paints Border.item in the item template",
            List(RowTemplateStyled, "<Border Classes=\"item\">" + Plain + "</Border>")),
        C("J18", "template Panel.Styles paints the Border it holds",
            List(Theme("<Panel><Panel.Styles><Style Selector=\"Border\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></Panel.Styles><Border>"
                       + Presenter + "</Border></Panel>"), Plain)),
        C("J22", "template Border.foo (literal class) styled by its own Border.Styles",
            List(Theme("<Border Classes=\"foo\"><Border.Styles><Style Selector=\"Border.foo\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></Border.Styles>"
                       + Presenter + "</Border>"), Plain)),
        C("J22b", "item Border Classes.foo={Binding Flag} styled by its own Border.Styles",
            List(NullStyle, "<Border Classes.foo=\"{Binding Flag}\"><Border.Styles><Style Selector=\"Border.foo\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></Border.Styles>"
                            + Plain + "</Border>")),
        C("J22c", "item Border.foo, the style on the parent StackPanel (the entry's GOOD form)",
            List(NullStyle, "<StackPanel><StackPanel.Styles><Style Selector=\"Border.foo\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></StackPanel.Styles><Border Classes.foo=\"{Binding Flag}\">"
                            + Plain + "</Border></StackPanel>")),
        C("J19", "template sibling StackPanel.Styles, Border beside it",
            List(Theme("<Panel><StackPanel><StackPanel.Styles><Style Selector=\"Border\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></StackPanel.Styles></StackPanel><Border>"
                       + Presenter + "</Border></Panel>"), Plain)),

        // ── New: docks ──────────────────────────────────────────────────────────
        C("K1", "DockPanel LastChildFill={Binding Flag}(true): last child Dock=Left, Transparent",
            List(NullStyle, "<DockPanel LastChildFill=\"{Binding Flag}\"><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><Border DockPanel.Dock=\"Left\" Background=\"Transparent\"/></DockPanel>")),
        C("K1b", "DockPanel LastChildFill={Binding NotFlag}(false): last child Dock=Left",
            List(NullStyle, "<DockPanel LastChildFill=\"{Binding NotFlag}\"><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><Border DockPanel.Dock=\"Left\" Background=\"Transparent\"/></DockPanel>")),
        C("K2", "DockPanel: first child Dock={Binding Side}(Left), empty Transparent",
            List(NullStyle, "<DockPanel><Border DockPanel.Dock=\"{Binding Side}\" Background=\"Transparent\"/><TextBlock Text=\"{Binding Label}\"/></DockPanel>")),
        C("K2b", "DockPanel: first child Dock={Binding TopSide}(Top), Height 60, Transparent",
            List(NullStyle, "<DockPanel><Border DockPanel.Dock=\"{Binding TopSide}\" Height=\"60\" Background=\"Transparent\"/><TextBlock Text=\"{Binding Label}\"/></DockPanel>")),
        C("K3", "nested docks: inner dock fills, its last child Dock=Left paints",
            List(NullStyle, "<DockPanel><TextBlock DockPanel.Dock=\"Right\" Text=\"{Binding Label}\"/><DockPanel><TextBlock DockPanel.Dock=\"Right\" Text=\"x\"/>"
                            + "<Border DockPanel.Dock=\"Left\" Background=\"Transparent\"/></DockPanel></DockPanel>")),
        C("K4", "nested docks: inner dock docked Left (not last) holds a Transparent Border",
            List(NullStyle, "<DockPanel><DockPanel DockPanel.Dock=\"Left\"><Border Background=\"Transparent\"/></DockPanel><TextBlock Text=\"{Binding Label}\"/></DockPanel>")),
        C("K5", "dock's last child is a Panel holding a Transparent Border",
            List(NullStyle, "<DockPanel><TextBlock DockPanel.Dock=\"Left\" Text=\"{Binding Label}\"/><Panel><Border Background=\"Transparent\"/></Panel></DockPanel>")),
        C("K5b", "dock's last child is a Panel holding a Left-aligned painted Border with text",
            List(NullStyle, "<DockPanel><TextBlock DockPanel.Dock=\"Right\" Text=\"x\"/><Panel><Border HorizontalAlignment=\"Left\" Background=\"Transparent\">"
                            + "<TextBlock Text=\"{Binding Label}\"/></Border></Panel></DockPanel>")),
        C("K6", "DockPanel: Transparent Border first, last child IsVisible=False",
            List(NullStyle, "<DockPanel><Border Background=\"Transparent\"/><TextBlock IsVisible=\"False\" Text=\"x\"/></DockPanel>")),
        C("K19", "DockPanel: first child docks Top by a property element, Height 60",
            List(NullStyle, "<DockPanel><Border Height=\"60\" Background=\"Transparent\"><DockPanel.Dock>Top</DockPanel.Dock></Border><TextBlock Text=\"{Binding Label}\"/></DockPanel>")),

        // ── New: sized elements among fixed and Auto columns ────────────────────
        C("K7", "Grid 16,*: col 0 Border MinWidth=10 Width=600, Transparent",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Grid.Column=\"0\" MinWidth=\"10\" Width=\"600\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K7b", "Grid 16,*: col 0 Border Left MinWidth=10 Width=600, Transparent",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Grid.Column=\"0\" HorizontalAlignment=\"Left\" MinWidth=\"10\" Width=\"600\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K7c", "Grid 16,*: col 0 Border Left MinWidth=4 Width={Binding Wide}(600)",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Grid.Column=\"0\" HorizontalAlignment=\"Left\" MinWidth=\"4\" Width=\"{Binding Wide}\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K7e", "Grid 16,*: col 0 Border Left MinWidth={Binding Wide}(600) Width=10",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Grid.Column=\"0\" HorizontalAlignment=\"Left\" MinWidth=\"{Binding Wide}\" Width=\"10\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K7d", "Grid 16,*: col 0 Border Width=600 alone (control for K7)",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Grid.Column=\"0\" Width=\"600\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K8", "Grid 16,Auto,* spacing 4: Width=40 Border in the Auto column",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,Auto,*\" ColumnSpacing=\"4\"><Border Grid.Column=\"1\" Width=\"40\" Background=\"Transparent\"/><TextBlock Grid.Column=\"2\" Text=\"{Binding Label}\"/></Grid>")),
        C("K8b", "Grid 16,16,* spacing 8: Width=40 Border spanning two (fits 40)",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,16,*\" ColumnSpacing=\"8\"><Border Grid.ColumnSpan=\"2\" Width=\"40\" Background=\"Transparent\"/><TextBlock Grid.Column=\"2\" Text=\"{Binding Label}\"/></Grid>")),
        C("K8c", "Grid 16,16,* spacing 8: span 2, Left, MinWidth=30 Width=300",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,16,*\" ColumnSpacing=\"8\"><Border Grid.ColumnSpan=\"2\" HorizontalAlignment=\"Left\" MinWidth=\"30\" Width=\"300\" Background=\"Transparent\"/><TextBlock Grid.Column=\"2\" Text=\"{Binding Label}\"/></Grid>")),
        C("K9", "Grid Auto,*: MinWidth=40 Border in the Auto column beside a 600 px element",
            List(NullStyle, "<Grid ColumnDefinitions=\"Auto,*\"><Border Grid.Column=\"0\" Width=\"600\"/><Border Grid.Column=\"0\" MinWidth=\"40\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K9b", "Grid Auto,*: unsized Border in the Auto column beside a 600 px element",
            List(NullStyle, "<Grid ColumnDefinitions=\"Auto,*\"><Border Grid.Column=\"0\" Width=\"600\"/><Border Grid.Column=\"0\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),
        C("K21", "item Grid: Left Border whose Width=600 is a property element",
            List(NullStyle, "<Grid><Border HorizontalAlignment=\"Left\" Background=\"Transparent\"><Border.Width>600</Border.Width></Border>" + Plain + "</Grid>")),
        C("K22", "item Grid: Left Border.wide, a list style sets MinWidth=600",
            List("<ListBox.Styles>" + NullSetter + "<Style Selector=\"Border.wide\"><Setter Property=\"MinWidth\" Value=\"600\"/></Style></ListBox.Styles>",
                "<Grid><Border Classes=\"wide\" HorizontalAlignment=\"Left\" Background=\"Transparent\"/>" + Plain + "</Grid>")),
        C("K20", "Grid 16,*: Transparent Border placed in column 1 by a Grid.Column property element",
            List(NullStyle, "<Grid ColumnDefinitions=\"16,*\"><Border Background=\"Transparent\"><Grid.Column>1</Grid.Column></Border><TextBlock Grid.Column=\"0\" Text=\"{Binding Label}\"/></Grid>")),
        C("K23", "Grid col 0 Width=16 MinWidth=400: unsized Transparent Border in it",
            List(NullStyle, "<Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"16\" MinWidth=\"400\"/><ColumnDefinition Width=\"*\"/></Grid.ColumnDefinitions>"
                            + "<Border Grid.Column=\"0\" Background=\"Transparent\"/><TextBlock Grid.Column=\"1\" Text=\"{Binding Label}\"/></Grid>")),

        // ── New: what a content control presents ────────────────────────────────
        C("K10", "ContentControl with literal element content p:Vm; list DataTemplate paints",
            List(NullStyle + PaintedVmTemplates, "<ContentControl><p:Vm/></ContentControl>")),
        C("K10c", "ContentControl.Content property element holding p:Vm; list DataTemplate paints",
            List(NullStyle + PaintedVmTemplates, "<ContentControl><ContentControl.Content><p:Vm/></ContentControl.Content></ContentControl>")),
        C("K10e", "UserControl with literal element content p:Vm; list DataTemplate paints",
            List(NullStyle + PaintedVmTemplates, "<UserControl><p:Vm/></UserControl>")),
        C("K10p", "ContentPresenter with literal element content p:Vm; list DataTemplate paints",
            List(NullStyle + PaintedVmTemplates, "<ContentPresenter><p:Vm/></ContentPresenter>")),
        C("K10n", "ContentControl with literal p:Vm, NO data template for it (control)",
            List(NullStyle, "<ContentControl><p:Vm/></ContentControl>")),
        C("K10f", "ContentControl Content=\"row\" (a string); list DataTemplate for sys:String paints",
            List(NullStyle + "<ListBox.DataTemplates><DataTemplate DataType=\"sys:String\"><Border Background=\"Transparent\"><TextBlock Text=\"{Binding}\" HorizontalAlignment=\"Left\"/></Border></DataTemplate></ListBox.DataTemplates>",
                "<ContentControl Content=\"row\"/>")),
        C("K26", "ContentControl Content=\"row\"; a list style sets a painted ContentTemplate",
            List("<ListBox.Styles>" + NullSetter + "<Style Selector=\"ContentControl.painted\"><Setter Property=\"ContentTemplate\"><DataTemplate>"
                 + "<Border Background=\"Transparent\"><TextBlock Text=\"{Binding}\" HorizontalAlignment=\"Left\"/></Border></DataTemplate></Setter></Style></ListBox.Styles>",
                "<ContentControl Classes=\"painted\" Content=\"row\"/>")),

        // ── New: rows whose type registers or sets a Background ─────────────────
        C("K11", "PaintedRow (static ctor default Transparent), theme BasedOn nothing, no Background",
            Host("p:PaintedRowList", UnpaintedTheme, Plain)),
        C("K11n", "stock ListBoxItem, the same theme (control for K11)",
            List(UnpaintedTheme, Plain)),
        C("K11b", "CodeRow (ctor sets Background through the setter), list style {x:Null}",
            Host("p:CodeRowList", NullStyle, Plain)),
        C("K11c", "SetValueRow (ctor SetValue(BackgroundProperty)), list style {x:Null}",
            Host("p:SetValueRowList", NullStyle, Plain)),
        C("K11d", "LambdaRow (setter called in a handler capturing a local), style {x:Null}",
            Host("p:LambdaRowList", NullStyle, Plain)),
        C("K11e", "l:LibCodeRow (ctor sets Background; referenced assembly), style {x:Null}",
            Host("l:LibCodeRowList", NullStyle, Plain)),

        // ── New: template elements whose code sets a Background ─────────────────
        C("K12", "item root LambdaView (setter in a handler capturing a local)",
            List(NullStyle, "<p:LambdaView/>"), ("LambdaView.axaml", ViewMarkup("LambdaView"))),
        C("K12b", "item root AsyncView (setter after an await, in its state machine)",
            List(NullStyle, "<p:AsyncView/>"), ("AsyncView.axaml", ViewMarkup("AsyncView"))),
        C("K27", "item root CompiledView: compiled markup paints only a right-aligned badge",
            List(NullStyle, "<p:CompiledView/>"), ("CompiledView.axaml", SourceView("CompiledView.axaml"))),
        C("K27b", "item root CompiledPlainView: compiled markup paints nothing",
            List(NullStyle, "<p:CompiledPlainView/>"), ("CompiledPlainView.axaml", SourceView("CompiledPlainView.axaml"))),
        C("K12c", "item Grid holds l:LibCodeBorder (ctor sets Background; referenced assembly)",
            List(NullStyle, "<Grid><l:LibCodeBorder/>" + Plain + "</Grid>")),
        C("K12d", "item root l:LibCodeView (code-behind sets Background; referenced assembly)",
            List(NullStyle, "<l:LibCodeView/>"), ("LibCodeView.axaml", ViewMarkup("LibCodeView", "ProbeLib"))),
        C("K12e", "item Grid holds l:LibDefaultBorder (static default Transparent; referenced)",
            List(NullStyle, "<Grid><l:LibDefaultBorder/>" + Plain + "</Grid>")),
        C("K12f", "item Grid holds p:CodeBorder (ctor sets Background; scanned assembly)",
            List(NullStyle, "<Grid><p:CodeBorder/>" + Plain + "</Grid>")),

        // ── New: what presents an item with no item template ────────────────────
        new Case("K13", "no item template; App.axaml DataTemplates holds a view locator",
            NoItemTemplate("ListBox", NullStyle), [("App.axaml", AppWithLocator)], "locator"),
        new Case("K13b", "item template ContentControl Content={Binding}; locator in App.axaml",
            List(NullStyle, "<ContentControl Content=\"{Binding}\"/>"), [("App.axaml", AppWithLocator)], "locator"),
        new Case("K13c", "no item template; App.axaml DataTemplate for p:Row paints",
            NoItemTemplate("ListBox", NullStyle), [("App.axaml", AppWithDataTemplate)], "template"),
        new Case("K13d", "no item template; a view locator added in code only",
            NoItemTemplate("ListBox", NullStyle), [], "locator"),
        new Case("K13e", "no item template; no locator, no data template (control)",
            NoItemTemplate("ListBox", NullStyle), []),

        // ── New: styles and themes in the row template ──────────────────────────
        C("K14", "row template Border.Styles: Style Grid sets Transparent; item root Grid",
            List(RowTemplateWithStyle("<Style Selector=\"Grid\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"),
                "<Grid>" + Plain + "</Grid>")),
        C("K14d", "row template Border.Styles: Style TextBlock sets Transparent; item root TextBlock",
            List(RowTemplateWithStyle("<Style Selector=\"TextBlock\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"),
                "<TextBlock Text=\"{Binding Label}\"/>")),
        C("K14e", "row template sibling StackPanel.Styles: Style Grid; item root Grid",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Panel>
                        <StackPanel><StackPanel.Styles><Style Selector="Grid"><Setter Property="Background" Value="Transparent"/></Style></StackPanel.Styles></StackPanel>
                        <Border Background="{TemplateBinding Background}">PRESENTER</Border>
                      </Panel>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                """.Replace("PRESENTER", Presenter), "<Grid>" + Plain + "</Grid>")),
        C("K24", "row presenter binds Content but not ContentTemplate; list DataTemplate paints Row",
            List("""
                <ListBox.ItemContainerTheme>
                  <ControlTheme TargetType="ListBoxItem">
                    <Setter Property="Background" Value="{x:Null}"/>
                    <Setter Property="Template"><ControlTemplate>
                      <Border Background="{TemplateBinding Background}"><ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}"/></Border>
                    </ControlTemplate></Setter>
                  </ControlTheme>
                </ListBox.ItemContainerTheme>
                <ListBox.DataTemplates><DataTemplate DataType="p:Row"><Border Background="Transparent"><TextBlock Text="{Binding Label}" HorizontalAlignment="Left"/></Border></DataTemplate></ListBox.DataTemplates>
                """, Plain)),
        C("K25", "row template: bare presenter and a 20 px PaintedPanel (default Transparent) at the right",
            List(Theme("<Grid>" + Presenter + "<p:PaintedPanel HorizontalAlignment=\"Right\" Width=\"20\"/></Grid>"), Plain)),
        C("K25b", "row template: bare presenter and a 20 px Border Background=Transparent at the right",
            List(Theme("<Grid>" + Presenter + "<Border HorizontalAlignment=\"Right\" Width=\"20\" Background=\"Transparent\"/></Grid>"), Plain)),
        C("K16", "row template: a stretched Chip its theme paints, beside the bare presenter",
            List(Theme("<Grid><p:Chip/>" + Presenter + "</Grid>")
                 + ThemeFor("p:Chip", "<Setter Property=\"Background\" Value=\"Transparent\"/>"), Plain)),
    ];
}

internal sealed class ProbeApp : Application
{
    public static string Theme { get; set; } = "fluent";

    public override void Initialize()
    {
        Styles.Add(Theme switch
        {
            "simple" => new SimpleTheme(),
            "semi" => new SemiTheme(),
            _ => new FluentTheme(),
        });
    }
}
