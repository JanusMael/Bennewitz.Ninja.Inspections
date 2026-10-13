namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>Whether a committed output is what a fresh run writes.</summary>
public enum DriftState
{
    /// <summary>The committed copy is the fresh output, line endings aside.</summary>
    UpToDate,

    /// <summary>The committed copy differs from the fresh output, which was written beside it.</summary>
    Stale,

    /// <summary>There is no committed copy.</summary>
    Missing,
}

/// <summary>
/// The drift check: whether a committed output, such as a report or a generated dictionary, is what a fresh run writes.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>One comparison, for the tool and a consumer's test alike.</b> <c>theme-audit</c>'s <c>--check</c> runs it, and
/// a test that generates an output in process calls it with what it generated, so neither writes the comparison again.
/// </para>
/// <para>
/// ⚠ <b>CRLF reads as LF.</b> git on Windows checks a text file out with CRLF unless told otherwise, and an output a
/// fresh run writes with LF would read as stale there and up to date everywhere else.
/// </para>
/// <para>
/// ⚠ <b>On drift, the fresh output is written beside the committed one</b>, as <c>name.received.ext</c>, so the
/// difference can be read, or the file taken, without a second run; a check that finds the output up to date removes
/// one an earlier check left. A repository that runs the check ignores <c>*.received.*</c>. A received copy keeps the
/// committed file's extension, so where a build compiles every file of that extension, as Avalonia's does
/// <c>.axaml</c>, it is compiled too until it is removed.
/// </para>
/// </remarks>
public static class OutputDrift
{
    /// <summary>Compares the committed copy at <paramref name="path"/> with <paramref name="fresh"/>.</summary>
    /// <param name="path">The committed output.</param>
    /// <param name="fresh">What a fresh run writes.</param>
    /// <returns>
    /// <see cref="DriftState.Missing"/> when there is no committed copy, <see cref="DriftState.Stale"/> when it differs,
    /// line endings aside, and <see cref="DriftState.UpToDate"/> when it does not.
    /// </returns>
    public static DriftState Check(string path, string fresh)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(fresh);

        if (!File.Exists(path))
        {
            return DriftState.Missing;
        }

        string received = ReceivedPathOf(path);
        if (string.Equals(Lf(File.ReadAllText(path)), Lf(fresh), StringComparison.Ordinal))
        {
            File.Delete(received);
            return DriftState.UpToDate;
        }

        File.WriteAllText(received, fresh);
        return DriftState.Stale;
    }

    /// <summary>
    /// Where the fresh output of a stale <paramref name="path"/> is written: beside it, as <c>name.received.ext</c>.
    /// </summary>
    /// <param name="path">The committed output.</param>
    /// <returns>The received copy's path.</returns>
    public static string ReceivedPathOf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.Combine(
            Path.GetDirectoryName(path) ?? string.Empty,
            Path.GetFileNameWithoutExtension(path) + ".received" + Path.GetExtension(path));
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
