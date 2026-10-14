using System.Reflection;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.Rules;

/// <summary>
/// Every template part a control looks up by name is declared in that control's own theme.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>This one fails silently and completely.</b> A control asks its template for
/// <c>PART_Minimap</c>; the theme spells it <c>PART_MiniMap</c>, or a refactor renamed one half.
/// The lookup returns null, the control skips the feature, and the build is clean, the app starts,
/// and nothing is logged. The minimap is simply not there. No other check in this library — and no
/// compiler — can see it, because the two halves live in different languages.
/// </para>
/// <para>
/// ⚠ <b>Only one direction is a defect.</b> A part the control looks up and the theme omits is
/// unambiguously broken. The reverse — a name in the theme that no control asks for — is NOT:
/// naming an element so a style can select it (<c>#PART_Badge</c>) is ordinary and correct. A rule
/// that reported both would generate false positives on well-written themes, which is how a rule
/// stops being run. Measured on a real theme set: 25 of 51 part names existed only for selectors.
/// </para>
/// <para>
/// ⛔ <b>Parts are matched inside their own <c>ControlTheme</c>, not across the file.</b> One
/// resource dictionary commonly holds several, and a file-wide match would let one control's theme
/// satisfy another's lookups — passing precisely when two templates have drifted apart, which is
/// the case worth catching.
/// </para>
/// <para>
/// ⭐ <b>Its reading is public.</b> <see cref="ReadParts"/> returns what the rule reads: each control's
/// constants, each lookup with the code that makes it, and the parts each of its themes declares.
/// <see cref="Analyze"/> derives its findings from that same reading, so a parts reference a consumer
/// builds from it lists exactly what the rule checked.
/// </para>
/// <para>
/// ⚠ <b>What the rule sees but cannot check is named in <see cref="XamlRuleResult.Skipped"/>.</b>
/// A control with a theme in the scan and no part found in its code contributes nothing to
/// <see cref="XamlRuleResult.Inspected"/>, and neither does one whose parts are known but whose
/// theme is not in the scan, since it may ship from another package. A lower count showed that
/// something fell out; the skip says which control, and why.
/// </para>
/// <para>
/// ⛔ <b>A scan given no assemblies checks nothing, and says so.</b> Parts are read from compiled
/// code, so without <see cref="XamlScanContext.WithAssemblies"/> there is nothing to read. That
/// used to return zero inspected and nothing else, which a consumer who copied a
/// <see cref="XamlScanContext.Load"/> call without it would read as success. Every themed control
/// in the scan is now named as skipped.
/// </para>
/// <para>
/// ⛔ <b>Build output whose dependencies are not beside it is read, never thrown at.</b> A control
/// whose base type lives in an assembly the process cannot load does not load itself, and the rule
/// threw out of <see cref="Analyze"/> at the first method body that named such a type. Now a control
/// the scanned assemblies define but could not load is named in <see cref="XamlRuleResult.Skipped"/>
/// with what stopped it, where it would otherwise pass for a framework control the scan was not
/// given, and a control whose code could be read only in part is checked on that part and named for
/// the rest.
/// </para>
/// </remarks>
public sealed class TemplatePartRule : IXamlRule
{
    /// <summary>The prefix that marks a named template part, by convention across XAML frameworks.</summary>
    public const string PartPrefix = "PART_";

    /// <inheritdoc />
    public string Id => "BNXQ1003";

    /// <inheritdoc />
    public string Summary => "Every template part a control looks up is declared in its own theme.";

    /// <inheritdoc />
    public XamlRuleResult Analyze(XamlScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Assemblies.Count == 0)
        {
            // Nothing to read parts from. Zero findings AND zero inspected, and every themed control
            // is named as skipped: a consumer who forgot WithAssemblies is told so, not reassured.
            // A scan with no themes had nothing to check, and says nothing.
            return XamlRuleResult.Clean(0) with
            {
                Skipped =
                [
                    .. ControlThemes.In(context).Select(theme => new XamlSkip(
                        theme.Target,
                        "The scan was given no assemblies, so this control's parts could not be read and "
                        + "nothing was checked. Call WithAssemblies with the assembly that defines it.",
                        theme.File.RelativePath,
                        theme.Line)),
                ],
            };
        }

        Reading reading = ReadControls(context);
        Dictionary<string, ControlTemplateParts> controls = reading.Parts.Controls.ToDictionary(control => control.Name, StringComparer.Ordinal);

        List<XamlFinding> findings = [];
        List<XamlSkip> skipped = [];
        HashSet<string> themed = new(StringComparer.Ordinal);
        int inspected = 0;

        foreach ((XamlFile file, string target, int? line, ThemeParts? theme) in reading.Themes)
        {
            themed.Add(target);

            if (reading.Shared.TryGetValue(target, out (string Types, bool HasParts) twins))
            {
                skipped.Add(new XamlSkip(
                    target,
                    $"More than one type the scan was given is named {target}: {twins.Types}. A ControlTheme names "
                    + "its target without the namespace this rule reads, so which one it themes is not known, and its "
                    + "parts were not checked.",
                    file.RelativePath,
                    line));
                continue;
            }

            if (theme is null)
            {
                if (reading.Known.Contains(target))
                {
                    skipped.Add(new XamlSkip(
                        target,
                        reading.PartlyRead.TryGetValue(target, out string? unread)
                            ? "It has a ControlTheme here, and no template part was found in the part of its "
                              + $"code that could be read, but the rest could not be: {unread}. So nothing was "
                              + $"checked. {LoadedTypes.Remedy}"
                            : "It has a ControlTheme here, but no template part was found in its code: no "
                              + "PART_ constant, and no PART_ literal passed to a Find or Get lookup. Either it "
                              + "has no parts, or it names them in a way this rule cannot read: built at "
                              + "runtime, looked up through a helper named otherwise, or looked up in code its "
                              + "template reaches only through an interface or virtual call, or in a lambda "
                              + "another type declares, which this rule credits to the type that declares that "
                              + "code.",
                        file.RelativePath,
                        line));
                }
                else if (reading.NotLoaded.TryGetValue(target, out (string Source, string Cause) notLoaded))
                {
                    // Not a framework control: the scan was given the assembly that defines it.
                    skipped.Add(new XamlSkip(
                        target,
                        $"It has a ControlTheme here, and {notLoaded.Source}, which the scan was given, defines "
                        + $"it, but it could not be loaded: {notLoaded.Cause}. Its parts were not read and "
                        + $"nothing was checked. {LoadedTypes.Remedy}",
                        file.RelativePath,
                        line));
                }

                // Any other theme is for a type the scanned assemblies do not define, which is not the
                // scan's to explain: it is usually a framework control's.
                continue;
            }

            HashSet<string> present = new(theme.Declared.Select(part => part.Name), StringComparer.Ordinal);

            foreach (string part in Wanted(controls[target]))
            {
                inspected++;
                if (present.Contains(part))
                {
                    continue;
                }

                findings.Add(new XamlFinding(
                    Id,
                    file.Path,
                    file.RelativePath,
                    line,
                    $"{target} looks up the template part '{part}', and this ControlTheme does "
                    + $"not declare it. The lookup will return null at runtime and the feature "
                    + $"behind it will be silently absent. Add Name=\"{part}\" to the element "
                    + "that plays that part, or drop the lookup."));
            }

            if (reading.PartlyRead.TryGetValue(target, out string? partly))
            {
                skipped.Add(new XamlSkip(
                    target,
                    $"Part of its code could not be read: {partly}. A part it looks up there was not "
                    + $"checked, though the parts found in the rest were. {LoadedTypes.Remedy}",
                    file.RelativePath,
                    line));
            }
        }

        foreach (ControlTemplateParts control in reading.Parts.Controls.Where(control => !themed.Contains(control.Name)))
        {
            skipped.Add(new XamlSkip(
                control.Name,
                $"It declares {string.Join(", ", Wanted(control))}, but no ControlTheme for it is in the scanned "
                + "markup, so those parts were not checked. Its theme may ship from another package; "
                + "if it is yours, scan the folder that holds it. Or these parts are another control's, looked up "
                + "in code its template reaches only through an interface or virtual call, or in a lambda this "
                + "type declares, which this rule does not follow, so that control's theme was not checked for them."));
        }

        foreach (KeyValuePair<string, (string Types, bool HasParts)> twin in reading.Shared
                     .Where(twin => twin.Value.HasParts && !themed.Contains(twin.Key))
                     .OrderBy(twin => twin.Key, StringComparer.Ordinal))
        {
            skipped.Add(new XamlSkip(
                twin.Key,
                $"More than one type the scan was given is named {twin.Key}: {twin.Value.Types}, and parts were "
                + "found in their code. A ControlTheme names its target without the namespace this rule reads, so "
                + "their parts were not read as one control's, and none was checked."));
        }

        return new XamlRuleResult(findings, inspected) { Skipped = skipped };
    }

    /// <summary>
    /// What this rule reads from a scan: each control's template parts, the code that looks each up, and the parts
    /// each of its themes declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⭐ <b>The reading <see cref="Analyze"/> checks, made public.</b> A consumer that builds a parts reference from
    /// it lists the parts the rule checked, read as the rule read them: the constants a control declares apart from
    /// the parts some code looks up, whether each lookup is the control's own code, a base class's or a helper's it
    /// hands its template to, and what each of its themes declares, with the element and the automation id. Read
    /// twice, by the rule and by a consumer, one fact drifts.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads what <see cref="Analyze"/> reads, the context's markup and assemblies, and nothing else.</b> A
    /// base class in an assembly the scan was not given is not read, so pass the library that defines it to
    /// <see cref="XamlScanContext.WithAssemblies"/> as well. Given no assemblies, it reads nothing, and one skip says
    /// so.
    /// </para>
    /// <para>
    /// ⚠ <b>An entry is every type the rule credits a part to</b>, by the simple name a <c>ControlTheme</c>'s
    /// <c>TargetType</c> gives. That is a control, or a helper no control hands the template its lookups are made on,
    /// whose lookups the rule keeps as its own. A themed control that declares no part has no entry: the rule names
    /// it in a skip instead. Nor has a name more than one top-level type the scan was given carries, since the rule
    /// reads a theme's target by its name alone and so cannot say which of them a theme is for: the reading names it in
    /// its skips, where parts were found in their code.
    /// </para>
    /// </remarks>
    /// <param name="context">The scan, with the assemblies that define its controls.</param>
    /// <returns>Each control's parts, in an order that compares as text, and what could not be read.</returns>
    public static TemplatePartReading ReadParts(XamlScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ReadControls(context).Parts;
    }

    /// <summary>
    /// The part names each type declares in its compiled code, with the code that looks each up, the parts each
    /// theme in the scan declares, and the name of every type the scan can see.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⭐ <b>Two sources, because controls use both.</b> A part name is a <c>PART_</c> string
    /// constant, public or not, or a <c>PART_</c> string literal the type's code passes to a lookup.
    /// The literal is the case constants alone missed: <c>NameScope.Find("PART_Foo")</c> in an
    /// <c>OnApplyTemplate</c> override declares a part as surely as a constant does, and a rule that
    /// read only public constants reported such a control clean while checking none of it. Non-public
    /// types are read too, for two reasons: internal controls exist, and every lambda's closure is a
    /// private nested type, so reading public types alone loses each literal written in a lambda.
    /// </para>
    /// <para>
    /// ⛔ <b>Only a literal passed to a LOOKUP counts</b>: a call whose name starts with <c>Find</c> or
    /// <c>Get</c>, which covers <c>NameScope.Find</c>, <c>FindControl</c>, <c>FindName</c>,
    /// <c>Get</c> and <c>GetTemplateChild</c>. Compiled XAML loads every element name too, passing it
    /// to <c>set_Name</c> and <c>Register</c> to name the element, or to <c>Name</c> to build a
    /// selector. Counting those made every compiled theme look like a control with parts. Measured on
    /// a real codebase before this filter: 34 true lookups, every one followed by <c>Find</c>, beside
    /// 162 literals in compiled XAML, every one followed by one of those three.
    /// </para>
    /// <para>
    /// ⭐ <b>A lookup belongs to the control whose template it is made on, whichever type's code
    /// makes it.</b> A control can hand its template to a helper: in one real codebase two controls
    /// each pass the arguments of their <c>OnApplyTemplate</c> to one controller, which makes every
    /// lookup. Credited to the type whose code makes them, those lookups belonged to a type with no
    /// theme, and reached a consumer only as the prose of a skip. So a lookup is credited to each
    /// control that hands the method making it its template, or the template's name scope: from the
    /// <c>OnApplyTemplate</c> the control resolves to, through any chain of calls in the scanned
    /// assemblies that passes either on. A base class's lookups reach a subclass the same way, since the
    /// base's <c>OnApplyTemplate</c> runs on the subclass's template. A method no control hands its
    /// template to keeps its lookups as its own type's, as a handler on another control's template does.
    /// Each lookup keeps where it came from: the control's own code, a base class's, or another type's,
    /// a type nested in the control included, and that type's full name, its method's compiled name and
    /// its assembly's. A closure's code is the type that declares it.
    /// </para>
    /// <para>
    /// ⚠ <b>Lambdas, local functions and iterators compile into nested types</b>, so their literals
    /// are credited to the outermost type that declares them, the one a <c>ControlTheme</c> targets.
    /// </para>
    /// <para>
    /// ⚠ <b>Four limits.</b> A name built at runtime (<c>"PART_" + name</c>) is invisible, and so is
    /// a lookup through a helper whose name does not start with <c>Find</c> or <c>Get</c>; a control
    /// left with no parts that way is named in <see cref="XamlRuleResult.Skipped"/>. A lookup no
    /// control hands its template to is taken to be on its own type's template, so one that reaches
    /// into a child control's template is checked against this type's theme, and reported there. And
    /// the template is followed through direct calls only: a call through an interface or a virtual
    /// method names the method declared, not the one that runs, and creating a lambda calls nothing, so
    /// code the template reaches only that way keeps its lookups as its own type's. Following every
    /// implementation would charge one control with another's lookups: in one real codebase two controls
    /// hand one controller their templates, it calls back through an interface, and only one of the two
    /// has the part its implementation looks up. So the skips that name a control with no part found,
    /// and parts with no theme, say so instead.
    /// </para>
    /// <para>
    /// ⛔ <b>A name more than one top-level type carries is no control's.</b> This rule reads a
    /// <c>ControlTheme</c>'s target by its name alone, not by the namespace its prefix gives, so which of
    /// them a theme is for is not known, and read as one, a theme for either would be checked against both
    /// classes' parts. Each theme of such a name is named in <see cref="XamlRuleResult.Skipped"/>, as
    /// <c>BNXQ1006</c> names it, and so is the name where parts were found in its types' code and nothing
    /// themes it. A theme whose prefix names the one type with parts is skipped too, so a check the prefix
    /// would allow is lost; reading the prefix, as <c>ThemeReader</c> does, would be a change of its own.
    /// </para>
    /// </remarks>
    private static Reading ReadControls(XamlScanContext context)
    {
        HashSet<string> known = new(StringComparer.Ordinal);
        Dictionary<string, string> partlyRead = new(StringComparer.Ordinal);
        Dictionary<string, (string Source, string Cause)> notLoaded = new(StringComparer.Ordinal);

        if (context.Assemblies.Count == 0)
        {
            XamlSkip nothingRead = new(
                "Assemblies",
                "The scan was given no assemblies, so no template part was read. Call WithAssemblies with the "
                + "assemblies that define the controls, and with any library that defines a base class of theirs.");
            return new Reading(new TemplatePartReading([], [nothingRead]), [], known, partlyRead, notLoaded, new(StringComparer.Ordinal));
        }

        HashSet<Assembly> scanned = [.. context.Assemblies];
        Dictionary<string, SortedSet<string>> constants = new(StringComparer.Ordinal);
        Dictionary<(Module Module, int Token), Lookup> lookups = [];
        List<Type> topLevel = [];

        foreach (Assembly assembly in context.Assemblies)
        {
            LoadedTypes types = LoadedTypes.Of(assembly);
            foreach ((string name, string cause) in types.NotLoaded)
            {
                notLoaded.TryAdd(name, (types.Source, cause));
            }

            foreach (Type type in types.Loaded)
            {
                // A nested type whose enclosing type did not load belongs to a control that did not
                // either, and that control is already among the types that did not load.
                // Compiler-generated at the top level: <Module>, <PrivateImplementationDetails>.
                if (OwnerOf(type) is not { } owner || owner.Name.StartsWith('<'))
                {
                    continue;
                }

                known.Add(owner.Name);
                if (owner == type)
                {
                    topLevel.Add(type);
                }

                (List<string> declared, List<(MethodBase Method, List<string> Parts)> made) = PartNamesIn(type, out string? unread);
                foreach (string constant in declared)
                {
                    if (!constants.TryGetValue(owner.Name, out SortedSet<string>? named))
                    {
                        named = new SortedSet<string>(StringComparer.Ordinal);
                        constants[owner.Name] = named;
                    }

                    named.Add(constant);
                }

                foreach ((MethodBase method, List<string> parts) in made)
                {
                    lookups[KeyOf(method)] = new Lookup(owner, CodeTypeOf(type), method.Name, types.Source, parts);
                }

                if (unread is not null)
                {
                    partlyRead.TryAdd(owner.Name, unread);
                }
            }
        }

        // The names more than one top-level type carries, each with the full names that carry it. This rule reads a
        // theme's target by its name alone, so it cannot say which of them a theme is for.
        Dictionary<string, string[]> twins = topLevel
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .Select(named => (Name: named.Key, Types: FullNames(named)))
            .Where(named => named.Types.Length > 1)
            .ToDictionary(named => named.Name, named => named.Types, StringComparer.Ordinal);

        // Which controls hand each method their template.
        Dictionary<(Module Module, int Token), List<Type>> handedBy = [];
        foreach (Type control in topLevel)
        {
            foreach ((Module Module, int Token) reached in HandedTemplate(control, scanned))
            {
                if (!handedBy.TryGetValue(reached, out List<Type>? hosts))
                {
                    hosts = [];
                    handedBy[reached] = hosts;
                }

                hosts.Add(control);
            }
        }

        Dictionary<string, HashSet<TemplatePartLookup>> credited = new(StringComparer.Ordinal);
        foreach (((Module Module, int Token) method, Lookup lookup) in lookups)
        {
            IEnumerable<(string Host, TemplatePartOrigin Origin)> creditedTo = handedBy.TryGetValue(method, out List<Type>? hosts)
                ? hosts.Select(host => (host.Name, OriginOf(lookup.Code, host)))
                : [(lookup.Owner.Name, TemplatePartOrigin.Own)];

            foreach ((string host, TemplatePartOrigin origin) in creditedTo)
            {
                if (!credited.TryGetValue(host, out HashSet<TemplatePartLookup>? parts))
                {
                    parts = [];
                    credited[host] = parts;
                }

                foreach (string part in lookup.Parts)
                {
                    parts.Add(new TemplatePartLookup(part, origin, lookup.Code.FullName ?? lookup.Code.Name, lookup.Method, lookup.Assembly));
                }
            }
        }

        // The themes, in the scan's order: by file, and within a file as they are written.
        Dictionary<string, List<ThemeParts>> themesOf = new(StringComparer.Ordinal);
        List<ThemeSeen> themes = [];
        foreach ((XamlFile file, XElement theme, string target, int? line) in ControlThemes.In(context))
        {
            ThemeParts? parts = null;
            if (constants.ContainsKey(target) || credited.ContainsKey(target))
            {
                parts = new ThemeParts(file.RelativePath, line, PartsDeclaredIn(theme));
                if (!themesOf.TryGetValue(target, out List<ThemeParts>? targeting))
                {
                    targeting = [];
                    themesOf[target] = targeting;
                }

                targeting.Add(parts);
            }

            themes.Add(new ThemeSeen(file, target, line, parts));
        }

        ControlTemplateParts[] controls =
        [
            .. constants.Keys.Union(credited.Keys, StringComparer.Ordinal)
                .Where(name => !twins.ContainsKey(name))
                .Order(StringComparer.Ordinal)
                .Select(name => new ControlTemplateParts(
                    name,
                    constants.TryGetValue(name, out SortedSet<string>? named) ? [.. named] : [],
                    credited.TryGetValue(name, out HashSet<TemplatePartLookup>? looked)
                        ?
                        [
                            .. looked.OrderBy(lookup => lookup.Part, StringComparer.Ordinal)
                                .ThenBy(lookup => lookup.Type, StringComparer.Ordinal)
                                .ThenBy(lookup => lookup.Method, StringComparer.Ordinal)
                                .ThenBy(lookup => lookup.Assembly, StringComparer.Ordinal)
                                .ThenBy(lookup => lookup.Origin),
                        ]
                        : [],
                    themesOf.TryGetValue(name, out List<ThemeParts>? targeting) ? targeting : [])),
        ];

        Dictionary<string, (string Types, bool HasParts)> shared = twins.ToDictionary(
            twin => twin.Key,
            twin => (LoadedTypes.List(twin.Value), constants.ContainsKey(twin.Key) || credited.ContainsKey(twin.Key)),
            StringComparer.Ordinal);

        XamlSkip[] skipped =
        [
            .. notLoaded.Select(entry => new XamlSkip(
                    entry.Key,
                    $"{entry.Value.Source}, which the scan was given, defines it, but it could not be loaded: "
                    + $"{entry.Value.Cause}. Its parts were not read. {LoadedTypes.Remedy}"))
                .Concat(partlyRead.Select(entry => new XamlSkip(
                    entry.Key,
                    $"Part of its code could not be read: {entry.Value}. A part it looks up there is not in this "
                    + $"reading, though the parts found in the rest are. {LoadedTypes.Remedy}")))
                .Concat(shared.Where(twin => twin.Value.HasParts).Select(twin => new XamlSkip(
                    twin.Key,
                    $"More than one type the scan was given is named {twin.Key}: {twin.Value.Types}. A ControlTheme "
                    + "names its target without the namespace this rule reads, so whose parts these are is not known, "
                    + "and none of them is in this reading.")))
                .OrderBy(skip => skip.Subject, StringComparer.Ordinal)
                .ThenBy(skip => skip.Reason, StringComparer.Ordinal),
        ];

        return new Reading(new TemplatePartReading(controls, skipped), themes, known, partlyRead, notLoaded, shared);
    }

    /// <summary>
    /// What a control's themes must declare: its constants and the parts looked up on its template, in ordinal order.
    /// </summary>
    private static SortedSet<string> Wanted(ControlTemplateParts control) =>
        new([.. control.Constants, .. control.Lookups.Select(lookup => lookup.Part)], StringComparer.Ordinal);

    /// <summary>
    /// Whose code a lookup is, as seen from <paramref name="host"/>, which hands the method in <paramref name="code"/> its
    /// template: its own, a base class's, or another type's, a type nested in it included.
    /// </summary>
    private static TemplatePartOrigin OriginOf(Type code, Type host)
    {
        if (code == host)
        {
            return TemplatePartOrigin.Own;
        }

        try
        {
            for (Type? link = host.BaseType; link is not null; link = link.BaseType)
            {
                if ((link.IsConstructedGenericType ? link.GetGenericTypeDefinition() : link) == code)
                {
                    return TemplatePartOrigin.BaseClass;
                }
            }
        }
        catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
        {
            // A chain that cannot be read names no base, and the lookup is read as another type's.
        }

        return TemplatePartOrigin.Helper;
    }

    /// <summary>
    /// The part names <paramref name="type"/> declares in its own members: its <c>PART_</c>
    /// constants, and the lookups each of its methods makes, with what stopped the rest being read in
    /// <paramref name="unread"/>, or <c>null</c> when nothing did.
    /// </summary>
    /// <remarks>
    /// ⛔ <b>Each field and each method is read on its own</b>, so one that needs an assembly the
    /// process cannot load costs only itself. Reading a method's body loads the types of its locals,
    /// and one naming a framework type the scan's process does not have threw out of
    /// <see cref="Analyze"/>: <c>FileNotFoundException</c>, measured on a library's Release output.
    /// </remarks>
    private static (List<string> Constants, List<(MethodBase Method, List<string> Parts)> Lookups) PartNamesIn(Type type, out string? unread)
    {
        const BindingFlags Everything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                                        | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        List<string> constants = [];
        List<(MethodBase Method, List<string> Parts)> lookups = [];
        List<Exception> failures = [];

        foreach (FieldInfo field in Read(() => type.GetFields(Everything), failures) ?? [])
        {
            if (Read(() => field.IsLiteral && field.FieldType == typeof(string) ? field.GetRawConstantValue() as string : null, failures)
                    is { } constant
                && IsPartName(constant))
            {
                constants.Add(constant);
            }
        }

        MethodBase[] methods = Read(() => (MethodBase[])[.. type.GetMethods(Everything), .. type.GetConstructors(Everything)], failures) ?? [];
        foreach (MethodBase method in methods)
        {
            List<string> parts = Read(
                () => CompiledStrings.LoadedBy(method, IsPartName)
                    .Where(loaded => IsLookup(loaded.NextCall))
                    .Select(loaded => loaded.Value)
                    .ToList(),
                failures) ?? [];
            if (parts.Count > 0)
            {
                lookups.Add((method, parts));
            }
        }

        unread = failures.Count == 0 ? null : LoadedTypes.CauseOf(failures);
        return (constants, lookups);
    }

    /// <summary>
    /// Every method a control's template reaches: the <c>OnApplyTemplate</c> the control resolves to,
    /// when the scanned assemblies declare it, and each method in them that the template, or the
    /// template's name scope, is handed to, however many calls deep.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Handing the template on is a call to a method with a parameter that can take what
    /// <c>OnApplyTemplate</c> was given</b>, or a value its type declares as a property, such as a
    /// name scope. A parameter of type <see cref="object"/> takes anything, so it is not counted. Read
    /// by name and signature, so no framework type is referenced: a framework whose
    /// <c>OnApplyTemplate</c> takes nothing hands nothing on, and its controls read as they always did.
    /// </remarks>
    private static IReadOnlyCollection<(Module Module, int Token)> HandedTemplate(Type control, HashSet<Assembly> scanned)
    {
        MethodInfo? start;
        Type[] handed;
        try
        {
            start = ResolvedOnApplyTemplate(control);
            if (start is null || !scanned.Contains(start.Module.Assembly))
            {
                return [];
            }

            handed = HandedTypes(start);
        }
        catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
        {
            return [];
        }

        HashSet<(Module Module, int Token)> reached = [KeyOf(start)];
        Queue<MethodBase> pending = new([start]);
        while (pending.TryDequeue(out MethodBase? method))
        {
            foreach (MethodBase callee in CompiledStrings.CalledBy(method))
            {
                bool handedOn;
                try
                {
                    handedOn = scanned.Contains(callee.Module.Assembly) && Takes(callee, handed);
                }
                catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
                {
                    handedOn = false;
                }

                if (handedOn && reached.Add(KeyOf(callee)))
                {
                    pending.Enqueue(callee);
                }
            }
        }

        return reached;
    }

    /// <summary>
    /// The <c>OnApplyTemplate</c> a control's template is applied through: the most-derived one in its
    /// chain that takes one argument, or <c>null</c> when none does.
    /// </summary>
    private static MethodInfo? ResolvedOnApplyTemplate(Type control)
    {
        const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        for (Type? link = control; link is not null; link = link.BaseType)
        {
            if (link.GetMethods(Declared).FirstOrDefault(method => method.Name == "OnApplyTemplate" && method.GetParameters().Length == 1)
                is { } declared)
            {
                return declared;
            }
        }

        return null;
    }

    /// <summary>
    /// What <paramref name="onApplyTemplate"/> is given, and the types of the properties that type
    /// declares, such as its name scope; never <see cref="object"/>, <see cref="string"/> or a primitive.
    /// </summary>
    private static Type[] HandedTypes(MethodInfo onApplyTemplate)
    {
        Type argument = onApplyTemplate.GetParameters()[0].ParameterType;
        return
        [
            argument,
            .. argument.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(property => property.PropertyType)
                .Where(type => type != typeof(object) && type != typeof(string) && !type.IsPrimitive && !type.IsEnum),
        ];
    }

    /// <summary>Whether a parameter of <paramref name="callee"/> can take one of <paramref name="handed"/>.</summary>
    private static bool Takes(MethodBase callee, Type[] handed) =>
        callee.GetParameters().Any(parameter =>
            parameter.ParameterType != typeof(object) && handed.Any(type => parameter.ParameterType.IsAssignableFrom(type)));

    /// <summary>The full names of <paramref name="types"/>, each once, in ordinal order, so a reading compares as text.</summary>
    private static string[] FullNames(IEnumerable<Type> types) =>
        [.. types.Select(type => type.FullName ?? type.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>A method's identity across the reads that find it: its module and metadata token.</summary>
    private static (Module Module, int Token) KeyOf(MethodBase method) => (method.Module, method.MetadataToken);

    /// <summary>
    /// The outermost type declaring <paramref name="type"/>, which is the one a <c>ControlTheme</c>
    /// targets, or <c>null</c> when an enclosing type does not load.
    /// </summary>
    private static Type? OwnerOf(Type type)
    {
        try
        {
            Type owner = type;
            while (owner.DeclaringType is { } outer)
            {
                owner = outer;
            }

            return owner;
        }
        catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
        {
            return null;
        }
    }

    /// <summary>
    /// The type whose source holds <paramref name="type"/>'s code: the type itself, or, for what the compiler nests in a
    /// type of its own, a closure, an iterator or an async method's state machine, the nearest type that declares it
    /// and that the compiler did not write.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A type a programmer nests is a type of its own.</b> The rule credits the lookups no control hands a
    /// template to to the outermost type, as it does a closure's, but the code that makes them is the nested type's,
    /// so a reading names that type, not the outermost one, which may have no method of that name.
    /// </remarks>
    private static Type CodeTypeOf(Type type)
    {
        Type code = type;
        while (code.Name.StartsWith('<') && code.DeclaringType is { } outer)
        {
            code = outer;
        }

        return code;
    }

    /// <summary>A reflective read, or <c>null</c> with the exception kept when what it needs does not load.</summary>
    private static T? Read<T>(Func<T?> read, List<Exception> failures)
        where T : class
    {
        try
        {
            return read();
        }
        catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
        {
            failures.Add(ex);
            return null;
        }
    }

    /// <summary>A part name: the prefix and something after it.</summary>
    /// <remarks>
    /// ⚠ The bare prefix is not a part. <c>"PART_" + name</c> loads exactly <c>"PART_"</c>, and
    /// reading it as a name would report a part called <c>PART_</c> that no theme could declare.
    /// </remarks>
    private static bool IsPartName(string value) =>
        value.Length > PartPrefix.Length && value.StartsWith(PartPrefix, StringComparison.Ordinal);

    /// <summary>Whether a call looks a name up, as opposed to registering it or building a selector.</summary>
    private static bool IsLookup(string? call) =>
        call is not null
        && (call.StartsWith("Find", StringComparison.Ordinal) || call.StartsWith("Get", StringComparison.Ordinal));

    /// <summary>
    /// The parts a theme declares: each element inside it named as a part, in either naming spelling, in the order the
    /// theme declares them, with the element's local name and the automation id it declares.
    /// </summary>
    /// <remarks>
    /// An element that gives one name in both spellings declares one part; one that gives two names, which does not
    /// load, declares both, as the rule has always compared both.
    /// </remarks>
    private static List<DeclaredPart> PartsDeclaredIn(XElement theme)
    {
        List<DeclaredPart> declared = [];

        foreach (XElement element in theme.Descendants())
        {
            // "Name" and x:"Name" both land here as the local name "Name".
            foreach (string name in element.Attributes()
                         .Where(attribute => string.Equals(attribute.Name.LocalName, "Name", StringComparison.Ordinal) && IsPartName(attribute.Value))
                         .Select(attribute => attribute.Value)
                         .Distinct(StringComparer.Ordinal))
            {
                declared.Add(new DeclaredPart(name, element.Name.LocalName, AutomationIdentifier.ValueOf(element)));
            }
        }

        return declared;
    }

    /// <summary>
    /// What the scan says: the public reading; every theme in the scan's order, with the parts it declares where it
    /// targets a type with parts; the name of every type that loaded; each type whose code could not all be read,
    /// with why; each type the assemblies define that did not load, with the assembly that defines it and why; and each
    /// name more than one top-level type carries, with the types listed and whether parts were found in their code.
    /// </summary>
    private sealed record Reading(
        TemplatePartReading Parts,
        IReadOnlyList<ThemeSeen> Themes,
        HashSet<string> Known,
        Dictionary<string, string> PartlyRead,
        Dictionary<string, (string Source, string Cause)> NotLoaded,
        Dictionary<string, (string Types, bool HasParts)> Shared);

    /// <summary>A theme in the scan, and the parts it declares where it targets a type with parts, else <c>null</c>.</summary>
    private sealed record ThemeSeen(XamlFile File, string Target, int? Line, ThemeParts? Parts);

    /// <summary>
    /// A method's lookups: the outermost type that declares it, which the rule credits them to; the type whose code it
    /// is, which a reading names; its compiled name; that type's assembly; and the parts.
    /// </summary>
    private sealed record Lookup(Type Owner, Type Code, string Method, string Assembly, List<string> Parts);
}
