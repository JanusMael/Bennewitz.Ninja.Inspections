// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>A checkout's state as a git tree: what --parallel gives each worktree, and what each shard says it ran over.</summary>
internal static class Source
{
    /// <summary>
    /// The tree this checkout's working state would commit as — HEAD, every tracked change, and each
    /// untracked file the ignore rules let through, wherever it is — written through a
    /// temporary index, so that this checkout's own index and every ref stay where they are and no commit
    /// is made. Equal trees are equal states, which is how a worktree is shown to be this checkout.
    /// </summary>
    public static string Tree(string repo)
    {
        string directory = Path.Combine(Path.GetTempPath(), "bennewitz-mutate", "indexes");
        Directory.CreateDirectory(directory);
        string index = Path.Combine(directory, $"index-{Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}");
        Dictionary<string, string> environment = new(StringComparer.Ordinal) { ["GIT_INDEX_FILE"] = index };
        try
        {
            Git(repo, environment, ["read-tree", "HEAD"]);
            Git(repo, environment, ["add", "--all"]);
            return Git(repo, environment, ["write-tree"]).Trim();
        }
        finally
        {
            File.Delete(index);
        }
    }

    /// <summary>Gives a worktree cut from HEAD the state <paramref name="tree"/> holds, in its index and its files.</summary>
    public static void Load(string worktree, string tree) => Git(worktree, null, ["read-tree", "--reset", "-u", tree]);

    private static string Git(string repo, IReadOnlyDictionary<string, string>? environment, string[] arguments)
    {
        Shell.Result result = Shell.Run(repo, "git", arguments, environment: environment);
        return result.Exit == 0
            ? result.Stdout
            : throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed in {repo}: {result.Output.Trim()}");
    }
}
