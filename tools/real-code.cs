#!/usr/bin/env dotnet
// The real-code comparison of plans/00001: every rule in this tree's library, run over consumers'
// markup and build output at the commits tools/real-code/inputs.json pins, and compared with the
// record of what main printed over each, tools/real-code/expected/<input>.txt.
//
// ⭐ WHY A RECORD. A change that alters what a rule says about real code then shows it in its own
// diff, where its reviewer reads it, and CI's `Verification tools` job holds every other pull
// request to the record. A finding that appears is a claim about someone else's code: read it before
// writing it into the record.
//
// ⭐ PINNED BY COMMIT, NOT STORED. Each input is fetched at its commit, or taken from a local clone
// given with --from, and built once into a cache outside the repository, which is reused while the
// input's commit and project stay the same. Nothing of another repository is committed here.
//
// Usage, from anywhere in the repository:
//   dotnet run --file tools/real-code.cs                      compare every input with its record
//   dotnet run --file tools/real-code.cs -- --update          write this tree's output as the records
//   dotnet run --file tools/real-code.cs -- --from NAME=DIR   take the repository whose URL ends in
//                                                             NAME, ".git" aside, from the clone in DIR
//
//   XQ_REAL_CODE_CACHE   where the inputs are fetched and built; by default, under the user's local
//                        application data
//
// Exit codes: 0 every output equals its record, or --update wrote it; 1 an output differs from its
// record, or the runner failed; 2 an input could not be prepared, or a usage error.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

bool update = false;
Dictionary<string, string> clones = new(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--update")
    {
        update = true;
    }
    else if (args[i] == "--from" && i + 1 < args.Length && args[i + 1].Split('=', 2) is [{ Length: > 0 } name, { Length: > 0 } directory])
    {
        clones[name] = Path.GetFullPath(directory);
        i++;
    }
    else
    {
        return Usage($"unknown or incomplete argument '{args[i]}'.");
    }
}

(int topExit, string top, _) = Shell.Run("git", Directory.GetCurrentDirectory(), "rev-parse", "--show-toplevel");
if (topExit != 0)
{
    return Usage("run it from inside the repository.");
}

string root = Path.GetFullPath(top.Trim());
string manifest = Path.Combine(root, "tools", "real-code", "inputs.json");
string records = Path.Combine(root, "tools", "real-code", "expected");
string work = Path.Combine(root, "artifacts", "real-code");
string cache = Environment.GetEnvironmentVariable("XQ_REAL_CODE_CACHE") is { Length: > 0 } configured
    ? Path.GetFullPath(configured)
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bennewitz.Ninja.XamlQuality", "real-code");

Input[] inputs;
try
{
    inputs = Input.ReadAll(manifest);
}
catch (Exception error) when (error is IOException or JsonException or InvalidDataException)
{
    return Usage($"{Relative(root, manifest)}: {error.Message}");
}

if (clones.Keys.FirstOrDefault(name => !inputs.Any(input => string.Equals(input.RepositoryName, name, StringComparison.OrdinalIgnoreCase))) is { } stray)
{
    return Usage($"--from names '{stray}', and no input's repository is called that.");
}

Stopwatch clock = Stopwatch.StartNew();
foreach (Input input in inputs)
{
    string folder = Path.Combine(cache, input.Name, input.Commit);
    bool cached = Inputs.IsPrepared(folder, input);
    if (!cached && Inputs.Prepare(folder, input, clones.GetValueOrDefault(input.RepositoryName)) is { } failure)
    {
        Console.Error.WriteLine($"real-code: {input.Name}: {failure}");
        return 2;
    }

    Console.WriteLine($"{input.Name}: {(cached ? "cached" : "fetched and built")} at {input.Commit[..7]} ({clock.Elapsed.TotalSeconds:F0} s)");
}

Directory.CreateDirectory(work);
string runner = Path.Combine(work, "runner");
(int buildExit, string build, string buildErrors) = Shell.Run("dotnet", root,
    "build", Path.Combine(root, "tools", "Runner", "Runner.csproj"), "-c", "Release", "-o", runner, "-nologo", "-v:q");
if (buildExit != 0)
{
    Console.Error.WriteLine("real-code: the runner did not build:\n" + Shell.Tail(build + buildErrors));
    return 1;
}

int failed = 0;
StringBuilder summary = new("| Input | Commit | Result |\n|---|---|---|\n");
foreach (Input input in inputs)
{
    string folder = Path.Combine(cache, input.Name, input.Commit);
    (int exit, string output, string errors) = Shell.Run("dotnet", root,
        [Path.Combine(runner, "Runner.dll"), "all", Path.Combine(folder, "src", input.Markup), Path.Combine(folder, "bin"), .. input.Assemblies]);
    string[] lines = output.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
    if (exit != 0 || !lines[0].StartsWith("library ", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"real-code: {input.Name}: the runner exited {exit}:\n" + Shell.Tail(output + errors));
        summary.Append($"| {input.Name} | `{input.Commit[..7]}` | the runner failed |\n");
        failed++;
        continue;
    }

    // The first line names the library by its hash, which differs from build to build.
    string actual = string.Join('\n', lines.Skip(1)) + "\n";
    string actualPath = Path.Combine(work, input.Name + ".txt");
    File.WriteAllText(actualPath, actual);
    string recordPath = Path.Combine(records, input.Name + ".txt");
    string? record = File.Exists(recordPath) ? File.ReadAllText(recordPath).ReplaceLineEndings("\n") : null;

    string result;
    if (record == actual)
    {
        result = "same as its record";
    }
    else if (update)
    {
        Directory.CreateDirectory(records);
        File.WriteAllText(recordPath, actual);
        result = record is null ? "record written" : "record rewritten";
    }
    else
    {
        failed++;
        result = record is null ? "no record: read the output, then run with --update" : "differs from its record";
        if (record is not null)
        {
            (_, string diff, _) = Shell.Run("git", root, "diff", "--no-index", "--no-color", "--", Relative(root, recordPath), Relative(root, actualPath));
            Console.WriteLine(diff.TrimEnd());
        }
    }

    Console.WriteLine($"{input.Name}: {result} ({lines[0]})");
    summary.Append($"| {input.Name} | `{input.Commit[..7]}` | {result} |\n");
}

if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summaryFile)
{
    File.AppendAllText(summaryFile, "### Real-code comparison\n\n" + summary + "\n");
}

Console.WriteLine(failed == 0
    ? $"real-code: every input's output is its record ({clock.Elapsed.TotalSeconds:F0} s)"
    : $"real-code: {failed} of {inputs.Length} inputs differ from their records ({clock.Elapsed.TotalSeconds:F0} s)");
return failed == 0 ? 0 : 1;

static int Usage(string problem)
{
    Console.Error.WriteLine("real-code: " + problem);
    Console.Error.WriteLine("usage: dotnet run --file tools/real-code.cs [-- [--update] [--from NAME=DIR]...]");
    return 2;
}

static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

/// <summary>One consumer the rules are run over: its markup at a commit, and what it builds there.</summary>
sealed record Input(string Name, string Repository, string Commit, string Markup, string Project, string[] Assemblies)
{
    /// <summary>The repository as its URL ends, ".git" aside: the name --from takes.</summary>
    public string RepositoryName
    {
        get
        {
            string url = Repository.TrimEnd('/');
            string last = url[(url.LastIndexOf('/') + 1)..];
            return last.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? last[..^4] : last;
        }
    }

    // ⚠ Read through JsonDocument, not the serializer: a file-based app is analysed for native AOT,
    // which rejects reflection-based deserialization, and warnings are errors here.
    public static Input[] ReadAll(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("inputs", out JsonElement entries) || entries.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("it lists no inputs.");
        }

        List<Input> inputs = [];
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            string? name = Text(entry, "name");
            string? repository = Text(entry, "repository");
            string? commit = Text(entry, "commit");
            string? markup = Text(entry, "markup");
            string? project = Text(entry, "project");
            string[]? assemblies = entry.TryGetProperty("assemblies", out JsonElement files) && files.ValueKind == JsonValueKind.Array
                ? [.. files.EnumerateArray().Select(file => file.ValueKind == JsonValueKind.String ? file.GetString() ?? "" : "")]
                : null;
            if (name is null || repository is null || commit is null || markup is null || project is null || assemblies is not { Length: > 0 })
            {
                throw new InvalidDataException($"{name ?? "an input"} needs a name, repository, commit, markup, project and assemblies.");
            }

            // The name is a folder of the cache and a file of the records.
            if (!Regex.IsMatch(name, "^[a-z0-9][a-z0-9-]*$") || inputs.Any(input => input.Name == name))
            {
                throw new InvalidDataException($"'{name}' is not a unique name of lower-case letters, digits and dashes.");
            }

            if (!Regex.IsMatch(commit, "^[0-9a-f]{40}$"))
            {
                throw new InvalidDataException($"{name}: a commit is pinned by its full hash, not '{commit}'.");
            }

            if (!repository.StartsWith("https://", StringComparison.Ordinal)
                || !IsInside(markup) || !IsInside(project)
                || assemblies.Any(file => file.Length == 0 || file.Contains('/') || file.Contains('\\')))
            {
                throw new InvalidDataException($"{name}: the repository is an https URL, the markup and project paths stay inside it, written with '/', and each assembly is a file name.");
            }

            inputs.Add(new Input(name, repository, commit, markup, project, assemblies));
        }

        return [.. inputs];
    }

    private static string? Text(JsonElement entry, string property) =>
        entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool IsInside(string path) =>
        path.Length > 0 && !path.Contains('\\') && !Path.IsPathRooted(path) && !path.Split('/').Contains("..");
}

static class Inputs
{
    // ⛔ A library's build output holds its own assemblies and none of its packages', so every type the
    // rules read would fail to load and be named in Skipped: a scan that checks nothing and looks
    // clean. An application copies its packages already; this makes a library do the same.
    private static readonly string[] BuildProperties = ["-p:CopyLocalLockFileAssemblies=true"];

    /// <summary>
    /// The stamp names what was built and how, so a changed project, or a changed way of building it,
    /// is built again at the same commit.
    /// </summary>
    private static string Stamp(Input input) => string.Join(' ', [input.Commit, input.Project, .. BuildProperties]);

    public static bool IsPrepared(string folder, Input input)
    {
        string stamp = Path.Combine(folder, "prepared");
        return File.Exists(stamp) && File.ReadAllText(stamp).Trim() == Stamp(input);
    }

    /// <summary>Fetches the input's commit and builds its project into the folder; null when it did.</summary>
    public static string? Prepare(string folder, Input input, string? clone)
    {
        // A folder without the stamp is a preparation that stopped part way, so it starts again.
        Shell.DeleteTree(folder);
        Directory.CreateDirectory(folder);
        string source = Path.Combine(folder, "src");

        string[][] steps = clone is not null
            ?
            [
                ["clone", "--quiet", "--no-checkout", "--shared", clone, source],
                ["-C", source, "checkout", "--quiet", "--detach", input.Commit],
            ]
            :
            [
                ["init", "--quiet", source],
                ["-C", source, "fetch", "--quiet", "--depth", "1", input.Repository, input.Commit],
                ["-C", source, "checkout", "--quiet", "--detach", "FETCH_HEAD"],
            ];
        foreach (string[] step in steps)
        {
            (int exit, string output, string errors) = Shell.Run("git", folder, step);
            if (exit != 0)
            {
                return $"git {string.Join(' ', step)} failed:\n" + Shell.Tail(output + errors);
            }
        }

        (_, string head, _) = Shell.Run("git", folder, "-C", source, "rev-parse", "HEAD");
        if (head.Trim() != input.Commit)
        {
            return $"the checkout is at '{head.Trim()}', not {input.Commit}.";
        }

        // ⚠ From the input's own folder, so its global.json chooses the SDK and its NuGet.config the feeds.
        string bin = Path.Combine(folder, "bin");
        (int buildExit, string build, string buildErrors) = Shell.Run("dotnet", source,
            ["build", Path.Combine(source, input.Project), "-c", "Release", "-o", bin, "-nologo", "-v:q", .. BuildProperties]);
        if (buildExit != 0)
        {
            return "the build failed:\n" + Shell.Tail(build + buildErrors);
        }

        if (input.Assemblies.FirstOrDefault(file => !File.Exists(Path.Combine(bin, file))) is { } missing)
        {
            return $"the build wrote no {missing}.";
        }

        File.WriteAllText(Path.Combine(folder, "prepared"), Stamp(input) + "\n");
        return null;
    }
}

static class Shell
{
    public static (int Exit, string Output, string Errors) Run(string file, string directory, params string[] arguments)
    {
        ProcessStartInfo start = new(file)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, output.Result, errors.Result);
    }

    public static string Tail(string text) =>
        string.Join('\n', text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').TakeLast(40));

    /// <summary>Deletes a folder that may hold a git checkout, whose pack files Windows marks read-only.</summary>
    public static void DeleteTree(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(folder, recursive: true);
    }
}
