using System.Reflection;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>The tests that capture the process's console, which no other test may write to meanwhile.</summary>
[CollectionDefinition(nameof(ConsoleCapture), DisableParallelization = true)]
public sealed class ConsoleCapture;

/// <summary>
/// The <c>theme-audit</c> tool's drift checks, <c>report --check</c> and <c>compat --check</c>, by their exit codes and
/// messages, over copies of the committed fixtures under <c>Fixtures/audit</c> and <c>Fixtures/compat</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A consumer's CI branches on these.</b> The exit code says up to date, drift or an unreadable file, and a
/// configuration error; the message says which file and what to run. Nothing tested either before the drift check moved
/// into the library, so these were written first, against the tool as it was, to hold both through the move.
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

    private const string CompatSummary = "4 mapped, 12 copied, 2 literal (review), 1 restored, 8 skipped";

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
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private string AuditConfig => Path.Combine(_root, "audit", "theme-audit.json");

    private string Report => Path.Combine(_root, "audit", "docs", "theme-audit.md");

    private string CompatConfig => Path.Combine(_root, "compat", "theme-audit.json");

    private string CompatOutput => Path.Combine(_root, "compat", "out", "Compat.axaml");

    // ── report --check ──────────────────────────────────────────────────────────

    [Fact]
    public void Report_writes_the_report_and_says_where()
    {
        Assert.Equal(
            (0, $"theme-audit: wrote {Report} ({AuditSummary}).\n", ""),
            Run("report", "--config", AuditConfig));
        Assert.True(File.Exists(Report));
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
                + "  skipped 1 key(s): references Nope which nothing defines, and it is not a colour\n",
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

    [Fact]
    public void Compat_with_no_dictionaries_configured_exits_2()
    {
        Assert.Equal(
            (2, "", "theme-audit: the configuration declares no compat dictionaries.\n"),
            Run("compat", "--check", "--config", AuditConfig));
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
