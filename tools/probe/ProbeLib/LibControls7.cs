using Avalonia.Controls;
using Avalonia.Media;

namespace ProbeLib;

/// <summary>A library base view that sets its Background in a handler capturing a local, a closure nested in this type.</summary>
public class LibLambdaBase : UserControl
{
    public LibLambdaBase()
    {
        IBrush brush = Brushes.Transparent;
        AttachedToVisualTree += (_, _) => Background = brush;
    }
}

/// <summary>A generic library base view that sets its Background in its constructor.</summary>
public class LibGenericBase<T> : UserControl
{
    public LibGenericBase() => Background = Brushes.Transparent;

    public T? Model { get; set; }
}
