namespace Bennewitz.Ninja.AssemblyQuality;

/// <summary>Where in the source a finding is: a file and a line, read from the assembly's symbols.</summary>
/// <remarks>
/// ⚠ <b>Only where symbols say so.</b> A line comes from a portable PDB's sequence points, embedded in the
/// DLL or beside it. Without one there is no line to give, and a finding names its member instead.
/// </remarks>
/// <param name="File">The document the symbols name, as the compiler wrote it: a path, or a markup file.</param>
/// <param name="Line">The line the sequence point starts on, counted from 1.</param>
public sealed record SourceLine(string File, int Line)
{
    /// <summary>The file and the line, as <c>file:line</c>.</summary>
    public override string ToString() => $"{File}:{Line}";
}
