using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>Judge 10: a Border, not a UserControl, whose own x:Class markup sets its Background Transparent.</summary>
public partial class XClassBorder : Border
{
    public XClassBorder() => AvaloniaXamlLoader.Load(this);
}

/// <summary>Judge 10: a Panel, not a UserControl, whose own x:Class markup sets its Background Transparent.</summary>
public partial class XClassPanel : Panel
{
    public XClassPanel() => AvaloniaXamlLoader.Load(this);
}

/// <summary>Judge 10: a view whose own markup holds a Border with IsEnabled False.</summary>
public partial class DisabledPartView : UserControl
{
    public DisabledPartView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>Judge 10: the same view with the Border enabled.</summary>
public partial class EnabledPartView : UserControl
{
    public EnabledPartView() => AvaloniaXamlLoader.Load(this);
}
