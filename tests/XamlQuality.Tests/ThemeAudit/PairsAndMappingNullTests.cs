using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// The files a configuration names hold to the rule <c>theme-audit.json</c> holds to: a <c>null</c> where the audit
/// needs a value is a configuration error that names the file and the field. Before, a contrast pairs file or a compat
/// mapping file with such a null either escaped <c>theme-audit</c> as an exception it did not expect, and the tool
/// exited 1, which a consumer's CI reads as drift, or was read silently, as a variant mapped to no source variant was.
/// </summary>
public sealed class PairsAndMappingNullTests : IDisposable
{
    private const string Variants = """ "variants": { "Light": "Light", "Dark": "Dark" } """;

    private readonly string _root = Directory.CreateTempSubdirectory("theme-nulls-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void A_pairs_file_with_null_pairs_a_null_pair_or_a_pair_with_no_colour_is_a_configuration_error()
    {
        string[] expected =
        [
            "'pairs' must be a list of pairs",
            "a 'pairs' entry is empty",
            "a pair needs a 'foreground' and a 'background'",
            "a pair needs a 'foreground' and a 'background'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                path => ContrastPairsFile.Load(path),
                """{ "pairs": null }""",
                """{ "pairs": [null] }""",
                """{ "pairs": [ { "foreground": null, "background": "Page" } ] }""",
                """{ "pairs": [ { "foreground": "Ink", "background": null } ] }"""));
    }

    [Fact]
    public void A_mapping_file_with_null_variants_or_a_variant_mapped_to_null_is_a_configuration_error()
    {
        string[] expected =
        [
            "'variants' must map at least one target variant to a source variant",
            "'variants' maps 'Dark' to no source variant",
        ];

        Assert.Equal(
            expected,
            Thrown(
                path => CompatMapping.Load(path),
                """{ "variants": null }""",
                """{ "variants": { "Light": "Light", "Dark": null } }"""));
    }

    [Fact]
    public void A_mapping_file_with_null_keys_or_a_key_mapped_to_null_is_a_configuration_error()
    {
        string[] expected =
        [
            "'keys' must map source keys to targets",
            "'keys' maps 'Ink' to no target",
        ];

        Assert.Equal(
            expected,
            Thrown(
                path => CompatMapping.Load(path),
                "{ " + Variants + """, "keys": null }""",
                "{ " + Variants + """, "keys": { "Accent": "ToBlue", "Ink": null } }"""));
    }

    [Fact]
    public void A_mapping_file_with_null_skip_or_a_null_key_to_skip_is_a_configuration_error()
    {
        string[] expected =
        [
            "'skip' must be a list of keys",
            "'skip' names an empty key",
        ];

        Assert.Equal(
            expected,
            Thrown(
                path => CompatMapping.Load(path),
                "{ " + Variants + """, "skip": null }""",
                "{ " + Variants + """, "skip": ["Ink", null] }"""));
    }

    [Fact]
    public void A_mapping_file_with_a_null_copy_policy_list_or_entry_is_a_configuration_error()
    {
        string[] expected =
        [
            "'copy' may be left out, but not set to null",
            "'copy' may leave out 'namespaces' or 'skipElements', but not set them to null",
            "'copy' may leave out 'namespaces' or 'skipElements', but not set them to null",
            "'copy' names an empty entry in 'namespaces' or 'skipElements'",
            "'copy' names an empty entry in 'namespaces' or 'skipElements'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                path => CompatMapping.Load(path),
                "{ " + Variants + """, "copy": null }""",
                "{ " + Variants + """, "copy": { "namespaces": null } }""",
                "{ " + Variants + """, "copy": { "skipElements": null } }""",
                "{ " + Variants + """, "copy": { "namespaces": [null] } }""",
                "{ " + Variants + """, "copy": { "skipElements": ["Style", null] } }"""));
    }

    /// <summary>
    /// A pair's <c>over</c> and <c>purpose</c>, and a mapping's <c>description</c> and a target's <c>why</c>, may be
    /// <c>null</c>; and a mapping that leaves out <c>keys</c>, <c>skip</c> and <c>copy</c> takes their defaults.
    /// </summary>
    [Fact]
    public void A_null_where_a_value_may_be_left_out_reads_as_left_out()
    {
        IReadOnlyList<ContrastPair> pairs = ContrastPairsFile.Load(Write(
            """{ "pairs": [ { "foreground": "Ink", "background": "Page", "over": null, "purpose": null } ] }"""));
        CompatMapping mapping = CompatMapping.Load(Write(
            """{ "description": null, "variants": { "Dark": "Dark" }, "keys": { "Ink": { "to": "ToInk", "why": null } } }"""));
        CompatMapping bare = CompatMapping.Load(Write("""{ "variants": { "Dark": "Dark" } }"""));

        Assert.Equal(new ContrastPair("Ink", "Page", Contrast.AaText), Assert.Single(pairs));
        Assert.Equal(new MappingTarget("ToInk", null), mapping.Keys["Ink"]);
        Assert.Null(mapping.Description);
        Assert.Equal((0, 0), (bare.Keys.Count, bare.Skip.Count));
        Assert.NotEmpty(bare.Copy.Namespaces);
        Assert.NotEmpty(bare.Copy.SkipElements);
    }

    /// <summary>
    /// Loads each file with <paramref name="load"/> and returns the configuration error each throws, with the file's
    /// path, which every message starts with, taken off.
    /// </summary>
    private string[] Thrown(Func<string, object> load, params string[] files) =>
    [
        .. files.Select(content =>
        {
            string path = Write(content);
            string message = Assert.Throws<AuditConfigException>(() => load(path)).Message;
            Assert.StartsWith(path + ": ", message, StringComparison.Ordinal);
            return message[(path.Length + 2)..];
        }),
    ];

    private string Write(string content)
    {
        string path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, content);
        return path;
    }
}
