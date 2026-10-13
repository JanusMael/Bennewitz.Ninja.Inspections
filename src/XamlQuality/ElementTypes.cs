using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bennewitz.Ninja.XamlQuality;

/// <summary>What an element's local name resolved to.</summary>
internal enum ElementKind
{
    /// <summary>No type of that name that loads, in the scanned assemblies or the assemblies they reference.</summary>
    Missing,

    /// <summary>More than one type of that name takes part in focus, so which one is meant is unknown.</summary>
    Ambiguous,

    /// <summary>A type that takes no part in focus: a template, a definition, a brush, a setter.</summary>
    Other,

    /// <summary>A type with a <c>FocusableProperty</c>, so the framework's focus model applies to it.</summary>
    InputElement,
}

/// <summary>An element's local name, resolved.</summary>
/// <param name="Kind">What the name resolved to.</param>
/// <param name="Type">The type, when <paramref name="Kind"/> is <see cref="ElementKind.Other"/> or <see cref="ElementKind.InputElement"/>.</param>
internal readonly record struct ElementType(ElementKind Kind, Type? Type);

/// <summary>How the XAML compiler takes <c>Owner.Name</c> written on an element, at 12.1.3.</summary>
internal enum OwnerSpelling
{
    /// <summary>The owner is the element's type or a base: the instance CLR property of the name found up the owner's chain.</summary>
    Instance,

    /// <summary>
    /// Both are <c>AvaloniaObject</c>s, the owner is outside the element's chain, and the first CLR property of the name up
    /// its chain is an instance one, whose declaring type's field holds no <c>AttachedProperty</c>: that property, bound on
    /// the element, which only a binding gives.
    /// </summary>
    AttachedInstance,

    /// <summary>The owner declares a public static <c>Set</c> or <c>Get</c> accessor of the name that takes the element: an attached property, which any value sets.</summary>
    Attached,

    /// <summary>None of those, so the markup does not load.</summary>
    None,
}

/// <summary>
/// The compiled types a scan's markup names, and what the framework they belong to says about them,
/// read through reflection over the assemblies a consumer supplied.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Names, not namespaces.</b> An element resolves by its local name, the way every rule here
/// matches markup, so one reading serves Avalonia's markup and WPF's. The supplied assemblies are
/// searched first and the assemblies they reference second, so a consumer's own control wins over a
/// framework type of the same name, and handing over the application's assembly is enough to reach
/// the framework it is built on.
/// </para>
/// <para>
/// ⛔ <b>A per-type default is registered by the type's static constructor, which loading an
/// assembly does not run.</b> Read cold, every control reports the base default, which for
/// <c>Focusable</c> is <c>false</c> for every control there is: plausible, and wrong. So each class
/// constructor in a type's chain is run before its default is read. Measured on Avalonia 12.1.3: read
/// cold, <c>ListBoxItem</c>, <c>Button</c> and <c>TextBox</c> all report <c>false</c>; after their
/// constructors, <c>true</c>. The entry in <c>docs/avalonia-gotchas.md</c> has the rest.
/// </para>
/// <para>
/// ⚠ <b>Running a class constructor runs the consumer's code.</b> A control's static constructor
/// registers its properties and little else, and a test process that references the control has
/// already agreed to load it. A constructor that throws leaves the type unreadable, and the caller
/// says so rather than guessing.
/// </para>
/// </remarks>
internal sealed class ElementTypes
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private readonly Dictionary<string, List<Type>> _supplied = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Type>> _referenced = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, (bool? Focusable, string? Why)> _focusable = [];
    private readonly Dictionary<Type, Type?> _containers = [];
    private readonly Dictionary<Type, (Type Key, bool Known)> _styleKeys = [];
    private readonly Dictionary<string, (string Source, string Cause)> _notLoaded = new(StringComparer.Ordinal);
    private readonly List<string> _unloadedReferences = [];
    private readonly Dictionary<(Type Type, string Field), (bool Readable, object? Value)> _defaults = [];
    private readonly Dictionary<(Type Owner, Type Target, string Name), (OwnerSpelling Spelling, bool Read, object? Property)> _ownerProperties = [];
    private readonly Dictionary<(Type Owner, string Name), (bool Read, object? Property)> _publicProperties = [];
    private readonly Dictionary<Type, bool> _codeSetsPaint = [];
    private readonly Dictionary<Type, bool> _drawsItself = [];
    private readonly Dictionary<Type, bool> _compiledMarkup = [];

    internal ElementTypes(IReadOnlyList<Assembly> assemblies)
    {
        HashSet<string> indexed = new(StringComparer.Ordinal);

        foreach (Assembly assembly in assemblies)
        {
            if (indexed.Add(assembly.GetName().Name ?? string.Empty))
            {
                LoadedTypes types = LoadedTypes.Of(assembly);
                Index(types.Loaded, _supplied);
                foreach ((string name, string cause) in types.NotLoaded)
                {
                    _notLoaded.TryAdd(name, (types.Source, cause));
                }
            }
        }

        foreach (Assembly assembly in assemblies)
        {
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                if (!indexed.Add(reference.Name ?? string.Empty))
                {
                    continue;
                }

                if (Load(reference) is { } loaded)
                {
                    Index(LoadedTypes.Of(loaded).Loaded, _referenced);
                }
                else
                {
                    _unloadedReferences.Add(reference.Name ?? reference.FullName);
                }
            }
        }
    }

    /// <summary>
    /// Why <paramref name="localName"/> resolved to no type, when that is known, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// ⛔ <b>"Not a type in the scanned assemblies" is wrong when the scan was given it.</b> A
    /// control whose dependencies are not beside its assembly does not load, and a message that told
    /// the consumer to pass the assembly that defines it sent them to do what they had already done.
    /// So a type an assembly defines but could not load is named with what stopped it, and when an
    /// assembly the scanned ones reference could not be loaded, that is said instead.
    /// </remarks>
    internal string? WhyUnresolved(string localName)
    {
        if (_notLoaded.TryGetValue(localName, out (string Source, string Cause) notLoaded))
        {
            return $"{localName}, defined in {notLoaded.Source}, could not be loaded: {notLoaded.Cause}";
        }

        return _unloadedReferences.Count == 0
            ? null
            : $"{localName} is not a type in the scanned assemblies, and {LoadedTypes.List(_unloadedReferences)}, "
              + "which they reference, could not be loaded";
    }

    /// <summary>What <paramref name="localName"/> names: the supplied assemblies first, then what they reference.</summary>
    /// <remarks>
    /// ⚠ A type that takes part in focus outranks one that does not, whichever assembly holds it. A
    /// consumer's model class that happens to share a control's name must not hide the control an
    /// element in markup means, because a hidden <c>Button</c> is a focusable element the rule would miss.
    /// </remarks>
    internal ElementType Resolve(string localName)
    {
        ElementType supplied = Resolve(localName, _supplied);
        if (supplied.Kind is ElementKind.InputElement or ElementKind.Ambiguous)
        {
            return supplied;
        }

        ElementType referenced = Resolve(localName, _referenced);
        if (referenced.Kind is ElementKind.InputElement or ElementKind.Ambiguous || supplied.Kind == ElementKind.Missing)
        {
            return referenced;
        }

        return supplied;
    }

    /// <summary>
    /// The type called <paramref name="name"/> in the CLR namespace <paramref name="clrNamespace"/>: from
    /// the supplied assemblies first, then what they reference, then the core library; <c>null</c> when
    /// none has one, or more than one assembly at the same level does.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>By namespace as well as name</b>, because an <c>x:DataType</c> names its type through the
    /// <c>xmlns</c> that says which namespace it is in, and a consumer's <c>Person</c> is not another
    /// library's. <c>System</c>'s types are reached through facades that forward them rather than
    /// define them, so the core library is asked last.
    /// </remarks>
    internal Type? TypeNamed(string clrNamespace, string name)
    {
        foreach (Dictionary<string, List<Type>> index in (Dictionary<string, List<Type>>[])[_supplied, _referenced])
        {
            if (index.TryGetValue(name, out List<Type>? types))
            {
                Type[] matching = [.. types.Distinct().Where(type => Safely(() => type.Namespace == clrNamespace, false))];
                if (matching.Length > 0)
                {
                    return matching.Length == 1 ? matching[0] : null;
                }
            }
        }

        return Safely(() => typeof(object).Assembly.GetType(clrNamespace + "." + name), (Type?)null);
    }

    /// <summary>
    /// The type a control's implicit theme is keyed by: the one its most-derived <c>StyleKeyOverride</c>
    /// returns with a <c>typeof</c>, else the control's own type.
    /// </summary>
    /// <remarks>
    /// ⭐ <b>A custom control usually borrows its base's theme this way</b>, as a WPF-to-Avalonia
    /// port's <c>LinesListBox</c> does with <c>StyleKeyOverride =&gt; typeof(ListBox)</c>, and then the
    /// implicit theme that reaches it is its base's, not one keyed by its own name. The <c>typeof</c> is
    /// an <c>ldtoken</c> in the getter's IL, read without running the control.
    /// </remarks>
    internal Type StyleKeyOf(Type type) => StyleKeyRead(type).Key;

    /// <summary>
    /// Whether <see cref="StyleKeyOf"/> read <paramref name="type"/>'s style key rather than took its own type for it:
    /// <c>false</c> where the <c>StyleKeyOverride</c> its chain overrides returns no single <c>typeof</c>, or cannot be read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>An override that returns a type through a field, or one it computes, names a key its getter's IL does not
    /// show</b>: measured, a list whose <c>StyleKeyOverride</c> returned <c>ListBox</c> through a static field took none of
    /// the setters of a theme for its own type. The root declaration returns <c>GetType()</c>, the control's own type.
    /// </remarks>
    internal bool StyleKeyKnown(Type type) => StyleKeyRead(type).Known;

    private (Type Key, bool Known) StyleKeyRead(Type type)
    {
        if (_styleKeys.TryGetValue(type, out (Type Key, bool Known) known))
        {
            return known;
        }

        (Type Key, bool Known) read = Safely(() =>
        {
            for (Type? link = type; link is not null; link = link.BaseType)
            {
                PropertyInfo? property = link.GetProperties(Declared).FirstOrDefault(candidate => candidate.Name == "StyleKeyOverride");
                if (property?.GetMethod is not { } getter)
                {
                    continue;
                }

                Type[] named = [.. CompiledStrings.TypeOfsIn(getter).Distinct()];
                return named.Length == 1 ? (named[0], true)
                    : getter.GetBaseDefinition().DeclaringType == link ? (type, true)
                    : (type, false);
            }

            return (type, true);
        }, (type, false));

        _styleKeys[type] = read;
        return read;
    }

    /// <summary>Whether the framework's focus model applies to <paramref name="type"/>; <c>false</c> when unreadable.</summary>
    internal static bool IsInputElement(Type type) => Safely(() => FocusablePropertyOf(type) is not null, false);

    /// <summary>Whether <paramref name="type"/> presents a collection through generated item containers.</summary>
    /// <remarks>By name, as XAML frameworks spell it, so no framework assembly is referenced.</remarks>
    internal static bool IsItemsControl(Type type) => Safely(() => Chain(type).Any(link => link.Name == "ItemsControl"), false);

    /// <summary>
    /// Whether <paramref name="type"/> draws itself through a template it is given; <c>true</c> when
    /// unreadable, since a template is where focus the markup cannot show would hide.
    /// </summary>
    internal static bool IsTemplated(Type type) => HasProperty(type, "Template");

    /// <summary>
    /// Whether <paramref name="type"/> has a property called <paramref name="name"/> a consumer can set;
    /// <c>true</c> when unreadable, so that the caller asks for what the property would hold.
    /// </summary>
    internal static bool HasProperty(Type type, string name) =>
        Safely(() => type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(property => property.Name == name), true);

    /// <summary>Whether <paramref name="type"/> can stand where <paramref name="container"/> is expected; <c>false</c> when unreadable.</summary>
    internal static bool IsAssignable(Type container, Type type) => Safely(() => container.IsAssignableFrom(type), false);

    /// <summary>
    /// The type in <paramref name="type"/>'s chain called <paramref name="name"/>, most derived first, as
    /// <see cref="NamesOf"/> names them; <c>null</c> when none is, or the chain cannot be read.
    /// </summary>
    internal static Type? InChain(Type type, string name) =>
        Safely(() => Chain(type).TakeWhile(link => link != typeof(object)).FirstOrDefault(link => link.Name == name), (Type?)null);

    /// <summary>
    /// Every type in <paramref name="type"/>'s chain called <paramref name="name"/>, most derived first; none when the
    /// chain cannot be read.
    /// </summary>
    internal static IReadOnlyList<Type> AllInChain(Type type, string name) =>
        Safely<IReadOnlyList<Type>>(() => [.. Chain(type).TakeWhile(link => link != typeof(object)).Where(link => link.Name == name)], []);

    /// <summary>The names of <paramref name="type"/> and its bases, as far as they can be read, without <c>Object</c>.</summary>
    internal static IReadOnlyCollection<string> NamesOf(Type type)
    {
        List<string> names = [];
        Safely(() =>
        {
            foreach (Type link in Chain(type).TakeWhile(link => link != typeof(object)))
            {
                names.Add(link.Name);
            }

            return true;
        }, false);

        return names;
    }

    /// <summary>
    /// Whether <paramref name="exception"/> is what reflection throws for a type whose dependencies
    /// cannot be loaded, the one failure a rule reading someone else's build output has to expect.
    /// </summary>
    internal static bool IsUnreadable(Exception exception) =>
        exception is FileNotFoundException or FileLoadException or TypeLoadException or BadImageFormatException
            or ReflectionTypeLoadException or MissingMemberException or NotSupportedException;

    /// <summary>A reflective read that an unloadable dependency turns into <paramref name="whenUnreadable"/>, never an exception.</summary>
    private static T Safely<T>(Func<T> read, T whenUnreadable)
    {
        try
        {
            return read();
        }
        catch (Exception ex) when (IsUnreadable(ex))
        {
            return whenUnreadable;
        }
    }

    /// <summary>
    /// The value the framework gives <c>Focusable</c> on <paramref name="type"/> when nothing sets it,
    /// or <c>null</c> with <paramref name="why"/> when it cannot be read.
    /// </summary>
    internal bool? FocusableByDefault(Type type, out string? why)
    {
        if (!_focusable.TryGetValue(type, out (bool? Focusable, string? Why) known))
        {
            known = ReadFocusable(type);
            _focusable[type] = known;
        }

        why = known.Why;
        return known.Focusable;
    }

    /// <summary>
    /// Whether the framework gives <paramref name="type"/> a <c>Background</c> or a <c>Fill</c> that is not
    /// null when nothing sets it: <c>false</c> when both defaults are null or it has neither property, and
    /// <c>null</c> when either cannot be read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Read from the property's metadata after every class constructor in the type's chain has
    /// run</b>, as <see cref="FocusableByDefault"/> reads <c>Focusable</c>, because a control that paints
    /// by default says so in its static constructor, with <c>OverrideDefaultValue</c>. Measured on
    /// Avalonia 12.1.3, a shape whose static constructor made its <c>Fill</c> <c>Transparent</c> took the
    /// click as a panel whose constructor did that to its <c>Background</c> did.
    /// </remarks>
    internal bool? PaintsByDefault(Type type)
    {
        bool? background = PaintedByDefault(type, "BackgroundProperty");
        bool? fill = PaintedByDefault(type, "FillProperty");
        return background is true || fill is true ? true : background is null || fill is null ? null : false;
    }

    /// <summary>
    /// The default <paramref name="type"/>'s metadata gives the property in its static field
    /// <paramref name="field"/>, as text, read as <see cref="PaintsByDefault"/> reads a brush's, after every
    /// class constructor in its chain has run; <c>null</c> when it has no such field, its default is null, or
    /// it cannot be read.
    /// </summary>
    internal string? DefaultText(Type type, string field) =>
        DefaultOf(type, field) is (true, { } value) ? Safely(() => value.ToString(), (string?)null) : null;

    /// <summary>
    /// The property an element of <paramref name="type"/> sets when it writes <paramref name="name"/> alone, as
    /// <see cref="OwnerPropertyOn"/> reads it with the type as its own owner: <c>(true, null)</c> where it sets none a
    /// field holds, and <c>(false, null)</c> where that cannot be read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A property is the object a field holds, and many types may hold one.</b> At 12.1.3 <c>Panel</c>,
    /// <c>ContentPresenter</c>, <c>TemplatedControl</c>, <c>TextBlock</c>, <c>TextPresenter</c> and <c>TextElement</c>
    /// each hold <c>Border</c>'s <c>BackgroundProperty</c>, which a styled property's <c>AddOwner</c> returns itself,
    /// and a direct property's <c>AddOwner</c> returns a copy that <c>AvaloniaProperty.Equals</c> takes for it, by
    /// its <c>Id</c>. So two spellings name one property when the fields the compiler takes for them hold one object,
    /// as <c>Equals</c> compares it, whatever their names: an attached property of the same name, such as a
    /// consumer's <c>Paint.Background</c>, is another.
    /// </remarks>
    internal (bool Read, object? Property) OwnPropertyOf(Type type, string name)
    {
        (OwnerSpelling spelling, bool read, object? property) = OwnerPropertyOn(type, type, name);
        return spelling == OwnerSpelling.None ? (read, null) : (read, property);
    }

    /// <summary>
    /// How the XAML compiler takes <c>Owner.Name</c> written on an element of <paramref name="target"/>, with
    /// <paramref name="owner"/> as its owner, and the property it sets there: <c>(…, true, null)</c> where it sets none
    /// a field holds, and <c>(…, false, null)</c> where that cannot be read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Three routes, each reading its own field</b>, at 12.1.3. An owner that is the element's type or a base
    /// names the instance CLR property XamlX's <c>PropertyReferenceResolver</c> finds up the owner's chain, most
    /// derived first, and <c>AvaloniaXamlIlAvaloniaPropertyResolver</c> takes the <c>NameProperty</c> field that
    /// property's declaring type declares. Where both are <c>AvaloniaObject</c>s and the owner is outside the element's
    /// chain, <c>AvaloniaXamlIlTransformInstanceAttachedProperties</c> takes the first CLR property of the name up the
    /// owner's chain where it is an instance one, and the static field its declaring type declares, unless that field's
    /// type is an <c>AttachedProperty</c>: it binds that property on the element, and gives it no setter a literal can
    /// take. Otherwise <c>PropertyReferenceResolver</c> reads the owner as declaring an attached property, by a public
    /// static <c>Set</c> or <c>Get</c> accessor of its own that takes the element, and the resolver takes the field the
    /// owner declares; with neither, the markup does not load. Measured under Fluent, Simple and Semi: a template's
    /// <c>Panel</c> written <c>BgAlias.Background="Red"</c>, where <c>BgAlias</c> is a static class whose field holds
    /// <c>Border.BackgroundProperty</c> and has accessors, painted <c>Red</c>, and a <c>Border</c> written
    /// <c>ShadowPanel.Background="{TemplateBinding Background}"</c>, where <c>ShadowPanel</c> derives from <c>Panel</c>
    /// and hides its field with one of another property, drew the row's background. Every class constructor in the
    /// owner's chain runs first, as a field's initializer may need its base's.
    /// </remarks>
    internal (OwnerSpelling Spelling, bool Read, object? Property) OwnerPropertyOn(Type owner, Type target, string name)
    {
        if (!_ownerProperties.TryGetValue((owner, target, name), out (OwnerSpelling Spelling, bool Read, object? Property) known))
        {
            try
            {
                known = ReadOwnerProperty(owner, target, name);
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                known = (OwnerSpelling.None, false, null);
            }

            _ownerProperties[(owner, target, name)] = known;
        }

        return known;
    }

    /// <summary>
    /// The property <c>Owner.Name</c> names in a setter, a <c>TemplateBinding</c> or a binding path: the one the
    /// owner's most-derived public static <c>NameProperty</c> field holds, by
    /// <c>XamlIlAvaloniaPropertyHelper.CreateNode</c> and <c>XamlIlBindingPathHelper</c> at 12.1.3, once every class
    /// constructor in its chain has run; <c>(true, null)</c> where it has none, and <c>(false, null)</c> where that
    /// cannot be read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Public only.</b> A type that hides its base's field with a protected one of another property leaves the
    /// base's for these spellings: measured, a row theme's setter for <c>ProtectedShadow.Background</c>, where
    /// <c>ProtectedShadow</c> derives from <c>Panel</c> so, set the row's <c>Background</c>.
    /// </remarks>
    internal (bool Read, object? Property) PublicPropertyOf(Type owner, string name)
    {
        if (!_publicProperties.TryGetValue((owner, name), out (bool Read, object? Property) known))
        {
            try
            {
                RunClassConstructors(owner);
                FieldInfo? field = Chain(owner)
                    .Select(link => link.GetField(name + "Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    .FirstOrDefault(found => found is not null);
                known = (true, field?.GetValue(null));
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                known = (false, null);
            }

            _publicProperties[(owner, name)] = known;
        }

        return known;
    }

    /// <summary>Whether the brush in the property <paramref name="field"/> holds is not null by default; <c>null</c> when that cannot be read.</summary>
    private bool? PaintedByDefault(Type type, string field) =>
        DefaultOf(type, field) switch
        {
            (false, _) => null,
            (true, var value) => value is not null,
        };

    /// <summary>
    /// Whether <paramref name="type"/>, or a type in its chain outside the framework, draws itself: it
    /// declares a <c>Render</c> that takes a <c>DrawingContext</c>, which may fill its bounds.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The framework's own are left out</b>, those in an assembly named <c>Avalonia</c> or
    /// <c>Avalonia.</c> anything, because what they draw is read from what sets it, a <c>Background</c> or
    /// a <c>Fill</c>. Measured on Avalonia 12.1.3, a control with no template whose <c>Render</c> filled
    /// its bounds with <c>Transparent</c> took the click across a row.
    /// </remarks>
    internal bool DrawsItself(Type type)
    {
        if (!_drawsItself.TryGetValue(type, out bool known))
        {
            known = Safely(() => Chain(type).Any(link =>
                !IsFramework(link.Assembly)
                && link.GetMethods(Declared).Any(method => method.Name == "Render"
                                                          && method.GetParameters() is [{ ParameterType.Name: "DrawingContext" }])), false);
            _drawsItself[type] = known;
        }

        return known;
    }

    /// <summary>
    /// Whether <paramref name="type"/> itself holds markup compiled under its <c>x:Class</c>, read from the
    /// static <c>!XamlIlPopulate</c> the XAML compiler adds to the class it compiles a file's markup into, by
    /// <c>XamlCompilerTaskExecutor</c> at 12.1.3; <c>false</c> where its methods cannot be read.
    /// </summary>
    internal bool HasCompiledMarkup(Type type)
    {
        if (!_compiledMarkup.TryGetValue(type, out bool known))
        {
            known = Safely(() => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(method => method.Name == "!XamlIlPopulate"), false);
            _compiledMarkup[type] = known;
        }

        return known;
    }

    /// <summary>Whether an assembly is the framework's: named <c>Avalonia</c>, or <c>Avalonia.</c> anything.</summary>
    private static bool IsFramework(Assembly assembly) =>
        assembly.GetName().Name is { } name && (name == "Avalonia" || name.StartsWith("Avalonia.", StringComparison.Ordinal));

    /// <summary>
    /// The default that <paramref name="type"/>'s metadata gives the property in its static field
    /// <paramref name="field"/>, public or protected, after every class constructor in its chain has run:
    /// <c>(true, null)</c> when it has no such field, and <c>(false, null)</c> when that cannot be read.
    /// </summary>
    private (bool Readable, object? Value) DefaultOf(Type type, string field)
    {
        if (!_defaults.TryGetValue((type, field), out (bool Readable, object? Value) known))
        {
            known = ReadDefault(type, field);
            _defaults[(type, field)] = known;
        }

        return known;
    }

    /// <summary>What <see cref="OwnerPropertyOn"/> reads, by the routes its remarks name; throws what reading a field throws.</summary>
    private static (OwnerSpelling Spelling, bool Read, object? Property) ReadOwnerProperty(Type owner, Type target, string name)
    {
        const BindingFlags declaredStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        string fieldName = name + "Property";
        RunClassConstructors(owner);
        if (owner.IsAssignableFrom(target))
        {
            if (FirstProperty(owner, name, instanceOnly: true) is { } property)
            {
                // A CLR property whose declaring type holds no field of it is set by its own setter, which is not read.
                return property.DeclaringType!.GetField(fieldName, declaredStatic) is { } field
                    ? (OwnerSpelling.Instance, true, field.GetValue(null))
                    : (OwnerSpelling.Instance, false, null);
            }
        }
        else if (IsAvaloniaObject(owner) && IsAvaloniaObject(target)
                 && FirstProperty(owner, name, instanceOnly: false) is { } clr
                 && (clr.GetMethod is { IsStatic: false } || clr.SetMethod is { IsStatic: false })
                 && clr.DeclaringType!.GetField(fieldName, declaredStatic) is { } instanceField
                 && IsInstanceAttachable(instanceField.FieldType))
        {
            return (OwnerSpelling.AttachedInstance, true, instanceField.GetValue(null));
        }

        if (!HasAccessors(owner, target, name))
        {
            return (OwnerSpelling.None, true, null);
        }

        // An attached property with no field of its own is set by its accessor, which is not read.
        return owner.GetField(fieldName, declaredStatic) is { } attached
            ? (OwnerSpelling.Attached, true, attached.GetValue(null))
            : (OwnerSpelling.Attached, false, null);
    }

    /// <summary>
    /// The first property called <paramref name="name"/> up <paramref name="type"/>'s chain, most derived first, each
    /// type's own in any visibility, as XamlX's <c>GetAllProperties</c> walks it; an instance one only where
    /// <paramref name="instanceOnly"/>, as <c>PropertyReferenceResolver</c> asks.
    /// </summary>
    private static PropertyInfo? FirstProperty(Type type, string name, bool instanceOnly) =>
        Chain(type)
            .SelectMany(link => link.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                                   | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .FirstOrDefault(property => property.Name == name && (!instanceOnly || IsInstanceProperty(property)));

    /// <summary>A property with an instance getter that takes nothing or an instance setter that takes a value, as <c>PropertyReferenceResolver</c> asks.</summary>
    private static bool IsInstanceProperty(PropertyInfo property) =>
        (property.GetMethod is { IsStatic: false } getter && getter.GetParameters().Length == 0)
        || (property.SetMethod is { IsStatic: false } setter && setter.GetParameters().Length == 1);

    /// <summary>
    /// Whether a field of <paramref name="fieldType"/> takes the instance-attached route: its chain reaches
    /// <c>AvaloniaProperty</c> or <c>AvaloniaProperty&lt;T&gt;</c> before any <c>AttachedProperty</c>, as
    /// <c>AvaloniaXamlIlTransformInstanceAttachedProperties</c> walks it at 12.1.3; found by name, as the framework is.
    /// </summary>
    private static bool IsInstanceAttachable(Type fieldType)
    {
        for (Type? link = fieldType; link is not null; link = link.BaseType)
        {
            if (link.Name is "AvaloniaProperty" or "AvaloniaProperty`1")
            {
                return link.GetGenericArguments().Length <= 1;
            }

            if (link.Name.StartsWith("AttachedProperty", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="owner"/> declares a public static <c>Set</c> accessor of <paramref name="name"/> that takes
    /// an element of <paramref name="target"/> and a value, or a <c>Get</c> accessor that takes the element, as
    /// <c>PropertyReferenceResolver</c> looks for an attached property.
    /// </summary>
    private static bool HasAccessors(Type owner, Type target, string name) =>
        owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Any(method =>
            (method.Name == "Set" + name && method.GetParameters() is [var on, _] && on.ParameterType.IsAssignableFrom(target))
            || (method.Name == "Get" + name && method.GetParameters() is [var of] && of.ParameterType.IsAssignableFrom(target)));

    /// <summary>Whether <paramref name="type"/> is an <c>AvaloniaObject</c>: one is in its chain, found by name, as the framework is.</summary>
    private static bool IsAvaloniaObject(Type type) => Chain(type).Any(link => link.Name == "AvaloniaObject");

    /// <summary>Runs every class constructor in <paramref name="type"/>'s chain, base first.</summary>
    private static void RunClassConstructors(Type type)
    {
        foreach (Type link in Chain(type).Reverse())
        {
            RuntimeHelpers.RunClassConstructor(link.TypeHandle);
        }
    }

    /// <summary>Whether <paramref name="exception"/> is what running a class constructor or reading a field may throw.</summary>
    private static bool IsReadFailure(Exception exception) =>
        exception is TypeInitializationException or TargetInvocationException or MemberAccessException or AmbiguousMatchException
        || IsUnreadable(exception);

    /// <summary>
    /// Whether the code of <paramref name="type"/>, or of any type in its chain, calls a <c>Background</c>
    /// or a <c>Fill</c> setter: in a method or a constructor, static or not, or in the closures and state
    /// machines the compiler nests inside it, as a view's code-behind can.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Read from IL in every assembly the chain passes through, not only the scanned ones</b>, since
    /// a base class in a library sets a background as surely as the type's own code. Measured on Avalonia
    /// 12.1.3, a setter called in a lambda that captures a local, after an <c>await</c>, or in the
    /// constructor of a base class in a referenced assembly each painted the control. The framework's own
    /// code at 12.1.3 calls the setter in <c>TopLevel</c>, <c>ResizeGripLayer</c>,
    /// <c>ConnectedAnimation</c>, <c>ColorSlider</c> and Win32's <c>TrayIconImpl</c>, by its source, and a
    /// list, combo or tab row's chain holds none of them.
    /// </para>
    /// <para>
    /// ⚠ <b>Compiled markup is left out.</b> The XAML compiler writes a view's markup as methods on the
    /// view's own type whose names begin with <c>!</c>, such as <c>!XamlIlPopulate</c>, and the content of
    /// each template in it as a type nested in the view, which XamlX names <c>XamlClosure_</c> at 12.1.3,
    /// by its source. Both set what the markup sets, on whichever element sets it, and the markup is read
    /// as markup. A value set through <c>SetValue</c>, a setter called through a method of another type,
    /// such as an extension method, and code whose body cannot be read are not seen.
    /// </para>
    /// </remarks>
    internal bool CodeSetsPaint(Type type) => Chain(type).Any(LinkSetsPaint);

    /// <summary>
    /// Whether code outside the framework in <paramref name="type"/>'s chain calls a <c>Background</c> or a
    /// <c>Fill</c> setter, read as <see cref="CodeSetsPaint"/> reads it: a list's own code, which may paint
    /// the rows it prepares.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The framework's own lists are left out.</b> They do not paint the rows they prepare: measured,
    /// stock list, combo and tab rows took the click as their themes say. And their code sets a fill
    /// elsewhere: Avalonia's <c>ComboBox</c> fills the <c>Rectangle</c> that shows a selected control with a
    /// <c>VisualBrush</c>, by its source at 12.1.3, which is not a row.
    /// </remarks>
    internal bool ConsumerCodeSetsPaint(Type type) => Chain(type).Where(link => !IsFramework(link.Assembly)).Any(LinkSetsPaint);

    /// <summary>Whether one type of a chain, with the types nested in it, calls a <c>Background</c> or a <c>Fill</c> setter.</summary>
    private bool LinkSetsPaint(Type link)
    {
        if (!_codeSetsPaint.TryGetValue(link, out bool known))
        {
            known = Safely(() => AndNested(link).Any(declaring =>
                declaring.GetMethods(Declared | BindingFlags.Static).Cast<MethodBase>()
                    .Concat(declaring.GetConstructors(Declared | BindingFlags.Static))
                    .Where(method => !method.Name.StartsWith('!'))
                    .Any(method => CompiledStrings.CalledBy(method).Any(callee => callee.Name is "set_Background" or "set_Fill"))), false);
            _codeSetsPaint[link] = known;
        }

        return known;
    }

    /// <summary>
    /// A type and every type nested in it, at any depth: its closures, state machines and helpers, but
    /// not the types the XAML compiler nests in a view for its markup.
    /// </summary>
    private static IEnumerable<Type> AndNested(Type type) =>
        type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(nested => !IsCompiledMarkup(nested))
            .SelectMany(AndNested)
            .Prepend(type);

    /// <summary>
    /// Whether a nested type is the XAML compiler's: XamlX names the type holding a template's content
    /// <c>XamlClosure_</c>, and Avalonia's compiler names its namespace record <c>__AvaloniaXamlIl</c>.
    /// </summary>
    private static bool IsCompiledMarkup(Type nested) =>
        nested.Name.StartsWith("XamlClosure_", StringComparison.Ordinal)
        || nested.Name.StartsWith("__AvaloniaXamlIl", StringComparison.Ordinal);

    /// <summary>
    /// The container type an items control generates for an item, read from the <c>newobj</c> in its
    /// most-derived container factory, or <c>null</c> when that cannot be read.
    /// </summary>
    /// <remarks>
    /// ⚠ The factory is <c>CreateContainerForItemOverride</c> in Avalonia and
    /// <c>GetContainerForItemOverride</c> in WPF. One that delegates rather than constructing, as
    /// Avalonia's <c>TreeViewItem</c> does, or that constructs more than one kind of control, has no
    /// single answer and gets none.
    /// </remarks>
    internal Type? ContainerOf(Type itemsControl)
    {
        if (_containers.TryGetValue(itemsControl, out Type? known))
        {
            return known;
        }

        Type? container = Safely(() =>
        {
            for (Type? type = itemsControl; type is not null; type = type.BaseType)
            {
                MethodInfo? factory = type.GetMethods(Declared)
                    .FirstOrDefault(method => method.Name is "CreateContainerForItemOverride" or "GetContainerForItemOverride");
                if (factory is null)
                {
                    continue;
                }

                Type[] built = [.. CompiledStrings.ConstructedBy(factory).Where(IsInputElement).Distinct()];
                return built.Length == 1 ? built[0] : null;
            }

            return null;
        }, (Type?)null);

        _containers[itemsControl] = container;
        return container;
    }

    /// <remarks>
    /// ⛔ A type that cannot be read is not taken for one that takes no part in focus. Reading it as
    /// <see cref="ElementKind.Other"/> would drop it from what can take focus, and a binding it keeps
    /// alive would be reported dead; it resolves as <see cref="ElementKind.Missing"/> instead.
    /// </remarks>
    private static ElementType Resolve(string localName, Dictionary<string, List<Type>> index)
    {
        if (!index.TryGetValue(localName, out List<Type>? types))
        {
            return new ElementType(ElementKind.Missing, null);
        }

        List<Type> inputs = [];
        bool unreadable = false;
        foreach (Type type in types.Distinct())
        {
            switch (Safely<bool?>(() => FocusablePropertyOf(type) is not null, null))
            {
                case true:
                    inputs.Add(type);
                    break;
                case null:
                    unreadable = true;
                    break;
            }
        }

        return (inputs.Count, unreadable) switch
        {
            (1, false) => new ElementType(ElementKind.InputElement, inputs[0]),
            (0, false) => new ElementType(ElementKind.Other, types[0]),
            (0, true) => new ElementType(ElementKind.Missing, null),
            _ => new ElementType(ElementKind.Ambiguous, null),
        };
    }

    private static (bool? Focusable, string? Why) ReadFocusable(Type type)
    {
        try
        {
            // Base first, so an override registered anywhere in the chain is in place before the read.
            foreach (Type link in Chain(type).Reverse())
            {
                RuntimeHelpers.RunClassConstructor(link.TypeHandle);
            }

            object? property = FocusablePropertyOf(type)?.GetValue(null);
            if (property is null)
            {
                return (null, $"{type.Name} has no FocusableProperty to read.");
            }

            object? metadata = property.GetType().GetMethod("GetMetadata", [typeof(Type)])?.Invoke(property, [type]);
            PropertyInfo? defaultValue = metadata?.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(candidate => candidate.Name == "DefaultValue")
                .OrderByDescending(candidate => Chain(candidate.DeclaringType).Count())
                .FirstOrDefault();

            return defaultValue?.GetValue(metadata) is bool value
                ? (value, null)
                : (null, $"The Focusable metadata for {type.Name} has no DefaultValue this rule can read.");
        }
        catch (Exception ex) when (ex is TypeInitializationException or TargetInvocationException
                                       or MemberAccessException or AmbiguousMatchException or TypeLoadException
                                       or FileNotFoundException or FileLoadException or NotSupportedException)
        {
            return (null, $"Reading Focusable for {type.Name} failed with {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static FieldInfo? FocusablePropertyOf(Type type) =>
        type.GetField("FocusableProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

    private static (bool Readable, object? Value) ReadDefault(Type type, string field)
    {
        try
        {
            if (HeldIn(type, field) is not { } property)
            {
                return (true, null);
            }

            object? metadata = property.GetType().GetMethod("GetMetadata", [typeof(Type)])?.Invoke(property, [type]);
            PropertyInfo? defaultValue = metadata?.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(candidate => candidate.Name == "DefaultValue")
                .OrderByDescending(candidate => Chain(candidate.DeclaringType).Count())
                .FirstOrDefault();

            return defaultValue is null ? (false, null) : (true, defaultValue.GetValue(metadata));
        }
        catch (Exception ex) when (ex is TypeInitializationException or TargetInvocationException
                                       or MemberAccessException or AmbiguousMatchException or TypeLoadException
                                       or FileNotFoundException or FileLoadException or NotSupportedException)
        {
            return (false, null);
        }
    }

    /// <summary>
    /// What <paramref name="type"/>'s static field <paramref name="field"/>, its own or a base's, public or protected,
    /// holds once every class constructor in its chain has run; <c>null</c> when it has no such field. Throws what
    /// running a constructor or reading the field throws.
    /// </summary>
    private static object? HeldIn(Type type, string field)
    {
        // Base first, so an override registered anywhere in the chain is in place before the read.
        RunClassConstructors(type);

        // Public or protected: SelectingItemsControl's SelectionModeProperty is protected at 12.1.3.
        return type.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);
    }

    private static IEnumerable<Type> Chain(Type? type)
    {
        for (Type? link = type; link is not null; link = link.BaseType)
        {
            yield return link;
        }
    }

    private static void Index(IEnumerable<Type> loaded, Dictionary<string, List<Type>> index)
    {
        foreach (Type type in loaded)
        {
            // Markup names top-level types; <Module> and friends are the compiler's. A type whose
            // dependencies do not load cannot even say whether it is nested, and is left out.
            if (Safely(() => type.IsNested || type.Name.StartsWith('<') ? null : type.Name, (string?)null) is not { } name)
            {
                continue;
            }

            if (!index.TryGetValue(name, out List<Type>? types))
            {
                types = [];
                index[name] = types;
            }

            types.Add(type);
        }
    }

    private static Assembly? Load(AssemblyName reference)
    {
        try
        {
            return Assembly.Load(reference);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
    }
}
