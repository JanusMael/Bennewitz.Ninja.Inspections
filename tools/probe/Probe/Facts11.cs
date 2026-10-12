using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace FpProbe6;

// Round 11's facts pass: Facts9's measurement of one row, made for each of the first three rows in turn, each on a
// fresh window, so a row that starts selected is told apart from the rest. A tab control's rows are its tabs.
internal static partial class Facts9
{
    public static void RunRows(string? only)
    {
        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only || (only.EndsWith('*') && c.Id.StartsWith(only.TrimEnd('*'), StringComparison.Ordinal))))
        {
            for (int index = 0; index < 3; index++)
            {
                string result;
                try
                {
                    result = MeasureRow(c, index);
                }
                catch (Exception e)
                {
                    result = $"threw {e.GetType().Name}: {e.Message.Split('\n')[0]}";
                }

                Console.WriteLine($"{c.Id,-6} row{index} {result}");
            }
        }
    }

    private static string MeasureRow(Case c, int index)
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

            ItemsControl? host = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(root).OfType<ItemsControl>().FirstOrDefault(x => x.Name == "Inner")
                                 ?? root as ItemsControl;
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

            if (host.ContainerFromIndex(index) is not TemplatedControl templated)
            {
                return "no row realised";
            }

            string state = (templated is ListBoxItem { IsSelected: true } || templated is TabItem { IsSelected: true } ? "selected " : string.Empty)
                           + (templated.IsEffectivelyEnabled ? string.Empty : "disabled");
            IBrush? start = templated.Background;
            List<(Avalonia.Visual Visual, string Property, IBrush? Before)> drawers = [];
            foreach (Avalonia.Visual visual in TemplateVisuals(templated))
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
            foreach ((Avalonia.Visual visual, string property, IBrush? before) in drawers)
            {
                IBrush? after = Read(visual);
                string name = $"{visual.GetType().Name}{(visual is Avalonia.StyledElement { Name: { } n } ? "#" + n : string.Empty)}.{property}";
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
                   + $"[{state.Trim()}] takers [{string.Join(", ", takers)}] standIns [{string.Join(", ", standIns)}] painters [{string.Join(", ", painters)}]";
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
