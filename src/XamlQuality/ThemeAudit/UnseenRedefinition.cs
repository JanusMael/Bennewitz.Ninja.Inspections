namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>
/// A key a consumer redefines that its theme's own reads never see, by plans/00007, or one the audit cannot decide: one
/// redefinition under the variants that share its verdict, its reads and its keys to redefine instead, so a key may have
/// more than one.
/// </summary>
/// <remarks>
/// ⓘ <b>A member added later is an <c>init</c> property, never a new positional parameter</b>, so that code compiled
/// against an earlier release keeps compiling.
/// </remarks>
/// <param name="Key">The key the consumer redefines.</param>
/// <param name="Variants">The display names of the variants it is redefined under, in the audit's order.</param>
/// <param name="DefinedAt">
/// Each file and line where the consumer defines it with a value other than the theme's, in the nearest of the
/// application's dictionaries that holds it under those variants, with the variant of the theme dictionary holding it where one does,
/// written as the report writes them: the path below the directory it was read from, with <c>/</c>.
/// </param>
/// <param name="Reads">How many reads the theme has of it under those variants, each counted once by its file and line.</param>
/// <param name="RedefineInstead">
/// The keys the theme reads dynamically under those variants whose definition, followed alias by alias, reads
/// <paramref name="Key"/>, ordinally; empty where there are none.
/// </param>
/// <param name="Why">Why one not decided is not; <c>null</c> for a finding.</param>
public sealed record UnseenRedefinition(
    string Key,
    IReadOnlyList<string> Variants,
    IReadOnlyList<string> DefinedAt,
    int Reads,
    IReadOnlyList<string> RedefineInstead,
    string? Why)
{
    /// <summary>Each read the theme has of it, by its file below the theme's base directory and its line, by path and then line.</summary>
    internal IReadOnlyList<string> ReadAt { get; init; } = [];
}
