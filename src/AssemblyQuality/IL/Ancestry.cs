using System.Collections.Immutable;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.Metadata;

namespace Bennewitz.Ninja.AssemblyQuality.IL;

/// <summary>Every type a type derives from or implements, read from metadata across its references' closure.</summary>
/// <remarks>
/// <para>
/// ⛔ <b>The decompiler's type system loads the scanned assembly's own references, and no further.</b> It follows a
/// type forwarder past them, but not a reference one of them makes. Measured, plan 00011 step 3: every Avalonia control
/// in <c>AppServices.AvaloniaUI</c> implements <c>INotifyPropertyChanged</c>, which lives in <c>System.ObjectModel</c>,
/// which only Avalonia references, so the type system gave each control an unknown ancestor and no subtype match
/// could be decided. So ancestry is read here, a reference at a time, through the same resolver.
/// </para>
/// <para>
/// ⚠ <b>An ancestor that resolves nowhere makes the answer unknown</b>, never false: a type that does not resolve
/// could be the one asked about.
/// </para>
/// </remarks>
internal sealed class Ancestry(MetadataFile main, IAssemblyResolver resolver)
{
    private readonly Dictionary<string, MetadataFile?> _files = new(StringComparer.OrdinalIgnoreCase)
    {
        [main.Metadata.GetString(main.Metadata.GetAssemblyDefinition().Name)] = main,
    };

    private readonly Dictionary<MetadataFile, Dictionary<string, TypeDefinitionHandle>> _definitions = [];
    private readonly Dictionary<MetadataFile, Dictionary<string, ExportedTypeHandle>> _exported = [];
    private readonly Dictionary<(MetadataFile File, TypeDefinitionHandle Type), (HashSet<string> Names, bool Unknown)> _memo = [];

    /// <summary>
    /// The full names, in reflection syntax, of <paramref name="type"/> and every type it derives from or implements,
    /// and whether any of them did not resolve.
    /// </summary>
    internal (HashSet<string> Names, bool Unknown) Of(MetadataFile file, TypeDefinitionHandle type)
    {
        if (_memo.TryGetValue((file, type), out var known))
        {
            return known;
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        _memo[(file, type)] = (names, false);
        bool unknown = Walk(file, type, names, []);
        _memo[(file, type)] = (names, unknown);
        return (names, unknown);
    }

    private bool Walk(MetadataFile file, TypeDefinitionHandle handle, HashSet<string> names, HashSet<(MetadataFile, TypeDefinitionHandle)> seen)
    {
        if (!seen.Add((file, handle)))
        {
            return false;
        }

        MetadataReader metadata = file.Metadata;
        names.Add(NameOf(metadata, handle));
        TypeDefinition definition = metadata.GetTypeDefinition(handle);
        bool unknown = false;

        if (!definition.BaseType.IsNil)
        {
            unknown |= Follow(file, definition.BaseType, names, seen);
        }

        foreach (InterfaceImplementationHandle implementation in definition.GetInterfaceImplementations())
        {
            unknown |= Follow(file, metadata.GetInterfaceImplementation(implementation).Interface, names, seen);
        }

        return unknown;
    }

    private bool Follow(MetadataFile file, EntityHandle handle, HashSet<string> names, HashSet<(MetadataFile, TypeDefinitionHandle)> seen)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return Walk(file, (TypeDefinitionHandle)handle, names, seen);
            case HandleKind.TypeSpecification:
                EntityHandle generic = file.Metadata.GetTypeSpecification((TypeSpecificationHandle)handle)
                    .DecodeSignature(GenericTypeOf.Instance, null);
                return generic.IsNil || Follow(file, generic, names, seen);
            case HandleKind.TypeReference:
                if (Resolve(file, (TypeReferenceHandle)handle) is ({ } target, var definition))
                {
                    return Walk(target, definition, names, seen);
                }

                names.Add(NameOf(file.Metadata, (TypeReferenceHandle)handle));
                return true;
            default:
                return true;
        }
    }

    /// <summary>The file and definition a type reference names, following type forwarders, or null.</summary>
    private (MetadataFile? File, TypeDefinitionHandle Type) Resolve(MetadataFile file, TypeReferenceHandle handle)
    {
        MetadataReader metadata = file.Metadata;
        TypeReference reference = metadata.GetTypeReference(handle);
        string name = NameOf(metadata, handle);

        MetadataFile? target = reference.ResolutionScope.Kind switch
        {
            HandleKind.AssemblyReference => FileOf(file, (AssemblyReferenceHandle)reference.ResolutionScope),
            HandleKind.TypeReference => Resolve(file, (TypeReferenceHandle)reference.ResolutionScope).File,
            _ => file,
        };

        return target is null ? (null, default) : Find(target, name, 0);
    }

    private (MetadataFile? File, TypeDefinitionHandle Type) Find(MetadataFile file, string name, int depth)
    {
        if (depth > 8)
        {
            return (null, default);
        }

        if (Definitions(file).TryGetValue(name, out TypeDefinitionHandle definition))
        {
            return (file, definition);
        }

        if (Exported(file).TryGetValue(name, out ExportedTypeHandle exported)
            && file.Metadata.GetExportedType(exported).Implementation is { Kind: HandleKind.AssemblyReference } implementation
            && FileOf(file, (AssemblyReferenceHandle)implementation) is { } forwarded)
        {
            return Find(forwarded, name, depth + 1);
        }

        return (null, default);
    }

    private MetadataFile? FileOf(MetadataFile from, AssemblyReferenceHandle handle)
    {
        string name = from.Metadata.GetString(from.Metadata.GetAssemblyReference(handle).Name);
        if (_files.TryGetValue(name, out MetadataFile? known))
        {
            return known;
        }

        MetadataFile? resolved = null;
        try
        {
            if (from.AssemblyReferences.FirstOrDefault(r => r.Name == name) is { } reference)
            {
                resolved = resolver.Resolve(reference);
            }
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or MetadataFileNotSupportedException)
        {
            resolved = null;
        }

        _files[name] = resolved;
        return resolved;
    }

    private Dictionary<string, TypeDefinitionHandle> Definitions(MetadataFile file)
    {
        if (!_definitions.TryGetValue(file, out var index))
        {
            index = new Dictionary<string, TypeDefinitionHandle>(StringComparer.Ordinal);
            foreach (TypeDefinitionHandle handle in file.Metadata.TypeDefinitions)
            {
                index.TryAdd(NameOf(file.Metadata, handle), handle);
            }

            _definitions[file] = index;
        }

        return index;
    }

    private Dictionary<string, ExportedTypeHandle> Exported(MetadataFile file)
    {
        if (!_exported.TryGetValue(file, out var index))
        {
            index = new Dictionary<string, ExportedTypeHandle>(StringComparer.Ordinal);
            foreach (ExportedTypeHandle handle in file.Metadata.ExportedTypes)
            {
                ExportedType type = file.Metadata.GetExportedType(handle);
                string space = file.Metadata.GetString(type.Namespace);
                string simple = file.Metadata.GetString(type.Name);
                index.TryAdd(space.Length == 0 ? simple : $"{space}.{simple}", handle);
            }

            _exported[file] = index;
        }

        return index;
    }

    private static string NameOf(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        TypeDefinition definition = metadata.GetTypeDefinition(handle);
        string simple = metadata.GetString(definition.Name);
        TypeDefinitionHandle outer = definition.GetDeclaringType();
        if (!outer.IsNil)
        {
            return $"{NameOf(metadata, outer)}+{simple}";
        }

        string space = metadata.GetString(definition.Namespace);
        return space.Length == 0 ? simple : $"{space}.{simple}";
    }

    private static string NameOf(MetadataReader metadata, TypeReferenceHandle handle)
    {
        TypeReference reference = metadata.GetTypeReference(handle);
        string simple = metadata.GetString(reference.Name);
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return $"{NameOf(metadata, (TypeReferenceHandle)reference.ResolutionScope)}+{simple}";
        }

        string space = metadata.GetString(reference.Namespace);
        return space.Length == 0 ? simple : $"{space}.{simple}";
    }

    /// <summary>Decodes a type specification to the generic type it instantiates, or nil for any other shape.</summary>
    private sealed class GenericTypeOf : ISignatureTypeProvider<EntityHandle, object?>
    {
        internal static readonly GenericTypeOf Instance = new();

        public EntityHandle GetGenericInstantiation(EntityHandle genericType, ImmutableArray<EntityHandle> typeArguments) => genericType;

        public EntityHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle;

        public EntityHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle;

        public EntityHandle GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => default;

        public EntityHandle GetArrayType(EntityHandle elementType, ArrayShape shape) => default;

        public EntityHandle GetByReferenceType(EntityHandle elementType) => default;

        public EntityHandle GetFunctionPointerType(MethodSignature<EntityHandle> signature) => default;

        public EntityHandle GetGenericMethodParameter(object? genericContext, int index) => default;

        public EntityHandle GetGenericTypeParameter(object? genericContext, int index) => default;

        public EntityHandle GetModifiedType(EntityHandle modifier, EntityHandle unmodifiedType, bool isRequired) => unmodifiedType;

        public EntityHandle GetPinnedType(EntityHandle elementType) => default;

        public EntityHandle GetPointerType(EntityHandle elementType) => default;

        public EntityHandle GetPrimitiveType(PrimitiveTypeCode typeCode) => default;

        public EntityHandle GetSZArrayType(EntityHandle elementType) => default;
    }
}
