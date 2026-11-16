// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>A set's mutations dealt into shards, a shard's verdicts written down, and shards read back as one run.</summary>
internal static class Shards
{
    /// <summary>
    /// The kth of n shards: every nth mutation from the kth. Dealt round rather than cut into runs, so the
    /// slow mutations of a set, which sit together, spread across the shards.
    /// </summary>
    public static IEnumerable<Mutation> Of(IReadOnlyList<Mutation> all, int index, int count) =>
        all.Where((_, position) => position % count == index - 1);

    /// <summary>How a set is named in a shard's result: as it was given, with <c>/</c>.</summary>
    public static string Name(string? set) => set is null ? "(built in)" : set.Replace('\\', '/');

    /// <summary>
    /// One run's verdicts as tab-separated lines — the shard it was, the tree it ran over and its set, then
    /// a line per verdict, per test each mutation failed and per first assertion it said. Plain text, so two
    /// runs compare by sorting and diffing. Nothing is written when <paramref name="path"/> is null.
    /// </summary>
    public static void Emit(
        string? path,
        string label,
        string? tree,
        string? set,
        IReadOnlyList<Mutation> chosen,
        IReadOnlyDictionary<string, (Reading Reading, string Outcome)> results)
    {
        if (path is null)
        {
            return;
        }

        List<string> lines = [$"shard\t{label}\t{tree}\t{Name(set)}"];
        foreach (Mutation m in chosen)
        {
            if (!results.TryGetValue(m.Name, out (Reading Reading, string Outcome) r))
            {
                continue;
            }

            lines.Add($"mutation\t{m.Name}\t{r.Outcome}");
            lines.AddRange(r.Reading.Failed.Select(test => $"killed\t{m.Name}\t{test}"));
            if (r.Reading.Message.Length > 0)
            {
                lines.Add($"said\t{m.Name}\t{r.Reading.Message.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')}");
            }
        }

        File.WriteAllLines(path, lines);
    }

    /// <summary>
    /// One set's shards read back as one run's results, in the declared order. ⛔ Answers why it refuses
    /// unless the files are shards k/n of that set for every k of one n, over one tree, naming every
    /// declared mutation exactly once between them: a merge of a partial set would judge only what it
    /// reached and present that as the whole, so a lost shard must fail the run rather than shrink it.
    /// </summary>
    public static string? Read(
        IReadOnlyList<string> files,
        string? set,
        IReadOnlyList<Mutation> declared,
        Dictionary<string, (Reading Reading, string Outcome)> results,
        out string tree)
    {
        tree = string.Empty;
        int? total = null;
        HashSet<int> shards = [];
        Dictionary<string, string> outcomes = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> kills = new(StringComparer.Ordinal);
        Dictionary<string, string> said = new(StringComparer.Ordinal);
        foreach (string file in files)
        {
            if (!File.Exists(file))
            {
                return $"no shard's result at {file}";
            }

            string[] lines = File.ReadAllLines(file);
            string[] header = lines.Length > 0 ? lines[0].Split('\t') : [];
            string[] place = header.Length == 4 && header[0] == "shard" ? header[1].Split('/') : [];
            if (place.Length != 2
                || !int.TryParse(place[0], NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                || !int.TryParse(place[1], NumberStyles.None, CultureInfo.InvariantCulture, out int count))
            {
                return $"{file} is not a shard's result: its first line names no shard k/n";
            }

            if (header[3] != Name(set))
            {
                return $"{file} is a result for {header[3]}, not for {Name(set)}";
            }

            if (total is { } n && n != count)
            {
                return $"{file} is a shard of {count.ToString(CultureInfo.InvariantCulture)}, where the others are shards of {n.ToString(CultureInfo.InvariantCulture)}";
            }

            if (tree.Length > 0 && header[2] != tree)
            {
                return $"{file} ran over tree {header[2]}, where the others ran over {tree}";
            }

            if (!shards.Add(index))
            {
                return $"shard {index.ToString(CultureInfo.InvariantCulture)}/{count.ToString(CultureInfo.InvariantCulture)} is given twice";
            }

            (total, tree) = (count, header[2]);
            foreach (string line in lines.Skip(1))
            {
                switch (line.Split('\t'))
                {
                    case ["mutation", string name, string outcome]:
                        if (!outcomes.TryAdd(name, outcome))
                        {
                            return $"\"{name}\" is reported by two shards";
                        }

                        break;

                    case ["killed", string name, string test]:
                        if (!kills.TryGetValue(name, out List<string>? tests))
                        {
                            kills[name] = tests = [];
                        }

                        tests.Add(test);
                        break;

                    case ["said", string name, string words]:
                        said[name] = words;
                        break;

                    default:
                        return $"{file} holds a line no run writes: '{line}'";
                }
            }
        }

        if (total is not { } whole || shards.Count != whole || shards.Any(s => s < 1 || s > whole))
        {
            return $"the results are {shards.Count.ToString(CultureInfo.InvariantCulture)} shard(s) of "
                + $"{total?.ToString(CultureInfo.InvariantCulture) ?? "none"}, and every shard must report";
        }

        string[] missing = [.. declared.Select(m => m.Name).Where(name => !outcomes.ContainsKey(name))];
        string[] unknown = [.. outcomes.Keys.Where(name => declared.All(m => m.Name != name))];
        if (missing.Length > 0 || unknown.Length > 0)
        {
            return $"the shards report {outcomes.Count.ToString(CultureInfo.InvariantCulture)} mutation(s) where "
                + $"{declared.Count.ToString(CultureInfo.InvariantCulture)} are declared"
                + (missing.Length > 0 ? $"; missing: {string.Join(", ", missing)}" : string.Empty)
                + (unknown.Length > 0 ? $"; not declared: {string.Join(", ", unknown)}" : string.Empty);
        }

        string[] strangers = [.. kills.Keys.Concat(said.Keys).Where(name => !outcomes.ContainsKey(name)).Distinct(StringComparer.Ordinal)];
        if (strangers.Length > 0)
        {
            return $"kills or words are credited to mutations no shard reports: {string.Join(", ", strangers)}";
        }

        foreach (Mutation m in declared)
        {
            IReadOnlyList<string> failed = kills.TryGetValue(m.Name, out List<string>? tests) ? tests : [];
            results[m.Name] = (new Reading(string.Empty, said.GetValueOrDefault(m.Name) ?? string.Empty, [], failed), outcomes[m.Name]);
        }

        return null;
    }

    /// <summary>
    /// A set's shards judged as one run of it, exactly as a run judges itself — or refused, answering 2,
    /// when they are not every mutation exactly once or ran over a tree other than <paramref name="expectedTree"/>.
    /// </summary>
    public static int JudgeShards(string? set, Plan plan, Declarations declared, IReadOnlyList<string> files, string expectedTree)
    {
        Dictionary<string, (Reading Reading, string Outcome)> results = new(StringComparer.Ordinal);
        if (Read(files, set, plan.Mutations, results, out string tree) is { } refusal)
        {
            Console.Error.WriteLine($"mutate: {refusal}");
            return 2;
        }

        if (tree != expectedTree)
        {
            Console.Error.WriteLine($"mutate: the shards ran over tree {tree}, and this checkout is tree {expectedTree}, so their verdicts are about other code");
            return 2;
        }

        Dictionary<string, string> killedBy = new(StringComparer.Ordinal);
        foreach (Mutation m in plan.Mutations)
        {
            Verdict.RecordKills(killedBy, m.Name, results[m.Name].Reading.Failed);
        }

        Console.WriteLine($"merged {files.Count.ToString(CultureInfo.InvariantCulture)} shard result(s) over tree {tree}");
        return Verdict.Report(plan.Mutations, results, declared, killedBy, narrowed: false, restoresHeld: true);
    }
}
