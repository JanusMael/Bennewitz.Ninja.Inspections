using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.Rules;
using Xunit;

namespace XamlQuality.Tests.Rules;

/// <summary><see cref="InputTooltipRule"/> against markup written for the test.</summary>
public sealed class InputTooltipRuleTests : IDisposable
{
    private readonly string _root;

    public InputTooltipRuleTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "xq-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) { Directory.Delete(_root, recursive: true); }
    }

    private void Write(string name, string markup) =>
        File.WriteAllText(Path.Combine(_root, name), markup);

    private XamlRuleResult Run(params string[] additional)
        => new InputTooltipRule(additional).Analyze(XamlScanContext.Load(_root));

    private const string NamespaceHeader =
        "xmlns=\"https://github.com/avaloniaui\" " +
        "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

    private const string WpfHeader =
        "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
        "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

    [Fact]
    public void AButtonWithNoTooltip_IsReported()
    {
        Write("Bad.axaml", $"<UserControl {NamespaceHeader}><Button x:Name=\"Save\" Content=\"Save\" /></UserControl>");

        XamlRuleResult result = Run();

        Assert.Equal(1, result.Inspected);
        XamlFinding finding = Assert.Single(result.Findings);
        Assert.Equal("BNXQ1011", finding.RuleId);
        Assert.Equal(1, finding.Line);
        Assert.Contains("Button (Save) has no tooltip", finding.Message, StringComparison.Ordinal);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void EveryInputControl_IsInspected()
    {
        string[] controls =
        [
            "AutoCompleteBox", "Button", "CalendarDatePicker", "CheckBox", "ComboBox", "DatePicker", "DropDownButton",
            "NumericUpDown", "RadioButton", "RepeatButton", "Slider", "SplitButton", "TextBox", "TimePicker",
            "ToggleButton", "ToggleSplitButton", "ToggleSwitch",
        ];
        Write("All.axaml", $"<StackPanel {NamespaceHeader}>" + string.Concat(controls.Select(c => $"<{c} />")) + "</StackPanel>");

        XamlRuleResult result = Run();

        Assert.Equal(controls.Length, result.Inspected);
        Assert.Equal(controls.Length, result.Findings.Count);
    }

    [Fact]
    public void ATooltipInEitherSpelling_Counts()
    {
        Write("Good.axaml",
            $"<StackPanel {NamespaceHeader}>"
            + "<Button ToolTip.Tip=\"Save the file\" />"
            + "<Button ToolTip.Tip=\"{Binding SaveHint}\" />"
            + "<CheckBox><ToolTip.Tip>Wrap long lines</ToolTip.Tip></CheckBox>"
            + "<Slider><ToolTip.Tip><TextBlock Text=\"Zoom\" /></ToolTip.Tip></Slider>"
            + "</StackPanel>");

        XamlRuleResult result = Run();

        Assert.Empty(result.Findings);
        Assert.Equal(4, result.Inspected);
    }

    [Fact]
    public void WpfAndMauiSpellings_Count()
    {
        Write("Wpf.xaml",
            $"<StackPanel {WpfHeader}>"
            + "<Button ToolTip=\"Save the file\" />"
            + "<Button ToolTipService.ToolTip=\"Save the file\" />"
            + "<CheckBox><CheckBox.ToolTip>Wrap long lines</CheckBox.ToolTip></CheckBox>"
            + "</StackPanel>");
        Write("Maui.xaml",
            "<VerticalStackLayout xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\">"
            + "<Button ToolTipProperties.Text=\"Save the file\" />"
            + "</VerticalStackLayout>");

        XamlRuleResult result = Run();

        Assert.Empty(result.Findings);
        Assert.Equal(4, result.Inspected);
    }

    [Fact]
    public void AnEmptyOrBlankTooltip_IsReportedAsEmpty()
    {
        Write("Blank.axaml",
            $"<StackPanel {NamespaceHeader}><Button ToolTip.Tip=\"\" /><Button ToolTip.Tip=\"  \" /></StackPanel>");

        XamlRuleResult result = Run();

        Assert.Equal(2, result.Inspected);
        Assert.All(result.Findings, finding => Assert.Contains("declares an empty tooltip", finding.Message, StringComparison.Ordinal));
        Assert.Equal(2, result.Findings.Count);
    }

    [Fact]
    public void AnEmptyPropertyElement_IsNoTooltip()
    {
        Write("Empty.axaml", $"<StackPanel {NamespaceHeader}><Button><ToolTip.Tip></ToolTip.Tip></Button><Button><ToolTip.Tip /></Button></StackPanel>");

        XamlRuleResult result = Run();

        Assert.Equal(2, result.Findings.Count);
        Assert.All(result.Findings, finding => Assert.Contains("has no tooltip", finding.Message, StringComparison.Ordinal));
    }

    /// <summary>A <c>ToolTip</c> control as content is not a tooltip, and an automation name or id is not one either.</summary>
    [Fact]
    public void WhatIsNotATooltip_DoesNotCount()
    {
        Write("Other.axaml",
            $"<StackPanel {NamespaceHeader}>"
            + "<Button AutomationProperties.Name=\"Save\" AutomationProperties.AutomationId=\"SaveButton\" />"
            + "<Button><ToolTip>Save</ToolTip></Button>"
            + "</StackPanel>");

        XamlRuleResult result = Run();

        Assert.Equal(2, result.Findings.Count);
    }

    [Fact]
    public void AControlInAThemesTemplate_IsChecked()
    {
        Write("Theme.axaml",
            $"<ResourceDictionary {NamespaceHeader}>"
            + "<ControlTheme x:Key=\"Find\" TargetType=\"TemplatedControl\"><Setter Property=\"Template\"><ControlTemplate>"
            + "<StackPanel><TextBox Name=\"PART_Query\" /><Button ToolTip.Tip=\"Next match\" /></StackPanel>"
            + "</ControlTemplate></Setter></ControlTheme>"
            + "</ResourceDictionary>");

        XamlRuleResult result = Run();

        Assert.Equal(2, result.Inspected);
        XamlFinding finding = Assert.Single(result.Findings);
        Assert.Contains("TextBox (PART_Query)", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A style that gives a tooltip, by any spelling of its setter, to controls of the type, all of them or some,
    /// leaves the type's controls with none of their own in <c>Skipped</c>, and touches no other type.
    /// </summary>
    [Fact]
    public void AStyleThatMayGiveTheTooltip_LeavesTheControlSkipped()
    {
        Write("Styles.axaml",
            $"<Styles {NamespaceHeader}>"
            + "<Style Selector=\"Button.icon\"><Setter Property=\"(ToolTip.Tip)\" Value=\"Icon\" /></Style>"
            + "<Style Selector=\"CheckBox\"><Setter><Setter.Property>ToolTip.Tip</Setter.Property><Setter.Value>Tick</Setter.Value></Setter></Style>"
            + "</Styles>");
        Write("View.axaml",
            $"<StackPanel {NamespaceHeader}>"
            + "<Button Classes=\"icon\" /><CheckBox /><CheckBox ToolTip.Tip=\"Own\" /><TextBox />"
            + "</StackPanel>");

        XamlRuleResult result = Run();

        Assert.Equal(2, result.Skipped.Count);
        Assert.Contains(result.Skipped, skip => skip.Subject == "Button" && skip.Reason.Contains("Styles.axaml:1", StringComparison.Ordinal));
        Assert.Contains(result.Skipped, skip => skip.Subject == "CheckBox");
        Assert.Equal(2, result.Inspected);
        XamlFinding finding = Assert.Single(result.Findings);
        Assert.Contains("TextBox", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A theme for the type that gives a tooltip, itself or in a nested style, and a WPF style by its target type.</summary>
    [Fact]
    public void AThemeOrAWpfStyleThatMayGiveTheTooltip_LeavesTheControlSkipped()
    {
        Write("Theme.axaml",
            $"<ResourceDictionary {NamespaceHeader}>"
            + "<ControlTheme x:Key=\"{x:Type ToggleButton}\" TargetType=\"ToggleButton\"><Setter Property=\"ToolTip.Tip\" Value=\"Toggle\" /></ControlTheme>"
            + "<ControlTheme x:Key=\"Pin\" TargetType=\"Slider\"><Style Selector=\"^:pointerover\"><Setter Property=\"ToolTip.Tip\" Value=\"Zoom\" /></Style></ControlTheme>"
            + "</ResourceDictionary>");
        Write("Wpf.xaml",
            $"<ResourceDictionary {WpfHeader}><Style TargetType=\"RadioButton\"><Setter Property=\"ToolTip\" Value=\"Pick\" /></Style></ResourceDictionary>");
        Write("View.axaml", $"<StackPanel {NamespaceHeader}><ToggleButton /><Slider /><RadioButton /><Button /></StackPanel>");

        XamlRuleResult result = Run();

        Assert.Equal(["RadioButton", "Slider", "ToggleButton"], result.Skipped.Select(skip => skip.Subject).Order(StringComparer.Ordinal));
        XamlFinding finding = Assert.Single(result.Findings);
        Assert.Contains("Button has no tooltip", finding.Message, StringComparison.Ordinal);
        Assert.Equal(1, result.Inspected);
    }

    [Fact]
    public void AStyleForAnotherProperty_ChangesNothing()
    {
        Write("Styles.axaml",
            $"<Styles {NamespaceHeader}><Style Selector=\"Button\"><Setter Property=\"Background\" Value=\"Red\" /></Style></Styles>");
        Write("View.axaml", $"<StackPanel {NamespaceHeader}><Button /></StackPanel>");

        XamlRuleResult result = Run();

        Assert.Empty(result.Skipped);
        Assert.Single(result.Findings);
    }

    [Fact]
    public void AConsumersOwnControl_IsCheckedWhenNamed()
    {
        Write("View.axaml", $"<StackPanel {NamespaceHeader} xmlns:local=\"using:App\"><local:IconButton /></StackPanel>");

        Assert.Equal(0, Run().Inspected);
        XamlRuleResult result = Run("IconButton");
        Assert.Equal(1, result.Inspected);
        Assert.Single(result.Findings);
    }

    [Fact]
    public void MarkupWithNoInputControl_InspectsNothing()
    {
        Write("Empty.axaml", $"<UserControl {NamespaceHeader}><TextBlock Text=\"Hello\" /><MenuItem Header=\"Open\" /></UserControl>");

        XamlRuleResult result = Run();

        Assert.Equal(0, result.Inspected);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Skipped);
    }
}
