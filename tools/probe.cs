#!/usr/bin/env dotnet
// The headless probe of plans/00001: the cases in tools/probe/Probe, lists, combos and tab controls the
// reviews of BNXQ1005, BNXQ1009 and BNXQ1010 wrote, measured under Avalonia's Fluent, Simple and Semi
// themes, dumped as markup, read by those three rules in this tree's library, and each rule's verdict
// judged against what was measured. A port of run-probe.ps1 and tabulate16fix.py, their judgement kept.
//
// ⭐ TWO PASSES, EACH UNDER EVERY THEME, and no window anyone sees. The facts pass ("facts11") gives a
// case's list its items in code and reads each of the first three rows on a fresh window: the
// Background it starts with, and what in its template draws it. The bound pass ("diag13") gives the
// markup a data context its own bindings read, sets nothing in code, and reads where the rows came
// from, what takes focus, whether F5 runs a key binding's command, and the name each row's automation
// peer reports.
//
// ⭐ A VERDICT IS JUDGED AS THE REVIEWS JUDGED IT:
//   BNXQ1010  a "can have a null Background" finding holds where, under every theme, some row starts
//             null and something in its template takes the row's Background; a "draw no background"
//             finding holds where every row draws nothing; a clean verdict is MISSED where either holds.
//   BNXQ1009  a finding holds where, under every theme, every row's peer reports its item's
//             ToString(), and that is object's or the compiler's, which name the item by its type,
//             not one its type writes; a clean verdict is MISSED where that is so.
//   BNXQ1005  a finding holds where, under every theme, nothing takes focus and F5 ran the command 0
//             times; a clean verdict is MISSED where that is so.
//   A skip claims nothing, and markup that does not load under some theme makes every verdict claim
//   nothing.
//
// Usage, from anywhere in the repository:
//   dotnet run --file tools/probe.cs -- <pattern>
//
//   <pattern> is a case's id, or ends in '*' to take every case whose id starts with what precedes it,
//   as the probe itself reads it; '*' alone, quoted in a shell that expands it, takes every case.
//
// It writes to artifacts/probe/: facts.txt and bound.txt, the two passes; cases/, each case's markup as
// the rules read it; rules.txt, what the rules print over each case; and table.txt, every verdict
// beside what was measured, then the counts and the verdicts the measurements refute.
//
// Exit codes: 0 no verdict is FALSE or MISSED; 1 one is; 2 a usage error, or a build, the probe or the
// runner failed.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

if (args is not [{ Length: > 0 } pattern] || !Regex.IsMatch(pattern, @"^\w*\*?$"))
{
    return Usage("name one case, or a prefix of ids ending in '*'.");
}

(int topExit, string top, _) = Shell.Run("git", Directory.GetCurrentDirectory(), null, ["rev-parse", "--show-toplevel"]);
if (topExit != 0)
{
    return Usage("run it from inside the repository.");
}

string root = Path.GetFullPath(top.Trim());
string work = Path.Combine(root, "artifacts", "probe");
string probeOut = Path.Combine(work, "out");
string runnerOut = Path.Combine(work, "runner");
string cases = Path.Combine(work, "cases");
string dump = Path.Combine(work, "dump");

Console.CancelKeyPress += (_, press) =>
{
    // Stop what is running; the step it was in then fails, and nothing more starts.
    press.Cancel = true;
    Shell.StopAll();
};

// ⛔ Avalonia's build telemetry starts a process that outlives the build, and another project's build
// is not this repository's to report: AVALONIA_TELEMETRY_OPTOUT is what Avalonia.BuildServices reads.
Dictionary<string, string?> building = new() { ["AVALONIA_TELEMETRY_OPTOUT"] = "1" };

// ⚠ Two reviews' cases hold a class constructor that fails unless its review's switch is set, as one
// that needs what a scan's process lacks does. The passes set the switches as each review measured its
// cases, judge 16's set and judge 15's unset, so no measurement moves from the one its review judged;
// the rules run with neither. Either way, whatever the shell that started this one holds.
Dictionary<string, string?> measuring = new() { ["J15_CCTOR_OK"] = null, ["J16_CCTOR_OK"] = "1" };
Dictionary<string, string?> scanning = new() { ["J15_CCTOR_OK"] = null, ["J16_CCTOR_OK"] = null };

Stopwatch clock = Stopwatch.StartNew();
(string Project, string Output)[] builds =
[
    (Path.Combine(root, "tools", "probe", "Probe", "Probe.csproj"), probeOut),
    (Path.Combine(root, "tools", "Runner", "Runner.csproj"), runnerOut),
];
foreach ((string project, string output) in builds)
{
    (int exit, string text, string errors) = Shell.Run("dotnet", root, building,
        ["build", project, "-c", "Release", "-o", output, "-nologo", "-v:q", "-nodeReuse:false"]);
    if (exit != 0)
    {
        return Failed($"{Path.GetFileName(project)} did not build", text + errors);
    }
}

Console.WriteLine($"probe: built ({clock.Elapsed.TotalSeconds:F0} s)");
string probe = Path.Combine(probeOut, "FpProbe6.dll");

Shell.DeleteTree(dump);
Shell.DeleteTree(cases);
(int dumpExit, string dumped, string dumpErrors) = Shell.Run("dotnet", root, null, [probe, "dump", dump]);
if (dumpExit != 0)
{
    return Failed("the probe did not dump its cases", dumped + dumpErrors);
}

// The ids the probe itself takes the pattern to name, so that the rules read the cases it measures.
string[] ids =
[
    .. Directory.GetDirectories(dump)
        .Select(folder => Path.GetFileName(folder))
        .Where(id => id == pattern || (pattern.EndsWith('*') && id.StartsWith(pattern.TrimEnd('*'), StringComparison.Ordinal)))
        .Order(StringComparer.Ordinal),
];
Directory.CreateDirectory(cases);
foreach (string id in ids)
{
    Directory.Move(Path.Combine(dump, id), Path.Combine(cases, id));
}

Shell.DeleteTree(dump);
if (ids.Length == 0)
{
    return Usage($"no case's id is '{pattern}'.");
}

Console.WriteLine($"probe: {ids.Length} case{(ids.Length == 1 ? "" : "s")} match '{pattern}'");

Dictionary<string, string> passes = [];
(string Pass, string Mode)[] modes = [("facts", "facts11"), ("bound", "diag13")];
foreach ((string pass, string mode) in modes)
{
    StringBuilder text = new();
    foreach (string theme in Judge.Themes)
    {
        (int exit, string output, string errors) = Shell.Run("dotnet", root, measuring, [probe, theme, mode, pattern]);
        if (exit != 0)
        {
            return Failed($"the {pass} pass under {theme} exited {exit}", output + errors);
        }

        text.Append($"== {theme}\n").Append(Lines(output)).Append(Lines(errors));
    }

    passes[pass] = text.ToString();
    File.WriteAllText(Path.Combine(work, pass + ".txt"), passes[pass]);
    Console.WriteLine($"probe: the {pass} pass ({clock.Elapsed.TotalSeconds:F0} s)");
}

// Every case in one process, in the order of the reviews' records: BNXQ1010, BNXQ1009, BNXQ1005.
(int runExit, string verdicts, string runErrors) = Shell.Run("dotnet", root, scanning,
    [Path.Combine(runnerOut, "Runner.dll"), "BNXQ1010,BNXQ1009,BNXQ1005", "--each", cases, probeOut, "FpProbe6.dll"]);
if (runExit != 0 || !verdicts.StartsWith("library ", StringComparison.Ordinal))
{
    return Failed($"the runner exited {runExit}", verdicts + runErrors);
}

File.WriteAllText(Path.Combine(work, "rules.txt"), Lines(verdicts));
Console.WriteLine($"probe: the rules ({clock.Elapsed.TotalSeconds:F0} s)");

(string table, string[] counts, string[] flagged) = Judge.Tabulate(passes["facts"], passes["bound"], verdicts, ids);
string tablePath = Path.Combine(work, "table.txt");
File.WriteAllText(tablePath, table);
foreach (string line in counts)
{
    Console.WriteLine(line);
}

Console.WriteLine($"flagged: {flagged.Length}");
foreach (string line in flagged)
{
    Console.WriteLine("  " + line);
}

Console.WriteLine($"probe: {Path.GetRelativePath(root, tablePath).Replace('\\', '/')} written ({clock.Elapsed.TotalSeconds:F0} s)");
return flagged.Length == 0 ? 0 : 1;

static string Lines(string text)
{
    string lines = text.ReplaceLineEndings("\n");
    return lines.Length == 0 || lines.EndsWith('\n') ? lines : lines + "\n";
}

static int Usage(string problem)
{
    Console.Error.WriteLine("probe: " + problem);
    Console.Error.WriteLine("usage: dotnet run --file tools/probe.cs -- <case id | id prefix followed by *>");
    return 2;
}

static int Failed(string what, string output)
{
    Console.Error.WriteLine($"probe: {what}:\n" + Shell.Tail(output));
    return 2;
}

/// <summary>
/// The reviews' judgement, tabulate16fix.py's, ported line for line: its regular expressions, its order
/// and its text, but for the second build it set beside the one judged, which one tree does not have.
/// </summary>
static class Judge
{
    public static readonly string[] Themes = ["fluent", "simple", "semi"];

    private static readonly string[] Rules = ["BNXQ1010", "BNXQ1009", "BNXQ1005"];
    private static readonly Regex RowLine = new(@"^(\w+)\s+row(\d)\s+(.*)$");
    private static readonly Regex CaseLine = new(@"^(\w+)\s+(.*)$");
    private static readonly Regex CaseHeader = new(@"^\[(\w+)\]$");
    private static readonly Regex Counts = new(@"^(BNXQ10\d\d): inspected (\d+), findings (\d+), skipped (\d+)$");
    private static readonly Regex Start = new(@"start (\S+)");
    private static readonly Regex State = new(@"\[([^\]]*)\]");
    private static readonly Regex Peer = new(@"peer name ""([^""]*)"" \(item ToString ""([^""]*)""(?:, written by ([^)]*))?\)");
    private static readonly Regex Focus = new(@"focus: takes focus \[([^\]]*)\]");
    private static readonly Regex Runs = new(@"F5 ran the command (\d+) time");

    /// <summary>The table, each rule's counts, and the verdicts the measurements refute.</summary>
    public static (string Table, string[] Counts, string[] Flagged) Tabulate(string facts, string bound, string rules, string[] ids)
    {
        HashSet<string> known = [.. ids];
        Dictionary<string, Dictionary<string, List<string>>> factLines = PassLines(facts, known, rows: true);
        Dictionary<string, Dictionary<string, List<string>>> boundLines = PassLines(bound, known, rows: false);
        Dictionary<string, Dictionary<string, string>> verdicts = Verdicts(rules);

        List<string> table = [];
        List<string> counted = [];
        Dictionary<string, SortedDictionary<string, int>> counts = [];
        List<string> flagged = [];
        foreach (string id in verdicts.Keys.Order(StringComparer.Ordinal))
        {
            table.Add($"[{id}]");
            Dictionary<string, List<string>> factsOf = factLines.GetValueOrDefault(id) ?? [];
            Dictionary<string, List<string>> boundOf = boundLines.GetValueOrDefault(id) ?? [];
            foreach (string rule in Rules)
            {
                string verdict = verdicts[id].GetValueOrDefault(rule) ?? "?";
                (string result, string measured) = rule switch
                {
                    "BNXQ1010" => Judge1010(verdict, factsOf),
                    "BNXQ1009" => Judge1009(verdict, boundOf),
                    _ => Judge1005(verdict, boundOf),
                };
                if (result == "no key binding" || (rule != "BNXQ1010" && verdict == "not inspected"))
                {
                    continue;
                }

                if (!counts.TryGetValue(rule, out SortedDictionary<string, int>? by))
                {
                    counts[rule] = by = new SortedDictionary<string, int>(StringComparer.Ordinal);
                    counted.Add(rule);
                }

                by[result] = by.GetValueOrDefault(result) + 1;
                if (result.StartsWith("FALSE", StringComparison.Ordinal) || result == "MISSED")
                {
                    flagged.Add($"{id} {rule}: {verdict} -> {result}");
                }

                table.Add($"  {rule}: {verdict} -> {result}");
                table.Add($"    measured: {measured}");
            }
        }

        string[] countLines = [.. counted.Select(rule => $"{rule}: " + string.Join(", ", counts[rule].Select(pair => $"{pair.Key} {pair.Value}")))];
        table.Add("");
        table.Add("counts:");
        table.AddRange(countLines.Select(line => "  " + line));
        table.Add("flagged (FALSE or MISSED):");
        table.AddRange(flagged.Select(line => "  " + line));
        return (string.Join("\n", table) + "\n", countLines, [.. flagged]);
    }

    /// <summary>A pass's lines by case and theme: what follows a case's id, and in the facts pass what follows "rowN".</summary>
    private static Dictionary<string, Dictionary<string, List<string>>> PassLines(string text, HashSet<string> known, bool rows)
    {
        Dictionary<string, Dictionary<string, List<string>>> lines = [];
        string? theme = null;
        foreach (string line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("== ", StringComparison.Ordinal))
            {
                theme = line[3..].Trim();
                continue;
            }

            if (theme is null)
            {
                continue;
            }

            Match match = rows ? RowLine.Match(line) : Match.Empty;
            int group = 3;
            if (!match.Success || !known.Contains(match.Groups[1].Value))
            {
                match = CaseLine.Match(line);
                group = 2;
            }

            if (!match.Success || !known.Contains(match.Groups[1].Value))
            {
                continue;
            }

            string id = match.Groups[1].Value;
            if (!lines.TryGetValue(id, out Dictionary<string, List<string>>? byTheme))
            {
                lines[id] = byTheme = [];
            }

            if (!byTheme.TryGetValue(theme, out List<string>? list))
            {
                byTheme[theme] = list = [];
            }

            list.Add(match.Groups[group].Value);
        }

        return lines;
    }

    /// <summary>Each case's verdict by each rule: skip, finding (for BNXQ1010, C1 or C2), clean or not inspected.</summary>
    private static Dictionary<string, Dictionary<string, string>> Verdicts(string text)
    {
        Dictionary<string, Dictionary<string, string>> verdicts = [];
        string? id = null;
        string? current = null;
        foreach (string line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            Match header = CaseHeader.Match(line);
            if (header.Success)
            {
                id = header.Groups[1].Value;
                verdicts[id] = [];
                continue;
            }

            Match counts = Counts.Match(line.Trim());
            if (counts.Success && id is { Length: > 0 })
            {
                current = counts.Groups[1].Value;
                int inspected = int.Parse(counts.Groups[2].Value);
                int findings = int.Parse(counts.Groups[3].Value);
                int skipped = int.Parse(counts.Groups[4].Value);
                verdicts[id][current] = skipped > 0 ? "skip" : findings > 0 ? "finding" : inspected > 0 ? "clean" : "not inspected";
                continue;
            }

            // ⚠ As the reviews read it: a BNXQ1010 finding names its kind, over a skip in the same result.
            if (id is { Length: > 0 } && current == "BNXQ1010" && line.Trim().StartsWith("finding ", StringComparison.Ordinal))
            {
                string finding = line.Trim();
                verdicts[id][current] = finding.Contains("can have a null Background", StringComparison.Ordinal) ? "C1"
                    : finding.Contains("draw no background", StringComparison.Ordinal) ? "C2"
                    : "finding";
            }
        }

        return verdicts;
    }

    private static bool DoesNotLoad(List<string> lines) =>
        lines.Any(line => (" " + line).Contains(" threw ", StringComparison.Ordinal)
                          || line.StartsWith("threw", StringComparison.Ordinal)
                          || line.StartsWith("load threw", StringComparison.Ordinal));

    private static (string Result, string Measured) Judge1010(string verdict, Dictionary<string, List<string>> facts)
    {
        List<string> summary = [];
        bool c1All = true;
        bool c2All = true;
        bool loads = true;
        foreach (string theme in Themes)
        {
            List<string> rows = facts.GetValueOrDefault(theme) ?? [];
            if (rows.Count == 0 || DoesNotLoad(rows))
            {
                loads = false;
                summary.Add($"{theme}: does not load");
                continue;
            }

            rows = [.. rows.Where(row => Start.IsMatch(row))];
            if (rows.Count == 0)
            {
                loads = false;
                summary.Add($"{theme}: no rows");
                continue;
            }

            bool[] c1 = [.. rows.Select(row => row.Contains("C1 yes", StringComparison.Ordinal))];
            bool[] c2 = [.. rows.Select(row => row.Contains("C2 yes", StringComparison.Ordinal))];
            string[] starts = [.. rows.Select(row => Start.Match(row).Groups[1].Value)];
            string[] states = [.. rows.Select(row => State.Match(row).Groups[1].Value)];
            summary.Add($"{theme}: start {string.Join('/', starts)} C1 {string.Concat(c1.Select(x => x ? 'y' : 'n'))} "
                        + $"C2 {string.Concat(c2.Select(x => x ? 'y' : 'n'))} [{string.Join('/', states.Select(state => state.Length > 0 ? state : "-"))}]");
            c1All = c1All && c1.Any(x => x);
            c2All = c2All && c2.All(x => x);
        }

        string measured = string.Join("; ", summary);
        return !loads ? ("does not load", measured)
            : verdict == "skip" ? ("skip", measured)
            : verdict == "C1" ? (c1All ? "holds" : "FALSE C1", measured)
            : verdict == "C2" ? (c2All ? "holds" : "FALSE C2", measured)
            : verdict == "clean" ? (c1All || c2All ? "MISSED" : "agrees", measured)
            : (verdict, measured);
    }

    private static (string Result, string Measured) Judge1009(string verdict, Dictionary<string, List<string>> bound)
    {
        List<string> summary = [];
        bool toStringAll = true;
        bool loads = true;
        foreach (string theme in Themes)
        {
            List<string> lines = bound.GetValueOrDefault(theme) ?? [];
            if (lines.Count == 0 || DoesNotLoad(lines))
            {
                loads = false;
                summary.Add($"{theme}: does not load");
                continue;
            }

            // A ToString() the item's type writes names it by what it is, as a string or an enum's member does.
            Match[] peers = Peer.Matches(string.Join("\n", lines)).ToArray();
            summary.Add($"{theme}: peers {string.Join('/', peers.Select(peer => peer.Groups[1].Value))}");
            toStringAll = toStringAll && peers.Length > 0
                          && peers.All(peer => peer.Groups[1].Value == peer.Groups[2].Value && peer.Groups[3].Value != "its type");
        }

        string measured = string.Join("; ", summary);
        return !loads ? ("does not load", measured)
            : verdict == "skip" ? ("skip", measured)
            : verdict == "finding" ? (toStringAll ? "holds" : "FALSE", measured)
            : verdict == "clean" ? (toStringAll ? "MISSED" : "agrees", measured)
            : (verdict, measured);
    }

    private static (string Result, string Measured) Judge1005(string verdict, Dictionary<string, List<string>> bound)
    {
        List<string> summary = [];
        bool deadAll = true;
        bool hasBinding = false;
        bool loads = true;
        foreach (string theme in Themes)
        {
            List<string> lines = bound.GetValueOrDefault(theme) ?? [];
            if (lines.Count == 0 || DoesNotLoad(lines))
            {
                loads = false;
                summary.Add($"{theme}: does not load");
                continue;
            }

            string text = string.Join("\n", lines);
            Match focus = Focus.Match(text);
            Match runs = Runs.Match(text);
            hasBinding = hasBinding || runs.Success;
            summary.Add($"{theme}: focus [{(focus.Success ? focus.Groups[1].Value : "?")}] F5 {(runs.Success ? runs.Groups[1].Value : "-")}");
            deadAll = deadAll && runs.Success && runs.Groups[1].Value == "0" && focus.Success && focus.Groups[1].Value.Length == 0;
        }

        string measured = string.Join("; ", summary);
        return !loads ? ("does not load", measured)
            : !hasBinding ? ("no key binding", measured)
            : verdict == "skip" ? ("skip", measured)
            : verdict == "finding" ? (deadAll ? "holds" : "FALSE", measured)
            : verdict == "clean" ? (deadAll ? "MISSED" : "agrees", measured)
            : (verdict, measured);
    }
}

static class Shell
{
    // A pass over every case under one theme takes minutes; one that takes this long has hung.
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);
    private static readonly ConcurrentDictionary<int, Process> Running = new();

    /// <summary>Runs a process to its end; an environment value of null removes that variable.</summary>
    public static (int Exit, string Output, string Errors) Run(string file, string directory,
        IReadOnlyDictionary<string, string?>? environment, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo start = new(file)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach ((string name, string? value) in environment ?? new Dictionary<string, string?>())
        {
            if (value is null)
            {
                start.Environment.Remove(name);
            }
            else
            {
                start.Environment[name] = value;
            }
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
            if (!process.WaitForExit(Timeout))
            {
                Stop(process);
                process.WaitForExit();
                return (-1, output.Result, errors.Result + $"\nstopped after {Timeout.TotalMinutes:F0} minutes");
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

    /// <summary>Stops every process this run started, children included.</summary>
    public static void StopAll()
    {
        foreach (Process process in Running.Values)
        {
            Stop(process);
        }
    }

    public static string Tail(string text) =>
        string.Join('\n', text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').TakeLast(40));

    /// <summary>Deletes a folder, and any read-only file in it.</summary>
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
