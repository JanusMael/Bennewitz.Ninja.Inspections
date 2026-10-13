using System.Collections.Concurrent;
using Avalonia.Automation.Peers;

// A miniature framework for KeyBindingFocusRuleTests, ItemContainerNameRuleTests and
// ItemContainerBackgroundRuleTests. The rules read a framework through reflection alone, so these are
// shaped like one: a FocusableProperty, a BackgroundProperty, a shape's FillProperty and a list's
// SelectionModeProperty whose per-type defaults are registered by static constructors, items controls
// that construct their containers, a tab that is a headered content control, and containers that
// construct their automation peers, which are in ContainerNameFakes.cs. A property is the object its
// field holds, as the framework's is, so every type that inherits a field holds the same property: the
// Background a Border, a Panel and a row each hold, the ItemsSource of every list, the IsEnabled of every
// control, and the Text of a TextBlock and of a SelectableTextBlock. As the framework's are, the controls
// are AvaloniaObjects, the properties AvaloniaProperties, and each property the rules read after an owner
// has an instance CLR property declared beside its field, which is what the XAML compiler resolves.
//
// ⛔ Never instantiate these in a test. Construction runs the static constructors, and the rule has
// to run them itself: a read made before they run reports every control unfocusable, which is the
// trap the rule exists to get right, and a test that ran them first would hide it.
namespace XamlQuality.Tests.Rules.FocusFakes;

/// <summary>What every property derives from, found by its name, as the rules find a real framework's.</summary>
public abstract class AvaloniaProperty;

/// <summary>What every control derives from, found by its name, as the rules find a real framework's.</summary>
public class AvaloniaObject;

/// <summary>Shaped like a styled property: per-type defaults, looked up along the base chain.</summary>
public sealed class FakeFocusableProperty : AvaloniaProperty
{
    private readonly ConcurrentDictionary<Type, bool> _defaults = new();

    /// <summary>Registers a type's default, as a static constructor does.</summary>
    /// <param name="type">The type the default is for.</param>
    /// <param name="value">The default.</param>
    public void OverrideDefaultValue(Type type, bool value) => _defaults[type] = value;

    /// <summary>The metadata for <paramref name="type"/>, as registered so far.</summary>
    /// <param name="type">The type asked about.</param>
    /// <returns>Its metadata; the base default when nothing in its chain has registered one yet.</returns>
    public FakeMetadata GetMetadata(Type type)
    {
        for (Type? link = type; link is not null; link = link.BaseType)
        {
            if (_defaults.TryGetValue(link, out bool value))
            {
                return new FakeMetadata(value);
            }
        }

        return new FakeMetadata(false);
    }
}

/// <summary>Shaped like property metadata: a default value.</summary>
/// <param name="defaultValue">The default it carries.</param>
public sealed class FakeMetadata(bool defaultValue)
{
    /// <summary>The default.</summary>
    public bool DefaultValue { get; } = defaultValue;
}

/// <summary>Shaped like a styled property whose value is a brush: per-type defaults, looked up along the base chain.</summary>
public sealed class FakeBackgroundProperty : AvaloniaProperty
{
    private readonly ConcurrentDictionary<Type, object> _defaults = new();

    /// <summary>Registers a type's default, as a static constructor does.</summary>
    /// <param name="type">The type the default is for.</param>
    /// <param name="value">The default.</param>
    public void OverrideDefaultValue(Type type, object value) => _defaults[type] = value;

    /// <summary>The metadata for <paramref name="type"/>, as registered so far.</summary>
    /// <param name="type">The type asked about.</param>
    /// <returns>Its metadata; a null default when nothing in its chain has registered one yet.</returns>
    public FakeBackgroundMetadata GetMetadata(Type type)
    {
        for (Type? link = type; link is not null; link = link.BaseType)
        {
            if (_defaults.TryGetValue(link, out object? value))
            {
                return new FakeBackgroundMetadata(value);
            }
        }

        return new FakeBackgroundMetadata(null);
    }
}

/// <summary>Shaped like property metadata: a default value, which may be null.</summary>
/// <param name="defaultValue">The default it carries.</param>
public sealed class FakeBackgroundMetadata(object? defaultValue)
{
    /// <summary>The default.</summary>
    public object? DefaultValue { get; } = defaultValue;
}

/// <summary>Shaped like the styled property that holds how a list selects: per-type defaults, looked up along the base chain.</summary>
public sealed class FakeSelectionModeProperty : AvaloniaProperty
{
    private readonly ConcurrentDictionary<Type, string> _defaults = new();

    /// <summary>Registers a type's default, as a static constructor does.</summary>
    /// <param name="type">The type the default is for.</param>
    /// <param name="value">The default, as the framework's flags print.</param>
    public void OverrideDefaultValue(Type type, string value) => _defaults[type] = value;

    /// <summary>The metadata for <paramref name="type"/>, as registered so far.</summary>
    /// <param name="type">The type asked about.</param>
    /// <returns>Its metadata; <c>Single</c>, the framework's default, when nothing in its chain has registered one yet.</returns>
    public FakeBackgroundMetadata GetMetadata(Type type)
    {
        for (Type? link = type; link is not null; link = link.BaseType)
        {
            if (_defaults.TryGetValue(link, out string? value))
            {
                return new FakeBackgroundMetadata(value);
            }
        }

        return new FakeBackgroundMetadata("Single");
    }
}

/// <summary>Shaped like a property with no default a rule reads: which object a type's field holds is all a rule compares.</summary>
public sealed class FakeProperty : AvaloniaProperty;

/// <summary>
/// Shaped like a direct property: its <c>AddOwner</c> returns a copy, which the framework takes for the same
/// property, as <c>AvaloniaProperty.Equals</c> compares two by their id at 12.1.3.
/// </summary>
public sealed class FakeDirectProperty : AvaloniaProperty
{
    private static int s_next;

    /// <summary>A new property.</summary>
    public FakeDirectProperty() => Id = Interlocked.Increment(ref s_next);

    private FakeDirectProperty(int id) => Id = id;

    /// <summary>What the framework compares two properties by.</summary>
    public int Id { get; }

    /// <summary>A copy for another owner, as a direct property's <c>AddOwner</c> returns.</summary>
    /// <returns>The copy.</returns>
    public FakeDirectProperty AddOwner() => new(Id);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FakeDirectProperty other && other.Id == Id;

    /// <inheritdoc />
    public override int GetHashCode() => Id;
}

/// <summary>What every binding derives from, found by its name, as the rules find a real framework's.</summary>
public abstract class BindingBase;

/// <summary>Holds the attached properties automation reads, as the framework's does: none is a control's own.</summary>
public static class AutomationProperties
{
    /// <summary>Attached: another property than any a control holds.</summary>
    public static readonly FakeProperty NameProperty = new();

    /// <summary>Attached, likewise.</summary>
    public static readonly FakeProperty LabeledByProperty = new();
}

/// <summary>What a control that draws itself is handed to draw with; only its name matters.</summary>
public sealed class DrawingContext;

/// <summary>Where the focus model is declared.</summary>
public class InputElement : AvaloniaObject
{
    /// <summary>Found by name, as the rule finds a real framework's.</summary>
    public static readonly FakeFocusableProperty FocusableProperty = new();

    /// <summary>The one every control holds, as the framework's is.</summary>
    public static readonly FakeProperty IsEnabledProperty = new();

    /// <summary>Whether it takes focus; only the property's existence matters.</summary>
    public bool Focusable { get; set; }

    /// <summary>Whether it is enabled; only the property's existence matters.</summary>
    public bool IsEnabled { get; set; }
}

/// <summary>A control that cannot take focus.</summary>
public class Control : InputElement
{
    /// <summary>Found by name, as the rule finds a real framework's: null by default, as the framework's is.</summary>
    public static readonly FakeBackgroundProperty BackgroundProperty = new();

    /// <summary>Its background; only the setter matters, which code that sets it calls.</summary>
    public object? Background { get; set; }

    /// <summary>Shaped like the framework's: a peer no control-view search reaches.</summary>
    /// <returns>The peer.</returns>
    protected virtual AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}

/// <summary>A control drawn through a template it is given.</summary>
public class TemplatedControl : Control
{
    /// <summary>Its template; only the property's existence matters.</summary>
    public object? Template { get; set; }

    /// <summary>The key its implicit theme is found under, shaped like the framework's.</summary>
    protected virtual Type StyleKeyOverride => GetType();
}

/// <summary>The container an <see cref="ItemsControl"/> generates: not templated, and unfocusable.</summary>
public class ContentPresenter : Control;

/// <summary>A presenter derived from one, as the framework's scroll viewer's is.</summary>
public class ScrollContentPresenter : ContentPresenter;

/// <summary>A templated control that presents one piece of content, as the framework's rows and views do.</summary>
public class ContentControl : TemplatedControl;

/// <summary>A content control that hands its content to a presenter of its own, as the framework's transitioning one does.</summary>
public class TransitioningContentControl : ContentControl;

/// <summary>Presents items in containers it constructs.</summary>
public class ItemsControl : TemplatedControl
{
    /// <summary>The one every list holds, as the framework's is.</summary>
    public static readonly FakeProperty ItemsSourceProperty = new();

    /// <summary>The one every list holds, as the framework's is.</summary>
    public static readonly FakeProperty ItemContainerThemeProperty = new();

    /// <summary>The one every list holds, a direct property, as a selecting list's is in the framework.</summary>
    public static readonly FakeDirectProperty SelectedIndexProperty = new();

    /// <summary>Found by name, as the rule finds a real framework's: <c>Single</c> by default, and protected, as Avalonia's is at 12.1.3.</summary>
    protected static readonly FakeSelectionModeProperty SelectionModeProperty = new();

    /// <summary>Its source; only the property's existence matters.</summary>
    public object? ItemsSource { get; set; }

    /// <summary>Its rows' theme; only the property's existence matters.</summary>
    public object? ItemContainerTheme { get; set; }

    /// <summary>Its selection; only the property's existence matters.</summary>
    public int SelectedIndex { get; set; }

    /// <summary>Shaped like the framework's container factory.</summary>
    /// <returns>A new container.</returns>
    protected virtual Control CreateContainerForItemOverride() => new ContentPresenter();
}

/// <summary>A list's row: focusable by default, once its static constructor has run, and a list item to automation.</summary>
public class ListBoxItem : ContentControl
{
    static ListBoxItem() => FocusableProperty.OverrideDefaultValue(typeof(ListBoxItem), true);

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new ListItemAutomationPeer(this);
}

/// <summary>A list: unfocusable itself, with focusable rows.</summary>
public class ListBox : ItemsControl
{
    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride() => new ListBoxItem();
}

/// <summary>A drop-down's row, which keeps its base's peer.</summary>
public class ComboBoxItem : ListBoxItem;

/// <summary>A drop-down list, which adds itself as an owner of the list's selected index with a copy, as a direct property's <c>AddOwner</c> does.</summary>
public class ComboBox : ItemsControl
{
    /// <summary>A copy of the list's, which the framework takes for the same property.</summary>
    public static new readonly FakeDirectProperty SelectedIndexProperty = ItemsControl.SelectedIndexProperty.AddOwner();

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride() => new ComboBoxItem();
}

/// <summary>A custom list that borrows its base's theme key, as a WPF-to-Avalonia port's <c>LinesListBox</c> does.</summary>
public class BorrowingListBox : ListBox
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(ListBox);
}

/// <summary>A custom list that borrows its base's theme key through a field, so that its getter holds no <c>typeof</c>.</summary>
public class FieldKeyListBox : ListBox
{
    private static readonly Type Key = typeof(ListBox);

    /// <inheritdoc />
    protected override Type StyleKeyOverride => Key;
}

/// <summary>A tree's row, which presents items of its own.</summary>
public class TreeViewItem : ItemsControl
{
    static TreeViewItem() => FocusableProperty.OverrideDefaultValue(typeof(TreeViewItem), true);

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new TreeViewItemAutomationPeer(this);
}

/// <summary>A tree: unfocusable itself, with focusable rows.</summary>
public class TreeView : ItemsControl
{
    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride() => new TreeViewItem();
}

/// <summary>A content control that shows a header as well, as the framework's tabs do.</summary>
public class HeaderedContentControl : ContentControl;

/// <summary>A tab, which is a list item to automation, and shows the item as its header.</summary>
public class TabItem : HeaderedContentControl
{
    static TabItem() => FocusableProperty.OverrideDefaultValue(typeof(TabItem), true);

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new ListItemAutomationPeer(this);
}

/// <summary>Presents its selected item's content as well as its items, and always selects one, as the framework's does.</summary>
public class TabControl : ItemsControl
{
    static TabControl() => SelectionModeProperty.OverrideDefaultValue(typeof(TabControl), "AlwaysSelected");

    /// <summary>The template the selected content is shown with.</summary>
    public object? ContentTemplate { get; set; }

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride() => new TabItem();
}

/// <summary>Text: unfocusable, and not templated.</summary>
public class TextBlock : Control
{
    /// <summary>Its text, which a derived text block holds too, as the framework's does.</summary>
    public static readonly FakeProperty TextProperty = new();

    /// <summary>Its text; only the property's existence matters.</summary>
    public string? Text { get; set; }
}

/// <summary>Text a user can select: a text block, holding its text.</summary>
public class SelectableTextBlock : TextBlock;

/// <summary>A decorator: unfocusable, and not templated.</summary>
public class Border : Control;

/// <summary>A panel: unfocusable, not templated, and drawing its background.</summary>
public class Panel : Control;

/// <summary>A stacking panel.</summary>
public class StackPanel : Panel;

/// <summary>A grid.</summary>
public class Grid : Panel;

/// <summary>What shows its child in a layer of its own while it is open: not templated, and drawing no background.</summary>
public class Popup : Control;

/// <summary>Where an items control lays out what it presents: not templated, and drawing no background.</summary>
public class ItemsPresenter : Control;

/// <summary>A run of text, which is not a control: shaped like the framework's inline, whose background a text block draws.</summary>
public class Run;

/// <summary>A shape: unfocusable, not templated, and drawing its fill.</summary>
public class Shape : Control
{
    /// <summary>Found by name, as the rule finds a real framework's: null by default, as the framework's is.</summary>
    public static readonly FakeBackgroundProperty FillProperty = new();

    /// <summary>Its fill; only the setter matters, which code that sets it calls.</summary>
    public object? Fill { get; set; }
}

/// <summary>A rectangle: a shape as wide as its stroke when nothing gives it a width.</summary>
public class Rectangle : Shape;

/// <summary>A text input: focusable.</summary>
public class TextBox : TemplatedControl
{
    static TextBox() => FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
}

/// <summary>A button: focusable.</summary>
public class Button : TemplatedControl
{
    static Button() => FocusableProperty.OverrideDefaultValue(typeof(Button), true);
}

/// <summary>Unfocusable itself, with a template that may hold something focusable.</summary>
public class Expander : TemplatedControl;

/// <summary>A view: unfocusable, not an items control.</summary>
public class UserControl : ContentControl;
