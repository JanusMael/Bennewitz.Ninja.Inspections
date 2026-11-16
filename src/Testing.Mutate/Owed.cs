// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>Which committed sets a change owes a run.</summary>
internal static class Owed
{
    /// <summary>Every set committed beside its tests, <c>tests/&lt;project&gt;/Mutations/*.json</c>, named relative to the repository.</summary>
    public static IEnumerable<string> CommittedSets(string repo)
    {
        foreach (string project in Directory.EnumerateDirectories(Path.Combine(repo, "tests")).Order(StringComparer.Ordinal))
        {
            string mutations = Path.Combine(project, "Mutations");
            if (!Directory.Exists(mutations))
            {
                continue;
            }

            foreach (string set in Directory.EnumerateFiles(mutations, "*.json").Order(StringComparer.Ordinal))
            {
                yield return Path.GetRelativePath(repo, set).Replace('\\', '/');
            }
        }
    }

    /// <summary>Paths compared as the file system compares them.</summary>
    private static StringComparer Paths =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>A path as a set or a change names it, made relative to the repository with <c>/</c>.</summary>
    public static string Normal(string repo, string path) =>
        Path.GetRelativePath(repo, Path.GetFullPath(Path.Combine(repo, path.Trim().Replace('/', Path.DirectorySeparatorChar)))).Replace('\\', '/');

    /// <summary>
    /// What <c>--owed-by</c> reads in place of git: a changed path to a line, and for a pins file what moved in it,
    /// <c>&lt;path&gt; moves &lt;id&gt; …</c>, the word with no id after it saying that nothing did. A line without
    /// the word leaves what moved unknown, as a pins file that does not parse would.
    /// </summary>
    public static (IReadOnlyList<string> Changed, IReadOnlyDictionary<string, IReadOnlySet<string>?> Moves) ReadChanges(
        string repo, IEnumerable<string> lines)
    {
        List<string> changed = [];
        Dictionary<string, IReadOnlySet<string>?> moves = new(Paths);
        foreach (string line in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int at = Array.IndexOf(words, "moves");
            string path = Normal(repo, at < 0 ? line : string.Join(' ', words[..at]));
            changed.Add(path);
            moves[path] = at < 0 ? null : new HashSet<string>(words[(at + 1)..], StringComparer.OrdinalIgnoreCase);
        }

        return (changed, moves);
    }

    /// <summary>
    /// Whether a change owes a set a run, and why — or why not, where its pins changed. A change to the set's own
    /// file, one of its test files or a file one of its mutations edits owes it. Its pins changing owes it only
    /// where a package that moved reaches it: a marker in its tests names the package, or its project's restore
    /// resolves it. A pin reaches nothing else, so a set it does not reach would rebuild what it built before.
    /// What moved being unknown, or a project nothing has restored, owes the run rather than guessing.
    /// </summary>
    /// <param name="movedIn">What moved in a pins file, given its path as <see cref="Normal"/> makes it; null when that cannot be read.</param>
    public static (bool Owed, string? Why) Because(
        string repo, string setFile, Plan plan, IReadOnlyList<string> changed, Func<string, IReadOnlySet<string>?> movedIn)
    {
        HashSet<string> touched = new(changed.Select(p => Normal(repo, p)), Paths);
        string[] plain = [setFile, .. plan.Tests, .. plan.Mutations.Select(m => m.File)];
        if (plain.Select(p => Normal(repo, p)).FirstOrDefault(touched.Contains) is { } input)
        {
            return (true, $"{input} changed");
        }

        string pins = Normal(repo, plan.Pins);
        if (!touched.Contains(pins))
        {
            return (false, null);
        }

        IReadOnlySet<string>? moved = movedIn(pins);
        if (moved is null)
        {
            return (true, $"{pins} changed, and what moved in it is not known");
        }

        if (moved.Count == 0)
        {
            return (false, $"{pins} changed and moved no package");
        }

        HashSet<string> marked = new(
            plan.ReadTests(repo).Tests.SelectMany(t => t.InertAt).Select(p => p.Package), StringComparer.OrdinalIgnoreCase);
        string[] named = [.. moved.Where(marked.Contains).Order(StringComparer.OrdinalIgnoreCase)];
        if (named.Length > 0)
        {
            return (true, $"{pins} moved {string.Join(", ", named)}, which a marker in its tests names");
        }

        string all = string.Join(", ", moved.Order(StringComparer.OrdinalIgnoreCase));
        IReadOnlySet<string>? resolved = Resolved(repo, plan.Project);
        if (resolved is null)
        {
            return (true, $"{pins} moved {all}, and nothing has restored {plan.Project} to say whether it resolves it");
        }

        string[] reached = [.. moved.Where(resolved.Contains).Order(StringComparer.OrdinalIgnoreCase)];
        return reached.Length > 0
            ? (true, $"{pins} moved {string.Join(", ", reached)}, which {plan.Project} resolves")
            : (false, $"{pins} moved {all}, which {plan.Project} does not resolve");
    }

    /// <summary>
    /// The package ids a project's last restore resolved, direct and transitive, read from the
    /// <c>obj/project.assets.json</c> beside it; null when there is none, nothing having restored it here.
    /// </summary>
    public static IReadOnlySet<string>? Resolved(string repo, string project)
    {
        string directory = Path.GetDirectoryName(Path.Combine(repo, project.Replace('/', Path.DirectorySeparatorChar))) ?? repo;
        string assets = Path.Combine(directory, "obj", "project.assets.json");
        if (!File.Exists(assets))
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(assets));
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        if (document.RootElement.TryGetProperty("libraries", out JsonElement libraries))
        {
            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                ids.Add(library.Name.Split('/')[0]);
            }
        }

        return ids;
    }
}
