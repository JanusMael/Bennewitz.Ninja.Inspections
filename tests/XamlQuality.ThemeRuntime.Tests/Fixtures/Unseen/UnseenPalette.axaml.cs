namespace XamlQuality.ThemeRuntime.Tests;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

/// <summary>A palette UnseenRedefinitionDrawnTests' theme holds by its element, created with <c>new</c>; see UnseenPalette.axaml.</summary>
public partial class UnseenPalette : ResourceDictionary
{
    public UnseenPalette()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

/// <summary>ClassTheme.axaml's palette, whose read its own chain lacks; see UnseenOutsidePalette.axaml.</summary>
public partial class UnseenOutsidePalette : ResourceDictionary
{
    public UnseenOutsidePalette()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

/// <summary>The control whose implicit theme a consumer's own may hide, or be based on, in UnseenRedefinitionDrawnTests.</summary>
public sealed class UnseenProbe : Avalonia.Controls.Primitives.TemplatedControl
{
}
