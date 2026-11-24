using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bennewitz.Ninja.AssemblyQuality.IL;
using ICSharpCode.Decompiler.TypeSystem;

namespace Bennewitz.Ninja.AssemblyQuality.Rules;

/// <summary>
/// A named type or member is used only where its confinement allows it.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Where a thing is used is a claim only the method bodies hold.</b> "Only the dialog colours make a brush"
/// and "nothing constructs a window it did not declare" are true of an assembly until one call says otherwise,
/// and no signature, reference or attribute shows the call. This rule reads every method body, the compiler's
/// lambdas, local functions, state machines and compiled markup among them, through plan 00011's IL reader.
/// </para>
/// <para>
/// ⚠ <b>No default confinement, deliberately.</b> What belongs where is the consumer's architecture. Given none,
/// the rule reports nothing and inspects nothing, the honest answer to a question never asked.
/// </para>
/// <para>
/// ⚠ <b>A finding is only as good as its controls.</b> <see cref="AssemblyRuleResult.Inspected"/> counts the
/// bodies read, which a matcher gone blind still reads, so a consumer asserts its positive controls, the allowed
/// uses it expects, through <see cref="Read"/>, as the uses <see cref="Analyze"/> derives its findings from.
/// </para>
/// <para>
/// ⛔ <b>A constructor chaining to its base is no construction.</b> A window class's constructor calls
/// <c>Window</c>'s on <c>this</c>, which builds the window being constructed and no other; the reading lists it as a
/// <see cref="UseKind.Chain"/>, never reported, and that is how a consumer sees its own window classes.
/// </para>
/// </remarks>
public sealed class ConfinedUseRule : IAssemblyRule
{
    private readonly ConfinedUse[] _confinements;

    /// <summary>Confines nothing; reports that it inspected nothing.</summary>
    public ConfinedUseRule()
        : this([])
    {
    }

    /// <summary>Holds each of <paramref name="confinements"/>.</summary>
    public ConfinedUseRule(IEnumerable<ConfinedUse> confinements)
    {
        ArgumentNullException.ThrowIfNull(confinements);
        _confinements = [.. confinements];
        if (_confinements.Any(c => c is null))
        {
            throw new ArgumentException("A confinement is null.", nameof(confinements));
        }
    }

    /// <inheritdoc />
    public string Id => "BNAQ1010";

    /// <inheritdoc />
    public string Summary => "A named type or member is used only where its confinement allows it.";

    /// <inheritdoc />
    [RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]
    public AssemblyRuleResult Analyze(AssemblyScanContext context)
    {
        ConfinedUseReading reading = Read(context);
        List<AssemblyFinding> findings = [.. reading.Uses.Where(u => !u.Allowed).Select(Finding)];
        return new AssemblyRuleResult(findings, reading.Inspected) { Skipped = reading.Skipped };
    }

    /// <summary>Every use each confinement matches in <paramref name="context"/>, allowed or not.</summary>
    [RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]
    public ConfinedUseReading Read(AssemblyScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_confinements.Length == 0)
        {
            return new ConfinedUseReading([], 0, []);
        }

        List<string> skipped = [];
        List<AssemblyBodies> read = [];
        try
        {
            foreach (Assembly assembly in context.Assemblies)
            {
                if (MethodBodies.Read(assembly, skipped) is { } bodies)
                {
                    read.Add(bodies);
                }
            }

            return Match(read, skipped);
        }
        finally
        {
            foreach (AssemblyBodies bodies in read)
            {
                bodies.Dispose();
            }
        }
    }

    private ConfinedUseReading Match(List<AssemblyBodies> read, List<string> skipped)
    {
        HashSet<string> scanned = [.. read.Select(r => r.Name)];
        SkipUnresolved(read, skipped);

        SortedSet<string> undecided = new(StringComparer.Ordinal);
        List<(int Order, ConfinedUseMatch Match)> uses = [];
        foreach (AssemblyBodies bodies in read)
        {
            foreach (BodyUse use in bodies.Uses)
            {
                for (int index = 0; index < _confinements.Length; index++)
                {
                    ConfinedUse confinement = _confinements[index];
                    if (Matches(confinement, use, bodies, scanned, undecided))
                    {
                        uses.Add((index, new ConfinedUseMatch(confinement, use.Kind, MemberOf(use), MethodOf(use),
                            bodies.Name, use.Source, use.Offset, Allows(confinement, use))));
                    }
                }
            }
        }

        skipped.AddRange(undecided);
        ConfinedUseMatch[] ordered = [.. uses
            .OrderBy(u => u.Match.AssemblyName, StringComparer.Ordinal)
            .ThenBy(u => u.Match.Method, StringComparer.Ordinal)
            .ThenBy(u => u.Match.ILOffset)
            .ThenBy(u => u.Match.Member, StringComparer.Ordinal)
            .ThenBy(u => u.Order)
            .Select(u => u.Match)];
        return new ConfinedUseReading(ordered, read.Sum(r => r.Bodies), skipped);
    }

    /// <summary>Names in the skips, once per scan, each configured name that resolves in none of its assemblies.</summary>
    private void SkipUnresolved(List<AssemblyBodies> read, List<string> skipped)
    {
        foreach (ConfinedUse confinement in _confinements)
        {
            ITypeDefinition[] types = [.. read.Select(r => Resolve(r.Compilation, confinement.TypeName)).OfType<ITypeDefinition>()];
            if (types.Length == 0)
            {
                skipped.Add($"{confinement}: {confinement.TypeName} resolves to no type in the scan or what it "
                    + "references, so nothing was matched against it.");
            }
            else if (confinement.MemberName is { } member
                && !types.Any(t => t.GetMembers(m => NameOf(m) == member, GetMemberOptions.IgnoreInheritedMembers).Any()))
            {
                skipped.Add($"{confinement}: {confinement.TypeName} declares no member named {member}, so nothing "
                    + "was matched against it.");
            }
        }
    }

    private static bool Matches(ConfinedUse confinement, BodyUse use, AssemblyBodies bodies, HashSet<string> scanned,
        SortedSet<string> undecided)
    {
        bool kindFits = confinement.Target == ConfinedTarget.Constructions
            ? use.Kind is UseKind.Construction or UseKind.Chain
            : use.Kind is UseKind.Call or UseKind.Delegate or UseKind.Read;
        if (!kindFits)
        {
            return false;
        }

        if (confinement.Target == ConfinedTarget.NamedMember
            && (use.Member is null || NameOf(use.Member) != confinement.MemberName))
        {
            return false;
        }

        IType subject = use.Type;
        bool exact = DefinitionName(subject) == confinement.TypeName;
        if (!exact)
        {
            if (!confinement.IncludingSubtypes)
            {
                return false;
            }

            switch (Derives(bodies, subject, confinement.TypeName))
            {
                case false:
                    return false;
                case null:
                    undecided.Add($"{bodies.Name}: whether {DefinitionName(subject)} matches {confinement} could not be "
                        + "decided, since a type it derives from does not resolve from the assembly's folder or the "
                        + "shared framework.");
                    return false;
            }

            if (confinement.Target == ConfinedTarget.NamedMember && !SameSignature(use.Member!, confinement, bodies))
            {
                return false;
            }
        }

        if (confinement.ExceptTypesTheScanDeclares && use.Kind != UseKind.Chain
            && subject.GetDefinition()?.ParentModule?.AssemblyName is { } owner && scanned.Contains(owner))
        {
            return false;
        }

        return true;
    }

    /// <summary>Whether <paramref name="type"/> derives from or implements the named type, or null where it cannot be followed.</summary>
    private static bool? Derives(AssemblyBodies bodies, IType type, string name)
    {
        (HashSet<string> names, bool unknown) = bodies.AncestryOf(type);
        return names.Contains(name) ? true : unknown ? null : false;
    }

    /// <summary>Whether a member on a subtype has the parameters of a member the confinement's type declares by its name.</summary>
    private static bool SameSignature(IMember member, ConfinedUse confinement, AssemblyBodies bodies)
    {
        if (Resolve(bodies.Compilation, confinement.TypeName) is not { } named)
        {
            return false;
        }

        string[] parameters = Parameters(member);
        return named.GetMembers(m => NameOf(m) == confinement.MemberName, GetMemberOptions.IgnoreInheritedMembers)
            .Any(m => Parameters(m).SequenceEqual(parameters, StringComparer.Ordinal));
    }

    private static string[] Parameters(IMember member) =>
        member is IParameterizedMember parameterized
            ? [.. parameterized.Parameters.Select(p => p.Type.ReflectionName)]
            : [];

    private static bool Allows(ConfinedUse confinement, BodyUse use)
    {
        if (use.Kind == UseKind.Chain)
        {
            return true;
        }

        if (confinement.AllowedInTypes.Any(t => use.CreditType == t || use.CreditType.StartsWith(t + "+", StringComparison.Ordinal)))
        {
            return true;
        }

        if (use.CreditMethod is { } method && confinement.AllowedInMethods.Contains($"{use.CreditType}::{method}", StringComparer.Ordinal))
        {
            return true;
        }

        return use.Source is { } source
            && confinement.AllowedInMarkup.Contains(FileName(source.File), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A document's file name, whichever separator the compiler wrote its path with.</summary>
    private static string FileName(string path) => path[(path.LastIndexOfAny(['/', '\\']) + 1)..];

    private static ITypeDefinition? Resolve(ICompilation compilation, string name)
    {
        IType type = compilation.FindType(new FullTypeName(name));
        return type.Kind == TypeKind.Unknown ? null : type.GetDefinition();
    }

    /// <summary>A member's name, a property's for its accessors.</summary>
    private static string NameOf(IMember member) =>
        member is IMethod { AccessorOwner: { } owner } ? owner.Name : member.Name;

    private static string DefinitionName(IType type) => type.GetDefinition()?.ReflectionName ?? type.ReflectionName;

    private static string MemberOf(BodyUse use) =>
        use.Member is null ? DefinitionName(use.Type) : $"{DefinitionName(use.Member.DeclaringType)}::{NameOf(use.Member)}";

    private static string MethodOf(BodyUse use) =>
        use.CreditMethod is null ? use.CreditType : $"{use.CreditType}::{use.CreditMethod}";

    private AssemblyFinding Finding(ConfinedUseMatch use) =>
        new(Id, use.AssemblyName, $"{use.Member} in {use.Method}",
            $"{Verb(use.Kind)} in {use.Method}, outside where {use.Confinement} is confined: {Places(use.Confinement)}. "
            + "Move the use into one of those places, or allow this one if it is meant to make it.")
        {
            Source = use.Source,
        };

    private static string Verb(UseKind kind) => kind switch
    {
        UseKind.Construction => "Constructed",
        UseKind.Call => "Called",
        UseKind.Delegate => "Made into a delegate",
        _ => "Read",
    };

    private static string Places(ConfinedUse confinement)
    {
        string[] places =
        [
            .. confinement.AllowedInTypes,
            .. confinement.AllowedInMethods,
            .. confinement.AllowedInMarkup.Select(m => $"the markup {m}"),
        ];
        return places.Length == 0 ? "nowhere" : string.Join(", ", places);
    }
}
