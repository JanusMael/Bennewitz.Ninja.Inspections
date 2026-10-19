using Bennewitz.Ninja.XamlQuality.ThemeAudit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// A <c>null</c> in place of a value the audit needs is a configuration error that names the field, as a malformed file
/// is. Before, each of these escaped <c>theme-audit</c> as an exception it did not expect, and the tool exited 1, which a
/// consumer's CI reads as drift, where a configuration error exits 2.
/// </summary>
public sealed class AuditConfigNullTests
{
    [Fact]
    public void A_null_title_or_report_is_a_configuration_error()
    {
        string[] expected =
        [
            "'title' and 'report' may be left out, but not set to null",
            "'title' and 'report' may be left out, but not set to null",
        ];

        Assert.Equal(expected, Thrown("""{ "title": null }""", """{ "report": null }"""));
    }

    [Fact]
    public void Null_themes_a_null_theme_or_a_theme_with_no_name_is_a_configuration_error()
    {
        string[] expected =
        [
            "'themes' must be a list of themes",
            "a 'themes' entry is empty",
            "a 'themes' entry needs a 'name'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                """{ "themes": null }""",
                """{ "themes": [null] }""",
                """{ "themes": [ { "name": null, "entry": "t.axaml", "baseDirectory": "." } ] }"""));
    }

    [Fact]
    public void A_theme_naming_a_null_variant_path_key_or_provider_is_a_configuration_error()
    {
        string[] expected =
        [
            "theme 'T' names an empty variant in 'variants' or 'inheritance'",
            "theme 'T' names an empty variant in 'variants' or 'inheritance'",
            "theme 'T' names an empty path in 'merge' or 'links'",
            "theme 'T' names an empty path in 'merge' or 'links'",
            "theme 'T' names an empty key in 'surfaces' or 'variantSurfaces'",
            "theme 'T' names an empty key in 'surfaces' or 'variantSurfaces'",
            "theme 'T' names an empty key in 'surfaces' or 'variantSurfaces'",
            "theme 'T' names an empty entry in 'providers'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                Theme(""" "variants": [null] """),
                Theme(""" "inheritance": { "Vivid": null } """),
                Theme(""" "merge": [null] """),
                Theme(""" "links": { "Lines.axaml": null } """),
                Theme(""" "surfaces": { "Page": null } """),
                Theme(""" "variantSurfaces": { "Dark": null } """),
                Theme(""" "variantSurfaces": { "Dark": { "Page": null } } """),
                Theme(""" "providers": { "Swatch": null } """)));
    }

    /// <summary>A theme takes what it leaves empty from the one it extends, so a null it inherits is found too.</summary>
    [Fact]
    public void A_null_a_theme_inherits_through_extends_is_a_configuration_error()
    {
        AuditConfigException error = Assert.Throws<AuditConfigException>(() => AuditConfig.Parse(
            """
            {
              "themes": [
                { "name": "Base", "entry": "t.axaml", "baseDirectory": ".", "links": { "Lines.axaml": null } },
                { "name": "T", "extends": "Base" }
              ]
            }
            """,
            Path.GetTempPath()));

        Assert.Equal("theme 'Base' names an empty path in 'merge' or 'links'", error.Message);
    }

    [Fact]
    public void Null_consumers_a_null_consumer_or_a_consumer_with_no_name_is_a_configuration_error()
    {
        string[] expected =
        [
            "'consumers' must be a list of consumers",
            "a 'consumers' entry is empty",
            "a 'consumers' entry needs a 'name'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                """{ "consumers": null }""",
                """{ "consumers": [null] }""",
                """{ "consumers": [ { "name": null, "paths": ["app"] } ] }"""));
    }

    [Fact]
    public void A_consumer_with_null_paths_a_null_path_or_a_token_dictionary_with_no_entry_is_a_configuration_error()
    {
        string[] expected =
        [
            "consumer 'App' names no 'paths'",
            "consumer 'App' names an empty path in 'paths' or 'exclude'",
            "consumer 'App' names an empty path in 'paths' or 'exclude'",
            "consumer 'App' needs a 'tokens' with an 'entry'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                """{ "consumers": [ { "name": "App", "paths": null } ] }""",
                """{ "consumers": [ { "name": "App", "paths": [null] } ] }""",
                """{ "consumers": [ { "name": "App", "paths": ["app"], "exclude": [null] } ] }""",
                """{ "consumers": [ { "name": "App", "paths": ["app"], "tokens": { "entry": null } } ] }"""));
    }

    [Fact]
    public void Null_compat_a_null_entry_or_one_with_a_null_name_mapping_output_or_variant_key_is_a_configuration_error()
    {
        string[] expected =
        [
            "'compat' must be a list of compat dictionaries",
            "a 'compat' entry is empty",
            "a 'compat' entry needs a 'name'",
            "compat 'C' needs a 'mapping' and an 'output'",
            "compat 'C' needs a 'mapping' and an 'output'",
            "compat 'C' needs 'variantKeys' with a 'prefix', a 'namespace' and a 'type'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                """{ "compat": null }""",
                """{ "compat": [null] }""",
                Compat(""" "name": null, "mapping": "m.json", "output": "o.axaml" """),
                Compat(""" "name": "C", "mapping": null, "output": "o.axaml" """),
                Compat(""" "name": "C", "mapping": "m.json", "output": null """),
                Compat(""" "name": "C", "mapping": "m.json", "output": "o.axaml", "variantKeys": { "prefix": null, "namespace": "using:V", "type": "Variants" } """)));
    }

    [Fact]
    public void Null_parts_markup_or_assemblies_or_a_null_path_in_them_is_a_configuration_error()
    {
        string[] expected =
        [
            "'parts' needs 'report', 'markup' and 'assemblies'",
            "'parts' needs 'report', 'markup' and 'assemblies'",
            "'parts' names an empty path in 'markup' or 'assemblies'",
            "'parts' names an empty path in 'markup' or 'assemblies'",
        ];

        Assert.Equal(
            expected,
            Thrown(
                """{ "parts": { "report": "r.md", "markup": null, "assemblies": ["a.dll"] } }""",
                """{ "parts": { "report": "r.md", "markup": ["m"], "assemblies": null } }""",
                """{ "parts": { "report": "r.md", "markup": [null], "assemblies": ["a.dll"] } }""",
                """{ "parts": { "report": "r.md", "markup": ["m"], "assemblies": [null] } }"""));
    }

    /// <summary>
    /// Where a field may be left out with nothing in its place, a null leaves it out, and a provider may still supply a
    /// key with no value.
    /// </summary>
    [Fact]
    public void A_null_where_a_value_may_be_left_out_reads_as_left_out()
    {
        AuditConfig config = AuditConfig.Parse(
            """
            {
              "parts": null,
              "themes": [
                {
                  "name": "T", "extends": null, "entry": "t.axaml", "baseDirectory": ".", "assembly": null,
                  "variants": null, "inheritance": null, "links": null, "providers": { "Swatch": { "SwatchBrush": null } },
                  "surfaces": null, "variantSurfaces": null, "merge": null
                },
                { "name": "U", "entry": "t.axaml", "baseDirectory": "." }
              ],
              "consumers": [ { "name": "App", "paths": ["app"], "exclude": null, "tokens": null, "contrast": null, "themes": null } ],
              "compat": [ { "name": "C", "from": "T", "to": "U", "mapping": "m.json", "output": "o.axaml", "variantKeys": null } ]
            }
            """,
            Path.GetTempPath());

        Assert.Null(config.Parts);
        Assert.Null(config.Themes[0].Providers!["Swatch"]["SwatchBrush"]);
        Assert.Equal(["App"], config.Consumers.Select(consumer => consumer.Name));
        Assert.Equal(["C"], config.Compat.Select(compat => compat.Name));
    }

    private static string Theme(string fields) =>
        """{ "themes": [ { "name": "T", "entry": "t.axaml", "baseDirectory": ".", """ + fields + " } ] }";

    private static string Compat(string fields) =>
        """{ "themes": [ { "name": "T", "entry": "t.axaml", "baseDirectory": "." }, { "name": "U", "entry": "t.axaml", "baseDirectory": "." } ], "compat": [ { "from": "T", "to": "U", """
        + fields + " } ] }";

    private static string[] Thrown(params string[] configurations) =>
    [
        .. configurations.Select(json => Assert.Throws<AuditConfigException>(() => AuditConfig.Parse(json, Path.GetTempPath())).Message),
    ];
}
