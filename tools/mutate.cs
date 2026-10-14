#!/usr/bin/env dotnet
// The mutation runner of plans/00001: plants each defect a catalog lists, one at a time, in a copy of
// the working tree, rebuilds only what the defect touches, runs one test class, and reports whether
// exactly the tests the catalog expects failed. A port of pmutate_fast.py, its semantics kept.
//
// ⭐ ONE DEFECT A RUN. Two defects at once can mask each other, so a test that seems to catch one may
// fail for the other's sake. Each run plants one, and puts the file back byte for byte, checked by hash.
//
// ⭐ COPIES, NOT THE TREE. Each worker holds its own copy of the working tree under
// artifacts/mutate/<pool>/, brought level with the tree at the start of every run, with its own build
// output and its own temp folder. The tree itself is never edited, so a stopped run cannot leave a
// defect in it. The class runs from the test project that holds it, the folder under tests/ its file is
// in. A defect under src/XamlQuality/ rebuilds the library alone and swaps its DLL into that project's
// output; any other rebuilds that project.
//
// ⛔ UNDER 30 MINUTES. After the first worker's baseline it times one library rebuild and one class
// run, estimates the whole run from them, and refuses a run estimated past 30 minutes unless given
// --over-30-minutes.
//
// Usage:
//   dotnet run --file tools/mutate.cs -- <TestClass> <catalog.json> [<output.txt>]
//       [--workers N] [--only ID ...] [--pool NAME] [--over-30-minutes]
//
//   A catalog is a JSON array of { "id", "file", "old", "new", "expect" }: "old" must occur exactly once
//   in "file", a path relative to the repository root, and "expect" names the tests of the class the
//   defect should fail. The output defaults to artifacts/mutate/<TestClass>.txt; the pool, under
//   artifacts/mutate/, to the class's name, so that runs of different classes can go at once.
//
// Exit codes: 0 every defect failed exactly the tests expected; 1 one did not; 2 a usage error, a refused
// estimate, a baseline that is not clean, or a run that was stopped.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

string? testClass = null, catalogPath = null, outputPath = null, poolName = null;
int workerCount = 4;
bool overThirtyMinutes = false;
List<string> only = [];
for (int i = 0; i < args.Length; i++)
{
    string argument = args[i];
    if (argument == "--workers" && i + 1 < args.Length && int.TryParse(args[i + 1], out int count) && count > 0)
    {
        workerCount = count;
        i++;
    }
    else if (argument == "--pool" && i + 1 < args.Length)
    {
        poolName = args[++i];
    }
    else if (argument == "--over-30-minutes")
    {
        overThirtyMinutes = true;
    }
    else if (argument == "--only")
    {
        while (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            only.Add(args[++i]);
        }
    }
    else if (argument.StartsWith("--", StringComparison.Ordinal))
    {
        return Usage($"unknown or incomplete argument '{argument}'.");
    }
    else if (testClass is null)
    {
        testClass = argument;
    }
    else if (catalogPath is null)
    {
        catalogPath = argument;
    }
    else if (outputPath is null)
    {
        outputPath = argument;
    }
    else
    {
        return Usage($"unexpected argument '{argument}'.");
    }
}

if (testClass is null || catalogPath is null || !Regex.IsMatch(testClass, @"^\w+$"))
{
    return Usage("name a test class and a catalog.");
}

(int topExit, string top, _) = Shell.Run("git", Directory.GetCurrentDirectory(), null, ["rev-parse", "--show-toplevel"], Shell.Timeout);
if (topExit != 0)
{
    return Usage("run it from inside the repository.");
}

string root = Path.GetFullPath(top.Trim());
string className = testClass;
Mutation[] mutations;
try
{
    mutations = Mutation.ReadAll(Path.GetFullPath(catalogPath));
}
catch (Exception error) when (error is IOException or JsonException or InvalidDataException)
{
    return Usage($"{catalogPath}: {error.Message}");
}

if (only.Count > 0)
{
    mutations = [.. mutations.Where(mutation => only.Contains(mutation.Id))];
}

if (mutations.Length == 0)
{
    return Usage("the catalog, as filtered, holds no mutation.");
}

string pool = Path.Combine(root, "artifacts", "mutate", poolName ?? className);
string output = Path.GetFullPath(outputPath ?? Path.Combine(root, "artifacts", "mutate", className + ".txt"));

// The tree as a commit would see it with everything uncommitted added: tracked files that still exist,
// and untracked ones git does not ignore.
(int lsExit, string listing, string lsErrors) = Shell.Run("git", root, null,
    ["ls-files", "-z", "--cached", "--others", "--exclude-standard"], Shell.Timeout);
if (lsExit != 0)
{
    return Usage("git ls-files failed: " + lsErrors.Trim());
}

string[] files = [.. listing.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct().Where(file => File.Exists(Path.Combine(root, file)))];

using CancellationTokenSource stop = new();
Console.CancelKeyPress += (_, press) =>
{
    // Stop taking defects, stop what is running, and let each worker put its file back.
    press.Cancel = true;
    stop.Cancel();
    Shell.StopAll();
};

Worker[] workers = [.. Enumerable.Range(0, Math.Min(workerCount, mutations.Length)).Select(index => new Worker(index, pool, root, className))];
Stopwatch clock = Stopwatch.StartNew();
try
{
    workers[0].Prepare(files, stop.Token);
    double estimate = workers[0].Estimate(mutations, workers.Length);
    Console.Error.WriteLine($"estimate: {mutations.Length} mutations, about {estimate / 60:F0} min with {workers.Length} workers");
    if (estimate > 30 * 60 && !overThirtyMinutes)
    {
        Console.Error.WriteLine("mutate: refused: the run is estimated past 30 minutes. Cut the catalog, or pass --over-30-minutes.");
        return 2;
    }

    Parallel.ForEach(workers.Skip(1), worker => worker.Prepare(files, stop.Token));
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("mutate: stopped before any mutation ran.");
    return 2;
}
catch (Exception error) when (error is InvalidOperationException or AggregateException)
{
    Console.Error.WriteLine("mutate: " + (error is AggregateException many ? string.Join("\n", many.InnerExceptions.Select(inner => inner.Message)) : error.Message));
    return 2;
}

Console.Error.WriteLine($"{workers.Length} workers ready in {clock.Elapsed.TotalSeconds:F0} s");
string[] tests = workers[0].DiscoverTests();

ConcurrentQueue<int> pending = new(Enumerable.Range(0, mutations.Length));
Result?[] results = new Result?[mutations.Length];
ConcurrentBag<string> failures = [];
int done = 0;
Stopwatch began = Stopwatch.StartNew();
Task[] running =
[
    .. workers.Select(worker => Task.Run(() =>
    {
        try
        {
            while (!stop.IsCancellationRequested && failures.IsEmpty && pending.TryDequeue(out int index))
            {
                Stopwatch timer = Stopwatch.StartNew();
                Result result = worker.Mutate(mutations[index], stop.Token) with { Seconds = timer.Elapsed.TotalSeconds };
                results[index] = result;
                Console.Error.WriteLine($"[{Interlocked.Increment(ref done)}/{mutations.Length}] {result.Line} ({result.Seconds:F1} s, worker {worker.Index})");
            }

            if (!stop.IsCancellationRequested)
            {
                worker.Finish();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception error)
        {
            // A file that would not go back, or a library that would not rebuild: stop every worker.
            failures.Add($"worker {worker.Index}: {error.Message}");
        }
    })),
];
Task.WaitAll(running);

if (stop.IsCancellationRequested)
{
    Console.Error.WriteLine($"mutate: stopped after {done} of {mutations.Length} mutations; no output written.");
    return 2;
}

if (!failures.IsEmpty)
{
    Console.Error.WriteLine("mutate: " + string.Join("\n", failures));
    return 2;
}

List<string> lines = [$"baseline: {tests.Length} tests, none failing"];
HashSet<string> caught = [];
int mismatches = 0;
foreach (Result result in results.Select(result => result!))
{
    lines.Add(result.Line);
    caught.UnionWith(result.Failed);
    mismatches += result.Ok ? 0 : 1;
}

string[] never = [.. tests.Where(test => !caught.Contains(test))];
if (only.Count == 0)
{
    lines.Add($"tests never failed: {(never.Length > 0 ? Py.List(never) : "none")}");
}

lines.Add($"mutations not failing exactly as expected: {mismatches}");
lines.Add($"{mutations.Length} mutations in {began.Elapsed.TotalSeconds:F0} s with {workers.Length} workers, "
    + $"{results.Average(result => result!.Seconds):F1} s each in a worker");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, string.Join('\n', lines) + "\n");
foreach (string line in lines.TakeLast(3))
{
    Console.WriteLine(line);
}

return mismatches == 0 ? 0 : 1;

static int Usage(string problem)
{
    Console.Error.WriteLine("mutate: " + problem);
    Console.Error.WriteLine("usage: dotnet run --file tools/mutate.cs -- <TestClass> <catalog.json> [<output.txt>] [--workers N] [--only ID ...] [--pool NAME] [--over-30-minutes]");
    return 2;
}

/// <summary>One defect: the text in a file that it replaces, and the tests it should fail.</summary>
sealed record Mutation(string Id, string File, string Old, string New, string[] Expect)
{
    // ⚠ Read through JsonDocument, not the serializer: a file-based app is analysed for native AOT,
    // which rejects reflection-based deserialization, and warnings are errors here.
    public static Mutation[] ReadAll(string path)
    {
        using JsonDocument document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("a catalog is a JSON array of mutations.");
        }

        List<Mutation> mutations = [];
        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            string? id = Text(entry, "id"), file = Text(entry, "file"), old = Text(entry, "old"), replacement = Text(entry, "new");
            string[]? expect = entry.TryGetProperty("expect", out JsonElement tests) && tests.ValueKind == JsonValueKind.Array
                ? [.. tests.EnumerateArray().Select(test => test.ValueKind == JsonValueKind.String ? test.GetString() ?? "" : "")]
                : null;
            if (id is null || file is null || old is not { Length: > 0 } || replacement is null || expect is null)
            {
                throw new InvalidDataException($"{id ?? "a mutation"} needs an id, a file, a non-empty old, a new and an expect.");
            }

            mutations.Add(new Mutation(id, file.Replace('\\', '/'), old, replacement, expect));
        }

        return [.. mutations];
    }

    private static string? Text(JsonElement entry, string property) =>
        entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

/// <summary>What one defect did: the line the output carries, and the tests that failed.</summary>
sealed record Result(string Line, bool Ok, IReadOnlyCollection<string> Failed, double Seconds = 0);

/// <summary>
/// A test project under tests/, by paths from the repository root: its project file, its Release output,
/// and the name of the test assembly it builds there.
/// </summary>
sealed record TestProject(string Project, string Output, string Assembly)
{
    /// <summary>The project in the folder under tests/ that holds <paramref name="classFile"/>.</summary>
    public static TestProject Holding(string root, string classFile)
    {
        string[] below = Path.GetRelativePath(Path.Combine(root, "tests"), classFile).Replace('\\', '/').Split('/');
        if (below.Length < 2)
        {
            throw new InvalidOperationException($"{classFile} is in no project folder under tests/.");
        }

        string folder = "tests/" + below[0];
        string[] projects = Directory.GetFiles(Path.Combine(root, folder), "*.csproj");
        if (projects.Length != 1)
        {
            throw new InvalidOperationException($"{folder} holds {projects.Length} project files, so the test project is not known.");
        }

        string file = Path.GetFileName(projects[0]);
        string assembly = XDocument.Load(projects[0]).Descendants("AssemblyName").Select(element => element.Value.Trim()).FirstOrDefault(value => value.Length > 0)
            ?? Path.GetFileNameWithoutExtension(file);
        return new TestProject($"{folder}/{file}", $"{folder}/bin/Release/net10.0", assembly);
    }
}

/// <summary>A copy of the working tree, its build output, and its temp folder.</summary>
sealed class Worker(int index, string pool, string tree, string testClass)
{
    private const string LibraryProject = "src/XamlQuality/XamlQuality.csproj";
    private const string LibraryOutput = "src/XamlQuality/bin/Release/net10.0";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    // ⚠ Avalonia's build telemetry starts a process that outlives the build, and a test project here
    // takes Avalonia, so every build turns it off, as real-code.cs and probe.cs do.
    private static readonly Dictionary<string, string> BuildEnvironment = new() { ["AVALONIA_TELEMETRY_OPTOUT"] = "1" };

    private double baselineBuild;
    private double classRun;
    private string? qualifiedClass;
    private TestProject? testProject;

    public int Index => index;

    private string Root => Path.Combine(pool, $"w{index}");

    // Each worker's tests get a temp folder of their own: the tests delete their xq-views-* folders,
    // and two workers sharing one delete each other's.
    private string Temp => Path.Combine(pool, $"w{index}-tmp");

    /// <summary>The test project that holds the class, read from the copy once it is level with the tree.</summary>
    private TestProject Tests => testProject ??= TestProject.Holding(Root, ClassFile());

    /// <summary>Brings the copy level with the tree, builds it, and checks the class passes as it stands.</summary>
    public void Prepare(IReadOnlyList<string> files, CancellationToken token)
    {
        Mirror(files);
        Directory.CreateDirectory(Temp);
        Stopwatch timer = Stopwatch.StartNew();
        (int code, string output) = Build(Tests.Project, restore: true);
        baselineBuild = timer.Elapsed.TotalSeconds;
        token.ThrowIfCancellationRequested();
        if (code != 0)
        {
            throw new InvalidOperationException($"worker {index}: the baseline build failed:\n{Shell.Tail(output)}");
        }

        timer.Restart();
        (HashSet<string> failed, int exit, int total) = RunClass();
        classRun = timer.Elapsed.TotalSeconds;
        token.ThrowIfCancellationRequested();

        // ⛔ A class filter that matches nothing exits 0 with "Total: 0", and every defect would then
        // read as caught by nothing: the baseline must run at least the class's [Fact] and
        // [AvaloniaFact] tests.
        int facts = DiscoverTests().Length;
        if (failed.Count > 0 || exit != 0 || total == 0 || total < facts)
        {
            throw new InvalidOperationException(
                $"worker {index}: the baseline is not clean: failed {Py.List(failed.Order(StringComparer.Ordinal))}, exit {exit}, {total} tests run of the {facts} [Fact] and [AvaloniaFact] tests found");
        }

        Console.Error.WriteLine($"worker {index}: ready, baseline clean");
    }

    /// <summary>Seconds the whole run should take: one library rebuild and one class run per defect.</summary>
    public double Estimate(IReadOnlyList<Mutation> mutations, int workers)
    {
        Stopwatch timer = Stopwatch.StartNew();
        (int code, string output) = BuildLibrary(incremental: false);
        if (code != 0)
        {
            throw new InvalidOperationException($"worker {index}: rebuilding the library failed:\n{Shell.Tail(output)}");
        }

        double library = timer.Elapsed.TotalSeconds;
        return mutations.Sum(mutation => (IsLibrary(mutation.File) ? library : baselineBuild) + classRun) / workers;
    }

    /// <summary>
    /// The class's [Fact] methods, as pmutate_fast.py found them, and its [AvaloniaFact] ones, which
    /// Avalonia.Headless.XUnit runs on its UI thread; either may be an async Task.
    /// </summary>
    public string[] DiscoverTests() =>
        [.. Regex.Matches(File.ReadAllText(ClassFile()), @"\[(?:Fact|AvaloniaFact)\]\s+public (?:async\s+)?(?:void|Task) (\w+)\(")
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal)];

    /// <summary>Plants the defect, builds what it touches, runs the class, and puts the file back.</summary>
    public Result Mutate(Mutation mutation, CancellationToken token)
    {
        string path = Path.Combine(Root, mutation.File);
        if (!File.Exists(path))
        {
            return new Result($"{mutation.Id}: {mutation.File} does not exist, not run", false, []);
        }

        byte[] original = File.ReadAllBytes(path);
        string digest = Convert.ToHexString(SHA256.HashData(original));
        string text = Utf8.GetString(original);
        int count = Occurrences(text, mutation.Old);
        if (count != 1)
        {
            return new Result($"{mutation.Id}: old text occurs {count} times, not run", false, []);
        }

        HashSet<string> failed;
        try
        {
            File.WriteAllBytes(path, Utf8.GetBytes(text.Replace(mutation.Old, mutation.New, StringComparison.Ordinal)));
            (int code, string output) = BuildFor(mutation.File);
            token.ThrowIfCancellationRequested();
            if (code == Shell.TimedOut)
            {
                return new Result($"{mutation.Id}: TIMEOUT dotnet", false, []);
            }

            if (code != 0)
            {
                string[] errors = [.. output.ReplaceLineEndings("\n").Split('\n').Where(line => line.Contains("error", StringComparison.Ordinal)).Take(3)];
                return new Result($"{mutation.Id}: BUILD FAILED {Py.List(errors)}", false, []);
            }

            (failed, int exit, int total) = RunClass();
            token.ThrowIfCancellationRequested();
            if (exit == Shell.TimedOut)
            {
                return new Result($"{mutation.Id}: TIMEOUT dotnet", false, []);
            }

            // ⛔ Neither a run that ran nothing nor one that failed without naming a test says the
            // defect went uncaught.
            if (total == 0 || (exit != 0 && failed.Count == 0))
            {
                return new Result($"{mutation.Id}: THE TEST RUN FAILED, exit {exit}, {total} tests run, none named as failing", false, []);
            }
        }
        finally
        {
            File.WriteAllBytes(path, original);
            if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) != digest)
            {
                throw new InvalidOperationException($"{mutation.Id}: RESTORE FAILED for {path}");
            }
        }

        HashSet<string> expected = [.. mutation.Expect];
        bool ok = failed.SetEquals(expected);
        string line = $"{mutation.Id}: {(ok ? "ok" : "MISMATCH")} failed {failed.Count}" + (ok
            ? ""
            : $" | unexpected {Py.List(failed.Except(expected).Order(StringComparer.Ordinal))} | missing {Py.List(expected.Except(failed).Order(StringComparer.Ordinal))}");
        return new Result(line, ok, failed);
    }

    /// <summary>Puts the unmutated library back into the tests' output, so no worker keeps a mutant.</summary>
    public void Finish()
    {
        (int code, string output) = BuildFor("src/XamlQuality/");
        if (code != 0)
        {
            throw new InvalidOperationException($"rebuilding the unmutated library failed:\n{Shell.Tail(output)}");
        }
    }

    private static bool IsLibrary(string file) => file.StartsWith("src/XamlQuality/", StringComparison.Ordinal);

    private static int Occurrences(string text, string old)
    {
        int count = 0;
        for (int at = text.IndexOf(old, StringComparison.Ordinal); at >= 0; at = text.IndexOf(old, at + old.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Copies every file of the tree that the copy lacks or holds differently, and deletes from the copy
    /// what the tree no longer has, its build output aside.
    /// </summary>
    private void Mirror(IReadOnlyList<string> files)
    {
        Directory.CreateDirectory(Root);
        HashSet<string> wanted = new(files, StringComparer.Ordinal);
        foreach (string file in files)
        {
            string from = Path.Combine(tree, file);
            string to = Path.Combine(Root, file);
            if (File.Exists(to) && File.ReadAllBytes(to).AsSpan().SequenceEqual(File.ReadAllBytes(from)))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(from, to, overwrite: true);

            // ⚠ A copy keeps its source's time, which can be older than the copy's last build, and an
            // incremental build would then keep the stale output.
            File.SetLastWriteTimeUtc(to, DateTime.UtcNow);
        }

        foreach (string path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(Root, path).Replace('\\', '/');
            if (relative.Split('/').Any(part => part is "bin" or "obj") || wanted.Contains(relative))
            {
                continue;
            }

            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
    }

    // ⚠ No node reuse: an MSBuild node would outlive the build, so a run, or a stopped one, would leave
    // processes behind. The shared compiler server stays on: it serves every build on the machine, as
    // it does any dotnet build, and turning it off took each defect from 2.7 s to 10 s.
    private (int Code, string Output) Build(string project, bool restore, bool incremental = true)
    {
        List<string> arguments = ["build", Path.Combine(Root, project), "-c", "Release", "-nologo", "-v:q", "-nodeReuse:false"];
        if (!restore)
        {
            arguments.Add("--no-restore");
        }

        if (!incremental)
        {
            arguments.Add("--no-incremental");
        }

        (int code, string output, string errors) = Shell.Run("dotnet", Root, BuildEnvironment, arguments, Shell.Timeout);
        return (code, output + errors);
    }

    private (int Code, string Output) BuildFor(string file) => IsLibrary(file) ? BuildLibrary(incremental: true) : Build(Tests.Project, restore: false);

    private (int Code, string Output) BuildLibrary(bool incremental)
    {
        (int code, string output) = Build(LibraryProject, restore: false, incremental);
        if (code == 0)
        {
            // Release embeds the PDB in the DLL (Directory.Build.props); a Debug build has one beside it.
            foreach (string name in (string[])["XamlQuality.dll", "XamlQuality.pdb"])
            {
                string built = Path.Combine(Root, LibraryOutput, name);
                if (File.Exists(built))
                {
                    File.Copy(built, Path.Combine(Root, Tests.Output, name), overwrite: true);
                }
            }
        }

        return (code, output);
    }

    private (HashSet<string> Failed, int Exit, int Total) RunClass()
    {
        qualifiedClass ??= QualifiedClass();
        Dictionary<string, string> environment = new() { ["TMP"] = Temp, ["TEMP"] = Temp, ["TMPDIR"] = Temp };
        (int exit, string output, string errors) = Shell.Run("dotnet", Root, environment,
            [Path.Combine(Root, Tests.Output, Tests.Assembly + ".dll"), "-class", qualifiedClass], Shell.Timeout);
        HashSet<string> failed = [];
        Regex name = new(Regex.Escape(testClass) + @"\.(\w+)");
        foreach (string line in (output + errors).ReplaceLineEndings("\n").Split('\n'))
        {
            if ((line.Contains("[FAIL]", StringComparison.Ordinal) || line.Trim().StartsWith("failed ", StringComparison.Ordinal))
                && name.Match(line) is { Success: true } match)
            {
                failed.Add(match.Groups[1].Value);
            }
        }

        // xUnit's summary line: "XamlQuality.Tests  Total: 8, Errors: 0, Failed: 0, ...".
        MatchCollection totals = Regex.Matches(output + errors, @"Total:\s*(\d+)");
        int total = totals.Count > 0 ? int.Parse(totals[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        return (failed, exit, total);
    }

    private string ClassFile()
    {
        string[] found =
        [
            .. Directory.EnumerateFiles(Path.Combine(Root, "tests"), testClass + ".cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(Root, path).Replace('\\', '/').Split('/').Any(part => part is "bin" or "obj")),
        ];

        return found.Length switch
        {
            1 => found[0],
            0 => throw new InvalidOperationException($"no file under tests/ is called {testClass}.cs."),
            _ => throw new InvalidOperationException($"{found.Length} files under tests/ are called {testClass}.cs, so the test project is not known."),
        };
    }

    private string QualifiedClass() =>
        Regex.Match(File.ReadAllText(ClassFile()), @"^namespace\s+([\w.]+)\s*;", RegexOptions.Multiline) is { Success: true } match
            ? match.Groups[1].Value + "." + testClass
            : throw new InvalidOperationException($"{testClass}.cs declares no file-scoped namespace.");
}

/// <summary>Python's repr of a list of strings, so the output reads as pmutate_fast.py's did.</summary>
static class Py
{
    public static string List(IEnumerable<string> items) => "[" + string.Join(", ", items.Select(Repr)) + "]";

    private static string Repr(string text)
    {
        char quote = text.Contains('\'') && !text.Contains('"') ? '"' : '\'';
        StringBuilder repr = new();
        repr.Append(quote);
        foreach (char c in text)
        {
            repr.Append(c switch
            {
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when c == quote => "\\" + c,
                _ when c < ' ' || c == '\x7f' => $"\\x{(int)c:x2}",
                _ => c.ToString(),
            });
        }

        repr.Append(quote);
        return repr.ToString();
    }
}

static class Shell
{
    public const int TimedOut = int.MinValue;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(900);
    private static readonly ConcurrentDictionary<int, Process> Running = new();

    public static (int Exit, string Output, string Errors) Run(string file, string directory,
        IReadOnlyDictionary<string, string>? environment, IReadOnlyList<string> arguments, TimeSpan timeout)
    {
        ProcessStartInfo start = new(file)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach ((string name, string value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[name] = value;
        }

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        Running[process.Id] = process;
        try
        {
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeout))
            {
                Stop(process);
                process.WaitForExit();
                return (TimedOut, output.Result, errors.Result);
            }

            // The second wait lets the redirected streams drain.
            process.WaitForExit();
            return (process.ExitCode, output.Result, errors.Result);
        }
        finally
        {
            Running.TryRemove(process.Id, out _);
        }
    }

    /// <summary>Stops every process a run started, children included.</summary>
    public static void StopAll()
    {
        foreach (Process process in Running.Values)
        {
            Stop(process);
        }
    }

    public static string Tail(string text) =>
        string.Join('\n', text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').TakeLast(40));

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // It ended on its own between the check and the kill.
        }
    }
}
