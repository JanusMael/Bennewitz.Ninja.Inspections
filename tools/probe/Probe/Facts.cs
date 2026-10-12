using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FpProbe6;

// What BNXQ1010 round 9 claims, measured per case: the first row's Background in the state it starts in,
// what in the row's own template draws that Background, and what there paints a brush of its own.
//
// The row's template is what the row's visual tree holds, but for what a presenter shows through a content
// template or a data template, and what an items presenter holds: a ContentPresenter whose Content is not a
// control shows data through a template, and its subtree is passed over. What draws the row's Background is
// found by giving the row a marker brush as a local value: a Border, a Panel, a ContentPresenter, a
// TextPresenter or a TextBlock whose Background, or a shape whose Fill, then holds the marker, and held the
// row's own value before, draws it plainly. One that held something else before, such as a TargetNullValue
// standing in for a null, paints of its own. Anything else that holds a brush paints of its own. A control
// outside Avalonia that overrides Render is listed apart, since what it draws is not read here.
internal static class Facts
{
    private static readonly SolidColorBrush Marker = new(Color.FromArgb(255, 1, 2, 3));

    public static void Run(string? only)
    {
        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only || (only.EndsWith('*') && c.Id.StartsWith(only.TrimEnd('*'), StringComparison.Ordinal))))
        {
            Console.WriteLine($"{c.Id,-6} {Measure(c)}");
        }
    }

    private static string Measure(Case c)
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

        Window? window = null;
        ComboBox? combo = null;
        try
        {
            Control root;
            try
            {
                root = (Control)Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(c.Markup, typeof(Program).Assembly);
            }
            catch (Exception e)
            {
                return $"load threw {(e.InnerException ?? e).GetType().Name}";
            }

            if (root is not ItemsControl host)
            {
                return $"unknown host {root.GetType().Name}";
            }

            host.ItemsSource = new[] { new Row("Alpha"), new Row("Beta"), new Row("Gamma") };
            window = new Window { Width = 400, Height = 300, Content = host };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            if (host is ComboBox comboBox)
            {
                combo = comboBox;
                combo.IsDropDownOpen = true;
                Dispatcher.UIThread.RunJobs();
            }

            Control? row = host switch
            {
                TabControl tabs => tabs.ContainerFromIndex(1),
                _ => host.ContainerFromIndex(0),
            };
            if (row is not TemplatedControl templated)
            {
                return "no row realised";
            }

            IBrush? start = templated.Background;
            List<(Visual Visual, string Property, IBrush? Before)> drawers = [];
            List<string> selfRendered = [];
            foreach (Visual visual in TemplateVisuals(templated))
            {
                if (Painted(visual) is { } property)
                {
                    drawers.Add((visual, property, Read(visual, property)));
                }

                if (visual.GetType().Assembly.GetName().Name?.StartsWith("Avalonia", StringComparison.Ordinal) != true
                    && visual.GetType().GetMethod("Render", [typeof(DrawingContext)])?.DeclaringType?.Assembly.GetName().Name?.StartsWith("Avalonia", StringComparison.Ordinal) == false)
                {
                    selfRendered.Add(visual.GetType().Name);
                }
            }

            templated.Background = Marker;
            Dispatcher.UIThread.RunJobs();

            List<string> takers = [];
            List<string> standIns = [];
            List<string> painters = [];
            foreach ((Visual visual, string property, IBrush? before) in drawers)
            {
                IBrush? after = Read(visual, property);
                string name = $"{visual.GetType().Name}.{property}";
                if (ReferenceEquals(after, Marker))
                {
                    if (ReferenceEquals(before, start))
                    {
                        takers.Add(name);
                    }
                    else
                    {
                        standIns.Add(name + "=" + Shown(before));
                    }
                }
                else if (before is not null)
                {
                    painters.Add(name + "=" + Shown(before));
                }
                else if (after is not null)
                {
                    // Null before and a brush other than the marker after: derived from the row's, as a converter derives it.
                    standIns.Add(name + "=" + Shown(after) + " from the row's");
                }
            }

            bool c1 = start is null && takers.Count > 0;
            bool c2 = takers.Count == 0 && standIns.Count == 0 && painters.Count == 0 && selfRendered.Count == 0;
            return $"start {(start is null ? "null" : Shown(start)),-12} C1 {(c1 ? "yes" : "no "),-3} C2 {(c2 ? "yes" : "no "),-3} "
                   + $"takers [{string.Join(", ", takers)}] standIns [{string.Join(", ", standIns)}] painters [{string.Join(", ", painters)}]"
                   + (selfRendered.Count > 0 ? $" selfRendered [{string.Join(", ", selfRendered)}]" : string.Empty);
        }
        finally
        {
            if (combo is not null)
            {
                combo.IsDropDownOpen = false;
                Dispatcher.UIThread.RunJobs();
            }

            window?.Close();
            Dispatcher.UIThread.RunJobs();
            if (added is not null)
            {
                Application.Current!.DataTemplates.Remove(added);
            }
        }
    }

    /// <summary>
    /// The visuals of a row's own template, at any depth, with what the templated controls in it draw by
    /// their own templates; not what a presenter shows through a template, nor what an items presenter holds.
    /// </summary>
    private static IEnumerable<Visual> TemplateVisuals(Visual row)
    {
        foreach (Visual child in row.GetVisualChildren())
        {
            foreach (Visual visual in AndBelow(child))
            {
                yield return visual;
            }
        }
    }

    private static IEnumerable<Visual> AndBelow(Visual visual)
    {
        yield return visual;
        if (visual is ItemsPresenter || (visual is ContentPresenter presenter && presenter.Content is not Control))
        {
            yield break;
        }

        foreach (Visual child in visual.GetVisualChildren())
        {
            foreach (Visual below in AndBelow(child))
            {
                yield return below;
            }
        }
    }

    /// <summary>The property through which a visual draws a brush of its own, when its type draws one.</summary>
    private static string? Painted(Visual visual) => visual switch
    {
        Shape => "Fill",
        Border or Panel or ContentPresenter or TextPresenter or TextBlock => "Background",
        _ => null,
    };

    private static IBrush? Read(Visual visual, string property) => visual switch
    {
        Shape shape => shape.Fill,
        Border border => border.Background,
        Panel panel => panel.Background,
        ContentPresenter presenter => presenter.Background,
        TextPresenter text => text.Background,
        TextBlock block => block.Background,
        _ => null,
    };

    private static string Shown(IBrush? brush) => brush switch
    {
        null => "null",
        ISolidColorBrush solid => solid.Color.A == 0 ? "Transparent" : solid.Color.ToString(),
        _ => brush.GetType().Name,
    };
}
