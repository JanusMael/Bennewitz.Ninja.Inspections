namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>The repository a run is in.</summary>
internal static class Repo
{
    /// <summary>
    /// The top of the git checkout holding the current directory, as git names it. ⭐ Read from git, not from a
    /// file a repository happens to hold, so the tool runs in any repository (plans/00008, decision 4).
    /// </summary>
    public static string Root()
    {
        Shell.Result top = Shell.Run(Directory.GetCurrentDirectory(), "git", ["rev-parse", "--show-toplevel"]);
        return top.Exit == 0
            ? Path.GetFullPath(top.Stdout.Trim())
            : throw new InvalidOperationException("not inside a git checkout: run it from the repository whose tests it mutates");
    }
}
