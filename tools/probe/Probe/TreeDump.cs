using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FpProbe6;

/// <summary>
/// Prints the visual tree of a stock list row, combo row and tab under the running theme: each visual's
/// type, name, bounds, a background or fill and whether hit testing sees it, and for a presenter what it is
/// given as content and template, so that what the theme's row template draws and presents can be read.
/// </summary>
internal static class TreeDump
{
    private static readonly DataTemplateMarker ItemTemplate = new();

    public static void Run()
    {
        Row[] rows = [new Row("Alpha"), new Row("Beta"), new Row("Gamma")];

        ListBox list = new() { Width = 300, Height = 200, ItemsSource = rows, ItemTemplate = ItemTemplate };
        Dump("ListBox row 1", list, () => list.ContainerFromIndex(1));

        ComboBox combo = new() { Width = 300, ItemsSource = rows, ItemTemplate = ItemTemplate };
        Dump("ComboBox row 1", combo, () =>
        {
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            return combo.ContainerFromIndex(1);
        });

        TabControl tabs = new() { Width = 300, Height = 200, ItemsSource = rows, ItemTemplate = ItemTemplate };
        Dump("TabControl tab 1", tabs, () => tabs.ContainerFromIndex(1));
    }

    private static void Dump(string title, Control host, Func<Control?> container)
    {
        Window window = new() { Width = 400, Height = 300, Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Control? row = container();
        Console.WriteLine($"--- {title}: {row?.GetType().Name ?? "none"}"
                          + (row is TemplatedControl templated ? $" Background={Describe(templated.Background)} Padding={templated.Padding}" : string.Empty));
        if (row is not null)
        {
            Walk(row, 1);
        }

        window.Close();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Walk(Visual visual, int depth)
    {
        string paint = visual switch
        {
            Border border => $" Background={Describe(border.Background)}",
            Panel panel => $" Background={Describe(panel.Background)}",
            ContentPresenter presenter => $" Background={Describe(presenter.Background)} Content={DescribeContent(presenter.Content)} ContentTemplate={DescribeTemplate(presenter.ContentTemplate)}",
            TemplatedControl control => $" Background={Describe(control.Background)}",
            Avalonia.Controls.Shapes.Shape shape => $" Fill={Describe(shape.Fill)}",
            TextBlock text => $" Background={Describe(text.Background)} Text='{text.Text}'",
            _ => string.Empty,
        };
        string hit = visual is Avalonia.Input.IInputElement { IsHitTestVisible: false } ? " NOT-HIT-TEST-VISIBLE" : string.Empty;
        string visible = visual.IsVisible ? string.Empty : " INVISIBLE";
        Console.WriteLine($"{new string(' ', depth * 2)}{visual.GetType().Name}#{(visual as StyledElement)?.Name} [{visual.Bounds}]{paint}{hit}{visible}");
        foreach (Visual child in visual.GetVisualChildren())
        {
            Walk(child, depth + 1);
        }
    }

    private static string Describe(IBrush? brush) => brush switch
    {
        null => "null",
        ISolidColorBrush solid => solid.Color.ToString(),
        _ => brush.GetType().Name,
    };

    private static string DescribeContent(object? content) => content switch
    {
        null => "null",
        Row row => $"Row({row.Label})",
        _ => content.GetType().Name,
    };

    private static string DescribeTemplate(object? template) => template switch
    {
        null => "null",
        DataTemplateMarker => "THE-ITEM-TEMPLATE",
        _ => template.GetType().Name,
    };

    /// <summary>The item template the dump hands each host, recognisable where a presenter is given it.</summary>
    private sealed class DataTemplateMarker : Avalonia.Controls.Templates.IDataTemplate
    {
        public Control? Build(object? param) => new TextBlock { Text = (param as Row)?.Label ?? "?", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };

        public bool Match(object? data) => true;
    }
}
