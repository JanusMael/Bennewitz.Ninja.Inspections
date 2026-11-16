namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>
/// Where every mutation is planted: a detached worktree loaded with the checkout's state, never the checkout
/// itself (plans/00008, decision 2).
/// </summary>
/// <remarks>
/// <para>⭐ <b>The checkout is never edited.</b> A run killed while it edits a file cannot put the file back, and in
/// the checkout that loses whatever was uncommitted there. In a worktree it loses nothing: the next run removes the
/// worktree, once the process that made it is gone.</para>
/// <para>⭐ <b>One worktree is a run like any other.</b> Without <c>--parallel</c> this process cuts one worktree and
/// runs itself there with the arguments it was given, its output passing straight through, so <c>--only</c>, several
/// sets and every report read exactly as a run in place would. With it, <see cref="ParallelRun"/> deals the mutations
/// into shards, one worktree each.</para>
/// <para>⛔ <b>A run edits only where <see cref="Marker"/> names the checkout it is in.</b> This process sets it on
/// every run it starts in a worktree, and nothing else does, so a run in a person's own checkout, or in a worktree
/// they made, never edits it.</para>
/// </remarks>
internal static class Worktrees
{
    /// <summary>The environment variable naming the worktree a run was started in, by the run that cut it.</summary>
    public const string Marker = "BENNEWITZ_MUTATE_WORKTREE";

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>Whether <paramref name="repo"/> is a worktree a run of this tool cut for this process to edit.</summary>
    public static bool IsOurs(string repo) =>
        Environment.GetEnvironmentVariable(Marker) is { Length: > 0 } marked
        && string.Equals(Path.GetFullPath(marked).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(repo).TrimEnd(Path.DirectorySeparatorChar), PathComparison);

    /// <summary>Under the temp directory, where every run keeps its worktrees, each run in a folder of its own.</summary>
    public static string Home => Path.Combine(Path.GetTempPath(), "bennewitz-mutate", "worktrees");

    /// <summary>
    /// A folder for this run's worktrees, named by this process's id and start time, which is how <see cref="Sweep"/>
    /// tells a live run's from a dead one's even after the id is reused.
    /// </summary>
    public static string NewRoot()
    {
        using Process self = Process.GetCurrentProcess();
        string root = Path.Combine(Home, $"{Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}-{self.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)}");
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Removes every worktree of <paramref name="repo"/> under <see cref="Home"/> whose run is gone, and prunes. A run
    /// that was killed leaves its worktrees; the next run is what removes them. Answers how many it removed.
    /// </summary>
    public static int Sweep(string repo, TextWriter report)
    {
        Shell.Result listed = Shell.Run(repo, "git", ["worktree", "list", "--porcelain"]);
        if (listed.Exit != 0)
        {
            return 0;
        }

        string home = Path.GetFullPath(Home);
        int removed = 0;
        foreach (string line in listed.Stdout.Split('\n'))
        {
            if (!line.StartsWith("worktree ", StringComparison.Ordinal))
            {
                continue;
            }

            string tree = Path.GetFullPath(line["worktree ".Length..].Trim());
            if (!tree.StartsWith(home + Path.DirectorySeparatorChar, PathComparison))
            {
                continue;
            }

            string run = Path.GetRelativePath(home, tree).Split(Path.DirectorySeparatorChar)[0];
            if (IsLive(run))
            {
                continue;
            }

            report.WriteLine($"removing worktree {tree}, left by a run that is gone");
            if (Remove(repo, tree))
            {
                removed++;
            }
        }

        // ⭐ A dead run's folder goes whole, whichever repository its worktrees were of: they were disposable when it made
        // them, and one whose repository is gone is listed by no git at all. A repository still holding one finds it
        // missing and prunes it.
        Shell.Run(repo, "git", ["worktree", "prune"]);
        foreach (string folder in Directory.Exists(home) ? Directory.EnumerateDirectories(home) : [])
        {
            if (!IsLive(Path.GetFileName(folder)))
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // A shard's log still held; the next sweep tries again.
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Cuts a detached worktree from HEAD at <paramref name="path"/>, gives it <paramref name="tree"/>, and checks that
    /// it holds that tree. Answers why it could not, or null.
    /// </summary>
    public static string? Cut(string repo, string path, string tree)
    {
        Shell.Result cut = Shell.Run(repo, "git", ["worktree", "add", "--detach", path, "HEAD"]);
        if (cut.Exit != 0)
        {
            return $"could not cut a worktree at {path}: {cut.Output.Trim()}";
        }

        Source.Load(path, tree);

        // ⛔ The proof that the worktree is this checkout, before a minute is spent on it.
        string loaded = Source.Tree(path);
        return loaded == tree ? null : $"the worktree at {path} holds tree {loaded}, not this checkout's {tree}";
    }

    /// <summary>
    /// Runs this tool again in one worktree loaded with the checkout's state, with <paramref name="arguments"/>, its
    /// output passing straight through, and answers its exit code. The worktree is removed on every exit this
    /// process controls.
    /// </summary>
    public static int RunOne(string repo, IReadOnlyList<string> arguments)
    {
        Sweep(repo, Console.Out);
        string tree = Source.Tree(repo);
        string root = NewRoot();
        string path = Path.Combine(root, "1");
        Console.WriteLine($"in a worktree of this checkout as tree {tree}, at {path}; this checkout is not edited");
        try
        {
            if (Cut(repo, path, tree) is { } refusal)
            {
                Console.Error.WriteLine($"mutate: {refusal}");
                return 2;
            }

            (string self, string[] launcher) = Self.Binary();
            ProcessStartInfo start = new(self) { UseShellExecute = false, WorkingDirectory = path };
            foreach (string argument in (string[])[.. launcher, .. arguments])
            {
                start.ArgumentList.Add(argument);
            }

            start.Environment[Marker] = path;
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("could not start this tool again");
            void Stop(object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // It ended between the key and the kill.
                }
            }

            Console.CancelKeyPress += Stop;
            try
            {
                process.WaitForExit();
                return process.ExitCode;
            }
            finally
            {
                Console.CancelKeyPress -= Stop;
            }
        }
        finally
        {
            if (Directory.Exists(path) && !Remove(repo, path))
            {
                Console.Error.WriteLine($"mutate: the worktree {path} would not delete; the next run removes it");
            }

            Shell.Run(repo, "git", ["worktree", "prune"]);
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Left for the next run's sweep.
            }
        }
    }

    /// <summary>
    /// A build that has just finished can hold a file in its worktree a little longer, so this tries for about a
    /// minute, deleting the directory itself once git has given up on it.
    /// </summary>
    public static bool Remove(string repo, string tree)
    {
        Shell.Run(repo, "git", ["worktree", "remove", "--force", tree]);
        foreach (int seconds in (int[])[0, 2, 5, 10, 20, 30])
        {
            if (!Directory.Exists(tree))
            {
                return true;
            }

            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            try
            {
                Directory.Delete(tree, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Still held; the next pass waits longer.
            }
        }

        return !Directory.Exists(tree);
    }

    /// <summary>Whether the run a folder under <see cref="Home"/> is named for, <c>pid-ticks</c>, is still running.</summary>
    private static bool IsLive(string run)
    {
        string[] parts = run.Split('-');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int pid)
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks))
        {
            // Not a folder a run made: never ours to remove.
            return true;
        }

        try
        {
            using Process process = Process.GetProcessById(pid);
            return process.StartTime.ToUniversalTime().Ticks == ticks;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            // Its start time cannot be read: count it live rather than remove a running run's worktree.
            return true;
        }
    }
}
