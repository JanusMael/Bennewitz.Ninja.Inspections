// What ItemContainerNameRuleTests read beside FocusFakes.cs: the automation peers its containers
// construct, the item types its templates declare, and a list whose rows name themselves; and, for
// ItemContainerBackgroundRuleTests, a consumer's templated controls, views, panels, borders, shapes and
// rows, some painted by a default their static constructor registers, some by their own code, and some
// whose static constructor fails; one that draws itself, and one that borrows Border's style key; a type
// that registers an attached Background of its own, which is another property than a control's; a
// binding of the consumer's own; and the owners the XAML compiler's routes tell apart: static aliases
// with accessors whose field holds a control's own property, an AvaloniaObject that adds itself as an
// owner with accessors and no instance property, a panel, a text block and a list that hide their base's field,
// an attached Name of the consumer's own, a list that re-declares the selected index beside a copy of
// it, a row whose name a row in another namespace carries too, and, in a namespace of their own, classes
// named as the framework's controls are.
// Nothing here is constructed; the rule reads the types, and so do the tests.

// The peers are in Avalonia's own namespace, because the rule recognises a list or tree row by its
// peer's full name. They are shaped like 12.1.3's: a list row's peer takes its name from the content
// control peer's GetNameCore, and a tree row's from the control peer's.
namespace Avalonia.Automation.Peers
{
    /// <summary>Where naming is declared.</summary>
    public abstract class AutomationPeer
    {
        /// <summary>The name, as the peer computes it.</summary>
        /// <returns>The name, or <c>null</c>.</returns>
        protected abstract string? GetNameCore();
    }

    /// <summary>A control's peer: named by <c>AutomationProperties</c> alone.</summary>
    /// <param name="owner">The control.</param>
    public class ControlAutomationPeer(object owner) : AutomationPeer
    {
        /// <summary>The control.</summary>
        public object Owner { get; } = owner;

        /// <inheritdoc />
        protected override string? GetNameCore() => null;
    }

    /// <summary>The peer a control that makes none gets.</summary>
    /// <param name="owner">The control.</param>
    public class NoneAutomationPeer(object owner) : ControlAutomationPeer(owner);

    /// <summary>A content control's peer: its own name, then what it presents, then its content's <c>ToString()</c>.</summary>
    /// <param name="owner">The control.</param>
    public class ContentControlAutomationPeer(object owner) : ControlAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override string? GetNameCore() => base.GetNameCore();
    }

    /// <summary>A list or tab row's peer, which keeps the content control's naming.</summary>
    /// <param name="owner">The row.</param>
    public class ListItemAutomationPeer(object owner) : ContentControlAutomationPeer(owner);

    /// <summary>An items control's peer.</summary>
    /// <param name="owner">The control.</param>
    public class ItemsControlAutomationPeer(object owner) : ControlAutomationPeer(owner);

    /// <summary>A tree row's peer, which keeps the control peer's naming.</summary>
    /// <param name="owner">The row.</param>
    public class TreeViewItemAutomationPeer(object owner) : ItemsControlAutomationPeer(owner);
}

namespace XamlQuality.Tests.Rules.ItemFakes
{
    using Avalonia.Automation.Peers;
    using XamlQuality.Tests.Rules.FocusFakes;

    /// <summary>A view model that writes no <c>ToString()</c>.</summary>
    public sealed class PlainItem
    {
        /// <summary>What a row shows.</summary>
        public string Label { get; } = string.Empty;
    }

    /// <summary>A view model that writes its own <c>ToString()</c>.</summary>
    public sealed class NamedItem
    {
        /// <summary>What a row shows.</summary>
        public string Label { get; } = string.Empty;

        /// <inheritdoc />
        public override string ToString() => Label;
    }

    /// <summary>A record, whose <c>ToString()</c> the compiler writes.</summary>
    /// <param name="Label">What a row shows.</param>
    public sealed record RecordItem(string Label);

    /// <summary>A record that writes its own <c>ToString()</c>.</summary>
    /// <param name="Label">What a row shows.</param>
    public sealed record NamedRecordItem(string Label)
    {
        /// <inheritdoc />
        public override string ToString() => Label;
    }

    /// <summary>A record struct, whose <c>ToString()</c> the compiler writes too.</summary>
    /// <param name="Label">What a row shows.</param>
    public readonly record struct RecordStructItem(string Label);

    /// <summary>A struct that writes no <c>ToString()</c>, so <c>ValueType</c>'s names it.</summary>
    public readonly struct StructItem
    {
        /// <summary>What a row shows.</summary>
        public int Value { get; }
    }

    /// <summary>An enum, named by its member.</summary>
    public enum ModeItem
    {
        /// <summary>One.</summary>
        First,

        /// <summary>Another.</summary>
        Second,
    }

    /// <summary>A base the items' concrete types derive from.</summary>
    public abstract class AbstractItem;

    /// <summary>What the items' concrete types implement.</summary>
    public interface IItem;

    /// <summary>A type that writes no <c>ToString()</c>, with a subclass that does.</summary>
    public class BaseItem;

    /// <summary>The subclass that writes one.</summary>
    public sealed class DerivedItem : BaseItem
    {
        /// <inheritdoc />
        public override string ToString() => nameof(DerivedItem);
    }

    /// <summary>An unsealed type that writes no <c>ToString()</c>, and nothing derives from it.</summary>
    public class OpenItem;

    /// <summary>A control used as an item.</summary>
    public class ControlItem : Control;

    /// <summary>A consumer's row peer that computes its own name.</summary>
    /// <param name="owner">The row.</param>
    public sealed class SelfNamingPeer(object owner) : ListItemAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override string? GetNameCore() => "row";
    }

    /// <summary>A consumer's row that creates that peer.</summary>
    public class SelfNamingRow : ListBoxItem
    {
        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new SelfNamingPeer(this);
    }

    /// <summary>A consumer's list whose rows name themselves.</summary>
    public class SelfNamingList : ListBox
    {
        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride() => new SelfNamingRow();
    }

    /// <summary>A consumer's templated control, drawn by whatever theme the markup gives it.</summary>
    public class Chip : TemplatedControl;

    /// <summary>A consumer's view, whose markup a test writes under its <c>x:Class</c>.</summary>
    public class RowView : UserControl;

    /// <summary>A consumer's view that another view's markup holds, whose own markup a test writes under its <c>x:Class</c>.</summary>
    public class InnerView : UserControl;

    /// <summary>A consumer's Border, not a view, whose own markup a test writes under its <c>x:Class</c>.</summary>
    public class XClassBorder : Border;

    /// <summary>A consumer's panel, not a view, whose own markup a test writes under its <c>x:Class</c>.</summary>
    public class XClassPanel : Panel;

    /// <summary>A consumer's templated control, not a view, whose own markup a test writes under its <c>x:Class</c>.</summary>
    public class XClassChip : TemplatedControl;

    /// <summary>A subclass of <see cref="XClassBorder"/>, onto which its base's constructor loads the base's markup; a test may write markup of its own.</summary>
    public class DerivedXClassBorder : XClassBorder;

    /// <summary>A subclass of <see cref="XClassPanel"/> with no markup of its own.</summary>
    public class DerivedXClassPanel : XClassPanel;

    /// <summary>A consumer's type that takes no part in focus, whose attached properties a test names after it, as <c>Wrapping.IsEnabled</c>.</summary>
    public static class Wrapping;

    /// <summary>
    /// A consumer's type that registers an attached <c>Background</c> of its own, as an attached property's owner
    /// does: its field holds another property than the one a control's <c>Background</c> is.
    /// </summary>
    public static class Paint
    {
        /// <summary>Found by name; not the object <see cref="Control.BackgroundProperty"/> holds.</summary>
        public static readonly FakeBackgroundProperty BackgroundProperty = new();

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The brush.</param>
        public static void SetBackground(Control control, object? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="control">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static object? GetBackground(Control control) => null;
    }

    /// <summary>A consumer's binding, which markup writes as an element or an extension of its own name.</summary>
    public sealed class RowBinding : BindingBase;

    /// <summary>
    /// A consumer's static class whose field holds a control's own <c>Background</c>, with accessors: an attached
    /// property, which any value sets, and the property a control's <c>Background</c> is.
    /// </summary>
    public static class BgAlias
    {
        /// <summary>The control's own.</summary>
        public static readonly FakeBackgroundProperty BackgroundProperty = Control.BackgroundProperty;

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The brush.</param>
        public static void SetBackground(Control control, object? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="control">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static object? GetBackground(Control control) => null;
    }

    /// <summary>
    /// A consumer's object, outside every control's chain, that adds itself as an owner of a control's
    /// <c>Background</c> and has accessors but no instance property: an attached property, which any value sets.
    /// </summary>
    public class BgOwner : AvaloniaObject
    {
        /// <summary>The control's own, as a styled property's <c>AddOwner</c> returns it.</summary>
        public static readonly FakeBackgroundProperty BackgroundProperty = Control.BackgroundProperty;

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The brush.</param>
        public static void SetBackground(Control control, object? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="control">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static object? GetBackground(Control control) => null;
    }

    /// <summary>A consumer's static class whose field holds a text block's own <c>Text</c>, with accessors.</summary>
    public static class TextAlias
    {
        /// <summary>The text block's own.</summary>
        public static readonly FakeProperty TextProperty = TextBlock.TextProperty;

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="textBlock">The text block.</param>
        /// <param name="value">The text.</param>
        public static void SetText(TextBlock textBlock, string? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="textBlock">The text block.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static string? GetText(TextBlock textBlock) => null;
    }

    /// <summary>A consumer's static class whose field holds a control's own <c>Focusable</c>, with accessors.</summary>
    public static class FocusAlias
    {
        /// <summary>The control's own.</summary>
        public static readonly FakeFocusableProperty FocusableProperty = InputElement.FocusableProperty;

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="element">The control.</param>
        /// <param name="value">Whether it takes focus.</param>
        public static void SetFocusable(InputElement element, bool value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="element">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static bool GetFocusable(InputElement element) => false;
    }

    /// <summary>A consumer's panel that hides its base's field with a public one of another property, and keeps its base's CLR property.</summary>
    public class ShadowPanel : Panel
    {
        /// <summary>Not the object <see cref="Control.BackgroundProperty"/> holds.</summary>
        public static new readonly FakeBackgroundProperty BackgroundProperty = new();
    }

    /// <summary>A consumer's panel that hides its base's field with a protected one of another property.</summary>
    public class ProtectedShadow : Panel
    {
        /// <summary>Not the object <see cref="Control.BackgroundProperty"/> holds.</summary>
        protected static new readonly FakeBackgroundProperty BackgroundProperty = new();
    }

    /// <summary>A consumer's text block that hides its base's field with a public one of another property, and keeps its base's CLR property.</summary>
    public class ShadowTextBlock : TextBlock
    {
        /// <summary>Not the object <see cref="TextBlock.TextProperty"/> holds.</summary>
        public static new readonly FakeProperty TextProperty = new();
    }

    /// <summary>A consumer's list that hides its base's <c>Focusable</c> field with a public one of another property, and keeps its base's CLR property.</summary>
    public class ShadowList : ListBox
    {
        /// <summary>Not the object <see cref="InputElement.FocusableProperty"/> holds.</summary>
        public static new readonly FakeFocusableProperty FocusableProperty = new();
    }

    /// <summary>A consumer's row whose name a type in another namespace carries too, so that the name alone resolves to neither.</summary>
    public class AmbiguousRow : ListBoxItem;

    /// <summary>A consumer's static class that registers an attached <c>Name</c> of its own, which no automation property is.</summary>
    public static class NameTag
    {
        /// <summary>Its own.</summary>
        public static readonly FakeProperty NameProperty = new();

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The name.</param>
        public static void SetName(Control control, string? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="control">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static string? GetName(Control control) => null;
    }

    /// <summary>
    /// A consumer's list that adds itself as an owner of a list's selected index, with the copy a direct property's
    /// <c>AddOwner</c> returns, and declares the CLR property beside it, so the field the XAML compiler takes is the copy.
    /// </summary>
    public class CopyingList : ItemsControl
    {
        /// <summary>A copy of the list's, which the framework takes for the same property.</summary>
        public static new readonly FakeDirectProperty SelectedIndexProperty = ItemsControl.SelectedIndexProperty.AddOwner();

        /// <summary>Its selection; only the property's existence matters.</summary>
        public new int SelectedIndex { get; set; }
    }

    /// <summary>A consumer's templated control whose static constructor makes its Background a brush by default.</summary>
    public class PaintedChip : TemplatedControl
    {
        static PaintedChip() => BackgroundProperty.OverrideDefaultValue(typeof(PaintedChip), "Transparent");
    }

    /// <summary>A consumer's view whose static constructor makes its Background a brush by default.</summary>
    public class PaintedView : UserControl
    {
        static PaintedView() => BackgroundProperty.OverrideDefaultValue(typeof(PaintedView), "Transparent");
    }

    /// <summary>A consumer's view whose code sets its Background, as code-behind can.</summary>
    public class CodePaintedView : UserControl
    {
        /// <summary>Sets the background its markup does not show.</summary>
        public CodePaintedView() => Background = "Transparent";
    }

    /// <summary>A consumer's view whose code sets its Background in a method, as an event handler does.</summary>
    public class HandlerPaintedView : UserControl
    {
        /// <summary>Sets the background its markup does not show.</summary>
        public void Highlight() => Background = "Transparent";
    }

    /// <summary>A consumer's view whose Background a static method of its own sets.</summary>
    public class StaticPaintedView : UserControl
    {
        /// <summary>Sets the background of a view of this type.</summary>
        /// <param name="view">The view.</param>
        public static void Highlight(StaticPaintedView view)
        {
            ArgumentNullException.ThrowIfNull(view);
            view.Background = "Transparent";
        }
    }

    /// <summary>
    /// A consumer's view whose code sets its Background in a handler that captures a local, which the
    /// compiler writes as a closure nested in the view's type.
    /// </summary>
    public class LambdaPaintedView : UserControl
    {
        /// <summary>Keeps a handler that sets the background its markup does not show.</summary>
        public LambdaPaintedView()
        {
            string brush = "Transparent";
            Paint = () => Background = brush;
        }

        /// <summary>The handler.</summary>
        public Action Paint { get; }
    }

    /// <summary>
    /// A consumer's view whose code sets its Background after an await, which the compiler writes as a
    /// state machine nested in the view's type.
    /// </summary>
    public class AsyncPaintedView : UserControl
    {
        /// <summary>Sets the background its markup does not show, once the caller's turn is done.</summary>
        /// <returns>The work.</returns>
        public async Task PaintAsync()
        {
            await Task.Yield();
            Background = "Transparent";
        }
    }

    /// <summary>A consumer's panel, not templated, whose static constructor makes its Background a brush by default.</summary>
    public class PaintedPanel : StackPanel
    {
        static PaintedPanel() => BackgroundProperty.OverrideDefaultValue(typeof(PaintedPanel), "Transparent");
    }

    /// <summary>A consumer's panel whose static constructor fails, as one that needs an assembly the scan's process lacks does.</summary>
    public class UnreadablePanel : StackPanel
    {
        static UnreadablePanel() => throw new InvalidOperationException("A dependency did not load.");
    }

    /// <summary>A consumer's text block whose static constructor fails, as one that needs an assembly the scan's process lacks does.</summary>
    public class UnreadableTextBlock : TextBlock
    {
        static UnreadableTextBlock() => throw new InvalidOperationException("A dependency did not load.");
    }

    /// <summary>A consumer's row whose static constructor makes its Background a brush by default.</summary>
    public class PaintedRow : ListBoxItem
    {
        static PaintedRow() => BackgroundProperty.OverrideDefaultValue(typeof(PaintedRow), "Transparent");
    }

    /// <summary>A consumer's list of those rows.</summary>
    public class PaintedRowList : ListBox
    {
        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride() => new PaintedRow();
    }

    /// <summary>A consumer's row whose code sets its Background.</summary>
    public class CodePaintedRow : ListBoxItem
    {
        /// <summary>Sets the background markup does not show.</summary>
        public CodePaintedRow() => Background = "Transparent";
    }

    /// <summary>A consumer's list of those rows.</summary>
    public class CodePaintedRowList : ListBox
    {
        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride() => new CodePaintedRow();
    }

    /// <summary>A consumer's list whose own code paints the rows it prepares, as an override of the framework's container preparation can.</summary>
    public class CodePaintingList : ListBox
    {
        /// <summary>Sets the background of a row it prepares, which markup does not show.</summary>
        /// <param name="row">The row.</param>
        protected virtual void Prepare(Control row)
        {
            ArgumentNullException.ThrowIfNull(row);
            row.Background = "Transparent";
        }
    }

    /// <summary>A consumer's row whose static constructor fails, so its default Background cannot be read.</summary>
    public class UnreadableRow : ListBoxItem
    {
        static UnreadableRow() => throw new InvalidOperationException("A dependency did not load.");
    }

    /// <summary>A consumer's list of those rows.</summary>
    public class UnreadableRowList : ListBox
    {
        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride() => new UnreadableRow();
    }

    /// <summary>A consumer's Border whose code sets its Background.</summary>
    public class CodePaintedBorder : Border
    {
        /// <summary>Sets the background markup does not show.</summary>
        public CodePaintedBorder() => Background = "Transparent";
    }

    /// <summary>A consumer's control with no template that draws itself, as one overriding the framework's Render does.</summary>
    public class FillRender : Control
    {
        /// <summary>Shaped like the framework's: what it draws, across its bounds.</summary>
        /// <param name="context">What it draws with.</param>
        public virtual void Render(DrawingContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    /// <summary>A consumer's Border that borrows Border's style key, so a Border selector styles it and its own name does not.</summary>
    public class KeyedBorder : Border
    {
        /// <summary>The key its styles and implicit theme are found under, shaped like the framework's.</summary>
        protected virtual Type StyleKeyOverride => typeof(Border);
    }

    /// <summary>A consumer's rectangle whose static constructor makes its Fill a brush by default.</summary>
    public class FilledRect : Rectangle
    {
        static FilledRect() => FillProperty.OverrideDefaultValue(typeof(FilledRect), "Transparent");
    }

    /// <summary>A consumer's rectangle whose code sets its Fill.</summary>
    public class CodeFilledRect : Rectangle
    {
        /// <summary>Sets the fill markup does not show.</summary>
        public CodeFilledRect() => Fill = "Transparent";
    }
}

namespace XamlQuality.Tests.Rules.ItemFakes.Elsewhere
{
    /// <summary>Another <c>PlainItem</c>, in another namespace, that writes its own <c>ToString()</c>.</summary>
    public sealed class PlainItem
    {
        /// <summary>What a row shows.</summary>
        public string Label { get; } = string.Empty;

        /// <inheritdoc />
        public override string ToString() => Label;
    }

    /// <summary>Another <c>AmbiguousRow</c>, in another namespace.</summary>
    public class AmbiguousRow : FocusFakes.ListBoxItem;
}

// A consumer's classes named as the framework's controls are, in a namespace of their own, which markup names
// through a prefix: each registers an attached Background of its own.
namespace XamlQuality.Tests.Rules.ItemFakes.Alt
{
    using XamlQuality.Tests.Rules.FocusFakes;

    /// <summary>Named as the framework's templated control is; another type, with an attached <c>Background</c> of its own.</summary>
    public static class TemplatedControl
    {
        /// <summary>Its own.</summary>
        public static readonly FakeBackgroundProperty BackgroundProperty = new();

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The brush.</param>
        public static void SetBackground(Control control, object? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="control">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static object? GetBackground(Control control) => null;
    }

    /// <summary>Named as the framework's panel is; another type, with an attached <c>Background</c> of its own.</summary>
    public static class Panel
    {
        /// <summary>Its own.</summary>
        public static readonly FakeBackgroundProperty BackgroundProperty = new();

        /// <summary>The accessor that makes it an attached property.</summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The brush.</param>
        public static void SetBackground(Control control, object? value) { }

        /// <summary>The accessor that reads it.</summary>
        /// <param name="control">The control.</param>
        /// <returns>Nothing; only the accessor's existence matters.</returns>
        public static object? GetBackground(Control control) => null;
    }
}
