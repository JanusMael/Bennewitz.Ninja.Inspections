using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality;

/// <summary>A property's value as markup states it: text, or an element, and where it was stated.</summary>
internal sealed record Setting(string? Text, XElement? Element, XamlFile File, XElement At)
{
    public string Where => ThemeReader.LineOf(At) is { } line ? $"{File.RelativePath}:{line}" : File.RelativePath;

    public string Shown => Text ?? $"<{Element?.Name.LocalName}>";
}

/// <summary>A lookup that found a setting, found nothing, or cannot say.</summary>
internal sealed record Found(Setting? Setting, string? Unknown)
{
    public static readonly Found Nothing = new(null, null);

    public bool IsSet => Setting is not null;

    public bool IsUnknown => Unknown is not null;

    public static Found Of(Setting setting) => new(setting, null);

    public static Found Undecidable(string why) => new(null, why);
}

/// <summary>Where a theme's <c>BasedOn</c> chain ends, as far as the scan holds it.</summary>
internal enum ChainEnd
{
    /// <summary>At a theme <c>BasedOn</c> nothing, which leaves every property it does not set at its default.</summary>
    Nothing,

    /// <summary>At the framework's own theme for a type, which the scan does not hold.</summary>
    Framework,
}

/// <summary>Whether a property written after an owner is the property of the control it is set on.</summary>
internal enum Ownership
{
    /// <summary>
    /// It is: written alone, or after the control's type or a base, or, in a setter, a template binding or a binding
    /// path, after an owner whose field holds the control's own property.
    /// </summary>
    Own,

    /// <summary>
    /// On an element, after an owner outside its type's chain whose field holds its own property: the XAML compiler
    /// binds that property on the element, which only a binding can give.
    /// </summary>
    Bound,

    /// <summary>It is not: the owner's field holds another property, an attached one of its own, or none.</summary>
    Other,

    /// <summary>The owner, or the control, does not resolve to one type, or a field cannot be read.</summary>
    Unknown,
}

/// <summary>
/// An owner a property is written after: its name without a prefix, and the XML namespace its prefix declares, or the
/// default one where it has none; <c>null</c> where its prefix is declared nowhere it is written. <see cref="Markup"/>
/// is the namespace of the root element of the markup it is written in.
/// </summary>
/// <remarks>
/// ⚠ <b>Three kinds of XML namespace.</b> A <c>using:</c> or <c>clr-namespace:</c> one names its type exactly; none, or
/// the framework's, finds it by its name, as an element is found; and any other URI names whatever the
/// <c>XmlnsDefinition</c> attributes of the assemblies the markup is compiled against map it to, as
/// <c>TypeReferenceResolver</c> resolves it at 12.1.3, which the rules do not read, so an owner there is not decided:
/// measured, a setter after <c>u:TemplatedControl</c>, where <c>u</c> is a consumer's URI mapped to a namespace with a
/// <c>TemplatedControl</c> of its own, set another property than the row's <c>Background</c>. WPF's and MAUI's
/// presentation URIs are the framework's only in markup whose root is in them: at 12.1.3 neither Avalonia nor XamlX maps
/// either, so in Avalonia's markup a type there is one a consumer's <c>XmlnsDefinition</c> maps, like any other URI's.
/// </remarks>
internal sealed record Owner(string Local, XNamespace? Namespace, XNamespace? Markup = null)
{
    /// <summary>Avalonia's URI, whose types are found by name in any markup.</summary>
    private const string AvaloniaUri = "https://github.com/avaloniaui";

    /// <summary>WPF's and MAUI's URIs, whose types are found by name only in their own frameworks' markup.</summary>
    private static readonly string[] OtherFrameworkUris =
    [
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
        "http://schemas.microsoft.com/dotnet/2021/maui",
    ];

    /// <summary>
    /// The CLR namespace a <c>using:</c> or <c>clr-namespace:</c> XML namespace names, where the owner's is one, which
    /// names its type exactly; <c>null</c> for any other.
    /// </summary>
    public string? Clr => ClrOf(Namespace);

    /// <summary>Whether the owner's XML namespace is none or the framework's, where its type is found by its name.</summary>
    public bool IsFramework =>
        Namespace is { } ns
        && (ns == XNamespace.None
            || ns.NamespaceName == AvaloniaUri
            || (ns == Markup && OtherFrameworkUris.Contains(ns.NamespaceName, StringComparer.Ordinal)));

    /// <summary>
    /// The CLR namespace a <c>using:</c> or <c>clr-namespace:</c> XML namespace names; <c>null</c> for any other, or none.
    /// </summary>
    internal static string? ClrOf(XNamespace? xmlNamespace) =>
        xmlNamespace?.NamespaceName is not { } uri ? null
        : uri.StartsWith("using:", StringComparison.Ordinal) ? uri["using:".Length..].Trim()
        : uri.StartsWith("clr-namespace:", StringComparison.Ordinal) ? uri["clr-namespace:".Length..].Split(';')[0].Trim()
        : null;
}

/// <summary>
/// What a control's themes and templates say, followed the way the framework follows them, as far as
/// markup shows it.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Shared because two rules resolving one theme is how their answers drift.</b> <c>BNXQ1005</c>
/// asks what a theme says about focus, <c>BNXQ1009</c> what it says about a container's name, and
/// <c>BNXQ1010</c> what it says about a container's background. Each follows a control's
/// <c>Theme</c> or its implicit one, an <c>ItemContainerTheme</c>, the <c>BasedOn</c> chain under
/// each, and what a template holds. This began as <c>BNXQ1005</c>'s own reading and moved here
/// unchanged; each caller names the properties that matter to it where a theme's reach is in doubt.
/// </para>
/// <para>
/// ⚠ <b>What markup cannot decide is said, not guessed.</b> A value that is a binding, a resource the
/// scan does not hold, a theme set in a nested style that applies only in some state, and an implicit
/// theme whose reach depends on where a view is placed all come back as a reason a caller can name
/// in <see cref="XamlRuleResult.Skipped"/>.
/// </para>
/// </remarks>
internal sealed class ThemeReader(ResourceScope scope, ElementTypes types)
{
    private readonly ResourceScope _scope = scope;
    private readonly ElementTypes _types = types;

    /// <summary>The theme the control itself uses: the one it names, else its implicit one.</summary>
    internal (Definition? Theme, string? Unknown) ThemeOfHost(XElement host, XamlFile file, Type type, params string[] relevant)
    {
        Found named = Local(host, file, "Theme", type);
        if (named.IsUnknown)
        {
            return (null, named.Unknown);
        }

        if (named.IsSet)
        {
            (Definition? theme, string? unknown, bool cleared) = ThemeOf(named.Setting!, "its Theme");
            if (!cleared)
            {
                return Applied((theme, unknown), type, $"this {host.Name.LocalName}");
            }
        }

        return Applied(ImplicitTheme(_types.StyleKeyOf(type).Name, host, $"this {host.Name.LocalName}", relevant), type,
            $"this {host.Name.LocalName}");
    }

    /// <summary>
    /// The theme the generated containers use: the <c>ItemContainerTheme</c> the control sets, or its
    /// theme sets, else the container type's implicit theme.
    /// </summary>
    internal (Definition? Theme, string? Unknown) ItemContainerThemeOf(
        XElement host, XamlFile file, Definition? hostTheme, Type container, params string[] relevant)
    {
        Found named = Local(host, file, "ItemContainerTheme");
        if (!named.IsSet && !named.IsUnknown && hostTheme is not null)
        {
            named = InTheme(hostTheme, "ItemContainerTheme");
        }

        if (named.IsUnknown)
        {
            return (null, named.Unknown);
        }

        if (named.IsSet)
        {
            (Definition? theme, string? unknown, bool cleared) = ThemeOf(named.Setting!, "its ItemContainerTheme");
            if (!cleared)
            {
                return Applied((theme, unknown), container, $"the {container.Name} containers");
            }
        }

        string subject = $"the {container.Name} containers of this {host.Name.LocalName}";
        return Applied(ImplicitTheme(_types.StyleKeyOf(container).Name, host, subject, relevant), container, subject);
    }

    /// <summary>
    /// The implicit theme for <paramref name="typeName"/> that reaches <paramref name="from"/>, or an
    /// explanation when one that matters may or may not: one that sets one of <paramref name="relevant"/>, after
    /// any owner, since a caller may name an attached property.
    /// </summary>
    internal (Definition? Theme, string? Unknown) ImplicitTheme(string typeName, XElement from, string subject, params string[] relevant)
    {
        Lookup lookup = _scope.Find(ResourceScope.ImplicitKey(typeName), from);
        switch (lookup.Reach)
        {
            case Reach.Found:
                return (lookup.Definition, null);
            case Reach.MayReach:
                foreach (Definition candidate in lookup.Others)
                {
                    foreach (string property in relevant)
                    {
                        if (InTheme(candidate, property, _ => true) is { IsSet: true } or { IsUnknown: true })
                        {
                            return (null,
                                $"An implicit ControlTheme for {typeName} at {candidate.Where} sets {property}, and it "
                                + $"is in no scope this markup shows reaching {subject}: whether it applies depends on "
                                + "where the view is placed at runtime.");
                        }
                    }
                }

                return (null, null);
            default:
                return (null, null);
        }
    }

    /// <summary>
    /// A theme as the framework applies it to a control of <paramref name="type"/>: not at all where its
    /// <c>TargetType</c> is not assignable from the control's style key, which is then named as not decided.
    /// </summary>
    /// <remarks>
    /// ⚠ At 12.1.3 a <c>ControlTheme</c> applies only where its <c>TargetType</c> is assignable from the control's
    /// <c>StyleKey</c>, by its own check: measured, a list whose <c>StyleKeyOverride</c> is <c>ListBox</c>, given a theme
    /// whose <c>TargetType</c> is the list's own type, took none of its setters. What applies instead is not read, so
    /// such a theme is named rather than followed. A <c>TargetType</c> is the type its prefix names, as an owner is
    /// (<see cref="Owner"/>): measured, a list's rows took nothing from a theme whose <c>TargetType</c> named, through a
    /// <c>clr-namespace:</c> prefix, a row type whose name another scanned type carries. One that does not resolve to one
    /// type, and a control whose style key its code returns other than by a <c>typeof</c>, are named too.
    /// </remarks>
    private (Definition? Theme, string? Unknown) Applied((Definition? Theme, string? Unknown) found, Type type, string subject)
    {
        if (found.Theme is not { } theme || ControlThemes.TargetTypeOf(theme.Element) is not { } target)
        {
            return found;
        }

        if (TargetTypeOf(theme, target) is not { } targetType)
        {
            return (null, $"The ControlTheme at {theme.Where} targets {target}, which does not resolve to one type in the scan, "
                          + $"so whether the framework applies it to {subject} is not decided from markup.");
        }

        if (!_types.StyleKeyKnown(type))
        {
            return (null, $"The style key of {subject} is what its StyleKeyOverride returns other than by a typeof, which "
                          + $"is not read, so whether the framework applies the ControlTheme at {theme.Where} is not decided "
                          + "from markup.");
        }

        Type key = _types.StyleKeyOf(type);
        return ElementTypes.IsAssignable(targetType, key)
            ? found
            : (null, $"The ControlTheme at {theme.Where} targets {targetType.Name}, which the style key of {subject}, {key.Name}, "
                     + "is not, so the framework does not apply it, and what applies instead is not decided from markup.");
    }

    /// <summary>
    /// The type a theme's <c>TargetType</c> names: through its prefix's namespace where that is a <c>using:</c> or
    /// <c>clr-namespace:</c> one, else by its name; <c>null</c> where it does not resolve to one type.
    /// </summary>
    private Type? TargetTypeOf(Definition theme, string target) =>
        (ControlThemes.TargetTypePrefixOf(theme.Element) is { } prefix
         && Owner.ClrOf(theme.Element.GetNamespaceOfPrefix(prefix)) is { } clr
            ? _types.TypeNamed(clr, target)
            : null)
        ?? _types.Resolve(target).Type;

    /// <summary>A theme a property names: inline, by resource key, or <c>{x:Null}</c>.</summary>
    internal (Definition? Theme, string? Unknown, bool Cleared) ThemeOf(Setting setting, string what)
    {
        if (setting.Element is { } element)
        {
            if (element.Name.LocalName == "ControlTheme")
            {
                return (new Definition(setting.File, element), null, false);
            }

            if (element.Name.LocalName is "StaticResource" or "DynamicResource"
                && element.Attribute("ResourceKey")?.Value is { } resourceKey
                && ResourceScope.NormalizeKey(resourceKey) is { } elementKey)
            {
                return Resolved(_scope.Find(elementKey, element), elementKey, setting, what);
            }

            return (null, $"{Capitalised(what)} at {setting.Where} is a <{element.Name.LocalName}>, which this rule cannot follow.", false);
        }

        string text = setting.Text!.Trim();
        if (IsNull(text))
        {
            return (null, null, true);
        }

        return ResourceScope.ReferencedKey(text) is { } key
            ? Resolved(_scope.Find(key, setting.At), key, setting, what)
            : (null, $"{Capitalised(what)} at {setting.Where} is {text}, which markup cannot evaluate.", false);
    }

    private static (Definition? Theme, string? Unknown, bool Cleared) Resolved(Lookup lookup, string key, Setting setting, string what) =>
        lookup.Reach switch
        {
            Reach.Found when lookup.Definition!.Element.Name.LocalName == "ControlTheme" => (lookup.Definition, null, false),
            Reach.Found => (null, $"{Capitalised(what)} at {setting.Where} names {key}, which is a {lookup.Definition!.Element.Name.LocalName} at {lookup.Definition.Where}, not a ControlTheme.", false),
            Reach.NotInScan when ResourceScope.IsImplicit(key) => (null, null, false),
            Reach.NotInScan => (null, $"{Capitalised(what)} at {setting.Where} names {key}, which is not defined in the scanned markup, so what it sets is unknown.", false),
            _ => (null, $"{Capitalised(what)} at {setting.Where} names {key}, which is defined {lookup.Others.Count} times, none in a scope this markup shows reaching it.", false),
        };

    /// <summary>
    /// What a theme sets <paramref name="property"/> to: its own last setter, else what the theme it
    /// is <c>BasedOn</c> sets, and so on down.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="property">The property, named without an owner.</param>
    /// <param name="which">
    /// The setters for <paramref name="property"/>, after any owner, that count, the last of them winning, as for an
    /// attached property the caller tells apart by its owner; else only one for the property of the control the theme
    /// targets, as <see cref="LastSetting(IEnumerable{XElement}, XamlFile, string, string, Type)"/> reads it.
    /// </param>
    internal Found InTheme(Definition theme, string property, Func<XElement, bool>? which = null) => InTheme(theme, property, which, []);

    private Found InTheme(Definition theme, string property, Func<XElement, bool>? which, HashSet<XElement> seen)
    {
        if (!seen.Add(theme.Element))
        {
            return Found.Nothing;
        }

        // A nested style that sets it changes it in some state only, which markup cannot decide.
        if (theme.Element.Elements()
                .Where(child => child.Name.LocalName == "Style")
                .SelectMany(style => style.DescendantsAndSelf().Where(nested => nested.Name.LocalName == "Style"))
                .Where(style => style.Attribute("Selector")?.Value.Contains("/template/", StringComparison.Ordinal) != true)
                .SelectMany(style => style.Elements())
                .FirstOrDefault(setter => SetsProperty(setter, property)) is { } conditional)
        {
            return Found.Undecidable(
                $"The ControlTheme at {theme.Where} sets {property} in a nested style at line {LineOf(conditional)}, "
                + "which applies only in some state.");
        }

        (XElement? setter, string? unknown) = which is not null
            ? (theme.Element.Elements().LastOrDefault(child => SetsProperty(child, property) && which(child)), null)
            : LastSetting(theme.Element.Elements(), theme, property);
        if (unknown is not null)
        {
            return Found.Undecidable(unknown);
        }

        if (setter is not null)
        {
            return Found.Of(ValueOf(setter, theme.File));
        }

        if (theme.Element.Attribute("BasedOn")?.Value is not { } basedOn)
        {
            return Found.Nothing;
        }

        if (ResourceScope.ReferencedKey(basedOn) is not { } key)
        {
            return Found.Undecidable($"The ControlTheme at {theme.Where} is BasedOn {basedOn}, which this rule cannot follow.");
        }

        Lookup lookup = _scope.Find(key, theme.Element, exclude: theme.Element);
        switch (lookup.Reach)
        {
            case Reach.Found:
                return InTheme(lookup.Definition!, property, which, seen);
            case Reach.NotInScan when ResourceScope.IsImplicit(key):
                // The framework's own theme for the type, which sets nothing this rule reads.
                return Found.Nothing;
            case Reach.NotInScan:
                return Found.Undecidable(
                    $"The ControlTheme at {theme.Where} is BasedOn {key}, which is not defined in the scanned markup.");
            case Reach.MayReach:
                foreach (Definition candidate in lookup.Others)
                {
                    if (InTheme(candidate, property, which, [.. seen]) is { IsSet: true } or { IsUnknown: true })
                    {
                        return Found.Undecidable(
                            $"The ControlTheme at {theme.Where} is BasedOn {key}, and the implicit theme at "
                            + $"{candidate.Where} sets {property}, but it is in no scope this markup shows reaching it.");
                    }
                }

                return Found.Nothing;
            default:
                return Found.Undecidable(
                    $"The ControlTheme at {theme.Where} is BasedOn {key}, which is defined {lookup.Others.Count} times.");
        }
    }

    /// <summary>
    /// The themes <paramref name="theme"/>'s <c>BasedOn</c> chain passes through, itself first, as far
    /// as the scan holds them; where the chain ends; or why it cannot be followed further.
    /// </summary>
    /// <remarks>
    /// ⚠ An implicit theme in another file, which may reach the chain from where a view is placed, is
    /// taken as the framework's when none of its setters touches one of <paramref name="relevant"/>, as
    /// <see cref="InTheme(Definition, string, Func{XElement, bool})"/> takes it.
    /// </remarks>
    internal (IReadOnlyList<Definition> Themes, ChainEnd End, string? Unknown) ChainOf(Definition theme, params string[] relevant)
    {
        List<Definition> themes = [];
        HashSet<XElement> seen = [];
        for (Definition link = theme; ;)
        {
            if (!seen.Add(link.Element))
            {
                return (themes, ChainEnd.Nothing, $"The ControlTheme at {link.Where} is BasedOn a chain that comes back to it.");
            }

            themes.Add(link);
            if (link.Element.Attribute("BasedOn")?.Value is not { } basedOn)
            {
                return (themes, ChainEnd.Nothing, null);
            }

            if (ResourceScope.ReferencedKey(basedOn) is not { } key)
            {
                return (themes, ChainEnd.Nothing, $"The ControlTheme at {link.Where} is BasedOn {basedOn}, which this rule cannot follow.");
            }

            Lookup lookup = _scope.Find(key, link.Element, exclude: link.Element);
            switch (lookup.Reach)
            {
                case Reach.Found:
                    link = lookup.Definition!;
                    continue;
                case Reach.NotInScan when ResourceScope.IsImplicit(key):
                    return (themes, ChainEnd.Framework, null);
                case Reach.NotInScan:
                    return (themes, ChainEnd.Nothing,
                        $"The ControlTheme at {link.Where} is BasedOn {key}, which is not defined in the scanned markup.");
                case Reach.MayReach:
                    foreach (Definition candidate in lookup.Others)
                    {
                        if (relevant.FirstOrDefault(property => candidate.Element.Descendants().Any(setter => SetsProperty(setter, property))) is { } touched)
                        {
                            return (themes, ChainEnd.Nothing,
                                $"The ControlTheme at {link.Where} is BasedOn {key}, and the implicit theme at {candidate.Where} "
                                + $"sets {touched}, but it is in no scope this markup shows reaching it.");
                        }
                    }

                    return (themes, ChainEnd.Framework, null);
                default:
                    return (themes, ChainEnd.Nothing,
                        $"The ControlTheme at {link.Where} is BasedOn {key}, which is defined {lookup.Others.Count} times.");
            }
        }
    }

    /// <summary>
    /// What a theme sets <paramref name="property"/> to in the state a control starts in, before anyone
    /// points at it, selects it or focuses it, and, when nothing along its <c>BasedOn</c> chain sets
    /// it, where the chain ends.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Unlike <see cref="InTheme(Definition, string, Func{XElement, bool})"/>, a nested style is not a reason to stop.</b>
    /// A nested style whose selector is <c>^</c> alone applies in every state, so it is read, and it
    /// wins over the theme's own setter. The rest apply only in some states or to some controls, and
    /// the caller, which knows which of them matter to it, reads them itself. A setter counts only for the
    /// property of the control the theme targets, as
    /// <see cref="LastSetting(IEnumerable{XElement}, XamlFile, string, string, Type)"/> reads it, unless
    /// <paramref name="which"/> chooses.
    /// </remarks>
    /// <param name="theme">The theme.</param>
    /// <param name="property">The property, named without an owner.</param>
    /// <param name="which">
    /// The setters for <paramref name="property"/>, after any owner, that count, the last of them winning, as for a
    /// property a style's selector names, which is matched by its name; else only one for the property of the control
    /// the theme targets.
    /// </param>
    internal (Found Found, ChainEnd End) InBaseState(Definition theme, string property, Func<XElement, bool>? which = null)
    {
        (IReadOnlyList<Definition> themes, ChainEnd end, string? unknown) = ChainOf(theme, property);
        foreach (Definition link in themes)
        {
            foreach (IEnumerable<XElement> setters in (IEnumerable<XElement>[])
                     [
                         link.Element.Elements()
                             .Where(child => child.Name.LocalName == "Style" && child.Attribute("Selector")?.Value.Trim() == "^")
                             .SelectMany(style => style.Elements()),
                         link.Element.Elements(),
                     ])
            {
                (XElement? setter, string? doubt) = which is not null
                    ? (setters.LastOrDefault(child => SetsProperty(child, property) && which(child)), null)
                    : LastSetting(setters, link, property);
                if (doubt is not null)
                {
                    return (Found.Undecidable(doubt), end);
                }

                if (setter is not null)
                {
                    return (Found.Of(ValueOf(setter, link.File)), end);
                }
            }
        }

        return (unknown is null ? Found.Nothing : Found.Undecidable(unknown), end);
    }

    /// <summary>Adds what a template setting holds to <paramref name="content"/>; the reason when it cannot.</summary>
    internal string? TemplateContent(Found found, List<(XamlFile File, XElement Root)> content)
    {
        if (found.IsUnknown)
        {
            return found.Unknown;
        }

        if (!found.IsSet)
        {
            return null;
        }

        Setting setting = found.Setting!;
        if (setting.Element is { } element)
        {
            if (element.Name.LocalName is "StaticResource" or "DynamicResource"
                && element.Attribute("ResourceKey")?.Value is { } resourceKey
                && ResourceScope.NormalizeKey(resourceKey) is { } elementKey)
            {
                return Template(_scope.Find(elementKey, element), elementKey, setting, content);
            }

            content.Add((setting.File, element));
            return null;
        }

        string text = setting.Text!.Trim();
        if (IsNull(text))
        {
            return null;
        }

        return ResourceScope.ReferencedKey(text) is { } key
            ? Template(_scope.Find(key, setting.At), key, setting, content)
            : $"The template at {setting.Where} is {text}, which markup cannot evaluate.";
    }

    private static string? Template(Lookup lookup, string key, Setting setting, List<(XamlFile File, XElement Root)> content)
    {
        if (lookup.Reach != Reach.Found)
        {
            return $"The template at {setting.Where} names {key}, which "
                   + (lookup.Reach == Reach.NotInScan ? "is not defined in the scanned markup." : "cannot be told apart from its other definitions.");
        }

        content.Add((lookup.Definition!.File, lookup.Definition.Element));
        return null;
    }

    /// <summary>
    /// The value an element sets locally for its own <paramref name="property"/>: as an attribute or a property
    /// element, written alone or after an owner, as the XAML compiler takes each, as <see cref="OwnershipOf"/> reads
    /// an owner; or why one may and that is not decided.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="file">The file it is in.</param>
    /// <param name="property">The property, named without an owner.</param>
    /// <param name="type">
    /// The element's type, where it is not the one its name resolves to, as a view's root is its <c>x:Class</c>'s, which
    /// <c>XamlCompilerTaskExecutor</c> makes the root's type before any transform at 12.1.3; else <c>null</c>, and its name
    /// is resolved where an owner makes that matter.
    /// </param>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Two spellings that may set it decide nothing.</b> The framework applies each in turn, in the order the
    /// document gives them, and a binding's value arrives once the data it reads does, so the value an element is left
    /// with is not decided from markup: measured under Fluent, Simple and Semi, a template's <c>Panel</c> written
    /// <c>Border.Background="{TemplateBinding Background}"</c> and then <c>TextBlock.Background="{DynamicResource Red}"</c>
    /// painted <c>Red</c>, and written the other way round drew the row's background, and a <c>TextBlock</c> written
    /// <c>Text=""</c> beside <c>SelectableTextBlock.Text="{Binding Label}"</c> was named by its label.
    /// </para>
    /// <para>
    /// ⚠ <b>After an owner outside the element's chain, on one route of three, only a binding sets it.</b> Where
    /// <see cref="ElementTypes.OwnerPropertyOn"/> finds the instance-attached route, the XAML compiler gives the
    /// property no setter that takes a value: XamlX's <c>ConvertPropertyValuesToAssignmentsTransformer</c> finds a
    /// <c>BindingSetter</c> alone. So a binding, a <c>TemplateBinding</c> or a <c>DynamicResource</c>, written as an
    /// extension or as an element, sets the element's own property where that route's field holds it, and a literal
    /// does not load: measured, a list written <c>ComboBox.ItemsSource="{Binding Rows}"</c>, or with it as a property
    /// element, generated its rows, and a template's <c>Panel</c> written <c>Border.Background="Red"</c> did not load.
    /// An attached property's accessors take any value.
    /// </para>
    /// </remarks>
    internal Found Local(XElement element, XamlFile file, string property, Type? type = null)
    {
        List<(Setting Value, string? Doubt, string Spelling)> givings = [.. Givings(element, file, property, type)];
        if (givings.Count > 1)
        {
            return Found.Undecidable(
                $"The {element.Name.LocalName} at {Where(file, element)} gives its {property} {givings.Count} ways, "
                + $"{string.Join(" and ", givings.Select(giving => giving.Spelling))}, which the framework applies in turn, "
                + "so the value it is left with is not decided from markup.");
        }

        return givings is [var only]
            ? only.Doubt is null ? Found.Of(only.Value) : Found.Undecidable(only.Doubt)
            : Found.Nothing;
    }

    /// <summary>
    /// Every value an element gives its own <paramref name="property"/>, or may: each spelling <see cref="Local"/>
    /// reads, decided or not.
    /// </summary>
    /// <remarks>For a caller to whom a value that may be set counts as one that is, and any of them as the one applied.</remarks>
    internal IEnumerable<Setting> MayGive(XElement element, XamlFile file, string property, Type? type = null) =>
        Givings(element, file, property, type).Select(giving => giving.Value);

    /// <summary>
    /// Whether an element may set <paramref name="property"/> locally in any spelling: by its name, after any owner,
    /// or as a property element. Whether an owner makes it the element's own takes its type, which
    /// <see cref="Local"/> reads.
    /// </summary>
    internal static bool MaySet(XElement element, string property)
    {
        string suffix = "." + property;
        return element.Attributes().Any(attribute => attribute.Name.LocalName == property || attribute.Name.LocalName.EndsWith(suffix, StringComparison.Ordinal))
               || element.Elements().Any(child => child.Name.LocalName.EndsWith(suffix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Whether <paramref name="owner"/>, written before <paramref name="property"/> on an element called
    /// <paramref name="name"/> of type <paramref name="type"/>, or in a setter, a <c>TemplateBinding</c> or a binding
    /// path read against such a control, names that control's own property.
    /// </summary>
    /// <param name="owner">The owner; <c>null</c> when the property is written alone.</param>
    /// <param name="property">The property, named without an owner.</param>
    /// <param name="name">The element's name, or the name of the type the control is.</param>
    /// <param name="nameNamespace">The XML namespace <paramref name="name"/> is in, where it is known.</param>
    /// <param name="type">The control's type, or <c>null</c> when it does not resolve.</param>
    /// <param name="local">
    /// Whether it is written on the element itself, as <see cref="ElementTypes.OwnerPropertyOn"/> reads it; else in a
    /// setter, a <c>TemplateBinding</c> or a path, as <see cref="ElementTypes.PublicPropertyOf"/> reads it.
    /// </param>
    /// <remarks>
    /// ⚠ <b>An owner names the property the field the compiler takes for it holds</b>, and the control has that
    /// property only where it is the one the control's own spelling sets, as <see cref="ElementTypes.OwnPropertyOf"/>
    /// reads it. Measured under Fluent, Simple and Semi: a row theme's setter for <c>(Border.Background)</c> set the
    /// row's, and its setter for a consumer's attached property named <c>Background</c> did not, nor did a
    /// <c>TemplateBinding</c> or a path that named that one take the row's. An owner with no prefix, or in a
    /// framework's namespace, is found by its name, as an element is, one in a <c>using:</c> or <c>clr-namespace:</c>
    /// namespace as exactly that type, and one in any other is not decided (<see cref="Owner"/>). On an element, an
    /// owner that is the control's type or a base names its own. In a setter, a <c>TemplateBinding</c> or a path, it
    /// names what its most-derived public field holds, which a subclass may hide with another property: measured, a
    /// list whose theme set <c>Focusable</c> to <c>True</c> and then <c>ShadowList.Focusable</c> to <c>False</c>, where
    /// <c>ShadowList</c> derives from <c>ListBox</c> and declares a public field of another property, took focus. Where
    /// two types in the control's chain carry an owner's name, as a consumer's subclass named as the framework type it
    /// derives from does, which one the namespace names is not read, so a setter, a <c>TemplateBinding</c> or a path after
    /// it is decided only where they hold one property: measured, a row theme's setter for
    /// <c>ContentControl.Background</c>, on rows of a consumer's <c>ContentControl</c> that derives from
    /// <c>ListBoxItem</c> and hides the field, set the rows' own. An owner or a control whose fields cannot be read, a
    /// class constructor failing among them, is not decided, and never read as another property's.
    /// </remarks>
    internal Ownership OwnershipOf(Owner? owner, string property, string name, XNamespace? nameNamespace, Type? type, bool local)
    {
        if (owner is null)
        {
            return Ownership.Own;
        }

        if (owner.Namespace is null)
        {
            return Ownership.Unknown;
        }

        Type? holder;
        bool inChain;
        if (owner.Clr is { } clr)
        {
            holder = _types.TypeNamed(clr, owner.Local);
            inChain = (owner.Local == name && owner.Namespace == nameNamespace)
                      || (holder is not null && type is not null && ElementTypes.IsAssignable(holder, type));
        }
        else if (owner.IsFramework)
        {
            inChain = owner.Local == name || (type is not null && ElementTypes.NamesOf(type).Contains(owner.Local, StringComparer.Ordinal));
            holder = inChain && type is not null ? ElementTypes.InChain(type, owner.Local) : _types.Resolve(owner.Local).Type;
        }
        else
        {
            // A URI that only an assembly's XmlnsDefinition maps to a type, which is not read.
            return Ownership.Unknown;
        }

        if (inChain && (local || type is null || holder is null))
        {
            // On an element the compiler takes the CLR property found up the owner's chain: the control's own.
            return Ownership.Own;
        }

        if (type is null || holder is null)
        {
            return Ownership.Unknown;
        }

        (bool ownRead, object? own) = _types.OwnPropertyOf(type, property);
        if (!local)
        {
            // An owner found by its name is the framework's type of that name, unless an assembly maps a consumer's type of
            // that name to its namespace too, which is not read: every type in the chain that carries it must agree.
            IReadOnlyList<Type> named = inChain && owner.Clr is null ? ElementTypes.AllInChain(type, owner.Local) : [];
            IReadOnlyList<Type> candidates = named.Count == 0 ? [holder] : named;
            object? common = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                (bool publicRead, object? field) = _types.PublicPropertyOf(candidates[i], property);
                if (!publicRead || !ownRead || (i > 0 && !Agree(field, common)))
                {
                    return Ownership.Unknown;
                }

                common = field;
            }

            return Same(common, own) ? Ownership.Own : Ownership.Other;
        }

        (OwnerSpelling spelling, bool heldRead, object? held) = _types.OwnerPropertyOn(holder, type, property);
        if (!heldRead || !ownRead)
        {
            return Ownership.Unknown;
        }

        if (spelling == OwnerSpelling.None)
        {
            // The markup does not load.
            return Ownership.Other;
        }

        return !Same(held, own) ? Ownership.Other
            : spelling == OwnerSpelling.AttachedInstance ? Ownership.Bound
            : Ownership.Own;
    }

    /// <summary>Whether two fields hold one property, as <c>AvaloniaProperty.Equals</c> compares them.</summary>
    private static bool Same(object? one, object? other) => one is not null && other is not null && one.Equals(other);

    /// <summary>Whether two owners name the same thing: one property, or none.</summary>
    private static bool Agree(object? one, object? other) => (one is null && other is null) || Same(one, other);

    /// <summary>
    /// The last of <paramref name="setters"/> that sets <paramref name="property"/> of a control called
    /// <paramref name="name"/> of type <paramref name="type"/>: written alone, or after an owner that names the
    /// control's own, as <see cref="OwnershipOf"/> reads it; or, where the last that may is after an owner that
    /// cannot be read, why.
    /// </summary>
    internal (XElement? Setter, string? Unknown) LastSetting(IEnumerable<XElement> setters, XamlFile file, string property, string name, Type? type)
    {
        foreach (XElement setter in setters.Reverse())
        {
            if (ResourceScope.SetterProperty(setter) is not { } written || ResourceScope.PropertyName(written) != property)
            {
                continue;
            }

            Owner? owner = ResourceScope.OwnerOf(written, setter);
            switch (OwnershipOf(owner, property, name, null, type, local: false))
            {
                case Ownership.Own:
                    return (setter, null);
                case Ownership.Unknown:
                    return (null,
                        $"A Setter at {Where(file, setter)} sets {written}, which is the {name}'s own {property} only where "
                        + $"{owner?.Local} holds the same property, and whether it does could not be read.");
            }
        }

        return (null, null);
    }

    /// <summary>The last of a theme's <paramref name="setters"/> that sets <paramref name="property"/> of the control the theme targets.</summary>
    private (XElement? Setter, string? Unknown) LastSetting(IEnumerable<XElement> setters, Definition theme, string property)
    {
        string name = ControlThemes.TargetTypeOf(theme.Element) ?? string.Empty;
        return LastSetting(setters, theme.File, property, name, name.Length == 0 ? null : _types.Resolve(name).Type);
    }

    /// <summary>
    /// Every spelling in which an element gives <paramref name="property"/> a value that sets its own or may: by the
    /// property's name, after an owner, then as a property element; each with <c>null</c> where it sets the element's
    /// own and why where it may, and as it is written.
    /// </summary>
    private IEnumerable<(Setting Value, string? Doubt, string Spelling)> Givings(XElement element, XamlFile file, string property, Type? type)
    {
        string name = element.Name.LocalName;
        if (element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == property) is { } bare)
        {
            yield return (new Setting(bare.Value, null, file, element), null, property);
        }

        // An attribute with no prefix names its owner in the element's default namespace, as a property element does.
        string suffix = "." + property;
        XNamespace unprefixed = element.GetDefaultNamespace();
        XNamespace? markup = element.Document?.Root?.Name.Namespace;
        IEnumerable<(Owner Owner, Setting Value, string Spelling)> owned =
            element.Attributes()
                .Where(attribute => attribute.Name.LocalName.EndsWith(suffix, StringComparison.Ordinal))
                .Select(attribute => (
                    new Owner(attribute.Name.LocalName[..^suffix.Length], attribute.Name.Namespace == XNamespace.None ? unprefixed : attribute.Name.Namespace, markup),
                    new Setting(attribute.Value, null, file, element),
                    attribute.Name.LocalName))
                .Concat(element.Elements()
                    .Where(child => child.Name.LocalName.EndsWith(suffix, StringComparison.Ordinal))
                    .Select(child => (
                        new Owner(child.Name.LocalName[..^suffix.Length], child.Name.Namespace, markup),
                        child.Elements().FirstOrDefault() is { } held ? new Setting(null, held, file, child) : new Setting(child.Value, null, file, child),
                        $"<{child.Name.LocalName}>")));

        bool resolved = type is not null;
        foreach ((Owner owner, Setting value, string spelling) in owned)
        {
            if (!resolved && !(owner.Local == name && (owner.Clr is null || owner.Namespace == element.Name.Namespace)))
            {
                type = _types.Resolve(name).Type;
                resolved = true;
            }

            switch (OwnershipOf(owner, property, name, element.Name.Namespace, type, local: true))
            {
                case Ownership.Own:
                    yield return (value, null, spelling);
                    break;
                case Ownership.Bound when Binds(value) is { } binds:
                    // Anything but a binding does not load.
                    if (binds)
                    {
                        yield return (value, null, spelling);
                    }

                    break;
                case Ownership.Bound:
                    yield return (value,
                        $"The {owner.Local}.{property} at {value.Where} is {value.Shown}, which gives the {name} its own {property} only if "
                        + "it is a binding, and markup does not say whether it is.", spelling);
                    break;
                case Ownership.Unknown:
                    yield return (value,
                        $"The {owner.Local}.{property} at {value.Where} is the {name}'s own {property} only where {owner.Local} holds the "
                        + "same property, and whether it does could not be read.", spelling);
                    break;
            }
        }
    }

    /// <summary>
    /// Whether a value is a binding: <c>true</c> for a <c>BindingBase</c>, written as an extension or as an element;
    /// <c>false</c> for text, <c>{x:Null}</c> or an element of another type; <c>null</c> for another extension, whose
    /// value markup does not show, or an element whose type does not resolve.
    /// </summary>
    /// <remarks>
    /// <c>Binding</c>, <c>CompiledBinding</c>, <c>ReflectionBinding</c>, <c>MultiBinding</c>, <c>TemplateBinding</c> and
    /// <c>DynamicResource</c> are bindings at 12.1.3, by their source, whatever the scan resolves; any other is one where
    /// its type derives from <c>BindingBase</c>.
    /// </remarks>
    private bool? Binds(Setting value)
    {
        if (value.Element is null && (value.Text is not { } text || MarkupExtension.Parse(text) is null))
        {
            return false;
        }

        string written = value.Element?.Name.LocalName ?? MarkupExtension.Parse(value.Text!)!.Name;
        string name = written[(written.LastIndexOf(':') + 1)..];
        string kind = name.EndsWith("Extension", StringComparison.Ordinal) ? name[..^"Extension".Length] : name;
        if (kind == "Null")
        {
            return false;
        }

        if (kind is "Binding" or "CompiledBinding" or "ReflectionBinding" or "MultiBinding" or "TemplateBinding" or "DynamicResource")
        {
            return true;
        }

        Type? type = _types.Resolve(name).Type ?? (value.Element is null ? _types.Resolve(name + "Extension").Type : null);
        if (type is not null && ElementTypes.NamesOf(type).Contains("BindingBase", StringComparer.Ordinal))
        {
            return true;
        }

        return value.Element is not null && type is not null ? false : null;
    }

    private static string Where(XamlFile file, XElement element) =>
        LineOf(element) is { } line ? $"{file.RelativePath}:{line}" : file.RelativePath;

    /// <summary>A setter's value: its <c>Value</c> attribute, its <c>Setter.Value</c> element, or its content.</summary>
    /// <remarks>
    /// ⚠ <c>Value</c> is <c>Setter</c>'s content property, so <c>&lt;Setter Property="Template"&gt;&lt;ControlTemplate&gt;</c>
    /// sets it as surely as the other two spellings, and it is how Avalonia's own themes write their
    /// templates. Read as empty, it made a theme's template, container theme or focus undecidable.
    /// </remarks>
    internal static Setting ValueOf(XElement setter, XamlFile file)
    {
        if (setter.Attribute("Value") is { } attribute)
        {
            return new Setting(attribute.Value, null, file, setter);
        }

        if (setter.Elements().FirstOrDefault(child => child.Name.LocalName.EndsWith(".Value", StringComparison.Ordinal)) is { } valueElement)
        {
            return valueElement.Elements().FirstOrDefault() is { } value
                ? new Setting(null, value, file, setter)
                : new Setting(valueElement.Value, null, file, setter);
        }

        // Its own text, not a Setter.Property element's.
        return setter.Elements().FirstOrDefault(child => !child.Name.LocalName.Contains('.', StringComparison.Ordinal)) is { } content
            ? new Setting(null, content, file, setter)
            : new Setting(string.Concat(setter.Nodes().OfType<XText>().Select(text => text.Value)), null, file, setter);
    }

    /// <summary>Whether a setter sets <paramref name="property"/>, after any owner, in any spelling <see cref="ResourceScope.SetterProperty"/> reads.</summary>
    internal static bool SetsProperty(XElement element, string property) =>
        ResourceScope.SetterProperty(element) is { } written && ResourceScope.PropertyName(written) == property;

    internal static bool IsNull(string text) =>
        text.Replace(" ", string.Empty, StringComparison.Ordinal) is "{x:Null}" or "{Null}";

    internal static string Capitalised(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    internal static int? LineOf(XElement element) =>
        (element as IXmlLineInfo).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : null;
}
