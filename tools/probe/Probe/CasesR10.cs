namespace FpProbe6;

// Round 10's cases for BNXQ1010: the ancestor a binding names when the template puts one of its type between,
// a view held in another view's markup, and what a disabled list's rows, and their template's elements, take
// while disabled. A case ending in "c" is a control.
internal static partial class Cases
{
    /// <summary>A StackPanel with IsEnabled False holding the list named Inner.</summary>
    private static string DisabledHolder(string fragment, string itemRoot) => """
        <StackPanel NAMESPACES IsEnabled="False">
          <ListBox Name="Inner" Width="300" Height="200" ItemsSource="{Binding Rows}">
            FRAGMENT
            <ListBox.ItemTemplate><DataTemplate>ITEM</DataTemplate></ListBox.ItemTemplate>
          </ListBox>
        </StackPanel>
        """.Replace("NAMESPACES", Namespaces).Replace("FRAGMENT", fragment).Replace("ITEM", itemRoot);

    /// <summary>The list in <paramref name="markup"/> with IsEnabled False.</summary>
    private static string DisabledList(string markup) =>
        markup.Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" IsEnabled=\"False\"");

    /// <summary>A list style setting Background on what <paramref name="selector"/> picks.</summary>
    private static string ListStyle(string selector, string value) =>
        "<ListBox.Styles><Style Selector=\"" + selector + "\"><Setter Property=\"Background\" Value=\"" + value + "\"/></Style></ListBox.Styles>";

    private const string DisabledTransparent = "<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>";

    public static readonly Case[] AllR10 =
    [
        // ── An ancestor of the binding's type nearer than the row ────────────────────────────────
        C("W01", "row theme {x:Null}; template Panel: a ContentControl holding a Border bound to $parent[ContentControl].Background, beside the presenter",
            List(NullRowTemplate("<Panel><ContentControl><Border Height=\"10\" Background=\"{Binding $parent[ContentControl].Background}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
        C("W01c", "the same bound to $parent[ContentControl;1], past the ContentControl to the row (control)",
            List(NullRowTemplate("<Panel><ContentControl><Border Height=\"10\" Background=\"{Binding $parent[ContentControl;1].Background}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
        C("W02", "row theme {x:Null}; template Border: a Panel holding a Border bound to $parent[Control].Background",
            List(NullRowTemplate("<Border><Panel><Border Height=\"10\" Background=\"{Binding $parent[Control].Background}\"/>" + Presenter + "</Panel></Border>"), Plain)),
        C("W03", "row theme {x:Null}; template Panel: a ContentControl holding a Border bound by RelativeSource AncestorType=ListBoxItem (visual tree)",
            List(NullRowTemplate("<Panel><ContentControl><Border Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem}}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
        C("W03b", "the same with Tree=Logical",
            List(NullRowTemplate("<Panel><ContentControl><Border Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, Tree=Logical}}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
        C("W10", "row theme {x:Null}; template Panel: a Viewbox holding a Border bound by RelativeSource AncestorType=Control, AncestorLevel=3 (visual tree: the Viewbox's own container comes first)",
            List(NullRowTemplate("<Panel><Viewbox Height=\"10\"><Border Width=\"10\" Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=Control, AncestorLevel=3}}\"/></Viewbox>" + Presenter + "</Panel>"), Plain)),
        C("W10b", "the same with Tree=Logical, where the Viewbox and the Panel come first and the row is the third",
            List(NullRowTemplate("<Panel><Viewbox Height=\"10\"><Border Width=\"10\" Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=Control, AncestorLevel=3, Tree=Logical}}\"/></Viewbox>" + Presenter + "</Panel>"), Plain)),

        // ── A view held in another view's markup ─────────────────────────────────────────────────
        C("W04", "row theme Transparent; template Panel: p:HolderView, whose markup holds p:TintView given Background {x:Null}; TintView's own root is Transparent",
            List(TransparentTheme(string.Empty, "<Panel><p:HolderView/>" + Presenter + "</Panel>"), Plain),
            ("HolderView.axaml", SourceView("HolderView.axaml")), ("TintView.axaml", SourceView("TintView.axaml"))),
        C("W04c", "the same with p:HolderPlainView, whose markup holds p:TintView given nothing (control)",
            List(TransparentTheme(string.Empty, "<Panel><p:HolderPlainView/>" + Presenter + "</Panel>"), Plain),
            ("HolderPlainView.axaml", SourceView("HolderPlainView.axaml")), ("TintView.axaml", SourceView("TintView.axaml"))),

        // ── Rows a disabled ancestor disables, and what their template holds while disabled ──────
        C("W05", "a StackPanel with IsEnabled False holds the list; row theme {x:Null}, Border takes it, nested '^:disabled' sets Transparent",
            DisabledHolder(RowTheme(DisabledTransparent), Plain)),
        C("W06", "row theme Transparent; template Panel: Border#Bg with IsEnabled False beside the presenter; list style 'ListBoxItem /template/ Border:disabled' Transparent",
            List(TransparentTheme(string.Empty, "<Panel><Border Name=\"Bg\" Height=\"10\" IsEnabled=\"False\"/>" + Presenter + "</Panel>")
                 + ListStyle("ListBoxItem /template/ Border:disabled", "Transparent"), Plain)),
        C("W06c", "the same with the Border enabled (control)",
            List(TransparentTheme(string.Empty, "<Panel><Border Name=\"Bg\" Height=\"10\"/>" + Presenter + "</Panel>")
                 + ListStyle("ListBoxItem /template/ Border:disabled", "Transparent"), Plain)),
        C("W07", "list IsEnabled False; row theme Transparent; template Border#Bg sets none; list style 'Border:disabled' Transparent",
            DisabledList(List(TransparentTheme(string.Empty, "<Border Name=\"Bg\">" + Presenter + "</Border>") + ListStyle("Border:disabled", "Transparent"), Plain))),
        C("W07c", "the same with the list enabled (control)",
            List(TransparentTheme(string.Empty, "<Border Name=\"Bg\">" + Presenter + "</Border>") + ListStyle("Border:disabled", "Transparent"), Plain)),
        C("W08", "list IsEnabled False; row theme {x:Null}, Border takes it; an implicit Border theme in the list's resources nests '^:disabled' Red",
            DisabledList(List(RowTheme(string.Empty) + "<ListBox.Resources><ControlTheme x:Key=\"{x:Type Border}\" TargetType=\"Border\">"
                                                     + "<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Red\"/></Style></ControlTheme></ListBox.Resources>", Plain))),
        C("W08c", "the same with the list enabled (control)",
            List(RowTheme(string.Empty) + "<ListBox.Resources><ControlTheme x:Key=\"{x:Type Border}\" TargetType=\"Border\">"
                                        + "<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Red\"/></Style></ControlTheme></ListBox.Resources>", Plain)),
        C("W09", "list IsEnabled False; row theme Transparent; template Panel: p:Chip beside the presenter; the Chip's implicit theme nests '^:disabled' Red",
            DisabledList(List(TransparentTheme(string.Empty, "<Panel><p:Chip Height=\"10\"/>" + Presenter + "</Panel>")
                              + ThemeFor("p:Chip", "<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Red\"/></Style>"), Plain))),
        C("W09c", "the same with the list enabled (control)",
            List(TransparentTheme(string.Empty, "<Panel><p:Chip Height=\"10\"/>" + Presenter + "</Panel>")
                 + ThemeFor("p:Chip", "<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Red\"/></Style>"), Plain)),
    ];
}
