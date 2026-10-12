using System.Windows.Input;

namespace FpProbe6;

/// <summary>Commands that can always, or never, execute, for a button whose command disables what it holds.</summary>
public static class R11Commands
{
    public static ICommand Never { get; } = new Fixed(false);

    public static ICommand Always { get; } = new Fixed(true);

    private sealed class Fixed(bool can) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => can;

        public void Execute(object? parameter)
        {
        }
    }
}

// Round 11's cases for BNXQ1010: the siblings round 11 added, the :selected routing, and what a presenter or a
// TransitioningContentControl in the template holds. A case ending in "c" is a control. Each is measured row by
// row (facts11), since a selection makes the first row differ from the rest.
internal static partial class Cases
{
    private static readonly string FluentPresenter =
        Presenter.Replace("Name=\"PART_ContentPresenter\"", "Name=\"PART_ContentPresenter\" Background=\"{TemplateBinding Background}\"");

    /// <summary>A row theme, Background as given, whose template is <paramref name="template"/>, with nested styles <paramref name="nested"/>.</summary>
    private static string NestedTheme(string background, string template, string nested) => """
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem">
            <Setter Property="Background" Value="BACKGROUND"/>
            <Setter Property="Template"><ControlTemplate>TEMPLATE</ControlTemplate></Setter>
            NESTED
          </ControlTheme>
        </ListBox.ItemContainerTheme>
        """.Replace("BACKGROUND", background).Replace("TEMPLATE", template).Replace("NESTED", nested);

    /// <summary>Fluent's own shape at 12.1.3: a nested '^:selected' holding '^ /template/ ContentPresenter#PART_ContentPresenter'.</summary>
    private const string SelectedPresenterRed =
        "<Style Selector=\"^:selected\"><Style Selector=\"^ /template/ ContentPresenter#PART_ContentPresenter\">"
        + "<Setter Property=\"Background\" Value=\"Red\"/></Style></Style>";

    private static string Selecting(string markup) =>
        markup.Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" SelectedIndex=\"0\"");

    /// <summary>A button, with the given command, holding a list named Inner whose markup is <paramref name="list"/>.</summary>
    private static string InButton(string command, string list) => """
        <Button NAMESPACES Command="{x:Static p:R11Commands.COMMAND}" Width="320" Height="220">
          LIST
        </Button>
        """.Replace("NAMESPACES", Namespaces).Replace("COMMAND", command)
        .Replace("LIST", list.Replace(Namespaces, string.Empty).Replace("<ListBox ", "<ListBox Name=\"Inner\" "));

    public static readonly Case[] AllR11 =
    [
        // ── A part painted only while selected (F6): the rows selected draw there, and no other ─────────
        C("EL01", "SelectedIndex 0; row theme {x:Null}; template PART_ContentPresenter takes it; nested '^:selected' > '^ /template/ PART_ContentPresenter' Red, as Fluent writes it",
            Selecting(List(NestedTheme("{x:Null}", FluentPresenter, SelectedPresenterRed), Plain))),
        C("EL01c", "the same with nothing selected (control)",
            List(NestedTheme("{x:Null}", FluentPresenter, SelectedPresenterRed), Plain)),
        C("EL02", "SelectedIndex 0; row theme Transparent; template PART_ContentPresenter draws nothing; the same nested '^:selected' Red",
            Selecting(List(NestedTheme("Transparent", Presenter, SelectedPresenterRed), Plain))),
        C("EL02c", "the same with nothing selected (control)",
            List(NestedTheme("Transparent", Presenter, SelectedPresenterRed), Plain)),
        C("EL03", "SelectedIndex 0; row theme {x:Null}, Border takes it; list style 'ListBoxItem:not(:selected)' Transparent",
            Selecting(List(RowTheme(string.Empty) + ListStyle("ListBoxItem:not(:selected)", "Transparent"), Plain))),
        C("EL04", "tab control, whose SelectionMode always selects; tab theme {x:Null}, template draws nothing: Panel with Border#Bg; style 'TabItem:selected /template/ Border#Bg' Red",
            TabHost(TabRowTheme("{x:Null}", "<Border Name=\"Bg\" Height=\"10\"/>")
                    + "<TabControl.Styles><Style Selector=\"TabItem:selected /template/ Border#Bg\"><Setter Property=\"Background\" Value=\"Red\"/></Style></TabControl.Styles>", Plain)),
        C("EL04c", "the same with 'TabItem:pointerover' in place of ':selected' (control)",
            TabHost(TabRowTheme("{x:Null}", "<Border Name=\"Bg\" Height=\"10\"/>")
                    + "<TabControl.Styles><Style Selector=\"TabItem:pointerover /template/ Border#Bg\"><Setter Property=\"Background\" Value=\"Red\"/></Style></TabControl.Styles>", Plain)),

        // ── A command that cannot execute disables its button and all it holds ─────────────────────────
        C("EL05", "a Button whose command never executes holds the list; row theme {x:Null}, Border takes it, nested '^:disabled' Transparent",
            InButton("Never", List(RowTheme(DisabledTransparent), Plain))),
        C("EL05c", "the same with a command that always executes (control)",
            InButton("Always", List(RowTheme(DisabledTransparent), Plain))),

        // ── A type's own markup against what the template gives it ─────────────────────────────────────
        C("EL06", "row theme {x:Null}; template Panel: p:TintView, whose own markup is Transparent, bound to $parent[ListBoxItem].Background, beside the presenter",
            List(NullRowTemplate("<Panel><p:TintView Height=\"10\" Background=\"{Binding $parent[ListBoxItem].Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("TintView.axaml", SourceView("TintView.axaml"))),
        C("EL06c", "the same given {TemplateBinding Background} (control)",
            List(NullRowTemplate("<Panel><p:TintView Height=\"10\" Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("TintView.axaml", SourceView("TintView.axaml"))),
        C("EL07", "row theme {x:Null}; template Panel: p:XClassBorder, whose own markup is Transparent, bound to $parent[ListBoxItem].Background, beside the presenter",
            List(NullRowTemplate("<Panel><p:XClassBorder Height=\"10\" Background=\"{Binding $parent[ListBoxItem].Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),
        C("EL07c", "the same given {TemplateBinding Background} (control)",
            List(NullRowTemplate("<Panel><p:XClassBorder Height=\"10\" Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("XClassBorder.axaml", SourceView("XClassBorder.axaml"))),

        // ── What a presenter, or a TransitioningContentControl, in the template holds as markup content ─
        C("EL08", "row theme {x:Null}; template Panel: a ScrollContentPresenter holding a Border with {TemplateBinding Background}, beside the presenter",
            List(NullRowTemplate("<Panel><ScrollContentPresenter><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></ScrollContentPresenter>" + Presenter + "</Panel>"), Plain)),
        C("EL09", "row theme {x:Null}; template Panel: a TransitioningContentControl holding a Border with {TemplateBinding Background}, beside the presenter",
            List(NullRowTemplate("<Panel><TransitioningContentControl><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></TransitioningContentControl>" + Presenter + "</Panel>"), Plain)),
        C("EL09b", "the same under a row theme whose Background is Transparent",
            List(TransparentTheme(string.Empty, "<Panel><TransitioningContentControl><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></TransitioningContentControl>" + Presenter + "</Panel>"), Plain)),
        C("EL09c", "EL09 with a ContentControl in place of the TransitioningContentControl (control)",
            List(NullRowTemplate("<Panel><ContentControl><Border Height=\"10\" Background=\"{TemplateBinding Background}\"/></ContentControl>" + Presenter + "</Panel>"), Plain)),
    ];
}
