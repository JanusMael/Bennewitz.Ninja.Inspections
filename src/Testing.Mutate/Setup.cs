// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>What is checked before anything is edited, built or run.</summary>
internal static partial class Setup
{
    /// <param name="mutations">The mutations this run chose, which every edit-level check reads.</param>
    /// <param name="whole">Every mutation of the set, which the set-level rules read: a shard or an
    /// <c>--only</c> holds a part, and a part dealt no green mutation is not a set without one.</param>
    public static IEnumerable<string> Faults(
        string repo, IReadOnlyList<Mutation> mutations, IReadOnlyList<Mutation> whole, Declarations declared,
        IReadOnlyDictionary<string, string> pins, bool hasDeclaredTests)
    {
        // ⛔ A set's positive control (DECISIONS.md, 2026-10-22): a green run is the only one in which a
        // test failing under every mutation shows, each red one reading as its kill.
        if (whole.Any(m => m.Expect == "red") && !whole.Any(m => m.Expect == "green"))
        {
            yield return "this set holds red mutations and no green mutation, so a test that fails under every one of "
                + "them would read as killed by each — add a control: an edit that changes nothing, declared green with "
                + "its because";
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (Mutation m in mutations)
        {
            if (!names.Add(m.Name))
            {
                yield return $"\"{m.Name}\" is declared twice, and a name is how a mutation is found";
            }

            string path = Path.Combine(repo, m.File.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                yield return $"\"{m.Name}\" names a file that does not exist: {m.File}";
                continue;
            }

            Snapshot.Decode(File.ReadAllBytes(path), out string text, out _);
            int matches = Count(text, m.Old);
            if (matches != 1)
            {
                yield return $"\"{m.Name}\" matches {matches.ToString(CultureInfo.InvariantCulture)} times in {m.File}, not once";
            }

            if (hasDeclaredTests && m.Killer is { } killer && !declared.Declares(killer))
            {
                yield return $"\"{m.Name}\" names a killer the set's test files do not declare: {killer}";
            }
        }

        // ⛔ A marker is judged against its pin before anything runs, and in a dry run too: a moved pin
        // is exactly the change that lands green while the test it excused stops being excused.
        foreach (DeclaredTest test in declared.Tests)
        {
            foreach (Pin marker in test.InertAt)
            {
                string? pinned = pins.GetValueOrDefault(marker.Package);
                if (pinned != marker.Version)
                {
                    string now = pinned is null ? $"the pins name no {marker.Package}" : $"the pins hold it at {pinned}";
                    yield return $"{test.Name} is marked inert at {marker}, and {now} — re-measure it, then give it a mutation or mark it again";
                }
            }
        }

        foreach (string stray in declared.StrayMarkers)
        {
            yield return $"{stray} holds a marker no declared test does — it excuses nothing; put it in the body of the test it is "
                + "for, or find why the reader did not see that test";
        }

        // ⛔ The positive control on the reading itself: a file in which the reader finds no test would
        // pass every judgement made of its tests, a lost test reading as no test at all.
        foreach (string file in declared.Barren)
        {
            yield return $"{file} declares no test the reader can see, and a reading that finds nothing passes";
        }
    }

    public static int Count(string body, string needle)
    {
        int n = 0;
        for (int at = body.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = body.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }

    /// <summary>
    /// The tests a file declares — a public method whose block of attributes, read upward from it,
    /// holds a <c>Fact</c> or a <c>Theory</c>, Avalonia's or xUnit's, whatever else sits beside it —
    /// each with the pins its body marks it inert at; and the line of every marker no test's body holds.
    /// </summary>
    /// <remarks>
    /// ⛔ Read by lines, never by matching brackets: an attribute's arguments may hold a <c>]</c> of
    /// their own — <c>[InlineData("""{ "candidates": [ 3 ] }""")]</c> is plan 00034 step 4's — and a
    /// pattern for <c>[…]</c> stops at it and loses the test, which then reads as one no mutation is
    /// asked to kill rather than as a test the reader never saw. A body runs from the declaration to
    /// the first line closing it at the declaration's own indentation, which is how every test file
    /// here is laid out.
    /// </remarks>
    public static (IReadOnlyList<DeclaredTest> Tests, IReadOnlyList<int> StrayMarkers) TestsIn(string source)
    {
        string[] lines = source.Split('\n');
        List<(string Name, int From, int To)> bodies = [];
        for (int i = 0; i < lines.Length; i++)
        {
            Match method = TestMethod().Match(lines[i]);
            if (!method.Success)
            {
                continue;
            }

            for (int above = i - 1; above >= 0; above--)
            {
                string line = lines[above].Trim();
                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!line.StartsWith('['))
                {
                    break;
                }

                if (TestAttribute().IsMatch(line))
                {
                    bodies.Add((method.Groups["name"].Value, i, BodyEnd(lines, i, method.Groups["indent"].Value)));
                    break;
                }
            }
        }

        List<Pin>[] marks = [.. bodies.Select(_ => new List<Pin>())];
        List<int> strays = [];
        for (int i = 0; i < lines.Length; i++)
        {
            Match marker = InertMarker().Match(lines[i]);
            if (!marker.Success)
            {
                continue;
            }

            int holder = bodies.FindIndex(b => b.From < i && i < b.To);
            if (holder < 0)
            {
                strays.Add(i + 1);
                continue;
            }

            marks[holder].Add(new Pin(marker.Groups["package"].Value, marker.Groups["version"].Value));
        }

        return ([.. bodies.Select((b, at) => new DeclaredTest(b.Name, marks[at]))], strays);
    }

    /// <summary>
    /// The line closing the member declared on line <paramref name="at"/>: the first after it that holds
    /// only a brace at the declaration's own indentation, or the end of the file.
    /// </summary>
    private static int BodyEnd(string[] lines, int at, string indent)
    {
        for (int i = at + 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd('\r') == indent + "}")
            {
                return i;
            }
        }

        return lines.Length;
    }

    /// <summary>
    /// The classes a test file declares, each qualified by the file's namespace, as <c>--filter-class</c>
    /// takes them. A nested class comes out with its own name alone, which no class has, so it filters in
    /// nothing; a file declaring none gives nothing, and a mutation judged by it then runs its set's whole filter.
    /// </summary>
    public static IEnumerable<string> ClassesIn(string source)
    {
        Match space = Namespace().Match(source);
        string prefix = space.Success ? space.Groups["name"].Value + "." : string.Empty;
        foreach (Match declared in ClassDeclaration().Matches(source))
        {
            yield return prefix + declared.Groups["name"].Value;
        }
    }

    /// <summary>
    /// The first mutation that edits the set declaring it, or null. ⛔ Under its own edit the set no longer
    /// reads as it did, so a run, and any test that reads the set, would judge a set that is not this one:
    /// DiffView found it when its runner's own set's green control went red.
    /// </summary>
    public static Mutation? EditsItsOwnSet(string repo, string? setFile, IEnumerable<Mutation> mutations)
    {
        if (setFile is null)
        {
            return null;
        }

        StringComparison paths = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        string set = Path.GetFullPath(Path.IsPathRooted(setFile) ? setFile : Path.Combine(repo, setFile));
        return mutations.FirstOrDefault(
            m => string.Equals(Path.GetFullPath(Path.Combine(repo, m.File.Replace('/', Path.DirectorySeparatorChar))), set, paths));
    }

    [GeneratedRegex(@"(?m)^\s*namespace\s+(?<name>[\w.]+)")]
    private static partial Regex Namespace();

    [GeneratedRegex(@"(?m)^\s*(?:(?:public|internal|sealed|static|partial|abstract)\s+)*class\s+(?<name>\w+)")]
    private static partial Regex ClassDeclaration();

    [GeneratedRegex(@"^(?<indent>\s*)public\s+(?:async\s+Task|void)\s+(?<name>\w+)\s*\(")]
    private static partial Regex TestMethod();

    [GeneratedRegex(@"^\[(?:Avalonia)?(?:Fact|Theory)\b")]
    private static partial Regex TestAttribute();

    /// <summary>AGENTS.md §5's marker, spelled as <c>scripts/mutate-gates</c> reads it.</summary>
    [GeneratedRegex(@"^\s*//\s*inert-at-pin:\s*(?<package>\S+)\s+(?<version>\S+)\s*$")]
    private static partial Regex InertMarker();
}
