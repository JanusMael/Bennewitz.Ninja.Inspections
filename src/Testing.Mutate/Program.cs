// mutate — Bennewitz.Ninja.Testing.Mutate, the family's one mutation runner (plans/00008).
//
// Proves a test able to fail, by breaking the thing it guards and checking that it goes red — and that it goes red in
// the right place — and by making a change that should not matter and checking that it stays green. Moved from
// DiffView's scripts/mutate-behaviour.cs, whose set format, verdicts, completeness, --owed and --parallel it keeps,
// and converged with the runner Inspections kept under tools/, whose rule it keeps that the checkout is never edited, and whose
// budget.
//
//   mutate --set <file.json> --list                  name the set's mutations and what each run filters to, run nothing
//   mutate --set <file.json> --check                 every edit applies once, every killer is declared; run nothing
//   mutate --set <file.json>                         run the set, in a worktree of this checkout
//   mutate --set <file.json> --only <text>           the mutations whose name holds the text
//   mutate --set <a.json> --set <b.json>             several sets, one after another, each judged on its own
//   mutate --owed <ref>                              the committed sets a change since <ref> owes a run
//   mutate --parallel 4 [--set …|--owed <ref>]       every mutation in 4 worktrees at once, each set judged as one run
//   mutate --judge <log> red|green [--killer <test>] [--says <text>]      judge a captured run, run nothing
//   mutate --set <file.json> --kills <kills.json>    judge a set's tests against recorded kills, run nothing
//   mutate --set <file.json> --merge <file.tsv> …    judge shards' results as one run of the set
//   mutate --moved <before> <after>                  the packages a change moved between two pins files
//
//   --configuration <name>   every build and test run's configuration, Debug unless given
//   --property <name=value>  an MSBuild property every build and test run is given; may be repeated
//   --budget <minutes>       how long a run may be estimated to take before it is refused, 30 unless given
//   --swap <project>         a mutation under the project's folder builds it alone and swaps its assembly into the
//                            test output, rather than building the test project; may be repeated
//
// ⭐ The checkout is never edited. A run that would edit cuts a detached worktree from HEAD, loads it with this
// checkout's state as a git tree written through a temporary index, uncommitted work and untracked files included,
// checks that it holds that tree, and runs there; --parallel cuts one worktree a shard. A run killed partway leaves
// only a worktree, which the next run removes once the process that made it is gone (Worktrees).
//
// ⭐ Expect is COMPARED, not printed. A red mutation may name its KILLER, the test that must be among the failures, and
// what its failure must SAY; a green one says BECAUSE, why the change should not matter. Every way of looking like a
// kill without being the declared one fails the run: `wrong-killer`, `wrong-assertion`, and `aborted`, a host that died
// with no test failed. Anything but an explicit `Passed!` is red.
//
// ⭐ A set that names its TEST FILES is held to every test in them: a full run fails while any of those tests was
// killed by no mutation, unless a line in its body reads `// inert-at-pin: <package> <version>` and the set's pins
// still hold that version. A set with red mutations and no green one, its positive control, is refused.
//
// ⛔ A run is estimated after its baseline, and one estimated past its budget is refused before a mutation is built.
//
// Exit codes: 0 every mutation as declared and every declared test accounted for; 1 one was not, a setup fault, or a
// baseline that does not build or is red; 2 a usage error, an unreadable file, a refused budget, a stray write, or a
// merge refused.

using Bennewitz.Ninja.Testing.Mutate;

// Help answers anywhere, a checkout or not, so it is read before anything asks git where the checkout is.
if (args.Any(a => a is "--help" or "-h" or "-?"))
{
    Console.WriteLine(Usage.Text);
    return 0;
}

// Read before anything starts a process, which would otherwise inherit it.
bool startedInAWorktree = Worktrees.TakeStarted();

string repo;
try
{
    repo = Repo.Root();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"mutate: {ex.Message}");
    return 2;
}

List<string> only = [];
List<string> setFiles = [];
string? killsFile = null;
string? shardText = null;
string? parallelText = null;
string? emit = null;
List<string> merge = [];
string? owedRef = null;
string? owedBy = null;
bool list = false;
bool check = false;
string? judgeLog = null;
string? judgeExpect = null;
string? judgeKiller = null;
string? judgeSays = null;

// ⛔ Every flag is COLLECTED here and acted on below, none of them acting from inside the loop: a filter that does
// nothing is worse than one that errors, because the output looks like an answer. Only the two seams return early,
// `--strayed` and `--moved`, and neither builds anything or runs a test.
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--list":
            list = true;
            break;

        case "--check":
            check = true;
            break;

        case "--only" when i + 1 < args.Length:
            only.Add(args[++i]);
            break;

        case "--set" when i + 1 < args.Length:
            setFiles.Add(args[++i]);
            break;

        case "--kills" when i + 1 < args.Length:
            killsFile = args[++i];
            break;

        case "--shard" when i + 1 < args.Length:
            shardText = args[++i];
            break;

        case "--parallel" when i + 1 < args.Length:
            parallelText = args[++i];
            break;

        case "--emit" when i + 1 < args.Length:
            emit = Path.GetFullPath(args[++i]);
            break;

        case "--merge" when i + 1 < args.Length:
            merge.Add(Path.GetFullPath(args[++i]));
            break;

        case "--owed" when i + 1 < args.Length:
            owedRef = args[++i];
            break;

        // Runs no git: the changed paths are read from a file, one to a line, and a pins file's line may say what moved
        // in it, `<path> moves <id> …`. It is how the choice of owed sets is proven, because what git says changed is
        // the state of whatever checkout runs it.
        case "--owed-by" when i + 1 < args.Length:
            owedBy = args[++i];
            break;

        case "--judge" when i + 2 < args.Length:
            judgeLog = args[++i];
            judgeExpect = args[++i];
            break;

        case "--killer" when i + 1 < args.Length:
            judgeKiller = args[++i];
            break;

        case "--says" when i + 1 < args.Length:
            judgeSays = args[++i];
            break;

        case "--configuration" when i + 1 < args.Length:
            Options.Configuration = args[++i];
            break;

        case "--property" when i + 1 < args.Length:
            string property = args[++i];
            if (!property.Contains('=', StringComparison.Ordinal) || property.StartsWith('='))
            {
                Console.Error.WriteLine($"mutate: --property takes name=value, not `{property}`");
                return 2;
            }

            Options.Properties.Add(property);
            break;

        case "--budget" when i + 1 < args.Length:
            string budget = args[++i];
            if (!double.TryParse(budget, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double minutes) || minutes <= 0)
            {
                Console.Error.WriteLine($"mutate: --budget takes a number of minutes above 0, not `{budget}`");
                return 2;
            }

            Options.Budget = TimeSpan.FromMinutes(minutes);
            break;

        case "--swap" when i + 1 < args.Length:
            string swapped = args[++i];
            if (!File.Exists(Path.Combine(repo, swapped)))
            {
                Console.Error.WriteLine($"mutate: --swap takes a project file from the repository's root, and {swapped} is none");
                return 2;
            }

            Options.Swaps.Add(swapped);
            break;

        // Runs nothing: two captured `git status` readings and the file a mutation declared. It is how the stray-write
        // guard is proven, because a real stray is not something a test can arrange.
        case "--strayed" when i + 3 < args.Length:
            string strayed = Git.Strayed(File.ReadAllText(args[i + 1]), File.ReadAllText(args[i + 2]), args[i + 3]);
            Console.WriteLine(strayed.Length == 0 ? "nothing strayed" : strayed);
            return strayed.Length == 0 ? 0 : 1;

        // Runs nothing: two pins files, before a change and after it.
        case "--moved" when i + 2 < args.Length:
            IReadOnlySet<string> moved = Pin.Moved(Pin.HeldBy(args[i + 1]), Pin.HeldBy(args[i + 2]));
            Console.WriteLine(moved.Count == 0
                ? "nothing moved"
                : string.Join(Environment.NewLine, moved.Order(StringComparer.OrdinalIgnoreCase)));
            return 0;

        default:
            Console.Error.WriteLine($"mutate: unknown argument `{args[i]}`");
            return 2;
    }
}

if (judgeLog is not null)
{
    if (judgeExpect is not ("red" or "green"))
    {
        Console.Error.WriteLine($"mutate: --judge expects `red` or `green`, not `{judgeExpect}`");
        return 2;
    }

    string log = Judge.Plain(File.ReadAllText(judgeLog));
    Reading read = Judge.Read(log);
    string outcome = Judge.Outcome(read, new Declared(judgeExpect, judgeKiller, judgeSays), log);
    Console.WriteLine(read);
    Console.WriteLine(outcome);
    return outcome == Judge.Ok ? 0 : 1;
}

if (judgeKiller is not null || judgeSays is not null)
{
    Console.Error.WriteLine("mutate: --killer and --says qualify --judge, and mean nothing without it");
    return 2;
}

// --shard k/n: every nth mutation from the kth, k counted from 1.
(int Index, int Count)? shard = null;
if (shardText is not null)
{
    string[] parts = shardText.Split('/');
    if (parts.Length != 2
        || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int shardIndex)
        || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int shardCount)
        || shardCount < 1 || shardIndex < 1 || shardIndex > shardCount)
    {
        Console.Error.WriteLine($"mutate: --shard takes k/n with 1 <= k <= n, not `{shardText}`");
        return 2;
    }

    shard = (shardIndex, shardCount);
}

int? parallel = null;
if (parallelText is not null)
{
    if (!int.TryParse(parallelText, NumberStyles.None, CultureInfo.InvariantCulture, out int workers) || workers < 1)
    {
        Console.Error.WriteLine($"mutate: --parallel takes a number of worktrees, at least 1, not `{parallelText}`");
        return 2;
    }

    parallel = workers;
}

// Each of these chooses or judges the mutations in a way the others contradict, so a combination is refused rather
// than resolved by a precedence nobody would guess.
string? clash =
    parallel is not null && (shard is not null || merge.Count > 0 || only.Count > 0 || list || check || killsFile is not null || emit is not null)
        ? "--parallel runs every mutation of each set and judges them together; it combines with --set and --owed alone"
    : merge.Count > 0 && (shard is not null || only.Count > 0 || list || check || killsFile is not null || emit is not null || setFiles.Count > 1)
        ? "--merge judges what one set's shards wrote; it combines with one --set alone"
    : shard is not null && only.Count > 0
        ? "--shard and --only each choose a subset, and a merge needs shards of the whole set"
    : killsFile is not null && (shard is not null || emit is not null)
        ? "--kills judges recorded kills and runs nothing, so there is no shard to choose and nothing to emit"
    : owedRef is not null && owedBy is not null
        ? "--owed asks git what changed and --owed-by reads it from a file; give one of them"
    : null;
if (clash is not null)
{
    Console.Error.WriteLine($"mutate: {clash}");
    return 2;
}

// --owed: of the sets named, or else of every committed one, those a change owes a run.
if (owedRef is not null || owedBy is not null)
{
    IReadOnlyList<string> changed;
    Func<string, IReadOnlySet<string>?> movedIn;
    List<string> candidates;
    try
    {
        if (owedBy is not null)
        {
            (changed, IReadOnlyDictionary<string, IReadOnlySet<string>?> moves) = Owed.ReadChanges(repo, File.ReadAllLines(owedBy));
            movedIn = pins => moves.GetValueOrDefault(pins);
        }
        else
        {
            changed = Git.Changed(repo, owedRef!);
            movedIn = pins => Git.MovedSince(repo, owedRef!, pins);
        }

        candidates = setFiles.Count > 0 ? [.. setFiles] : [.. Owed.CommittedSets(repo)];
    }
    catch (Exception ex) when (Unreadable(ex))
    {
        Console.Error.WriteLine($"mutate: what changed cannot be read: {ex.Message}");
        return 2;
    }

    setFiles.Clear();
    foreach (string candidate in candidates)
    {
        (bool Owed, string? Why) verdict;
        try
        {
            verdict = Owed.Because(repo, candidate, Plan.Load(repo, candidate), changed, movedIn);
        }
        catch (Exception ex) when (Unreadable(ex))
        {
            Console.Error.WriteLine($"mutate: the set {candidate} cannot be read: {ex.Message}");
            return 2;
        }

        string why = verdict.Why is null ? string.Empty : $" — {verdict.Why}";
        Console.WriteLine(verdict.Owed ? $"owed      {candidate}{why}" : $"not owed  {candidate}{why}");
        if (verdict.Owed)
        {
            setFiles.Add(candidate);
        }
    }

    if (setFiles.Count == 0)
    {
        Console.WriteLine("no set is owed a run");
        return 0;
    }
}

if (setFiles.Count == 0)
{
    Console.Error.WriteLine("mutate: name a set with --set <file.json>, or the sets a change owes with --owed <ref>");
    return 2;
}

if (setFiles.Count > 1 && (only.Count > 0 || killsFile is not null || emit is not null || shard is not null))
{
    Console.Error.WriteLine("mutate: several sets run one after another, each one whole; --only, --kills, --emit and --shard each name a part of one set");
    return 2;
}

// --merge: shards' results judged as one run of the set, against this checkout's state as it is now — so a merge over
// shards that ran over other code is refused rather than judged.
if (merge.Count > 0)
{
    try
    {
        Plan mergedPlan = Plan.Load(repo, setFiles[0]);
        return Shards.JudgeShards(setFiles[0], mergedPlan, mergedPlan.ReadTests(repo), merge, Source.Tree(repo));
    }
    catch (Exception ex) when (Unreadable(ex))
    {
        Console.Error.WriteLine($"mutate: the set {setFiles[0]} cannot be read: {ex.Message}");
        return 2;
    }
}

// --parallel: every set's setup is checked here, before a worktree is cut, so a set that cannot run costs nothing.
if (parallel is { } worktrees)
{
    List<(string? File, Plan Plan, Declarations Declared)> runs = [];
    foreach (string each in setFiles)
    {
        Plan eachPlan;
        IReadOnlyDictionary<string, string> eachPins;
        try
        {
            eachPlan = Plan.Load(repo, each);
            eachPins = PinsOf(repo, eachPlan);
        }
        catch (Exception ex) when (Unreadable(ex))
        {
            Console.Error.WriteLine($"mutate: the set {each} or its pins cannot be read: {ex.Message}");
            return 2;
        }

        Declarations eachDeclared = eachPlan.ReadTests(repo);
        List<string> eachFaults = [.. Setup.Faults(repo, eachPlan.Mutations, eachPlan.Mutations, eachDeclared, eachPins, eachPlan.Tests.Count > 0)];
        if (Setup.EditsItsOwnSet(repo, each, eachPlan.Mutations) is { } own)
        {
            eachFaults.Add(OwnSetFault(own));
        }

        foreach (string fault in eachFaults)
        {
            Console.WriteLine($"  setup  {Shards.Name(each)}: {fault}");
        }

        if (eachFaults.Count > 0)
        {
            return 1;
        }

        runs.Add((each, eachPlan, eachDeclared));
    }

    return ParallelRun.Run(repo, runs, worktrees);
}

if (setFiles.Count > 1)
{
    List<string> passed = [.. Options.Forwarded];
    if (list)
    {
        passed.Add("--list");
    }

    if (check)
    {
        passed.Add("--check");
    }

    return Self.EachSet(repo, setFiles, passed);
}

string setFile = setFiles[0];

Plan plan;
try
{
    plan = Plan.Load(repo, setFile);
}
catch (Exception ex) when (Unreadable(ex))
{
    Console.Error.WriteLine($"mutate: the set {setFile} cannot be read: {ex.Message}");
    return 2;
}

IReadOnlyDictionary<string, string> pins;
try
{
    pins = PinsOf(repo, plan);
}
catch (Exception ex) when (Unreadable(ex))
{
    Console.Error.WriteLine($"mutate: the pins {plan.Pins} cannot be read: {ex.Message}");
    return 2;
}

List<Mutation> chosen = only.Count == 0
    ? [.. plan.Mutations]
    : [.. plan.Mutations.Where(m => only.Any(o => m.Name.Contains(o, StringComparison.OrdinalIgnoreCase)))];

if (shard is { } part)
{
    chosen = [.. Shards.Of(plan.Mutations, part.Index, part.Count)];
}

// A shard may hold nothing — more worktrees than a set has mutations — and that is a result, not an error.
if (chosen.Count == 0 && shard is null)
{
    Console.Error.WriteLine("mutate: --only matched no mutation. Try --list.");
    return 2;
}

if (list)
{
    // What a run filters to is read from the set's test files. A set naming one that is missing still lists, each of
    // its runs as the whole project; the setup check, which a listing does not reach, refuses it before anything runs.
    IReadOnlyList<string> listedClasses = [];
    try
    {
        listedClasses = plan.ReadClasses(repo);
    }
    catch (Exception ex) when (Unreadable(ex))
    {
        Console.Error.WriteLine($"mutate: the set's test files cannot be read, so each run is listed as the whole project: {ex.Message}");
    }

    // In the order a run applies them, because whether a build restores depends on the mutation before it.
    Mutation? listedBefore = null;
    foreach (Mutation m in chosen)
    {
        Console.WriteLine($"  {m.Name}");
        Console.WriteLine($"      {m.File}");
        Console.WriteLine($"      expects: {m.Describe()}");
        Console.WriteLine($"      runs: {m.Runs(listedClasses, listedBefore)}");
        listedBefore = m;
    }

    return 0;
}

// The setup check runs before every run, not only under --check: a mutation that cannot apply, or names a test that
// does not exist, would otherwise cost a build and a test run to report itself.
Declarations declared = plan.ReadTests(repo);
List<string> faults = [.. Setup.Faults(repo, chosen, plan.Mutations, declared, pins, plan.Tests.Count > 0)];
if (Setup.EditsItsOwnSet(repo, setFile, plan.Mutations) is { } ownSet)
{
    faults.Add(OwnSetFault(ownSet));
}

foreach (string fault in faults)
{
    Console.WriteLine($"  setup  {fault}");
}

int marked = declared.Tests.Count(t => t.InertAt.Count > 0);
Console.WriteLine(
    $"setup: {chosen.Count.ToString(CultureInfo.InvariantCulture)} mutation(s) over "
    + $"{declared.Tests.Count.ToString(CultureInfo.InvariantCulture)} declared test(s)"
    + (marked > 0 ? $" ({marked.ToString(CultureInfo.InvariantCulture)} marked inert)" : string.Empty)
    + $", {faults.Count.ToString(CultureInfo.InvariantCulture)} fault(s)");

if (faults.Count > 0)
{
    return 1;
}

if (check)
{
    // What a run of this set would give every build and test over the environment they inherit, read from the one
    // place those runs read it.
    foreach ((string name, string value) in Dotnet.BuildEnvironment)
    {
        Console.WriteLine($"  note   every build and test run is given {name}={value}");
    }

    Console.WriteLine($"  note   every build and test run uses the {Options.Configuration} configuration");
    foreach (string property in Options.Properties)
    {
        Console.WriteLine($"  note   every build and test run is given -p:{property}");
    }

    foreach (string swap in Options.Swaps)
    {
        Console.WriteLine($"  note   a mutation under {swap}'s folder builds it alone and swaps its assembly into the test output");
    }

    Console.WriteLine($"  note   a run estimated past {Minutes(Options.Budget)} is refused");
    return 0;
}

bool narrowed = only.Count > 0 || shard is not null;

// Runs nothing past the setup: kills recorded in a file stand in for the runs that would have made them, and are judged
// exactly as a run's own are.
if (killsFile is not null)
{
    // A judgement of no tests at all would hold, and say so.
    if (plan.Tests.Count == 0)
    {
        Console.Error.WriteLine("mutate: --kills judges the tests a set's files declare, and this set names no test file");
        return 2;
    }

    Dictionary<string, string> recorded = new(StringComparer.Ordinal);
    try
    {
        string path = Path.IsPathRooted(killsFile) ? killsFile : Path.Combine(repo, killsFile);
        foreach ((string mutation, List<string> failed) in JsonSerializer.Deserialize(File.ReadAllText(path), MutationSetJson.Default.DictionaryStringListString) ?? [])
        {
            Verdict.RecordKills(recorded, mutation, failed);
        }
    }
    catch (Exception ex) when (Unreadable(ex))
    {
        Console.Error.WriteLine($"mutate: the kills {killsFile} cannot be read: {ex.Message}");
        return 2;
    }

    Completeness judged = Completeness.Of(declared.Tests, recorded, narrowed);
    judged.Print();
    Console.WriteLine();
    Console.WriteLine(judged.Summary);
    Console.WriteLine(judged.Holds
        ? "every declared test is accounted for"
        : "A DECLARED TEST WAS NEVER BROKEN, OR ONE MARKED INERT WAS");
    return judged.Holds ? 0 : 1;
}

// ⛔ From here the run edits files, and only in a worktree this tool cut for it (plans/00008, decision 2): anywhere
// else, it cuts one and runs again there with the same arguments.
if (!Worktrees.IsOurs(repo))
{
    // ⛔ A run started in a worktree a run cut for it never cuts another: one that does not know the worktree as its
    // own would cut one more each time, without end.
    if (startedInAWorktree)
    {
        Console.Error.WriteLine($"mutate: started in a worktree cut for this run, but git names this checkout {repo}, "
            + $"and the run that cut it named {Environment.GetEnvironmentVariable(Worktrees.Marker)}; refusing to cut another");
        return 2;
    }

    return Worktrees.RunOne(repo, args);
}

// The state this run is over, taken before anything is edited: a shard's merge must find every shard over the one
// tree --parallel gave them.
string? ranOver = emit is null ? null : Source.Tree(repo);
string label = shard is { } of ? $"{of.Index.ToString(CultureInfo.InvariantCulture)}/{of.Count.ToString(CultureInfo.InvariantCulture)}" : "1/1";

if (chosen.Count == 0)
{
    Shards.Emit(emit, label, ranOver, setFile, chosen, new Dictionary<string, (Reading Reading, string Outcome)>());
    Console.WriteLine($"shard {label} of this set holds no mutation, so nothing was built or run");
    return 0;
}

Console.WriteLine($"at {Git.Head(repo)}, {plan.Project}{(plan.Class is null ? string.Empty : " " + plan.Class)}");

Stopwatch baselineClock = Stopwatch.StartNew();
if (!Dotnet.Build(repo, plan.Project, out string baselineBuild))
{
    Console.Error.WriteLine("baseline: the clean tree does not build — nothing was mutated");
    Console.Error.WriteLine(Tail(baselineBuild));
    return 1;
}

TimeSpan buildTime = baselineClock.Elapsed;

// A mutation that names its killer runs only the classes of the set's test files, decided by Mutation.ClassesFrom,
// which --list prints. The baseline runs the set's own filter, the widest any mutation here is judged by.
IReadOnlyList<string> setClasses = plan.ReadClasses(repo);

baselineClock.Restart();
Reading baseline = Judge.Read(Dotnet.Test(repo, plan.Project, plan.Class is { } setClass ? [setClass] : []));
TimeSpan testTime = baselineClock.Elapsed;
if (baseline.Verdict != "green")
{
    Console.Error.WriteLine($"baseline: the clean tree is red — nothing was mutated. {baseline}");
    return 1;
}

Console.WriteLine("baseline: green");

// ⛔ The budget (plans/00008, decision 3): each mutation is a build and a test run, so the baseline's pair, which
// restored and ran the widest filter, is what each is estimated at.
TimeSpan estimate = (buildTime + testTime) * chosen.Count;
Console.WriteLine($"estimate: {chosen.Count.ToString(CultureInfo.InvariantCulture)} mutation(s), at most about {Minutes(estimate)}, against a budget of {Minutes(Options.Budget)}");
if (estimate > Options.Budget)
{
    Console.Error.WriteLine($"mutate: refused: the run is estimated at {Minutes(estimate)}, past its budget of {Minutes(Options.Budget)}. "
        + "Narrow it with --only, spread it with --parallel, or give it --budget <minutes>.");
    return 2;
}

// Read once the baseline has built both, from MSBuild, for these builds' configuration and properties.
List<Swap> swaps = [];
foreach (string project in Options.Swaps)
{
    if (Swap.Resolve(repo, plan.Project, project, out string fault) is not { } swap)
    {
        Console.Error.WriteLine($"mutate: {fault}");
        return 2;
    }

    swaps.Add(swap);
    Console.WriteLine($"swap: a mutation under {project}'s folder builds it alone and copies {Path.GetFileName(swap.Assembly)} into the test output");
}

Dictionary<string, (Reading Reading, string Outcome)> results = [];
Dictionary<string, string> killedBy = new(StringComparer.Ordinal);
Dictionary<string, string> backups = new(StringComparer.Ordinal);
bool started = false;
bool restoresHeld = true;
Mutation? applied = null;
int index = 0;

try
{
    foreach (Mutation mutation in chosen)
    {
        index++;
        string path = Path.Combine(repo, mutation.File.Replace('/', Path.DirectorySeparatorChar));
        byte[] original = File.ReadAllBytes(path);
        Snapshot.Decode(original, out string before, out Encoding encoding);

        string progress = $"[{index.ToString(CultureInfo.InvariantCulture)}/{chosen.Count.ToString(CultureInfo.InvariantCulture)}]";
        Stopwatch mutationClock = Stopwatch.StartNew();
        int matches = Setup.Count(before, mutation.Old);
        if (matches != 1)
        {
            Console.WriteLine($"{progress} no-op  {mutation.Name} — matched {matches.ToString(CultureInfo.InvariantCulture)} times, not once");
            results[mutation.Name] = (Reading.NoOp(matches), "no-op");
            continue;
        }

        if (!backups.ContainsKey(path))
        {
            backups[path] = Snapshot.Keep(repo, path, original);
        }

        string statusBefore = Git.Fingerprint(repo);
        byte[] mutated = [.. encoding.GetPreamble(), .. encoding.GetBytes(before.Replace(mutation.Old, mutation.New, StringComparison.Ordinal))];
        File.WriteAllBytes(path, mutated);
        started = true;

        try
        {
            // Compared before the run, not after it: an edit that lands somewhere unexpected must stop the run before it
            // has spent minutes producing a verdict about the wrong tree.
            string stray = Git.Strayed(statusBefore, Git.Fingerprint(repo), mutation.File);
            if (stray.Length > 0)
            {
                Console.Error.WriteLine($"mutate: `{mutation.Name}` wrote outside {mutation.File}:");
                Console.Error.WriteLine(stray);
                return 2;
            }

            // The baseline restored. A mutation's build restores again when its edit is to what restore reads, and so
            // does the build after it: a revert leaves obj/ describing the project as the edit made it.
            bool restore = mutation.RestoresAfter(applied);
            applied = mutation;

            // A swap never restores, so a mutation that must is built as the test project (plans/00008, decision 5).
            Swap? swap = restore ? null : swaps.FirstOrDefault(s => s.Takes(repo, mutation));
            string how = swap is null ? string.Empty : "swapped, ";
            string buildLog;
            if (!(swap is null ? Dotnet.Build(repo, plan.Project, out buildLog, restore: restore) : swap.Build(repo, out buildLog)))
            {
                Console.WriteLine($"{progress} build  {mutation.Name} — the mutation does not compile ({how}{Seconds(mutationClock.Elapsed)})");
                results[mutation.Name] = (Reading.Unbuildable(buildLog), "build");
                continue;
            }

            string log = Dotnet.Test(repo, plan.Project, mutation.ClassesFrom(setClasses));
            Reading reading = Judge.Read(log);
            Verdict.RecordKills(killedBy, mutation.Name, reading.Failed);
            string outcome = Judge.Outcome(reading, mutation.Declared, log);
            results[mutation.Name] = (reading, outcome);
            Console.WriteLine(outcome == Judge.Ok
                ? $"{progress} ok     {mutation.Name} ({how}{Seconds(mutationClock.Elapsed)})"
                : $"{progress} WRONG  {mutation.Name} — expected {mutation.Describe()}, got {outcome} ({how}{Seconds(mutationClock.Elapsed)})");
        }
        finally
        {
            File.WriteAllBytes(path, original);
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original))
            {
                restoresHeld = false;
                Console.Error.WriteLine($"mutate: the revert of {mutation.File} did not take — its original is at {backups[path]}");
            }
        }
    }
}
finally
{
    if (started)
    {
        // A revert leaves the mutation's assemblies in bin/. Anything run after this program without a rebuild would
        // test them.
        Console.WriteLine("rebuilding, because a revert does not unbuild a mutation");
        Dotnet.Build(repo, plan.Project, out _);
    }

    if (restoresHeld)
    {
        foreach (string backup in backups.Values)
        {
            File.Delete(backup);
        }
    }
}

Shards.Emit(emit, label, ranOver, setFile, chosen, results);
return Verdict.Report(chosen, results, declared, killedBy, narrowed, restoresHeld);

static string Tail(string log) => log.Length > 1200 ? log[^1200..] : log;

static string Seconds(TimeSpan time) => time.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";

static string Minutes(TimeSpan time) => time.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + " min";

// The pins a set's markers are judged against. A set that names none, in a repository that pins nothing centrally,
// holds none, so any marker in it is a fault, as one naming a package the pins do not hold is; pins a set names must
// be there.
static IReadOnlyDictionary<string, string> PinsOf(string repo, Plan plan)
{
    string path = Path.Combine(repo, plan.Pins.Replace('/', Path.DirectorySeparatorChar));
    return plan.Pins == Plan.DefaultPins && !File.Exists(path)
        ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        : Pin.HeldBy(path);
}

static string OwnSetFault(Mutation mutation) =>
    $"\"{mutation.Name}\" edits the set that declares it, so under its own edit the set would no longer read as it does "
    + "— a set is never a mutation's subject, and no test reads a set it is held to";

static bool Unreadable(Exception ex) =>
    ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException or XmlException;
