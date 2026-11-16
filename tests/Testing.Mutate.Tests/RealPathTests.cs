using Bennewitz.Ninja.Testing.Mutate;

namespace Testing.Mutate.Tests;

/// <summary>
/// How a dead run's worktree is found: by the path with every link resolved, which is how git names it. On macOS the
/// temp folder is reached through a link, <c>/var</c> to <c>/private/var</c>, so a sweep that compared paths as written
/// never found the worktrees it was there to remove.
/// </summary>
public sealed class RealPathTests
{
    [Fact]
    public void A_path_through_a_link_is_the_path_the_link_leads_to()
    {
        string target = Directory.CreateTempSubdirectory("mutate-real-").FullName;
        string link = target + "-link";
        try
        {
            try
            {
                Directory.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // Windows gives a link only to an administrator or in developer mode; Linux and macOS always do.
                Assert.Skip($"this account cannot make a link: {ex.Message}");
            }

            Assert.Equal(Worktrees.Real(Path.Combine(target, "a", "b")), Worktrees.Real(Path.Combine(link, "a", "b")));
            Assert.NotEqual(Path.GetFullPath(Path.Combine(target, "a")), Path.GetFullPath(Path.Combine(link, "a")));
        }
        finally
        {
            if (Directory.Exists(link))
            {
                Directory.Delete(link);
            }

            Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void A_path_with_no_link_is_its_full_path()
    {
        string folder = Directory.CreateTempSubdirectory("mutate-real-").FullName;
        try
        {
            string real = Worktrees.Real(folder);
            Assert.Equal(Worktrees.Real(Path.Combine(folder, "missing", "..", "x")), Path.Combine(real, "x"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
