using System.Reflection;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>The tests that capture the process's console, which no other test may write to meanwhile.</summary>
[CollectionDefinition(nameof(ConsoleCapture), DisableParallelization = true)]
public sealed class ConsoleCapture;

/// <summary>
/// The <c>theme-audit</c> tool's outputs and drift checks, <c>report</c>, <c>compat</c>, <c>parts</c> and <c>brand</c>,
/// by their exit codes and messages, over copies of the committed fixtures under <c>Fixtures/audit</c> and
/// <c>Fixtures/compat</c>, a control library emitted at test time, and a palette and a brand template written here.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A consumer's CI branches on these.</b> The exit code says up to date, drift or an unreadable file, and a
/// configuration error; the message says which file and what to run. Nothing tested the first two before the drift
/// check moved into the library, so their tests were written first, against the tool as it was, to hold both through
/// the move.
/// </para>
/// <para>
/// ⚠ <b>Run in this process, not as one of its own.</b> A redirected console on Windows writes in its code page, which
/// turns the messages' em dash into a hyphen; the tool's own entry point, with the console captured, prints what a
/// person reads. The capture is the process's, so the class runs alone.
/// </para>
/// </remarks>
[Collection(nameof(ConsoleCapture))]
public sealed class ThemeAuditToolTests : IDisposable
{
    private const string AuditSummary = "2 themes, 2 consumers, 5 undefined-key findings, 2 low-contrast findings";

    private const string CompatSummary = "4 mapped, 12 copied, 0 literal (review), 1 restored, 10 skipped";

    private readonly string _root = Directory.CreateTempSubdirectory("theme-tool-").FullName;

    public ThemeAuditToolTests()
    {
        Copy(FixturePaths.Fixture("audit"), Path.Combine(_root, "audit"));
        Copy(FixturePaths.Fixture("compat"), Path.Combine(_root, "compat"));
        File.WriteAllText(
            Path.Combine(_root, "compat", "theme-audit.json"),
            """
            {
              "themes": [
                { "name": "From", "entry": "From.axaml", "baseDirectory": "." },
                { "name": "To", "entry": "To.axaml", "baseDirectory": ".", "inheritance": { "Vivid": "Dark" } }
              ],
              "compat": [
                { "name": "Compat", "from": "From", "to": "To", "mapping": "mapping.json", "output": "out/Compat.axaml" }
              ]
            }
            """);

        Directory.CreateDirectory(Path.Combine(_root, "parts", "themes"));
        File.WriteAllText(
            Path.Combine(_root, "parts", "themes", "Gauge.axaml"),
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:f="clr-namespace:Fixtures">
              <ControlTheme x:Key="GaugeTheme" TargetType="f:Gauge">
                <Setter Property="Template"><ControlTemplate><Border Name="PART_Needle" AutomationProperties.AutomationId="Needle" /></ControlTemplate></Setter>
              </ControlTheme>
            </ResourceDictionary>
            """);
        File.WriteAllText(
            PartsConfig,
            $$"""
            {
              "parts": { "report": "docs/template-parts.md", "markup": ["themes"], "assemblies": ["{{EmittedParts.Assembly.Replace('\\', '/')}}"] }
            }
            """);

        Directory.CreateDirectory(Path.Combine(_root, "brand", "theme"));
        File.WriteAllText(
            Path.Combine(_root, "brand", "theme", "Palette.axaml"),
            """
            <ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="GroundBrush" Color="#454545" />
              <SolidColorBrush x:Key="AccentBrush" Color="#4CAF50" />
            </ResourceDictionary>
            """);
        File.WriteAllText(BrandTemplate, "# Brand\n\nThe accent is {{tok AccentBrush}}, at {{cr AccentBrush on GroundBrush}} on the ground.\n");
        File.WriteAllText(
            BrandConfigPath,
            """
            {
              "themes": [ { "name": "App", "entry": "theme/Palette.axaml", "baseDirectory": "theme" } ],
              "brand": [ { "template": "Brand.template.md", "output": "docs/Brand.md", "theme": "App" } ]
            }
            """);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private string AuditConfig => Path.Combine(_root, "audit", "theme-audit.json");

    private string Report => Path.Combine(_root, "audit", "docs", "theme-audit.md");

    private string CompatConfig => Path.Combine(_root, "compat", "theme-audit.json");

    private string CompatOutput => Path.Combine(_root, "compat", "out", "Compat.axaml");

    private string PartsConfig => Path.Combine(_root, "parts", "theme-audit.json");

    private string PartsReport => Path.Combine(_root, "parts", "docs", "template-parts.md");

    private string BrandConfigPath => Path.Combine(_root, "brand", "theme-audit.json");

    private string BrandTemplate => Path.Combine(_root, "brand", "Brand.template.md");

    private string BrandDocument => Path.Combine(_root, "brand", "docs", "Brand.md");

    // ── report --check ──────────────────────────────────────────────────────────

    [Fact]
    public void Report_writes_the_report_and_says_where()
    {
        Assert.Equal(
            (0, $"theme-audit: wrote {Report} ({AuditSummary}).\n", ""),
            Run("report", "--config", AuditConfig));
        Assert.True(File.Exists(Report));
    }

    /// <summary>Regenerating a stale report removes the fresh copy the check left beside it, which the report now is.</summary>
    [Fact]
    public void Report_regenerated_after_a_stale_check_removes_the_received_copy()
    {
        string received = Path.Combine(_root, "audit", "docs", "theme-audit.received.md");
        Run("report", "--config", AuditConfig);
        File.AppendAllText(Report, "edited\n");
        Run("report", "--check", "--config", AuditConfig);
        Assert.True(File.Exists(received));

        Run("report", "--config", AuditConfig);

        Assert.False(File.Exists(received));
    }

    /// <summary>
    /// A received copy in a consumer's folder, as a failing <c>compat --check</c> leaves where a consumer keeps its compat
    /// dictionary, is not read as the consumer's markup, so the report a later check compares is unchanged.
    /// </summary>
    [Fact]
    public void Report_does_not_read_a_received_copy_in_a_consumers_folder()
    {
        Run("report", "--config", AuditConfig);
        string themes = Path.Combine(_root, "audit", "app", "Themes");
        File.Copy(Path.Combine(themes, "App.axaml"), Path.Combine(themes, "App.received.axaml"));

        Assert.Equal(
            (0, $"theme-audit: {Report} is up to date ({AuditSummary}).\n", ""),
            Run("report", "--check", "--config", AuditConfig));
    }

    [Fact]
    public void Report_check_without_a_report_exits_1_and_says_how_to_create_it()
    {
        Assert.Equal(
            (1, "", $"theme-audit: {Report} does not exist; run `theme-audit report` to create it.\n"),
            Run("report", "--check", "--config", AuditConfig));
    }

    [Fact]
    public void Report_check_on_a_fresh_report_exits_0()
    {
        Run("report", "--config", AuditConfig);

        Assert.Equal(
            (0, $"theme-audit: {Report} is up to date ({AuditSummary}).\n", ""),
            Run("report", "--check", "--config", AuditConfig));
    }

    /// <summary>A checkout that writes CRLF, as git does on Windows, is not drift.</summary>
    [Fact]
    public void Report_check_reads_a_CRLF_copy_as_up_to_date()
    {
        Run("report", "--config", AuditConfig);
        File.WriteAllText(Report, File.ReadAllText(Report).Replace("\n", "\r\n", StringComparison.Ordinal));

        Assert.Equal(0, Run("report", "--check", "--config", AuditConfig).ExitCode);
    }

    [Fact]
    public void Report_check_on_a_stale_report_exits_1_and_says_how_to_regenerate_it()
    {
        Run("report", "--config", AuditConfig);
        File.AppendAllText(Report, "edited\n");

        Assert.Equal(
            (1, "", $"theme-audit: {Report} is stale — it differs from a fresh run ({AuditSummary}); regenerate it with `theme-audit report`.\n"),
            Run("report", "--check", "--config", AuditConfig));
    }

    [Fact]
    public void A_configuration_that_does_not_exist_exits_2()
    {
        string missing = Path.Combine(_root, "audit", "nope.json");

        Assert.Equal(
            (2, "", $"theme-audit: configuration file not found: {missing}\n"),
            Run("report", "--check", "--config", missing));
    }

    /// <summary>A null where the configuration expects a value is a configuration error, not an exception that exits 1.</summary>
    [Fact]
    public void A_configuration_that_sets_a_section_to_null_exits_2_and_names_it()
    {
        File.WriteAllText(AuditConfig, """{ "themes": null }""");

        Assert.Equal(
            (2, "", "theme-audit: 'themes' must be a list of themes\n"),
            Run("report", "--check", "--config", AuditConfig));
    }

    // ── compat --check ──────────────────────────────────────────────────────────

    [Fact]
    public void Compat_writes_each_dictionary_and_says_what_it_holds()
    {
        Assert.Equal(
            (0,
                $"theme-audit: wrote out/Compat.axaml — {CompatSummary}.\n"
                + "  skipped 1 key(s): ControlTheme is not copied by policy\n"
                + "  skipped 1 key(s): mapped to NoSuchTarget, which the target theme does not define under this variant\n"
                + "  skipped 1 key(s): mapped to ToInk, a brush, but the source defines a colour; map a colour key to a colour key\n"
                + "  skipped 1 key(s): references Nope which nothing defines, and it is not a colour\n"
                + "  skipped 1 key(s): references NopeOpacity which nothing defines, and it is not a colour\n",
                ""),
            Run("compat", "--config", CompatConfig));
        Assert.True(File.Exists(CompatOutput));
    }

    [Fact]
    public void Compat_check_without_the_dictionary_exits_1_and_says_how_to_regenerate_it()
    {
        Assert.Equal(
            (1, "", $"theme-audit: out/Compat.axaml is stale or missing — {CompatSummary}; regenerate with `theme-audit compat`.\n"),
            Run("compat", "--check", "--config", CompatConfig));
    }

    [Fact]
    public void Compat_check_on_a_fresh_dictionary_exits_0()
    {
        Run("compat", "--config", CompatConfig);

        Assert.Equal(
            (0, $"theme-audit: out/Compat.axaml is up to date — {CompatSummary}.\n", ""),
            Run("compat", "--check", "--config", CompatConfig));
    }

    [Fact]
    public void Compat_check_on_a_stale_dictionary_exits_1()
    {
        Run("compat", "--config", CompatConfig);
        File.AppendAllText(CompatOutput, "<!-- edited -->\n");

        Assert.Equal(
            (1, "", $"theme-audit: out/Compat.axaml is stale or missing — {CompatSummary}; regenerate with `theme-audit compat`.\n"),
            Run("compat", "--check", "--config", CompatConfig));
    }

    /// <summary>Regenerating a stale dictionary removes the fresh copy the check left beside it, which the dictionary now is.</summary>
    [Fact]
    public void Compat_regenerated_after_a_stale_check_removes_the_received_copy()
    {
        string received = Path.Combine(_root, "compat", "out", "Compat.received.axaml");
        Run("compat", "--config", CompatConfig);
        File.AppendAllText(CompatOutput, "<!-- edited -->\n");
        Run("compat", "--check", "--config", CompatConfig);
        Assert.True(File.Exists(received));

        Run("compat", "--config", CompatConfig);

        Assert.False(File.Exists(received));
    }

    [Fact]
    public void Compat_with_no_dictionaries_configured_exits_2()
    {
        Assert.Equal(
            (2, "", "theme-audit: the configuration declares no compat dictionaries.\n"),
            Run("compat", "--check", "--config", AuditConfig));
    }

    // ── parts --check ───────────────────────────────────────────────────────────

    private const string PartsSummary = "1 control(s), 1 lookup(s), 0 not read";

    [Fact]
    public void Parts_writes_the_report_and_says_where()
    {
        Assert.Equal(
            (0, $"theme-audit: wrote docs/template-parts.md ({PartsSummary}).\n", ""),
            Run("parts", "--config", PartsConfig));

        string report = File.ReadAllText(PartsReport);
        Assert.Contains("## `Gauge`\n\nTheme: `themes/Gauge.axaml`, line 2.\n", report, StringComparison.Ordinal);
        Assert.Contains("| `PART_Needle` | `Border` | `Needle` | ✓ | — |\n", report, StringComparison.Ordinal);
        Assert.Contains("| `PART_Dial` | **missing** |  |  | ✓ |\n", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Parts_check_without_a_report_exits_1_and_says_how_to_create_it()
    {
        Assert.Equal(
            (1, "", "theme-audit: docs/template-parts.md does not exist; run `theme-audit parts` to create it.\n"),
            Run("parts", "--check", "--config", PartsConfig));
    }

    [Fact]
    public void Parts_check_on_a_fresh_report_exits_0()
    {
        Run("parts", "--config", PartsConfig);

        Assert.Equal(
            (0, $"theme-audit: docs/template-parts.md is up to date ({PartsSummary}).\n", ""),
            Run("parts", "--check", "--config", PartsConfig));
    }

    /// <summary>On drift the fresh report lands beside the committed one, where the failure can be read or taken.</summary>
    [Fact]
    public void Parts_check_on_a_stale_report_exits_1_and_writes_the_fresh_one_beside_it()
    {
        Run("parts", "--config", PartsConfig);
        string fresh = File.ReadAllText(PartsReport);
        File.AppendAllText(PartsReport, "edited\n");

        Assert.Equal(
            (1, "", $"theme-audit: docs/template-parts.md is stale — it differs from a fresh run ({PartsSummary}); regenerate it with `theme-audit parts`.\n"),
            Run("parts", "--check", "--config", PartsConfig));
        Assert.Equal(fresh, File.ReadAllText(Path.Combine(_root, "parts", "docs", "template-parts.received.md")));
    }

    /// <summary>Regenerating a stale parts report removes the fresh copy the check left beside it, which the report now is.</summary>
    [Fact]
    public void Parts_regenerated_after_a_stale_check_removes_the_received_copy()
    {
        string received = Path.Combine(_root, "parts", "docs", "template-parts.received.md");
        Run("parts", "--config", PartsConfig);
        File.AppendAllText(PartsReport, "edited\n");
        Run("parts", "--check", "--config", PartsConfig);
        Assert.True(File.Exists(received));

        Run("parts", "--config", PartsConfig);

        Assert.False(File.Exists(received));
    }

    [Fact]
    public void Parts_with_no_parts_report_configured_exits_2()
    {
        Assert.Equal(
            (2, "", "theme-audit: the configuration declares no parts report.\n"),
            Run("parts", "--check", "--config", AuditConfig));
    }

    [Fact]
    public void A_parts_report_with_no_markup_is_a_configuration_error()
    {
        File.WriteAllText(PartsConfig, """{ "parts": { "report": "docs/template-parts.md", "assemblies": ["x.dll"] } }""");

        Assert.Equal(
            (2, "", "theme-audit: 'parts' needs 'report', 'markup' and 'assemblies'\n"),
            Run("parts", "--config", PartsConfig));
    }

    // ── brand --check ───────────────────────────────────────────────────────────

    private const string BrandSummary = "4 lines";

    [Fact]
    public void Brand_writes_each_document_and_says_where()
    {
        Assert.Equal(
            (0, $"theme-audit: wrote docs/Brand.md ({BrandSummary}).\n", ""),
            Run("brand", "--config", BrandConfigPath));

        Assert.Equal(
            "<!-- Generated from Brand.template.md by the theme audit's brand output. Edit the template, then regenerate this file. -->\n"
            + "# Brand\n\nThe accent is #FF4CAF50, at 3.4:1 on the ground.\n",
            File.ReadAllText(BrandDocument));
    }

    [Fact]
    public void Brand_check_without_the_document_exits_1_and_says_how_to_create_it()
    {
        Assert.Equal(
            (1, "", "theme-audit: docs/Brand.md does not exist; run `theme-audit brand` to create it.\n"),
            Run("brand", "--check", "--config", BrandConfigPath));
    }

    [Fact]
    public void Brand_check_on_a_fresh_document_exits_0()
    {
        Run("brand", "--config", BrandConfigPath);

        Assert.Equal(
            (0, $"theme-audit: docs/Brand.md is up to date ({BrandSummary}).\n", ""),
            Run("brand", "--check", "--config", BrandConfigPath));
    }

    /// <summary>On drift the fresh document lands beside the committed one, and writing it again removes that copy.</summary>
    [Fact]
    public void Brand_check_on_a_stale_document_exits_1_and_writes_the_fresh_one_beside_it()
    {
        string received = Path.Combine(_root, "brand", "docs", "Brand.received.md");
        Run("brand", "--config", BrandConfigPath);
        string fresh = File.ReadAllText(BrandDocument);
        File.AppendAllText(BrandDocument, "edited\n");

        Assert.Equal(
            (1, "", $"theme-audit: docs/Brand.md is stale — it differs from a fresh run ({BrandSummary}); regenerate it with `theme-audit brand`.\n"),
            Run("brand", "--check", "--config", BrandConfigPath));
        Assert.Equal(fresh, File.ReadAllText(received));

        Run("brand", "--config", BrandConfigPath);

        Assert.False(File.Exists(received));
    }

    /// <summary>Every problem of every document is listed, and no document is written, nor a received copy left.</summary>
    [Fact]
    public void Brand_whose_claims_fail_lists_every_problem_exits_1_and_writes_nothing()
    {
        File.WriteAllText(BrandTemplate, "{{cr AccentBrush on GroundBrush <3}}\n{{tok Nope}}\n");

        Assert.Equal(
            (1,
                "",
                "theme-audit: docs/Brand.md cannot be filled — 2 problem(s):\n"
                + "  Brand.template.md:1: {{cr AccentBrush on GroundBrush <3}} is 3.45:1\n"
                + "  Brand.template.md:2: {{tok Nope}}: Nope names no token\n"
                + "theme-audit: nothing was written.\n"),
            Run("brand", "--check", "--config", BrandConfigPath));
        Assert.Equal(1, Run("brand", "--config", BrandConfigPath).ExitCode);
        Assert.False(Directory.Exists(Path.Combine(_root, "brand", "docs")));
    }

    /// <summary>Every document is filled before any is written, so one that cannot be filled leaves the others unwritten too.</summary>
    [Fact]
    public void Brand_with_one_document_that_cannot_be_filled_writes_none_of_them()
    {
        File.WriteAllText(Path.Combine(_root, "brand", "Bad.template.md"), "{{tok Nope}}\n");
        File.WriteAllText(
            BrandConfigPath,
            """
            {
              "themes": [ { "name": "App", "entry": "theme/Palette.axaml", "baseDirectory": "theme" } ],
              "brand": [
                { "template": "Brand.template.md", "output": "docs/Brand.md", "theme": "App" },
                { "template": "Bad.template.md", "output": "docs/Bad.md", "theme": "App" }
              ]
            }
            """);

        Assert.Equal(
            (1,
                "",
                "theme-audit: docs/Bad.md cannot be filled — 1 problem(s):\n"
                + "  Bad.template.md:1: {{tok Nope}}: Nope names no token\n"
                + "theme-audit: nothing was written.\n"),
            Run("brand", "--config", BrandConfigPath));
        Assert.False(Directory.Exists(Path.Combine(_root, "brand", "docs")));
    }

    [Fact]
    public void Brand_with_no_documents_configured_exits_2()
    {
        Assert.Equal(
            (2, "", "theme-audit: the configuration declares no brand documents.\n"),
            Run("brand", "--check", "--config", AuditConfig));
    }

    [Fact]
    public void A_brand_document_of_an_unknown_theme_is_a_configuration_error()
    {
        File.WriteAllText(BrandConfigPath, """{ "brand": [ { "template": "Brand.template.md", "output": "docs/Brand.md", "theme": "Nope" } ] }""");

        Assert.Equal(
            (2, "", "theme-audit: brand 'docs/Brand.md' names an unknown theme 'Nope'\n"),
            Run("brand", "--config", BrandConfigPath));
    }

    /// <summary>
    /// Runs the tool's entry point with the console captured, and returns its exit code and what it wrote, each line
    /// ending in LF whatever the platform's line ending is.
    /// </summary>
    private static (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        MethodInfo entry = Assembly.Load("ThemeAudit").EntryPoint
                           ?? throw new InvalidOperationException("ThemeAudit.dll has no entry point.");

        TextWriter output = Console.Out;
        TextWriter error = Console.Error;
        using StringWriter capturedOutput = new();
        using StringWriter capturedError = new();
        Console.SetOut(capturedOutput);
        Console.SetError(capturedError);
        try
        {
            int exitCode = (int)entry.Invoke(null, [args])!;
            return (exitCode, Lf(capturedOutput.ToString()), Lf(capturedError.ToString()));
        }
        finally
        {
            Console.SetOut(output);
            Console.SetError(error);
        }
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static void Copy(string from, string to)
    {
        foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
