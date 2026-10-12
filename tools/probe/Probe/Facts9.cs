using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FpProbe6;

// Judge 9's facts pass: Facts.cs's measurement (the first row's Background as it starts; the template elements
// whose Background or Fill takes a marker given to the row and held the row's value before; those holding a
// brush of their own; stand-ins), with three changes: the host is the list named Inner when there is one; each
// element is listed with its type and name; and each taker is checked in a rendered frame, at three quarters of
// its width and half its height, for the marker's colour (drawn) or not.
internal static partial class Facts9
{
    private static readonly Color MarkerColor = Color.FromArgb(255, 1, 2, 3);
    private static readonly SolidColorBrush Marker = new(MarkerColor);

    public static void Run(string? only)
    {
        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only || (only.EndsWith('*') && c.Id.StartsWith(only.TrimEnd('*'), StringComparison.Ordinal))))
        {
            string result;
            try
            {
                result = Measure(c);
            }
            catch (Exception e)
            {
                result = $"threw {e.GetType().Name}: {e.Message.Split('\n')[0]}";
            }

            Console.WriteLine($"{c.Id,-6} {result}");
        }
    }

    /// <summary>Which of a few types the running theme gives an implicit ControlTheme, and what its template's root is.</summary>
    public static void Themes()
    {
        foreach (Type type in (Type[])[typeof(ContentControl), typeof(UserControl), typeof(Border), typeof(Panel), typeof(ContentPresenter), typeof(ListBoxItem), typeof(ComboBoxItem), typeof(TabItem)])
        {
            bool found = Application.Current!.TryGetResource(type, ThemeVariant.Default, out object? value);
            string what = found && value is ControlTheme theme
                ? "ControlTheme, Template " + (theme.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == TemplatedControl.TemplateProperty)?.Value?.GetType().Name ?? "none")
                : found ? value?.GetType().Name ?? "null" : "none";
            Console.WriteLine($"theme for {type.Name,-16} {what}");
        }
    }

    private static string Measure(Case c)
    {
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
                return $"load threw {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message.Split('\n')[0]}";
            }

            ItemsControl? host = root.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(x => x.Name == "Inner") ?? root as ItemsControl;
            if (host is null)
            {
                return $"unknown host {root.GetType().Name}";
            }

            host.ItemsSource = new[] { new Row("Alpha"), new Row("Beta"), new Row("Gamma") };
            window = new Window { Width = 400, Height = 300, Content = root };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            if (host is ComboBox comboBox)
            {
                combo = comboBox;
                combo.IsDropDownOpen = true;
                Dispatcher.UIThread.RunJobs();
            }

            Control? row = host is TabControl tabs ? tabs.ContainerFromIndex(1) : host.ContainerFromIndex(0);
            if (row is not TemplatedControl templated)
            {
                return "no row realised";
            }

            IBrush? start = templated.Background;
            List<(Visual Visual, string Property, IBrush? Before)> drawers = [];
            foreach (Visual visual in TemplateVisuals(templated))
            {
                if (Painted(visual) is { } property)
                {
                    drawers.Add((visual, property, Read(visual)));
                }
            }

            templated.Background = Marker;
            Dispatcher.UIThread.RunJobs();
            TopLevel top = TopLevel.GetTopLevel(templated)!;
            using WriteableBitmap? frame = top.CaptureRenderedFrame();

            List<string> takers = [];
            List<string> standIns = [];
            List<string> painters = [];
            foreach ((Visual visual, string property, IBrush? before) in drawers)
            {
                IBrush? after = Read(visual);
                string name = $"{visual.GetType().Name}{(visual is StyledElement { Name: { } n } ? "#" + n : string.Empty)}.{property}";
                if (ReferenceEquals(after, Marker))
                {
                    if (ReferenceEquals(before, start))
                    {
                        takers.Add(name + " " + Pixel(frame, visual, top));
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
                    standIns.Add(name + "=" + Shown(after) + " from the row's");
                }
            }

            bool c1 = start is null && takers.Count > 0;
            bool c2 = takers.Count == 0 && standIns.Count == 0 && painters.Count == 0;
            return $"start {(start is null ? "null" : Shown(start)),-12} C1 {(c1 ? "yes" : "no "),-3} C2 {(c2 ? "yes" : "no "),-3} "
                   + $"takers [{string.Join(", ", takers)}] standIns [{string.Join(", ", standIns)}] painters [{string.Join(", ", painters)}]";
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
        }
    }
}
