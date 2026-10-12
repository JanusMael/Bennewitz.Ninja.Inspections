using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>Round 10: a compiled view whose markup holds a TintView given Background {x:Null}.</summary>
public partial class HolderView : UserControl
{
    public HolderView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>Round 10: a compiled view whose markup holds a TintView given nothing.</summary>
public partial class HolderPlainView : UserControl
{
    public HolderPlainView() => AvaloniaXamlLoader.Load(this);
}
