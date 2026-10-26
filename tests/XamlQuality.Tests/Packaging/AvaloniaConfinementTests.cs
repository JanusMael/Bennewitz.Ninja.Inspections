using System.Text.Json;
using System.Xml.Linq;

namespace XamlQuality.Tests.Packaging;

/// <summary>
/// Avalonia stays in the one project that compiles the runtime theme check's files: no other project in
/// <c>Inspections.slnx</c> takes a package whose id names Avalonia, directly or through another package.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>The library takes no framework, Avalonia included,</b> and since <c>plans/00003</c> the solution
/// holds a project that does: <c>tests/XamlQuality.ThemeRuntime.Tests</c>, which compiles the source-only
/// package's files against Avalonia's headless platform. Any other project that took Avalonia, or a
/// package that brings it along, as <c>Semi.Avalonia</c> and <c>Avalonia.AvaloniaEdit</c> do, would carry
/// it into the library's tests or a shipped assembly unannounced.
/// </para>
/// <para>
/// ⚠ <b>Read from each project's restore graph, <c>obj/project.assets.json</c>,</b> which records every
/// package a project takes, transitive ones included, where a csproj names only its direct references. A
/// project whose assets file is missing fails: unread, it would pass for one that takes nothing.
/// </para>
/// </remarks>
public sealed class AvaloniaConfinementTests
{
    /// <summary>The one project in the solution that may take Avalonia, as the solution writes its path.</summary>
    private const string AvaloniaProject = "tests/XamlQuality.ThemeRuntime.Tests/XamlQuality.ThemeRuntime.Tests.csproj";

    [Fact]
    public void No_other_project_in_the_solution_takes_an_Avalonia_package()
    {
        string root = RepoRoot();
        List<string> found = [];
        foreach (string project in SolutionProjects(root).Where(project => project != AvaloniaProject))
        {
            string assets = Path.Combine(root, Path.GetDirectoryName(project)!, "obj", "project.assets.json");
            Assert.True(File.Exists(assets), $"{project} has no obj/project.assets.json, so what it takes cannot be read. "
                + "Restore the solution, or build XamlQuality.Tests, which restores every other project.");
            found.AddRange(AvaloniaPackagesIn(File.ReadAllText(assets)).Select(package => $"{project}: {package}"));
        }

        Assert.True(found.Count == 0, "Avalonia reaches beyond " + AvaloniaProject + ":" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }

    [Fact]
    public void The_guard_reads_every_other_project_the_solution_holds()
    {
        // ⓘ A solution read as holding nothing would pass the test above with nothing checked.
        string[] projects = SolutionProjects(RepoRoot());

        Assert.Contains(AvaloniaProject, projects);
        Assert.Contains("src/XamlQuality/XamlQuality.csproj", projects);
        Assert.Contains("tests/XamlQuality.Tests/XamlQuality.Tests.csproj", projects);
    }

    [Fact]
    public void A_package_whose_id_names_Avalonia_is_found_and_a_project_is_not()
    {
        const string assets = """
            {
              "version": 3,
              "libraries": {
                "Avalonia.AvaloniaEdit/12.0.0": { "type": "package" },
                "Bennewitz.Ninja.XamlQuality/1.0.0": { "type": "project" },
                "Semi.Avalonia/12.0.0": { "type": "package" },
                "System.CommandLine/2.0.0": { "type": "package" },
                "Some.Avalonia.Views/1.0.0": { "type": "project" }
              }
            }
            """;

        Assert.Equal(["Avalonia.AvaloniaEdit/12.0.0", "Semi.Avalonia/12.0.0"], AvaloniaPackagesIn(assets));
    }

    /// <summary>Each package in a restore graph whose id names Avalonia, as <c>id/version</c>, in ordinal order.</summary>
    private static string[] AvaloniaPackagesIn(string assets)
    {
        using JsonDocument document = JsonDocument.Parse(assets);
        return
        [
            .. document.RootElement.GetProperty("libraries").EnumerateObject()
                .Where(library => library.Value.TryGetProperty("type", out JsonElement type) && type.GetString() == "package")
                .Select(library => library.Name)
                .Where(name => name.Split('/')[0].Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>Every project <c>Inspections.slnx</c> holds, by its path from the root, written with <c>/</c>.</summary>
    private static string[] SolutionProjects(string root) =>
    [
        .. XDocument.Load(Path.Combine(root, "Inspections.slnx"))
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value.Replace('\\', '/'))
            .OfType<string>(),
    ];

    /// <summary>
    /// Walks up from the test assembly to the directory holding the solution, so the tests do not
    /// depend on the working directory a runner happens to choose.
    /// </summary>
    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Inspections.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
