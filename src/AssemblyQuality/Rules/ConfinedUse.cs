namespace Bennewitz.Ninja.AssemblyQuality.Rules;

/// <summary>What a method body does with a type or a member.</summary>
public enum UseKind
{
    /// <summary>Constructs a type: <c>newobj</c>, a constructor called on an address, or <c>initobj</c> of a value type.</summary>
    Construction,

    /// <summary>Calls a method, a property's setter among them.</summary>
    Call,

    /// <summary>Makes a delegate from a method.</summary>
    Delegate,

    /// <summary>Reads a field or a property.</summary>
    Read,

    /// <summary>A constructor chaining to its own base's or its own type's, which constructs nothing.</summary>
    Chain,
}

/// <summary>What a <see cref="ConfinedUse"/> confines.</summary>
public enum ConfinedTarget
{
    /// <summary>Constructing the type.</summary>
    Constructions,

    /// <summary>Any use of the type's members: a call, a delegate or a read. Its constructors are not among them.</summary>
    AnyMember,

    /// <summary>A use of one member, by name: a call, a delegate or a read.</summary>
    NamedMember,
}

/// <summary>One confinement for <see cref="ConfinedUseRule"/>: a type or a member, and where its uses are allowed.</summary>
/// <remarks>
/// <para>
/// ⭐ <b>Matched by full name, never by a reference.</b> A name is in reflection syntax, a nested type written
/// <c>Outer+Inner</c>, so a confinement can name an internal type of a library the consumer never references,
/// such as Avalonia's <c>KnownColors</c>.
/// </para>
/// <para>
/// ⚠ <b>A place is allowed by type, by method, or by markup file.</b> A type allows its nested types, the closure
/// classes and state machines the compiler nests in it among them. A method, written <c>Type::Method</c>, allows
/// its lambdas, local functions and state machine. A markup file, named as its symbols name it, allows the code
/// compiled from it, which is how a resource dictionary with no <c>x:Class</c> is allowed.
/// </para>
/// </remarks>
public sealed record ConfinedUse
{
    private ConfinedUse(ConfinedTarget target, string typeName, string? memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        Target = target;
        TypeName = typeName;
        MemberName = memberName;
    }

    /// <summary>Confines constructing <paramref name="typeName"/>.</summary>
    public static ConfinedUse Constructing(string typeName) => new(ConfinedTarget.Constructions, typeName, null);

    /// <summary>Confines every use of <paramref name="typeName"/>'s members.</summary>
    public static ConfinedUse AnyMemberOf(string typeName) => new(ConfinedTarget.AnyMember, typeName, null);

    /// <summary>Confines the uses of one member of <paramref name="typeName"/>, every overload of its name.</summary>
    public static ConfinedUse Member(string typeName, string memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        return new(ConfinedTarget.NamedMember, typeName, memberName);
    }

    /// <summary>What is confined.</summary>
    public ConfinedTarget Target { get; }

    /// <summary>The type named, in reflection syntax.</summary>
    public string TypeName { get; }

    /// <summary>The member named, for <see cref="ConfinedTarget.NamedMember"/>.</summary>
    public string? MemberName { get; }

    /// <summary>
    /// Whether a type that derives from <see cref="TypeName"/>, or implements it, directly or through another
    /// interface, matches too. A member then matches on such a type by its name and its parameters.
    /// </summary>
    public bool IncludingSubtypes { get; init; }

    /// <summary>Whether a type an assembly of the scan declares is left out, as a window class the assembly writes is.</summary>
    public bool ExceptTypesTheScanDeclares { get; init; }

    /// <summary>The types, in reflection syntax, whose code may make the use, their nested types included.</summary>
    public IReadOnlyList<string> AllowedInTypes { get; init; } = [];

    /// <summary>The methods, written <c>Type::Method</c>, whose code may make the use, every overload of the name.</summary>
    public IReadOnlyList<string> AllowedInMethods { get; init; } = [];

    /// <summary>The markup files, by file name, whose compiled code may make the use.</summary>
    public IReadOnlyList<string> AllowedInMarkup { get; init; } = [];

    /// <summary>The confinement as a reader would name it.</summary>
    public override string ToString() => Target switch
    {
        ConfinedTarget.Constructions => $"constructing {TypeName}",
        ConfinedTarget.AnyMember => $"the members of {TypeName}",
        _ => $"{TypeName}::{MemberName}",
    };
}

/// <summary>One use <see cref="ConfinedUseRule"/> matched, allowed or not.</summary>
/// <param name="Confinement">The confinement it matched.</param>
/// <param name="Kind">What the body does.</param>
/// <param name="Member">The member or type used, as <c>Type::Member</c> or a type name.</param>
/// <param name="Method">The code the source wrote it in, as <c>Type::Method</c>, or a type for a closure's plumbing.</param>
/// <param name="AssemblyName">The assembly it is in.</param>
/// <param name="Source">Its file and line, where the symbols give one.</param>
/// <param name="ILOffset">Its IL offset in the body it was compiled into.</param>
/// <param name="Allowed">Whether the confinement allows it. A <see cref="UseKind.Chain"/> always is.</param>
public sealed record ConfinedUseMatch(
    ConfinedUse Confinement,
    UseKind Kind,
    string Member,
    string Method,
    string AssemblyName,
    SourceLine? Source,
    int ILOffset,
    bool Allowed);

/// <summary>What <see cref="ConfinedUseRule.Read"/> found: every matched use, and what it read and skipped.</summary>
/// <param name="Uses">Every matched use, allowed ones and base-constructor chains included, in a stable order.</param>
/// <param name="Inspected">How many method bodies were read; zero when no confinement was given.</param>
/// <param name="Skipped">What could not be read or decided, one line each.</param>
public sealed record ConfinedUseReading(
    IReadOnlyList<ConfinedUseMatch> Uses,
    int Inspected,
    IReadOnlyList<string> Skipped);
