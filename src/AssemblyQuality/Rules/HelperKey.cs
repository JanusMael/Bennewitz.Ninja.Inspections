namespace Bennewitz.Ninja.AssemblyQuality.Rules;

/// <summary>What a value a key argument takes turned out to be.</summary>
public enum KeyValueKind
{
    /// <summary>A constant one of the key class's <c>const string</c> fields holds, which it reads as.</summary>
    Field,

    /// <summary>A constant no field of the key class holds, or, for <see cref="ConfinedUseRule"/>, any constant.</summary>
    Literal,

    /// <summary>A parameter that carries the key attribute, whose own callers give the keys.</summary>
    MarkedParameter,

    /// <summary>Anything the rule could not follow to a constant or a marked parameter.</summary>
    Unknown,
}

/// <summary>One value a key argument takes, and where it arises.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Text">
/// The field, as <c>Class::Field</c>; the literal; the marked parameter, as <c>Type::Method(name)</c>; or why the value
/// is unknown.
/// </param>
/// <param name="Method">The code the value arises in, as <c>Type::Method</c>, or a type for a closure's plumbing.</param>
/// <param name="Source">Its file and line, where the symbols give one.</param>
/// <param name="ILOffset">Its IL offset in the body it was compiled into.</param>
public sealed record KeyValue(KeyValueKind Kind, string Text, string Method, SourceLine? Source, int ILOffset)
{
    /// <summary>The value as a reader would name it, with where it arises.</summary>
    public override string ToString()
    {
        string what = Kind switch
        {
            KeyValueKind.Field => Text,
            KeyValueKind.Literal => $"\"{Text}\"",
            KeyValueKind.MarkedParameter => $"the marked parameter {Text}",
            _ => Text,
        };
        return Source is null ? $"{what}, in {Method}" : $"{what}, in {Method} ({Source})";
    }
}

/// <summary>A parameter that carries the key attribute, and the keys its class holds.</summary>
/// <param name="Method">Its method, as <c>Type::Method</c>.</param>
/// <param name="Parameter">Its name.</param>
/// <param name="KeyClass">The class the attribute names, in reflection syntax.</param>
/// <param name="AssemblyName">The assembly that declares its method.</param>
/// <param name="Fields">The names of the class's <c>const string</c> fields, in ordinal order.</param>
public sealed record MarkedParameter(
    string Method,
    string Parameter,
    string KeyClass,
    string AssemblyName,
    IReadOnlyList<string> Fields);

/// <summary>One call to a marked parameter, or one place a method with one is made into a delegate.</summary>
/// <param name="Kind"><see cref="UseKind.Call"/>, <see cref="UseKind.Construction"/> or <see cref="UseKind.Delegate"/>.</param>
/// <param name="Helper">The method called, as <c>Type::Method</c>.</param>
/// <param name="Parameter">The marked parameter's name.</param>
/// <param name="KeyClass">The class its attribute names, in reflection syntax.</param>
/// <param name="Method">The code that makes the call, as <c>Type::Method</c>, or a type for a closure's plumbing.</param>
/// <param name="AssemblyName">The assembly the call is in.</param>
/// <param name="Source">Its file and line, where the symbols give one.</param>
/// <param name="ILOffset">Its IL offset in the body it was compiled into.</param>
/// <param name="Values">Every value the argument takes; none for a delegate, since no call shows its keys.</param>
public sealed record HelperKeyCall(
    UseKind Kind,
    string Helper,
    string Parameter,
    string KeyClass,
    string Method,
    string AssemblyName,
    SourceLine? Source,
    int ILOffset,
    IReadOnlyList<KeyValue> Values);

/// <summary>What <see cref="HelperKeyRule.Read"/> found.</summary>
/// <param name="Parameters">Every marked parameter the scan declares or calls, its skipped ones left out.</param>
/// <param name="Calls">Every call to one, and every delegate made of a method with one, in a stable order.</param>
/// <param name="Inspected">How many calls were checked; zero where no marked parameter is called.</param>
/// <param name="Skipped">What could not be read or decided, one line each.</param>
public sealed record HelperKeyReading(
    IReadOnlyList<MarkedParameter> Parameters,
    IReadOnlyList<HelperKeyCall> Calls,
    int Inspected,
    IReadOnlyList<string> Skipped);
