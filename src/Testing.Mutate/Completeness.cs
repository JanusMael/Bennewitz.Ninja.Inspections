// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>A package at a version: what an inert-at-pin marker names, and what a set's pins hold.</summary>
internal readonly record struct Pin(string Package, string Version)
{
    public override string ToString() => $"{Package} {Version}";

    /// <summary>
    /// The version a props file pins each package at, read as central package management reads one:
    /// every <c>PackageVersion</c> element in any namespace, none inside a comment, and the package ids
    /// without regard to case, as NuGet's are.
    /// </summary>
    public static IReadOnlyDictionary<string, string> HeldBy(string path) => HeldIn(XDocument.Load(path));

    /// <summary>The pins a props document holds, read as <see cref="HeldBy"/> reads a file's.</summary>
    public static IReadOnlyDictionary<string, string> HeldIn(XDocument props)
    {
        Dictionary<string, string> held = new(StringComparer.OrdinalIgnoreCase);
        foreach (XElement entry in props.Descendants().Where(e => e.Name.LocalName == "PackageVersion"))
        {
            if (entry.Attribute("Include")?.Value is { } package && entry.Attribute("Version")?.Value is { } version)
            {
                held.TryAdd(package, version);
            }
        }

        return held;
    }

    /// <summary>
    /// The packages a change moved between two readings of a pins file: each pinned at another version, and each
    /// pinned on one side alone. Ids without regard to case, as NuGet's are; a version compared as it is written.
    /// </summary>
    public static IReadOnlySet<string> Moved(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after)
    {
        HashSet<string> moved = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string package, string version) in before)
        {
            if (!after.TryGetValue(package, out string? now) || !string.Equals(now, version, StringComparison.Ordinal))
            {
                moved.Add(package);
            }
        }

        foreach (string package in after.Keys.Where(p => !before.ContainsKey(p)))
        {
            moved.Add(package);
        }

        return moved;
    }
}

/// <summary>A test a set's files declare, and every pin it is marked inert at — none, for a test some mutation must kill.</summary>
internal sealed record DeclaredTest(string Name, IReadOnlyList<Pin> InertAt);

/// <summary>
/// What a set's test files declare: their tests, where each marker sits that none of those tests holds,
/// and every file in which the reader found no test at all.
/// </summary>
internal sealed record Declarations(IReadOnlyList<DeclaredTest> Tests, IReadOnlyList<string> StrayMarkers, IReadOnlyList<string> Barren)
{
    public bool Declares(string test) => Tests.Any(t => t.Name == test);
}

/// <summary>
/// Whether every test a set declares was shown able to fail — killed by some mutation — or is marked
/// inert at a pin the set still holds and was killed by none.
/// </summary>
/// <remarks>
/// A test marked inert that a mutation killed anyway was never inert, and its marker is the defect.
/// That is judged in a narrowed run too, because a kill is evidence whatever else ran; a test killed by
/// none is judged only over the whole set, which is the only run that asked every mutation.
/// </remarks>
internal sealed record Completeness(
    IReadOnlyList<DeclaredTest> Killed,
    IReadOnlyList<DeclaredTest> Inert,
    IReadOnlyList<DeclaredTest> Unkilled,
    IReadOnlyList<(DeclaredTest Test, string By)> Belied,
    bool Narrowed)
{
    /// <param name="tests">The tests the set declares.</param>
    /// <param name="killedBy">Each test some mutation failed, and the first mutation that failed it.</param>
    /// <param name="narrowed">Whether the run was narrowed with --only.</param>
    public static Completeness Of(IReadOnlyList<DeclaredTest> tests, IReadOnlyDictionary<string, string> killedBy, bool narrowed) =>
        new(
            [.. tests.Where(t => t.InertAt.Count == 0 && killedBy.ContainsKey(t.Name))],
            [.. tests.Where(t => t.InertAt.Count > 0 && !killedBy.ContainsKey(t.Name))],
            [.. tests.Where(t => t.InertAt.Count == 0 && !killedBy.ContainsKey(t.Name))],
            [.. tests.Where(t => t.InertAt.Count > 0 && killedBy.ContainsKey(t.Name)).Select(t => (t, killedBy[t.Name]))],
            narrowed);

    public bool Holds => (Unkilled.Count == 0 || Narrowed) && Belied.Count == 0;

    public string Summary =>
        $"declared tests — {(Killed.Count + Inert.Count + Unkilled.Count + Belied.Count).ToString(CultureInfo.InvariantCulture)}: "
        + $"{Killed.Count.ToString(CultureInfo.InvariantCulture)} killed by a mutation, "
        + $"{Inert.Count.ToString(CultureInfo.InvariantCulture)} inert at the current pin, "
        + $"{Unkilled.Count.ToString(CultureInfo.InvariantCulture)} killed by none"
        + (Narrowed ? " (a narrowed run judges none of them)" : string.Empty)
        + $", {Belied.Count.ToString(CultureInfo.InvariantCulture)} with a bad marker";

    /// <summary>Every test a reader has to act on, or to know was excused, by name.</summary>
    public void Print()
    {
        if (Killed.Count + Inert.Count + Unkilled.Count + Belied.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("declared tests no mutation killed:");
        foreach (DeclaredTest test in Unkilled)
        {
            Console.WriteLine($"  {test.Name}");
        }

        if (Unkilled.Count == 0)
        {
            Console.WriteLine("  (none)");
        }

        if (Inert.Count > 0)
        {
            Console.WriteLine("declared tests excused, marked inert at a pin the set's pins still hold and killed by none:");
            foreach (DeclaredTest test in Inert)
            {
                Console.WriteLine($"  {test.Name}, inert at {string.Join(" and ", test.InertAt)}");
            }
        }

        if (Belied.Count > 0)
        {
            Console.WriteLine("declared tests marked inert that a mutation killed anyway — remove each marker:");
            foreach ((DeclaredTest test, string by) in Belied)
            {
                Console.WriteLine($"  {test.Name}, marked inert at {string.Join(" and ", test.InertAt)}, killed by \"{by}\"");
            }
        }
    }
}
