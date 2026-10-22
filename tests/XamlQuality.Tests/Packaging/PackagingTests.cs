using System.Xml.Linq;
using Xunit;

namespace XamlQuality.Tests.Packaging;

/// <summary>
/// Guards what this repository publishes: that every packable project is deliberately classified
/// in <c>packages.push</c> or <c>packages.local</c>, and that the release workflow publishes what
/// <c>packages.push</c> declares and globs nothing.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>A published NuGet version can never be replaced</b> — only unlisted. A <c>*.nupkg</c> glob
/// publishes whatever happens to be in the folder, so the one time it picks up something unintended
/// is the one time it cannot be undone. So the guard runs on every push rather than at release
/// time, when finding out is still free.
/// </para>
/// <para>
/// ⚠ <b>The argument this exists to refuse is "it is fine, every packed id is pushable."</b> That
/// is true of today's project list and of no other. Another packable project — an analyzer, a
/// second tool, a sample — would be swept up by a glob with nothing objecting. Bennewitz.Ninja.DiffView
/// came within one project of exactly that: its <c>src/ThemeAudit</c> was packable and
/// local-feed-only, so a glob would have published it.
/// </para>
/// <para>
/// These are the family template's <c>PackagingTests</c>, from <c>templates/bbpkg</c> in
/// Bennewitz.Ninja.Templates, with one assertion of this repository's: the workflow spells out no
/// id that <c>packages.push</c> declares.
/// </para>
/// </remarks>
public sealed class PackagingTests
{
    /// <summary>
    /// Every packable project appears in <c>packages.push</c> or <c>packages.local</c>. Adding one
    /// and classifying it nowhere fails here rather than at the push.
    /// </summary>
    [Fact]
    public void Every_packable_project_is_classified()
    {
        HashSet<string> packable = PackableIds();
        HashSet<string> classified = [.. Ids("packages.push"), .. Ids("packages.local")];

        Assert.NotEmpty(packable);

        string[] unclassified = [.. packable.Except(classified).Order()];
        Assert.True(
            unclassified.Length == 0,
            "Packable but in neither packages.push nor packages.local: " + string.Join(", ", unclassified) +
            ". Classify it — publishing it by accident cannot be undone.");

        string[] phantom = [.. classified.Except(packable).Order()];
        Assert.True(
            phantom.Length == 0,
            "Listed but nothing packs it: " + string.Join(", ", phantom) +
            ". A stale entry means the push names a package that will not exist.");
    }

    /// <summary>
    /// No id is in both lists, which would say "publish this" and "never publish this" at once.
    /// </summary>
    [Fact]
    public void No_id_is_both_published_and_private()
    {
        string[] both = [.. Ids("packages.push").Intersect(Ids("packages.local")).Order()];

        Assert.True(both.Length == 0, "In both packages.push and packages.local: " + string.Join(", ", both));
    }

    /// <summary>
    /// No release step globs, the push, the GitHub release and the preflight's list each take their
    /// ids from <c>packages.push</c>, and no id it declares is repeated in the workflow.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The ids are deliberately NOT asserted literally in the workflow.</b> A workflow that
    /// repeats the list has two homes for it, and the second one silently rots when a package is
    /// added — the failure this whole file exists to prevent, reintroduced by the guard meant to
    /// stop it. Driving every step from the declaration gives the list exactly one home, and
    /// <c>scripts/assert-packages.cs</c> closes the loop by checking the packed output against
    /// that same file.
    /// </remarks>
    [Fact]
    public void The_release_workflow_globs_nothing_and_publishes_what_is_declared()
    {
        string[] instructions = ReleaseWorkflow();
        string workflow = string.Join('\n', instructions);

        Assert.DoesNotContain("*.nupkg", workflow, StringComparison.Ordinal);

        foreach (string step in (string[])["Report the preflight result", "Push to NuGet.org", "Create GitHub Release"])
        {
            Assert.True(
                Step(instructions, step).Contains("packages.push", StringComparison.Ordinal),
                $"The step \"{step}\" does not read packages.push, so it stops publishing what is declared.");
        }

        foreach (string id in Ids("packages.push"))
        {
            Assert.False(
                workflow.Contains(id, StringComparison.Ordinal),
                $"release.yml spells out {id}, which packages.push declares. A second home for the list rots.");
        }
    }

    /// <summary>
    /// The packed output is checked against both lists before the push, so a package nothing
    /// declares stops the release before it becomes permanent.
    /// </summary>
    [Fact]
    public void The_release_workflow_checks_what_was_packed_before_it_pushes()
    {
        string[] instructions = ReleaseWorkflow();

        int check = Array.FindIndex(instructions, line => line.Contains("scripts/assert-packages.cs", StringComparison.Ordinal));
        int push = Array.FindIndex(instructions, line => line.Contains("dotnet nuget push", StringComparison.Ordinal));

        Assert.True(check >= 0, "release.yml never runs scripts/assert-packages.cs.");
        Assert.True(push > check, "release.yml pushes before it checks what was packed.");
    }

    /// <summary>
    /// Nothing in <c>packages.local</c> is named anywhere in the release workflow — a private
    /// package should have no path to a publishing step at all.
    /// </summary>
    [Fact]
    public void The_release_workflow_never_names_a_private_package()
    {
        string workflow = string.Join('\n', ReleaseWorkflow());

        foreach (string id in Ids("packages.local"))
        {
            Assert.DoesNotContain(id, workflow, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Both publishing steps still exist. Without this the tests above pass vacuously against a
    /// workflow that has stopped publishing at all.
    /// </summary>
    [Fact]
    public void The_release_workflow_still_has_both_publishing_steps()
    {
        string workflow = string.Join('\n', ReleaseWorkflow());

        Assert.Contains("dotnet nuget push", workflow, StringComparison.Ordinal);
        Assert.Contains("gh release create", workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// The release workflow's lines, comment lines dropped, because the comments above the
    /// publishing steps explain the very glob these tests forbid — a whole-file search would fail
    /// on the explanation.
    /// </summary>
    private static string[] ReleaseWorkflow()
    {
        string path = Path.Combine(RepoRoot(), ".github", "workflows", "release.yml");
        Assert.True(File.Exists(path), "There is no release workflow, so nothing publishes — and nothing gates what would.");

        return [.. File.ReadLines(path).Where(line => !line.TrimStart().StartsWith('#'))];
    }

    /// <summary>The lines of the step named <paramref name="name"/>, up to the next step.</summary>
    private static string Step(string[] lines, string name)
    {
        int start = Array.FindIndex(lines, line => line.Trim() == "- name: " + name);
        Assert.True(start >= 0, $"release.yml has no step named \"{name}\".");

        int end = Array.FindIndex(lines, start + 1, line => line.TrimStart().StartsWith("- name: ", StringComparison.Ordinal));
        return string.Join('\n', lines[start..(end < 0 ? lines.Length : end)]);
    }

    /// <summary>
    /// Reads one of the two package lists, dropping blank lines and <c>#</c> comments.
    /// </summary>
    private static HashSet<string> Ids(string fileName)
    {
        string path = Path.Combine(RepoRoot(), fileName);
        Assert.True(File.Exists(path), fileName + " is missing, so nothing declares what may be published.");

        return
        [
            .. File.ReadLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
        ];
    }

    /// <summary>
    /// Package ids of every packable project under <c>src</c>, read from the project files rather
    /// than from packed output, so the guard runs before anything has been packed.
    /// </summary>
    /// <remarks>
    /// ⚠ <c>PackageId</c> falls back to <c>AssemblyName</c> and then the project file name when it
    /// is not set, which is how a project acquires an id nobody chose. Mirrored here so an
    /// unclassified project cannot hide behind an unset property.
    /// </remarks>
    private static HashSet<string> PackableIds()
    {
        HashSet<string> ids = [];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.csproj", SearchOption.AllDirectories))
        {
            XDocument document = XDocument.Load(project);

            // ⛔ Fails closed: a project is packable unless it says otherwise, because the SDK packs a
            // class library by default. Counting only an explicit IsPackable=true missed exactly the
            // project nobody classified. The last declaration wins, as it does in MSBuild.
            bool packable = !string.Equals(
                document.Descendants("IsPackable").LastOrDefault()?.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase);

            if (!packable)
            {
                continue;
            }

            string? declared = document.Descendants("PackageId").LastOrDefault()?.Value.Trim();
            string? assembly = document.Descendants("AssemblyName").LastOrDefault()?.Value.Trim();

            ids.Add(Pick(declared, assembly, Path.GetFileNameWithoutExtension(project)));
        }

        return ids;

        static string Pick(string? declared, string? assembly, string fallback) =>
            !string.IsNullOrWhiteSpace(declared) ? declared
            : !string.IsNullOrWhiteSpace(assembly) ? assembly
            : fallback;
    }

    /// <summary>
    /// Walks up from the test assembly to the directory holding the solution, so the tests do not
    /// depend on the working directory a runner happens to choose.
    /// </summary>
    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "XamlQuality.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
