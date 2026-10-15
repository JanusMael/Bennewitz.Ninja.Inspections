[assembly: Avalonia.Metadata.XmlnsDefinition("https://judge.example/probe6", "FpProbe6")]

namespace FpProbe6;

// The review of PR #81: BNXQ1009's data template search, where its reading may depart from the framework's, and what a
// style or a theme gives the rows to present with no item template: a tab's header, its header template, and a row's
// content. Each list is typed by x:DataType as Model13, the data context the bound pass gives it. A case ending in "c"
// is a control.
internal static partial class Cases
{
    private const string QNamespaces = Namespaces + "\n                 xmlns:q=\"https://judge.example/probe6\"";

    private const string QRowText = "<TextBlock Text=\"{Binding Label}\" HorizontalAlignment=\"Left\"/>";

    private static string QTyped(string host, string fragment) => """
        <HOST NAMESPACES
                 x:DataType="p:Model13"
                 Width="300" Height="200" ItemsSource="{Binding Rows}">
          FRAGMENT
        </HOST>
        """.Replace("HOST", host).Replace("NAMESPACES", QNamespaces).Replace("FRAGMENT", fragment);

    private const string QAppWithUriRowText = """
        <Application xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:q="https://judge.example/probe6">
          <Application.DataTemplates>
            <DataTemplate DataType="q:Row"><TextBlock Text="{Binding Label}" HorizontalAlignment="Left"/></DataTemplate>
          </Application.DataTemplates>
        </Application>
        """;

    private static string QOwner(string inner) => """
        <Border NAMESPACES
                x:DataType="p:Model13"
                Width="320" Height="220">
          <Border.DataTemplates>
            <DataTemplate DataType="p:Model13"><ListBox Name="Inner" Width="300" Height="200" ItemsSource="{Binding Rows}"/></DataTemplate>
            <DataTemplate DataType="p:Row"><StackPanel>ROW</StackPanel></DataTemplate>
          </Border.DataTemplates>
          <ContentControl Content="{Binding}">INNER</ContentControl>
        </Border>
        """.Replace("NAMESPACES", QNamespaces).Replace("ROW", QRowText).Replace("INNER", inner);

    public static readonly Case[] AllQ =
    [
        C("ZJ01", "no ItemTemplate; the list's DataTemplates hold one with DataType q:Row, q an XmlnsDefinition URI for FpProbe6, whose root is a TextBlock with the label",
            QTyped("ListBox", "<ListBox.DataTemplates><DataTemplate DataType=\"q:Row\">" + QRowText + "</DataTemplate></ListBox.DataTemplates>")),
        C("ZJ01x", "ZJ01 with x:DataType q:Row in place of DataType",
            QTyped("ListBox", "<ListBox.DataTemplates><DataTemplate x:DataType=\"q:Row\">" + QRowText + "</DataTemplate></ListBox.DataTemplates>")),
        new("ZJ02", "no ItemTemplate; the application's DataTemplates hold one for q:Row, q an XmlnsDefinition URI, whose root is a TextBlock with the label",
            QTyped("ListBox", string.Empty), [("App.axaml", QAppWithUriRowText)], "rowtext"),
        C("ZJ03", "a list in a Model13 data template in a Border's DataTemplates, beside a p:Row template whose root is a StackPanel; the ContentControl that presents the model holds a p:Row template whose root is a TextBlock",
            QOwner("<ContentControl.DataTemplates><DataTemplate DataType=\"p:Row\">" + QRowText + "</DataTemplate></ContentControl.DataTemplates>")),
        C("ZJ03c", "ZJ03 with no data template in the ContentControl (control)",
            QOwner(string.Empty)),
        C("ZJ04", "TabControl, no ItemTemplate; its DataTemplates hold one with DataType p:Row whose root is a TextBlock with the label",
            QTyped("TabControl", "<TabControl.DataTemplates><DataTemplate DataType=\"p:Row\">" + QRowText + "</DataTemplate></TabControl.DataTemplates>")),
        C("ZJ05", "no ItemTemplate; a p:Row TextBlock data template in the list's DataTemplates; a Style in the list's Styles gives its rows a template whose presenter is not PART_ContentPresenter",
            QTyped("ListBox", "<ListBox.DataTemplates><DataTemplate DataType=\"p:Row\">" + QRowText + "</DataTemplate></ListBox.DataTemplates>"
                + "<ListBox.Styles><Style Selector=\"ListBoxItem\"><Setter Property=\"Template\"><ControlTemplate>"
                + "<ContentPresenter Name=\"Other\" Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"/>"
                + "</ControlTemplate></Setter></Style></ListBox.Styles>")),
    ];

    public static readonly Case[] AllQ2 =
    [
        C("ZJ06", "TabControl, no ItemTemplate; a TabItem Style in its Styles sets HeaderTemplate to a TextBlock of the label",
            QTyped("TabControl", "<TabControl.Styles><Style Selector=\"TabItem\"><Setter Property=\"HeaderTemplate\"><DataTemplate x:DataType=\"p:Row\">"
                + QRowText + "</DataTemplate></Setter></Style></TabControl.Styles>")),
        C("ZJ07", "TabControl, no ItemTemplate; a TabItem Style in its Styles sets Header to the label",
            QTyped("TabControl", "<TabControl.Styles><Style Selector=\"TabItem\" x:DataType=\"p:Row\"><Setter Property=\"Header\" Value=\"{Binding Label}\"/></Style></TabControl.Styles>")),
        C("ZJ08", "ListBox, no ItemTemplate; a ListBoxItem Style in its Styles sets Content to the label",
            QTyped("ListBox", "<ListBox.Styles><Style Selector=\"ListBoxItem\" x:DataType=\"p:Row\"><Setter Property=\"Content\" Value=\"{Binding Label}\"/></Style></ListBox.Styles>")),
        C("ZJ09", "TabControl, no ItemTemplate; its ItemContainerTheme, BasedOn the TabItem theme, sets HeaderTemplate to a TextBlock of the label",
            QTyped("TabControl", "<TabControl.ItemContainerTheme><ControlTheme TargetType=\"TabItem\" BasedOn=\"{StaticResource {x:Type TabItem}}\">"
                + "<Setter Property=\"HeaderTemplate\"><DataTemplate x:DataType=\"p:Row\">" + QRowText + "</DataTemplate></Setter>"
                + "</ControlTheme></TabControl.ItemContainerTheme>")),
    ];
}
