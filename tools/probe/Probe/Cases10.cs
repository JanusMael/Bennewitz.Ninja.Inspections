namespace FpProbe6;

// The seventh judge's combo ("CB") and tab ("TB") cases. A combo row is the first row of the open
// drop-down, clicked in the popup's own top level at a quarter, the centre and 3 px inside its right edge;
// a tab is the second, unselected tab, clicked beside its header: 3 px inside its left edge, 3 px below its
// top at its centre, and 3 px inside its right edge.

internal static partial class Cases
{
    private static string ComboHost(string fragment, string itemRoot) => """
        <ComboBox NAMESPACES
                 Width="300" ItemsSource="{Binding Rows}">
          FRAGMENT
          <ComboBox.ItemTemplate><DataTemplate>ITEM</DataTemplate></ComboBox.ItemTemplate>
        </ComboBox>
        """.Replace("NAMESPACES", Namespaces).Replace("FRAGMENT", fragment).Replace("ITEM", itemRoot);

    private static string TabHost(string fragment, string itemRoot) => """
        <TabControl NAMESPACES
                 Width="300" Height="200" ItemsSource="{Binding Rows}">
          FRAGMENT
          <TabControl.ItemTemplate><DataTemplate>ITEM</DataTemplate></TabControl.ItemTemplate>
        </TabControl>
        """.Replace("NAMESPACES", Namespaces).Replace("FRAGMENT", fragment).Replace("ITEM", itemRoot);

    private const string ComboNullStyle =
        "<ComboBox.Styles><Style Selector=\"ComboBoxItem\"><Setter Property=\"Background\" Value=\"{x:Null}\"/></Style></ComboBox.Styles>";

    private const string TabNullStyle =
        "<TabControl.Styles><Style Selector=\"TabItem\"><Setter Property=\"Background\" Value=\"{x:Null}\"/></Style></TabControl.Styles>";

    /// <summary>A combo row theme BasedOn the framework's own, with a null Background.</summary>
    private const string ComboBasedNull = """
        <ComboBox.ItemContainerTheme>
          <ControlTheme TargetType="ComboBoxItem" BasedOn="{StaticResource {x:Type ComboBoxItem}}">
            <Setter Property="Background" Value="{x:Null}"/>
          </ControlTheme>
        </ComboBox.ItemContainerTheme>
        """;

    /// <summary>A tab row theme BasedOn the framework's own, with a null Background.</summary>
    private const string TabBasedNull = """
        <TabControl.ItemContainerTheme>
          <ControlTheme TargetType="TabItem" BasedOn="{StaticResource {x:Type TabItem}}">
            <Setter Property="Background" Value="{x:Null}"/>
          </ControlTheme>
        </TabControl.ItemContainerTheme>
        """;

    /// <summary>A combo row theme based on nothing, Background null, whose template's root takes the row's by <paramref name="background"/>.</summary>
    private static string ComboRowTheme(string background) => """
        <ComboBox.ItemContainerTheme>
          <ControlTheme TargetType="ComboBoxItem">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Template"><ControlTemplate><Border Background="BACKGROUND">PRESENTER</Border></ControlTemplate></Setter>
          </ControlTheme>
        </ComboBox.ItemContainerTheme>
        """.Replace("BACKGROUND", background).Replace("PRESENTER", Presenter);

    /// <summary>
    /// A tab row theme based on nothing, Background null and Padding 20, whose root Border takes the row's
    /// Background by <paramref name="background"/> and holds the header presenter; <paramref name="extra"/> sits
    /// beside that Border in a Panel.
    /// </summary>
    private static string TabRowTheme(string background, string extra) => """
        <TabControl.ItemContainerTheme>
          <ControlTheme TargetType="TabItem">
            <Setter Property="Background" Value="{x:Null}"/>
            <Setter Property="Padding" Value="20"/>
            <Setter Property="Template"><ControlTemplate>
              <Panel>
                <Border Background="BACKGROUND" Padding="{TemplateBinding Padding}">
                  <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Header}" ContentTemplate="{TemplateBinding HeaderTemplate}"/>
                </Border>
                EXTRA
              </Panel>
            </ControlTemplate></Setter>
          </ControlTheme>
        </TabControl.ItemContainerTheme>
        """.Replace("BACKGROUND", background).Replace("EXTRA", extra);

    private const string TargetNullBackground =
        "{Binding Background, RelativeSource={RelativeSource TemplatedParent}, TargetNullValue={x:Static Brushes.Transparent}}";

    public static readonly Case[] All10 =
    [
        // ── Combo rows ────────────────────────────────────────────────────────────────────────────
        C("CB01", "combo: stock rows, plain item template", ComboHost(string.Empty, Plain)),
        C("CB02", "combo: a ComboBoxItem style sets Background {x:Null}; plain item template", ComboHost(ComboNullStyle, Plain)),
        C("CB03", "combo: row theme BasedOn the framework's ComboBoxItem theme, Background {x:Null}; plain", ComboHost(ComboBasedNull, Plain)),
        C("CB04", "combo: row theme Background null; template Border binds it with TargetNullValue Transparent",
            ComboHost(ComboRowTheme(TargetNullBackground), Plain)),
        C("CB04c", "combo: the same, the template Border by TemplateBinding (control for CB04)",
            ComboHost(ComboRowTheme("{TemplateBinding Background}"), Plain)),
        C("CB05", "combo: style {x:Null}; item template root paints", ComboHost(ComboNullStyle, Painted)),

        // ── Tab rows ──────────────────────────────────────────────────────────────────────────────
        C("TB01", "tabs: stock tabs, plain header template", TabHost(string.Empty, Plain)),
        C("TB02", "tabs: a TabItem style sets Background {x:Null}; plain header template", TabHost(TabNullStyle, Plain)),
        C("TB03", "tabs: tab theme BasedOn the framework's TabItem theme, Background {x:Null}; plain", TabHost(TabBasedNull, Plain)),
        C("TB04", "tabs: tab theme Background null, Padding 20; a presenter of the tab's IndicatorTemplate, which the TabControl sets painted",
            TabHost(TabRowTheme("{TemplateBinding Background}", "<ContentPresenter ContentTemplate=\"{TemplateBinding IndicatorTemplate}\"/>")
                    + "<TabControl.IndicatorTemplate><DataTemplate><Border Background=\"Transparent\"/></DataTemplate></TabControl.IndicatorTemplate>", Plain)),
        C("TB04c", "tabs: the same with no IndicatorTemplate set (control for TB04)",
            TabHost(TabRowTheme("{TemplateBinding Background}", "<ContentPresenter ContentTemplate=\"{TemplateBinding IndicatorTemplate}\"/>"), Plain)),
        C("TB05", "tabs: tab theme Background null, Padding 20; root Border binds it with TargetNullValue Transparent",
            TabHost(TabRowTheme(TargetNullBackground, string.Empty), Plain)),
        C("TB05c", "tabs: the same by TemplateBinding (control for TB05)",
            TabHost(TabRowTheme("{TemplateBinding Background}", string.Empty), Plain)),
        C("TB06", "tabs: style {x:Null}; header template root paints", TabHost(TabNullStyle, Painted)),
    ];
}
