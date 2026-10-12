using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ProbeLib;

// A consumer's control library, referenced by the application assembly the scan is given, and not
// given to the scan itself: the way WithAssemblies' own message asks a consumer to scan ("the
// assembly whose markup this is; the framework is reached through its references").

/// <summary>A Border whose own code sets its Background, in an assembly the scan reaches only by reference.</summary>
public class LibCodeBorder : Border
{
    public LibCodeBorder() => Background = Brushes.Transparent;
}

/// <summary>A Border whose static constructor makes its Background default Transparent, in a referenced assembly.</summary>
public class LibDefaultBorder : Border
{
    static LibDefaultBorder() => BackgroundProperty.OverrideDefaultValue<LibDefaultBorder>(Brushes.Transparent);
}

/// <summary>A row whose own code sets its Background through the setter, in a referenced assembly.</summary>
public class LibCodeRow : ListBoxItem
{
    public LibCodeRow() => Background = Brushes.Transparent;

    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}

/// <summary>A list of those rows, in the same referenced assembly.</summary>
public class LibCodeRowList : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new LibCodeRow();
}

/// <summary>A view whose code-behind sets its Background, in a referenced assembly; its markup paints nothing.</summary>
public class LibCodeView : UserControl
{
    public LibCodeView()
    {
        Content = new TextBlock { Text = "view", HorizontalAlignment = HorizontalAlignment.Left };
        Background = Brushes.Transparent;
    }
}
