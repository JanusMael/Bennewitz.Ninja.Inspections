using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FpProbe6.Alt;

// Judge 14: consumer types whose local names are framework types', each with an attached property named Background
// of its own, which is not Border's. Markup names them with the prefix alt, as a consumer's own xmlns would.

/// <summary>A consumer attached-property holder named TemplatedControl.</summary>
public sealed class TemplatedControl : AvaloniaObject
{
    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, Control, IBrush?>("Background");

    public static IBrush? GetBackground(Control control) => control.GetValue(BackgroundProperty);

    public static void SetBackground(Control control, IBrush? value) => control.SetValue(BackgroundProperty, value);
}

/// <summary>A consumer attached-property holder named Panel.</summary>
public sealed class Panel : AvaloniaObject
{
    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<Panel, Control, IBrush?>("Background");

    public static IBrush? GetBackground(Control control) => control.GetValue(BackgroundProperty);

    public static void SetBackground(Control control, IBrush? value) => control.SetValue(BackgroundProperty, value);
}
