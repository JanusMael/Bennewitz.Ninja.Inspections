using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FpProbe6;

/// <summary>Judge 13: a command that counts its runs, for an F5 key binding.</summary>
public sealed class Count13 : ICommand
{
    public int Runs { get; set; }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => Runs++;
}

/// <summary>Judge 13: the command the cases' key bindings name by x:Static.</summary>
public static class Diag13Commands
{
    public static Count13 Count { get; } = new();
}

/// <summary>Judge 13: the data context the markup's own bindings read.</summary>
public sealed class Model13
{
    public Row[] Rows { get; } = [new("Alpha"), new("Beta"), new("Gamma")];

    /// <summary>A selection a binding reads: the first row.</summary>
    public int Zero => 0;
}

internal static class Diag13
{
    public static void Run(string? only) => Facts9.Bound(only);
}

// Judge 13's bound pass. The case's markup is loaded at run time, as the facts pass loads it, and given a data context
// whose Rows its own bindings read, so the rows are the ones its markup generates: nothing is set in code. For the
// list: where its ItemsSource came from, how many rows are realised, its ItemContainerTheme (by resource key), and
// whether it takes focus; where it has key bindings, whether F5 runs the command with focus put on the first thing in
// the binding's path that takes it, the list, a row, or what a row's template holds. For each of the first three rows:
// its theme, the name its automation peer reports, whether it and what its template holds take focus, and the facts
// pass's drawing facts (start, takers, stand-ins, painters), read on the same rows.
internal static partial class Facts9
{
    private static readonly SolidColorBrush BoundMarker = new(Color.FromArgb(255, 1, 2, 3));

    public static void Bound(string? only)
    {
        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only || (only.EndsWith('*') && c.Id.StartsWith(only.TrimEnd('*'), StringComparison.Ordinal))))
        {
            List<string> lines;
            try
            {
                lines = MeasureBound(c);
            }
            catch (Exception e)
            {
                lines = [$"threw {e.GetType().Name}: {e.Message.Split('\n')[0]}"];
            }

            foreach (string line in lines)
            {
                Console.WriteLine($"{c.Id,-6} {line}");
            }
        }
    }

    private static List<string> MeasureBound(Case c)
    {
        List<string> lines = [];
        Window? window = null;
        try
        {
            Control root;
            try
            {
                root = (Control)Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(c.Markup, typeof(Program).Assembly);
            }
            catch (Exception e)
            {
                lines.Add($"load threw {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message.Split('\n')[0]}");
                return lines;
            }

            Model13 model = new();
            root.DataContext = model;
            window = new Window { Width = 400, Height = 300, Content = root };
            window.Show();
            window.Activate();
            Dispatcher.UIThread.RunJobs();

            ItemsControl? host = root.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(x => x.Name == "Inner") ?? root as ItemsControl;
            if (host is null)
            {
                lines.Add($"unknown host {root.GetType().Name}");
                return lines;
            }

            string source = host.ItemsSource is null ? "null"
                : ReferenceEquals(host.ItemsSource, model.Rows) ? "the model's Rows (the markup's binding)"
                : host.ItemsSource.GetType().Name;
            int realised = Enumerable.Range(0, 3).Count(i => host.ContainerFromIndex(i) is not null);
            lines.Add($"list {host.GetType().Name}: ItemsSource {source}; rows realised {realised}; ItemContainerTheme {KeyOf(host, host.ItemContainerTheme)}; "
                      + $"ItemTemplate {(host.ItemTemplate is null ? "null" : "set")}; Focusable {host.Focusable}");

            // Focus: the list, each row, and what each row's template holds, in the key binding's path.
            List<(string What, InputElement Element)> path = [("the list", host)];
            for (int index = 0; index < 3; index++)
            {
                if (host.ContainerFromIndex(index) is not { } row)
                {
                    continue;
                }

                path.Add(($"row{index}", row));
                foreach (Visual visual in row.GetVisualDescendants())
                {
                    if (visual is InputElement inner && inner.Focusable)
                    {
                        path.Add(($"row{index} {inner.GetType().Name}", inner));
                    }
                }
            }

            List<string> takesFocus = [];
            foreach ((string what, InputElement element) in path)
            {
                if (element.Focusable && element.IsEffectivelyEnabled && element.IsEffectivelyVisible && element.Focus() && element.IsFocused)
                {
                    takesFocus.Add(what);
                }
            }

            lines.Add($"focus: takes focus [{string.Join(", ", takesFocus)}]");

            if (host.KeyBindings.Count > 0)
            {
                Diag13Commands.Count.Runs = 0;
                (string What, InputElement Element)? first = null;
                foreach ((string What, InputElement Element) entry in path)
                {
                    if (takesFocus.Contains(entry.What))
                    {
                        first = entry;
                        break;
                    }
                }

                if (first is { } target)
                {
                    target.Element.Focus();
                }
                else
                {
                    window.Focus();
                }

                Dispatcher.UIThread.RunJobs();
                window.KeyPressQwerty(PhysicalKey.F5, RawInputModifiers.None);
                window.KeyReleaseQwerty(PhysicalKey.F5, RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                lines.Add($"key binding: focus on {(first is { } on ? on.What : "nothing in its path")}; F5 ran the command {Diag13Commands.Count.Runs} time(s)");
            }

            for (int index = 0; index < 3; index++)
            {
                if (host.ContainerFromIndex(index) is not TemplatedControl row)
                {
                    lines.Add($"row{index} not realised");
                    continue;
                }

                string name = ControlAutomationPeer.CreatePeerForElement(row).GetName() ?? "(null)";
                string item = host.ItemsSource is System.Collections.IList list && index < list.Count ? list[index]?.ToString() ?? "null" : "?";
                string state = (row is ListBoxItem { IsSelected: true } ? "selected " : string.Empty) + (row.IsEffectivelyEnabled ? string.Empty : "disabled");
                lines.Add($"row{index} {row.GetType().Name}: theme {KeyOf(host, row.Theme)}; peer name \"{name}\" (item ToString \"{item}\"); "
                          + $"[{state.Trim()}] {Drawing(row, window)}");
            }
        }
        finally
        {
            window?.Close();
            Dispatcher.UIThread.RunJobs();
        }

        return lines;
    }

    /// <summary>The facts pass's reading of one row: start, takers, stand-ins and painters, with the row's own value restored after.</summary>
    private static string Drawing(TemplatedControl row, TopLevel top)
    {
        IBrush? start = row.Background;
        List<(Visual Visual, string Property, IBrush? Before)> drawers = [];
        foreach (Visual visual in TemplateVisuals(row))
        {
            if (Painted(visual) is { } property)
            {
                drawers.Add((visual, property, Read(visual)));
            }
        }

        row.Background = BoundMarker;
        Dispatcher.UIThread.RunJobs();
        using WriteableBitmap? frame = top.CaptureRenderedFrame();

        List<string> takers = [];
        List<string> standIns = [];
        List<string> painters = [];
        foreach ((Visual visual, string property, IBrush? before) in drawers)
        {
            IBrush? after = Read(visual);
            string name = $"{visual.GetType().Name}{(visual is StyledElement { Name: { } n } ? "#" + n : string.Empty)}.{property}";
            if (ReferenceEquals(after, BoundMarker))
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

        row.ClearValue(TemplatedControl.BackgroundProperty);
        Dispatcher.UIThread.RunJobs();

        bool c1 = start is null && takers.Count > 0;
        bool c2 = takers.Count == 0 && standIns.Count == 0 && painters.Count == 0;
        return $"start {(start is null ? "null" : Shown(start))} C1 {(c1 ? "yes" : "no")} C2 {(c2 ? "yes" : "no")} "
               + $"takers [{string.Join(", ", takers)}] standIns [{string.Join(", ", standIns)}] painters [{string.Join(", ", painters)}]";
    }

    /// <summary>The key, in the list's resources, of a theme; "inline" for one with none there; "none" for null.</summary>
    private static string KeyOf(ItemsControl host, ControlTheme? theme)
    {
        if (theme is null)
        {
            return "none";
        }

        foreach (object key in host.Resources.Keys)
        {
            if (host.Resources.TryGetResource(key, null, out object? value) && ReferenceEquals(value, theme))
            {
                return key is Type type ? "{x:Type " + type.Name + "}" : key.ToString() ?? "?";
            }
        }

        return "inline";
    }
}
