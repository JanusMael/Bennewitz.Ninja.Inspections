using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>A compiled view whose own markup holds a Transparent Border 600 wide.</summary>
public partial class WideView : UserControl
{
    public WideView() => AvaloniaXamlLoader.Load(this);
}
