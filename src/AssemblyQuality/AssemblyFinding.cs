namespace Bennewitz.Ninja.AssemblyQuality;

/// <summary>One rule violation, at one place in an assembly's shape.</summary>
/// <remarks>
/// <para>
/// ⭐ <b>A finding, not an assertion failure.</b> Rules return these rather than throwing, which is
/// what lets one rule drive an xUnit assertion, an MSTest one, a CLI report or an MSBuild warning.
/// A library that threw would have chosen the consumer's runner for them, and the severity too.
/// </para>
/// <para>
/// ⚠ <b>A line only where the symbols give one.</b> Metadata carries no line, so a rule that reads
/// it names the member and leaves <see cref="Source"/> null. A rule that reads method bodies takes
/// the line from the assembly's portable PDB, embedded or beside it; without one it names the method
/// instead. Inventing a position would send a reader somewhere specific and wrong.
/// </para>
/// </remarks>
/// <param name="RuleId">The <see cref="IAssemblyRule.Id"/> that produced this finding.</param>
/// <param name="AssemblyName">Simple name of the assembly the violation is in.</param>
/// <param name="Subject">
/// What is wrong, as a reader would name it — a type, a member, a namespace or a reference.
/// Qualified enough to find by searching, which is how a reader will look for it.
/// </param>
/// <param name="Message">
/// What is wrong and what to do about it. ⚠ Written for whoever hits it six months from now with
/// no context: name the consequence and the fix, not just the rule.
/// </param>
public sealed record AssemblyFinding(
    string RuleId,
    string AssemblyName,
    string Subject,
    string Message)
{
    /// <summary>Where in the source the violation is, where the assembly's symbols say; null otherwise.</summary>
    public SourceLine? Source { get; init; }

    /// <summary>A single line suitable for a failure message or a console report.</summary>
    public override string ToString() =>
        Source is null
            ? $"{AssemblyName}: [{RuleId}] {Subject} — {Message}"
            : $"{AssemblyName}: [{RuleId}] {Subject} ({Source}) — {Message}";
}
