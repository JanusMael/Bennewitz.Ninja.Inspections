using Avalonia;
using Avalonia.Media;

// Judge 16: the presentation URIs of WPF and MAUI, which no Avalonia assembly maps at 12.1.3, mapped by a consumer's
// own XmlnsDefinition, as any other URI a control library declares is.
[assembly: Avalonia.Metadata.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "FpProbe6.Alt16W")]
[assembly: Avalonia.Metadata.XmlnsDefinition("http://schemas.microsoft.com/dotnet/2021/maui", "FpProbe6.Alt16W")]

namespace FpProbe6.Alt16W
{
    // Consumer attached-property holders named as framework types are, in the namespace the two URIs map to.

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

    /// <summary>A consumer class named InputElement whose Focusable is a CLR attached property storing a flag of its own.</summary>
    public static class InputElement
    {
        public static readonly AttachedProperty<bool> FocusFlagProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.Control, bool>("FocusFlag16", typeof(InputElement));

        public static bool GetFocusable(Avalonia.Controls.Control control) => control.GetValue(FocusFlagProperty);

        public static void SetFocusable(Avalonia.Controls.Control control, bool value) => control.SetValue(FocusFlagProperty, value);
    }
}

namespace FpProbe6.Alt16
{
    /// <summary>
    /// Judge 16: a consumer list named as a framework base in its own chain (Avalonia's SelectingItemsControl), whose
    /// public FocusableProperty field hides InputElement's with another property; its CLR Focusable is InputElement's.
    /// </summary>
    public class SelectingItemsControl : Avalonia.Controls.ListBox
    {
        public new static readonly StyledProperty<bool> FocusableProperty =
            AvaloniaProperty.Register<SelectingItemsControl, bool>("Focusable");
    }

    /// <summary>
    /// Judge 16: a consumer row named as a framework base in its own chain (Avalonia's ContentControl), whose public
    /// BackgroundProperty field hides TemplatedControl's with another property; its CLR Background is TemplatedControl's.
    /// </summary>
    public class ContentControl : Avalonia.Controls.ListBoxItem
    {
        public new static readonly StyledProperty<IBrush?> BackgroundProperty =
            AvaloniaProperty.Register<ContentControl, IBrush?>("Background");
    }

    /// <summary>Judge 16: a list whose rows are the ContentControl above, with ListBox's style key.</summary>
    public class ContentControlList : Avalonia.Controls.ListBox
    {
        protected override Type StyleKeyOverride => typeof(Avalonia.Controls.ListBox);

        protected override Avalonia.Controls.Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new ContentControl();
    }
}

namespace FpProbe6.J16A
{
    /// <summary>Judge 16: one of two ListBoxItem subclasses that share a name.</summary>
    public class AmbRow16 : Avalonia.Controls.ListBoxItem;

    /// <summary>Judge 16: one of two ListBox subclasses that share a name.</summary>
    public class AmbList16 : Avalonia.Controls.ListBox;
}

namespace FpProbe6.J16B
{
    /// <summary>Judge 16: the other ListBoxItem subclass of the name.</summary>
    public class AmbRow16 : Avalonia.Controls.ListBoxItem;

    /// <summary>Judge 16: the other ListBox subclass of the name.</summary>
    public class AmbList16 : Avalonia.Controls.ListBox;
}
