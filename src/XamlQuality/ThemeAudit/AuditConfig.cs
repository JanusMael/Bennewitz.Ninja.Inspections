using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>A problem in the audit configuration itself, as opposed to a finding.</summary>
public sealed class AuditConfigException(string message) : Exception(message);

/// <summary>
/// The audit's inputs, read from a JSON file: the themes to inventory, the consumers to scan,
/// and the compat dictionaries to generate. Every path in the file is relative to the file's
/// directory, so the same configuration runs from any working directory and on any machine
/// that has the checkouts.
/// </summary>
public sealed class AuditConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>The report's title.</summary>
    public string Title { get; set; } = "Theme audit";

    /// <summary>Where the report is written, relative to the configuration file.</summary>
    public string Report { get; set; } = "theme-audit.md";

    /// <summary>The themes, in report order.</summary>
    public List<ThemeConfig> Themes { get; set; } = [];

    /// <summary>The consumers, in report order.</summary>
    public List<ConsumerConfig> Consumers { get; set; } = [];

    /// <summary>The compat dictionaries to generate.</summary>
    public List<CompatConfig> Compat { get; set; } = [];

    /// <summary>The template parts report <c>theme-audit parts</c> writes; none when omitted.</summary>
    public PartsConfig? Parts { get; set; }

    /// <summary>The brand documents <c>theme-audit brand</c> fills, in configuration order.</summary>
    public List<BrandConfig> Brand { get; set; } = [];

    /// <summary>The configuration file, absolute.</summary>
    [JsonIgnore]
    public string ConfigPath { get; private set; } = string.Empty;

    /// <summary>The directory every relative path in the file is resolved against.</summary>
    [JsonIgnore]
    public string ConfigDirectory { get; private set; } = string.Empty;

    /// <summary>Reads and validates a configuration file.</summary>
    /// <exception cref="AuditConfigException">The file is missing, malformed, or inconsistent.</exception>
    public static AuditConfig Load(string path)
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            throw new AuditConfigException($"configuration file not found: {full}");
        }

        AuditConfig config;
        try
        {
            config = JsonSerializer.Deserialize<AuditConfig>(File.ReadAllText(full), JsonOptions)
                     ?? throw new AuditConfigException($"{full}: the configuration is empty");
        }
        catch (JsonException ex)
        {
            throw new AuditConfigException($"{full}: {ex.Message}");
        }

        config.ConfigPath = full;
        config.ConfigDirectory = Path.GetDirectoryName(full) ?? Directory.GetCurrentDirectory();
        config.Validate();
        return config;
    }

    /// <summary>Parses configuration text; <paramref name="directory"/> is what relative paths resolve against.</summary>
    /// <exception cref="AuditConfigException">The text is malformed or inconsistent.</exception>
    public static AuditConfig Parse(string json, string directory)
    {
        AuditConfig config;
        try
        {
            config = JsonSerializer.Deserialize<AuditConfig>(json, JsonOptions)
                     ?? throw new AuditConfigException("the configuration is empty");
        }
        catch (JsonException ex)
        {
            throw new AuditConfigException(ex.Message);
        }

        config.ConfigPath = Path.Combine(Path.GetFullPath(directory), "theme-audit.json");
        config.ConfigDirectory = Path.GetFullPath(directory);
        config.Validate();
        return config;
    }

    /// <summary>A path from the file, made absolute.</summary>
    public string Resolve(string relative)
    {
        return Path.GetFullPath(Path.Combine(ConfigDirectory, relative));
    }

    /// <summary>An absolute path, made relative to the configuration directory with forward slashes — the report's form.</summary>
    public string Relative(string full)
    {
        return Path.GetRelativePath(ConfigDirectory, full).Replace('\\', '/');
    }

    /// <summary>The theme named <paramref name="name"/>.</summary>
    /// <exception cref="AuditConfigException">No theme has that name.</exception>
    public ThemeConfig Theme(string name)
    {
        return Themes.Find(t => string.Equals(t.Name, name, StringComparison.Ordinal))
               ?? throw new AuditConfigException($"no theme named '{name}'");
    }

    private void Validate()
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (ThemeConfig theme in Themes)
        {
            if (!names.Add(theme.Name))
            {
                throw new AuditConfigException($"theme '{theme.Name}' is declared twice");
            }
        }

        // Resolve `extends` into the child's empty fields, in declaration order so a chain works
        // as long as the base comes first.
        foreach (ThemeConfig theme in Themes)
        {
            if (theme.Extends is null)
            {
                continue;
            }

            ThemeConfig parent = Theme(theme.Extends);
            if (ReferenceEquals(parent, theme))
            {
                throw new AuditConfigException($"theme '{theme.Name}' extends itself");
            }

            theme.Entry ??= parent.Entry;
            theme.BaseDirectory ??= parent.BaseDirectory;
            theme.Assembly ??= parent.Assembly;
            theme.Variants ??= parent.Variants;
            theme.Inheritance ??= parent.Inheritance;
            theme.Links ??= parent.Links;
            theme.Providers ??= parent.Providers;
            theme.Surfaces ??= parent.Surfaces;
            theme.VariantSurfaces ??= parent.VariantSurfaces;
            theme.Merge = [.. parent.Merge ?? [], .. theme.Merge ?? []];
        }

        foreach (ThemeConfig theme in Themes)
        {
            if (string.IsNullOrWhiteSpace(theme.Entry) || string.IsNullOrWhiteSpace(theme.BaseDirectory))
            {
                throw new AuditConfigException($"theme '{theme.Name}' needs 'entry' and 'baseDirectory'");
            }
        }

        HashSet<string> consumerNames = new(StringComparer.Ordinal);
        foreach (ConsumerConfig consumer in Consumers)
        {
            if (!consumerNames.Add(consumer.Name))
            {
                throw new AuditConfigException($"consumer '{consumer.Name}' is declared twice");
            }

            if (consumer.Paths.Count == 0)
            {
                throw new AuditConfigException($"consumer '{consumer.Name}' names no 'paths'");
            }

            foreach (string themeName in consumer.Themes ?? [])
            {
                if (!names.Contains(themeName))
                {
                    throw new AuditConfigException($"consumer '{consumer.Name}' is audited against unknown theme '{themeName}'");
                }
            }
        }

        foreach (CompatConfig compat in Compat)
        {
            if (!names.Contains(compat.From) || !names.Contains(compat.To))
            {
                throw new AuditConfigException($"compat '{compat.Name}' names an unknown theme ('{compat.From}' → '{compat.To}')");
            }
        }

        if (Parts is { } parts
            && (string.IsNullOrWhiteSpace(parts.Report) || parts.Markup.Count == 0 || parts.Assemblies.Count == 0))
        {
            throw new AuditConfigException("'parts' needs 'report', 'markup' and 'assemblies'");
        }

        foreach (BrandConfig? brand in Brand ?? throw new AuditConfigException("'brand' must be a list of documents"))
        {
            if (brand is null)
            {
                throw new AuditConfigException("a 'brand' entry is empty");
            }

            if (string.IsNullOrWhiteSpace(brand.Template) || string.IsNullOrWhiteSpace(brand.Output)
                || string.IsNullOrWhiteSpace(brand.Variant))
            {
                throw new AuditConfigException($"brand '{brand.Output}' needs a 'template', an 'output' and, when it names one, a 'variant'");
            }

            if (!names.Contains(brand.Theme))
            {
                throw new AuditConfigException($"brand '{brand.Output}' names an unknown theme '{brand.Theme}'");
            }

            if ((brand.Tokens ?? []).Concat(brand.Near ?? []).Any(string.IsNullOrWhiteSpace))
            {
                throw new AuditConfigException($"brand '{brand.Output}' names an empty path in 'tokens' or 'near'");
            }

            if (brand.Frame is { } frame
                && (string.IsNullOrWhiteSpace(frame.Ground) || string.IsNullOrWhiteSpace(frame.Stroke) || string.IsNullOrWhiteSpace(frame.Text)))
            {
                throw new AuditConfigException($"brand '{brand.Output}' needs a 'frame' with a 'ground', a 'stroke' and a 'text'");
            }
        }
    }
}

/// <summary>
/// One brand document <c>theme-audit brand</c> fills: a Markdown template whose placeholders take their colours and
/// figures from a theme's palette, read in one of its variants. Paths are relative to the configuration file.
/// </summary>
public sealed class BrandConfig
{
    /// <summary>The template the document is filled from.</summary>
    public required string Template { get; set; }

    /// <summary>Where the filled document is written.</summary>
    public required string Output { get; set; }

    /// <summary>The theme, from the configuration's <c>themes</c>, whose palette the document describes.</summary>
    public required string Theme { get; set; }

    /// <summary>
    /// The variant the palette is read in, by display name, as Avalonia serves a requested variant: the variant, else
    /// its nearest ancestor, else <c>Default</c>. <c>Default</c> when omitted.
    /// </summary>
    public string Variant { get; set; } = ThemeInventory.DefaultVariant;

    /// <summary>
    /// The files whose keyed brushes and colours are the palette's tokens, each a file the theme reads; every file the
    /// theme reads when omitted.
    /// </summary>
    public List<string>? Tokens { get; set; }

    /// <summary>The folders a <c>{{near}}</c> path is looked for in, in order; the configuration's folder when omitted.</summary>
    public List<string>? Near { get; set; }

    /// <summary>The colours <c>{{swatches}}</c> draws its chart in; a template that draws no chart needs none.</summary>
    public BrandFrame? Frame { get; set; }
}

/// <summary>
/// The colours a brand document's chart is drawn in, resolved in the document's variant, so a chart of a light variant
/// is framed in light colours. Each is a colour as the template's placeholders write one: a token's key, or a literal
/// <c>#RRGGBB</c> or <c>#AARRGGBB</c>.
/// </summary>
public sealed class BrandFrame
{
    /// <summary>The ground every swatch is drawn over, which fills the chart; it must draw opaque.</summary>
    public required string Ground { get; set; }

    /// <summary>The outline of the chart and of every swatch, drawn over the ground.</summary>
    public required string Stroke { get; set; }

    /// <summary>The colour of the chart's titles, drawn over the ground.</summary>
    public required string Text { get; set; }
}

/// <summary>
/// The template parts report: <c>BNXQ1003</c>'s reading of the controls the assemblies define and the themes the markup
/// holds. Paths are relative to the configuration file.
/// </summary>
public sealed class PartsConfig
{
    /// <summary>Where the report is written.</summary>
    public string Report { get; set; } = string.Empty;

    /// <summary>The folders whose markup the themes are read from.</summary>
    public List<string> Markup { get; set; } = [];

    /// <summary>
    /// The assemblies whose code the parts are read from: the controls' own, and any library that defines a base class
    /// of theirs whose lookups the report should show.
    /// </summary>
    public List<string> Assemblies { get; set; } = [];
}

/// <summary>One theme to inventory. Paths are relative to the configuration file.</summary>
public sealed class ThemeConfig
{
    /// <summary>The name the report and the consumers use.</summary>
    public required string Name { get; set; }

    /// <summary>Another theme whose fields fill in whatever this one leaves empty; its <c>merge</c> list is prepended.</summary>
    public string? Extends { get; set; }

    /// <summary>The theme's entry file.</summary>
    public string? Entry { get; set; }

    /// <summary>The assembly's <c>avares</c> base directory.</summary>
    public string? BaseDirectory { get; set; }

    /// <summary>The theme's assembly name, so its own <c>avares://</c> includes resolve.</summary>
    public string? Assembly { get; set; }

    /// <summary>The variants to audit, by display name; every declared variant when omitted.</summary>
    public List<string>? Variants { get; set; }

    /// <summary>Variant → the variant it inherits from.</summary>
    public Dictionary<string, string>? Inheritance { get; set; }

    /// <summary>Resource path → the file that supplies it (an MSBuild-linked file).</summary>
    public Dictionary<string, string>? Links { get; set; }

    /// <summary>Code-behind element name → the keys it supplies (colour literal, alias key, or null).</summary>
    public Dictionary<string, Dictionary<string, string?>>? Providers { get; set; }

    /// <summary>Surface placeholder (<c>Page</c>, <c>Text</c>) → the theme key that paints it.</summary>
    public Dictionary<string, string>? Surfaces { get; set; }

    /// <summary>Variant → per-variant surface overrides.</summary>
    public Dictionary<string, Dictionary<string, string>>? VariantSurfaces { get; set; }

    /// <summary>Dictionaries layered after the theme, as an application including them would see them.</summary>
    public List<string>? Merge { get; set; }
}

/// <summary>One consumer to scan for the keys it references.</summary>
public sealed class ConsumerConfig
{
    /// <summary>The name the report uses.</summary>
    public required string Name { get; set; }

    /// <summary>The directories to scan; each entry is a path or a list of candidate paths, the first that exists winning.</summary>
    public List<PathCandidates> Paths { get; set; } = [];

    /// <summary>Directories under <see cref="Paths"/> left out of the scan — a generated subtree the consumer does not author.</summary>
    public List<string>? Exclude { get; set; }

    /// <summary>The consumer's own token dictionary, for contrast scoring.</summary>
    public TokensConfig? Tokens { get; set; }

    /// <summary>The contrast pairs file.</summary>
    public string? Contrast { get; set; }

    /// <summary>The themes to audit this consumer against; every theme when omitted.</summary>
    public List<string>? Themes { get; set; }
}

/// <summary>A consumer's token dictionary.</summary>
public sealed class TokensConfig
{
    /// <summary>The dictionary's entry file.</summary>
    public required string Entry { get; set; }

    /// <summary>Its <c>avares</c> base directory; the entry's directory when omitted.</summary>
    public string? BaseDirectory { get; set; }

    /// <summary>Its assembly name.</summary>
    public string? Assembly { get; set; }
}

/// <summary>One compat dictionary to generate: the keys <see cref="From"/> defines and <see cref="To"/> lacks.</summary>
public sealed class CompatConfig
{
    /// <summary>The name the report uses.</summary>
    public required string Name { get; set; }

    /// <summary>The theme whose keys are carried over.</summary>
    public required string From { get; set; }

    /// <summary>The theme that lacks them.</summary>
    public required string To { get; set; }

    /// <summary>The reviewed mapping file.</summary>
    public required string Mapping { get; set; }

    /// <summary>Where the generated dictionary is written.</summary>
    public required string Output { get; set; }

    /// <summary>
    /// How a custom (non built-in) target variant is keyed in the output. Avalonia's
    /// <c>ThemeVariant</c> converter accepts only <c>Default</c>, <c>Light</c> and <c>Dark</c> as
    /// strings; any other variant needs <c>{x:Static Type.Member}</c>, and the type must be
    /// visible to the project that compiles the dictionary. Omitted, the target theme's own key
    /// is written (<c>{x:Static semi:SemiTheme.Aquatic}</c>), which compiles only where that theme
    /// is referenced; set, the member is looked up on the named type by the variant's name —
    /// <c>ThemeVariant</c> equality is by key string, so a stand-in type with the same names
    /// matches the theme's variants at runtime without referencing it.
    /// </summary>
    public VariantKeyStyle? VariantKeys { get; set; }
}

/// <summary>The static type whose members name a theme's custom variants, for <c>{x:Static prefix:Type.Member}</c> keys.</summary>
public sealed class VariantKeyStyle
{
    /// <summary>The XML prefix to declare.</summary>
    public required string Prefix { get; set; }

    /// <summary>The XML namespace (<c>using:Some.Namespace</c>).</summary>
    public required string Namespace { get; set; }

    /// <summary>The type whose static members are the variants.</summary>
    public required string Type { get; set; }
}

/// <summary>A path or a list of candidate paths; the first that exists is used.</summary>
[JsonConverter(typeof(PathCandidatesConverter))]
public sealed class PathCandidates
{
    /// <param name="candidates">The candidates, in preference order.</param>
    public PathCandidates(IReadOnlyList<string> candidates)
    {
        if (candidates.Count == 0)
        {
            throw new AuditConfigException("a path entry is empty");
        }

        Candidates = candidates;
    }

    /// <summary>The candidates, in preference order.</summary>
    public IReadOnlyList<string> Candidates { get; }

    /// <summary>The candidates as the report shows them.</summary>
    public string Display => string.Join(" or ", Candidates);

    /// <summary>The first candidate that exists as a directory under <paramref name="config"/>, absolute.</summary>
    /// <exception cref="DirectoryNotFoundException">None exists.</exception>
    public string ResolveDirectory(AuditConfig config)
    {
        foreach (string candidate in Candidates)
        {
            string full = config.Resolve(candidate);
            if (Directory.Exists(full))
            {
                return full;
            }
        }

        throw new DirectoryNotFoundException(
            $"none of the configured directories exists: {Display} (relative to {config.ConfigDirectory}); fetch the reference sources first");
    }
}

/// <summary>Reads a JSON string or array of strings as <see cref="PathCandidates"/>.</summary>
public sealed class PathCandidatesConverter : JsonConverter<PathCandidates>
{
    /// <inheritdoc/>
    public override PathCandidates Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new PathCandidates([reader.GetString()!]);
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<string> candidates = [];
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException("a path list may contain only strings");
                }

                candidates.Add(reader.GetString()!);
            }

            return new PathCandidates(candidates);
        }

        throw new JsonException("a path is a string or a list of strings");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, PathCandidates value, JsonSerializerOptions options)
    {
        if (value.Candidates.Count == 1)
        {
            writer.WriteStringValue(value.Candidates[0]);
            return;
        }

        writer.WriteStartArray();
        foreach (string candidate in value.Candidates)
        {
            writer.WriteStringValue(candidate);
        }

        writer.WriteEndArray();
    }
}

/// <summary>The contrast pairs file a consumer points at.</summary>
public sealed class ContrastPairsFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>The pairs, in report order.</summary>
    public List<ContrastPairConfig> Pairs { get; set; } = [];

    /// <summary>Reads a pairs file.</summary>
    /// <exception cref="AuditConfigException">The file is missing or malformed.</exception>
    public static IReadOnlyList<ContrastPair> Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new AuditConfigException($"contrast pairs file not found: {path}");
        }

        try
        {
            ContrastPairsFile file = JsonSerializer.Deserialize<ContrastPairsFile>(File.ReadAllText(path), JsonOptions)
                                     ?? throw new AuditConfigException($"{path}: the pairs file is empty");
            return file.Pairs.Select(p => p.ToPair()).ToList();
        }
        catch (JsonException ex)
        {
            throw new AuditConfigException($"{path}: {ex.Message}");
        }
    }
}

/// <summary>One pair as written in the pairs file.</summary>
public sealed class ContrastPairConfig
{
    /// <summary>The foreground key or surface placeholder.</summary>
    public required string Foreground { get; set; }

    /// <summary>The background key or surface placeholder.</summary>
    public required string Background { get; set; }

    /// <summary>The WCAG floor: 4.5 for text, 3.0 for large text and graphics.</summary>
    public double Floor { get; set; } = Contrast.AaText;

    /// <summary>The key a translucent background is composited over first.</summary>
    public string? Over { get; set; }

    /// <summary>What the pair is.</summary>
    public string? Purpose { get; set; }

    /// <summary>The pair the audit scores.</summary>
    public ContrastPair ToPair()
    {
        return new ContrastPair(Foreground, Background, Floor) { Over = Over, Purpose = Purpose };
    }
}
