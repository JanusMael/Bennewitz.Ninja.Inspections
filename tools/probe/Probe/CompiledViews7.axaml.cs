using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>A compiled view whose markup paints only inside a DataTemplate it never uses.</summary>
public partial class CompiledTemplateView : UserControl
{
    public CompiledTemplateView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>A compiled view whose markup paints only inside a ControlTheme it never uses.</summary>
public partial class CompiledThemeView : UserControl
{
    public CompiledThemeView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>A compiled view that paints only a badge at its right, through a DataTemplate in its resources.</summary>
public partial class CompiledBadgeView : UserControl
{
    public CompiledBadgeView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>A compiled view whose markup paints only inside the item template of an empty list it holds.</summary>
public partial class CompiledListView : UserControl
{
    public CompiledListView() => AvaloniaXamlLoader.Load(this);
}
