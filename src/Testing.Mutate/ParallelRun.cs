// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>
/// --parallel: each set's mutations dealt into shards, run at once in worktrees, and each set judged as one
/// run. This checkout is read, never edited.
/// </summary>
internal static partial class ParallelRun
{
    public static int Run(string repo, IReadOnlyList<(string? File, Plan Plan, Declarations Declared)> sets, int count)
    {
        Worktrees.Sweep(repo, Console.Out);
        string tree = Source.Tree(repo);
        string root = Worktrees.NewRoot();
        string[] trees = [.. Enumerable.Range(1, count).Select(k => Path.Combine(root, k.ToString(CultureInfo.InvariantCulture)))];
        string Result(int s, int k) => Path.Combine(root, $"set-{(s + 1).ToString(CultureInfo.InvariantCulture)}-shard-{(k + 1).ToString(CultureInfo.InvariantCulture)}.tsv");
        string Log(int s, int k) => Path.ChangeExtension(Result(s, k), ".log");

        (string self, string[] launcher) = Self.Binary();
        int total = sets.Sum(s => s.Plan.Mutations.Count);
        Console.WriteLine($"--parallel {count.ToString(CultureInfo.InvariantCulture)}: {total.ToString(CultureInfo.InvariantCulture)} mutation(s) in "
            + $"{sets.Count.ToString(CultureInfo.InvariantCulture)} set(s), over this checkout as tree {tree}, in worktrees under {root}");
        Console.WriteLine("  if this is killed, the next run removes the worktrees it leaves");

        Process?[] running = new Process?[count];
        int[] onSet = new int[count];
        int[] done = new int[count];
        int[,] codes = new int[sets.Count, count];
        bool interrupted = false;
        void Stop(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Volatile.Write(ref interrupted, true);
        }

        Console.CancelKeyPress += Stop;
        Stopwatch clock = Stopwatch.StartNew();
        try
        {
            Directory.CreateDirectory(root);
            for (int k = 0; k < count; k++)
            {
                if (Worktrees.Cut(repo, trees[k], tree) is { } refusal)
                {
                    Console.Error.WriteLine($"mutate: worktree {(k + 1).ToString(CultureInfo.InvariantCulture)}: {refusal}");
                    return 2;
                }
            }

            Task[] workers = [.. Enumerable.Range(0, count).Select(k => Task.Run(() =>
            {
                for (int s = 0; s < sets.Count && !Volatile.Read(ref interrupted); s++)
                {
                    Volatile.Write(ref onSet[k], s);
                    Volatile.Write(ref done[k], 0);
                    ProcessStartInfo info = new(self)
                    {
                        UseShellExecute = false,
                        WorkingDirectory = trees[k],
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    string[] setArgument = sets[s].File is { } file ? ["--set", file] : [];
                    foreach (string argument in (string[])[.. launcher, .. setArgument, .. Options.Forwarded, "--shard", $"{(k + 1).ToString(CultureInfo.InvariantCulture)}/{count.ToString(CultureInfo.InvariantCulture)}", "--emit", Result(s, k)])
                    {
                        info.ArgumentList.Add(argument);
                    }

                    // The worktree this shard may edit, and no other (Worktrees.Marker), named as git names it there.
                    Worktrees.Mark(info.Environment, trees[k]);

                    using StreamWriter writer = new(Log(s, k)) { AutoFlush = true };
                    using Process process = new() { StartInfo = info };
                    int worker = k;
                    void Record(object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data is not { } line)
                        {
                            return;
                        }

                        lock (writer)
                        {
                            writer.WriteLine(line);
                        }

                        if (Progress().IsMatch(line))
                        {
                            Interlocked.Increment(ref done[worker]);
                        }
                    }

                    // BNCQ1009 is suppressed for these two alone: Record writes the line to the shard's log under
                    // a lock and matches a regex against it, and neither throws on any line a shard prints.
#pragma warning disable BNCQ1009
                    process.OutputDataReceived += Record;
                    process.ErrorDataReceived += Record;
#pragma warning restore BNCQ1009
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    Volatile.Write(ref running[k], process);
                    process.WaitForExit();
                    codes[s, k] = process.ExitCode;
                    Volatile.Write(ref running[k], null);
                }
            }))];

            // ⭐ Prints as it goes, a line every half minute, so a long run reads as moving or stopped.
            TimeSpan printed = TimeSpan.Zero;
            while (!Task.WaitAll(workers, 2000))
            {
                if (Volatile.Read(ref interrupted))
                {
                    Console.Error.WriteLine("mutate: interrupted, so the shards are stopped and their worktrees removed");
                    return 2;
                }

                if (clock.Elapsed - printed >= TimeSpan.FromSeconds(30))
                {
                    printed = clock.Elapsed;
                    Console.WriteLine($"  {Minutes(clock.Elapsed)}   " + string.Join("   ", Enumerable.Range(0, count).Select(k =>
                    {
                        int s = Volatile.Read(ref onSet[k]);
                        int size = Shards.Of(sets[s].Plan.Mutations, k + 1, count).Count();
                        return $"shard {(k + 1).ToString(CultureInfo.InvariantCulture)}: set {(s + 1).ToString(CultureInfo.InvariantCulture)}/"
                            + $"{sets.Count.ToString(CultureInfo.InvariantCulture)} {Volatile.Read(ref done[k]).ToString(CultureInfo.InvariantCulture)}/{size.ToString(CultureInfo.InvariantCulture)}";
                    })));
                }
            }

            Console.WriteLine($"all {count.ToString(CultureInfo.InvariantCulture)} shard(s) finished in {Minutes(clock.Elapsed)}");

            int worst = 0;
            for (int s = 0; s < sets.Count; s++)
            {
                Console.WriteLine();
                Console.WriteLine($"=== {Shards.Name(sets[s].File)}");
                bool usable = true;
                for (int k = 0; k < count; k++)
                {
                    // 0 and 1 are both results — a shard judges its own share's verdicts and never the set's
                    // tests — and the merge reads either. Anything else is the harness refusing or failing.
                    if (codes[s, k] is not (0 or 1) || !File.Exists(Result(s, k)))
                    {
                        usable = false;
                        Console.Error.WriteLine($"shard {(k + 1).ToString(CultureInfo.InvariantCulture)}/{count.ToString(CultureInfo.InvariantCulture)}: "
                            + $"exit {codes[s, k].ToString(CultureInfo.InvariantCulture)}, NO RESULT; its log is {Log(s, k)}");
                    }
                }

                int code = usable
                    ? Shards.JudgeShards(sets[s].File, sets[s].Plan, sets[s].Declared, [.. Enumerable.Range(0, count).Select(k => Result(s, k))], tree)
                    : 2;
                Console.WriteLine($"=== exit {code.ToString(CultureInfo.InvariantCulture)}");
                worst = Math.Max(worst, code);
            }

            Console.WriteLine();
            Console.WriteLine($"{sets.Count.ToString(CultureInfo.InvariantCulture)} set(s) in {count.ToString(CultureInfo.InvariantCulture)} worktree(s), "
                + $"{Minutes(clock.Elapsed)}; each shard's log and result are kept in {root}");
            return worst;
        }
        finally
        {
            Console.CancelKeyPress -= Stop;
            foreach (Process? process in running)
            {
                try
                {
                    if (process is { HasExited: false })
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit();
                    }
                }
                catch (InvalidOperationException)
                {
                    // Exited and disposed between the read and the kill.
                }
            }

            string[] left = [.. trees.Where(Directory.Exists).Where(t => !Worktrees.Remove(repo, t))];
            Shell.Run(repo, "git", ["worktree", "prune"]);
            if (left.Length > 0)
            {
                Console.Error.WriteLine($"mutate: {left.Length.ToString(CultureInfo.InvariantCulture)} worktree(s) would not delete, and are left:");
                foreach (string t in left)
                {
                    Console.Error.WriteLine("  " + t);
                }

                Console.Error.WriteLine("Something still holds a file in each; the next run removes them once it lets go.");
            }
        }
    }

    private static string Minutes(TimeSpan elapsed) => elapsed.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + " min";

    /// <summary>A shard's progress line, one per mutation it has run.</summary>
    [GeneratedRegex(@"^\[\d+/\d+\]")]
    private static partial Regex Progress();
}
