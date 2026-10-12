using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FpProbe6;

// Judge 10's diagnosis: for the first row of each case, every Border, Panel and ContentPresenter of its
// template with its TemplatedParent, its logical and visual ancestors, its IsEffectivelyEnabled and its
// Background; and the row's own logical parent, IsEffectivelyEnabled, pseudo-classes and Background.
internal static class Diag10
{
    public static void Run(string? only)
    {
        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only || (only.EndsWith('*') && c.Id.StartsWith(only.TrimEnd('*'), StringComparison.Ordinal))))
        {
            Window? window = null;
            try
            {
                Control root = (Control)Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(c.Markup, typeof(Program).Assembly);
                ItemsControl host = root.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(x => x.Name == "Inner") ?? (ItemsControl)root;
                host.ItemsSource = new[] { new Row("Alpha"), new Row("Beta"), new Row("Gamma") };
                window = new Window { Width = 400, Height = 300, Content = root };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                if (host.ContainerFromIndex(0) is not ListBoxItem row)
                {
                    Console.WriteLine($"{c.Id}: no row");
                    continue;
                }

                Console.WriteLine($"{c.Id}: row enabled={row.IsEffectivelyEnabled} classes=[{string.Join(" ", row.Classes)}] "
                                  + $"background={Shown(row.Background)} logicalParent={Name(row.GetLogicalParent())}");
                foreach (Visual visual in row.GetVisualDescendants())
                {
                    if (visual is not (Border or Panel or Avalonia.Controls.Presenters.ContentPresenter))
                    {
                        continue;
                    }

                    StyledElement element = (StyledElement)visual;
                    IBrush? brush = visual switch
                    {
                        Border border => border.Background,
                        Panel panel => panel.Background,
                        Avalonia.Controls.Presenters.ContentPresenter presenter => presenter.Background,
                        _ => null,
                    };
                    string logical = string.Join(" > ", element.GetLogicalAncestors().Take(4).Select(Name));
                    string visualUp = string.Join(" > ", visual.GetVisualAncestors().Take(4).Select(v => Name(v as StyledElement)));
                    bool enabled = visual is Avalonia.Input.InputElement input && input.IsEffectivelyEnabled;
                    Console.WriteLine($"    {Name(element)} templatedParent={Name(element.TemplatedParent as StyledElement)} enabled={enabled} "
                                      + $"background={Shown(brush)} logical: {logical} | visual: {visualUp}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{c.Id}: threw {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
            finally
            {
                window?.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }
    }

    private static string Name(object? element) => element switch
    {
        null => "null",
        StyledElement { Name: { Length: > 0 } name } styled => styled.GetType().Name + "#" + name,
        _ => element.GetType().Name,
    };

    private static string Shown(IBrush? brush) => brush switch
    {
        null => "null",
        ISolidColorBrush solid => solid.Color.A == 0 ? "Transparent" : solid.Color.ToString(),
        _ => brush.GetType().Name,
    };
}
