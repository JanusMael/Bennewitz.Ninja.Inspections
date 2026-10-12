using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace FpProbe6;

// Judge 12's diagnostic: loads a case's markup as the facts pass does and prints its logical tree, so a case whose
// host is not found can be told apart from one whose markup does not load.
internal static class Diag12
{
    public static void Run(string? only)
    {
        foreach (Case c in Cases.Every.Where(c => only is null || c.Id == only))
        {
            try
            {
                Control root = (Control)Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(c.Markup, typeof(Program).Assembly);
                Console.WriteLine($"{c.Id} root {root.GetType().FullName}");
                Window window = new() { Width = 400, Height = 300, Content = root };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                Print(root, 1);
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{c.Id} threw {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
        }
    }

    private static void Print(ILogical node, int depth)
    {
        string name = node is Avalonia.StyledElement { Name: { } n } ? "#" + n : string.Empty;
        Console.WriteLine(new string(' ', depth * 2) + node.GetType().Name + name);
        if (depth > 6)
        {
            return;
        }

        foreach (ILogical child in node.LogicalChildren)
        {
            Print(child, depth + 1);
        }
    }
}
