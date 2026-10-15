using System.CommandLine;
using Bennewitz.Ninja.XamlQuality.Rules;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;

RootCommand root = new("theme-audit — finds the resource keys an Avalonia theme leaves undefined and the tokens below a contrast floor.");

// ---- inventory: the quick per-directory key count -------------------------------------------

Argument<DirectoryInfo> pathArgument = new("path")
{
    Description = "Directory containing .axaml/.xaml files (a theme checkout, or a project's templates).",
};
pathArgument.AcceptExistingOnly();

Command inventory = new("inventory", "List the resource keys (x:Key) defined under a directory, per file and in total.");
inventory.Arguments.Add(pathArgument);
inventory.SetAction(parseResult =>
{
    DirectoryInfo directory = parseResult.GetValue(pathArgument)!;

    IReadOnlyList<ResourceKey> keys;
    try
    {
        keys = ResourceKeyScanner.Scan(directory.FullName);
    }
    catch (InvalidDataException ex)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 1;
    }

    foreach (IGrouping<string, ResourceKey> file in keys.GroupBy(k => k.File).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        Console.WriteLine($"{file.Count(),6}  {Path.GetRelativePath(directory.FullName, file.Key)}");
    }

    Console.WriteLine();
    Console.WriteLine($"{keys.Count} keys, {keys.Select(k => k.Key).Distinct(StringComparer.Ordinal).Count()} unique, in {keys.Select(k => k.File).Distinct(StringComparer.Ordinal).Count()} files under {directory.FullName}");
    return 0;
});

root.Subcommands.Add(inventory);

// ---- shared options ---------------------------------------------------------------------------

Option<FileInfo> configOption = new("--config", "-c")
{
    Description = "The audit configuration (themes, consumers, compat).",
    DefaultValueFactory = _ => new FileInfo("theme-audit.json"),
};

Option<bool> checkOption = new("--check")
{
    Description = "Compare each committed output with a fresh run instead of writing it; exit 1 when one is stale or missing, with a stale one's fresh output written beside it as name.received.ext (the drift check).",
};

// ---- compat: the generated dictionaries -----------------------------------------------------

Command compat = new("compat", "Generate the configured compat dictionaries: the keys one theme defines that another lacks, mapped onto the latter's tokens.");
compat.Options.Add(configOption);
compat.Options.Add(checkOption);
compat.SetAction(parseResult =>
{
    FileInfo configFile = parseResult.GetValue(configOption)!;
    bool check = parseResult.GetValue(checkOption);

    try
    {
        AuditConfig config = AuditConfig.Load(configFile.FullName);
        if (config.Compat.Count == 0)
        {
            Console.Error.WriteLine("theme-audit: the configuration declares no compat dictionaries.");
            return 2;
        }

        Dictionary<string, ThemeTarget> themes = new(StringComparer.Ordinal);
        int stale = 0;
        foreach (CompatConfig entry in config.Compat)
        {
            ThemeTarget from = themes.TryGetValue(entry.From, out ThemeTarget? f) ? f : themes[entry.From] = AuditRunner.BuildTheme(config, config.Theme(entry.From));
            ThemeTarget to = themes.TryGetValue(entry.To, out ThemeTarget? t) ? t : themes[entry.To] = AuditRunner.BuildTheme(config, config.Theme(entry.To));
            CompatMapping mapping = CompatMapping.Load(CompatMapping.Locate(config, entry.Mapping));
            CompatGeneration generation = CompatGenerator.Generate(from.Inventory, to.Inventory, mapping, entry.From, entry.To, entry.VariantKeys);
            string output = config.Resolve(entry.Output);

            string summary = Summarize(generation);
            if (check)
            {
                if (OutputDrift.Check(output, generation.Xml) != DriftState.UpToDate)
                {
                    Console.Error.WriteLine($"theme-audit: {config.Relative(output)} is stale or missing — {summary}; regenerate with `theme-audit compat`.");
                    stale++;
                }
                else
                {
                    Console.WriteLine($"theme-audit: {config.Relative(output)} is up to date — {summary}.");
                }

                continue;
            }

            OutputDrift.Write(output, generation.Xml);
            Console.WriteLine($"theme-audit: wrote {config.Relative(output)} — {summary}.");
            foreach (IGrouping<string, CompatSkipped> group in generation.Skipped.GroupBy(s => s.Reason, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                Console.WriteLine($"  skipped {group.Select(s => s.Key).Distinct(StringComparer.Ordinal).Count()} key(s): {group.Key}");
            }
        }

        return stale == 0 ? 0 : 1;
    }
    catch (AuditConfigException ex)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 2;
    }
    catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 1;
    }
});

root.Subcommands.Add(compat);

// ---- report: the configured audit -----------------------------------------------------------

Option<string?> outputOption = new("--output", "-o")
{
    Description = "Where to write the report; the configuration's 'report' path when omitted.",
};

Command report = new("report", "Inventory the configured themes, scan the consumers, and write the Markdown report.");
report.Options.Add(configOption);
report.Options.Add(outputOption);
report.Options.Add(checkOption);
report.SetAction(parseResult =>
{
    FileInfo configFile = parseResult.GetValue(configOption)!;
    string? outputOverride = parseResult.GetValue(outputOption);
    bool check = parseResult.GetValue(checkOption);

    try
    {
        AuditConfig config = AuditConfig.Load(configFile.FullName);
        AuditResult result = AuditRunner.Run(config);
        string markdown = MarkdownReport.Render(result);
        string output = outputOverride is not null ? Path.GetFullPath(outputOverride) : config.Resolve(config.Report);

        int undefined = result.Findings.Sum(f => f.Undefined.Select(u => u.Key).Distinct(StringComparer.Ordinal).Count());
        int lowContrast = result.Findings.Sum(f => f.Contrast.Count(c => c.Status == ContrastStatus.Fail));
        string summary = $"{result.Themes.Count} themes, {result.Consumers.Count} consumers, {undefined} undefined-key findings, {lowContrast} low-contrast findings";

        if (check)
        {
            switch (OutputDrift.Check(output, markdown))
            {
                case DriftState.Missing:
                    Console.Error.WriteLine($"theme-audit: {output} does not exist; run `theme-audit report` to create it.");
                    return 1;
                case DriftState.Stale:
                    Console.Error.WriteLine($"theme-audit: {output} is stale — it differs from a fresh run ({summary}); regenerate it with `theme-audit report`.");
                    return 1;
                default:
                    Console.WriteLine($"theme-audit: {output} is up to date ({summary}).");
                    return 0;
            }
        }

        OutputDrift.Write(output, markdown);
        Console.WriteLine($"theme-audit: wrote {output} ({summary}).");
        return 0;
    }
    catch (AuditConfigException ex)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 2;
    }
    catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 1;
    }
});

root.Subcommands.Add(report);

// ---- parts: the template parts report ---------------------------------------------------------

Command parts = new("parts", "Read the configured controls' template parts, as BNXQ1003 reads them, and write the parts report.");
parts.Options.Add(configOption);
parts.Options.Add(checkOption);
parts.SetAction(parseResult =>
{
    FileInfo configFile = parseResult.GetValue(configOption)!;
    bool check = parseResult.GetValue(checkOption);

    try
    {
        AuditConfig config = AuditConfig.Load(configFile.FullName);
        if (config.Parts is null)
        {
            Console.Error.WriteLine("theme-audit: the configuration declares no parts report.");
            return 2;
        }

        TemplatePartReading reading = PartsRunner.Read(config);
        string markdown = reading.ToMarkdown();
        string output = config.Resolve(config.Parts.Report);
        string relative = config.Relative(output);
        string summary = $"{reading.Controls.Count} control(s), {reading.Controls.Sum(c => c.Lookups.Count)} lookup(s), {reading.Skipped.Count} not read";

        if (check)
        {
            switch (OutputDrift.Check(output, markdown))
            {
                case DriftState.Missing:
                    Console.Error.WriteLine($"theme-audit: {relative} does not exist; run `theme-audit parts` to create it.");
                    return 1;
                case DriftState.Stale:
                    Console.Error.WriteLine($"theme-audit: {relative} is stale — it differs from a fresh run ({summary}); regenerate it with `theme-audit parts`.");
                    return 1;
                default:
                    Console.WriteLine($"theme-audit: {relative} is up to date ({summary}).");
                    return 0;
            }
        }

        OutputDrift.Write(output, markdown);
        Console.WriteLine($"theme-audit: wrote {relative} ({summary}).");
        return 0;
    }
    catch (AuditConfigException ex)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 2;
    }
    catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or BadImageFormatException)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 1;
    }
});

root.Subcommands.Add(parts);

// ---- brand: the brand documents -----------------------------------------------------------------

Command brand = new("brand", "Fill each configured brand document from its template and its theme's palette, and write it.");
brand.Options.Add(configOption);
brand.Options.Add(checkOption);
brand.SetAction(parseResult =>
{
    FileInfo configFile = parseResult.GetValue(configOption)!;
    bool check = parseResult.GetValue(checkOption);

    try
    {
        AuditConfig config = AuditConfig.Load(configFile.FullName);
        if (config.Brand.Count == 0)
        {
            Console.Error.WriteLine("theme-audit: the configuration declares no brand documents.");
            return 2;
        }

        // Every document is filled before any is written, so one whose claim fails leaves the others as they were.
        List<(string Output, BrandResult Result)> documents = [.. config.Brand.Select(entry => (config.Resolve(entry.Output), BrandRunner.Fill(config, entry)))];
        if (documents.Any(document => document.Result.Problems.Count > 0))
        {
            foreach ((string output, BrandResult result) in documents.Where(document => document.Result.Problems.Count > 0))
            {
                Console.Error.WriteLine($"theme-audit: {config.Relative(output)} cannot be filled — {result.Problems.Count} problem(s):");
                foreach (string problem in result.Problems)
                {
                    Console.Error.WriteLine($"  {problem}");
                }
            }

            Console.Error.WriteLine("theme-audit: nothing was written.");
            return 1;
        }

        int stale = 0;
        foreach ((string output, BrandResult result) in documents)
        {
            string relative = config.Relative(output);
            string summary = $"{result.Markdown.Count(c => c == '\n')} lines";
            if (check)
            {
                switch (OutputDrift.Check(output, result.Markdown))
                {
                    case DriftState.Missing:
                        Console.Error.WriteLine($"theme-audit: {relative} does not exist; run `theme-audit brand` to create it.");
                        stale++;
                        break;
                    case DriftState.Stale:
                        Console.Error.WriteLine($"theme-audit: {relative} is stale — it differs from a fresh run ({summary}); regenerate it with `theme-audit brand`.");
                        stale++;
                        break;
                    default:
                        Console.WriteLine($"theme-audit: {relative} is up to date ({summary}).");
                        break;
                }

                continue;
            }

            OutputDrift.Write(output, result.Markdown);
            Console.WriteLine($"theme-audit: wrote {relative} ({summary}).");
        }

        return stale == 0 ? 0 : 1;
    }
    catch (AuditConfigException ex)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 2;
    }
    catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"theme-audit: {ex.Message}");
        return 1;
    }
});

root.Subcommands.Add(brand);

return root.Parse(args).Invoke();

static string Summarize(CompatGeneration generation)
{
    int Count(CompatHow how) => generation.Entries.Count(e => e.How == how);
    return $"{Count(CompatHow.Mapped)} mapped, {Count(CompatHow.Copied)} copied, {Count(CompatHow.Literal)} literal (review), {Count(CompatHow.Restored)} restored, {generation.Skipped.Count} skipped";
}
