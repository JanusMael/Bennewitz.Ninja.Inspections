namespace FpProbe6;

internal static partial class Cases
{
    private static string HostStyle(string host, string selector) =>
        "<HOST.Styles><Style Selector=\"SELECTOR\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></HOST.Styles>"
            .Replace("HOST", host).Replace("SELECTOR", selector);

    public static readonly Case[] AllJ9c =
    [
        // ── Selectors through the host, for a combo and a tab control (false negatives if they miss) ─
        C("V910", "combo: row theme {x:Null}, Border takes it; combo style 'ComboBox ComboBoxItem' sets Transparent",
            ComboHost(NullRowsFor("ComboBox", "ComboBoxItem") + HostStyle("ComboBox", "ComboBox ComboBoxItem"), Plain)),
        C("V910b", "combo: the same with 'ComboBox > ComboBoxItem'",
            ComboHost(NullRowsFor("ComboBox", "ComboBoxItem") + HostStyle("ComboBox", "ComboBox &gt; ComboBoxItem"), Plain)),
        C("V911", "tabs: row theme {x:Null}, Border takes it; tab control style 'TabControl TabItem' sets Transparent",
            TabHost(NullRowsFor("TabControl", "TabItem") + HostStyle("TabControl", "TabControl TabItem"), Plain)),
        C("V911b", "tabs: the same with 'TabControl > TabItem'",
            TabHost(NullRowsFor("TabControl", "TabItem") + HostStyle("TabControl", "TabControl &gt; TabItem"), Plain)),

        // ── A view's own markup outranks what the template gives it ──────────────────────────────
        C("V918b", "row theme Transparent; template Panel: p:TintView given Background {x:Null}, beside the presenter; nothing else paints",
            List(TransparentTheme(string.Empty, "<Panel><p:TintView Background=\"{x:Null}\"/>" + Presenter + "</Panel>"), Plain),
            ("TintView.axaml", SourceView("TintView.axaml"))),
        C("V918c", "row theme {x:Null}; template Panel: a plain UserControl (no markup of its own) taking the row's Background (control for V918)",
            List(NullRowTemplate("<Panel><UserControl Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain)),
    ];
}
