// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

internal static class Git
{
    public static string Head(string repo) =>
        Shell.Run(repo, "git", ["rev-parse", "--short", "HEAD"]).Output.Trim();

    /// <summary>
    /// The whole repository's status, each entry prefixed by a hash of the file it names. The hash is
    /// what sees an already-modified file modified again — its status line does not move — and the
    /// prefix keeps the entry ending in its path, which is how the declared file is recognised.
    /// ⛔ `--untracked-files=all`, not the default: `status.showUntrackedFiles=no` in a user's config
    /// would otherwise hide exactly the file a stray write created.
    /// </summary>
    public static string Fingerprint(string repo)
    {
        string status = Shell.Run(repo, "git", ["status", "--porcelain", "--untracked-files=all"]).Output;
        StringBuilder lines = new();
        foreach (string raw in status.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string entry = raw.TrimEnd('\r');
            string file = Path.Combine(repo, entry.Length > 3 ? entry[3..].Trim('"') : string.Empty);
            string hash = File.Exists(file)
                ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)))[..12]
                : "------------";
            lines.Append(hash).Append(' ').Append(entry).Append('\n');
        }

        return lines.ToString();
    }

    /// <summary>
    /// Whatever appeared between two readings of the repository's status that the mutation did not
    /// declare. Pure, and separated from the reading so it can be proven: this is the one safety
    /// property this harness adds over the one-off it came from, and an untested guard is a claim.
    /// </summary>
    public static string Strayed(string before, string after, string declared)
    {
        HashSet<string> was = [.. before.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim())];
        List<string> strayed = [];
        foreach (string line in after.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string entry = line.Trim();

            // The declared file is matched at the end of the status entry, past its ` M ` marker —
            // and only the declared one, so a second file the edit touched is a stray even when the
            // mutation meant to touch it.
            if (!was.Contains(entry) && !entry.EndsWith(declared, StringComparison.Ordinal))
            {
                strayed.Add(entry);
            }
        }

        return string.Join(Environment.NewLine, strayed);
    }

    /// <summary>
    /// Every path a change since <paramref name="since"/> touches, committed or not: what
    /// <c>git diff --name-only</c> reports against that ref, and each untracked file the ignore rules let
    /// through. Paths as git names them, relative to the repository with <c>/</c>.
    /// </summary>
    public static IReadOnlyList<string> Changed(string repo, string since)
    {
        Shell.Result diff = Shell.Run(repo, "git", ["diff", "--name-only", since, "--"]);
        Shell.Result untracked = Shell.Run(repo, "git", ["ls-files", "--others", "--exclude-standard"]);
        if (diff.Exit != 0 || untracked.Exit != 0)
        {
            throw new InvalidDataException($"git cannot say what changed since `{since}`: {(diff.Exit != 0 ? diff.Output : untracked.Output).Trim()}");
        }

        return [.. (diff.Stdout + "\n" + untracked.Stdout).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// The packages a change since <paramref name="since"/> moved in the pins file at <paramref name="path"/>,
    /// relative to the repository with <c>/</c>: <see cref="Pin.Moved"/> between the file as git holds it at that
    /// ref and as it is now, uncommitted edits included. A file that did not exist at the ref pinned nothing then,
    /// and one that does not exist now pins nothing. Null when either side does not parse, which owes the run
    /// rather than guessing.
    /// </summary>
    public static IReadOnlySet<string>? MovedSince(string repo, string since, string path)
    {
        Shell.Result shown = Shell.Run(repo, "git", ["show", $"{since}:{path}"]);
        string current = Path.Combine(repo, path.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            IReadOnlyDictionary<string, string> before = shown.Exit == 0
                ? Pin.HeldIn(XDocument.Parse(shown.Stdout.TrimStart('﻿')))
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyDictionary<string, string> after = File.Exists(current)
                ? Pin.HeldBy(current)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return Pin.Moved(before, after);
        }
        catch (XmlException)
        {
            return null;
        }
    }
}
