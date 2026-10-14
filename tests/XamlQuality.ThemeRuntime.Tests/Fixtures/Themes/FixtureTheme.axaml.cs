namespace XamlQuality.ThemeRuntime.Tests;

using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

/// <summary>The fixture theme the tests audit as markup and build at run time; see FixtureTheme.axaml.</summary>
public partial class FixtureTheme : Styles
{
    /// <summary>The key of the resource the theme adds in code, where the audit cannot see it.</summary>
    public const string CodeAdded = "CodeAdded";

    public FixtureTheme()
    {
        AvaloniaXamlLoader.Load(this);
        Resources[CodeAdded] = new SolidColorBrush(Colors.Teal);
    }
}

/// <summary>The fixture theme's own variant, which inherits Dark, as Semi's high-contrast variants do.</summary>
public static class FixtureVariants
{
    /// <summary>Contrast, whose <c>InheritVariant</c> is Dark.</summary>
    public static ThemeVariant Contrast { get; } = new("Contrast", ThemeVariant.Dark);
}
