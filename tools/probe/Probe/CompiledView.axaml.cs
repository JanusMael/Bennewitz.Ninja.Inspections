using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>A view whose markup is compiled, as an application's are: it paints only a badge at its right.</summary>
public partial class CompiledView : UserControl
{
    public CompiledView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>A view whose markup is compiled and sets no Background anywhere.</summary>
public partial class CompiledPlainView : UserControl
{
    public CompiledPlainView() => AvaloniaXamlLoader.Load(this);
}
