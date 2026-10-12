using Avalonia;
using Avalonia.Media;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://judge15.example/alt", "FpProbe6.Alt15")]

namespace FpProbe6.Alt15;

// Judge 15: consumer types whose local names are framework types', in a namespace a consumer's own XmlnsDefinition maps
// to a URI, as a control library's is. Each holds an attached property of its own, which is not the framework's.
// Markup names them with the prefix u (the URI) or a15 (clr-namespace) or au (using:).

/// <summary>A consumer attached-property holder named TemplatedControl.</summary>
public sealed class TemplatedControl : AvaloniaObject
{
    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, Avalonia.Controls.Control, IBrush?>("Background");

    public static IBrush? GetBackground(Avalonia.Controls.Control control) => control.GetValue(BackgroundProperty);

    public static void SetBackground(Avalonia.Controls.Control control, IBrush? value) => control.SetValue(BackgroundProperty, value);
}

/// <summary>A consumer attached-property holder named Panel.</summary>
public sealed class Panel : AvaloniaObject
{
    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<Panel, Avalonia.Controls.Control, IBrush?>("Background");

    public static IBrush? GetBackground(Avalonia.Controls.Control control) => control.GetValue(BackgroundProperty);

    public static void SetBackground(Avalonia.Controls.Control control, IBrush? value) => control.SetValue(BackgroundProperty, value);
}

/// <summary>A consumer attached-property holder named TextBlock.</summary>
public sealed class TextBlock : AvaloniaObject
{
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, Avalonia.Controls.Control, string?>("Text");

    public static string? GetText(Avalonia.Controls.Control control) => control.GetValue(TextProperty);

    public static void SetText(Avalonia.Controls.Control control, string? value) => control.SetValue(TextProperty, value);
}

/// <summary>A consumer attached-property holder named AutomationProperties, with a Name of its own.</summary>
public sealed class AutomationProperties : AvaloniaObject
{
    public static readonly AttachedProperty<string?> NameProperty =
        AvaloniaProperty.RegisterAttached<AutomationProperties, Avalonia.Controls.Control, string?>("Name");

    public static string? GetName(Avalonia.Controls.Control control) => control.GetValue(NameProperty);

    public static void SetName(Avalonia.Controls.Control control, string? value) => control.SetValue(NameProperty, value);
}

/// <summary>
/// A consumer class named InputElement whose Focusable is a CLR attached property, its accessors storing a flag of its
/// own; it declares no FocusableProperty field, so it does not pass for a focusable type.
/// </summary>
public static class InputElement
{
    public static readonly AttachedProperty<bool> FocusFlagProperty =
        AvaloniaProperty.RegisterAttached<Avalonia.Controls.Control, bool>("FocusFlag", typeof(InputElement));

    public static bool GetFocusable(Avalonia.Controls.Control control) => control.GetValue(FocusFlagProperty);

    public static void SetFocusable(Avalonia.Controls.Control control, bool value) => control.SetValue(FocusFlagProperty, value);
}

/// <summary>A consumer attached-property holder named Control, with a Focusable of its own, which takes a binding.</summary>
public sealed class Control : AvaloniaObject
{
    public static readonly AttachedProperty<bool> FocusableProperty =
        AvaloniaProperty.RegisterAttached<Control, Avalonia.Controls.Control, bool>("Focusable");

    public static bool GetFocusable(Avalonia.Controls.Control control) => control.GetValue(FocusableProperty);

    public static void SetFocusable(Avalonia.Controls.Control control, bool value) => control.SetValue(FocusableProperty, value);
}
