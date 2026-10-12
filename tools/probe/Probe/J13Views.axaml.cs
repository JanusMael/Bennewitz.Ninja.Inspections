using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>Judge 13: a view, compiled at build time, holding N01's list: its source written ComboBox.ItemsSource.</summary>
public partial class N13SourceView : UserControl
{
    public N13SourceView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>Judge 13: a view, compiled at build time, holding N10's list: a template Panel written Border.Background.</summary>
public partial class N13PanelView : UserControl
{
    public N13PanelView() => AvaloniaXamlLoader.Load(this);
}
