using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// The brand output, by plans/00004's decisions 5 to 11: each placeholder of the language, each kind of claim holding
/// and failing, every problem listed together with no document given, a palette's light and dark variants filled in
/// each, its chart framed in the variant's own colours, and blocks given and missing.
/// </summary>
/// <remarks>
/// Every figure here was worked out apart from the code, by the WCAG 2.2 formulas and Avalonia's alpha arithmetic.
/// </remarks>
public sealed class BrandRunnerTests : IDisposable
{
    private const string Palette =
        """
        <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Color x:Key="InkColor">#1C1F23</Color>
          <SolidColorBrush x:Key="GroundBrush" Color="#FF454545" />
          <SolidColorBrush x:Key="AccentBrush" Color="#4CAF50" />
          <SolidColorBrush x:Key="TextBrush" Color="#E5FFFFFF" />
          <SolidColorBrush x:Key="FaintBrush" Color="#FFFFFF" Opacity="0.6" />
          <SolidColorBrush x:Key="InkBrush" Color="{StaticResource InkColor}" />
          <SolidColorBrush x:Key="BoundBrush" Color="{Binding Ink}" />
          <x:Double x:Key="Spacing">4</x:Double>
          <SolidColorBrush x:Key="DimBrush" Color="#FFFFFF" Opacity="0.12" />
          <StaticResource x:Key="AccentAlias" ResourceKey="AccentBrush" />
          <DynamicResource x:Key="InkAlias" ResourceKey="InkColor" />
          <StaticResource x:Key="SpacingAlias" ResourceKey="Spacing" />
          <StaticResource x:Key="DanglingAlias" ResourceKey="Nowhere" />
        </ResourceDictionary>
        """;

    private const string LightAndDark =
        """
        <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
              <SolidColorBrush x:Key="GroundBrush" Color="#FFFFFF" />
              <SolidColorBrush x:Key="StrokeBrush" Color="#C6CACD" />
              <SolidColorBrush x:Key="TextBrush" Color="#1C1F23" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
              <SolidColorBrush x:Key="GroundBrush" Color="#16161A" />
              <SolidColorBrush x:Key="StrokeBrush" Color="#43444A" />
              <SolidColorBrush x:Key="TextBrush" Color="#F9F9F9" />
            </ResourceDictionary>
          </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
        """;

    private const string NoBlockMessage = "the language has no such placeholder, and no block fills it";

    private const string ValMessage = "{{val}} writes one colour, with no opacity: composite a colour with {{mix}}";

    private readonly string _root = Directory.CreateTempSubdirectory("brand-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    // ── the placeholders ───────────────────────────────────────────────────────

    [Fact]
    public void Tok_writes_the_colour_a_token_resolves_to_and_at_where_it_is_declared()
    {
        Assert.Equal(
            "#FF4CAF50 Palette.axaml:4 #FF1C1F23 Palette.axaml:7 #FF1C1F23 #99FFFFFF\n",
            Body(Fill("{{tok AccentBrush}} {{at AccentBrush}} {{tok InkBrush}} {{at InkBrush}} {{tok InkColor}} {{tok FaintBrush}}\n")));
    }

    [Fact]
    public void Val_writes_an_opaque_colour_as_RRGGBB_and_a_translucent_one_with_its_alpha()
    {
        Assert.Equal(
            "#4CAF50 #E5FFFFFF #4CAF50 #80FFFFFF\n",
            Body(Fill("{{val AccentBrush}} {{val TextBrush}} {{val #4caf50}} {{val #80FFFFFF}}\n")));
    }

    /// <summary>BC-2: an opacity is composited, so a <c>{{val}}</c>, which composites nothing, refuses one.</summary>
    [Fact]
    public void Val_takes_no_opacity()
    {
        BrandResult result = Fill("{{val #FFFFFF@0.12}}\n{{val AccentBrush@0.5}}\n");

        string[] expected =
        [
            "Brand.template.md:1: {{val #FFFFFF@0.12}}: " + ValMessage,
            "Brand.template.md:2: {{val AccentBrush@0.5}}: " + ValMessage,
        ];
        Assert.Equal(expected, result.Problems);
        Assert.Equal(string.Empty, result.Markdown);
    }

    [Fact]
    public void Mix_writes_the_opaque_colour_a_stack_draws_as_and_lab_writes_it_for_a_label()
    {
        Assert.Equal(
            "#ECECEC #5B5B5B #4CAF50 #35;ECECEC\n",
            Body(Fill("{{mix TextBrush/GroundBrush}} {{mix #FFFFFF@0.12/GroundBrush}} {{mix AccentBrush}} {{lab TextBrush/GroundBrush}}\n")));
    }

    /// <summary>
    /// Plans/00004's measured cases: a brush at 0.6 inside an element at 0.56 drew 85, one at 0.12 inside 0.99 drew 30,
    /// and one at 0.5 inside 0.99 drew 126, over black. Truncating after each opacity in turn gives 85, 29 and 125. A
    /// token's own opacity is its brush's, so a token at 0.12 drawn inside 0.99 is 30 too.
    /// </summary>
    [Fact]
    public void An_alpha_is_every_opacity_on_the_way_truncated_once()
    {
        Assert.Equal(
            "#555555 #1E1E1E #7E7E7E #555555 #1E1E1E\n",
            Body(Fill(
                "{{mix #FFFFFF@0.6@0.56/#000000}} {{mix #FFFFFF@0.12@0.99/#000000}} {{mix #FFFFFF@0.5@0.99/#000000}} "
                + "{{mix FaintBrush@0.56/#000000}} {{mix DimBrush@0.99/#000000}}\n")));
    }

    [Fact]
    public void Cr_writes_the_contrast_of_colours_drawn_over_a_stack_and_wc_its_verdict()
    {
        Assert.Equal(
            "21.0:1 AAA · 4.5:1 AA · 3.0:1 AA large only · 1.6:1 fails · 8.1:1 AAA\n",
            Body(Fill(
                "{{cr #000000 on #FFFFFF}} {{wc #000000 on #FFFFFF}} · {{cr #767676 on #FFFFFF}} {{wc #767676 on #FFFFFF}} · "
                + "{{cr #949494 on #FFFFFF}} {{wc #949494 on #FFFFFF}} · {{cr #CCCCCC on #FFFFFF}} {{wc #CCCCCC on #FFFFFF}} · "
                + "{{cr TextBrush on GroundBrush}} {{wc TextBrush on GroundBrush}}\n")));
    }

    /// <summary>6.998 rounds to 7.0, a verdict it does not reach, so it keeps two decimals, rounded down; and so for 4.5 and 3.</summary>
    [Fact]
    public void A_figure_is_never_rounded_onto_a_threshold_it_does_not_reach()
    {
        Assert.Equal(
            "6.99:1 AA · 4.47:1 AA large only · 2.96:1 fails\n",
            Body(Fill(
                "{{cr #0740F0 on #FFFFFF}} {{wc #0740F0 on #FFFFFF}} · {{cr #777777 on #FFFFFF}} {{wc #777777 on #FFFFFF}} · "
                + "{{cr #8A8AF8 on #FFFFFF}} {{wc #8A8AF8 on #FFFFFF}}\n")));
    }

    [Fact]
    public void A_claim_that_holds_writes_its_figure_and_one_that_fails_is_a_problem()
    {
        Assert.Equal(
            "4.47:1 AA large only #4CAF50\n",
            Body(Fill("{{cr #777777 on #FFFFFF <4.5}} {{wc #777777 on #FFFFFF >=3}} {{mix AccentBrush =#4caf50}}\n")));

        BrandResult result = Fill("{{cr #777777 on #FFFFFF >=4.5}}\n{{wc #777777 on #FFFFFF <3}}\n{{mix AccentBrush =#4CAF51}}\n");

        string[] expected =
        [
            "Brand.template.md:1: {{cr #777777 on #FFFFFF >=4.5}} is 4.48:1",
            "Brand.template.md:2: {{wc #777777 on #FFFFFF <3}} is 4.48:1",
            "Brand.template.md:3: {{mix AccentBrush =#4CAF51}} draws as #4CAF50",
        ];
        Assert.Equal(expected, result.Problems);
    }

    [Fact]
    public void Near_writes_the_line_holding_a_phrase_and_a_range_between_two()
    {
        Write(
            "src/Thing.cs",
            """
            // one
            int alpha = 1;
            // three
            string pin = "Include='X'";
            int omega = 2;
            """);

        Assert.Equal(
            "src/Thing.cs:2 src/Thing.cs:2-5 src/Thing.cs:4\n",
            Body(Fill("""{{near src/Thing.cs "alpha"}} {{near src/Thing.cs "alpha" "omega"}} {{near src/Thing.cs '"Include'}}""" + "\n")));
    }

    [Fact]
    public void Near_looks_for_its_path_in_each_folder_in_order()
    {
        Write("first/Thing.cs", "a\nmarker\n");
        Write("second/Thing.cs", "marker\n");
        Write("second/Other.cs", "x\ny\nmarker\n");

        Assert.Equal(
            "Thing.cs:2 Other.cs:3\n",
            Body(Fill("""{{near Thing.cs "marker"}} {{near Other.cs "marker"}}""" + "\n", brand: """, "near": ["first", "second"]""")));
    }

    [Fact]
    public void A_phrase_on_no_line_or_on_two_a_range_backwards_and_a_path_that_is_not_there_are_problems()
    {
        Write("src/Thing.cs", "one\ntwo\ntwo\nthree\n");

        BrandResult result = Fill(
            """
            {{near src/Thing.cs "four"}}
            {{near src/Thing.cs "two"}}
            {{near src/Thing.cs "three" "one"}}
            {{near src/Nope.cs "one"}}
            {{near /src/Thing.cs "one"}}

            """);

        string[] expected =
        [
            """Brand.template.md:1: {{near src/Thing.cs "four"}}: src/Thing.cs holds 'four' on 0 lines, where it must be one""",
            """Brand.template.md:2: {{near src/Thing.cs "two"}}: src/Thing.cs holds 'two' on 2 lines, where it must be one""",
            """Brand.template.md:3: {{near src/Thing.cs "three" "one"}}: in src/Thing.cs, 'one' comes before 'three'""",
            """Brand.template.md:4: {{near src/Nope.cs "one"}}: there is no src/Nope.cs in .""",
            """Brand.template.md:5: {{near /src/Thing.cs "one"}}: /src/Thing.cs is not relative, and the document holds no absolute path""",
        ];
        Assert.Equal(expected, result.Problems);
    }

    [Fact]
    public void Pin_writes_the_version_the_nearest_props_at_or_above_the_configuration_pins_and_claims_it()
    {
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="Semi.Avalonia" Version="12.1.0.1" /></ItemGroup></Project>""");

        Assert.Equal("12.1.0.1\n", Body(Fill("{{pin Semi.Avalonia 12.1.0.1}}\n", directory: "app")));

        BrandResult result = Fill("{{pin Semi.Avalonia 12.2.0}}\n{{pin Nope 1.0}}\n", directory: "app");

        string[] expected =
        [
            "Brand.template.md:1: {{pin Semi.Avalonia 12.2.0}}: ../Directory.Packages.props pins Semi.Avalonia 12.1.0.1",
            "Brand.template.md:2: {{pin Nope 1.0}}: ../Directory.Packages.props pins no Nope",
        ];
        Assert.Equal(expected, result.Problems);
    }

    [Fact]
    public void A_package_pinned_twice_or_a_props_that_does_not_parse_is_a_problem()
    {
        Write(
            "Directory.Packages.props",
            """<Project><ItemGroup><PackageVersion Include="Semi.Avalonia" Version="1.0" /><PackageVersion Include="semi.avalonia" Version="2.0" /></ItemGroup></Project>""");
        string[] twice = ["Brand.template.md:1: {{pin Semi.Avalonia 1.0}}: Directory.Packages.props pins Semi.Avalonia 2 times, where it must be once"];
        Assert.Equal(twice, Fill("{{pin Semi.Avalonia 1.0}}\n").Problems);

        Write("Directory.Packages.props", "<Project><ItemGroup>");
        BrandResult result = Fill("{{pin Semi.Avalonia 1.0}}\n");
        Assert.StartsWith("Brand.template.md:1: {{pin Semi.Avalonia 1.0}}: Directory.Packages.props is not well-formed XML: ", Assert.Single(result.Problems), StringComparison.Ordinal);
    }

    [Fact]
    public void Pin_reads_the_nearest_props()
    {
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="Semi.Avalonia" Version="1.0" /></ItemGroup></Project>""");
        Write("app/Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="Semi.Avalonia" Version="2.0" /></ItemGroup></Project>""");

        Assert.Equal("2.0\n", Body(Fill("{{pin Semi.Avalonia 2.0}}\n", directory: "app")));
    }

    // ── the chart ──────────────────────────────────────────────────────────────

    [Fact]
    public void Swatches_draw_each_token_over_the_frames_ground_labelled_with_its_key_and_colour()
    {
        const string twoTokens =
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="GroundBrush" Color="#FF454545" />
              <SolidColorBrush x:Key="TextBrush" Color="#E5FFFFFF" />
            </ResourceDictionary>
            """;

        Assert.Equal(
            """
            ```mermaid
            ---
            config:
              themeVariables:
                titleColor: "#F9F9F9"
            ---
            flowchart TB
                subgraph palpanel ["the palette's tokens,<br/>on the window's ground"]
                    direction TB
                    subgraph prow0 ["Palette.axaml:2-3"]
                        direction LR
                        t_GroundBrush["Ground<br/>#35;454545"] ~~~ t_TextBrush["Text<br/>#35;E5FFFFFF"]
                    end
                end
                style palpanel fill:#454545,stroke:#5B5B5B,color:#F9F9F9
                style prow0 fill:#454545,stroke:#454545,color:#F9F9F9
                style t_GroundBrush fill:#454545,stroke:#5B5B5B,color:#FFFFFF
                style t_TextBrush fill:#ECECEC,stroke:#5B5B5B,color:#000000
            ```

            """,
            Body(Fill("{{swatches}}\n", brand: Frame("GroundBrush", "#5B5B5B", "#F9F9F9"), palette: twoTokens)));
    }

    /// <summary>Rows unlinked stack in whatever order the layout picks, so each row is linked to the next.</summary>
    [Fact]
    public void Swatches_put_four_to_a_row_file_by_file_in_the_order_the_theme_reads_them()
    {
        Write(
            "theme/Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="/Second.axaml" />
                <ResourceInclude Source="/First.axaml" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);
        Write(
            "theme/Second.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="S1" Color="#111111" />
              <SolidColorBrush x:Key="S2" Color="#222222" />
              <SolidColorBrush x:Key="S3" Color="#333333" />
              <SolidColorBrush x:Key="S4" Color="#444444" />
              <SolidColorBrush x:Key="S5" Color="#555555" />
            </ResourceDictionary>
            """);
        Write(
            "theme/First.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="F1" Color="#FFFFFF" />
            </ResourceDictionary>
            """);

        string chart = Body(Fill("{{swatches}}\n", brand: Frame("#000000", "#5B5B5B", "#F9F9F9"), entry: "theme/Index.axaml"));

        Assert.Contains(
            """
                    subgraph prow0 ["Second.axaml:2-5"]
                        direction LR
                        t_S1["S1<br/>#35;111111"] ~~~ t_S2["S2<br/>#35;222222"] ~~~ t_S3["S3<br/>#35;333333"] ~~~ t_S4["S4<br/>#35;444444"]
                    end
                    subgraph prow1 ["Second.axaml:6"]
                        direction LR
                        t_S5["S5<br/>#35;555555"]
                    end
                    subgraph prow2 ["First.axaml:2"]
                        direction LR
                        t_F1["F1<br/>#35;FFFFFF"]
                    end
                    prow0 ~~~ prow1
                    prow1 ~~~ prow2
                end
            """,
            chart,
            StringComparison.Ordinal);
    }

    /// <summary>BC-3 and BC-4: one template, filled in each variant, takes that variant's colours, its chart's frame included.</summary>
    [Fact]
    public void A_palette_with_light_and_dark_variants_is_filled_in_each_and_its_chart_framed_in_that_variants_colours()
    {
        const string template = "{{tok TextBrush}} {{at TextBrush}} {{cr TextBrush on GroundBrush}}\n{{swatches}}\n";
        string frame = Frame("GroundBrush", "StrokeBrush", "TextBrush");

        string light = Body(Fill(template, brand: frame + """, "variant": "Light" """, palette: LightAndDark));
        string dark = Body(Fill(template, brand: frame + """, "variant": "Dark" """, palette: LightAndDark));

        Assert.StartsWith("#FF1C1F23 Palette.axaml:6 16.5:1\n", light, StringComparison.Ordinal);
        Assert.Contains("    titleColor: \"#1C1F23\"\n", light, StringComparison.Ordinal);
        Assert.Contains("subgraph prow0 [\"Palette.axaml:4-6\"]", light, StringComparison.Ordinal);
        Assert.Contains("    style palpanel fill:#FFFFFF,stroke:#C6CACD,color:#1C1F23\n", light, StringComparison.Ordinal);
        Assert.Contains("    style t_TextBrush fill:#1C1F23,stroke:#C6CACD,color:#FFFFFF\n", light, StringComparison.Ordinal);

        Assert.StartsWith("#FFF9F9F9 Palette.axaml:11 17.1:1\n", dark, StringComparison.Ordinal);
        Assert.Contains("    titleColor: \"#F9F9F9\"\n", dark, StringComparison.Ordinal);
        Assert.Contains("subgraph prow0 [\"Palette.axaml:9-11\"]", dark, StringComparison.Ordinal);
        Assert.Contains("    style palpanel fill:#16161A,stroke:#43444A,color:#F9F9F9\n", dark, StringComparison.Ordinal);
        Assert.Contains("    style t_TextBrush fill:#F9F9F9,stroke:#43444A,color:#000000\n", dark, StringComparison.Ordinal);
    }

    /// <summary>A mermaid id takes letters, digits and underscores, so two keys may read as one id; each keeps one of its own.</summary>
    [Fact]
    public void Swatches_give_keys_that_read_as_one_id_an_id_each()
    {
        const string twins =
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="Ink.Primary" Color="#111111" />
              <SolidColorBrush x:Key="Ink_Primary" Color="#222222" />
            </ResourceDictionary>
            """;

        string chart = Body(Fill("{{swatches}}\n", brand: Frame("#FFFFFF", "#5B5B5B", "#1C1F23"), palette: twins));

        Assert.Contains("t_Ink_Primary[\"Ink.Primary<br/>#35;111111\"] ~~~ t_Ink_Primary_2[\"Ink_Primary<br/>#35;222222\"]", chart, StringComparison.Ordinal);
        Assert.Contains("    style t_Ink_Primary fill:#111111,", chart, StringComparison.Ordinal);
        Assert.Contains("    style t_Ink_Primary_2 fill:#222222,", chart, StringComparison.Ordinal);
    }

    [Fact]
    public void Swatches_with_no_frame_or_one_whose_ground_draws_translucent_are_a_problem()
    {
        string[] noFrame = ["Brand.template.md:1: {{swatches}}: the document declares no frame to draw its chart in"];
        Assert.Equal(noFrame, Fill("{{swatches}}\n").Problems);

        string[] translucent =
        [
            "Brand.template.md:1: {{swatches}}: the frame's ground: the bottom of a stack must draw opaque, and TextBrush draws as #E5FFFFFF",
        ];
        Assert.Equal(translucent, Fill("{{swatches}}\n", brand: Frame("TextBrush", "#5B5B5B", "#F9F9F9")).Problems);
    }

    // ── tokens, problems and blocks ────────────────────────────────────────────

    [Fact]
    public void A_key_that_names_no_token_or_a_token_that_resolves_to_no_colour_is_a_problem()
    {
        BrandResult result = Fill("{{tok Nope}} {{tok Spacing}}\n{{mix BoundBrush}}\n");

        string[] expected =
        [
            "Brand.template.md:1: {{tok Nope}}: Nope names no token",
            "Brand.template.md:1: {{tok Spacing}}: Spacing names no token",
            "Brand.template.md:2: {{mix BoundBrush}}: BoundBrush resolves to no colour",
        ];
        Assert.Equal(expected, result.Problems);
    }

    [Fact]
    public void The_tokens_are_the_keyed_brushes_and_colours_of_the_files_listed()
    {
        Write(
            "theme/Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="/Palette.axaml" />
                <ResourceInclude Source="/Extra.axaml" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);
        Write(
            "theme/Extra.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="ExtraBrush" Color="#123456" />
            </ResourceDictionary>
            """);

        Assert.Equal("#FF123456\n", Body(Fill("{{tok ExtraBrush}}\n", entry: "theme/Index.axaml")));

        string[] expected = ["Brand.template.md:1: {{tok ExtraBrush}}: ExtraBrush names no token"];
        Assert.Equal(expected, Fill("{{tok ExtraBrush}}\n", brand: """, "tokens": ["theme/Palette.axaml"]""", entry: "theme/Index.axaml").Problems);
    }

    /// <summary>A token's key may be an alias, followed; an alias of a number is no token, and one of nothing resolves to no colour.</summary>
    [Fact]
    public void An_alias_of_a_colour_is_a_token_and_an_alias_of_anything_else_is_not()
    {
        Assert.Equal("#FF4CAF50 #FF1C1F23 #ECECEC\n", Body(Fill("{{tok AccentAlias}} {{tok InkAlias}} {{mix TextBrush/GroundBrush}}\n")));

        string[] expected =
        [
            "Brand.template.md:1: {{tok SpacingAlias}}: SpacingAlias names no token",
            "Brand.template.md:1: {{tok DanglingAlias}}: DanglingAlias resolves to no colour",
        ];
        Assert.Equal(expected, Fill("{{tok SpacingAlias}} {{tok DanglingAlias}}\n").Problems);
    }

    /// <summary>
    /// A file of the theme that cannot be read would leave its tokens out of the document unseen; an include of another
    /// assembly's theme, which the audit does not follow, is no problem.
    /// </summary>
    [Fact]
    public void A_theme_file_that_cannot_be_read_is_a_problem_and_an_include_of_another_assembly_is_not()
    {
        Write(
            "theme/Index.axaml",
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="/Palette.axaml" />
                <ResourceInclude Source="/Broken.axaml" />
                <ResourceInclude Source="/Missing.axaml" />
                <ResourceInclude Source="avares://Other.Assembly/Themes/Index.axaml" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);
        Write("theme/Broken.axaml", "<ResourceDictionary><SolidColorBrush");

        BrandResult result = Fill("{{tok AccentBrush}}\n", entry: "theme/Index.axaml");

        Assert.Equal(2, result.Problems.Count);
        Assert.StartsWith("theme 'App': theme/Broken.axaml cannot be read: not well-formed XML: ", result.Problems[0], StringComparison.Ordinal);
        Assert.Equal("theme 'App': theme/Index.axaml includes /Missing.axaml, which cannot be read: file not found", result.Problems[1]);
        Assert.Equal(string.Empty, result.Markdown);

        // Listed as a token file, the one that cannot be read is the same problem, not a configuration error.
        Assert.Equal(result.Problems, Fill("{{tok AccentBrush}}\n", brand: """, "tokens": ["theme/Palette.axaml", "theme/Broken.axaml"]""", entry: "theme/Index.axaml").Problems);
    }

    /// <summary>A misspelt variant would be served as Default, and fill a document of another variant's colours.</summary>
    [Fact]
    public void A_variant_the_theme_does_not_declare_is_a_configuration_error_unless_it_is_one_of_Avalonias_own()
    {
        AuditConfigException error = Assert.Throws<AuditConfigException>(
            () => Fill("{{tok TextBrush}}\n", brand: """, "variant": "Drak" """, palette: LightAndDark));

        Assert.Equal("brand 'docs/Brand.md': theme 'App' declares no variant 'Drak', which is not Default, Light or Dark either", error.Message);
        Assert.Equal("#FF4CAF50\n", Body(Fill("{{tok AccentBrush}}\n", brand: """, "variant": "Dark" """)));
    }

    [Fact]
    public void A_brand_entry_with_an_empty_path_or_frame_colour_is_a_configuration_error()
    {
        string[] entries =
        [
            """{ "template": "t.md", "output": "docs/Brand.md", "theme": "App", "tokens": [null] }""",
            """{ "template": "t.md", "output": "docs/Brand.md", "theme": "App", "near": [""] }""",
            """{ "template": "t.md", "output": "docs/Brand.md", "theme": "App", "frame": { "ground": "", "stroke": "#5B5B5B", "text": "#F9F9F9" } }""",
        ];
        string[] messages =
        [
            "brand 'docs/Brand.md' names an empty path in 'tokens' or 'near'",
            "brand 'docs/Brand.md' names an empty path in 'tokens' or 'near'",
            "brand 'docs/Brand.md' needs a 'frame' with a 'ground', a 'stroke' and a 'text'",
        ];

        string[] thrown =
        [
            .. entries.Select(entry => Assert.Throws<AuditConfigException>(() => AuditConfig.Parse(
                """{ "themes": [ { "name": "App", "entry": "theme/Palette.axaml", "baseDirectory": "theme" } ], "brand": [ """ + entry + " ] }",
                _root)).Message),
        ];
        Assert.Equal(messages, thrown);
    }

    [Fact]
    public void A_token_file_the_theme_does_not_read_is_a_configuration_error()
    {
        Write("theme/Stray.axaml", Palette);

        AuditConfigException error = Assert.Throws<AuditConfigException>(() => Fill("\n", brand: """, "tokens": ["theme/Stray.axaml"]"""));
        AuditConfigException missing = Assert.Throws<AuditConfigException>(() => Fill("\n", brand: """, "tokens": ["theme/Nope.axaml"]"""));

        Assert.Equal("brand 'docs/Brand.md': token file theme/Stray.axaml is not a file theme 'App' reads", error.Message);
        Assert.Equal("brand 'docs/Brand.md': token file theme/Nope.axaml does not exist", missing.Message);
    }

    [Fact]
    public void A_brand_document_of_an_unknown_theme_is_a_configuration_error()
    {
        AuditConfigException error = Assert.Throws<AuditConfigException>(() => AuditConfig.Parse(
            """{ "brand": [ { "template": "t.md", "output": "docs/Brand.md", "theme": "Nope" } ] }""",
            _root));

        Assert.Equal("brand 'docs/Brand.md' names an unknown theme 'Nope'", error.Message);
    }

    /// <summary>One run shows every problem, in the template's order, and a document with one is never given.</summary>
    [Fact]
    public void Every_problem_is_listed_together_with_its_line_and_no_document_is_given()
    {
        BrandResult result = Fill(
            """
            # Brand
            {{cr AccentBrush on GroundBrush <3}}
            {{tok Nope}} and {{tok Spacing}}
            {{HUE_TABLE}}
            {{val #FFFFFF@0.12}}
            {{mix TextBrush}}
            {{mix #FFFFFF@1.5/GroundBrush}}
            {{tok AccentBrush}
            {{cr AccentBrush}}

            """);

        string[] expected =
        [
            "Brand.template.md:2: {{cr AccentBrush on GroundBrush <3}} is 3.45:1",
            "Brand.template.md:3: {{tok Nope}}: Nope names no token",
            "Brand.template.md:3: {{tok Spacing}}: Spacing names no token",
            "Brand.template.md:4: {{HUE_TABLE}}: " + NoBlockMessage,
            "Brand.template.md:5: {{val #FFFFFF@0.12}}: " + ValMessage,
            "Brand.template.md:6: {{mix TextBrush}}: the bottom of a stack must draw opaque, and TextBrush draws as #E5FFFFFF",
            "Brand.template.md:7: {{mix #FFFFFF@1.5/GroundBrush}}: '1.5' is no opacity, which is a number from 0 to 1",
            "Brand.template.md:8: '{{' opens no placeholder that closes on its line",
            "Brand.template.md:9: {{cr AccentBrush}}: expected {{cr FG on STACK}}, with an optional <N or >=N",
        ];
        Assert.Equal(expected, result.Problems);
        Assert.Equal(string.Empty, result.Markdown);
    }

    /// <summary>A block is the caller's own rendering: it replaces its placeholder verbatim and is never read as the language.</summary>
    [Fact]
    public void A_block_fills_the_placeholder_it_names_verbatim()
    {
        Dictionary<string, string> blocks = new(StringComparer.Ordinal)
        {
            ["HUE_TABLE"] = "| hue |\r\n|---|\r\n| {{tok AccentBrush}} |",
            ["huename dark"] = "yellow Mid",
        };

        Assert.Equal(
            "| hue |\n|---|\n| {{tok AccentBrush}} |\nThe accent is yellow Mid, and the brand's #FF4CAF50.\n",
            Body(Fill("{{HUE_TABLE}}\nThe accent is {{huename dark}}, and the brand's {{tok AccentBrush}}.\n", blocks: blocks)));
    }

    [Fact]
    public void A_block_nothing_names_and_one_named_as_the_languages_own_are_problems()
    {
        Dictionary<string, string> blocks = new(StringComparer.Ordinal)
        {
            ["UNUSED"] = "x",
            ["mix hue:dark"] = "#FFEA00",
            ["swatches"] = "a chart",
        };

        BrandResult result = Fill("{{mix hue:dark}}\n", blocks: blocks);

        string[] expected =
        [
            "Brand.template.md:1: {{mix hue:dark}}: hue:dark names no token",
            "block 'UNUSED' fills nothing: the template has no {{UNUSED}}",
            "block 'mix hue:dark' is named as one of the language's own placeholders, which the language fills",
            "block 'swatches' is named as one of the language's own placeholders, which the language fills",
        ];
        Assert.Equal(expected, result.Problems);
    }

    [Fact]
    public void The_document_starts_with_a_line_naming_its_template_and_holds_no_carriage_return()
    {
        Assert.Equal(
            "<!-- Generated from Brand.template.md by the theme audit's brand output. Edit the template, then regenerate this file. -->\n"
            + "# Brand\n\n#FF4CAF50\n",
            Fill("# Brand\r\n\r\n{{tok AccentBrush}}\r\n").Markdown);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills <paramref name="template"/> as the one brand document of a configuration in <paramref name="directory"/>,
    /// whose theme <c>App</c> is <paramref name="palette"/> written at <c>theme/Palette.axaml</c>, its entry
    /// <paramref name="entry"/>; <paramref name="brand"/> adds properties to the document's entry.
    /// </summary>
    private BrandResult Fill(
        string template,
        string brand = "",
        IReadOnlyDictionary<string, string>? blocks = null,
        string palette = Palette,
        string entry = "theme/Palette.axaml",
        string directory = "")
    {
        string configDirectory = Path.Combine(_root, directory);
        Write(Path.Combine(directory, "theme", "Palette.axaml"), palette);
        Write(Path.Combine(directory, "Brand.template.md"), template);
        AuditConfig config = AuditConfig.Parse(
            $$"""
            {
              "themes": [ { "name": "App", "entry": "{{entry}}", "baseDirectory": "theme" } ],
              "brand": [ { "template": "Brand.template.md", "output": "docs/Brand.md", "theme": "App"{{brand}} } ]
            }
            """,
            configDirectory);
        return BrandRunner.Fill(config, config.Brand[0], blocks);
    }

    private static string Frame(string ground, string stroke, string text) =>
        $$""", "frame": { "ground": "{{ground}}", "stroke": "{{stroke}}", "text": "{{text}}" }""";

    /// <summary>The document without its first line, which names the template; there must be no problem.</summary>
    private static string Body(BrandResult result)
    {
        Assert.True(result.Problems.Count == 0, string.Join(Environment.NewLine, result.Problems));
        return result.Markdown[(result.Markdown.IndexOf('\n', StringComparison.Ordinal) + 1)..];
    }

    private void Write(string relative, string content)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
