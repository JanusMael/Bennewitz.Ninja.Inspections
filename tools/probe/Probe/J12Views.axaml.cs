using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FpProbe6;

/// <summary>Judge 12: a view, compiled at build time, holding M01's list: a Setter names IsEnabled in a Setter.Property element.</summary>
public partial class SetterElementView : UserControl
{
    public SetterElementView() => AvaloniaXamlLoader.Load(this);
}

/// <summary>Judge 12: a view, compiled at build time, holding M04's list: a row theme's Setter names IsSelected in a Setter.Property element.</summary>
public partial class SetterElementSelectView : UserControl
{
    public SetterElementSelectView() => AvaloniaXamlLoader.Load(this);
}
