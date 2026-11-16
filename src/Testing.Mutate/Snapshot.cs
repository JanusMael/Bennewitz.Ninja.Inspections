// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>A file's bytes kept aside before its first edit, and how its text is read and written.</summary>
internal static class Snapshot
{
    /// <summary>
    /// Writes <paramref name="original"/> under the temp directory and answers where — in a directory
    /// of this checkout's own, so two runs in two worktrees never share a snapshot's path.
    /// </summary>
    public static string Keep(string repo, string path, byte[] original)
    {
        string directory = DirectoryFor(repo);
        Directory.CreateDirectory(directory);
        string hash = Convert.ToHexString(SHA256.HashData(original))[..12];
        string backup = Path.Combine(directory, $"{Path.GetFileName(path)}.{hash}.orig");
        File.WriteAllBytes(backup, original);
        return backup;
    }

    /// <summary>The directory of <paramref name="repo"/>'s own under the temp directory: its snapshots.</summary>
    public static string DirectoryFor(string repo) =>
        Path.Combine(Path.GetTempPath(), "bennewitz-mutate", "snapshots", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(repo)))[..12]);

    /// <summary>
    /// The text of <paramref name="bytes"/>, and an encoding that writes it back with the same
    /// byte-order mark or none, so a mutation changes the edit it declares and nothing else.
    /// </summary>
    public static void Decode(byte[] bytes, out string text, out Encoding encoding)
    {
        bool marked = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);
        encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: marked);
        text = new UTF8Encoding(false).GetString(bytes, marked ? 3 : 0, bytes.Length - (marked ? 3 : 0));
    }
}
