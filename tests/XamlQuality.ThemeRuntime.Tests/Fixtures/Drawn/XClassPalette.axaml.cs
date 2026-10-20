namespace XamlQuality.ThemeRuntime.Tests;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

/// <summary>A dictionary a theme holds by its element, created with <c>new</c>; see XClassPalette.axaml.</summary>
public partial class XClassPalette : ResourceDictionary
{
    public XClassPalette()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
