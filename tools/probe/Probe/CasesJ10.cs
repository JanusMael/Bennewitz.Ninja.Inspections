namespace FpProbe6;

// Judge 10's cases for BNXQ1010 round 10. A case ending in "c" is a control.
internal static partial class Cases
{
    private const string PanelDisabled =
        "<ListBox.ItemsPanel><ItemsPanelTemplate><StackPanel IsEnabled=\"False\"/></ItemsPanelTemplate></ListBox.ItemsPanel>";

    private const string PanelEnabled =
        "<ListBox.ItemsPanel><ItemsPanelTemplate><StackPanel/></ItemsPanelTemplate></ListBox.ItemsPanel>";

    private const string ListTemplateDisabled =
        "<ListBox.Template><ControlTemplate><Border><ItemsPresenter Name=\"PART_ItemsPresenter\" IsEnabled=\"False\" "
        + "ItemsPanel=\"{TemplateBinding ItemsPanel}\"/></Border></ControlTemplate></ListBox.Template>";

    private const string ListTemplateEnabled =
        "<ListBox.Template><ControlTemplate><Border><ItemsPresenter Name=\"PART_ItemsPresenter\" "
        + "ItemsPanel=\"{TemplateBinding ItemsPanel}\"/></Border></ControlTemplate></ListBox.Template>";

    /// <summary>A row theme Background Transparent whose template is a Panel holding Border#Bg, with no background, beside the presenter.</summary>
    private static string BgPanelTheme() =>
        TransparentTheme(string.Empty, "<Panel><Border Name=\"Bg\" Height=\"10\"/>" + Presenter + "</Panel>");

    /// <summary>An implicit Chip theme whose template is a Border, IsEnabled as given, that does not take the Chip's Background.</summary>
    private static string ChipPartTheme(string isEnabled) => """
        <ListBox.Resources>
          <ControlTheme x:Key="{x:Type p:Chip}" TargetType="p:Chip">
            <Setter Property="Template">
              <ControlTemplate>
                <Border Name="ChipBg" ENABLEDHeight="10"><TextBlock Text="chip" HorizontalAlignment="Left"/></Border>
              </ControlTemplate>
            </Setter>
          </ControlTheme>
        </ListBox.Resources>
        """.Replace("ENABLED", isEnabled);

    public static readonly Case[] AllJ10 =
    [
        // ── What disables rows from the start that markup shows, outside what round 10 reads ──────
        C("J01", "ItemsPanel StackPanel IsEnabled False; row theme {x:Null}, Border takes it, nested '^:disabled' Transparent",
            List(RowTheme(DisabledTransparent) + PanelDisabled, Plain)),
        C("J01c", "the same with the ItemsPanel enabled (control)",
            List(RowTheme(DisabledTransparent) + PanelEnabled, Plain)),
        C("J02", "list's own Template: ItemsPresenter IsEnabled False; row theme {x:Null}, Border takes it, nested '^:disabled' Transparent",
            List(RowTheme(DisabledTransparent) + ListTemplateDisabled, Plain)),
        C("J02c", "the same with the ItemsPresenter enabled (control)",
            List(RowTheme(DisabledTransparent) + ListTemplateEnabled, Plain)),
        C("J03", "ItemsPanel IsEnabled False; row theme Transparent, template Panel: Border#Bg beside the presenter; list style 'ListBoxItem:disabled /template/ Border#Bg' Red",
            List(BgPanelTheme() + PanelDisabled + ListStyle("ListBoxItem:disabled /template/ Border#Bg", "Red"), Plain)),
        C("J03c", "the same with the ItemsPanel enabled (control)",
            List(BgPanelTheme() + PanelEnabled + ListStyle("ListBoxItem:disabled /template/ Border#Bg", "Red"), Plain)),

        // ── A part that starts disabled inside a view's markup or a templated control's template ──
        C("J04", "row theme Transparent; template Panel: p:DisabledPartView beside the presenter, whose markup holds a Border IsEnabled False; list style 'Border:disabled' Red",
            List(TransparentTheme(string.Empty, "<Panel><p:DisabledPartView/>" + Presenter + "</Panel>") + ListStyle("Border:disabled", "Red"), Plain),
            ("DisabledPartView.axaml", SourceView("DisabledPartView.axaml"))),
        C("J04c", "the same with p:EnabledPartView, whose Border is enabled (control)",
            List(TransparentTheme(string.Empty, "<Panel><p:EnabledPartView/>" + Presenter + "</Panel>") + ListStyle("Border:disabled", "Red"), Plain),
            ("EnabledPartView.axaml", SourceView("EnabledPartView.axaml"))),
        C("J05", "row theme Transparent; template Panel: p:Chip beside the presenter; the Chip's implicit theme's template is Border#ChipBg IsEnabled False; list style 'Border:disabled' Red",
            List(TransparentTheme(string.Empty, "<Panel><p:Chip Height=\"10\"/>" + Presenter + "</Panel>") + ChipPartTheme("IsEnabled=\"False\" ")
                 + ListStyle("Border:disabled", "Red"), Plain)),
        C("J05c", "the same with Border#ChipBg enabled (control)",
            List(TransparentTheme(string.Empty, "<Panel><p:Chip Height=\"10\"/>" + Presenter + "</Panel>") + ChipPartTheme(string.Empty)
                 + ListStyle("Border:disabled", "Red"), Plain)),

        // ── An element whose own x:Class markup paints, and which is not a UserControl ────────────
        C("J06", "row theme Transparent; template Panel: p:XClassBorder beside the presenter, a Border whose own x:Class markup sets Background Transparent",
            List(TransparentTheme(string.Empty, "<Panel><p:XClassBorder Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),
        C("J06b", "the same with p:XClassPanel, a Panel whose own x:Class markup sets Background Transparent",
            List(TransparentTheme(string.Empty, "<Panel><p:XClassPanel Height=\"10\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassPanel.axaml", SourceView("XClassPanel.axaml"))),
        C("J06c", "the same with a plain Border (control)",
            List(TransparentTheme(string.Empty, "<Panel><Border Height=\"10\"/>" + Presenter + "</Panel>"), Plain)),

        // ── What a ContentPresenter in the template holds as markup content ───────────────────────
        C("J07", "row theme {x:Null}; template Panel: a ContentPresenter holding a Border with Background {TemplateBinding Background}, beside the presenter",
            List(NullRowTemplate("<Panel><ContentPresenter><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></ContentPresenter>" + Presenter + "</Panel>"), Plain)),
        C("J07b", "the same bound by RelativeSource TemplatedParent",
            List(NullRowTemplate("<Panel><ContentPresenter><Border Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource TemplatedParent}}\"/></ContentPresenter>" + Presenter + "</Panel>"), Plain)),
        C("J07c", "the same Border held by a ContentControl (control)",
            List(NullRowTemplate("<Panel><ContentControl><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
        C("J07d", "the same ContentPresenter's Border bound to $parent[ListBoxItem].Background (control)",
            List(NullRowTemplate("<Panel><ContentPresenter><Border Height=\"10\" Background=\"{Binding $parent[ListBoxItem].Background}\"/></ContentPresenter>" + Presenter + "</Panel>"), Plain)),
        C("J08", "row theme {x:Null}; template: PART_ContentPresenter holding a Border bound to $parent[Control;1].Background",
            List(NullRowTemplate("<ContentPresenter Name=\"PART_ContentPresenter\"><Border Height=\"10\" Background=\"{Binding $parent[Control;1].Background}\"/></ContentPresenter>"), Plain)),
        C("J08b", "the same bound by RelativeSource AncestorType=Control, AncestorLevel=2, Tree=Logical",
            List(NullRowTemplate("<ContentPresenter Name=\"PART_ContentPresenter\"><Border Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=Control, AncestorLevel=2, Tree=Logical}}\"/></ContentPresenter>"), Plain)),
        C("J08c", "the same as J08 with a ContentPresenter not named PART_ContentPresenter (control)",
            List(NullRowTemplate("<ContentPresenter><Border Height=\"10\" Background=\"{Binding $parent[Control;1].Background}\"/></ContentPresenter>"), Plain)),

        // ── A row that starts selected ─────────────────────────────────────────────────────────────
        C("J09", "SelectionMode AlwaysSelected; row theme Transparent, template Panel: Border#Bg beside the presenter; list style 'ListBoxItem:selected /template/ Border#Bg' Red",
            List(BgPanelTheme() + ListStyle("ListBoxItem:selected /template/ Border#Bg", "Red"), Plain)
                .Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" SelectionMode=\"AlwaysSelected\"")),
        C("J09b", "the same with SelectedIndex 0 in place of AlwaysSelected",
            List(BgPanelTheme() + ListStyle("ListBoxItem:selected /template/ Border#Bg", "Red"), Plain)
                .Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" SelectedIndex=\"0\"")),
        C("J09c", "the same with nothing selected (control)",
            List(BgPanelTheme() + ListStyle("ListBoxItem:selected /template/ Border#Bg", "Red"), Plain)),

        // ── Second batch ───────────────────────────────────────────────────────────────────────────
        C("JX01", "J07's template under a row theme whose Background is Transparent: a ContentPresenter holding a Border with {TemplateBinding Background}",
            List(TransparentTheme(string.Empty, "<Panel><ContentPresenter><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></ContentPresenter>" + Presenter + "</Panel>"), Plain)),
        C("JX01c", "the same Border held by a ContentControl (control)",
            List(TransparentTheme(string.Empty, "<Panel><ContentControl><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
        C("JX02", "row theme {x:Null}; template Panel: a Border bound by RelativeSource Mode=Self with AncestorType=ListBoxItem, beside the presenter",
            List(NullRowTemplate("<Panel><Border Height=\"10\" Background=\"{Binding Background, RelativeSource={RelativeSource Mode=Self, AncestorType=ListBoxItem}}\"/>" + Presenter + "</Panel>"), Plain)),
    ];
}
