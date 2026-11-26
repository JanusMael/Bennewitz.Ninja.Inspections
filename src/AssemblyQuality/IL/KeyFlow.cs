using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;

namespace Bennewitz.Ninja.AssemblyQuality.IL;

/// <summary>What a value a key argument takes turned out to be.</summary>
internal enum FlowKind
{
    /// <summary>A string constant, a <c>const</c> field's or a literal's, which the compiler cannot tell apart.</summary>
    Constant,

    /// <summary>A parameter that carries the key attribute, whose own callers give the keys.</summary>
    Marked,

    /// <summary>Anything the following could not take to a constant or a marked parameter.</summary>
    Unknown,
}

/// <summary>One value a key argument takes, and where it arises.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Text">A constant's value, a marked parameter as <c>Type::Method(name)</c>, or why it is unknown.</param>
/// <param name="Where">Where the value arises: the constant, the marked parameter's use, or what could not be followed.</param>
/// <param name="Parameter">The marked parameter, for <see cref="FlowKind.Marked"/>.</param>
internal sealed record FlowValue(FlowKind Kind, string Text, BodyPlace Where, IParameter? Parameter);

/// <summary>A call the scan makes, in the reading of its body that sees the most.</summary>
/// <param name="Call">The call; for a construction, its <c>newobj</c>.</param>
/// <param name="Function">The body it is read in.</param>
/// <param name="Bodies">The assembly that body is in.</param>
internal sealed record CallSite(CallInstruction Call, ReadFunction Function, AssemblyBodies Bodies)
{
    /// <summary>Where the call is.</summary>
    internal BodyPlace Place => Bodies.PlaceOf(Call, Function);
}

/// <summary>A method the scan makes into a delegate.</summary>
/// <param name="Method">The method.</param>
/// <param name="Instruction">The <c>ldftn</c> or <c>ldvirtftn</c>.</param>
/// <param name="Function">The body it is read in.</param>
/// <param name="Bodies">The assembly that body is in.</param>
internal sealed record DelegateSite(IMethod Method, ILInstruction Instruction, ReadFunction Function, AssemblyBodies Bodies)
{
    /// <summary>Where the delegate is made.</summary>
    internal BodyPlace Place => Bodies.PlaceOf(Instruction, Function);
}

/// <summary>Follows a key from the argument a call is given back to what it was made from, plan 00011 decision 6.</summary>
/// <remarks>
/// <para>
/// ⭐ <b>A value is followed within its method, and through a parameter to every caller in the scan.</b> A local
/// through each of its stores, a ternary or a <c>??</c> through both arms, a tuple element to the argument that built
/// it, and a parameter through every call to its method, to a depth of eight. A string constant ends a path, and so
/// does a parameter that carries the key attribute, whose own callers give its keys.
/// </para>
/// <para>
/// ⚠ <b>What it cannot follow is unknown, never assumed.</b> A field, a call's result, a delegate's parameter, and a
/// parameter whose method has callers the scan cannot see, one visible outside its assembly, one that overrides or
/// implements another, and one made into a delegate, each end a path as <see cref="FlowKind.Unknown"/>, with why.
/// </para>
/// <para>
/// ⚠ <b>A call is read where its variables are.</b> A lambda's body and a state machine's <c>MoveNext</c> are read
/// on their own and again folded into the method the source wrote; only the folded reading knows a captured local
/// as a variable rather than a closure's field, so a call kept once is kept from that reading.
/// </para>
/// </remarks>
internal sealed class KeyFlow
{
    /// <summary>The key attribute's full name, matched by name since every consumer declares its own.</summary>
    internal const string AttributeName = "Bennewitz.Ninja.ResourceKeyAttribute";

    /// <summary>How many callers deep a parameter is followed.</summary>
    internal const int Depth = 8;

    private readonly Dictionary<string, List<CallSite>> _callers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _delegated = new(StringComparer.Ordinal);
    private readonly Dictionary<(string, int, int), CallSite> _sites;

    internal KeyFlow(IReadOnlyList<AssemblyBodies> read)
    {
        var calls = new Dictionary<(string, int, int), CallSite>();
        var delegates = new Dictionary<(string, int, int), DelegateSite>();
        foreach (AssemblyBodies bodies in read)
        {
            foreach (ReadFunction function in bodies.Functions)
            {
                foreach (ILInstruction instruction in function.Top.Descendants)
                {
                    switch (instruction)
                    {
                        case CallInstruction call:
                            Keep(calls, bodies, function, call, new CallSite(call, function, bodies));
                            break;
                        case LdFtn ldFtn:
                            Keep(delegates, bodies, function, ldFtn, new DelegateSite(ldFtn.Method, ldFtn, function, bodies));
                            break;
                        case LdVirtFtn ldVirtFtn:
                            Keep(delegates, bodies, function, ldVirtFtn,
                                new DelegateSite(ldVirtFtn.Method, ldVirtFtn, function, bodies));
                            break;
                    }
                }
            }
        }

        _sites = calls;
        Calls = [.. calls.Values];
        Delegates = [.. delegates.Values];
        foreach (CallSite site in Calls)
        {
            string key = KeyOf(site.Call.Method);
            if (!_callers.TryGetValue(key, out List<CallSite>? list))
            {
                _callers[key] = list = [];
            }

            list.Add(site);
        }

        foreach (DelegateSite site in Delegates)
        {
            _delegated.Add(KeyOf(site.Method));
        }
    }

    /// <summary>Every call the scan makes, each once.</summary>
    internal IReadOnlyList<CallSite> Calls { get; }

    /// <summary>Every method the scan makes into a delegate, each place once.</summary>
    internal IReadOnlyList<DelegateSite> Delegates { get; }

    /// <summary>The call compiled at <paramref name="offset"/> of body <paramref name="function"/>, or null.</summary>
    internal CallSite? SiteAt(string assembly, int function, int offset) =>
        _sites.TryGetValue((assembly, function, offset), out CallSite? site) ? site : null;

    /// <summary>The key attribute a parameter carries, or null.</summary>
    internal static IAttribute? KeyAttribute(IParameter parameter) =>
        parameter.GetAttributes().FirstOrDefault(a => a.AttributeType.FullName == AttributeName);

    /// <summary>
    /// A method as one name across every assembly of the scan, its generic instantiations folded into it, and a local
    /// function as the method the compiler made of it, which a call names in its reduced form.
    /// </summary>
    internal static string KeyOf(IMethod method)
    {
        if (method.IsLocalFunction && method.ReducedFrom is { } original)
        {
            method = original;
        }

        IMethod definition = method.MemberDefinition as IMethod ?? method;
        string assembly = definition.ParentModule?.AssemblyName ?? "?";
        string type = definition.DeclaringTypeDefinition?.ReflectionName ?? definition.DeclaringType.ReflectionName;
        string parameters = string.Join(",", definition.Parameters.Select(p => p.Type.ReflectionName));
        return $"{assembly}|{type}::{definition.Name}`{definition.TypeParameters.Count}({parameters})";
    }

    /// <summary>The values the argument given to parameter <paramref name="parameter"/> of <paramref name="site"/>'s method takes.</summary>
    internal IReadOnlyList<FlowValue> ValuesOf(CallSite site, int parameter)
    {
        int index = parameter + (site.Call.IsInstanceCall ? 1 : 0);
        if (parameter < 0 || index >= site.Call.Arguments.Count)
        {
            return [];
        }

        List<FlowValue> values = [];
        Trace(site.Call.Arguments[index], site.Function, site.Bodies, 0, [], values);
        return [.. values.Distinct()];
    }

    private static void Keep<T>(Dictionary<(string, int, int), T> kept, AssemblyBodies bodies, ReadFunction function,
        ILInstruction instruction, T site)
        where T : class
    {
        ILFunction owner = MethodBodies.OwnerOf(instruction, function.Top);
        var key = (bodies.Name, MethodBodies.FunctionToken(owner, function.Top), MethodBodies.OffsetOf(instruction));
        if (!kept.TryGetValue(key, out T? earlier) || (function.Folds(instruction) && !FoldedIn(earlier)))
        {
            kept[key] = site;
        }
    }

    private static bool FoldedIn(object site) => site switch
    {
        CallSite call => call.Function.Folds(call.Call),
        DelegateSite made => made.Function.Folds(made.Instruction),
        _ => false,
    };

    private void Trace(ILInstruction value, ReadFunction function, AssemblyBodies bodies, int depth,
        HashSet<ILVariable> seen, List<FlowValue> into)
    {
        switch (value)
        {
            case LdStr text:
                into.Add(new FlowValue(FlowKind.Constant, text.Value, bodies.PlaceOf(value, function), null));
                return;
            case LdLoc load:
                Variable(load.Variable, value, function, bodies, depth, seen, into);
                return;
            case IfInstruction choice when choice.ResultType != StackType.Void:
                Trace(choice.TrueInst, function, bodies, depth, seen, into);
                Trace(choice.FalseInst, function, bodies, depth, seen, into);
                return;
            case NullCoalescingInstruction coalesce:
                Trace(coalesce.ValueInst, function, bodies, depth, seen, into);
                Trace(coalesce.FallbackInst, function, bodies, depth, seen, into);
                return;
            case Block block when block.FinalInstruction.ResultType != StackType.Void:
                Trace(block.FinalInstruction, function, bodies, depth, seen, into);
                return;
        }

        if (value is LdObj { Target: LdFlda { Field: var field, Target: var tuple } } && TupleItem(field) is int item)
        {
            TupleElement(tuple, item, function, bodies, depth, seen, into);
            return;
        }

        Unknown(Describe(value), value, function, bodies, into);
    }

    private void Variable(ILVariable variable, ILInstruction load, ReadFunction function, AssemblyBodies bodies, int depth,
        HashSet<ILVariable> seen, List<FlowValue> into)
    {
        if (!seen.Add(variable))
        {
            return;
        }

        if (variable.AddressCount > 0)
        {
            Unknown($"the local {variable.Name}, whose address is taken, so a call may set it", load, function, bodies, into);
            return;
        }

        foreach (IStoreInstruction store in variable.StoreInstructions)
        {
            if (store is StLoc stored)
            {
                Trace(stored.Value, function, bodies, depth, seen, into);
            }
            else
            {
                Unknown($"{variable.Name}, which {((ILInstruction)store).OpCode} sets", load, function, bodies, into);
            }
        }

        if (variable.Kind == VariableKind.Parameter)
        {
            Parameter(variable, load, function, bodies, depth, seen, into);
        }
        else if (variable.StoreCount == 0)
        {
            Unknown($"the local {variable.Name}, which nothing sets", load, function, bodies, into);
        }
    }

    private void Parameter(ILVariable variable, ILInstruction load, ReadFunction function, AssemblyBodies bodies,
        int depth, HashSet<ILVariable> seen, List<FlowValue> into)
    {
        ILFunction declaring = variable.Function ?? function.Top;
        if (variable.Index is not int index || index < 0)
        {
            Unknown("this", load, function, bodies, into);
            return;
        }

        if (declaring.Kind is ILFunctionKind.Delegate or ILFunctionKind.ExpressionTree || declaring.Method is not { } method
            || index >= method.Parameters.Count)
        {
            Unknown($"the parameter {variable.Name} of a lambda, which whoever invokes the delegate gives", load, function,
                bodies, into);
            return;
        }

        IParameter parameter = method.Parameters[index];
        string named = $"{method.DeclaringType.ReflectionName}::{method.Name}({parameter.Name})";
        if (KeyAttribute(parameter) is not null)
        {
            into.Add(new FlowValue(FlowKind.Marked, named, bodies.PlaceOf(load, function), parameter));
            return;
        }

        if (WhyCallersUnseen(method) is { } why)
        {
            Unknown($"the parameter {named}, {why}, so the scan cannot see every caller", load, function, bodies, into);
            return;
        }

        if (depth >= Depth)
        {
            Unknown($"the parameter {named}, followed through {Depth} callers and no further", load, function, bodies, into);
            return;
        }

        if (!_callers.TryGetValue(KeyOf(method), out List<CallSite>? callers))
        {
            return;
        }

        foreach (CallSite caller in callers)
        {
            int argument = index + (caller.Call.IsInstanceCall ? 1 : 0);
            if (argument < caller.Call.Arguments.Count)
            {
                Trace(caller.Call.Arguments[argument], caller.Function, caller.Bodies, depth + 1, seen, into);
            }
        }
    }

    private void TupleElement(ILInstruction tuple, int item, ReadFunction function, AssemblyBodies bodies, int depth,
        HashSet<ILVariable> seen, List<FlowValue> into)
    {
        if (tuple is LdLoca { Variable: var variable })
        {
            TupleVariable(variable, tuple, item, function, bodies, depth, seen, into);
        }
        else
        {
            Unknown(Describe(tuple), tuple, function, bodies, into);
        }
    }

    /// <summary>Follows element <paramref name="item"/> of the tuple a local holds to whatever built or set it.</summary>
    private void TupleVariable(ILVariable variable, ILInstruction at, int item, ReadFunction function,
        AssemblyBodies bodies, int depth, HashSet<ILVariable> seen, List<FlowValue> into)
    {
        ILInstruction tuple = at;
        if (!seen.Add(variable))
        {
            return;
        }

        foreach (IStoreInstruction store in variable.StoreInstructions)
        {
            if (store is StLoc stored)
            {
                Built(stored.Value, item, function, bodies, depth, seen, into);
            }
            else
            {
                Unknown($"the tuple {variable.Name}, which {((ILInstruction)store).OpCode} sets", tuple, function, bodies, into);
            }
        }

        foreach (LdLoca address in variable.AddressInstructions)
        {
            if (address.Parent is LdFlda { Parent: LdObj })
            {
                continue;
            }

            if (address.Parent is LdFlda { Field: var field, Parent: StObj written } element && written.Target == element)
            {
                if (TupleItem(field) == item)
                {
                    Trace(written.Value, function, bodies, depth, seen, into);
                }
            }
            else if (address.Parent is Call { Method.IsConstructor: true } construct && construct.Arguments[0] == address
                && item < construct.Arguments.Count)
            {
                Trace(construct.Arguments[item], function, bodies, depth, seen, into);
            }
            else
            {
                Unknown($"the tuple {variable.Name}, whose address is taken, so a call may set it", tuple, function, bodies,
                    into);
            }
        }
    }

    /// <summary>Follows element <paramref name="item"/> of a tuple value back to the argument that built it.</summary>
    private void Built(ILInstruction tuple, int item, ReadFunction function, AssemblyBodies bodies, int depth,
        HashSet<ILVariable> seen, List<FlowValue> into)
    {
        switch (tuple)
        {
            case NewObj built when IsTuple(built.Method.DeclaringType) && item - 1 < built.Arguments.Count:
                Trace(built.Arguments[item - 1], function, bodies, depth, seen, into);
                return;
            case IfInstruction choice when choice.ResultType != StackType.Void:
                Built(choice.TrueInst, item, function, bodies, depth, seen, into);
                Built(choice.FalseInst, item, function, bodies, depth, seen, into);
                return;
            case LdLoc { Variable: var other }:
                TupleVariable(other, tuple, item, function, bodies, depth, seen, into);
                return;
        }

        Unknown(Describe(tuple), tuple, function, bodies, into);
    }

    /// <summary>Why a method may have callers the scan cannot see, or null where every caller is in it.</summary>
    private string? WhyCallersUnseen(IMethod method)
    {
        if (IsVisibleOutside(method))
        {
            return "of a method visible outside its assembly";
        }

        if (InheritanceHelper.GetBaseMembers(method, true).Any())
        {
            return "of a method that overrides or implements another";
        }

        return _delegated.Contains(KeyOf(method)) ? "of a method made into a delegate" : null;
    }

    private static bool IsVisibleOutside(IMethod method)
    {
        if (!Outside(method.Accessibility))
        {
            return false;
        }

        for (ITypeDefinition? type = method.DeclaringTypeDefinition; type is not null; type = type.DeclaringTypeDefinition)
        {
            if (!Outside(type.Accessibility))
            {
                return false;
            }
        }

        return true;

        static bool Outside(Accessibility accessibility) =>
            accessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
    }

    private static int? TupleItem(IField field) =>
        IsTuple(field.DeclaringType) && field.Name is ['I', 't', 'e', 'm', >= '1' and <= '7']
            ? field.Name[4] - '0'
            : null;

    private static bool IsTuple(IType type) =>
        type.Namespace == "System" && type.Name == "ValueTuple" && type.TypeParameterCount is > 0 and < 8;

    private static string Describe(ILInstruction value) => value switch
    {
        LdNull => "null",
        LdObj { Target: LdsFlda { Field: var field } } => $"the field {field.DeclaringType.ReflectionName}::{field.Name}",
        LdObj { Target: LdFlda { Field: var field } } => $"the field {field.DeclaringType.ReflectionName}::{field.Name}",
        CallInstruction { Method: { DeclaringType.Kind: TypeKind.Delegate } } => "what a delegate returns",
        CallInstruction call => $"what {call.Method.DeclaringType.ReflectionName}::{call.Method.Name} returns",
        _ => $"a value the rule does not follow ({value.OpCode})",
    };

    private static void Unknown(string why, ILInstruction at, ReadFunction function, AssemblyBodies bodies,
        List<FlowValue> into) =>
        into.Add(new FlowValue(FlowKind.Unknown, why, bodies.PlaceOf(at, function), null));
}
