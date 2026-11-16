// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <param name="Name">What is broken, in the words of the contract it breaks.</param>
/// <param name="File">The one file the edit may touch; anything else stops the run.</param>
/// <param name="Old">A literal that must appear exactly once.</param>
/// <param name="New">What replaces it.</param>
/// <param name="Class">The test class to run, or null for the set's own filter.</param>
/// <param name="Expect">`red` or `green`, compared rather than printed.</param>
/// <param name="Because">Why that is the right expectation — the part a reader needs in a year.</param>
/// <param name="Killer">For a red mutation, the test that must be among the failures.</param>
/// <param name="Says">Text the run must contain: the declared assertion's own words.</param>
internal sealed record Mutation(
    string Name, string File, string Old, string New, string? Class, string Expect, string? Because,
    string? Killer = null, string? Says = null)
{
    public Declared Declared => new(Expect, Killer, Says);

    /// <summary>Whether the edit lands in a file restore reads, so that the mutation's build must restore again.</summary>
    public bool EditsTheBuild =>
        File.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        || File.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
        || File.EndsWith(".targets", StringComparison.OrdinalIgnoreCase)
        || File.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
        || File.EndsWith("global.json", StringComparison.OrdinalIgnoreCase)
        || File.EndsWith("NuGet.config", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The test classes the mutation's run is filtered to, given the classes of its set's test files; none
    /// is the set's whole filter. Its own class, when it names one. A red mutation naming its killer runs
    /// only the set's classes, where that killer is declared — the trim the header measures. A green one,
    /// and a red one naming none, run everything their set would, so that narrowing can relabel a failure
    /// and never pass one.
    /// </summary>
    public IReadOnlyList<string> ClassesFrom(IReadOnlyList<string> setClasses) =>
        Class is { } named ? [named] : Expect == "red" && Killer is not null ? setClasses : [];

    /// <summary>
    /// Whether the mutation's build restores, given the mutation applied before it in the same run: when its own
    /// edit lands in a file restore reads, and when that mutation's did, because a revert leaves <c>obj/</c>
    /// describing the project as the edit made it — after a pin moved and moved back, the next build would
    /// otherwise build against the version the pin was moved to. The run and <c>--list</c> both ask this, so the
    /// listing says what the run does.
    /// </summary>
    public bool RestoresAfter(Mutation? previous) => EditsTheBuild || previous?.EditsTheBuild == true;

    /// <summary>What the mutation's run builds and tests, as <c>--list</c> prints it, after the mutation before it.</summary>
    public string Runs(IReadOnlyList<string> setClasses, Mutation? previous)
    {
        IReadOnlyList<string> classes = ClassesFrom(setClasses);
        string build = !RestoresAfter(previous) ? "built without a restore"
            : EditsTheBuild ? "restored before it builds, the edit being to what restore reads"
            : "restored before it builds, the mutation before it having edited what restore reads";
        return (classes.Count > 0 ? string.Join(", ", classes) : "the whole project") + ", " + build;
    }

    public string Describe()
    {
        string expectation = Killer is null ? Expect : $"{Expect}, killed by {Killer}";
        string reason = Because ?? (Says is null ? "the test it names guards it" : $"the test it names says \"{Says}\"");
        return $"{expectation} — {reason}";
    }
}

/// <summary>Which project the mutations are judged in, and what holds them to account.</summary>
/// <param name="Project">The test project, relative to the repository.</param>
/// <param name="Class">A test class every run is filtered to, or null for the whole project.</param>
/// <param name="Tests">Test files whose every test some mutation must kill; empty to judge none.</param>
/// <param name="Mutations">The mutations.</param>
/// <param name="Pins">The props file the tests' inert-at-pin markers are judged against, relative to the repository.</param>
internal sealed record Plan(string Project, string? Class, IReadOnlyList<string> Tests, IReadOnlyList<Mutation> Mutations, string Pins)
{
    /// <summary>The pins a set is judged against when it names none: the ones the build itself restores.</summary>
    public const string DefaultPins = "Directory.Packages.props";

    public Declarations ReadTests(string repo)
    {
        List<DeclaredTest> tests = [];
        List<string> strays = [];
        List<string> barren = [];
        foreach (string file in Tests)
        {
            (IReadOnlyList<DeclaredTest> found, IReadOnlyList<int> stray) =
                Setup.TestsIn(File.ReadAllText(Path.Combine(repo, file.Replace('/', Path.DirectorySeparatorChar))));
            tests.AddRange(found);
            strays.AddRange(stray.Select(line => $"{file}:{line.ToString(CultureInfo.InvariantCulture)}"));
            if (found.Count == 0)
            {
                barren.Add(file);
            }
        }

        return new Declarations(tests, strays, barren);
    }

    /// <summary>The classes the set's test files declare, named as the test runner's class filter names them.</summary>
    public IReadOnlyList<string> ReadClasses(string repo) =>
        [.. Tests.SelectMany(t => Setup.ClassesIn(File.ReadAllText(Path.Combine(repo, t.Replace('/', Path.DirectorySeparatorChar))))).Distinct(StringComparer.Ordinal)];

    /// <summary>Reads a committed set, refusing one that leaves a mutation unable to be judged.</summary>
    public static Plan Load(string repo, string file)
    {
        string path = Path.IsPathRooted(file) ? file : Path.Combine(repo, file);
        MutationSet set = JsonSerializer.Deserialize(File.ReadAllText(path), MutationSetJson.Default.MutationSet)
            ?? throw new InvalidDataException("the set is empty");

        if (string.IsNullOrWhiteSpace(set.Project))
        {
            throw new InvalidDataException("the set names no project");
        }

        List<Mutation> mutations = [];
        foreach (SetMutation m in set.Mutations ?? [])
        {
            string expect = m.Expect ?? "red";
            if (string.IsNullOrWhiteSpace(m.Name) || string.IsNullOrWhiteSpace(m.File) || string.IsNullOrEmpty(m.Old) || m.New is null)
            {
                throw new InvalidDataException($"a mutation lacks a name, a file, an old text or a new one: \"{m.Name}\"");
            }

            if (expect is not ("red" or "green"))
            {
                throw new InvalidDataException($"\"{m.Name}\" expects `{expect}`, which is neither red nor green");
            }

            // A mutation with no stated reason is one nobody can judge a year from now. For a red one
            // the killer it names is a reason — test names here are sentences — and a green one must
            // say why the change should not matter.
            if (m.Killer is null && m.Because is null)
            {
                throw new InvalidDataException($"\"{m.Name}\" names neither its killer nor why it expects what it does");
            }

            if (expect == "green" && (m.Killer is not null || m.Says is not null))
            {
                throw new InvalidDataException($"\"{m.Name}\" is declared green and names a killer or words to say, which only a red one can");
            }

            mutations.Add(new Mutation(m.Name, m.File, m.Old, m.New, m.Class ?? set.Class, expect, m.Because, m.Killer, m.Says));
        }

        return mutations.Count == 0
            ? throw new InvalidDataException("the set declares no mutation")
            : new Plan(set.Project, set.Class, set.Tests ?? [], mutations, set.Pins ?? DefaultPins);
    }
}

internal sealed record MutationSet(string Project, string? Class, List<string>? Tests, string? Pins, List<SetMutation>? Mutations);

internal sealed record SetMutation(
    string Name, string File, string Old, string New, string? Expect, string? Killer, string? Says, string? Because, string? Class);

// ⛔ A key the set's types do not know is REFUSED, not skipped: `"kiler"` read as nothing would drop
// the one check a mutation was written to make, and the run would report the mutation as declared.
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(MutationSet))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
partial class MutationSetJson : JsonSerializerContext;
