using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bennewitz.Ninja.AssemblyQuality.IL;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;

namespace Bennewitz.Ninja.AssemblyQuality.Rules;

/// <summary>
/// Every key a marked parameter is given is one of its key class's constants, and every such constant is given to one.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The code says which methods are helpers, and where their keys live.</b> A helper marks its key parameter
/// with the class whose <c>const string</c> fields are the keys, <c>Brush([ResourceKey(typeof(DialogColors))] string
/// key)</c>. No package ships the attribute: a consumer declares <c>Bennewitz.Ninja.ResourceKeyAttribute</c>
/// <c>internal</c>, once, in the lowest project that needs it, and the rule finds it by its full name. A rename keeps
/// it true, which a list of helpers in a configuration would not.
/// </para>
/// <para>
/// ⚠ <b>A key is followed, and what cannot be followed is a finding.</b> The argument is followed through locals,
/// ternaries, tuples and the parameters of methods the scan sees every caller of, to a depth of eight. A field, a
/// call's result, a delegate's parameter, and a parameter of a method visible outside its assembly, or one that
/// overrides, implements or is made into a delegate, are unknown, and an unknown key is reported, since a key the
/// rule could not follow is not a key it checked.
/// </para>
/// <para>
/// ⛔ <b>A literal that spells a field's value is that field.</b> A <c>const</c> is compiled into each of its uses,
/// so the IL holds the string, not the field, and the rule reads a key by its value. A literal equal to a key passes.
/// </para>
/// <para>
/// ⚠ <b>Every field must be given somewhere.</b> Every marked parameter naming one class is one set, and a field of
/// a class a marked parameter in the scan names that reaches none of them is reported. A class named only by a helper
/// outside the scan is checked for its keys, not for its unused fields, since the scan holds only some of its callers.
/// </para>
/// </remarks>
public sealed class HelperKeyRule : IAssemblyRule
{
    /// <inheritdoc />
    public string Id => "BNAQ1011";

    /// <inheritdoc />
    public string Summary =>
        "Every key a marked parameter is given is one of its key class's constants, and every such constant is given to one.";

    /// <inheritdoc />
    [RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]
    public AssemblyRuleResult Analyze(AssemblyScanContext context)
    {
        HelperKeyReading reading = Read(context);
        return new AssemblyRuleResult(Findings(reading), reading.Inspected) { Skipped = reading.Skipped };
    }

    /// <summary>Every marked parameter the scan declares, every call to one, and each value each call's key takes.</summary>
    [RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]
    public HelperKeyReading Read(AssemblyScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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

            return new Marking(skipped).Match(read);
        }
        finally
        {
            foreach (AssemblyBodies bodies in read)
            {
                bodies.Dispose();
            }
        }
    }

    private List<AssemblyFinding> Findings(HelperKeyReading reading)
    {
        Dictionary<string, string> classOf = new(StringComparer.Ordinal);
        foreach (MarkedParameter parameter in reading.Parameters)
        {
            classOf[$"{parameter.Method}({parameter.Parameter})"] = parameter.KeyClass;
        }

        List<AssemblyFinding> findings = [];
        HashSet<(string, string)> reached = [];
        foreach (HelperKeyCall call in reading.Calls)
        {
            if (call.Kind == UseKind.Delegate)
            {
                findings.Add(Finding(call, call.Source,
                    $"{call.Method} makes {call.Helper} into a delegate, so no call shows which keys it is given. Call it "
                    + $"directly, with one of {call.KeyClass}'s fields."));
                continue;
            }

            foreach (KeyValue value in call.Values)
            {
                switch (value.Kind)
                {
                    case KeyValueKind.Field:
                        reached.Add((call.KeyClass, value.Text));
                        break;
                    case KeyValueKind.Literal:
                        findings.Add(Finding(call, value.Source,
                            $"{call.Method} gives {call.Helper}'s {call.Parameter} the key \"{value.Text}\" ({value}), which "
                            + $"no const string field of {call.KeyClass} holds. Add the key to {call.KeyClass}, or pass one "
                            + "of its fields."));
                        break;
                    case KeyValueKind.MarkedParameter
                        when classOf.TryGetValue(value.Text, out string? other) && other != call.KeyClass:
                        findings.Add(Finding(call, value.Source,
                            $"{call.Method} gives {call.Helper}'s {call.Parameter}, which takes keys of {call.KeyClass}, "
                            + $"the marked parameter {value.Text}, which takes keys of {other}. Mark it with "
                            + $"{call.KeyClass}, or pass one of {call.KeyClass}'s fields."));
                        break;
                    case KeyValueKind.Unknown:
                        findings.Add(Finding(call, value.Source,
                            $"{call.Method} gives {call.Helper}'s {call.Parameter} a key the rule cannot follow to a "
                            + $"constant: {value}. Pass one of {call.KeyClass}'s fields, directly or through locals, "
                            + "ternaries, tuples or the parameters of methods the scan sees every caller of; or mark the "
                            + $"parameter it comes from with [ResourceKey(typeof({call.KeyClass}))]."));
                        break;
                }
            }
        }

        foreach (MarkedParameter parameter in reading.Parameters.DistinctBy(p => p.KeyClass))
        {
            foreach (string field in parameter.Fields)
            {
                if (!reached.Contains((parameter.KeyClass, $"{parameter.KeyClass}::{field}")))
                {
                    findings.Add(new AssemblyFinding(Id, parameter.AssemblyName, $"{parameter.KeyClass}::{field}",
                        $"{parameter.KeyClass}::{field} reaches no parameter marked [ResourceKey(typeof("
                        + $"{parameter.KeyClass}))] anywhere in the scan, so the key is unused or reaches a helper in a "
                        + "way the rule cannot see. Remove it, or pass it to a marked parameter."));
                }
            }
        }

        return findings;
    }

    private AssemblyFinding Finding(HelperKeyCall call, SourceLine? source, string message) =>
        new(Id, call.AssemblyName, $"{call.Helper}({call.Parameter}) from {call.Method}", message)
        {
            Source = source ?? call.Source,
        };

    /// <summary>A marked parameter as the rule reads it, or the reason it was skipped.</summary>
    private sealed record Mark(string KeyClass, IReadOnlyDictionary<string, string[]> FieldsByValue, string[] Fields);

    private sealed class Marking(List<string> skipped)
    {
        private readonly Dictionary<(string, int), Mark?> _marks = [];
        private readonly HashSet<string> _skippedClasses = new(StringComparer.Ordinal);

        internal HelperKeyReading Match(List<AssemblyBodies> read)
        {
            var flow = new KeyFlow(read);
            List<MarkedParameter> parameters = [];
            foreach (AssemblyBodies bodies in read)
            {
                foreach (ITypeDefinition type in bodies.Compilation.MainModule.TypeDefinitions)
                {
                    foreach (IMethod method in type.Methods)
                    {
                        for (int index = 0; index < method.Parameters.Count; index++)
                        {
                            if (MarkOf(method, index) is { } mark)
                            {
                                parameters.Add(new MarkedParameter(Named(method), method.Parameters[index].Name,
                                    mark.KeyClass, bodies.Name, mark.Fields));
                            }
                        }
                    }
                }
            }

            List<HelperKeyCall> calls = [];
            int inspected = 0;
            foreach (CallSite site in flow.Calls)
            {
                IMethod helper = site.Call.Method;
                for (int index = 0; index < helper.Parameters.Count; index++)
                {
                    if (MarkOf(helper, index) is not { } mark)
                    {
                        continue;
                    }

                    inspected++;
                    BodyPlace place = site.Place;
                    KeyValue[] values = [.. flow.ValuesOf(site, index).Select(v => ValueOf(v, mark))];
                    calls.Add(new HelperKeyCall(site.Call is NewObj ? UseKind.Construction : UseKind.Call, Named(helper),
                        helper.Parameters[index].Name, mark.KeyClass, place.Method, site.Bodies.Name, place.Source,
                        place.Offset, values));
                }
            }

            foreach (DelegateSite site in flow.Delegates)
            {
                for (int index = 0; index < site.Method.Parameters.Count; index++)
                {
                    if (MarkOf(site.Method, index) is { } mark)
                    {
                        BodyPlace place = site.Place;
                        calls.Add(new HelperKeyCall(UseKind.Delegate, Named(site.Method), site.Method.Parameters[index].Name,
                            mark.KeyClass, place.Method, site.Bodies.Name, place.Source, place.Offset, []));
                    }
                }
            }

            return new HelperKeyReading(
                [.. parameters
                    .OrderBy(p => p.AssemblyName, StringComparer.Ordinal)
                    .ThenBy(p => p.Method, StringComparer.Ordinal)
                    .ThenBy(p => p.Parameter, StringComparer.Ordinal)],
                [.. calls
                    .OrderBy(c => c.AssemblyName, StringComparer.Ordinal)
                    .ThenBy(c => c.Method, StringComparer.Ordinal)
                    .ThenBy(c => c.ILOffset)
                    .ThenBy(c => c.Helper, StringComparer.Ordinal)
                    .ThenBy(c => c.Parameter, StringComparer.Ordinal)],
                inspected,
                skipped);
        }

        private static KeyValue ValueOf(FlowValue value, Mark mark)
        {
            (KeyValueKind kind, string text) = value.Kind switch
            {
                FlowKind.Constant when mark.FieldsByValue.TryGetValue(value.Text, out string[]? fields) =>
                    (KeyValueKind.Field, $"{mark.KeyClass}::{fields[0]}"),
                FlowKind.Constant => (KeyValueKind.Literal, value.Text),
                FlowKind.Marked => (KeyValueKind.MarkedParameter, value.Text),
                _ => (KeyValueKind.Unknown, value.Text),
            };
            return new KeyValue(kind, text, value.Where.Method, value.Where.Source, value.Where.Offset);
        }

        /// <summary>The parameter's mark, or null where it carries none or was skipped, the reason named once.</summary>
        private Mark? MarkOf(IMethod method, int index)
        {
            var key = (KeyFlow.KeyOf(method), index);
            if (_marks.TryGetValue(key, out Mark? known))
            {
                return known;
            }

            return _marks[key] = Read(method, index);
        }

        private Mark? Read(IMethod method, int index)
        {
            IParameter parameter = method.Parameters[index];
            IAttribute? attribute;
            try
            {
                attribute = KeyFlow.KeyAttribute(parameter);
            }
            catch (Exception ex) when (ex is BadImageFormatException or InvalidOperationException)
            {
                skipped.Add($"{Named(method)}({parameter.Name}): its attributes could not be read ({ex.GetType().Name}: "
                    + $"{ex.Message}), so no call to it was checked.");
                return null;
            }

            if (attribute is null)
            {
                return null;
            }

            string named = $"{Named(method)}({parameter.Name})";
            if (!parameter.Type.IsKnownType(KnownTypeCode.String))
            {
                skipped.Add($"{named} carries [ResourceKey] but takes {parameter.Type.ReflectionName}, not a string, so no "
                    + "call to it was checked.");
                return null;
            }

            if (attribute.FixedArguments is not [{ Value: IType keys }] || keys.Kind == TypeKind.Unknown
                || keys.GetDefinition() is not { } definition)
            {
                skipped.Add($"{named} carries [ResourceKey] without one type, given to its constructor, that resolves, so "
                    + "no call to it was checked. Give the attribute the key class: [ResourceKey(typeof(Keys))].");
                return null;
            }

            string keyClass = definition.ReflectionName;
            IField[] constants = [.. definition.Fields
                .Where(f => f.IsConst && f.Type.IsKnownType(KnownTypeCode.String) && f.GetConstantValue() is string)
                .OrderBy(f => f.Name, StringComparer.Ordinal)];
            if (constants.Length == 0)
            {
                skipped.Add($"{named} names {keyClass}, which holds no const string field, so no call to it was checked.");
                return null;
            }

            Dictionary<string, string[]> byValue = constants
                .GroupBy(f => (string)f.GetConstantValue()!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Name).ToArray(), StringComparer.Ordinal);
            foreach ((string value, string[] fields) in byValue)
            {
                if (fields.Length > 1 && _skippedClasses.Add($"{keyClass}\0{value}"))
                {
                    skipped.Add($"{keyClass}: {string.Join(" and ", fields)} hold one value, \"{value}\", so a key of "
                        + $"that value is read as {fields[0]}, and whether {string.Join(" or ", fields[1..])} is given "
                        + "anywhere was not checked.");
                }
            }

            return new Mark(keyClass, byValue,
                [.. byValue.Values.Select(f => f[0]).Order(StringComparer.Ordinal)]);
        }

        private static string Named(IMethod method) => $"{method.DeclaringType.ReflectionName}::{method.Name}";
    }
}
