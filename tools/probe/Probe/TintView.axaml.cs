using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>Judge 9: a compiled view whose own markup gives its root Background Transparent.</summary>
public partial class TintView : UserControl
{
    public TintView() => AvaloniaXamlLoader.Load(this);
}
