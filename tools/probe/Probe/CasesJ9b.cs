namespace FpProbe6;

internal static partial class Cases
{
    public static readonly Case[] AllJ9 =
    [
        // ── C2: a style gives a template part the row's Background ───────────────────────────────
        C("V901", "row theme Transparent; template Panel#Bg sets none; list style 'ListBoxItem /template/ Panel#Bg' binds $parent[ListBoxItem].Background",
            List(TransparentTheme(string.Empty, BgPanel) + PartStyle("{Binding $parent[ListBoxItem].Background}"), Plain)),
        C("V901b", "the same, the list style binding Background through RelativeSource TemplatedParent",
            List(TransparentTheme(string.Empty, BgPanel) + PartStyle("{Binding Background, RelativeSource={RelativeSource TemplatedParent}}"), Plain)),
        C("V901c", "row theme Transparent; template Panel#Bg sets none; no style (control)",
            List(TransparentTheme(string.Empty, BgPanel), Plain)),
        C("V902", "row theme Transparent; its nested '^ /template/ Panel#Bg' sets {TemplateBinding Background}",
            List(TransparentTheme(NestedPartStyle("{TemplateBinding Background}"), BgPanel), Plain)),
        C("V903", "row theme Transparent; its nested '^ /template/ Panel#Bg' binds $parent[ListBoxItem].Background",
            List(TransparentTheme(NestedPartStyle("{Binding $parent[ListBoxItem].Background}"), BgPanel), Plain)),

        // ── C1: a binding that names an ancestor of the row's type at another level ──────────────
        C("V904", "row theme {x:Null}; template Border binds $parent[ListBoxItem;1].Background (the second ListBoxItem up)",
            List(NullRowTemplate("<Border Background=\"{Binding $parent[ListBoxItem;1].Background}\">" + Presenter + "</Border>"), Plain)),
        C("V905", "row theme {x:Null}; template Border binds Background of RelativeSource AncestorType=ListBoxItem, AncestorLevel=2",
            List(NullRowTemplate("<Border Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, AncestorLevel=2}}\">" + Presenter + "</Border>"), Plain)),
        C("V906", "V904's inner list inside an outer list's explicit row whose Background is Transparent",
            Nested(NullRowTemplate("<Border Background=\"{Binding $parent[ListBoxItem;1].Background}\">" + Presenter + "</Border>"))),
        C("V906b", "V905's inner list inside an outer list's explicit row whose Background is Transparent",
            Nested(NullRowTemplate("<Border Background=\"{Binding Background, RelativeSource={RelativeSource AncestorType=ListBoxItem, AncestorLevel=2}}\">" + Presenter + "</Border>"))),
        C("V906c", "an inner list with RowTheme's TemplateBinding inside the same outer row (control)",
            Nested(RowTheme(string.Empty))),

        // ── C1: the host's theme paints the rows ─────────────────────────────────────────────────
        C("V907", "row theme {x:Null}, Border takes it; the list's own Theme, BasedOn the framework's, nests '^ ListBoxItem' Transparent",
            List(RowTheme(string.Empty) + "<ListBox.Theme><ControlTheme TargetType=\"ListBox\" BasedOn=\"{StaticResource {x:Type ListBox}}\">"
                 + "<Style Selector=\"^ ListBoxItem\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style></ControlTheme></ListBox.Theme>", Plain)),

        // ── C1: templated controls the rule reads by the default template ────────────────────────
        C("V908", "row theme {x:Null}; template is a ContentControl taking the row's Background, Content and ContentTemplate",
            List(NullRowTemplate("<ContentControl Background=\"{TemplateBinding Background}\" Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"/>"), Plain)),
        C("V908b", "row theme {x:Null}; template Panel: a UserControl taking the row's Background, beside the presenter",
            List(NullRowTemplate("<Panel><UserControl Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain)),
        C("V918", "row theme {x:Null}; template Panel: p:TintView, whose own markup gives its root Background Transparent, taking the row's",
            List(NullRowTemplate("<Panel><p:TintView Background=\"{TemplateBinding Background}\"/>" + Presenter + "</Panel>"), Plain),
            ("TintView.axaml", SourceView("TintView.axaml"))),

        // ── C1: a disabled list, whose rows start disabled ───────────────────────────────────────
        C("V917", "IsEnabled False on the list; row theme {x:Null}, Border takes it, nested '^:disabled' sets Transparent",
            List(RowTheme("<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)
                .Replace("ItemsSource=\"{Binding Rows}\"", "ItemsSource=\"{Binding Rows}\" IsEnabled=\"False\"")),
        C("V917b", "row theme sets IsEnabled False, Background {x:Null}, Border takes it, nested '^:disabled' sets Transparent",
            List(RowTheme("<Setter Property=\"IsEnabled\" Value=\"False\"/><Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),
        C("V917c", "row theme {x:Null}, Border takes it, nested '^:disabled' sets Transparent; list enabled (control)",
            List(RowTheme("<Style Selector=\"^:disabled\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),

        // ── A row theme's nested styles that name a descendant, not /template/ ───────────────────
        C("V920", "row theme Transparent; template Panel#Bg sets none; the theme nests '^ Panel' Transparent",
            List(TransparentTheme("<Style Selector=\"^ Panel\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>", BgPanel), Plain)),
        C("V921", "row theme {x:Null}, Border takes it; the theme nests '^:not(.nope) Border' Transparent",
            List(RowTheme("<Style Selector=\"^:not(.nope) Border\"><Setter Property=\"Background\" Value=\"Transparent\"/></Style>"), Plain)),
    ];
}
