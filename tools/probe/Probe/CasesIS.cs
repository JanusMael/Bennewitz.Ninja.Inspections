namespace FpProbe6;

// BNXQ1009's ItemsSource reading: rows with no item template, or with one that declares no data type, whose items' type
// the rule reads from the ItemsSource binding's path through an x:DataType, and the data templates the rows' presenters
// choose by that type. Each list is typed by x:DataType as Model13, the data context the bound pass gives it, whose
// properties hold items of each kind of ToString(): a Row's is object's, a string's, an enum member's and a Labeled13's
// their types' own, and a Rec13's the compiler's. A case ending in "c" is a control.
internal static partial class Cases
{
    /// <summary>An application whose DataTemplates present a Row by a TextBlock with its label, as "rowtext" does in code.</summary>
    private const string AppWithRowText = """
        <Application xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:p="clr-namespace:FpProbe6;assembly=FpProbe6">
          <Application.DataTemplates>
            <DataTemplate DataType="p:Row"><TextBlock Text="{Binding Label}" HorizontalAlignment="Left"/></DataTemplate>
          </Application.DataTemplates>
        </Application>
        """;

    private const string RowText = "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>";

    /// <summary>A list typed by x:DataType as Model13, bound to <paramref name="source"/>, holding <paramref name="fragment"/>.</summary>
    private static string Typed(string host, string source, string fragment) => """
        <HOST NAMESPACES
                 x:DataType="p:Model13"
                 Width="300" Height="200" ItemsSource="{Binding SOURCE}">
          FRAGMENT
        </HOST>
        """.Replace("HOST", host).Replace("NAMESPACES", Namespaces).Replace("SOURCE", source).Replace("FRAGMENT", fragment);

    private static readonly string AroundTheList = """
        <Border NAMESPACES
                x:DataType="p:Model13"
                Width="320" Height="220">
          <Border.DataTemplates>
            <DataTemplate DataType="p:Row">ROOT</DataTemplate>
          </Border.DataTemplates>
          <ListBox Name="Inner" Width="300" Height="200" ItemsSource="{Binding Rows}"/>
        </Border>
        """.Replace("NAMESPACES", Namespaces).Replace("ROOT", RowText);

    public static readonly Case[] AllIS =
    [
        // ── No item template: the default template, a TextBlock of the item's ToString() ─────────────────
        C("IS01", "no ItemTemplate; ItemsSource Rows, through x:DataType p:Model13: each row shows its Row's ToString(), the type's full name",
            Typed("ListBox", "Rows", string.Empty)),
        C("IS02", "IS01 in a tab control, whose tabs present each item as their header",
            Typed("TabControl", "Rows", string.Empty)),
        C("IS03", "no ItemTemplate; strings, each its own ToString()",
            Typed("ListBox", "Texts", string.Empty)),
        C("IS04", "no ItemTemplate; an enum's members",
            Typed("ListBox", "Modes", string.Empty)),
        C("IS05", "no ItemTemplate; items whose type writes its own ToString()",
            Typed("ListBox", "Labeled", string.Empty)),
        C("IS06", "no ItemTemplate; records, whose ToString() the compiler writes",
            Typed("ListBox", "Records", string.Empty)),

        // ── No item template: a data template chosen by the items' type ────────────────────────────────
        C("IS07", "no ItemTemplate; the list's DataTemplates hold one with DataType p:Row whose root is a TextBlock with the label",
            Typed("ListBox", "Rows", ListTemplates("<DataTemplate DataType=\"p:Row\">" + RowText + "</DataTemplate>"))),
        C("IS08", "IS07 with x:DataType p:Row in place of DataType",
            Typed("ListBox", "Rows", ListTemplates("<DataTemplate x:DataType=\"p:Row\">" + RowText + "</DataTemplate>"))),
        C("IS09", "no ItemTemplate; the list's DataTemplates hold one with x:DataType p:Model13, another type, whose root is a TextBlock reading 'matched'",
            Typed("ListBox", "Rows", ListTemplates("<DataTemplate x:DataType=\"p:Model13\"><TextBlock Text=\"matched\" HorizontalAlignment=\"Left\"/></DataTemplate>"))),
        C("IS10", "no ItemTemplate; the list's DataTemplates hold one with no data type whose root is a TextBlock reading 'matched'",
            Typed("ListBox", "Rows", ListTemplates("<DataTemplate><TextBlock Text=\"matched\" HorizontalAlignment=\"Left\"/></DataTemplate>"))),
        C("IS11", "no ItemTemplate; the list's DataTemplates hold one with DataType p:Row whose root is a StackPanel holding the label",
            Typed("ListBox", "Rows", ListTemplates("<DataTemplate DataType=\"p:Row\"><StackPanel>" + RowText + "</StackPanel></DataTemplate>"))),
        C("IS12", "no ItemTemplate; the DataTemplates of a Border around the list hold one with DataType p:Row whose root is a TextBlock with the label",
            AroundTheList),
        new("IS13", "no ItemTemplate; the application's DataTemplates hold one for p:Row whose root is a TextBlock with the label",
            Typed("ListBox", "Rows", string.Empty), [("App.axaml", AppWithRowText)], "rowtext"),

        // ── An item template with no data type: its items are the ItemsSource's ────────────────────────
        C("IS14", "an ItemTemplate with no data type whose root is a StackPanel holding the label: its items are Rows",
            Typed("ListBox", "Rows", "<ListBox.ItemTemplate><DataTemplate><StackPanel>" + RowText + "</StackPanel></DataTemplate></ListBox.ItemTemplate>")),
        C("IS14c", "IS14 with a TextBlock root (control)",
            Typed("ListBox", "Rows", "<ListBox.ItemTemplate><DataTemplate>" + RowText + "</DataTemplate></ListBox.ItemTemplate>")),
    ];
}
