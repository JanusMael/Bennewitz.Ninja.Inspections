using System.Reflection;
using System.Runtime.Loader;
using Bennewitz.Ninja.XamlQuality.Rules;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// Reads the configured template parts: <c>BNXQ1003</c>'s reading of the assemblies the configuration names, against the
/// themes in each of its markup folders.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The reading the rule checks, from a configuration.</b> What <c>theme-audit parts</c> renders is
/// <see cref="TemplatePartRule.ReadParts"/>'s reading, so the committed report and the rule's findings cannot disagree.
/// A test that holds the report to a fresh run in its own process can call this, or the rule's reading directly with the
/// assemblies it already references.
/// </para>
/// <para>
/// ⚠ <b>The assemblies load apart from the tool's own</b>, each dependency from the folder of an assembly the
/// configuration names, so a consumer's build output reads with the framework and libraries it was built against, and
/// a library the tool also uses does not collide with the tool's copy.
/// </para>
/// <para>
/// ⚠ <b>A theme's file is relative to the configuration</b>, as every path in the audit's reports is, so themes read
/// from two markup folders are told apart, ordered by file, then line.
/// </para>
/// </remarks>
public static class PartsRunner
{
    /// <summary>Reads the parts <paramref name="config"/> configures.</summary>
    /// <param name="config">A configuration with a <see cref="AuditConfig.Parts"/> section.</param>
    /// <returns>The reading, its themes' files relative to the configuration.</returns>
    /// <exception cref="AuditConfigException">The configuration has no parts section.</exception>
    public static TemplatePartReading Read(AuditConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        PartsConfig parts = config.Parts ?? throw new AuditConfigException("the configuration declares no parts report");

        string[] files = [.. parts.Assemblies.Select(config.Resolve)];
        FolderLoadContext context = new(files.Select(file => Path.GetDirectoryName(file)!));
        Assembly[] assemblies = [.. files.Select(context.LoadFromAssemblyPath)];

        List<TemplatePartReading> readings = [];
        foreach (string folder in parts.Markup)
        {
            string root = config.Resolve(folder);
            TemplatePartReading reading = TemplatePartRule.ReadParts(XamlScanContext.Load(root).WithAssemblies(assemblies));
            readings.Add(reading with
            {
                Controls =
                [
                    .. reading.Controls.Select(control => control with
                    {
                        Themes = [.. control.Themes.Select(theme => theme with { File = config.Relative(Path.Combine(root, theme.File)) })],
                    }),
                ],
            });
        }

        // The controls, their constants and lookups, and what was not read come from the assemblies alone, so every
        // folder's reading holds the same ones; only the themes differ.
        TemplatePartReading first = readings[0];
        return first with
        {
            Controls =
            [
                .. first.Controls.Select(control => control with
                {
                    Themes =
                    [
                        .. readings
                            .SelectMany(reading => reading.Controls.Single(other => other.Name == control.Name).Themes)
                            .OrderBy(theme => theme.File, StringComparer.Ordinal)
                            .ThenBy(theme => theme.Line),
                    ],
                }),
            ],
        };
    }

    /// <summary>Loads assemblies apart from the tool's own, each dependency from the first of the folders that holds it.</summary>
    private sealed class FolderLoadContext(IEnumerable<string> folders) : AssemblyLoadContext("theme-audit parts")
    {
        private readonly string[] _folders = [.. folders.Distinct(StringComparer.Ordinal)];

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            foreach (string folder in _folders)
            {
                string candidate = Path.Combine(folder, assemblyName.Name + ".dll");
                if (File.Exists(candidate))
                {
                    return LoadFromAssemblyPath(candidate);
                }
            }

            // The framework, and whatever no folder holds, come from the default context.
            return null;
        }
    }
}
