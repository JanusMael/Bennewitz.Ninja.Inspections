using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.Rules;

namespace XamlQuality.Tests.Rules;

/// <summary>
/// <see cref="TemplatePartReading.ToMarkdown"/>, the parts report a consumer commits and holds to a fresh run.
/// </summary>
/// <remarks>
/// ⭐ <b>The format is a contract, so it is held byte for byte.</b> Every consumer regenerates its committed copy when the
/// format changes, and CI runs this on Linux, Windows and macOS, where the report must read the same.
/// </remarks>
public sealed class TemplatePartReportTests
{
    /// <summary>
    /// A reading that fills every column: each origin, a lambda, two lookups that read alike, two themes that declare a
    /// part differently, a theme that declares a part twice, a part no theme declares, a control with no theme, and a
    /// type that could not be read.
    /// </summary>
    private static readonly TemplatePartReading Golden = new(
        [
            new ControlTemplateParts(
                "AlphaBox",
                ["PART_Content"],
                [
                    new TemplatePartLookup("PART_Content", TemplatePartOrigin.Own, "Lib.AlphaBox", "OnApplyTemplate", "Lib"),
                    new TemplatePartLookup("PART_Content", TemplatePartOrigin.Own, "Lib.AlphaBox", "Refresh", "Lib"),
                    new TemplatePartLookup("PART_Popup", TemplatePartOrigin.BaseClass, "Vendor.BaseBox", "OnApplyTemplate", "Vendor"),
                    new TemplatePartLookup("PART_Scroll", TemplatePartOrigin.Own, "Lib.AlphaBox", "<OnApplyTemplate>b__4_0", "Lib"),
                    new TemplatePartLookup("PART_Scroll", TemplatePartOrigin.Helper, "Lib.Wiring+Wirer", "Wire", "Lib"),
                ],
                [
                    new ThemeParts(
                        "Themes/A.axaml",
                        3,
                        [
                            new DeclaredPart("PART_Content", "ContentPresenter", "Content"),
                            new DeclaredPart("PART_Badge", "Border", null),
                            new DeclaredPart("PART_Scroll", "ScrollViewer", "Scroll"),
                            new DeclaredPart("PART_Badge", "Border", "Badge"),
                        ]),
                    new ThemeParts(
                        "Themes/B.axaml",
                        7,
                        [
                            new DeclaredPart("PART_Content", "ContentControl", ""),
                            new DeclaredPart("PART_Scroll", "ScrollViewer", "Scroller"),
                        ]),
                ]),
            new ControlTemplateParts("Lonely", ["PART_Only"], [], []),
        ],
        [new XamlSkip("Thing", "Consumer, which the scan was given, defines it, but it could not be loaded: Dep could not be found.")]);

    [Fact]
    public void TheReport_IsTheGoldenText_ByteForByte()
    {
        string expected =
            """
            # Template parts

            Each control's template parts as `BNXQ1003` reads them: the `PART_` constants its code declares, the parts code looks up on its template, and the parts each of its themes declares. A part the code declares or looks up that a theme does not declare reads **missing** in that theme's column, which is what `BNXQ1003` reports. *Looked up by* reads ✓ for the control's own code, a base class's assembly for that base class's code, a helper's type for code the control hands its template to, and — where nothing looks the part up.

            ## `AlphaBox`

            Themes: 1 `Themes/A.axaml`, line 3; 2 `Themes/B.axaml`, line 7.

            | Part | Element 1 | AutomationId 1 | Element 2 | AutomationId 2 | Constant | Looked up by |
            | --- | --- | --- | --- | --- | --- | --- |
            | `PART_Content` | `ContentPresenter` | `Content` | `ContentControl` | `""` | ✓ | ✓ |
            | `PART_Badge` | `Border` | `Badge` |  |  |  | — |
            | `PART_Scroll` | `ScrollViewer` | `Scroll` | `ScrollViewer` | `Scroller` |  | ✓ `OnApplyTemplate (lambda)`, `Wiring.Wirer` |
            | `PART_Popup` | **missing** |  | **missing** |  |  | Vendor |

            ## `Lonely`

            No theme for it is in the scanned markup.

            | Part | Constant | Looked up by |
            | --- | --- | --- |
            | `PART_Only` | ✓ | — |

            ## Not read

            - `Thing`: Consumer, which the scan was given, defines it, but it could not be loaded: Dep could not be found.

            """;

        Assert.Equal(expected, Golden.ToMarkdown());
    }

    /// <summary>The report is written with LF alone, whatever the platform's line ending is.</summary>
    [Fact]
    public void TheReport_HasNoCarriageReturn()
    {
        Assert.DoesNotContain("\r", Golden.ToMarkdown(), StringComparison.Ordinal);
    }

    /// <summary>One theme takes unnumbered columns; a reading that skipped nothing says so, rather than ending in a bare heading.</summary>
    [Fact]
    public void OneTheme_TakesPlainColumns_AndNothingSkipped_IsSaid()
    {
        TemplatePartReading reading = new(
            [new ControlTemplateParts("Solo", [], [new TemplatePartLookup("PART_Item", TemplatePartOrigin.Own, "Lib.Solo", "OnApplyTemplate", "Lib")], [new ThemeParts("Solo.axaml", null, [])])],
            []);

        string report = reading.ToMarkdown();

        Assert.Contains("Theme: `Solo.axaml`.\n\n| Part | Element | AutomationId | Constant | Looked up by |\n", report, StringComparison.Ordinal);
        Assert.Contains("| `PART_Item` | **missing** |  |  | ✓ |\n", report, StringComparison.Ordinal);
        Assert.EndsWith("## Not read\n\nNothing.\n", report, StringComparison.Ordinal);
    }
}
