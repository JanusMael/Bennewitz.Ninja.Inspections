namespace Testing.Mutate.Tests;

/// <summary>Where the repository is, for tests that run the tool from its root, as a person does.</summary>
internal static class RepoPaths
{
    /// <summary>The folder holding <c>Inspections.slnx</c>, found upward from the test's own output.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>A path below the repository's root.</summary>
    public static string Source(string relative) => Path.Combine(Root, relative);

    private static string FindRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Inspections.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("no Inspections.slnx above the test's output");
    }
}
