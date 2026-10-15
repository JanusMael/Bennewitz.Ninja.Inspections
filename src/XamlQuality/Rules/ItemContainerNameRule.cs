using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static Bennewitz.Ninja.XamlQuality.StyleSelectors;

namespace Bennewitz.Ninja.XamlQuality.Rules;

/// <summary>
/// Every item container generated from <c>ItemsSource</c> is named by what it shows, not by its
/// item's type.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A row an items control generates is named by a fallback unless something names it.</b> A
/// <c>ListBoxItem</c>, <c>ComboBoxItem</c> or <c>TabItem</c> with no <c>AutomationProperties.Name</c>
/// of its own takes the text of a <c>TextBlock</c> at the root of what it presents, else that root's
/// own automation name, else <c>ToString()</c> on its item. A view model that writes no
/// <c>ToString()</c> gives every row its type's full name, and a record gives its type and every
/// property it has. Measured on Avalonia 12.1.3 under Fluent: with a panel or a border at the root of
/// the item template, rows were named <c>Probe.Items.Plain</c>, the type's full name, and a record's
/// <c>Rec { Label = Alpha }</c>; a <c>TextBlock</c> root, a named root, a <c>ToString()</c> override,
/// a container style and an <c>ItemContainerTheme</c> each named them <c>Alpha</c>.
/// </para>
/// <para>
/// ⚠ <b>A <c>TreeViewItem</c> has no fallback at all.</b> Its peer reads
/// <c>AutomationProperties.Name</c> and nothing else, so whatever its template shows, its name is
/// empty until a container style or its <c>ItemContainerTheme</c> gives it one. Measured: all 12
/// item and template shapes tried gave an empty name, and both ways of naming it named the nested
/// levels too.
/// </para>
/// <para>
/// ⭐ <b>What names a container, in the order the rule asks.</b> A <c>Style</c> in the host's own
/// <c>Styles</c>, an ancestor's or the application's, whose selector is the container type alone;
/// the <c>ItemContainerTheme</c> the host or its theme sets, else the container's implicit theme,
/// through its <c>BasedOn</c> chain; for a list or tab row, <c>DisplayMemberBinding</c>; then the
/// item template's root: a <c>TextBlock</c> with text, or any other root that declares
/// <c>AutomationProperties.Name</c> or <c>LabeledBy</c>. A <c>TextBlock</c>'s own automation name does
/// not count, because its peer reads only its text: measured, a named <c>TextBlock</c> with no text
/// left the row to <c>ToString()</c>. When the root is a panel or a decorator that declares neither,
/// or a <c>TextBlock</c> with no text, the name falls to <c>ToString()</c> on the items' type: the one
/// the template's <c>x:DataType</c> names, else the one its <c>ItemsSource</c> binding reads, which is
/// reported when it is <c>object</c>'s or <c>ValueType</c>'s, or the one the compiler writes for a
/// record.
/// </para>
/// <para>
/// ⭐ <b>With no item template, a data template chosen by the items' type presents each row, else their
/// <c>ToString()</c>.</b> A row's presenter takes the first data template that matches its item: its own
/// <c>ContentTemplate</c>, then each logical ancestor's <c>DataTemplates</c> in turn, nearest first and each in
/// order, then the application's; with none it shows the item's <c>ToString()</c> in a <c>TextBlock</c>, by
/// <c>ContentPresenter.CreateChild</c>, <c>DataTemplateExtensions.FindDataTemplate</c> and
/// <c>FuncDataTemplate.Default</c> at 12.1.3. A <c>DataTemplate</c> matches an item that is an instance of its
/// <c>DataType</c>, which an <c>x:DataType</c> gives it where none is written (<c>XDataTypeTransformer</c>); one with
/// neither, which would match every item, is refused by the collection that holds it, so its markup throws as it
/// loads. So the rule reads the items' type and asks the <c>DataTemplates</c> collections in that order: one that
/// matches every item presents the rows, and its root is read as an item template's is; one that may match only
/// some of them, or whose matching its own code decides, is named in <see cref="XamlRuleResult.Skipped"/>; and with
/// none, the rows are named by the items' <c>ToString()</c>. A collection is searched where markup shows it reaching
/// the list: the list's own, an ancestor's, and the application's. One in another view may reach it from where that
/// view is placed at runtime, and one that may match is named in <see cref="XamlRuleResult.Skipped"/>; one beside the
/// list in the same file does not reach it. Measured under Fluent, Simple and Semi, with no item template: list and
/// tab rows of a type that writes no <c>ToString()</c> were named by its full name, a record's by its compiler's, and
/// strings, an enum's members and a type's own <c>ToString()</c> by their text; a data template for the items' type,
/// written with <c>DataType</c> or with <c>x:DataType</c>, in the list's <c>DataTemplates</c>, an ancestor's or the
/// application's, named them by its <c>TextBlock</c> root's text; one whose <c>x:DataType</c> names another type was
/// passed over; one whose root is a <c>StackPanel</c> left them to <c>ToString()</c>; and one with no data type
/// stopped the markup loading.
/// </para>
/// <para>
/// ⭐ <b>The items' type is what the <c>ItemsSource</c> binding's path reads, as the XAML compiler reads it.</b>
/// Where no item template's <c>x:DataType</c> names it, the rule reads the binding's path against the data type
/// in scope, as <c>AvaloniaXamlIlDataContextTypeTransformer</c> infers it at 12.1.3: the nearest
/// <c>x:DataType</c>, a data template's <c>DataType</c>, or, for an item template that declares neither, the
/// items of the control it templates. A cast in the path, as in <c>((vm:Model)DataContext).Items</c>, starts it
/// again at the cast's type. Each step is the public instance property of that name, and the items are the
/// <c>T</c> of the <c>IEnumerable&lt;T&gt;</c> the last one's type is, or a subclass of it. A plain
/// <c>{Binding}</c> is read as compiled, as <c>AvaloniaUseCompiledBindingsByDefault</c> makes it at 12.1.3
/// where a project leaves the property unset. Measured: an item template that declares no data type, whose root is a
/// <c>StackPanel</c>, left the rows to the <c>ToString()</c> of the items the list's <c>ItemsSource</c> gave it.
/// </para>
/// <para>
/// ⛔ <b>What markup cannot decide is named, not guessed.</b> A selector with a state, a class, a
/// name or a context; a naming style in another file; a style that sets the host's template or the
/// container's theme; an item template, or a data template chosen by type, whose root names the rows
/// through a peer of its own; with no item template, a content template the containers' theme or a style
/// gives them, a data template that may match only some of the items, and one with no data type, which stops the
/// markup loading; an items' type that neither an
/// item template's <c>x:DataType</c> nor the <c>ItemsSource</c> binding reads, because the binding is a
/// reflection one, takes a converter or a source of its own, follows a <c>DataContext</c> set on its way,
/// or steps through an indexer, an attached property or an element; an item type that is abstract, an
/// interface, a control, or has a subclass that writes its own <c>ToString()</c>; and a container whose
/// peer writes its own name are all named in <see cref="XamlRuleResult.Skipped"/>. So is every
/// <c>ItemsSource</c> in a scan given no assemblies, because which container an items control generates,
/// and how that container is named, are read from compiled types.
/// </para>
/// <para>
/// ⚠ <b>Only rows whose naming was measured are checked.</b> A container is a subject when the peer
/// its <c>OnCreateAutomationPeer</c> creates is Avalonia's <c>ListItemAutomationPeer</c> or
/// <c>TreeViewItemAutomationPeer</c>, read from the container's compiled code, so another
/// framework's rows are left alone. Before Avalonia 12.1.2, by its source, a list row read only a
/// <c>TextBlock</c> root's <c>Text</c>, so a name on any other root did not reach it. The rule reads
/// 12.1.2's order: on an older Avalonia it can miss such a row, and never reports a named one.
/// </para>
/// </remarks>
public sealed class ItemContainerNameRule : IXamlRule
{
    /// <inheritdoc />
    public string Id => "BNXQ1009";

    /// <inheritdoc />
    public string Summary => "Every item container generated from ItemsSource is named by what it shows, not by its item's type.";

    /// <inheritdoc />
    public XamlRuleResult Analyze(XamlScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<(XamlFile File, XElement Host)> hosts = [.. HostsIn(context)];

        if (hosts.Count == 0)
        {
            // Nothing to check, so nothing to resolve: no type is loaded.
            return XamlRuleResult.Clean(0);
        }

        if (context.Assemblies.Count == 0)
        {
            return XamlRuleResult.Clean(0) with
            {
                Skipped =
                [
                    .. hosts.Select(entry => SkipOf(entry.File, entry.Host,
                        "The scan was given no assemblies, so whether this control generates item containers, and "
                        + "how they are named, could not be read and nothing was checked. Call WithAssemblies with "
                        + "the assembly whose markup this is; the framework is reached through its references.")),
                ],
            };
        }

        Analysis analysis = new(context);
        List<XamlFinding> findings = [];
        List<XamlSkip> skipped = [];
        int inspected = 0;

        foreach ((XamlFile file, XElement host) in hosts)
        {
            Verdict verdict;
            try
            {
                verdict = analysis.Decide(file, host);
            }
            catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
            {
                // A consumer's build output missing a dependency is a skip, never a failed scan.
                verdict = Verdict.Undecided(
                    $"Reading the compiled types behind this {host.Name.LocalName} failed with {ex.GetType().Name}: "
                    + $"{ex.Message} Pass assemblies whose dependencies load beside them.");
            }

            switch (verdict.Outcome)
            {
                case Outcome.NotASubject:
                    break;
                case Outcome.Undecided:
                    skipped.Add(SkipOf(file, host, verdict.Text));
                    break;
                default:
                    inspected++;
                    if (verdict.Outcome == Outcome.Unnamed)
                    {
                        findings.Add(new XamlFinding(Id, file.Path, file.RelativePath, ThemeReader.LineOf(host), verdict.Text));
                    }

                    break;
            }
        }

        return new XamlRuleResult(findings, inspected) { Skipped = skipped };
    }

    /// <summary>
    /// Every element that may set <c>ItemsSource</c>, as an attribute, after any owner, or as a property
    /// element, but a template: a <c>TreeDataTemplate</c> sets its children's source, and generates no rows
    /// itself. Whether an owner makes it the element's own is decided once its type is read.
    /// </summary>
    private static IEnumerable<(XamlFile File, XElement Host)> HostsIn(XamlScanContext context)
    {
        foreach (XamlFile file in context.ParsedFiles)
        {
            foreach (XElement element in file.Document!.Descendants())
            {
                if (!element.Name.LocalName.EndsWith("Template", StringComparison.Ordinal)
                    && ThemeReader.MaySet(element, "ItemsSource"))
                {
                    yield return (file, element);
                }
            }
        }
    }

    private static XamlSkip SkipOf(XamlFile file, XElement host, string reason) =>
        new(host.Name.LocalName, reason, file.RelativePath, ThemeReader.LineOf(host));

    private enum Outcome
    {
        NotASubject,
        Named,
        Unnamed,
        Undecided,
    }

    /// <summary>What was decided about one control's item containers, and the text that explains it.</summary>
    private readonly record struct Verdict(Outcome Outcome, string Text)
    {
        public static readonly Verdict NotASubject = new(Outcome.NotASubject, string.Empty);
        public static readonly Verdict Named = new(Outcome.Named, string.Empty);

        public static Verdict Unnamed(string why) => new(Outcome.Unnamed, why);

        public static Verdict Undecided(string why) => new(Outcome.Undecided, why);
    }

    /// <summary>Who wrote the <c>ToString()</c> a type resolves to.</summary>
    private enum Writer
    {
        /// <summary><c>object</c> or <c>ValueType</c>: the type's full name.</summary>
        Nobody,

        /// <summary>The compiler, for a record: its type and every property.</summary>
        Compiler,

        /// <summary>The type or a base, in code.</summary>
        Code,
    }

    /// <summary>Whether a data template matches the items of a list whose items are declared as one type.</summary>
    private enum Matching
    {
        /// <summary>It matches none of them.</summary>
        None,

        /// <summary>It may match some of them, or markup does not say.</summary>
        Some,

        /// <summary>It matches every one of them.</summary>
        Every,
    }

    /// <summary>
    /// The items an <c>ItemsSource</c> binding gives a control: the type they are declared as, the binding as it is
    /// written, and what its path is read against; or, in <see cref="Unknown"/>, why they are not read.
    /// </summary>
    private sealed record Items(Type? Type, string? Shown, string? From, string? Unknown)
    {
        public static Items Unread(string why) => new(null, null, null, why);

        /// <summary>The sentence that says where the items' type was read.</summary>
        public string Source => $"Its items are {Type!.Name}, by its ItemsSource binding {Shown} and {From}.";

        /// <summary>The clause that introduces the items' type in a reason.</summary>
        public string TypedAs => $"and its items' type, by its ItemsSource binding {Shown} and {From}, is";
    }

    /// <summary>One scan's worth of resolved types, resources and styles, shared by every control it decides.</summary>
    private sealed class Analysis
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        /// <summary>
        /// Roots whose automation peer is named by <c>AutomationProperties</c> alone: Avalonia's panels
        /// and decorators, none of which creates a peer of its own in 12.1.3.
        /// </summary>
        private static readonly HashSet<string> Layout = new(StringComparer.Ordinal)
        {
            "Border", "Canvas", "Decorator", "DockPanel", "Grid", "Panel", "RelativePanel", "StackPanel",
            "UniformGrid", "Viewbox", "VirtualizingStackPanel", "WrapPanel",
        };

        /// <summary>
        /// Elements a binding inside cannot take its data type through: templates and styles, whose content is placed
        /// where they are applied, and resources, which are placed where they are used.
        /// </summary>
        private static readonly HashSet<string> Unplaced = new(StringComparer.Ordinal)
        {
            "ControlTemplate", "ItemsPanelTemplate", "FocusAdornerTemplate", "Template", "Style", "ControlTheme", "Setter",
            "Styles", "ResourceDictionary",
        };

        private readonly XamlScanContext _context;
        private readonly ElementTypes _types;
        private readonly ResourceScope _scope;
        private readonly ThemeReader _themes;
        private readonly Dictionary<Type, (RowKind Kind, string? Why)> _kinds = [];
        private List<Type>? _supplied;
        private List<(XamlFile File, XElement Collection)>? _collections;

        public Analysis(XamlScanContext context)
        {
            _context = context;
            _types = new ElementTypes(context.Assemblies);
            _scope = new ResourceScope(context);
            _themes = new ThemeReader(_scope, _types);
        }

        /// <summary>Every <c>DataTemplates</c> collection in the scan, in file and document order.</summary>
        private List<(XamlFile File, XElement Collection)> Collections =>
            _collections ??=
            [
                .. _context.ParsedFiles.SelectMany(file => file.Document!.Descendants()
                    .Where(IsDataTemplates)
                    .Select(collection => (file, collection))),
            ];

        public Verdict Decide(XamlFile file, XElement host)
        {
            string hostName = host.Name.LocalName;
            ElementType resolved = _types.Resolve(hostName);

            switch (resolved.Kind)
            {
                case ElementKind.Missing:
                    return Verdict.Undecided(_types.WhyUnresolved(hostName) is { } why
                        ? $"{why}, so whether it generates item containers could not be read. {LoadedTypes.Remedy}"
                        : $"{hostName} is not a type in the scanned assemblies or the assemblies they reference, so "
                          + "whether it generates item containers could not be read. Pass the assembly that defines it to WithAssemblies.");
                case ElementKind.Ambiguous:
                    return Verdict.Undecided(
                        $"More than one type named {hostName} is a control in the scanned assemblies, so which one "
                        + "this is could not be told.");
                case ElementKind.Other:
                    return Verdict.NotASubject;
            }

            Type type = resolved.Type!;
            if (!ElementTypes.IsItemsControl(type))
            {
                return Verdict.NotASubject;
            }

            // An ItemsSource written after an owner outside its chain is its own only where that owner holds its
            // property, and only through a binding; else it gives it no source.
            Found source = _themes.Local(host, file, "ItemsSource", type);
            if (source.IsUnknown)
            {
                return Verdict.Undecided(source.Unknown!);
            }

            if (!source.IsSet)
            {
                return Verdict.NotASubject;
            }

            Type? container = _types.ContainerOf(type);
            if (container is null)
            {
                return Verdict.Undecided(
                    $"Which container this {hostName} generates for an item could not be read from its compiled "
                    + "code, so how its items are named is unknown.");
            }

            (RowKind kind, string? peerWhy) = KindOf(container);
            if (peerWhy is not null)
            {
                return Verdict.Undecided(peerWhy);
            }

            if (kind == RowKind.None)
            {
                return Verdict.NotASubject;
            }

            IReadOnlyCollection<string> chain = ElementTypes.NamesOf(container);
            string styleKey = _types.StyleKeyOf(container).Name;
            string rows = $"its {container.Name} containers";

            // A style whose selector is the container type alone names every row, whatever else applies, by Avalonia's
            // AutomationProperties; where that owner may or may not be Avalonia's, nothing is decided.
            IReadOnlyList<(XamlFile File, XElement Style)> reaching = _scope.StylesReaching(host);
            (XamlFile File, XElement Setter)? undecided = null;
            foreach ((XamlFile styleFile, XElement style) in reaching.Where(entry => Picks(entry.Style, styleKey, chain) == Pick.Every))
            {
                foreach (XElement setter in NamingSetters(style, styleFile))
                {
                    if (AutomationOwner(setter) == Automation.Avalonia)
                    {
                        return Verdict.Named;
                    }

                    undecided ??= (styleFile, setter);
                }
            }

            if (undecided is { } doubt)
            {
                return Verdict.Undecided(
                    $"A Style at {Where(doubt.File, doubt.Setter)} sets {ResourceScope.SetterProperty(doubt.Setter)} on every one "
                    + $"of {rows}, and its owner is in an XML namespace that only an assembly's XmlnsDefinition maps to a type, "
                    + "so whether it is Avalonia's AutomationProperties is not decided from markup.");
            }

            // A style that changes which theme or template applies is not evaluated.
            if (Styled(ElementTypes.NamesOf(type), $"the {hostName}", "ItemContainerTheme", "ItemTemplate", "DisplayMemberBinding", "Theme") is { } hostStyled)
            {
                return Verdict.Undecided(hostStyled);
            }

            if (Styled(chain, $"the {container.Name} containers", "Theme") is { } containerStyled)
            {
                return Verdict.Undecided(containerStyled);
            }

            (Definition? hostTheme, string? hostThemeUnknown) =
                _themes.ThemeOfHost(host, file, type, "ItemContainerTheme", "ItemTemplate", "DisplayMemberBinding");
            if (hostThemeUnknown is not null)
            {
                return Verdict.Undecided(hostThemeUnknown);
            }

            (Definition? containerTheme, string? containerThemeUnknown) =
                _themes.ItemContainerThemeOf(host, file, hostTheme, container, "Name", "LabeledBy");
            if (containerThemeUnknown is not null)
            {
                return Verdict.Undecided(containerThemeUnknown);
            }

            if (containerTheme is not null)
            {
                foreach (string property in (string[])["Name", "LabeledBy"])
                {
                    // Avalonia's AutomationProperties' attached properties: the last setter of theirs, which another
                    // owner's attached property of the same name, set after it, does not hide; where that last one's
                    // owner may or may not be Avalonia's, nothing is decided.
                    Found set = _themes.InTheme(containerTheme, property, setter => AutomationOwner(setter) != Automation.Other);
                    if (set.IsUnknown)
                    {
                        return Verdict.Undecided(set.Unknown!);
                    }

                    if (set.IsSet && AutomationOwner(set.Setting!.At) == Automation.Undecided)
                    {
                        return Verdict.Undecided(
                            $"A Setter at {Where(set.Setting!.File, set.Setting!.At)} sets {ResourceScope.SetterProperty(set.Setting!.At)}, "
                            + "whose owner is in an XML namespace that only an assembly's XmlnsDefinition maps to a type, so whether "
                            + "it is Avalonia's AutomationProperties is not decided from markup.");
                    }

                    if (set.IsSet && Names(set.Setting!))
                    {
                        return Verdict.Named;
                    }
                }
            }

            // A style that may name them in some state or place, or from where the view is placed.
            foreach ((XamlFile styleFile, XElement style) in reaching)
            {
                if (Picks(style, styleKey, chain) == Pick.Some && NamingSetter(style, styleFile) is { } some)
                {
                    return Verdict.Undecided(
                        $"A Style at {Where(styleFile, some)} sets {ResourceScope.SetterProperty(some)} on a selector "
                        + $"that picks {rows} only in some state or some places, and selectors are not evaluated, so "
                        + "whether it names every row is not decided from markup.");
                }
            }

            HashSet<XElement> reached = [.. reaching.Select(entry => entry.Style)];
            foreach ((XamlFile styleFile, XElement style) in _scope.Styles)
            {
                if (styleFile != file
                    && !reached.Contains(style)
                    && Picks(style, styleKey, chain) != Pick.No
                    && NamingSetter(style, styleFile) is { } elsewhere)
                {
                    return Verdict.Undecided(
                        $"A Style at {Where(styleFile, elsewhere)} sets {ResourceScope.SetterProperty(elsewhere)} on "
                        + $"{rows}, and it is in no scope this markup shows reaching this {hostName}: whether it "
                        + "applies depends on where the view is placed at runtime.");
                }
            }

            if (kind == RowKind.Tree)
            {
                return Verdict.Unnamed(
                    $"The {container.Name} containers this {hostName} generates from ItemsSource have no name. "
                    + $"{ThemeReader.Capitalised(A(container.Name))}'s automation peer reads AutomationProperties.Name and nothing else: not "
                    + "its header, its template or its item. Set AutomationProperties.Name, bound to what each item "
                    + $"shows, in its ItemContainerTheme or in a Style for {styleKey}.");
            }

            return ByContent(file, host, hostName, type, hostTheme, container);
        }

        /// <summary>A list or tab row that nothing names itself: named by what it presents, else its item's <c>ToString()</c>.</summary>
        private Verdict ByContent(XamlFile file, XElement host, string hostName, Type type, Definition? hostTheme, Type container)
        {
            string rows = $"its {container.Name} containers";

            Found display = _themes.Local(host, file, "DisplayMemberBinding");
            if (!display.IsSet && !display.IsUnknown && hostTheme is not null)
            {
                display = _themes.InTheme(hostTheme, "DisplayMemberBinding");
            }

            if (display.IsUnknown)
            {
                return Verdict.Undecided(display.Unknown!);
            }

            if (display.IsSet && Names(display.Setting!))
            {
                // Each row presents a TextBlock bound to the member.
                return Verdict.Named;
            }

            Found itemTemplate = _themes.Local(host, file, "ItemTemplate");
            if (!itemTemplate.IsSet && !itemTemplate.IsUnknown && hostTheme is not null)
            {
                itemTemplate = _themes.InTheme(hostTheme, "ItemTemplate");
            }

            if (itemTemplate.IsUnknown)
            {
                return Verdict.Undecided(itemTemplate.Unknown!);
            }

            List<(XamlFile File, XElement Root)> content = [];
            if (itemTemplate.IsSet && _themes.TemplateContent(itemTemplate, content) is { } unknownTemplate)
            {
                return Verdict.Undecided(unknownTemplate);
            }

            if (content.Count == 0)
            {
                return ByDataTemplates(file, host, hostName, type, hostTheme, container);
            }

            (XamlFile templateFile, XElement template) = content[0];
            if (!template.Name.LocalName.EndsWith("DataTemplate", StringComparison.Ordinal))
            {
                return Verdict.Undecided(
                    $"The ItemTemplate at {Where(templateFile, template)} is {A(template.Name.LocalName)}, which "
                    + "this rule cannot follow.");
            }

            (Verdict? decided, string? because) = ByRoot(templateFile, template, rows, "the item template");
            if (decided is { } verdict)
            {
                return verdict;
            }

            string falls = Falls(container, hostName, because!);
            XAttribute? declared = template.Attribute(Xaml + "DataType") ?? template.Attribute("DataType");
            if (declared is not null)
            {
                return TypeOf(declared.Value, template) is { } declaredType
                    ? ByItemType(falls, declaredType, null, "and its x:DataType is", TemplateRemedy(declaredType))
                    : Verdict.Undecided(
                        $"{falls}, and its x:DataType, {declared.Value}, is not a type in the scanned assemblies or the "
                        + "assemblies they reference.");
            }

            // An item template that declares no data type presents the items its control's ItemsSource gives it, as the
            // XAML compiler infers the template's data type from them.
            Items items = ItemsOf(host, file, type);
            return items.Type is { } itemType
                ? ByItemType(falls, itemType, items.Source, items.TypedAs, TemplateRemedy(itemType))
                : Verdict.Undecided(
                    $"{falls}, and the item template declares no x:DataType, nor is its items' type read from its "
                    + $"ItemsSource binding: {items.Unknown}. Declare its x:DataType for it to be checked.");
        }

        /// <summary>
        /// A list or tab row with no item template: presented by the first data template the search for one finds that
        /// matches its item, else by a text block of the item's <c>ToString()</c>.
        /// </summary>
        private Verdict ByDataTemplates(XamlFile file, XElement host, string hostName, Type type, Definition? hostTheme, Type container)
        {
            string rows = $"its {container.Name} containers";
            string none = $"No ItemTemplate is declared, so {rows} present what a data template chosen by each item's type "
                          + "shows, or else its ToString()";

            // A content template the containers' theme or a style gives them is what they present, and is not followed.
            if (Styled(ElementTypes.NamesOf(container), $"the {container.Name} containers", "ContentTemplate") is { } styled)
            {
                return Verdict.Undecided(styled);
            }

            (Definition? theme, string? themeUnknown) = _themes.ItemContainerThemeOf(host, file, hostTheme, container, "ContentTemplate");
            if (themeUnknown is not null)
            {
                return Verdict.Undecided(themeUnknown);
            }

            if (theme is not null)
            {
                Found given = _themes.InTheme(theme, "ContentTemplate");
                if (given.IsUnknown)
                {
                    return Verdict.Undecided(given.Unknown!);
                }

                if (given.IsSet && !(given.Setting!.Text is { } text && ThemeReader.IsNull(text.Trim())))
                {
                    return Verdict.Undecided(
                        $"{none}, and the ControlTheme at {theme.Where} gives them a ContentTemplate at {given.Setting.Where}, "
                        + "which this rule does not follow.");
                }

                // A data template the rows' own template declares is searched before any around the list.
                foreach (Definition link in _themes.ChainOf(theme, "Template").Themes)
                {
                    if (link.Element.Descendants().FirstOrDefault(IsDataTemplates) is { } held)
                    {
                        return Verdict.Undecided(
                            $"{none}, and the ControlTheme at {link.Where} declares data templates at {Where(link.File, held)}, "
                            + "which the rows' presenters search first, and this rule does not follow.");
                    }
                }
            }

            Items items = ItemsOf(host, file, type);
            if (items.Type is not { } itemType)
            {
                return Verdict.Undecided(
                    $"{none}, and its items' type is not read from its ItemsSource binding: {items.Unknown}. Declare an "
                    + "ItemTemplate with an x:DataType for it to be checked.");
            }

            if (ElementTypes.IsInputElement(itemType))
            {
                // A row presents a control item as it is, through no data template.
                return ControlsAsItems(itemType);
            }

            (Definition? chosen, string? doubt) = Chosen(host, file, itemType, hostName);
            if (doubt is not null)
            {
                return Verdict.Undecided($"{none}, and {doubt}.");
            }

            if (chosen is not null)
            {
                (Verdict? decided, string? because) = ByRoot(chosen.File, chosen.Element, rows, "the data template");
                return decided ?? ByItemType(
                    Falls(container, hostName,
                        $"no ItemTemplate is declared, so the data template at {chosen.Where} is chosen for its items by "
                        + $"their type, and {because}"),
                    itemType, items.Source, items.TypedAs, TemplateRemedy(itemType));
            }

            return ByItemType(
                Falls(container, hostName,
                    "no ItemTemplate is declared and no data template in reach is chosen for its items, so each row shows "
                    + "its item's ToString() in a TextBlock"),
                itemType, items.Source, items.TypedAs,
                $"Declare an ItemTemplate whose root is a TextBlock bound to what each item shows, set "
                + $"AutomationProperties.Name in the ItemContainerTheme, or give {itemType.Name} a ToString() of its own.");
        }

        /// <summary>
        /// What a template's root says of the rows it presents: a verdict, or, where they fall to their items'
        /// <c>ToString()</c>, why.
        /// </summary>
        /// <param name="file">The file the template is in.</param>
        /// <param name="template">The template.</param>
        /// <param name="rows">The rows, as a reason names them.</param>
        /// <param name="what">The template, as a reason names it: "the item template", "the data template".</param>
        private (Verdict? Decided, string? Because) ByRoot(XamlFile file, XElement template, string rows, string what)
        {
            XElement? root = template.Elements().FirstOrDefault(child => !child.Name.LocalName.Contains('.', StringComparison.Ordinal));
            if (root is null)
            {
                return (null, $"{what} at {Where(file, template)} is empty");
            }

            if (root.Name.LocalName is "TextBlock" or "SelectableTextBlock")
            {
                // A TextBlock's peer reads its text alone, so a name declared on it does not count.
                switch (HasText(root, file))
                {
                    case (true, _):
                        return (Verdict.Named, null);
                    case (null, { } unknown):
                        return (Verdict.Undecided(unknown), null);
                }

                return (null, $"the root of {what} at {Where(file, root)} is {A(root.Name.LocalName)} with no text");
            }

            if (AutomationName.IsDeclaredOn(root) || DeclaresLabeledBy(root))
            {
                return (Verdict.Named, null);
            }

            if (Layout.Contains(root.Name.LocalName))
            {
                return (null, $"the root of {what} at {Where(file, root)}, {A(root.Name.LocalName)}, declares no name");
            }

            return (Verdict.Undecided(
                $"The root of {what} at {Where(file, root)} is {A(root.Name.LocalName)}, "
                + $"whose own automation peer may name {rows} from what it holds, and that is not decided "
                + "from markup."), null);
        }

        private static string Falls(Type container, string hostName, string because) =>
            $"Nothing names the {container.Name} containers this {hostName} generates from ItemsSource "
            + $"but each item's ToString(), because {because}";

        private static Verdict ControlsAsItems(Type itemType) =>
            Verdict.Undecided(
                $"The items are controls, {itemType.Name}, which a container presents as they are, named by "
                + "their own automation peers, and that is not decided from markup.");

        private static string TemplateRemedy(Type itemType) =>
            "Bind AutomationProperties.Name to what each item shows, on the template's root or in "
            + $"the ItemContainerTheme, or give {itemType.Name} a ToString() of its own.";

        /// <summary>A row named by its item's <c>ToString()</c>, on the type its items are declared as.</summary>
        /// <param name="falls">Why the rows fall to their items' <c>ToString()</c>.</param>
        /// <param name="itemType">The type the items are declared as.</param>
        /// <param name="source">The sentence that says where that type was read, or <c>null</c> where it is an <c>x:DataType</c>.</param>
        /// <param name="typedAs">The clause that introduces that type where it says nothing of the items.</param>
        /// <param name="remedy">What would name the rows.</param>
        private Verdict ByItemType(string falls, Type itemType, string? source, string typedAs, string remedy)
        {
            string? unknowable = itemType == typeof(object) ? "object, which every item is"
                : itemType.IsInterface ? $"{itemType.Name}, an interface"
                : itemType.IsAbstract ? $"{itemType.Name}, which is abstract"
                : null;
            if (unknowable is not null)
            {
                return Verdict.Undecided(
                    $"{falls}, {typedAs} {unknowable}, so which ToString() the items have depends on "
                    + "their concrete types, which markup does not show.");
            }

            if (ElementTypes.IsInputElement(itemType))
            {
                return ControlsAsItems(itemType);
            }

            Writer writer = WriterOf(itemType);
            if (writer == Writer.Code)
            {
                return Verdict.Named;
            }

            string said = source is null ? $"{falls}." : $"{falls}. {source}";
            if (!itemType.IsSealed && SubclassWriting(itemType) is { } subclass)
            {
                return Verdict.Undecided(
                    $"{said} {subclass.Name} derives from {itemType.Name} and writes its own ToString(), and "
                    + "which types the items are is not in the markup.");
            }

            return Verdict.Unnamed(writer == Writer.Compiler
                ? $"{said} {itemType.Name} is a record, and the ToString() the compiler writes for it lists its "
                  + $"type and every property, so each row is named \"{itemType.Name} {{ Property = value, … }}\". {remedy}"
                : $"{said} {itemType.Name} writes no ToString(), so every row is named \"{itemType.FullName}\", "
                  + $"its type's full name. {remedy}");
        }

        /// <summary>
        /// The items <paramref name="host"/>'s <c>ItemsSource</c> binding gives it, read as the XAML compiler reads a
        /// compiled binding's path: against the data type in scope, or a cast in the path.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Only a plain path is read.</b> A <c>ReflectionBinding</c>, and a <c>{Binding}</c> that
        /// <c>x:CompileBindings="False"</c> makes one, read whatever their data context is at run time rather than the
        /// type an <c>x:DataType</c> declares, and a binding that sets anything but its path and its mode, a converter,
        /// a source or a fallback among them, gives a value its path does not show; each is named rather than read.
        /// </remarks>
        private Items ItemsOf(XElement host, XamlFile file, Type? type)
        {
            Found source = _themes.Local(host, file, "ItemsSource", type);
            if (source.IsUnknown)
            {
                return Items.Unread($"the ItemsSource of the {host.Name.LocalName} at {Where(file, host)} is not decided");
            }

            if (source.Setting is not { } setting)
            {
                return Items.Unread($"the {host.Name.LocalName} at {Where(file, host)} sets no ItemsSource");
            }

            string shown = setting.Shown.Trim();
            string? kind;
            List<(string Name, string Value)> named;
            List<string> positional = [];
            if (setting.Element is { } element)
            {
                kind = BindingKind(element.Name.LocalName);
                named =
                [
                    .. element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration)
                        .Select(attribute => (attribute.Name.LocalName, attribute.Value)),
                    .. element.Elements().Select(child => (ResourceScope.PropertyName(child.Name.LocalName), child.Value)),
                ];
            }
            else if (MarkupExtension.Parse(setting.Text!) is { } extension)
            {
                kind = BindingKind(extension.Name);
                named = [.. extension.Named()];
                for (int index = 0; index < extension.Arguments.Count; index++)
                {
                    if (extension.Positional(index) is { } argument)
                    {
                        positional.Add(argument);
                    }
                }
            }
            else
            {
                return Items.Unread($"its ItemsSource is {shown}, not a binding");
            }

            if (kind is null)
            {
                return Items.Unread($"its ItemsSource is {shown}, which is not a binding this rule reads");
            }

            if (kind == "ReflectionBinding")
            {
                return Items.Unread(
                    $"its ItemsSource is {shown}, a ReflectionBinding, which reads whatever its data context is at run time, "
                    + "not the type an x:DataType declares");
            }

            if (kind == "Binding" && CompileBindingsOff(host) is { } off)
            {
                return Items.Unread(
                    $"x:CompileBindings is False at {Where(file, off)}, so its ItemsSource binding {shown} reads whatever its "
                    + "data context is at run time, not the type an x:DataType declares");
            }

            string? path = null;
            foreach ((string name, string value) in named)
            {
                if (name == "Path")
                {
                    path = value;
                }
                else if (name != "Mode")
                {
                    return Items.Unread($"its ItemsSource binding {shown} sets {name}, which this rule does not evaluate");
                }
            }

            if (positional.Count > 1 || (positional.Count == 1 && path is not null))
            {
                return Items.Unread($"its ItemsSource binding {shown} gives its path more than once");
            }

            string written = Unquoted(positional.Count == 1 ? positional[0] : path ?? string.Empty);
            (Type? cast, string[] steps, string? pathUnknown) = PathOf(written, host);
            if (pathUnknown is not null)
            {
                return Items.Unread(pathUnknown);
            }

            Type current;
            string from;
            if (cast is not null)
            {
                (current, from) = (cast, $"the cast to {cast.Name} in its path");
            }
            else
            {
                (Type? scoped, string? where, string? scopeUnknown) = DataTypeAt(host, file);
                if (scoped is null)
                {
                    return Items.Unread(scopeUnknown!);
                }

                (current, from) = (scoped, where!);
            }

            foreach (string step in steps)
            {
                if (PropertyOf(current, step) is not { } property)
                {
                    return Items.Unread(
                        $"its ItemsSource binding {shown} reads {step} on {current.Name}, by {from}, which has no public "
                        + "property of that name");
                }

                current = property.PropertyType;
            }

            Type[] enumerables =
            [
                .. (current.IsInterface ? [current, .. current.GetInterfaces()] : current.GetInterfaces())
                    .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Distinct(),
            ];

            return enumerables switch
            {
                [var only] => new Items(only.GetGenericArguments()[0], shown, from, null),
                [] => Items.Unread(
                    $"its ItemsSource binding {shown} reads {current.Name}, by {from}, which is no IEnumerable<T>, so the "
                    + "type of its items is not declared"),
                _ => Items.Unread(
                    $"its ItemsSource binding {shown} reads {current.Name}, by {from}, which enumerates "
                    + $"{enumerables.Length} types of item"),
            };
        }

        /// <summary>
        /// The data type a compiled binding on <paramref name="start"/> reads against, as the XAML compiler infers it at
        /// 12.1.3: the nearest <c>x:DataType</c>, a data template's <c>DataType</c>, or, for an item template that
        /// declares neither, the items of the control it templates; and what gave it.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A <c>DataContext</c> set on the way changes what the binding reads</b>, to what that value is, which the
        /// compiler infers from a compiled binding and the rule does not follow; and a template, a style or a resource
        /// is placed where it is applied or used, which markup around it does not show. Each stops the reading.
        /// </remarks>
        private (Type? Type, string? From, string? Unknown) DataTypeAt(XElement start, XamlFile file)
        {
            for (XElement? element = start; element is not null; element = element.Parent)
            {
                if (element.Attribute(Xaml + "DataType") is { } directive)
                {
                    return Typed(directive.Value, element, $"the x:DataType at {Where(file, element)}");
                }

                bool dataTemplate = element.Name.LocalName is "DataTemplate" or "TreeDataTemplate";
                if (dataTemplate && element.Attribute("DataType") is { } dataType)
                {
                    return Typed(dataType.Value, element, $"the DataType of the data template at {Where(file, element)}");
                }

                if (dataTemplate)
                {
                    if (element.Parent is { } slot
                        && slot.Name.LocalName.EndsWith(".ItemTemplate", StringComparison.Ordinal)
                        && slot.Parent is { } owner
                        && _types.Resolve(owner.Name.LocalName).Type is { } ownerType
                        && ElementTypes.IsItemsControl(ownerType))
                    {
                        Items outer = ItemsOf(owner, file, ownerType);
                        return outer.Type is { } presented
                            ? (presented, $"the items of the {owner.Name.LocalName} at {Where(file, owner)}", null)
                            : (null, null,
                                $"the data template at {Where(file, element)} declares no data type, and the items of the "
                                + $"{owner.Name.LocalName} at {Where(file, owner)} it templates are not read either, since {outer.Unknown}");
                    }

                    return (null, null, $"the data template at {Where(file, element)} declares no data type");
                }

                if (ThemeReader.MaySet(element, "DataContext"))
                {
                    return (null, null, $"the {element.Name.LocalName} at {Where(file, element)} sets its DataContext, which is not followed");
                }

                string name = element.Name.LocalName;
                if (Unplaced.Contains(name)
                    || name.EndsWith(".Resources", StringComparison.Ordinal)
                    || name.EndsWith(".Styles", StringComparison.Ordinal))
                {
                    return (null, null, $"it is inside {A(name)} at {Where(file, element)}, whose data context markup does not show");
                }
            }

            return (null, null, "no x:DataType is in scope");

            (Type? Type, string? From, string? Unknown) Typed(string written, XElement element, string from) =>
                TypeOf(written, element) is { } type
                    ? (type, from, null)
                    : (null, null, $"{from}, {written}, is not a type in the scanned assemblies or the assemblies they reference");
        }

        /// <summary>
        /// The search a row's presenter makes for a data template, as markup shows it: the first that matches every item
        /// presents the rows, and one that may match only some, or whose matching is not decided, is why not.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The search walks the logical tree, which markup shows only part of.</b> From the list it climbs the
        /// list's ancestors in its file, then searches the application's <c>DataTemplates</c>. Between the two, the view
        /// is wherever it is placed at runtime, so a collection in another view that may match is named; and a template
        /// in a <c>DataTemplates</c> collection may be presented by any control its owner holds, so the collections that
        /// owner holds may be searched first. Where the climb leaves the tree, out of a template held anywhere but as an
        /// item, content or header template or in a <c>DataTemplates</c> collection, or out of resources or styles, any
        /// other collection in the file may be searched first too.
        /// </remarks>
        private (Definition? Template, string? Unknown) Chosen(XElement host, XamlFile file, Type itemType, string hostName)
        {
            HashSet<XElement> searched = [];
            List<XElement> owners = [];
            bool lost = false;
            XElement? previous = null;
            for (XElement? scope = host; scope is not null; previous = scope, scope = scope.Parent)
            {
                if (previous is not null)
                {
                    if (LeavesTheTree(previous, scope))
                    {
                        lost = true;
                        break;
                    }

                    if (IsDataTemplates(scope) && scope.Parent is { } owner)
                    {
                        owners.Add(owner);
                    }
                }

                foreach (XElement collection in scope.Elements().Where(IsDataTemplates))
                {
                    searched.Add(collection);
                    foreach (XElement entry in collection.Elements())
                    {
                        (Matching matching, string? why) = Matches(entry, itemType, file);
                        switch (matching)
                        {
                            case Matching.Every:
                                return (new Definition(file, entry), null);
                            case Matching.Some:
                                return (null, why);
                        }
                    }
                }
            }

            foreach ((XamlFile otherFile, XElement collection) in Collections)
            {
                bool global = _scope.Applications.Contains(otherFile) && collection.Parent == otherFile.Document!.Root;
                if (searched.Contains(collection)
                    || global
                    || (otherFile == file && !lost && !owners.Any(owner => collection.Ancestors().Contains(owner))))
                {
                    continue;
                }

                foreach (XElement entry in collection.Elements())
                {
                    (Matching matching, string? why) = Matches(entry, itemType, otherFile);
                    if (matching != Matching.None)
                    {
                        return (null,
                            (matching == Matching.Every ? $"the data template at {Where(otherFile, entry)} matches them" : why)
                            + $", and it is in no scope this markup shows reaching this {hostName}: whether the search for a "
                            + "data template reaches it depends on where the view is placed at runtime");
                    }
                }
            }

            foreach (XamlFile application in _scope.Applications)
            {
                foreach (XElement collection in application.Document!.Root!.Elements().Where(IsDataTemplates))
                {
                    if (!searched.Add(collection))
                    {
                        continue;
                    }

                    foreach (XElement entry in collection.Elements())
                    {
                        (Matching matching, string? why) = Matches(entry, itemType, application);
                        switch (matching)
                        {
                            case Matching.Every:
                                return (new Definition(application, entry), null);
                            case Matching.Some:
                                return (null, why);
                        }
                    }
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Whether a data template matches every item declared as <paramref name="itemType"/>, may match some, or none,
        /// as <c>DataTemplate.Match</c> decides at 12.1.3: an item that is an instance of its <c>DataType</c>, which an
        /// <c>x:DataType</c> gives it where none is written. One with neither is refused by the collection that holds it,
        /// so its markup does not load, which is named.
        /// </summary>
        private (Matching Matching, string? Why) Matches(XElement entry, Type itemType, XamlFile file)
        {
            string name = entry.Name.LocalName;
            if (name is not ("DataTemplate" or "TreeDataTemplate"))
            {
                return (Matching.Some, $"the {name} at {Where(file, entry)} is a data template whose own code decides which items it presents");
            }

            if (entry.Elements().Any(child => child.Name.LocalName.EndsWith(".DataType", StringComparison.Ordinal)))
            {
                return (Matching.Some, $"the data template at {Where(file, entry)} gives its DataType as a property element, which is not read");
            }

            XAttribute? written = entry.Attribute("DataType") ?? entry.Attribute(Xaml + "DataType");
            if (written is null)
            {
                return (Matching.Some,
                    $"the data template at {Where(file, entry)} has no DataType, which a DataTemplates collection refuses at "
                    + "12.1.3, so this markup throws as it loads");
            }

            if (TypeOf(written.Value, entry) is not { } dataType)
            {
                // Items of a sealed type are instances only of it, its bases and its interfaces, all of which resolve.
                string local = written.Value.Trim().Trim('{', '}');
                local = local[(local.LastIndexOf(':') + 1)..].Trim();
                return itemType.IsSealed && !Above(itemType).Any(link => link.Name == local)
                    ? (Matching.None, null)
                    : (Matching.Some,
                        $"the data template at {Where(file, entry)} is for {written.Value}, which is not a type in the scanned "
                        + "assemblies or the assemblies they reference, and some of the items may be one");
            }

            if (ElementTypes.IsAssignable(dataType, itemType))
            {
                return (Matching.Every, null);
            }

            return MayBe(dataType, itemType)
                ? (Matching.Some, $"the data template at {Where(file, entry)} is for {dataType.Name}, which some of the items may be")
                : (Matching.None, null);
        }

        /// <summary>
        /// Whether an item declared as <paramref name="itemType"/> may be a <paramref name="dataType"/> that not every
        /// one is: an instance of a type derived from both.
        /// </summary>
        private static bool MayBe(Type dataType, Type itemType) =>
            ElementTypes.IsAssignable(itemType, dataType)
            || (dataType.IsInterface && !itemType.IsSealed)
            || (itemType.IsInterface && !dataType.IsSealed);

        /// <summary>A type's bases and the interfaces it implements.</summary>
        private static IEnumerable<Type> Above(Type type)
        {
            for (Type? link = type.BaseType; link is not null; link = link.BaseType)
            {
                yield return link;
            }

            foreach (Type implemented in type.GetInterfaces())
            {
                yield return implemented;
            }
        }

        /// <summary>
        /// Whether stepping from <paramref name="child"/> to its markup parent leaves the logical tree a row's presenter
        /// searches: out of a template held anywhere but as an item, content or header template or in a
        /// <c>DataTemplates</c> collection, or out of resources or styles.
        /// </summary>
        private static bool LeavesTheTree(XElement child, XElement parent)
        {
            string name = parent.Name.LocalName;
            string childName = child.Name.LocalName;
            if (!childName.Contains('.', StringComparison.Ordinal) && childName.EndsWith("Template", StringComparison.Ordinal))
            {
                return !(name.EndsWith(".ItemTemplate", StringComparison.Ordinal)
                         || name.EndsWith(".ContentTemplate", StringComparison.Ordinal)
                         || name.EndsWith(".HeaderTemplate", StringComparison.Ordinal)
                         || IsDataTemplates(parent));
            }

            return Unplaced.Contains(name)
                   || name.EndsWith(".Resources", StringComparison.Ordinal)
                   || name.EndsWith(".Styles", StringComparison.Ordinal)
                   || name.EndsWith(".Value", StringComparison.Ordinal)
                   || name.EndsWith(".MergedDictionaries", StringComparison.Ordinal)
                   || name.EndsWith(".ThemeDictionaries", StringComparison.Ordinal);
        }

        private static bool IsDataTemplates(XElement element) =>
            element.Name.LocalName.EndsWith(".DataTemplates", StringComparison.Ordinal);

        /// <summary>
        /// The binding an extension or an element is, by its name without a prefix or an <c>Extension</c> suffix:
        /// <c>Binding</c>, <c>CompiledBinding</c> or <c>ReflectionBinding</c>; <c>null</c> for any other.
        /// </summary>
        private static string? BindingKind(string written)
        {
            string name = written[(written.LastIndexOf(':') + 1)..];
            name = name.EndsWith("Extension", StringComparison.Ordinal) ? name[..^"Extension".Length] : name;
            return name is "Binding" or "CompiledBinding" or "ReflectionBinding" ? name : null;
        }

        /// <summary>The nearest element on the way up from <paramref name="element"/> that sets <c>x:CompileBindings</c> to <c>False</c>, where none nearer sets it to <c>True</c>.</summary>
        private static XElement? CompileBindingsOff(XElement element) =>
            element.AncestorsAndSelf().FirstOrDefault(scope => scope.Attribute(Xaml + "CompileBindings") is not null) is { } nearest
            && string.Equals(nearest.Attribute(Xaml + "CompileBindings")!.Value.Trim(), "False", StringComparison.OrdinalIgnoreCase)
                ? nearest
                : null;

        private static string Unquoted(string text)
        {
            string trimmed = text.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '\'' && trimmed[^1] == '\'' ? trimmed[1..^1].Trim() : trimmed;
        }

        /// <summary>
        /// A compiled path's steps: dotted property names, after a cast group <c>((vm:T)Property)</c> where the path has
        /// one, whose type the steps after it start from; or why the path is not one the rule reads.
        /// </summary>
        private (Type? Cast, string[] Steps, string? Unknown) PathOf(string path, XElement context)
        {
            string unread = $"its ItemsSource binding's path, {path}, is not one this rule reads";
            string rest = path;
            Type? cast = null;
            int group = path.LastIndexOf("((", StringComparison.Ordinal);
            if (group >= 0)
            {
                int close = path.IndexOf(')', group + 2);
                int end = close < 0 ? -1 : path.IndexOf(')', close + 1);
                if ((group > 0 && path[group - 1] != '.') || close < 0 || end < 0 || !IsIdentifier(path[(close + 1)..end]))
                {
                    return (null, [], unread);
                }

                string typeName = path[(group + 2)..close].Trim();
                if (TypeOf(typeName, context) is not { } type)
                {
                    return (null, [],
                        $"the cast in its ItemsSource binding's path, to {typeName}, is not a type in the scanned assemblies or "
                        + "the assemblies they reference");
                }

                cast = type;
                rest = path[(end + 1)..];
                if (rest.Length > 0)
                {
                    if (rest[0] != '.')
                    {
                        return (null, [], unread);
                    }

                    rest = rest[1..];
                }
            }

            if (rest.Length == 0 || (cast is null && rest == "."))
            {
                return (cast, [], null);
            }

            string[] steps = rest.Split('.');
            return steps.All(IsIdentifier) ? (cast, steps, null) : (null, [], unread);
        }

        private static bool IsIdentifier(string text) =>
            text.Length > 0
            && (char.IsLetter(text[0]) || text[0] == '_')
            && text.All(character => char.IsLetterOrDigit(character) || character == '_');

        /// <summary>
        /// The public instance property a compiled path's step names on <paramref name="type"/>: the most derived one of
        /// that name, an interface's inherited ones included, but an indexer; <c>null</c> where there is none.
        /// </summary>
        private static PropertyInfo? PropertyOf(Type type, string name)
        {
            IEnumerable<Type> searched = type.IsInterface ? [type, .. type.GetInterfaces()] : [type];
            foreach (Type link in searched)
            {
                PropertyInfo[] candidates =
                [
                    .. link.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(property => property.Name == name && property.GetIndexParameters().Length == 0),
                ];
                if (candidates.Length > 0)
                {
                    // A property a subclass hides with new is listed beside its base's.
                    return candidates.FirstOrDefault(candidate => candidates.All(other =>
                               other.DeclaringType!.IsAssignableFrom(candidate.DeclaringType)))
                           ?? candidates[0];
                }
            }

            return null;
        }

        /// <summary>
        /// Whether <paramref name="container"/>'s automation peer is one whose naming was measured, read
        /// from the <c>newobj</c> in the <c>OnCreateAutomationPeer</c> it resolves to.
        /// </summary>
        private (RowKind Kind, string? Why) KindOf(Type container)
        {
            if (!_kinds.TryGetValue(container, out (RowKind Kind, string? Why) known))
            {
                RowPeer peer = RowPeers.Of(container);
                known = (peer.Kind, peer.NamesItself
                    ? $"Its {container.Name} containers are named by their automation peer, {peer.Peer!.Name}, which "
                      + "writes its own GetNameCore, so how they are named is not decided from markup."
                    : null);
                _kinds[container] = known;
            }

            return known;
        }

        /// <summary>
        /// The type an <c>x:DataType</c>, a <c>DataType</c> or a cast names, through the <c>xmlns</c> its prefix is
        /// declared with where <paramref name="context"/> is: <c>using:</c>, <c>clr-namespace:</c>, or the XAML
        /// language's own types.
        /// </summary>
        private Type? TypeOf(string written, XElement context)
        {
            string name = written.Trim();
            if (name.StartsWith('{') && name.EndsWith('}'))
            {
                // {x:Type m:Item} and {x:Type TypeName=m:Item}.
                string[] parts = name[1..^1].Trim().Split((char[])[' ', '\t', '\r', '\n'], 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || !parts[0].EndsWith("Type", StringComparison.Ordinal))
                {
                    return null;
                }

                name = parts[1].Trim();
                if (name.StartsWith("TypeName=", StringComparison.Ordinal))
                {
                    name = name["TypeName=".Length..].Trim();
                }
            }

            int colon = name.IndexOf(':', StringComparison.Ordinal);
            string prefix = colon >= 0 ? name[..colon] : string.Empty;
            string local = colon >= 0 ? name[(colon + 1)..] : name;
            if (local.Length == 0 || !local.All(character => char.IsLetterOrDigit(character) || character == '_'))
            {
                return null;
            }

            XNamespace? xmlns = prefix.Length == 0 ? context.GetDefaultNamespace() : context.GetNamespaceOfPrefix(prefix);
            if (xmlns is null)
            {
                return null;
            }

            if (xmlns == Xaml)
            {
                // x:String, x:Int32 and the rest of the language's own types.
                return _types.TypeNamed("System", local);
            }

            string uri = xmlns.NamespaceName;
            string? clr = uri.StartsWith("using:", StringComparison.Ordinal) ? uri["using:".Length..]
                : uri.StartsWith("clr-namespace:", StringComparison.Ordinal) ? uri["clr-namespace:".Length..].Split(';')[0]
                : null;

            return clr is null ? null : _types.TypeNamed(clr.Trim(), local);
        }

        /// <summary>Who wrote the public <c>ToString()</c> <paramref name="type"/> resolves to.</summary>
        private static Writer WriterOf(Type type)
        {
            MethodInfo? method = type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
            if (method is null || method.DeclaringType == typeof(object) || method.DeclaringType == typeof(ValueType))
            {
                return Writer.Nobody;
            }

            return method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ? Writer.Compiler : Writer.Code;
        }

        /// <summary>A type in the scanned assemblies that derives from <paramref name="type"/> and writes a <c>ToString()</c>.</summary>
        private Type? SubclassWriting(Type type)
        {
            _supplied ??= [.. _context.Assemblies.SelectMany(assembly => LoadedTypes.Of(assembly).Loaded)];

            foreach (Type candidate in _supplied)
            {
                try
                {
                    if (candidate != type && ElementTypes.IsAssignable(type, candidate) && WriterOf(candidate) == Writer.Code)
                    {
                        return candidate;
                    }
                }
                catch (Exception ex) when (ElementTypes.IsUnreadable(ex))
                {
                    // A type that cannot be read cannot be one the items are shown to be.
                }
            }

            return null;
        }

        /// <summary>A setter directly in <paramref name="style"/> that names what it applies to, or may, or <c>null</c>.</summary>
        private static XElement? NamingSetter(XElement style, XamlFile file) => NamingSetters(style, file).FirstOrDefault();

        /// <summary>The setters directly in <paramref name="style"/> that name what it applies to, or may.</summary>
        private static IEnumerable<XElement> NamingSetters(XElement style, XamlFile file) =>
            style.Elements().Where(setter =>
                setter.Name.LocalName == "Setter"
                && AutomationOwner(setter) != Automation.Other
                && Names(ThemeReader.ValueOf(setter, file)));

        /// <summary>
        /// Whether a setter sets Avalonia's <c>AutomationProperties.Name</c> or <c>AutomationProperties.LabeledBy</c>,
        /// in any spelling: bare, in parentheses, or with a namespace prefix, in its <c>Property</c> attribute or its
        /// <c>Setter.Property</c> element, as <see cref="ResourceScope.SetterProperty"/> reads it.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>An owner is the type its prefix names</b>, as <see cref="Owner"/> reads it: <c>AutomationProperties</c> in
        /// a framework's namespace, or through <c>using:Avalonia.Automation</c>, is Avalonia's; one in another
        /// <c>using:</c> or <c>clr-namespace:</c> namespace is another type's; and one in any other URI is not decided.
        /// Measured: rows whose theme set <c>AutomationProperties.Name</c> and then a consumer's
        /// <c>AutomationProperties.Name</c> of its own, through a <c>clr-namespace:</c> prefix, were named by their labels.
        /// </remarks>
        private static Automation AutomationOwner(XElement setter)
        {
            if (ResourceScope.SetterProperty(setter) is not { } written
                || ResourceScope.PropertyName(written) is not ("Name" or "LabeledBy")
                || ResourceScope.OwnerOf(written, setter) is not { Local: "AutomationProperties" } owner)
            {
                return Automation.Other;
            }

            if (owner.Clr is { } clr)
            {
                return clr == "Avalonia.Automation" ? Automation.Avalonia : Automation.Other;
            }

            return owner.IsFramework ? Automation.Avalonia : Automation.Undecided;
        }

        /// <summary>Whose <c>AutomationProperties</c> a setter sets.</summary>
        private enum Automation
        {
            /// <summary>Avalonia's.</summary>
            Avalonia,

            /// <summary>Another type's, or the setter sets something else.</summary>
            Other,

            /// <summary>One in a URI that only an assembly's <c>XmlnsDefinition</c> maps, so not decided.</summary>
            Undecided,
        }

        /// <summary>Whether a value names something: an element such as a binding, or text that is not blank or null.</summary>
        private static bool Names(Setting setting) =>
            setting.Element is not null
            || (setting.Text is { } text && !string.IsNullOrWhiteSpace(text) && !ThemeReader.IsNull(text.Trim()));

        /// <summary>
        /// Whether a <c>TextBlock</c> has text: a <c>Text</c> that is not blank, in the one spelling the XAML compiler
        /// takes it in, as <see cref="ThemeReader.Local"/> reads it, or inline content; <c>null</c>, with why, where a
        /// spelling may give it text and that is not decided, or where it gives both.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⚠ A <c>Text</c> written after <c>SelectableTextBlock</c>, which derives from <c>TextBlock</c>, is a
        /// <c>TextBlock</c>'s own where it is a binding: measured, rows whose item template's root was
        /// <c>&lt;TextBlock SelectableTextBlock.Text="{Binding Label}"/&gt;</c> were named by their labels.
        /// </para>
        /// <para>
        /// ⚠ A <c>Text</c> beside inline content decides nothing, as two spellings of <c>Text</c> do not: the framework
        /// applies each in turn, so which the text block is left with is not decided from markup.
        /// </para>
        /// </remarks>
        private (bool? Has, string? Unknown) HasText(XElement textBlock, XamlFile file)
        {
            Found text = _themes.Local(textBlock, file, "Text");
            if (text.IsUnknown)
            {
                return (null, text.Unknown);
            }

            bool inline = textBlock.Nodes().Any(node => node switch
            {
                XText run => !string.IsNullOrWhiteSpace(run.Value),
                XElement child when !child.Name.LocalName.Contains('.', StringComparison.Ordinal) => true,
                XElement property when property.Name.LocalName.EndsWith(".Inlines", StringComparison.Ordinal) =>
                    property.HasElements || !string.IsNullOrWhiteSpace(property.Value),
                _ => false,
            });

            if (text.Setting is not { } set)
            {
                return (inline, null);
            }

            if (inline)
            {
                return (null, $"The TextBlock at {Where(file, textBlock)} gives its Text {set.Shown} beside inline content, which the "
                              + "framework applies in turn, so the text it is left with is not decided from markup.");
            }

            // An attribute decides; a property element with nothing in it gives no text.
            return set.At == textBlock
                ? (!string.IsNullOrWhiteSpace(set.Text) && !ThemeReader.IsNull(set.Text.Trim()), null)
                : (set.Element is not null || !string.IsNullOrWhiteSpace(set.Text), null);
        }

        /// <summary>Whether an element declares <c>AutomationProperties.LabeledBy</c>, in either spelling, with something in it.</summary>
        private static bool DeclaresLabeledBy(XElement element) =>
            element.Attributes().Any(attribute =>
                attribute.Name.LocalName.EndsWith("AutomationProperties.LabeledBy", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(attribute.Value))
            || element.Elements().Any(child =>
                child.Name.LocalName.EndsWith("AutomationProperties.LabeledBy", StringComparison.Ordinal)
                && (child.HasElements || !string.IsNullOrWhiteSpace(child.Value)));

        /// <summary>
        /// The reason to stop when a top-level style sets one of <paramref name="properties"/> on one of
        /// <paramref name="names"/>, or <c>null</c>.
        /// </summary>
        private string? Styled(IReadOnlyCollection<string> names, string subject, params string[] properties) =>
            _scope.StylesSetting(names, properties) is [{ } style, ..]
                ? $"A Style at {style.Where} sets {style.Property} on a selector naming {subject}, and styles are not "
                  + "evaluated, so how its item containers are named is not decided from markup."
                : null;

        private static string Where(XamlFile file, XElement element) =>
            ThemeReader.LineOf(element) is { } line ? $"{file.RelativePath}:{line}" : file.RelativePath;

        /// <summary>A type name with its indefinite article: "a ListBoxItem", "an Image".</summary>
        private static string A(string name) =>
            (name.Length > 0 && "AEIOU".Contains(char.ToUpperInvariant(name[0]), StringComparison.Ordinal) ? "an " : "a ") + name;
    }
}
