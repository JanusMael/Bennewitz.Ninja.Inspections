using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.Rules;

/// <summary>
/// Every input control written in markup declares a tooltip.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A tooltip says what a control does to the person about to use it.</b> An automation name,
/// which <see cref="InteractiveAutomationNameRule"/> requires, reaches a screen reader; an automation
/// id, which <see cref="InteractiveAutomationIdRule"/> requires, reaches a test. Neither is shown to
/// someone pointing at an icon button. A window can pass both rules on every control and still leave
/// that person guessing, which is how this rule was asked for.
/// </para>
/// <para>
/// ⚠ <b>The input controls, by local name, as everywhere else.</b> <c>AutoCompleteBox</c>,
/// <c>Button</c>, <c>CalendarDatePicker</c>, <c>CheckBox</c>, <c>ComboBox</c>, <c>DatePicker</c>,
/// <c>DropDownButton</c>, <c>NumericUpDown</c>, <c>RadioButton</c>, <c>RepeatButton</c>,
/// <c>Slider</c>, <c>SplitButton</c>, <c>TextBox</c>, <c>TimePicker</c>, <c>ToggleButton</c>,
/// <c>ToggleSplitButton</c> and <c>ToggleSwitch</c>. A <c>MenuItem</c> is left out: its header is its
/// label. A consumer's own controls are passed to <see cref="InputTooltipRule(IEnumerable{string})"/>.
/// What counts as a tooltip is in <see cref="Tooltips"/>.
/// </para>
/// <para>
/// ⚠ <b>A control in a theme's template counts.</b> A <c>ControlTheme</c>'s template holds controls a
/// person points at like any other, so its parts are checked where they are written.
/// </para>
/// <para>
/// ⚠ <b>A style may give the tooltip, and the rule does not decide whether it does.</b> A control with
/// no tooltip of its own, that a style in the scan whose setter gives a tooltip names by its type,
/// with or without a class, a name, a state or a context, or that a <c>ControlTheme</c> for its type
/// gives one, is named in <c>Skipped</c>: the selector may or may not hold of it.
/// </para>
/// <para>
/// ⛔ <b>Only markup.</b> A control built in code, or a tooltip set in code, is not seen.
/// </para>
/// </remarks>
public sealed class InputTooltipRule : IXamlRule
{
    private static readonly string[] FrameworkInputElements =
    [
        "AutoCompleteBox", "Button", "CalendarDatePicker", "CheckBox", "ComboBox", "DatePicker",
        "DropDownButton", "NumericUpDown", "RadioButton", "RepeatButton", "Slider", "SplitButton",
        "TextBox", "TimePicker", "ToggleButton", "ToggleSplitButton", "ToggleSwitch",
    ];

    private readonly HashSet<string> _elements;

    /// <summary>Covers the framework's input controls, which the remarks list.</summary>
    public InputTooltipRule()
        : this([])
    {
    }

    /// <summary>Covers the framework's input controls, which the remarks list, and <paramref name="additionalElements"/>.</summary>
    /// <param name="additionalElements">
    /// Element local names of the consumer's own input controls, which no framework list will ever name.
    /// </param>
    public InputTooltipRule(IEnumerable<string> additionalElements)
    {
        ArgumentNullException.ThrowIfNull(additionalElements);
        _elements = new HashSet<string>(FrameworkInputElements, StringComparer.Ordinal);
        _elements.UnionWith(additionalElements);
    }

    /// <inheritdoc />
    public string Id => "BNXQ1011";

    /// <inheritdoc />
    public string Summary => "Every input control written in markup declares a tooltip.";

    /// <inheritdoc />
    public XamlRuleResult Analyze(XamlScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<XElement> tooltipStyles = [.. TooltipStyles(context)];
        List<XamlFinding> findings = [];
        List<XamlSkip> skipped = [];
        int inspected = 0;

        foreach (XamlFile file in context.ParsedFiles)
        {
            foreach (XElement element in file.Document!.Descendants())
            {
                string type = element.Name.LocalName;
                if (!_elements.Contains(type))
                {
                    continue;
                }

                TooltipDeclaration declared = Tooltips.Of(element);
                int? line = (element as IXmlLineInfo).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : null;

                if (declared == TooltipDeclaration.None && tooltipStyles.FirstOrDefault(style => MayGive(style, type)) is { } style)
                {
                    int? styleLine = (style as IXmlLineInfo).HasLineInfo() ? ((IXmlLineInfo)style).LineNumber : null;
                    string where = context.ParsedFiles.First(candidate => candidate.Document == style.Document).RelativePath;
                    skipped.Add(new XamlSkip(
                        $"{type} {NameOf(element)}".TrimEnd(),
                        $"It has no tooltip of its own, and a {style.Name.LocalName} at {where}:{styleLine} gives a tooltip to "
                        + $"some {type} controls, which markup cannot say include this one.",
                        file.RelativePath,
                        line));
                    continue;
                }

                inspected++;

                string? message = declared switch
                {
                    TooltipDeclaration.Declared => null,
                    TooltipDeclaration.Blank =>
                        $"This {type}{Named(element)} declares an empty tooltip, which tells the person pointing at it "
                        + "nothing. Give it a ToolTip.Tip that says what it does.",
                    _ =>
                        $"This {type}{Named(element)} has no tooltip, so nothing tells the person pointing at it what it "
                        + "does. Give it a ToolTip.Tip that says what it does.",
                };

                if (message is not null)
                {
                    findings.Add(new XamlFinding(Id, file.Path, file.RelativePath, line, message));
                }
            }
        }

        return new XamlRuleResult(findings, inspected) { Skipped = skipped };
    }

    /// <summary>Every <c>Style</c> or <c>ControlTheme</c> in the scan with a setter that gives a tooltip.</summary>
    private static IEnumerable<XElement> TooltipStyles(XamlScanContext context)
    {
        HashSet<XElement> found = [];
        foreach (XamlFile file in context.ParsedFiles)
        {
            foreach (XElement setter in file.Document!.Descendants())
            {
                if (ResourceScope.SetterProperty(setter) is { } property
                    && Tooltips.IsTooltipProperty(property)
                    && setter.Ancestors().FirstOrDefault(ancestor => ancestor.Name.LocalName is "Style" or "ControlTheme") is { } style
                    && found.Add(style))
                {
                    yield return style;
                }
            }
        }
    }

    /// <summary>
    /// Whether a style or theme that gives a tooltip may reach a control of the type <paramref name="type"/>: a style
    /// whose selector names it, one nested in a <c>ControlTheme</c> for it, which starts <c>^</c>, a WPF style or a
    /// theme whose <c>TargetType</c> is it.
    /// </summary>
    private static bool MayGive(XElement style, string type)
    {
        if (style.Attribute("Selector")?.Value is not { } selector)
        {
            return string.Equals(ControlThemes.TargetTypeOf(style), type, StringComparison.Ordinal);
        }

        return selector.TrimStart().StartsWith('^')
               && style.Ancestors().FirstOrDefault(ancestor => ancestor.Name.LocalName == "ControlTheme") is { } theme
            ? string.Equals(ControlThemes.TargetTypeOf(theme), type, StringComparison.Ordinal)
            : StyleSelectors.Picks(style, type, [type]) != Pick.No;
    }

    private static string Named(XElement element) => NameOf(element) is { Length: > 0 } name ? $" ({name})" : string.Empty;

    private static string NameOf(XElement element) =>
        element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Name")?.Value ?? string.Empty;
}
