using Avalonia.Controls;
using Avalonia.Media;

namespace FpProbe6;

// Judge 9's cases for BNXQ1010 round 9, aimed at C1 and C2 themselves. A case ending in "c" is a control.

internal static partial class Cases
{
    private const string BgPanel = "<Panel Name=\"Bg\">" + Presenter + "</Panel>";

    private const string TakingBorderOpen = "<Border Background=\"{TemplateBinding Background}\">";

    /// <summary>A list style that reaches into the rows' template and sets Panel#Bg's Background.</summary>
    private static string PartStyle(string value) =>
        "<ListBox.Styles><Style Selector=\"ListBoxItem /template/ Panel#Bg\"><Setter Property=\"Background\" Value=\"" + value + "\"/></Style></ListBox.Styles>";

    /// <summary>A row theme nested style that reaches into its own template and sets Panel#Bg's Background.</summary>
    private static string NestedPartStyle(string value) =>
        "<Style Selector=\"^ /template/ Panel#Bg\"><Setter Property=\"Background\" Value=\"" + value + "\"/></Style>";

    /// <summary>A row theme for <paramref name="type"/> under <paramref name="host"/>, Background {x:Null}, whose root Border takes it.</summary>
    private static string NullRowsFor(string host, string type, string extra = "") => """
        <HOST.ItemContainerTheme>
          <ControlTheme TargetType="TYPE">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Template"><ControlTemplate><Border Background="{TemplateBinding Background}">PRESENTER</Border></ControlTemplate></Setter>
            EXTRA
          </ControlTheme>
        </HOST.ItemContainerTheme>
        """.Replace("HOST", host).Replace("TYPE", type).Replace("PRESENTER", Presenter).Replace("EXTRA", extra);

    /// <summary>An outer list holding one explicit row, Background Transparent, which holds the inner list named Inner.</summary>
    private static string Nested(string innerFragment) => """
        <ListBox NAMESPACES
                 Width="300" Height="200">
          <ListBoxItem Background="Transparent">
            <ListBox Name="Inner" Width="280" Height="150" ItemsSource="{Binding Rows}">
              FRAGMENT
              <ListBox.ItemTemplate><DataTemplate>ITEM</DataTemplate></ListBox.ItemTemplate>
            </ListBox>
          </ListBoxItem>
        </ListBox>
        """.Replace("NAMESPACES", Namespaces).Replace("FRAGMENT", innerFragment).Replace("ITEM", Plain);
}
