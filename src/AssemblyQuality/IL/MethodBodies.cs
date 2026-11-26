using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Bennewitz.Ninja.AssemblyQuality.Rules;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace Bennewitz.Ninja.AssemblyQuality.IL;

/// <summary>One use a method body makes of a member or a type, credited to the code the source wrote.</summary>
/// <param name="Kind">What the body does with it.</param>
/// <param name="Member">The member used; for a construction, the constructor, or none for <c>initobj</c>.</param>
/// <param name="Type">The type constructed, or the member's declaring type.</param>
/// <param name="CreditType">The type the source wrote the use in, in reflection syntax.</param>
/// <param name="CreditMethod">The method the source wrote it in, or null for a generated type's plumbing.</param>
/// <param name="Source">Its file and line, where the symbols give one.</param>
/// <param name="Function">The metadata token of the body the instruction was compiled into.</param>
/// <param name="Offset">Its IL offset in that body.</param>
/// <param name="Instruction">The instruction that makes the use, whose arguments a key is followed from.</param>
internal sealed record BodyUse(
    UseKind Kind,
    IMember? Member,
    IType Type,
    string CreditType,
    string? CreditMethod,
    SourceLine? Source,
    int Function,
    int Offset,
    ILInstruction Instruction);

/// <summary>Where an instruction is: the code the source wrote it in, its line, and where it was compiled.</summary>
/// <param name="CreditType">The type the source wrote it in, in reflection syntax.</param>
/// <param name="CreditMethod">The method the source wrote it in, or null for a generated type's plumbing.</param>
/// <param name="Source">Its file and line, where the symbols give one.</param>
/// <param name="Function">The metadata token of the body it was compiled into.</param>
/// <param name="Offset">Its IL offset in that body.</param>
internal sealed record BodyPlace(string CreditType, string? CreditMethod, SourceLine? Source, int Function, int Offset)
{
    /// <summary>The code it is in, as <c>Type::Method</c>, or a type for a closure's plumbing.</summary>
    internal string Method => CreditMethod is null ? CreditType : $"{CreditType}::{CreditMethod}";

    /// <summary>The code and, where the symbols give one, the line.</summary>
    public override string ToString() => Source is null ? $"{Method} at IL_{Offset:x4}" : $"{Method} ({Source})";
}

/// <summary>One method body as the reader read it.</summary>
/// <param name="Top">Its tree, the functions folded into it included.</param>
/// <param name="Method">The method it is the body of.</param>
/// <param name="Credit">The code the source wrote it in.</param>
internal sealed record ReadFunction(ILFunction Top, IMethod Method, (string Type, string? Method) Credit)
{
    /// <summary>
    /// Whether the tree holds code compiled into another method's body: a lambda folded into the method that wrote
    /// it, or a state machine's <c>MoveNext</c> into its stub. That reading knows the variables the other method's own
    /// reading sees only as a closure's fields.
    /// </summary>
    internal bool Folds(ILInstruction instruction) =>
        MethodBodies.FunctionToken(MethodBodies.OwnerOf(instruction, Top), Top) != MethodBodies.TokenOf(Method);
}

/// <summary>One assembly's method bodies, read as the decompiler's transformed ILAst.</summary>
/// <remarks>
/// ⛔ <b>It holds the assembly's file open until it is disposed.</b> The type system reads names from the file's
/// metadata when it is asked, so the file outlives the reading and closes only when the rule has matched against it.
/// </remarks>
internal sealed class AssemblyBodies(
    string name,
    PEFile file,
    ICompilation compilation,
    Ancestry ancestry,
    MethodBodies.Symbols symbols,
    IReadOnlyList<ReadFunction> functions,
    IReadOnlyList<BodyUse> uses) : IDisposable
{
    /// <summary>The assembly's simple name.</summary>
    internal string Name { get; } = name;

    /// <summary>Its type system, which configured names are resolved against.</summary>
    internal ICompilation Compilation { get; } = compilation;

    /// <summary>How many method bodies were read.</summary>
    internal int Bodies => Functions.Count;

    /// <summary>Every body read, as its tree.</summary>
    internal IReadOnlyList<ReadFunction> Functions { get; } = functions;

    /// <summary>Every use the bodies make, each once.</summary>
    internal IReadOnlyList<BodyUse> Uses { get; } = uses;

    /// <summary>Where <paramref name="instruction"/>, of <paramref name="function"/>'s tree, is.</summary>
    internal BodyPlace PlaceOf(ILInstruction instruction, ReadFunction function)
    {
        ILFunction owner = MethodBodies.OwnerOf(instruction, function.Top);
        int token = MethodBodies.FunctionToken(owner, function.Top);
        int offset = MethodBodies.OffsetOf(instruction);
        (string Type, string? Method) credit = MethodBodies.CreditIn(owner, function);
        return new BodyPlace(credit.Type, credit.Method, symbols.LineOf(token, offset), token, offset);
    }

    /// <summary>
    /// The names of <paramref name="type"/> and every type it derives from or implements, and whether any did not
    /// resolve; for a type with no definition to read, only its own name, unknown.
    /// </summary>
    internal (HashSet<string> Names, bool Unknown) AncestryOf(IType type)
    {
        if (type.GetDefinition() is { ParentModule: MetadataModule { MetadataFile: { } owner } } definition
            && definition.MetadataToken.Kind == System.Reflection.Metadata.HandleKind.TypeDefinition)
        {
            return ancestry.Of(owner, (TypeDefinitionHandle)definition.MetadataToken);
        }

        return ([type.ReflectionName], true);
    }

    public void Dispose()
    {
        symbols.Dispose();
        file.Dispose();
    }
}

/// <summary>Reads every method body of an assembly, plan 00011's IL reader.</summary>
/// <remarks>
/// <para>
/// ⭐ <b>The transformed tree, not the raw one.</b> The decompiler's own transforms rebuild what the compiler
/// lowered: a lambda's body sits inside the method that wrote it, an async method's inside its state machine's
/// <c>MoveNext</c>, and a struct construction is one <c>newobj</c> wherever it is stored. Measured over
/// <c>AppServices.AvaloniaUI</c> and <c>ScopedEditors.AvaloniaUI</c> in Debug and Release, plan 00011 step 2:
/// the raw tree of a Debug build gave some instructions no sequence point that the transformed one gives.
/// </para>
/// <para>
/// ⚠ <b>Every body is read on its own as well, and a use is known by where it was compiled.</b> A lambda's
/// body is read inside its method and again as the method it is; both readings give its instructions the
/// metadata token of that method and their own IL offsets, so a use is kept once. A body the transforms
/// cannot rebuild is read raw, and one the reader cannot read at all is named in the skips.
/// </para>
/// <para>
/// ⚠ <b>A generated body is credited by the compiler's names.</b> A lambda <c>&lt;M&gt;b__0_0</c>, a local
/// function <c>&lt;M&gt;g__L|0_0</c> and a state machine <c>&lt;M&gt;d__0</c> are credited to <c>M</c>; the
/// plumbing of a closure class, <c>&lt;&gt;c</c> or a display class, to the type that holds it. A top-level
/// generated type, <c>&lt;PrivateImplementationDetails&gt;</c> or a source generator's <c>file</c> type, is
/// read as the code it is, and compiled XAML, whose types and methods carry no angle brackets, as its own,
/// its symbols naming the markup file.
/// </para>
/// </remarks>
internal static partial class MethodBodies
{
    /// <summary>Reads <paramref name="assembly"/>'s bodies, or returns null with the reason in <paramref name="skipped"/>.</summary>
    [RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]
    internal static AssemblyBodies? Read(Assembly assembly, ICollection<string> skipped)
    {
        string name = assembly.GetName().Name ?? "?";
        string path = FileOf(assembly);

        if (path.Length == 0 || !File.Exists(path))
        {
            skipped.Add($"{name}: has no file to read, so none of its method bodies were read.");
            return null;
        }

        PEFile file;
        try
        {
            file = new PEFile(path, PEStreamOptions.PrefetchEntireImage, MetadataReaderOptions.None, null);
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or MetadataFileNotSupportedException)
        {
            skipped.Add($"{name}: {path} could not be read ({ex.GetType().Name}: {ex.Message}), so none of its "
                + "method bodies were read.");
            return null;
        }

        Symbols? symbols = null;
        try
        {
            var resolver = new UniversalAssemblyResolver(path, false, file.DetectTargetFrameworkId(), null,
                PEStreamOptions.PrefetchMetadata, MetadataReaderOptions.None);
            var settings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest)
            {
                ThrowOnAssemblyResolveErrors = false,
            };
            var typeSystem = new DecompilerTypeSystem(file, resolver, settings);
            symbols = Symbols.Open(file, path);

            var reader = new Reading(name, file, typeSystem, settings, symbols, skipped);
            reader.ReadAll();
            return new AssemblyBodies(name, file, typeSystem, new Ancestry(file, resolver), symbols, reader.Functions,
                reader.Uses);
        }
        catch
        {
            symbols?.Dispose();
            file.Dispose();
            throw;
        }
    }

    /// <summary>The file an assembly was loaded from, or empty when it has none.</summary>
    /// <remarks>
    /// ⚠ <b>An assembly bundled into a single file has no location</b>, so the reader names it in the skips
    /// rather than reading nothing silently.
    /// </remarks>
    [UnconditionalSuppressMessage("SingleFile", "IL3000",
        Justification = "An empty location is the case handled: the assembly is named in the skips.")]
    private static string FileOf(Assembly assembly) => assembly.Location;

    /// <summary>A lambda, local function or state machine name: <c>&lt;M&gt;</c>, a letter, two underscores.</summary>
    [GeneratedRegex(@"^<(?<method>.*)>(?<kind>[a-z])__", RegexOptions.CultureInvariant)]
    private static partial Regex Mangled();

    /// <summary>The source method a compiler-generated name was made for, or null when it names none.</summary>
    /// <remarks>
    /// A name such as <c>&lt;Main&gt;$</c> or <c>&lt;Clone&gt;$</c>, which the compiler gives a member the
    /// source wrote, is not a mangled one, so it is credited to itself.
    /// </remarks>
    internal static bool TryMangled(string name, out string method)
    {
        Match match = Mangled().Match(name);
        method = match.Success ? match.Groups["method"].Value : string.Empty;
        return match.Success;
    }

    /// <summary>Whether a type's name is the compiler's, as a closure class's or a state machine's is.</summary>
    private static bool IsGenerated(ITypeDefinition type) => type.Name.Contains('<', StringComparison.Ordinal);

    private sealed class Reading(
        string name,
        PEFile file,
        DecompilerTypeSystem typeSystem,
        DecompilerSettings settings,
        Symbols symbols,
        ICollection<string> skipped)
    {
        private readonly List<IILTransform> _transforms = CSharpDecompiler.GetILTransforms();
        private readonly HashSet<(int Function, int Offset, UseKind Kind, string Member)> _seen = [];

        internal List<ReadFunction> Functions { get; } = [];

        internal List<BodyUse> Uses { get; } = [];

        internal void ReadAll()
        {
            MetadataReader metadata = file.Metadata;
            MetadataModule module = typeSystem.MainModule;

            foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
            {
                foreach (MethodDefinitionHandle handle in metadata.GetTypeDefinition(typeHandle).GetMethods())
                {
                    MethodDefinition definition = metadata.GetMethodDefinition(handle);
                    if (definition.RelativeVirtualAddress == 0)
                    {
                        continue;
                    }

                    IMethod method = module.GetDefinition(handle);
                    if (Credit(method) is not { } credit)
                    {
                        skipped.Add($"{name}: {method.DeclaringType.ReflectionName}::{method.Name} has a generated "
                            + "name the reader cannot take apart, so its uses were credited to nothing and not read.");
                        continue;
                    }

                    if (ReadBody(handle, definition, method) is not { } function)
                    {
                        continue;
                    }

                    var read = new ReadFunction(function, method, credit);
                    Functions.Add(read);
                    Collect(read);
                }
            }
        }

        /// <summary>The body as the transforms leave it, raw where they cannot, or null with the reason skipped.</summary>
        private ILFunction? ReadBody(MethodDefinitionHandle handle, MethodDefinition definition, IMethod method)
        {
            var context = new GenericContext(method.DeclaringTypeDefinition?.TypeParameters, method.TypeParameters);
            ILFunction raw;
            try
            {
                raw = Load(handle, definition, context);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                skipped.Add($"{name}: the body of {method.DeclaringType.ReflectionName}::{method.Name} could not be read "
                    + $"({ex.GetType().Name}: {ex.Message}), so its uses were not examined.");
                return null;
            }

            try
            {
                ILFunction function = Load(handle, definition, context);
                var transformContext = new ILTransformContext(function, typeSystem, null, settings);
                foreach (IILTransform transform in _transforms)
                {
                    transform.Run(function, transformContext);
                }

                return function;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return raw;
            }
        }

        private ILFunction Load(MethodDefinitionHandle handle, MethodDefinition definition, GenericContext context) =>
            new ILReader(typeSystem.MainModule).ReadIL(handle, file.GetMethodBody(definition.RelativeVirtualAddress),
                context, ILFunctionKind.TopLevelFunction, default);

        private void Collect(ReadFunction read)
        {
            ILFunction top = read.Top;
            foreach (ILInstruction instruction in top.Descendants)
            {
                ILFunction owner = OwnerOf(instruction, top);
                int function = FunctionToken(owner, top);
                (string Type, string? Method) credit = CreditIn(owner, read);

                switch (instruction)
                {
                    case NewObj newObj when newObj.Method.DeclaringType.Kind != TypeKind.Delegate:
                        Add(UseKind.Construction, newObj.Method, newObj.Method.DeclaringType, instruction, function, credit);
                        break;
                    case CallInstruction call when call.Method.IsConstructor:
                        bool chain = call.Arguments.Count > 0 && call.Arguments[0].MatchLdThis()
                            && owner.Method is { IsConstructor: true };
                        Add(chain ? UseKind.Chain : UseKind.Construction, call.Method, call.Method.DeclaringType,
                            instruction, function, credit);
                        break;
                    case CallInstruction call:
                        bool getter = call.Method.AccessorOwner is IProperty
                            && call.Method.AccessorKind == MethodSemanticsAttributes.Getter;
                        Add(getter ? UseKind.Read : UseKind.Call, call.Method, call.Method.DeclaringType, instruction,
                            function, credit);
                        break;
                    case LdFtn ldFtn:
                        Add(UseKind.Delegate, ldFtn.Method, ldFtn.Method.DeclaringType, instruction, function, credit);
                        break;
                    case LdVirtFtn ldVirtFtn:
                        Add(UseKind.Delegate, ldVirtFtn.Method, ldVirtFtn.Method.DeclaringType, instruction, function, credit);
                        break;
                    case LdsFlda field when !IsWritten(instruction):
                        Add(UseKind.Read, field.Field, field.Field.DeclaringType, instruction, function, credit);
                        break;
                    case LdFlda field when !IsWritten(instruction):
                        Add(UseKind.Read, field.Field, field.Field.DeclaringType, instruction, function, credit);
                        break;
                    case DefaultValue value when value.Type.IsReferenceType == false && value.Type.Kind != TypeKind.TypeParameter:
                        Add(UseKind.Construction, null, value.Type, instruction, function, credit);
                        break;
                }
            }
        }

        /// <summary>Whether a field address is the target of a store, which writes the field rather than reads it.</summary>
        private static bool IsWritten(ILInstruction address) =>
            address.Parent is StObj store && store.Target == address;

        private void Add(UseKind kind, IMember? member, IType type, ILInstruction instruction, int function,
            (string Type, string? Method) credit)
        {
            int offset = OffsetOf(instruction);
            string key = member is null ? type.ReflectionName : $"{member.DeclaringType.ReflectionName}::{member.Name}";
            if (!_seen.Add((function, offset, kind, key)))
            {
                return;
            }

            Uses.Add(new BodyUse(kind, member, type, credit.Type, credit.Method, symbols.LineOf(function, offset),
                function, offset, instruction));
        }
    }

    /// <summary>The function an instruction is in: the innermost one folded into the tree, or the tree's own.</summary>
    internal static ILFunction OwnerOf(ILInstruction instruction, ILFunction top) =>
        instruction.Ancestors.OfType<ILFunction>().FirstOrDefault() ?? top;

    /// <summary>The code the source wrote a function of <paramref name="read"/>'s tree in.</summary>
    internal static (string Type, string? Method) CreditIn(ILFunction owner, ReadFunction read) =>
        owner == read.Top
            ? read.Credit
            : owner.Method is { } nested && Credit(nested) is { } credit ? credit : read.Credit;

    /// <summary>An instruction's IL offset, or its nearest ancestor's where the transforms gave it none.</summary>
    internal static int OffsetOf(ILInstruction instruction)
    {
        for (ILInstruction? at = instruction; at is not null; at = at.Parent)
        {
            if (!at.ILRangeIsEmpty)
            {
                return at.StartILOffset;
            }
        }

        return -1;
    }

    /// <summary>The token of the body an instruction was compiled into: a nested function's own, or a state machine's.</summary>
    internal static int FunctionToken(ILFunction owner, ILFunction top)
    {
        if (owner == top && top.MoveNextMethod is { } moveNext && !moveNext.MetadataToken.IsNil)
        {
            return MetadataTokens.GetToken(moveNext.MetadataToken);
        }

        return owner.Method is { } method ? TokenOf(method) : 0;
    }

    /// <summary>A method's metadata token, or zero for one that has none.</summary>
    internal static int TokenOf(IMethod method) =>
        method.MetadataToken.IsNil ? 0 : MetadataTokens.GetToken(method.MetadataToken);

    /// <summary>
    /// The type and the method the source wrote a body in, or null for a generated name the reader cannot take
    /// apart.
    /// </summary>
    internal static (string Type, string? Method)? Credit(IMethod method)
    {
        ITypeDefinition? type = method.DeclaringTypeDefinition;
        if (type is null)
        {
            return null;
        }

        string? fromMethod = null;
        bool namesNoMethod = false;
        if (method.Name.Contains('<', StringComparison.Ordinal))
        {
            if (TryMangled(method.Name, out string made))
            {
                namesNoMethod = made.Length == 0;
                fromMethod = namesNoMethod ? null : made;
            }
            else
            {
                fromMethod = method.Name;
            }
        }

        string? fromType = null;
        bool inGenerated = false;
        while (IsGenerated(type) && type.DeclaringTypeDefinition is { } outer)
        {
            inGenerated = true;
            if (fromType is null && TryMangled(type.Name, out string made) && made.Length > 0)
            {
                fromType = made;
            }

            type = outer;
        }

        // A name that names no method, as an iterator's finally block <>m__Finally1 does, is its state machine's
        // plumbing, credited as the type that holds it is; only outside a generated type can it not be taken apart.
        if (namesNoMethod && !inGenerated)
        {
            return null;
        }

        string? credited = fromMethod ?? fromType ?? (inGenerated ? null : method.Name);
        return (type.ReflectionName, credited);
    }

    /// <summary>The sequence points of a portable PDB, embedded in the DLL or beside it.</summary>
    internal sealed class Symbols : IDisposable
    {
        private readonly MetadataReaderProvider? _provider;
        private readonly MetadataReader? _reader;

        private Symbols(MetadataReaderProvider? provider)
        {
            _provider = provider;
            _reader = provider?.GetMetadataReader();
        }

        internal static Symbols Open(PEFile file, string path)
        {
            try
            {
                foreach (DebugDirectoryEntry entry in file.Reader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                    {
                        return new Symbols(file.Reader.ReadEmbeddedPortablePdbDebugDirectoryData(entry));
                    }
                }

                string pdb = Path.ChangeExtension(path, ".pdb");
                return File.Exists(pdb)
                    ? new Symbols(MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(pdb)))
                    : new Symbols(null);
            }
            catch (Exception ex) when (ex is BadImageFormatException or IOException)
            {
                return new Symbols(null);
            }
        }

        /// <summary>The line of the last visible sequence point at or before <paramref name="offset"/>, or the first.</summary>
        internal SourceLine? LineOf(int function, int offset)
        {
            if (_reader is null || function == 0)
            {
                return null;
            }

            MethodDefinitionHandle handle = MetadataTokens.MethodDefinitionHandle(function & 0xFFFFFF);
            SequencePoint? best = null;
            SequencePoint? first = null;
            try
            {
                foreach (SequencePoint point in _reader.GetMethodDebugInformation(handle).GetSequencePoints())
                {
                    if (point.IsHidden)
                    {
                        continue;
                    }

                    first ??= point;
                    if (point.Offset <= offset)
                    {
                        best = point;
                    }
                }
            }
            catch (BadImageFormatException)
            {
                return null;
            }

            return (best ?? first) is { } found
                ? new SourceLine(_reader.GetString(_reader.GetDocument(found.Document).Name), found.StartLine)
                : null;
        }

        public void Dispose() => _provider?.Dispose();
    }
}
