using System.Globalization;
using System.Xml.Linq;
using static Bennewitz.Ninja.XamlQuality.StyleSelectors;

namespace Bennewitz.Ninja.XamlQuality.Rules;

/// <summary>
/// Every list, combo or tab row generated from <c>ItemsSource</c> draws a background of its own: its
/// template draws one, and where it draws the row's <c>Background</c>, that is never null.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A control is hit-tested where it draws, and a null background draws nothing.</b> Measured on
/// Avalonia 12.1.3 under Fluent, Simple and Semi 12.1.0.1, by a click where only a row's background can
/// be hit: a <c>ListBoxItem</c>, <c>ComboBoxItem</c> or <c>TabItem</c> whose <c>Background</c> was null
/// took no click there, and the click went to what lay behind it; a <c>ListBoxItem</c>'s
/// <c>IsPointerOver</c> stayed false there. Stock rows are <c>Transparent</c> in all three themes, which
/// draws. So the rule reports two things markup decides about the rows' own template: rows whose
/// <c>Background</c> can be null where their template draws it, and rows whose template draws no
/// background at all. It does not say where a click lands: what the rows present may draw across them,
/// and decides neither.
/// </para>
/// <para>
/// ⭐ <b>The row's <c>Background</c>, in the state rows start in.</b> A style reaching the list whose
/// selector holds of every row sets it, and wins over any theme; else the row's theme, its
/// <c>ItemContainerTheme</c> or implicit one, down its <c>BasedOn</c> chain, where a chain that ends at a
/// theme <c>BasedOn</c> nothing leaves the property's default, which is null unless the row's type
/// registers another in its static constructor. A value the row's own code sets outranks them all, and
/// markup does not show it, so such rows are not decided, and neither are the rows of a list whose own
/// code, outside the framework, sets a background or a fill: measured, rows a list's
/// <c>PrepareContainerForItemOverride</c> made <c>Transparent</c> took the click. A value can be null when
/// it is <c>{x:Null}</c>, or a binding with no non-null <c>TargetNullValue</c>: a binding answers null
/// wherever its source or its converter does, and <c>FallbackValue</c> did not help, since it covers a
/// failed binding, not a null value.
/// </para>
/// <para>
/// ⭐ <b>What draws the row's <c>Background</c>.</b> An element of the rows' template that draws its own
/// <c>Background</c>, a <c>Border</c>, a <c>Panel</c>, a <c>ContentPresenter</c>, a
/// <c>TextPresenter</c> or a <c>TextBlock</c>, by their source at 12.1.3, or a shape its <c>Fill</c>, and
/// takes the row's by a <c>TemplateBinding</c>, or by a binding to the templated parent or to the ancestor
/// of the row's type that the row is: measured, templates bound through <c>$parent[ListBoxItem]</c> or
/// <c>AncestorType</c> took the click, and ones bound through <c>$parent[ListBoxItem;1]</c> or
/// <c>AncestorLevel=2</c> drew an outer row's background, or none. Such a binding counts the ancestors of
/// that type from the nearest, any type derived from it included, by <c>ControlLocator</c> and
/// <c>VisualLocator</c> at 12.1.3, so one that an element of the template between them may answer first,
/// or whose count markup cannot make, is not the row's. A <c>RelativeSource</c> looks for an ancestor only
/// in the mode that finds one, which it is in when it names none: <c>Self</c> and <c>DataContext</c> bind the
/// element itself and its data context, whatever <c>AncestorType</c> they also name, by
/// <c>ExpressionNodeFactory</c> and <c>AvaloniaXamlIlBindingPathParser</c> at 12.1.3; measured, a
/// <c>Border</c> bound by <c>Mode=Self</c> with an <c>AncestorType</c> drew nothing. What a
/// <c>ContentPresenter</c> in the template holds as markup content is not where the template writes it: it
/// has no templated parent, and in one named <c>PART_ContentPresenter</c> its logical parent is the row,
/// past the presenter; measured, a <c>TemplateBinding</c> there took nothing, and <c>$parent[Control;1]</c>
/// reached the list. What a <c>TransitioningContentControl</c> holds has no templated parent either: unlike
/// every other <c>ContentControl</c>, it does not take its content as its logical child when it is set, but
/// hands it to a presenter of its own, by its source at 12.1.3. So what either holds is not followed. A
/// binding with a <c>TargetNullValue</c> that is
/// not null paints whatever the row's is, and one that passes it through a converter, or anything but
/// its path, its source, its mode, its fallback and what only writes back, is not decided: measured,
/// rows whose <c>Background</c> was null took the click through either. A templated control in the
/// template draws the row's <c>Background</c> only where it takes it and its own template draws its own,
/// read through its own theme as a row's is. A theme that sets no <c>Template</c> anywhere down its
/// chain leaves the row the framework theme's template, measured to draw it, or with a chain
/// <c>BasedOn</c> nothing, the default template that <c>ContentControl</c>'s static constructor
/// registers, a <c>ContentPresenter</c> that draws it, by its source at 12.1.3; measured, such a row took
/// the click when its background was <c>Transparent</c>. Only a <c>Template</c> of <c>{x:Null}</c>
/// presents nothing. Where something draws the row's <c>Background</c>, that value alone decides,
/// whatever else in the template paints or may.
/// </para>
/// <para>
/// ⭐ <b>What paints of its own.</b> Where nothing draws the row's <c>Background</c>, an element of the
/// template that paints a brush of its own draws a background, however little of the row it covers: a
/// literal brush, a binding with a <c>TargetNullValue</c> that is not null, the default its type registers
/// in its static constructor, or for a templated control, what its own theme draws, its template and its
/// <c>Background</c>. A type with markup of its own, found by its <c>x:Class</c>, a view, a
/// <c>UserControl</c> subclass, or any other, is read from it, and so is a subclass with none of its own,
/// onto which its base's constructor loads the base's: the <c>Background</c> its root gives it, and what that
/// markup holds; measured, a <c>Border</c> subclass and a <c>Panel</c> subclass whose own markup was
/// <c>Transparent</c> drew it, and so did a subclass of each with no markup of its own. In a template, that <c>Background</c> outranks the one the template gives
/// the element, which the XAML compiler sets at <c>BindingPriority.Template</c> at 12.1.3, but not a
/// binding, which it sets as a local value even there, after the type's own: measured, a view whose root
/// was <c>Transparent</c> drew that, given the row's null background or <c>{x:Null}</c> by the template. A
/// <c>ContentControl</c> or a <c>UserControl</c> with no theme in the scan takes the default template,
/// which draws only its own <c>Background</c>: neither has an implicit theme in Fluent or Simple at
/// 12.1.3, by their source. What may paint and is not decided leaves the rows in
/// <see cref="XamlRuleResult.Skipped"/> where nothing else draws: an element its theme, the one it names
/// or its implicit one, or a style that may pick it, may paint, give a theme or a template; one given a
/// <c>Template</c> of its own, which is not followed; any other templated control with no theme in the
/// scan, which its framework's theme may paint; one whose own code sets its <c>Background</c> or its
/// <c>Fill</c>, read in every method of its type and of each type it derives from, in any assembly, with
/// the closures and state machines the compiler nests in them, compiled markup aside; one outside the
/// framework that draws itself in a <c>Render</c> of its own; and one whose type neither the scanned
/// assemblies nor those they reference define once. Measured, an element that a class style,
/// its implicit theme, a template of its own, its own code or a <c>Render</c> of its own painted took the
/// click.
/// </para>
/// <para>
/// ⚠ <b>What a template's markup gives an element ranks below a style with a condition.</b> By
/// <c>BindingPriority</c> at 12.1.3, a style trigger ranks above a template's value and a style with no
/// condition below it, and what the element's own code sets above both. So a value in the template that a
/// style with a condition, a class, a pseudo-class, a property or <c>:not()</c>, may replace on that
/// element in the state it starts in, or a nested style with one in its theme, or its code, is not decided;
/// one a plain style or a theme's own setter would replace stands. A style that reaches into the rows'
/// template and sets a part's background, fill, theme or template in no state, and a theme's nested style
/// that does, may change what draws there, and leaves rows undecided where that could change the answer:
/// a part given a binding may draw as surely as one given a brush, measured, where it took the row's
/// <c>Background</c> through <c>$parent[ListBoxItem]</c>, the templated parent or a <c>TemplateBinding</c>.
/// </para>
/// <para>
/// ⚠ <b>A property in every spelling the XAML compiler takes, by the property its owner names.</b> A setter's
/// property is read alone, after an owner or in parentheses, in its <c>Property</c> attribute or a
/// <c>Setter.Property</c> element, by <see cref="ResourceScope.SetterProperty"/>, and an element's own as an
/// attribute or a property element, by <see cref="ThemeReader.Local"/>. An owner names the property its static
/// field holds, which is a control's own after its type or a base, and after another owner only where the control
/// holds the same one, as a templated control, a <c>Panel</c> and a <c>ContentPresenter</c> hold
/// <c>Border</c>'s <c>BackgroundProperty</c>, by <see cref="ThemeReader.OwnershipOf"/>; on an element, only a
/// binding sets it after an owner outside its chain. A row theme's setter is read against the type it targets,
/// a style's for the rows against the rows', and a <c>TemplateBinding</c>'s or a path's against the control whose
/// template it is. <c>BNXQ1005</c> and <c>BNXQ1009</c> read setters and elements the same way. Measured at
/// 12.1.3 under Fluent, Simple and Semi, a <c>Background</c> written <c>Border.Background</c>,
/// <c>(Border.Background)</c> or in a <c>Setter.Property</c> element set it as one written plainly did; a template's
/// <c>Panel</c> written <c>Border.Background="{TemplateBinding Background}"</c> drew the row's; and a setter, a
/// <c>TemplateBinding</c> or a path that named a consumer's attached <c>Background</c> set or took another property,
/// not the row's.
/// </para>
/// <para>
/// ⚠ <b>What decides nothing.</b> What the rows present: the item template, the list's, its theme's or one
/// chosen by the item's type, a view a view locator chooses, and whatever a presenter in the template shows
/// through a content template or a data template, the row's content, the item or content it is given. A
/// row whose content paints across it is still reported, and the fix costs nothing. What hides an element
/// from the pointer, an <c>IsVisible</c>, an <c>IsHitTestVisible</c> or an <c>IsEnabled</c>, and how much
/// of the row it covers. What a <c>Popup</c> in the template holds, which is shown in a layer of its own,
/// and only while it is open, and what a tooltip, a flyout, a menu, styles or resources hold.
/// </para>
/// <para>
/// ⚠ <b>The state a row starts in.</b> A style that paints a row only while it is pointed at, pressed,
/// selected, focused or disabled leaves the state rows start in as it was, so it is passed over. Rows that
/// markup may disable or select from the start are the exception. Wherever markup in the scan sets
/// <c>IsEnabled</c> to anything but <c>True</c>, as an attribute, a property element or a setter, or gives a
/// control a <c>Command</c>, which disables a button or a menu item while it cannot execute, in any spelling
/// Avalonia takes, after an owner such as <c>InputElement.IsEnabled</c>, in parentheses, or in a setter's
/// <c>Setter.Property</c> element included,
/// <c>:disabled</c> is read as a state rows, and what their template holds, may start in: an element is
/// enabled only where its visual parent is, by <c>InputElement</c> at 12.1.3, and markup cannot always say
/// what holds what. Measured, rows took a <c>^:disabled</c> style's background from the start where their
/// list, their theme, the list's <c>ItemsPanel</c> or the <c>ItemsPresenter</c> in its own template disabled
/// them, and a part of their template took a <c>:disabled</c> style's where a view's markup or a templated
/// control's theme disabled it. Where the list, its theme or its type always selects a row, as a
/// <c>TabControl</c> does by its source at 12.1.3, or markup may select one, by a <c>SelectionMode</c> that
/// always selects, by name or by number, a <c>SelectedIndex</c>, a <c>SelectedItem</c>, the <c>Text</c> of an
/// editable combo box or a setter for <c>IsSelected</c>, in any of those spellings,
/// <c>:selected</c> is read as a state the rows selected may start in: measured, with
/// <c>SelectionMode="AlwaysSelected"</c> or <c>SelectedIndex="0"</c>, the first row's template took a
/// <c>:selected</c> style's paint from the start. A <c>:selected</c> style paints the rows selected and no
/// other, so a null <c>Background</c> the rest draw is still reported; where it paints a part of their
/// template, rows whose template draws nothing are not decided, since the rows selected draw there. One that
/// clears the background in such a state can break a row: measured, a <c>:pointerover</c> style setting
/// <c>{x:Null}</c> made the row's <c>IsPointerOver</c> flip on the next pointer move in every run, under
/// all three themes with a row theme whose template binds the row's background, and a click there missed in
/// up to three runs of ten. So it is named in <see cref="XamlRuleResult.Skipped"/>, as is a style that
/// picks some rows by a class, a name, a place, a property or the absence of a state and may clear them. A
/// style whose selector holds of every row in the state rows start in, whatever the data, sets every
/// row's background, however it is written, and <see cref="StyleSelectors.RowsPicked"/> says which:
/// measured, <c>ListBox ListBoxItem</c>, <c>ListBox &gt; ListBoxItem</c> and
/// <c>ListBoxItem:not(.nope)</c> each painted every row, and so did a row theme's nested
/// <c>^:not(.nope)</c>. A <c>:not()</c> of a state holds only while a row is out of it, so it may not hold
/// of every row. One that may hold of every row and paints, such as a property selector markup cannot
/// evaluate, leaves a null background undecided, and a row theme's nested style that may give rows
/// another template or theme leaves them undecided either way: measured, a nested <c>^[IsEnabled=True]</c>
/// whose template drew nothing left rows missing the click. A style that paints only some rows changes no
/// other: one asking for a class, a name or a state, or for a property the rows' theme binds to each row's
/// item, which the data decides row by row.
/// </para>
/// <para>
/// ⚠ <b>List, combo and tab rows, and not tree rows.</b> Fluent's and Simple's <c>TreeViewItem</c>
/// templates give the header presenter a background of its own, by their source at 12.1.3, so a null
/// row background still takes the click under them and not under Semi, and a view's markup cannot show
/// which theme applies. So tree rows are left out. A row is recognised as <c>BNXQ1009</c> recognises
/// one, by the peer its container creates, read from its compiled code: Avalonia's
/// <c>ListItemAutomationPeer</c>, which a <c>ListBoxItem</c>, and so a <c>ComboBoxItem</c>, and a
/// <c>TabItem</c> create at 12.1.3, by its source. So another framework's rows, whose styling this rule
/// does not read, are left alone.
/// </para>
/// <para>
/// ⚠ <b>Taken as drawing, not proved:</b> a row with no theme in the scan, which draws with its
/// framework theme's, as Fluent's, Simple's and Semi's were measured to; an element that takes the row's
/// <c>Background</c>, whatever hides it or however little of the row it covers; and an element that
/// paints, likewise. <b>Taken as a brush, not proved:</b> a value that is a resource or a static member.
/// <b>Taken to resolve, not proved:</b> a binding, which answers its <c>FallbackValue</c> instead where
/// its path fails. <b>Taken as picking only some rows, not proved:</b> a style asking for a property the
/// rows' theme binds to each row's item, which data true of every item would make pick them all, the property
/// matched by its name whatever owner the selector writes it after; and one
/// asking for <c>:selected</c> where rows may start selected, which a list of one row, or one that selects
/// every row, would make pick them all. <b>Taken as starting enabled and unselected, not proved:</b> rows no
/// markup in the scan disables or selects, which code, or markup outside the scan, may. <b>Taken as
/// applied, not proved:</b> a type's own markup, or its base's, which applies only where its constructor, or
/// the base's, calls <c>InitializeComponent</c>. <b>Taken as a reflection binding, not proved:</b> a <c>RelativeSource</c>
/// with an <c>AncestorLevel</c> or <c>Tree=Logical</c>, which a compiled binding, the default where a
/// project says nothing else, reads otherwise at 12.1.3: it takes the nearest ancestor of the type whatever
/// the level, and drops a search of the logical tree, by <c>AvaloniaXamlIlBindingPathParser</c>.
/// <b>Taken as true, not proved:</b> that a control takes the pointer only where it draws; one that
/// hit-tests itself may take it elsewhere. <b>Not read:</b> what code does to a background other than call
/// its setter in the type's own chain, such as <c>SetValue</c>, a setter called through a method of
/// another type, a binding, or a style or a class it adds; what a framework control draws other than its
/// background or fill, such as an image, text or a stroke; a default registered anywhere but the static
/// constructors of the type's own chain; an <c>ItemContainerTheme</c> set in code; an animation, and what a
/// behavior or a trigger sets; and code whose body cannot be read. What markup cannot decide is named in
/// <see cref="XamlRuleResult.Skipped"/>: a theme or template the scan cannot follow, styles that disagree,
/// a style that sets the rows' theme or template or that may paint every row, a row theme's nested style
/// that may give them another template, rows whose own code, or their list's, sets their background or
/// whose type's default cannot be read, a value the rule does not evaluate, a binding that passes the
/// row's background through something this rule does not evaluate or names an ancestor markup cannot
/// count back to the row, a template element that may paint, that a presenter in the template holds as
/// markup content, or whose own markup, or a base's, two files declare, two types in its chain have, or its
/// assembly holds compiled where the scan does not,
/// where nothing draws the row's background, a style that paints, clears or binds a part
/// of the rows' template where that could change the answer, a
/// style in another view that could, and every
/// <c>ItemsSource</c> in a scan given no assemblies.
/// </para>
/// </remarks>
public sealed class ItemContainerBackgroundRule : IXamlRule
{

    /// <inheritdoc />
    public string Id => "BNXQ1010";

    /// <inheritdoc />
    public string Summary => "Every list, combo or tab row generated from ItemsSource draws a background of its own: its template draws one, and where it draws the row's Background, that is never null.";

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
                        "The scan was given no assemblies, so whether this control generates list, combo or tab rows, "
                        + "and which theme draws them, could not be read and nothing was checked. Call WithAssemblies "
                        + "with the assembly whose markup this is; the framework is reached through its references.")),
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
                    if (verdict.Outcome == Outcome.DrawsNothing)
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
        Draws,
        DrawsNothing,
        Undecided,
    }

    /// <summary>What was decided about one control's rows, and the text that explains it.</summary>
    private readonly record struct Verdict(Outcome Outcome, string Text)
    {
        public static readonly Verdict NotASubject = new(Outcome.NotASubject, string.Empty);
        public static readonly Verdict Draws = new(Outcome.Draws, string.Empty);

        public static Verdict DrawsNothing(string why) => new(Outcome.DrawsNothing, why);

        public static Verdict Undecided(string why) => new(Outcome.Undecided, why);
    }

    /// <summary>Whether a value paints.</summary>
    private enum Paint
    {
        /// <summary>A brush, or what is taken as one.</summary>
        Draws,

        /// <summary><c>{x:Null}</c>, or a binding that can answer null.</summary>
        Null,

        /// <summary>A value this rule does not evaluate.</summary>
        Unknown,
    }

    /// <summary>What a row's template, or a templated control's, draws.</summary>
    private enum Drawn
    {
        /// <summary>An element draws the templated control's own <c>Background</c>, the row's for a row, so that value decides.</summary>
        TakesTheRows,

        /// <summary>No element draws the control's <c>Background</c>, and one paints a brush of its own.</summary>
        OwnBrush,

        /// <summary>No element draws the control's <c>Background</c> or paints one of its own, and none may.</summary>
        Nothing,

        /// <summary>A template this rule cannot follow, or, where nothing draws the control's <c>Background</c>, an element something else may paint.</summary>
        Unknown,
    }

    /// <summary>What a template draws, and the words that say so.</summary>
    /// <param name="Drawn">What it draws.</param>
    /// <param name="Text">
    /// For <see cref="Drawn.TakesTheRows"/>, what draws the control's <c>Background</c>, such as "the Border at
    /// View.axaml:12"; for <see cref="Drawn.Nothing"/> and <see cref="Drawn.Unknown"/>, why.
    /// </param>
    /// <param name="AlsoPaints">Whether, beside what draws the control's <c>Background</c>, an element paints a brush of its own.</param>
    /// <param name="Doubt">Why, beside what draws the control's <c>Background</c>, an element may paint; else <c>null</c>.</param>
    private readonly record struct Drawing(Drawn Drawn, string Text, bool AlsoPaints = false, string? Doubt = null);

    /// <summary>How one element of a template paints.</summary>
    private enum ElementPaint
    {
        /// <summary>Nothing paints it.</summary>
        Nothing,

        /// <summary>It draws the templated control's own <c>Background</c>, the row's for a row.</summary>
        TakesTheRows,

        /// <summary>It paints a brush of its own.</summary>
        OwnBrush,

        /// <summary>Something this rule does not read may paint it, or its value is not evaluated.</summary>
        Unknown,
    }

    /// <summary>What a style that sets <c>Background</c> can change about the rows in the state they start in.</summary>
    private enum Effect
    {
        /// <summary>Nothing: it does not pick them, or it paints only some of them or only in some state.</summary>
        None,

        /// <summary>It sets every row's <c>Background</c>.</summary>
        Base,

        /// <summary>It may leave some rows' <c>Background</c> null.</summary>
        BlanksRows,

        /// <summary>It may paint every row, and whether it picks them all is not decided.</summary>
        PaintsRows,

        /// <summary>It may leave a part of the rows' template drawing nothing.</summary>
        BlanksPart,

        /// <summary>It paints a part of the rows' template, which may draw where the rows do not.</summary>
        PaintsPart,

        /// <summary>It gives a part of the rows' template a value that may be null or a brush, such as a binding: it may do either of the two above.</summary>
        BlanksOrPaintsPart,

        /// <summary>It sets a property written after an owner that may name the rows' <c>Background</c>, and whether it does could not be read.</summary>
        Unknown,
    }

    /// <summary>What the styles and themes around a row may change, beside what decides its background.</summary>
    private sealed class Doubts
    {
        /// <summary>Why some rows' own <c>Background</c> may be null after all.</summary>
        public string? RowsBlank { get; set; }

        /// <summary>Why a part of the rows' template may draw nothing.</summary>
        public string? PartBlank { get; set; }

        /// <summary>Why the rows' <c>Background</c> may be a brush after all.</summary>
        public string? RowsPainted { get; set; }

        /// <summary>Why a part of the rows' template may draw.</summary>
        public string? PartPainted { get; set; }

        /// <summary>
        /// Why a part of the template of the rows selected may draw, and of no other: the rows that are not
        /// selected draw as they would.
        /// </summary>
        public string? SelectedPartPainted { get; set; }

        /// <summary>Why the rows may take a template other than their theme's, whichever way that goes.</summary>
        public string? Template { get; set; }

        /// <summary>
        /// Notes why a part of the rows' template may draw: in <see cref="SelectedPartPainted"/> where the style
        /// that may paint it picks only the rows selected, else in <see cref="PartPainted"/>.
        /// </summary>
        public void PartMayDraw(string? why, bool onlySelected)
        {
            if (onlySelected)
            {
                SelectedPartPainted ??= why;
            }
            else
            {
                PartPainted ??= why;
            }
        }
    }

    /// <summary>
    /// The states the rows of the control being decided, and what their template holds, may start in, with
    /// why, as clauses, or <c>null</c>: <c>disabled</c> where markup may disable them or a part of their
    /// template, and <c>selected</c> where it may select a row from the start.
    /// </summary>
    private sealed record Starting(string? Disabled, string? Selected)
    {
        /// <summary>No markup disables or selects them, so they start out of every state.</summary>
        public static readonly Starting OutOfEveryState = new(null, null);

        /// <summary>The states they may start in, which are then not states they start out of.</summary>
        public IReadOnlySet<string> States { get; } = StatesOf(Disabled, Selected);

        /// <summary>The states every row may start in: <see cref="States"/> but <c>selected</c>, which holds only of the rows selected.</summary>
        public IReadOnlySet<string> StatesOfEveryRow { get; } = StatesOf(Disabled, null);

        private static HashSet<string> StatesOf(string? disabled, string? selected)
        {
            HashSet<string> states = new(StringComparer.Ordinal);
            if (disabled is not null)
            {
                states.Add("disabled");
            }

            if (selected is not null)
            {
                states.Add("selected");
            }

            return states;
        }
    }

    /// <summary>
    /// The rows a control generates, as a style picks them: their style key and type chain, the host's,
    /// whether the rows' theme binds a property to each row's item, and the states they may start in.
    /// </summary>
    private sealed record RowsPicking(
        string StyleKey, IReadOnlyCollection<string> Chain, string HostKey, IReadOnlyCollection<string> HostChain, Func<string, bool> BoundToEachItem,
        Starting Starting)
    {
        /// <summary>How far <paramref name="style"/> picks these rows in the state they start in.</summary>
        public RowsPick Of(XElement style) => RowsPicked(style, StyleKey, Chain, HostKey, HostChain, BoundToEachItem, Starting.States);

        /// <summary>
        /// Whether <paramref name="style"/>, which may apply in the state rows start in, applies there only to
        /// the rows selected: it would not apply if no row started selected.
        /// </summary>
        public bool OnlySelected(XElement style) => RequiresState(style, Starting.StatesOfEveryRow);

        /// <summary>How a style that clears the rows outside a state they start out of may pick some of them, as a clause.</summary>
        public string SomePicked
        {
            get
            {
                List<string> states = [];
                if (Starting.Disabled is { } disabled)
                {
                    states.Add($":disabled, which they may start in, since {disabled}");
                }

                if (Starting.Selected is { } selected)
                {
                    states.Add($":selected, which some of them may start in, since {selected}");
                }

                return states.Count == 0
                    ? "a class, a name, a place, a property or the absence of a state"
                    : "a class, a name, a place, a property, the absence of a state, or " + string.Join(", or ", states);
            }
        }
    }

    /// <summary>How a style or a theme picks an element: its name, its type when it resolves, its style key, its type chain, its classes and its name.</summary>
    private readonly record struct Identity(
        string Name, Type? Type, string StyleKey, IReadOnlyCollection<string> Chain, IReadOnlyCollection<string> Classes, string? ElementName);

    /// <summary>One scan's worth of resolved types, resources and styles, shared by every control it decides.</summary>
    private sealed class Analysis
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        /// <summary>What a style or a theme sets that may paint what it picks, or give it a theme or a template that does.</summary>
        private static readonly string[] PaintingProperties = ["Background", "Fill", "Theme", "Template"];

        /// <summary>What a style sets on a part of a template, beside its background, that may change what that part draws.</summary>
        private static readonly string[] PartProperties = ["Fill", "Theme", "Template"];

        /// <summary>
        /// The framework's types that draw their own <c>Background</c> across their bounds, by their source at
        /// 12.1.3: a type draws it when one of these is in its chain. A templated control draws its
        /// <c>Background</c> only where its template takes it, and a shape draws its <c>Fill</c>.
        /// </summary>
        private static readonly HashSet<string> BackgroundDrawers = new(StringComparer.Ordinal) { "Border", "ContentPresenter", "Panel", "TextBlock", "TextPresenter" };

        /// <summary>The properties an element keeps what it presents in, so a property element for one holds its content.</summary>
        private static readonly HashSet<string> ContentProperties = new(StringComparer.Ordinal) { "Child", "Children", "Content", "Inlines", "Items" };

        /// <summary>What selects a list's rows, and what selects a row.</summary>
        private static readonly string[] SelectingProperties = ["SelectionMode", "SelectedIndex", "SelectedItem", "SelectedItems", "SelectedValue", "Selection", "IsSelected"];

        private readonly ElementTypes _types;
        private readonly ResourceScope _scope;
        private readonly ThemeReader _themes;

        /// <summary>
        /// Each type's own markup, a view's or any other's, by the <c>x:Class</c> its root declares; for a class two
        /// files declare, the first, which <see cref="MarkupDownTheChain"/> does not read.
        /// </summary>
        private readonly Dictionary<string, (XamlFile File, XElement Root)> _views = new(StringComparer.Ordinal);

        /// <summary>The classes two or more files declare as their <c>x:Class</c>, whose markup is not known.</summary>
        private readonly HashSet<string> _declaredTwice = new(StringComparer.Ordinal);

        /// <summary>
        /// Where markup anywhere in the scan sets <c>IsEnabled</c> to anything but <c>True</c>, as an attribute, a
        /// property element or a setter, as a clause; else <c>null</c>.
        /// </summary>
        private readonly string? _disabling;

        /// <summary>Where a setter anywhere in the scan may select a row, or a list's rows, from the start, as a clause; else <c>null</c>.</summary>
        private readonly string? _selectingSetter;

        /// <summary>
        /// The states the rows of the control being decided, and what their template holds, may start in;
        /// they start out of every other, which <c>:disabled</c> and <c>:selected</c> then are.
        /// </summary>
        private Starting _starting = Starting.OutOfEveryState;

        public Analysis(XamlScanContext context)
        {
            _types = new ElementTypes(context.Assemblies);
            _scope = new ResourceScope(context);
            _themes = new ThemeReader(_scope, _types);

            _disabling = context.ParsedFiles.SelectMany(DisablingIn).FirstOrDefault();

            _selectingSetter = context.ParsedFiles
                .SelectMany(file => file.Document!.Descendants()
                    .SelectMany(setter => SelectingProperties
                        .Where(property => SetsInAnySpelling(setter, property, _ => true))
                        .Select(property => (Property: property, Value: ThemeReader.ValueOf(setter, file)))))
                .FirstOrDefault(selecting => MaySelect(selecting.Property, selecting.Value)) is { Value: not null } selects
                ? $"a Setter at {selects.Value.Where} sets {selects.Property} to {selects.Value.Shown}"
                : null;

            foreach (XamlFile file in context.ParsedFiles)
            {
                if (file.Document!.Root is { } root
                    && root.Attribute(Xaml + "Class")?.Value.Trim() is { Length: > 0 } name
                    && !_views.TryAdd(name, (file, root)))
                {
                    _declaredTwice.Add(name);
                }
            }
        }

        public Verdict Decide(XamlFile file, XElement host)
        {
            _starting = Starting.OutOfEveryState;
            string hostName = host.Name.LocalName;
            ElementType resolved = _types.Resolve(hostName);

            switch (resolved.Kind)
            {
                case ElementKind.Missing:
                    return Verdict.Undecided(_types.WhyUnresolved(hostName) is { } why
                        ? $"{why}, so whether it generates list, combo or tab rows could not be read. {LoadedTypes.Remedy}"
                        : $"{hostName} is not a type in the scanned assemblies or the assemblies they reference, so "
                          + "whether it generates list, combo or tab rows could not be read. Pass the assembly that defines it to WithAssemblies.");
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
                    + "code, so whether its rows draw a background is unknown.");
            }

            // A list, combo or tab row, whose behaviour was measured. A tree row creates another peer, and is
            // left out.
            if (RowPeers.Of(container).Kind != RowKind.Content)
            {
                return Verdict.NotASubject;
            }

            IReadOnlyCollection<string> chain = ElementTypes.NamesOf(container);
            string styleKey = _types.StyleKeyOf(container).Name;
            string rows = $"The {container.Name} rows this {hostName} generates from ItemsSource";
            string them = $"the {container.Name} rows";

            // A value the row's own code sets is local, and outranks every style and theme; so is one the
            // list's own code sets on the rows it prepares.
            if (_types.CodeSetsPaint(container))
            {
                return Verdict.Undecided(
                    $"The code of {container.Name} sets Background, which markup does not show, so whether {them} draw "
                    + "across their width is not decided.");
            }

            if (_types.ConsumerCodeSetsPaint(type))
            {
                return Verdict.Undecided(
                    $"The code of {type.Name} sets a Background or a Fill, which may be its rows', and markup does not show it, so "
                    + $"whether {them} draw across their width is not decided.");
            }

            // A style that changes which theme or template applies is not evaluated.
            if (Styled(ElementTypes.NamesOf(type), $"the {hostName}", "ItemContainerTheme", "Theme") is { } hostStyled)
            {
                return Verdict.Undecided(hostStyled);
            }

            if (Styled(chain, them, "Theme", "Template") is { } containerStyled)
            {
                return Verdict.Undecided(containerStyled);
            }

            (Definition? hostTheme, string? hostThemeUnknown) = _themes.ThemeOfHost(host, file, type, "ItemContainerTheme");
            if (hostThemeUnknown is not null)
            {
                return Verdict.Undecided(hostThemeUnknown);
            }

            (Definition? containerTheme, string? containerThemeUnknown) =
                _themes.ItemContainerThemeOf(host, file, hostTheme, container, "Background", "Template");
            if (containerThemeUnknown is not null)
            {
                return Verdict.Undecided(containerThemeUnknown);
            }

            // Whether markup may disable the rows, or a part of their template, from the start, when :disabled
            // holds of them, and whether it may select a row from the start, when :selected holds of it.
            _starting = new Starting(DisabledAtStart(file, host, containerTheme), SelectedAtStart(file, host, type, hostTheme));

            // How a style picks these rows: by their type, by the host, by what their theme binds to each item,
            // and by the states they may start in.
            RowsPicking picking = new(styleKey, chain, _types.StyleKeyOf(type).Name, ElementTypes.NamesOf(type),
                property => BoundToEachItem(containerTheme, property), _starting);

            // A style written with no type that may pick the rows and give them a theme or a template changes
            // what draws them, as one naming them does.
            foreach ((XamlFile styleFile, XElement style) in _scope.Styles)
            {
                if (Picks(style, styleKey, chain) == Pick.No && picking.Of(style) != RowsPick.None
                    && style.Elements().FirstOrDefault(setter => Sets(setter, ["Theme", "Template"])) is { } retheming)
                {
                    return Verdict.Undecided(
                        $"A Style at {Where(styleFile, retheming)} sets {SetProperty(retheming)} on {them}, and styles are not "
                        + "evaluated, so what draws the rows' background is not decided from markup.");
                }
            }

            // What the styles that reach the list do; one elsewhere may reach it or not.
            List<Setting> painted = [];
            Doubts doubts = new();
            HashSet<XElement> reached = [];
            foreach ((XamlFile styleFile, XElement style) in _scope.StylesReaching(host))
            {
                reached.Add(style);
                (Effect effect, Setting? value, string text) = Classify(styleFile, style, picking, container, them);
                switch (effect)
                {
                    case Effect.Unknown:
                        return Verdict.Undecided(text);
                    case Effect.Base:
                        painted.Add(value!);
                        break;
                    case Effect.BlanksRows:
                        doubts.RowsBlank ??= text;
                        break;
                    case Effect.PaintsRows:
                        doubts.RowsPainted ??= text;
                        break;
                    case Effect.BlanksPart:
                        doubts.PartBlank ??= text;
                        break;
                    case Effect.PaintsPart:
                        doubts.PartMayDraw(text, picking.OnlySelected(style));
                        break;
                    case Effect.BlanksOrPaintsPart:
                        doubts.PartBlank ??= text;
                        doubts.PartMayDraw(text, picking.OnlySelected(style));
                        break;
                }

                doubts.PartMayDraw(PartPresenting(styleFile, style, picking, them), picking.OnlySelected(style));
            }

            foreach ((XamlFile styleFile, XElement style) in _scope.Styles)
            {
                if (styleFile == file || reached.Contains(style))
                {
                    continue;
                }

                string reach = $", and it is in no scope this markup shows reaching this {hostName}: whether it applies depends "
                               + "on where the view is placed at runtime.";
                if (PartPresenting(styleFile, style, picking, them) is { } part)
                {
                    doubts.PartMayDraw(part.TrimEnd('.') + reach, picking.OnlySelected(style));
                }

                (Effect effect, Setting? found, string doubt) = Classify(styleFile, style, picking, container, them);
                if (effect == Effect.Unknown)
                {
                    return Verdict.Undecided(doubt.TrimEnd('.') + reach);
                }

                if (effect == Effect.None || found is not { } value)
                {
                    continue;
                }

                string what = effect switch
                {
                    Effect.Base => $"sets Background on {them} to {value.Shown}",
                    Effect.BlanksRows => $"sets the Background of some of {them} to {value.Shown}",
                    Effect.PaintsRows => $"may set the Background of every one of {them} to {value.Shown}",
                    _ => $"sets Background on a part of the template of {them} to {value.Shown}",
                };
                string elsewhere = $"A Style at {value.Where} {what}{reach}";
                switch (effect)
                {
                    case Effect.Base when Read(value).Paint == Paint.Draws:
                    case Effect.PaintsRows:
                        doubts.RowsPainted ??= elsewhere;
                        break;
                    case Effect.Base or Effect.BlanksRows:
                        doubts.RowsBlank ??= elsewhere;
                        break;
                    case Effect.BlanksPart:
                        doubts.PartBlank ??= elsewhere;
                        break;
                    case Effect.BlanksOrPaintsPart:
                        doubts.PartBlank ??= elsewhere;
                        doubts.PartMayDraw(elsewhere, picking.OnlySelected(style));
                        break;
                    default:
                        doubts.PartMayDraw(elsewhere, picking.OnlySelected(style));
                        break;
                }
            }

            if (containerTheme is not null)
            {
                Nested(containerTheme, picking, them, doubts);
            }

            if (doubts.Template is { } retemplated)
            {
                return Verdict.Undecided(retemplated);
            }

            Drawing drawing = TemplateDrawing(containerTheme, container, row: true, []);
            switch (drawing.Drawn)
            {
                case Drawn.Unknown:
                    // A reason that is a clause, about one element, says what it leaves undecided.
                    return Verdict.Undecided(drawing.Text.EndsWith('.')
                        ? drawing.Text
                        : drawing.Text + ", so whether their template draws a background is not decided from markup.");
                case Drawn.OwnBrush:
                    return doubts.PartBlank is { } partBlank ? Verdict.Undecided(partBlank) : Verdict.Draws;
                case Drawn.Nothing:
                    // A part the rows selected may paint draws for them, and these rows are said to draw nothing.
                    return (doubts.PartPainted ?? doubts.SelectedPartPainted) is { } partPainted
                        ? Verdict.Undecided(partPainted)
                        : Verdict.DrawsNothing(
                            $"{rows} draw no background: {drawing.Text}. A control takes the pointer where it draws, so a click "
                            + "or a hover reaches a row only where something it holds draws, and elsewhere reaches what lies "
                            + "behind it. Give the template's root Background=\"{TemplateBinding Background}\", as the "
                            + "framework's own row templates do.");
            }

            // Something draws the row's own Background, so that value decides; what else may paint does not.
            Verdict verdict = painted.Count > 0 ? ByStyles(painted, rows, them, drawing.Text) : ByTheme(containerTheme, container, rows, them, drawing.Text);
            return verdict.Outcome switch
            {
                Outcome.DrawsNothing when (doubts.RowsPainted ?? doubts.PartPainted) is { } painting => Verdict.Undecided(painting),
                Outcome.Draws when (doubts.RowsBlank ?? doubts.PartBlank) is { } blank => Verdict.Undecided(blank),
                _ => verdict,
            };
        }

        /// <summary>
        /// Why the rows a control generates, or a part of their template, may start disabled, as markup shows
        /// it, as a clause; else <c>null</c>. They do where the control, or an element that holds it, sets
        /// <c>IsEnabled</c> to anything but <c>True</c>, and so may they wherever markup in the scan does, as an
        /// attribute, a property element or a setter, in any spelling Avalonia takes.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A control disabled anywhere disables everything it holds, and markup cannot always say what
        /// that is.</b> An element is enabled only where its visual parent is, by
        /// <c>InputElement.UpdateIsEffectivelyEnabled</c> at 12.1.3, and a row's visual parents lie in its list's
        /// items panel and template, a part's in what the rows' template, a view's markup or a templated
        /// control's theme puts around it: measured, rows started <c>:disabled</c> where the list's
        /// <c>ItemsPanel</c>, or the <c>ItemsPresenter</c> in its own template, was disabled, and a part did where
        /// a view's markup or a templated control's theme disabled it. So any <c>IsEnabled</c> but <c>True</c>
        /// in the scan makes <c>:disabled</c> a state they may start in, and the control, what holds it and their
        /// template are named first.
        /// </remarks>
        private string? DisabledAtStart(XamlFile file, XElement host, Definition? containerTheme)
        {
            foreach (XElement holder in host.AncestorsAndSelf().Where(element => !element.Name.LocalName.Contains('.', StringComparison.Ordinal)))
            {
                if (GivenInAnySpelling(holder, file, "IsEnabled").FirstOrDefault(MayDisable) is { } value)
                {
                    return $"the {holder.Name.LocalName} at {Where(file, holder)} sets IsEnabled to {value.Shown}";
                }
            }

            List<(XamlFile File, XElement Root)> template = [];
            if (containerTheme is not null)
            {
                _ = _themes.TemplateContent(_themes.InBaseState(containerTheme, "Template").Found, template);
            }

            foreach ((XamlFile templateFile, XElement root) in template)
            {
                foreach (XElement element in root.Descendants().Where(element => !element.Name.LocalName.Contains('.', StringComparison.Ordinal)))
                {
                    if (GivenInAnySpelling(element, templateFile, "IsEnabled").FirstOrDefault(MayDisable) is { } value)
                    {
                        return $"the {element.Name.LocalName} at {Where(templateFile, element)} in their template sets IsEnabled to {value.Shown}";
                    }
                }
            }

            return _disabling;
        }

        /// <summary>
        /// Where a file's markup may disable what it reaches, each as a clause: a setter, or an element's
        /// attribute or property element, that sets <c>IsEnabled</c> to anything but <c>True</c>, or that gives
        /// a control a <c>Command</c>, in any spelling Avalonia takes: alone, after an owner, or in parentheses, and a
        /// setter's in its <c>Property</c> attribute or its <c>Setter.Property</c> element.
        /// </summary>
        /// <remarks>
        /// ⚠ A button or a menu item is disabled while its command cannot execute, by the <c>IsEnabledCore</c> of
        /// <c>Button</c>, <c>SplitButton</c> and <c>MenuItem</c> at 12.1.3, and so is all it holds, which markup
        /// cannot tell from the start.
        /// </remarks>
        private IEnumerable<string> DisablingIn(XamlFile file)
        {
            foreach (XElement element in file.Document!.Descendants())
            {
                bool enabling = SetsInAnySpelling(element, "IsEnabled", OwnsIsEnabled);
                if (enabling || SetsInAnySpelling(element, "Command", _ => true))
                {
                    Setting set = ThemeReader.ValueOf(element, file);
                    if (enabling ? MayDisable(set) : GivesACommand(set))
                    {
                        yield return $"a Setter at {set.Where} sets {SetProperty(element)} to {set.Shown}";
                    }
                }
                else if (!element.Name.LocalName.Contains('.', StringComparison.Ordinal))
                {
                    if (GivenInAnySpelling(element, file, "IsEnabled").FirstOrDefault(MayDisable) is { } enabled)
                    {
                        yield return $"the {element.Name.LocalName} at {Where(file, element)} sets IsEnabled to {enabled.Shown}";
                    }
                    else if (GivenInAnySpelling(element, file, "Command").FirstOrDefault(GivesACommand) is { } command)
                    {
                        yield return $"the {element.Name.LocalName} at {Where(file, element)} sets Command to {command.Shown}, which disables a "
                                     + "button or a menu item while it cannot execute";
                    }
                }
            }
        }

        /// <summary>Whether a value of <c>Command</c> gives a command: anything but <c>{x:Null}</c> or nothing.</summary>
        private static bool GivesACommand(Setting value) =>
            value.Element is not null ? value.Element.Name.LocalName != "Null" : !string.IsNullOrWhiteSpace(value.Text) && !IsNullLiteral(value);

        /// <summary>Whether a value of <c>IsEnabled</c> may disable: anything but a literal <c>True</c>, in any case; an empty one sets nothing.</summary>
        private static bool MayDisable(Setting value) =>
            value.Element is not null
            || (!string.IsNullOrWhiteSpace(value.Text) && !string.Equals(value.Text.Trim(), "True", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Every value markup may give an element for its own <paramref name="property"/>, in any spelling Avalonia
        /// takes: alone, or after an owner that names its own, as an attribute or a property element, as
        /// <see cref="ThemeReader.MayGive"/> reads them, where one that may be its own counts, and any may be the one
        /// the framework applies last.
        /// </summary>
        /// <remarks>
        /// ⚠ At 12.1.3 the XAML compiler takes <c>Owner.Name</c> for the property in the owner's static
        /// <c>NameProperty</c> field, by <c>XamlIlAvaloniaPropertyHelper</c>: measured, a list written
        /// <c>InputElement.IsEnabled="False"</c> started disabled, and one written <c>ListBox.SelectedIndex="0"</c>
        /// started with a row selected. After an owner outside the element's chain it takes a binding alone: measured,
        /// a list written <c>ComboBox.SelectedIndex="{Binding Zero}"</c> started with its first row selected.
        /// </remarks>
        private IEnumerable<Setting> GivenInAnySpelling(XElement element, XamlFile file, string property, Type? type = null) =>
            _themes.MayGive(element, file, property, type);

        /// <summary>
        /// Whether a setter sets <paramref name="property"/>, written alone or after an owner <paramref name="owns"/>
        /// accepts, as <see cref="ResourceScope.SetterProperty"/> reads its name: in parentheses or not, in its
        /// <c>Property</c> attribute or its <c>Setter.Property</c> element. The XAML compiler takes each at 12.1.3,
        /// and Avalonia's own themes write attached properties in parentheses.
        /// </summary>
        private static bool SetsInAnySpelling(XElement setter, string property, Func<string, bool> owns)
        {
            if (ResourceScope.SetterProperty(setter) is not { } name)
            {
                return false;
            }

            int dot = name.LastIndexOf('.');
            return dot < 0 ? name == property : name[(dot + 1)..] == property && owns(name[..dot]);
        }

        /// <summary>
        /// Whether the owner a setter names for <c>IsEnabled</c>, which may style any control, makes it a control's
        /// own: a type that takes part in focus, as <c>InputElement</c>, which declares it, and every control do, or
        /// one this rule cannot resolve. An owner that resolves to another type names a property of its own, an
        /// attached one, such as a <c>WordWrap.IsEnabled</c>.
        /// </summary>
        private bool OwnsIsEnabled(string owner) => _types.Resolve(LocalPart(owner)).Kind != ElementKind.Other;

        /// <summary>
        /// Whether a <c>SelectionMode</c> written as text may always select a row: unless every flag it names is
        /// <c>Single</c>, <c>Multiple</c> or <c>Toggle</c>, or a number without <c>AlwaysSelected</c>'s, 4 at
        /// 12.1.3, which the XAML compiler takes as well as a name: measured, <c>SelectionMode="4"</c> selected a
        /// row from the start.
        /// </summary>
        private static bool ModeSelects(string text) =>
            text.Split((char[])[',', ' ', '|'], StringSplitOptions.RemoveEmptyEntries).Any(flag =>
                flag is not ("Single" or "Multiple" or "Toggle")
                && (!int.TryParse(flag, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || (value & 4) != 0));

        /// <summary>
        /// Why a row the control generates may start selected, as markup shows it, as a clause; else
        /// <c>null</c>. One may where the control, or its theme, sets a selection, or a <c>SelectionMode</c>
        /// that always selects one; where it sets the <c>Text</c> an editable combo box selects a row by; where
        /// its type's own <c>SelectionMode</c> always selects one by default, as a <c>TabControl</c>'s does at
        /// 12.1.3; and where a setter anywhere in the scan selects a row or sets a list's selection.
        /// </summary>
        /// <remarks>
        /// ⚠ A list whose <c>SelectionMode</c> is <c>AlwaysSelected</c>, or whose markup selects an item, starts
        /// with that row selected: measured on Avalonia 12.1.3 under Fluent, Simple and Semi, with
        /// <c>SelectionMode="AlwaysSelected"</c> or <c>SelectedIndex="0"</c>, the first row's template took a
        /// <c>ListBoxItem:selected</c> style's paint from the start.
        /// </remarks>
        private string? SelectedAtStart(XamlFile file, XElement host, Type type, Definition? hostTheme)
        {
            foreach (string property in SelectingProperties)
            {
                if (GivenInAnySpelling(host, file, property, type).FirstOrDefault(given => MaySelect(property, given)) is { } value)
                {
                    return $"the {host.Name.LocalName} at {Where(file, host)} sets {property} to {value.Shown}";
                }

                if (hostTheme is not null && _themes.InBaseState(hostTheme, property).Found.Setting is { } themed && MaySelect(property, themed))
                {
                    return $"the ControlTheme at {hostTheme.Where} sets {property} on the {host.Name.LocalName} at {themed.Where} to {themed.Shown}";
                }
            }

            // An editable combo box selects the row its text names.
            if (GivenInAnySpelling(host, file, "Text", type).FirstOrDefault(given => MaySelect("Text", given)) is { } text)
            {
                return $"the {host.Name.LocalName} at {Where(file, host)} sets Text to {text.Shown}, which selects the row it names where it is editable";
            }

            if (_types.DefaultText(type, "SelectionModeProperty") is { } mode && ModeSelects(mode))
            {
                return $"the SelectionMode of a {type.Name} is {mode} unless markup says otherwise";
            }

            return _selectingSetter;
        }

        /// <summary>
        /// Whether a value of one of <see cref="SelectingProperties"/> may select a row from the start: a
        /// <c>SelectionMode</c> that may always select one, by name or by number, or any not written as text; an
        /// <c>IsSelected</c> but a literal <c>False</c>; a <c>SelectedIndex</c> but a negative number; and any
        /// other but <c>{x:Null}</c>. An empty one sets nothing.
        /// </summary>
        private static bool MaySelect(string property, Setting value)
        {
            if (value.Element is { } element)
            {
                return element.Name.LocalName != "Null";
            }

            string text = value.Text?.Trim() ?? string.Empty;
            if (text.Length == 0 || ThemeReader.IsNull(text))
            {
                return false;
            }

            bool literal = MarkupExtension.Parse(text) is null;
            return property switch
            {
                "SelectionMode" => !literal || ModeSelects(text),
                "IsSelected" => !literal || !string.Equals(text, "False", StringComparison.OrdinalIgnoreCase),
                "SelectedIndex" => !literal || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) || index >= 0,
                _ => true,
            };
        }

        /// <summary>What a style that may reach the list can change about the rows' <c>Background</c>.</summary>
        /// <remarks>
        /// A style whose selector holds of every row in the state rows start in, whatever the data, sets every
        /// row's <c>Background</c>, however it is written: <see cref="StyleSelectors.RowsPicked"/> says which.
        /// One that may hold of every row and paints leaves the rows undecided where they draw nothing. A setter
        /// sets the rows' <c>Background</c> only after an owner that names their own, as
        /// <see cref="ThemeReader.LastSetting(IEnumerable{XElement}, XamlFile, string, string, Type)"/> reads it; a part's, in any spelling, since what the part is is not
        /// read here.
        /// </remarks>
        private (Effect Effect, Setting? Value, string Text) Classify(XamlFile file, XElement style, RowsPicking picking, Type container, string them)
        {
            Pick part = PicksTemplateOf(style, picking.StyleKey, picking.Chain);
            (XElement? setter, string? unknown) = part != Pick.No
                ? (style.Elements().LastOrDefault(child => ThemeReader.SetsProperty(child, "Background")), null)
                : _themes.LastSetting(style.Elements(), file, "Background", container.Name, container);
            if (setter is null && unknown is null)
            {
                return (Effect.None, null, string.Empty);
            }

            Pick row = Picks(style, picking.StyleKey, picking.Chain);
            RowsPick rows = row == Pick.Every ? RowsPick.All : picking.Of(style);
            if (part == Pick.No && row == Pick.No && rows == RowsPick.None)
            {
                return (Effect.None, null, string.Empty);
            }

            if (unknown is not null)
            {
                return (Effect.Unknown, null, unknown);
            }

            Setting value = ThemeReader.ValueOf(setter!, file);
            bool draws = Read(value).Paint == Paint.Draws;
            bool inState = RequiresState(style, picking.Starting.States);
            if (part != Pick.No)
            {
                if (draws)
                {
                    return inState
                        ? (Effect.None, null, string.Empty)
                        : (Effect.PaintsPart, value,
                            $"A Style at {value.Where} paints a part of the template of {them} with {value.Shown}, which may "
                            + "draw where the rows themselves draw nothing; this rule does not follow a template's parts.");
                }

                // A binding, or a value this rule does not evaluate, may answer a brush as well as a null.
                if (!inState && !IsNullLiteral(value))
                {
                    return (Effect.BlanksOrPaintsPart, value,
                        $"A Style at {value.Where} sets Background on a part of the template of {them} to {value.Shown}, which may "
                        + "draw where the rows themselves draw nothing, or leave that part drawing nothing; this rule does not "
                        + "follow a template's parts.");
                }

                return (Effect.BlanksPart, value,
                    $"A Style at {value.Where} sets Background on a part of the template of {them} to {value.Shown}"
                    + $"{(inState ? " in some state" : string.Empty)}, which may leave that part drawing nothing; this rule "
                    + "does not follow a template's parts.");
            }

            if (rows == RowsPick.All)
            {
                return (Effect.Base, value, string.Empty);
            }

            if (draws)
            {
                return rows == RowsPick.Unknown
                    ? (Effect.PaintsRows, value,
                        $"A Style at {value.Where} may set the Background of every one of {them} to {value.Shown}, and whether "
                        + "its selector picks them all is not decided from markup.")
                    : (Effect.None, null, string.Empty);
            }

            return (Effect.BlanksRows, value, inState
                ? $"A Style at {value.Where} sets the Background of {them} to {value.Shown} while they are pointed at, "
                  + "pressed, selected, focused or disabled, and whether they then lose the pointer depends on their "
                  + "template and theme, which is not decided from markup."
                : $"A Style at {value.Where} sets the Background of some of {them} to {value.Shown}, picking them by "
                  + $"{picking.SomePicked}, and which rows it picks is not decided from markup.");
        }

        /// <summary>Notes in <paramref name="doubts"/> what the nested styles along a row theme's chain may change.</summary>
        /// <remarks>
        /// <para>
        /// A nested style whose selector is <c>^</c> alone is also the base that
        /// <see cref="ThemeReader.InBaseState(Definition, string, Func{XElement, bool})"/> reads, and when it sets a value that is
        /// not a brush, that value already decides the rows, whatever this adds.
        /// </para>
        /// <para>
        /// ⚠ <b>A nested style that may hold of every row paints them all.</b> Measured on Avalonia 12.1.3, a
        /// row theme whose own <c>Background</c> was null and whose nested <c>^:not(.nope)</c> or
        /// <c>^:not(:selected)</c> set <c>Transparent</c> took the click across every row. And one that gives
        /// some rows, or every row, another template changes what draws them, whichever way: measured, a
        /// nested <c>^[IsEnabled=True]</c> whose template drew nothing left rows whose theme's own template
        /// drew <c>Transparent</c> missing the click.
        /// </para>
        /// </remarks>
        private void Nested(Definition theme, RowsPicking picking, string them, Doubts doubts)
        {
            foreach (Definition link in _themes.ChainOf(theme, "Background", "Template").Themes)
            {
                foreach (XElement style in NestedStyles(link.Element))
                {
                    string selector = style.Attribute("Selector")?.Value.Trim() ?? string.Empty;
                    bool inTemplate = selector.Contains("/template/", StringComparison.Ordinal);
                    if (selector != "^" && !inTemplate && picking.Of(style) != RowsPick.None
                        && style.Elements().FirstOrDefault(setter => Sets(setter, ["Template", "Theme"])) is { } retemplate)
                    {
                        doubts.Template ??=
                            $"The ControlTheme at {link.Where} sets {SetProperty(retemplate)} on some or all of {them} in a nested style "
                            + $"at {Where(link.File, retemplate)}, and what that template draws, and which rows it picks, is not decided from markup.";
                    }

                    if (inTemplate && PartPresenting(link.File, style, them, picking.Starting.States) is { } part)
                    {
                        doubts.PartMayDraw(part, picking.OnlySelected(style));
                    }

                    if (style.Elements().LastOrDefault(setter => ThemeReader.SetsProperty(setter, "Background")) is not { } setter)
                    {
                        continue;
                    }

                    Setting value = ThemeReader.ValueOf(setter, link.File);
                    bool draws = Read(value).Paint == Paint.Draws;
                    bool inState = RequiresState(style, picking.Starting.States);
                    if (inTemplate)
                    {
                        if (!draws)
                        {
                            doubts.PartBlank ??=
                                $"The ControlTheme at {link.Where} sets Background on a part of its template to {value.Shown} "
                                + $"in a nested style at {value.Where}, which may leave that part drawing nothing; this rule "
                                + "does not follow a template's parts.";
                        }

                        // A binding, or a value this rule does not evaluate, may answer a brush as well as a null.
                        if (!inState && !IsNullLiteral(value))
                        {
                            doubts.PartMayDraw(
                                $"The ControlTheme at {link.Where} {(draws ? "paints a part of its template with" : "sets Background on a part of its template to")} "
                                + $"{value.Shown} in a nested style at {value.Where}, which may draw where the rows themselves draw nothing; this "
                                + "rule does not follow a template's parts.",
                                picking.OnlySelected(style));
                        }

                        continue;
                    }

                    if (!draws)
                    {
                        doubts.RowsBlank ??= inState
                            ? $"The ControlTheme at {link.Where} sets the Background of {them} to {value.Shown} in a nested "
                              + $"style at {value.Where} while they are pointed at, pressed, selected, focused or disabled, "
                              + "and whether they then lose the pointer depends on their template, which is not decided from markup."
                            : $"The ControlTheme at {link.Where} sets the Background of some of {them} to {value.Shown} in a "
                              + $"nested style at {value.Where}, picking them by {picking.SomePicked}, and which rows it picks "
                              + "is not decided from markup.";
                    }
                    else if (selector != "^" && picking.Of(style) is RowsPick.All or RowsPick.Unknown)
                    {
                        doubts.RowsPainted ??=
                            $"The ControlTheme at {link.Where} sets the Background of {them} to {value.Shown} in a nested style at "
                            + $"{value.Where} whose selector may hold of every row in the state rows start in, so they may draw after all.";
                    }
                }
            }
        }

        /// <summary>
        /// Why a style that reaches into the rows' template may change what a part of it draws: it may pick a
        /// part of their template, in the state rows start in, and sets its fill, its theme or its template;
        /// else <c>null</c>. What a part presents, its content or its content template, is what the rows
        /// present, and decides nothing.
        /// </summary>
        /// <remarks>
        /// ⚠ A selector with a condition outranks what the template's own markup gives a part, by
        /// <c>BindingPriority</c> at 12.1.3, where a style trigger ranks above a template's value: measured,
        /// <c>ListBoxItem:not(.nope) /template/ ContentPresenter#PART_ContentPresenter</c> giving the rows'
        /// presenter a <c>ContentTemplate</c> replaced the one its <c>TemplateBinding</c> gave it.
        /// </remarks>
        private static string? PartPresenting(XamlFile file, XElement style, RowsPicking picking, string them) =>
            PicksTemplateOf(style, picking.StyleKey, picking.Chain) != Pick.No ? PartPresenting(file, style, them, picking.Starting.States) : null;

        /// <summary>Why a style that reaches into a template, in no state the rows start out of, may change what a part of it draws.</summary>
        private static string? PartPresenting(XamlFile file, XElement style, string them, IReadOnlySet<string> startStates) =>
            !RequiresState(style, startStates) && style.Elements().FirstOrDefault(setter => Sets(setter, PartProperties)) is { } setter
                ? $"A Style at {Where(file, setter)} sets {SetProperty(setter)} on a part of the template of {them}, which may "
                  + "change what that part draws; this rule does not follow a template's parts."
                : null;

        /// <summary>
        /// Whether the rows' theme, in the state rows start in, binds <paramref name="property"/> to each row's item,
        /// after any owner, as a style's selector names it: an attached property, such as one a row's item highlights by,
        /// as surely as the row's own.
        /// </summary>
        private bool BoundToEachItem(Definition? theme, string property) =>
            theme is not null && _themes.InBaseState(theme, property, _ => true).Found.Setting is { } value && IsBoundToTheItem(value);

        /// <summary>
        /// Whether a value is bound to the data context, which on a row is its item: a binding with no source,
        /// relative source or element name, whose path does not start in the visual tree, or a multi-binding
        /// whose every binding is one.
        /// </summary>
        private static bool IsBoundToTheItem(Setting value)
        {
            if (value.Element is { } element)
            {
                return element.Name.LocalName switch
                {
                    "Binding" or "CompiledBinding" or "ReflectionBinding" =>
                        ReadsTheItem(element.Attributes().Select(attribute => attribute.Name.LocalName), element.Attribute("Path")?.Value),
                    "MultiBinding" => element.Elements().Where(part => !part.Name.LocalName.Contains('.', StringComparison.Ordinal)).ToList() is { Count: > 0 } parts
                                      && parts.All(part => IsBoundToTheItem(new Setting(null, part, value.File, value.At))),
                    _ => false,
                };
            }

            return value.Text is { } text
                   && MarkupExtension.Parse(text) is { } extension
                   && LocalPart(extension.Name) is "Binding" or "CompiledBinding" or "ReflectionBinding"
                   && ReadsTheItem(extension.Named().Select(argument => argument.Name), extension.Positional(0) ?? extension.Argument("Path"));
        }

        /// <summary>Whether a binding with these named arguments and this path reads its data context.</summary>
        private static bool ReadsTheItem(IEnumerable<string> names, string? path) =>
            !names.Any(name => name is "Source" or "RelativeSource" or "ElementName")
            && path?.TrimStart() is not ['$', ..] and not ['#', ..];

        /// <summary>The styles nested in a theme or a style, at any depth, but not those inside a template.</summary>
        private static IEnumerable<XElement> NestedStyles(XElement holder) =>
            holder.Elements()
                .Where(child => child.Name.LocalName == "Style")
                .SelectMany(style => NestedStyles(style).Prepend(style));

        /// <summary>
        /// What a row's template, or a templated control's, draws: what in it draws the control's own
        /// <c>Background</c>, and beside that, what paints a brush of its own and what may paint.
        /// </summary>
        /// <param name="theme">The control's theme, or <c>null</c> for a row with none in the scan.</param>
        /// <param name="templated">The control's type.</param>
        /// <param name="row">Whether the control is a row, whose framework template was measured to draw its <c>Background</c>.</param>
        /// <param name="seen">The themes and views being read further out, so that one holding itself is not read again.</param>
        /// <remarks>
        /// ⚠ <b>The elements of a template are those its markup writes</b>, at any depth, and what a property
        /// element holds where the property is the element's content. What a presenter in it shows through a
        /// content template or a data template is what the control presents, and decides nothing; what a
        /// <c>Popup</c> holds is shown in a layer of its own, or not at all.
        /// </remarks>
        private Drawing TemplateDrawing(Definition? theme, Type templated, bool row, HashSet<XElement> seen)
        {
            if (theme is null)
            {
                // The framework's own row template, which draws the row's Background: measured for Fluent,
                // Simple and Semi.
                return new Drawing(Drawn.TakesTheRows, "the framework theme's own row template");
            }

            (Found template, ChainEnd end) = _themes.InBaseState(theme, "Template");
            if (template.IsUnknown)
            {
                return new Drawing(Drawn.Unknown, template.Unknown!);
            }

            if (!template.IsSet)
            {
                // No template down the chain: the framework theme's, which was measured for a row, or the
                // default that ContentControl's static constructor registers, a ContentPresenter that takes
                // the control's Background. A row is a ContentControl.
                if (end == ChainEnd.Framework)
                {
                    return row
                        ? new Drawing(Drawn.TakesTheRows, $"the framework theme's own row template, which the ControlTheme at {theme.Where} is BasedOn")
                        : new Drawing(Drawn.Unknown, $"The ControlTheme at {theme.Where} sets no Template and is BasedOn the framework's theme, whose template may paint");
                }

                return row || ElementTypes.NamesOf(templated).Contains("ContentControl", StringComparer.Ordinal)
                    ? new Drawing(Drawn.TakesTheRows,
                        $"the ContentPresenter of the template ContentControl registers by default, which the ControlTheme at {theme.Where} leaves them")
                    : new Drawing(Drawn.Nothing, $"the ControlTheme at {theme.Where} sets no Template and is BasedOn nothing, so they present nothing");
            }

            List<(XamlFile File, XElement Root)> content = [];
            if (_themes.TemplateContent(template, content) is { } unknown)
            {
                return new Drawing(Drawn.Unknown, unknown);
            }

            string where = template.Setting!.Where;
            if (content.Count == 0)
            {
                return new Drawing(Drawn.Nothing, $"the template at {where} is null, so they present nothing at all");
            }

            string? taker = null;
            bool paints = false;
            string? doubt = null;
            foreach ((XamlFile file, XElement holder) in content)
            {
                // What the template element holds, not the template element itself.
                foreach (XElement element in Presented(holder).SelectMany(AndPresented))
                {
                    (ElementPaint paint, string? why) = PaintOf(file, element, templated, $"The template at {where}", seen);
                    switch (paint)
                    {
                        case ElementPaint.TakesTheRows:
                            taker ??= why;
                            break;
                        case ElementPaint.OwnBrush:
                            paints = true;
                            break;
                        case ElementPaint.Unknown:
                            doubt ??= why;
                            break;
                    }
                }
            }

            return taker is not null ? new Drawing(Drawn.TakesTheRows, taker, paints, doubt)
                : paints ? new Drawing(Drawn.OwnBrush, string.Empty)
                : doubt is not null ? new Drawing(Drawn.Unknown, doubt)
                : new Drawing(Drawn.Nothing,
                    $"no element of their template at {where} draws the row's Background or paints a Background or a Fill of its own");
        }

        /// <summary>
        /// How one element of a template, or of a view's own markup, paints: it draws the templated control's
        /// own <c>Background</c>, paints a brush of its own, may paint, or does neither. For
        /// <see cref="ElementPaint.TakesTheRows"/>, the words name what draws the <c>Background</c>, such as
        /// "the Border at View.axaml:12"; for <see cref="ElementPaint.Unknown"/>, they say why, as a clause
        /// that begins with <paramref name="whose"/>.
        /// </summary>
        /// <param name="file">The file the element is in.</param>
        /// <param name="element">The element.</param>
        /// <param name="templatedParent">
        /// The type of the templated control whose own template the element is in, the row's for the row's
        /// template; <c>null</c> for a view's markup, which is no template.
        /// </param>
        /// <param name="whose">How the clause names the template, such as "The template at View.axaml:12".</param>
        /// <param name="seen">The themes and views being read further out.</param>
        /// <remarks>
        /// <para>
        /// ⭐ <b>An element draws a <c>Background</c> only where its type does.</b> The framework's
        /// <c>Border</c>, <c>Panel</c>, <c>ContentPresenter</c>, <c>TextPresenter</c> and <c>TextBlock</c> draw
        /// their own, and a shape its <c>Fill</c>, by their source at 12.1.3; a templated control draws its
        /// <c>Background</c> only where its own template takes it, which <see cref="TemplatedPaint"/> reads.
        /// A value given to any other element is not known to be drawn, and is not decided.
        /// </para>
        /// <para>
        /// ⚠ <b>What a template's markup gives an element ranks below a style with a condition</b>, by
        /// <c>BindingPriority</c> at 12.1.3, where a style trigger ranks above a template's value and a style
        /// with no condition below it, and below what the element's own code sets. So a value that a style
        /// with a condition, a nested style of the element's theme, or its code may replace is not decided.
        /// Where nothing in markup sets one, any style or theme that may set it decides it.
        /// </para>
        /// </remarks>
        private (ElementPaint Paint, string? Why) PaintOf(
            XamlFile file, XElement element, Type? templatedParent, string whose, HashSet<XElement> seen)
        {
            string name = element.Name.LocalName;
            string at = Where(file, element);
            Identity id = IdentityOf(element);

            if (templatedParent is not null && PresenterHolding(element) is { } presenter)
            {
                return (ElementPaint.Unknown,
                    $"{whose} holds {A(name)} at {at} as the content of the {presenter.Name.LocalName} at {Where(file, presenter)}, where it has "
                    + "no templated parent and its logical parent may not be where the template writes it, so what it draws is not followed");
            }

            if (id.Type is { } templated && ElementTypes.IsTemplated(templated))
            {
                return TemplatedPaint(file, element, id, templated, templatedParent, whose, seen);
            }

            // An element whose type does not resolve may be a templated one, and a Template of its own says so;
            // with none, it may draw anything.
            if (id.Type is null && _themes.Local(element, file, "Template").Setting is { } ownTemplate)
            {
                return IsNullLiteral(ownTemplate)
                    ? (ElementPaint.Nothing, null)
                    : (ElementPaint.Unknown, $"{whose} gives {A(name)} at {at} a Template of its own, {ownTemplate.Shown}, which this rule does not follow");
            }

            if (id.Type is null)
            {
                return (ElementPaint.Unknown,
                    $"{whose} holds {A(name)} at {at}, a type neither the scanned assemblies nor those they reference define once, so what it draws is not read");
            }

            // A type with markup of its own, or a base's, found by its x:Class, as a view has: what that markup
            // holds paints beside it, and what its root gives it is a local value.
            if (OwnMarkupUnread(id.Type) is { } unread)
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose {unread}");
            }

            (XamlFile File, XElement Root)? own = OwnMarkupOf(id.Type);
            (bool ownPaints, string? ownDoubt) = (false, null);
            if (own is not null)
            {
                (bool paints, string? doubt, _, string? notRead) = OwnMarkupPaint(id.Type, name, at, whose, seen);
                if (notRead is not null)
                {
                    return (ElementPaint.Unknown, notRead);
                }

                (ownPaints, ownDoubt) = (paints, doubt);
            }

            List<(string Property, bool InTemplate)> cleared = [];
            foreach (string property in (string[])["Background", "Fill"])
            {
                // In a template, what its own markup gives it outranks what the template gives it, but for a
                // binding; elsewhere, what markup gives it replaces it.
                Found local = _themes.Local(element, file, property, id.Type);
                Found ownFound = own is { } markup ? _themes.Local(markup.Root, markup.File, property, id.Type) : Found.Nothing;
                if ((local.Unknown ?? ownFound.Unknown) is { } unreadable)
                {
                    return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, and {Lowered(unreadable)}");
                }

                Setting? given = local.Setting;
                Setting? ownValue = ownFound.Setting;
                bool ownWins = ownValue is not null && (given is null || (templatedParent is not null && !IsBinding(given)));
                if ((ownWins ? ownValue : given) is not { } paint)
                {
                    continue;
                }

                // A value the template gives it ranks as the template's, which a style with a condition outranks.
                bool inTemplate = templatedParent is not null && !ownWins;
                if (IsNullLiteral(paint))
                {
                    cleared.Add((property, inTemplate));
                    continue;
                }

                if (!Draws(id, property))
                {
                    return (ElementPaint.Unknown,
                        $"{whose} sets {property} on {A(name)} at {paint.Where}, which this rule does not know {A(name)} to draw");
                }

                ElementPaint painted;
                string? drawer = null;
                if (inTemplate && TakesTheRowsBackground(paint, element, templatedParent!))
                {
                    (painted, string? changed) = ThroughTheBinding(paint);
                    if (painted == ElementPaint.Unknown)
                    {
                        return (ElementPaint.Unknown, $"{whose} gives {A(name)} at {paint.Where} the templated control's Background {changed}");
                    }

                    drawer = $"the {name} at {at}";
                }
                else
                {
                    (Paint read, string why) = Read(paint);
                    if (read != Paint.Draws)
                    {
                        return (ElementPaint.Unknown, $"{whose} sets {property} on {A(name)} at {paint.Where} to {paint.Shown}, {why}");
                    }

                    painted = ElementPaint.OwnBrush;
                }

                if (MayOverride(file, element, id, property, inTemplate) is { } overridden)
                {
                    return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose {property} {overridden}");
                }

                return (painted, drawer);
            }

            if (cleared.Count > 0)
            {
                // Its own null stands unless something that outranks it may set a brush.
                foreach ((string property, bool inTemplate) in cleared)
                {
                    if (MayOverride(file, element, id, property, inTemplate) is { } overridden)
                    {
                        return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose {property} {overridden}");
                    }
                }

                return ownPaints ? (ElementPaint.OwnBrush, null) : ownDoubt is not null ? (ElementPaint.Unknown, ownDoubt) : (ElementPaint.Nothing, null);
            }

            // What its own markup holds paints, whatever paints the element itself.
            if (ownPaints)
            {
                return (ElementPaint.OwnBrush, null);
            }

            // What code sets is a local value; what draws itself may draw anything.
            Type type = id.Type;
            if (_types.CodeSetsPaint(type))
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose own code sets its Background or its Fill");
            }

            if (_types.DrawsItself(type))
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, which draws itself in a Render of its own");
            }

            // The theme it names; with none, or {x:Null}, the implicit one for its style key, which reaches an
            // element with no template as surely as a templated control.
            Found themeFound = _themes.Local(element, file, "Theme", id.Type);
            if (themeFound.IsUnknown)
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, and {Lowered(themeFound.Unknown!)}");
            }

            Setting? named = themeFound.Setting;
            (Definition? theme, string? unknown, bool noTheme) = named is null ? (null, null, true) : _themes.ThemeOf(named, "its Theme");
            if (!noTheme)
            {
                if (unknown is not null || theme is null || Paints(theme))
                {
                    return (ElementPaint.Unknown, $"{whose} gives {A(name)} at {at} a Theme, {named!.Shown}, which may paint it");
                }
            }
            else
            {
                (Definition? implicitTheme, string? implicitUnknown) =
                    _themes.ImplicitTheme(id.StyleKey, element, $"{A(name)} at {at}", PaintingProperties);
                if (implicitUnknown is not null)
                {
                    return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, and {Lowered(implicitUnknown)}");
                }

                if (implicitTheme is not null && Paints(implicitTheme))
                {
                    return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, which its implicit theme at {implicitTheme.Where} may paint");
                }
            }

            foreach ((XamlFile styleFile, XElement style) in StylesPicking(file, element, id))
            {
                if (style.Elements().FirstOrDefault(setter => Sets(setter, PaintingProperties)) is { } setter)
                {
                    return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, and a Style at {Where(styleFile, setter)} may paint it");
                }
            }

            // What its type registers as a default, which its static constructor can make a brush.
            return _types.PaintsByDefault(type) switch
            {
                null => (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose default Background or Fill could not be read"),
                true when Draws(id, "Background") || Draws(id, "Fill") => (ElementPaint.OwnBrush, null),
                true => (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose type registers a default it may draw"),
                false when ownDoubt is not null => (ElementPaint.Unknown, ownDoubt),
                false => (ElementPaint.Nothing, null),
            };
        }

        /// <summary>
        /// Whether an element draws its own <paramref name="property"/>: a <c>Background</c> where its type
        /// chain, or its name when its type does not resolve, holds one of <see cref="BackgroundDrawers"/>, and
        /// a <c>Fill</c> where it is a shape.
        /// </summary>
        private static bool Draws(Identity id, string property) =>
            property == "Fill" ? id.Chain.Contains("Shape", StringComparer.Ordinal) : id.Chain.Any(BackgroundDrawers.Contains);

        /// <summary>
        /// What may give an element another <paramref name="property"/> than its markup does, as a clause that
        /// continues "whose Background": its own code; and, in a template, a style with a condition that may
        /// pick it, or a nested style with one in its theme, in the state it starts in; else <c>null</c>.
        /// </summary>
        private string? MayOverride(XamlFile file, XElement element, Identity id, string property, bool inTemplate)
        {
            if (id.Type is { } type && _types.CodeSetsPaint(type))
            {
                return "its own code may set";
            }

            if (!inTemplate)
            {
                // Markup outside a template sets a local value, which no style outranks.
                return null;
            }

            foreach ((XamlFile styleFile, XElement style) in StylesPicking(file, element, id))
            {
                if (IsConditional(style) && style.Elements().FirstOrDefault(setter => Sets(setter, [property])) is { } setter)
                {
                    return $"a Style at {Where(styleFile, setter)} may set, outranking the template's own value";
                }
            }

            (Definition? theme, string? unknown) = ThemeOfElement(file, element, id, property);
            if (unknown is not null)
            {
                return $"its theme may set: {Lowered(unknown)}";
            }

            if (theme is not null)
            {
                foreach (Definition link in _themes.ChainOf(theme, property).Themes)
                {
                    if (NestedStyles(link.Element).FirstOrDefault(style => IsConditional(style) && !RequiresState(style, _starting.States)
                                                                           && style.Elements().Any(setter => Sets(setter, [property]))) is { } nested)
                    {
                        return $"a nested style of its theme at {link.Where} may set, at {Where(link.File, nested)}, outranking the template's own value";
                    }
                }
            }

            return null;
        }

        /// <summary>How a style or a theme picks <paramref name="element"/>.</summary>
        private Identity IdentityOf(XElement element)
        {
            string name = element.Name.LocalName;
            (IReadOnlyCollection<string> classes, string? elementName) = ClassesAndName(element);
            return _types.Resolve(name).Type is { } type
                ? new Identity(name, type, _types.StyleKeyOf(type).Name, ElementTypes.NamesOf(type), classes, elementName)
                : new Identity(name, null, name, [name], classes, elementName);
        }

        /// <summary>
        /// The styles that may pick an element in the state it starts in, which is the rows' when it is in their
        /// template: the scan's, and those declared in the <c>Styles</c> of what holds it in a template.
        /// </summary>
        private IEnumerable<(XamlFile File, XElement Style)> StylesPicking(XamlFile file, XElement element, Identity id) =>
            _scope.Styles.Concat(StylesDeclaredAround(file, element))
                .Where(entry => MayPick(entry.Item2, id.StyleKey, id.Chain, id.Classes, id.ElementName, _starting.States));

        /// <summary>
        /// The theme an element takes, as the framework finds it: the one its <c>Theme</c> names, else, with
        /// none or <c>{x:Null}</c>, the implicit one keyed by its style key that reaches it; or why that is
        /// not decided.
        /// </summary>
        /// <remarks>
        /// ⚠ Any styled element takes one, a <c>Border</c> as much as a control, by
        /// <c>StyledElement.GetEffectiveTheme</c> at 12.1.3; measured, a <c>Border</c> in an item template
        /// that an implicit theme in the list's resources made <c>Transparent</c> took the click.
        /// </remarks>
        private (Definition? Theme, string? Unknown) ThemeOfElement(XamlFile file, XElement element, Identity id, params string[] relevant)
        {
            Found found = _themes.Local(element, file, "Theme", id.Type);
            if (found.IsUnknown)
            {
                return (null, found.Unknown);
            }

            if (found.Setting is { } named)
            {
                (Definition? theme, string? unknown, bool cleared) = _themes.ThemeOf(named, "its Theme");
                if (!cleared)
                {
                    return (theme, unknown);
                }
            }

            return _themes.ImplicitTheme(id.StyleKey, element, $"{A(id.Name)} at {Where(file, element)}", relevant);
        }

        /// <summary>
        /// Whether a setter sets one of <paramref name="properties"/>, named without its owner, so that
        /// <c>Grid.Column</c> and <c>(Grid.Column)</c> both set <c>Column</c>.
        /// </summary>
        private static bool Sets(XElement setter, string[] properties) =>
            SetProperty(setter) is { } property && properties.Contains(property, StringComparer.Ordinal);

        /// <summary>The property a setter sets, named without its owner; <c>null</c> when it is not a setter.</summary>
        private static string? SetProperty(XElement setter) =>
            ResourceScope.SetterProperty(setter) is { } written ? ResourceScope.PropertyName(written) : null;

        /// <summary>
        /// The styles declared in the <c>Styles</c> of an element and of what holds it, inside a
        /// <c>ControlTheme</c>, where <see cref="ResourceScope.Styles"/> leaves them out.
        /// </summary>
        /// <remarks>
        /// ⚠ A style in a control's <c>Styles</c> reaches that control and what it holds, so a template's
        /// own styles reach its parts. Measured, a template's <c>Border</c> painted by a style in its own
        /// <c>Border.Styles</c> took the click.
        /// </remarks>
        private static IEnumerable<(XamlFile File, XElement Style)> StylesDeclaredAround(XamlFile file, XElement element) =>
            element.AncestorsAndSelf()
                .SelectMany(holder => holder.Elements().Where(child => child.Name.LocalName.EndsWith(".Styles", StringComparison.Ordinal)))
                .SelectMany(slot => slot.Descendants().Where(style => style.Name.LocalName == "Style" && style.Attribute("Selector") is not null))
                .Where(style => style.Ancestors().Any(ancestor => ancestor.Name.LocalName == "ControlTheme"))
                .Select(style => (file, style));

        /// <summary>
        /// How a templated control in a template, or in a view's markup, paints: by what its own template
        /// draws, and, where that template draws the control's own <c>Background</c>, by that value, which may
        /// be the row's.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⭐ <b>A templated control draws its <c>Background</c> only where its own template takes it.</b> So
        /// one whose <c>Background</c> takes the row's draws the row's only when its template draws its own:
        /// a <c>Background</c>, a brush or a binding, given to a control whose template ignores it, draws
        /// nothing. Its template is the one its markup gives it, else its theme's, the one it names or its
        /// implicit one, read as a row's is, down its <c>BasedOn</c> chain.
        /// </para>
        /// <para>
        /// ⚠ <b>With no theme in the scan, only a <c>ContentControl</c> or a <c>UserControl</c> is
        /// decided.</b> Neither has an implicit theme in Fluent or Simple at 12.1.3, by their source, so each
        /// takes the default template that <c>ContentControl</c>'s static constructor registers, a
        /// <c>ContentPresenter</c> that draws the control's <c>Background</c>. A view, a <c>UserControl</c>
        /// subclass, is read from its own markup as well. Any other templated control may be painted by its
        /// framework's theme.
        /// </para>
        /// <para>
        /// ⚠ <b>In a template, a view's own <c>Background</c> outranks the template's</b>, as does that of any
        /// type with markup of its own. The XAML compiler sets what a template's markup gives an element at
        /// <c>BindingPriority.Template</c>, by <c>AvaloniaXamlIlControlTemplatePriorityTransformer</c> at 12.1.3,
        /// and a view's own markup sets its root's as a local value, which ranks above that: measured, a view
        /// whose root was <c>Transparent</c> drew it, whether the template gave it the row's null background or
        /// <c>{x:Null}</c>. A binding is the exception: the compiler sets it as a local value even in a template,
        /// by <c>XamlIlAvaloniaPropertyHelper</c> at 12.1.3, after the view's own, so it replaces it. Outside a
        /// template, what markup gives a view is a local value set after the view's constructor has set its own,
        /// so it replaces it: measured, a view's markup that gave a <c>Transparent</c> view <c>{x:Null}</c> left
        /// it drawing nothing.
        /// </para>
        /// <para>
        /// ⚠ <b>A background no markup sets is the type's default</b>, read from its property metadata once
        /// its class constructors have run, since a control may register one there.
        /// </para>
        /// </remarks>
        private (ElementPaint Paint, string? Why) TemplatedPaint(
            XamlFile file, XElement element, Identity id, Type type, Type? templatedParent, string whose, HashSet<XElement> seen)
        {
            string name = id.Name;
            string at = Where(file, element);

            if (_types.CodeSetsPaint(type))
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose own code sets its Background or its Fill");
            }

            if (_types.DrawsItself(type))
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, which draws itself in a Render of its own");
            }

            foreach ((XamlFile styleFile, XElement style) in StylesPicking(file, element, id))
            {
                if (style.Elements().FirstOrDefault(setter => Sets(setter, PaintingProperties)) is { } setter)
                {
                    return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, and a Style at {Where(styleFile, setter)} may paint it");
                }
            }

            (Drawing own, Definition? theme, Setting? ownBackground) = OwnTemplate(file, element, id, type, whose, seen);
            switch (own.Drawn)
            {
                case Drawn.Unknown:
                    return (ElementPaint.Unknown, own.Text);
                case Drawn.OwnBrush:
                    return (ElementPaint.OwnBrush, null);
                case Drawn.Nothing:
                    return (ElementPaint.Nothing, null);
            }

            // Its template draws its own Background, so that value decides: in a template, what its own markup
            // gives it, which outranks the template's but for a binding; else what its markup gives it; else its
            // own markup's; else its theme's in the state it starts in; else its type's default.
            Found background = _themes.Local(element, file, "Background", type);
            if (background.IsUnknown)
            {
                return (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose own template draws its Background, and {Lowered(background.Unknown!)}");
            }

            Setting? given = background.Setting;
            Setting? local = templatedParent is not null && ownBackground is not null && !IsBinding(given) ? null : given;
            if (local is not null && templatedParent is not null && TakesTheRowsBackground(local, element, templatedParent))
            {
                (ElementPaint through, string? changed) = ThroughTheBinding(local);
                return through switch
                {
                    ElementPaint.Unknown => (ElementPaint.Unknown, $"{whose} gives {A(name)} at {local.Where} the templated control's Background {changed}"),
                    ElementPaint.OwnBrush => (ElementPaint.OwnBrush, null),
                    _ => (ElementPaint.TakesTheRows, $"the {name} at {at}, whose own template draws its Background with {own.Text}"),
                };
            }

            // Null only as written, {x:Null}, or as the default; a binding may answer a brush.
            Paint paint;
            string clause;
            if ((local ?? ownBackground) is { } effective)
            {
                (Paint read, string why) = Read(effective);
                (paint, clause) = IsNullLiteral(effective) ? (Paint.Null, string.Empty)
                    : read == Paint.Draws ? (Paint.Draws, string.Empty)
                    : (Paint.Unknown, $"its Background at {effective.Where} is {effective.Shown}, {why}");
            }
            else
            {
                (Found found, ChainEnd end) = theme is null ? (Found.Nothing, ChainEnd.Nothing) : _themes.InBaseState(theme, "Background");
                if (found.Setting is { } themed)
                {
                    (Paint read, string why) = Read(themed);
                    (paint, clause) = IsNullLiteral(themed) ? (Paint.Null, string.Empty)
                        : read == Paint.Draws ? (Paint.Draws, string.Empty)
                        : (Paint.Unknown, $"its theme sets its Background at {themed.Where} to {themed.Shown}, {why}");
                }
                else
                {
                    (paint, clause) = found.IsUnknown ? (Paint.Unknown, Lowered(found.Unknown!))
                        : end == ChainEnd.Framework ? (Paint.Unknown, "its theme is BasedOn the framework's, which may set its Background")
                        : _types.PaintsByDefault(type) switch
                        {
                            true => (Paint.Draws, string.Empty),
                            false => (Paint.Null, string.Empty),
                            null => (Paint.Unknown, "its default Background could not be read"),
                        };
                }
            }

            return paint switch
            {
                Paint.Draws => (ElementPaint.OwnBrush, null),
                Paint.Null when own.AlsoPaints => (ElementPaint.OwnBrush, null),
                Paint.Null when own.Doubt is { } doubt => (ElementPaint.Unknown, doubt),
                Paint.Null => (ElementPaint.Nothing, null),
                _ => (ElementPaint.Unknown, $"{whose} holds {A(name)} at {at}, whose own template draws its Background, and {clause}"),
            };
        }

        /// <summary>
        /// What a templated control's own template draws, and the theme that gives it, read as a row's is;
        /// for one with markup of its own, a view's or another found by its <c>x:Class</c>, with what that
        /// markup holds and the <c>Background</c> its root gives it.
        /// </summary>
        private (Drawing Drawing, Definition? Theme, Setting? OwnBackground) OwnTemplate(
            XamlFile file, XElement element, Identity id, Type type, string whose, HashSet<XElement> seen)
        {
            string name = id.Name;
            string at = Where(file, element);

            // A Template of its own outranks its theme's, and draws whatever it draws.
            Found template = _themes.Local(element, file, "Template", type);
            if (template.IsUnknown)
            {
                return (new Drawing(Drawn.Unknown, $"{whose} holds {A(name)} at {at}, and {Lowered(template.Unknown!)}"), null, null);
            }

            if (template.Setting is { } ownTemplate)
            {
                return (IsNullLiteral(ownTemplate)
                    ? new Drawing(Drawn.Nothing, string.Empty)
                    : new Drawing(Drawn.Unknown, $"{whose} gives {A(name)} at {at} a Template of its own, {ownTemplate.Shown}, which this rule does not follow"), null, null);
            }

            // A style that reaches into its template may change what a part of it draws.
            foreach ((XamlFile styleFile, XElement style) in _scope.Styles)
            {
                if (PicksTemplateOf(style, id.StyleKey, id.Chain) != Pick.No && !RequiresState(style, _starting.States)
                    && style.Elements().FirstOrDefault(setter => Sets(setter, PaintingProperties)) is { } setter)
                {
                    return (new Drawing(Drawn.Unknown,
                        $"{whose} holds {A(name)} at {at}, and a Style at {Where(styleFile, setter)} sets {SetProperty(setter)} on a part of its template"), null, null);
                }
            }

            (Definition? theme, string? unknown) = ThemeOfElement(file, element, id, "Background", "Template");
            if (unknown is not null)
            {
                return (new Drawing(Drawn.Unknown, $"{whose} holds {A(name)} at {at}, and {Lowered(unknown)}"), null, null);
            }

            // A view, a UserControl subclass, has markup of its own, and so may any other type, or a base, found by
            // its x:Class.
            bool view = id.Chain.Contains("UserControl", StringComparer.Ordinal) && type.Name != "UserControl";
            (bool ownPaints, string? ownDoubt, Setting? ownBackground) = (false, null, null);
            if (view || OwnMarkupOf(type) is not null || OwnMarkupUnread(type) is not null)
            {
                (bool paints, string? doubt, Setting? background, string? notRead) = OwnMarkupPaint(type, name, at, whose, seen);
                if (notRead is not null)
                {
                    return (new Drawing(Drawn.Unknown, notRead), null, null);
                }

                (ownPaints, ownDoubt, ownBackground) = (paints, doubt, background);
            }

            Drawing drawing;
            if (theme is null)
            {
                if (!view && type.Name is not ("ContentControl" or "UserControl"))
                {
                    return (new Drawing(Drawn.Unknown, $"{whose} holds {A(name)} at {at}, a templated control whose own theme may paint it"), null, null);
                }

                // The default template, a ContentPresenter that draws the control's own Background.
                drawing = new Drawing(Drawn.TakesTheRows, "the ContentPresenter of the template ContentControl registers by default");
            }
            else
            {
                if (!seen.Add(theme.Element))
                {
                    return (new Drawing(Drawn.Unknown, $"{whose} holds {A(name)} at {at}, whose theme at {theme.Where} holds what it themes"), null, null);
                }

                try
                {
                    if (NestedPainting(theme))
                    {
                        return (new Drawing(Drawn.Unknown, $"{whose} holds {A(name)} at {at}, which a nested style of its own theme at {theme.Where} may paint"), null, null);
                    }

                    drawing = TemplateDrawing(theme, type, row: false, seen);
                    if (drawing.Drawn == Drawn.Unknown)
                    {
                        return (new Drawing(Drawn.Unknown, $"{whose} holds {A(name)} at {at}, and {Lowered(drawing.Text)}"), null, null);
                    }
                }
                finally
                {
                    seen.Remove(theme.Element);
                }
            }

            // What its own markup holds paints beside its template.
            drawing = drawing.Drawn switch
            {
                Drawn.TakesTheRows => drawing with { AlsoPaints = drawing.AlsoPaints || ownPaints, Doubt = drawing.Doubt ?? ownDoubt },
                _ when ownPaints => new Drawing(Drawn.OwnBrush, string.Empty),
                _ when ownDoubt is not null => new Drawing(Drawn.Unknown, ownDoubt),
                _ => drawing,
            };
            return (drawing, theme, ownBackground);
        }

        /// <summary>
        /// Whether a nested style along a theme's chain may paint what the theme styles, or a part of its
        /// template, in the state it starts in: it sets a background, a fill, a theme or a template, asks
        /// for no state, and is not the theme alone, whose setters <see cref="ThemeReader.InBaseState"/> reads.
        /// </summary>
        private bool NestedPainting(Definition theme) =>
            _themes.ChainOf(theme, PaintingProperties).Themes
                .SelectMany(link => NestedStyles(link.Element))
                .Any(style => style.Attribute("Selector")?.Value.Trim() != "^"
                              && !RequiresState(style, _starting.States)
                              && style.Elements().Any(setter => PaintingProperties.Any(property => ThemeReader.SetsProperty(setter, property))));

        /// <summary>
        /// The markup a type's own <c>x:Class</c> names, or a base's, when it is the only markup of its own any type
        /// in its chain has, and the scan holds it once; else <c>null</c>, and <see cref="OwnMarkupUnread"/> says
        /// why when there is markup.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A subclass carries its base's markup.</b> Its constructor runs its base's, which loads the base's
        /// markup onto it: measured on Avalonia 12.1.3, a subclass with no markup of its own of a <c>Border</c> or
        /// a <c>Panel</c> whose own markup was <c>Transparent</c> was <c>Transparent</c>, and in a template that
        /// outranked a <c>TemplateBinding</c> as the base's own does. Markup compiled into a type's assembly whose
        /// file the scan does not hold, as a library's control's is, is not read, and leaves the type undecided.
        /// </remarks>
        private (XamlFile File, XElement Root)? OwnMarkupOf(Type? type) =>
            MarkupDownTheChain(type) is ([var only], null) ? only : null;

        /// <summary>
        /// Why markup a type, or a base, has of its own is not read, as a clause that continues "whose": a class
        /// two files declare, one whose compiled markup the scan does not hold, or more than one type in its chain
        /// with markup of its own, which each load in turn; else <c>null</c>.
        /// </summary>
        private string? OwnMarkupUnread(Type? type) =>
            MarkupDownTheChain(type) switch
            {
                (_, { } unread) => unread,
                ({ Count: > 1 }, _) => "type and a base both have markup of their own, found by their x:Class, which this rule does not combine",
                _ => null,
            };

        /// <summary>
        /// The markup that a type and each of its bases name by their <c>x:Class</c>, the type's first, and why
        /// any of theirs cannot be read, as a clause that continues "whose": a class two files declare, or one
        /// that holds compiled markup, found by the <c>!XamlIlPopulate</c> the XAML compiler adds to it, whose
        /// file the scan does not hold.
        /// </summary>
        private (List<(XamlFile File, XElement Root)> Markup, string? Unread) MarkupDownTheChain(Type? type)
        {
            List<(XamlFile File, XElement Root)> markup = [];
            string? unread = null;
            for (Type? link = type; link is not null; link = link.BaseType)
            {
                if (link.FullName is not { } fullName)
                {
                    continue;
                }

                if (_declaredTwice.Contains(fullName))
                {
                    unread ??= "own markup, or a base's, found by its x:Class, is not in the scan once";
                }
                else if (_views.TryGetValue(fullName, out (XamlFile File, XElement Root) found))
                {
                    markup.Add(found);
                }
                else if (_types.HasCompiledMarkup(link))
                {
                    unread ??= "own markup, or a base's, is compiled into its assembly and is not in the scan";
                }
            }

            return (markup, unread);
        }

        /// <summary>
        /// What a type's own markup, found by its <c>x:Class</c>, holds, a view's or any other's: whether
        /// something there paints a brush of its own, why something there may paint, and the
        /// <c>Background</c> its root gives the type; or why that markup cannot be read, as a clause that
        /// begins with <paramref name="whose"/>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Any type's markup is compiled alike.</b> The XAML compiler gives every <c>x:Class</c> root the
        /// same populate method whatever it derives from, which its constructor runs through
        /// <c>InitializeComponent</c>, and sets what the root gives it, and adds what it holds, as local
        /// values, by <c>XamlCompilerTaskExecutor</c> and <c>AvaloniaXamlIlControlTemplatePriorityTransformer</c>
        /// at 12.1.3: measured, a <c>Border</c> subclass and a <c>Panel</c> subclass whose own markup was
        /// <c>Transparent</c> drew it in a row's template.
        /// </remarks>
        private (bool Paints, string? Doubt, Setting? Background, string? NotRead) OwnMarkupPaint(Type type, string name, string at, string whose, HashSet<XElement> seen)
        {
            string kind = ElementTypes.NamesOf(type).Contains("UserControl", StringComparer.Ordinal) ? "a view whose" : "whose";
            if (OwnMarkupUnread(type) is { } unread)
            {
                return (false, null, null, $"{whose} holds {A(name)} at {at}, {kind} {unread}");
            }

            if (OwnMarkupOf(type) is not { } view)
            {
                return (false, null, null, $"{whose} holds {A(name)} at {at}, {kind} own markup, found by its x:Class, is not in the scan once");
            }

            Found template = _themes.Local(view.Root, view.File, "Template", type);
            Found theme = _themes.Local(view.Root, view.File, "Theme", type);
            if ((template.Unknown ?? theme.Unknown) is { } unreadable)
            {
                return (false, null, null, $"{whose} holds {A(name)} at {at}, {kind} own markup may give it a Template or a Theme, and {Lowered(unreadable)}");
            }

            if (template.IsSet || theme.IsSet)
            {
                return (false, null, null, $"{whose} holds {A(name)} at {at}, {kind} own markup gives it a Template or a Theme, which this rule does not follow");
            }

            Found background = _themes.Local(view.Root, view.File, "Background", type);
            if (background.IsUnknown)
            {
                return (false, null, null, $"{whose} holds {A(name)} at {at}, {kind} own markup may give it a Background, and {Lowered(background.Unknown!)}");
            }

            if (!seen.Add(view.Root))
            {
                return (false, null, null, $"{whose} holds {A(name)} at {at}, {kind} own markup holds it again");
            }

            try
            {
                string markup = $"The markup of {name} at {Where(view.File, view.Root)}";
                bool paints = false;
                string? doubt = null;
                foreach (XElement held in AndPresented(view.Root).Skip(1))
                {
                    (ElementPaint paint, string? why) = PaintOf(view.File, held, null, markup, seen);
                    paints |= paint == ElementPaint.OwnBrush;
                    if (paint == ElementPaint.Unknown)
                    {
                        doubt ??= why;
                    }
                }

                return (paints, doubt, background.Setting, null);
            }
            finally
            {
                seen.Remove(view.Root);
            }
        }

        /// <summary>
        /// What an element presents: its child elements, and what a property element holds when the
        /// property is where the element keeps its content; not a tooltip, a flyout, a menu, its styles or
        /// its resources, which are shown elsewhere or not at all, nor what a <c>Popup</c> holds, which is
        /// shown in a layer of its own, and only while it is open.
        /// </summary>
        private IEnumerable<XElement> Presented(XElement element)
        {
            if (element.Name.LocalName == "Popup" || IdentityOf(element).Chain.Contains("Popup", StringComparer.Ordinal))
            {
                yield break;
            }

            foreach (XElement child in element.Elements())
            {
                if (!child.Name.LocalName.Contains('.', StringComparison.Ordinal))
                {
                    yield return child;
                }
                else if (ContentProperties.Contains(ResourceScope.PropertyName(child.Name.LocalName)))
                {
                    foreach (XElement held in child.Elements().Where(node => !node.Name.LocalName.Contains('.', StringComparison.Ordinal)))
                    {
                        yield return held;
                    }
                }
            }
        }

        /// <summary>An element and everything it presents, at any depth.</summary>
        private IEnumerable<XElement> AndPresented(XElement element) =>
            Presented(element).SelectMany(AndPresented).Prepend(element);

        /// <summary>A sentence as a clause that continues another: its first letter lowered and its full stop dropped.</summary>
        private static string Lowered(string sentence) =>
            sentence.Length == 0 ? sentence : char.ToLowerInvariant(sentence[0]) + sentence[1..].TrimEnd('.');

        /// <summary>
        /// Whether a theme, down its chain, sets a background, a fill, a theme or a template anywhere in it, or
        /// reaches the framework's.
        /// </summary>
        private bool Paints(Definition theme)
        {
            (IReadOnlyList<Definition> themes, ChainEnd end, string? unknown) = _themes.ChainOf(theme, PaintingProperties);
            return unknown is not null
                   || end == ChainEnd.Framework
                   || themes.Any(link => link.Element.Descendants().Any(setter => Sets(setter, PaintingProperties)));
        }

        /// <summary>
        /// The classes an element may carry, from its <c>Classes</c> attribute and each <c>Classes.x</c>
        /// it sets, and its name, from <c>Name</c> or <c>x:Name</c>.
        /// </summary>
        private static (IReadOnlyCollection<string> Classes, string? Name) ClassesAndName(XElement element)
        {
            List<string> classes = [];
            string? name = null;
            foreach (XAttribute attribute in element.Attributes())
            {
                string local = attribute.Name.LocalName;
                if (local == "Classes")
                {
                    classes.AddRange(attribute.Value.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
                }
                else if (local.StartsWith("Classes.", StringComparison.Ordinal))
                {
                    classes.Add(local["Classes.".Length..]);
                }
                else if (local == "Name")
                {
                    name = attribute.Value.Trim();
                }
            }

            return (classes, name);
        }

        /// <summary>Whether a value is <c>{x:Null}</c> as written, in either spelling.</summary>
        private static bool IsNullLiteral(Setting value) =>
            value.Element is { Name.LocalName: "Null" } || (value.Text is { } text && ThemeReader.IsNull(text.Trim()));

        private static string Where(XamlFile file, XElement element) =>
            ThemeReader.LineOf(element) is { } line ? $"{file.RelativePath}:{line}" : file.RelativePath;

        /// <summary>The rows' <c>Background</c> as the styles that set it on every row say it, where <paramref name="drawer"/> draws it.</summary>
        private static Verdict ByStyles(List<Setting> painted, string rows, string them, string drawer)
        {
            List<(Setting Value, Paint Paint, string Why)> read = [];
            foreach (Setting value in painted)
            {
                (Paint paint, string why) = Read(value);
                read.Add((value, paint, why));
            }

            if (read.FirstOrDefault(entry => entry.Paint == Paint.Unknown) is { Value: not null } unknown)
            {
                return Verdict.Undecided(
                    $"A Style sets the Background of {them} at {unknown.Value.Where} to {unknown.Value.Shown}, {unknown.Why}.");
            }

            if (read.All(entry => entry.Paint == Paint.Draws))
            {
                return Verdict.Draws;
            }

            if (read.All(entry => entry.Paint == Paint.Null))
            {
                return Null(rows, drawer, $"a Style at {read[0].Value.Where} sets it to {read[0].Value.Shown}", read[0].Why,
                    "Make it Transparent where it would be null, for instance with TargetNullValue={x:Static Brushes.Transparent} "
                    + "on a binding; Transparent looks the same over the list.");
            }

            (Setting Value, Paint Paint, string Why) blank = read.First(entry => entry.Paint == Paint.Null);
            (Setting Value, Paint Paint, string Why) brush = read.First(entry => entry.Paint == Paint.Draws);
            return Verdict.Undecided(
                $"Styles at {blank.Value.Where} and {brush.Value.Where} both set the Background of {them}, one to a value "
                + "that can be null and one to a brush, and which wins depends on their order, which this rule does not evaluate.");
        }

        /// <summary>
        /// The rows' <c>Background</c> as their theme says it, down its <c>BasedOn</c> chain, and where the
        /// chain sets none, as the row's type gives it by default; where <paramref name="drawer"/> draws it.
        /// </summary>
        private Verdict ByTheme(Definition? containerTheme, Type container, string rows, string them, string drawer)
        {
            if (containerTheme is null)
            {
                // The framework's own theme for the row, which draws: measured for Fluent, Simple and Semi.
                return Verdict.Draws;
            }

            (Found found, ChainEnd end) = _themes.InBaseState(containerTheme, "Background");
            if (found.IsUnknown)
            {
                return Verdict.Undecided(found.Unknown!);
            }

            if (found.IsSet)
            {
                Setting value = found.Setting!;
                (Paint paint, string why) = Read(value);
                return paint switch
                {
                    Paint.Draws => Verdict.Draws,
                    Paint.Null => Null(rows, drawer, $"the ControlTheme sets it at {value.Where} to {value.Shown}", why,
                        "Make it Transparent where it would be null, for instance with TargetNullValue={x:Static Brushes.Transparent} "
                        + "on a binding; Transparent looks the same over the list."),
                    _ => Verdict.Undecided($"The ControlTheme sets the Background of {them} at {value.Where} to {value.Shown}, {why}."),
                };
            }

            if (end == ChainEnd.Framework)
            {
                return Verdict.Draws;
            }

            // What the row's type registers as its default, which its static constructor can override.
            return _types.PaintsByDefault(container) switch
            {
                true => Verdict.Draws,
                null => Verdict.Undecided(
                    $"The ControlTheme at {containerTheme.Where} sets no Background on {them} and is BasedOn nothing, so their "
                    + $"Background keeps the default {container.Name} registers, which could not be read."),
                false => Null(rows, drawer,
                    $"the ControlTheme at {containerTheme.Where} sets none and is BasedOn nothing, so it keeps its default, which is null",
                    string.Empty,
                    "Set Background to Transparent in the theme, or base it on the framework's own theme for the row."),
            };
        }

        /// <summary>
        /// A finding for rows whose <c>Background</c> can be null, as <paramref name="said"/> sets it, where
        /// <paramref name="drawer"/> in their template draws it.
        /// </summary>
        private static Verdict Null(string rows, string drawer, string said, string why, string fix) =>
            Verdict.DrawsNothing(
                $"{rows} can have a null Background where their template draws it: {drawer} draws the row's Background, "
                + $"and {said}{(why.Length == 0 ? string.Empty : ", " + why)}. Where it is null, that draws nothing and takes "
                + $"no click, so a click or a hover there reaches a row only where something else in it draws. {fix}");

        /// <summary>
        /// Whether a value paints. A brush does, and so, taken as a brush, does a resource or a static
        /// member; <c>{x:Null}</c> does not, nor a binding with no non-null <c>TargetNullValue</c>.
        /// </summary>
        private static (Paint Paint, string Why) Read(Setting value)
        {
            if (value.Element is { } element)
            {
                return element.Name.LocalName switch
                {
                    "Null" => (Paint.Null, string.Empty),
                    "Binding" or "CompiledBinding" or "ReflectionBinding" or "MultiBinding" =>
                        OfBinding(ElementArgument(element, "TargetNullValue")),
                    "TemplateBinding" => (Paint.Unknown, "a TemplateBinding, which this rule does not follow"),
                    _ => (Paint.Draws, string.Empty),
                };
            }

            string text = value.Text?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                return (Paint.Unknown, "an empty value, which this rule cannot evaluate");
            }

            if (ThemeReader.IsNull(text))
            {
                return (Paint.Null, string.Empty);
            }

            if (MarkupExtension.Parse(text) is not { } extension)
            {
                // A colour or a brush by name.
                return (Paint.Draws, string.Empty);
            }

            return LocalPart(extension.Name) switch
            {
                "Binding" or "CompiledBinding" or "ReflectionBinding" => OfBinding(extension.Argument("TargetNullValue")),
                "TemplateBinding" => (Paint.Unknown, "a TemplateBinding, which this rule does not follow"),
                _ => (Paint.Draws, string.Empty),
            };
        }

        /// <summary>
        /// Whether a value is a binding, which an element takes as a local value even in a template: the XAML
        /// compiler gives what a template's markup sets <c>BindingPriority.Template</c>, and drops it for a
        /// binding, which keeps its own, by <c>XamlIlAvaloniaPropertyHelper</c> at 12.1.3. A
        /// <c>TemplateBinding</c> is the template's.
        /// </summary>
        private static bool IsBinding(Setting? value) =>
            value?.Element?.Name.LocalName is "Binding" or "CompiledBinding" or "ReflectionBinding" or "MultiBinding"
            || (value?.Text is { } text && MarkupExtension.Parse(text) is { } extension
                && LocalPart(extension.Name) is "Binding" or "CompiledBinding" or "ReflectionBinding");

        /// <summary>A binding paints only when its <c>TargetNullValue</c>, as written, is present and not null.</summary>
        private static (Paint Paint, string Why) OfBinding(string? targetNullValue)
        {
            if (targetNullValue is null)
            {
                return (Paint.Null, "a binding with no TargetNullValue, which answers null wherever its source or its converter does");
            }

            string written = targetNullValue.Trim().Trim('\'').Trim();
            return written.Length == 0 || ThemeReader.IsNull(written)
                ? (Paint.Null, "a binding whose TargetNullValue is itself null or empty")
                : (Paint.Draws, string.Empty);
        }

        /// <summary>
        /// An argument of a binding written as an element, as its attribute or as a property element; a
        /// property element that holds an element reads as that element's name, and <c>x:Null</c> as null.
        /// </summary>
        private static string? ElementArgument(XElement element, string name)
        {
            if (element.Attribute(name)?.Value is { } attribute)
            {
                return attribute;
            }

            if (element.Elements().FirstOrDefault(child => child.Name.LocalName == element.Name.LocalName + "." + name) is { } property)
            {
                return property.Elements().FirstOrDefault() is { } held
                    ? held.Name.LocalName == "Null" ? "{x:Null}" : held.Name.LocalName
                    : property.Value;
            }

            return null;
        }

        /// <summary>
        /// Whether the <c>Background</c> a template gives <paramref name="element"/> is the row's own: it takes
        /// the <c>Background</c> of <paramref name="templatedParent"/>, the control whose template it is, as
        /// <see cref="RowsProperty"/> reads it, written alone or after an owner that names that control's own.
        /// </summary>
        private bool TakesTheRowsBackground(Setting value, XElement element, Type templatedParent) =>
            RowsProperty(value, ElementTypes.NamesOf(templatedParent), (type, visual) => NearerAncestors(element, type, visual),
                written => _themes.OwnershipOf(ResourceScope.OwnerOf(written, value.Element ?? value.At), ResourceScope.PropertyName(written),
                    templatedParent.Name, null, templatedParent, local: false) == Ownership.Own)
            == "Background";

        /// <summary>
        /// The <c>ContentPresenter</c> or <c>TransitioningContentControl</c> in a template that holds
        /// <paramref name="element"/>, at any depth, as its content written in markup, as a child or in its
        /// <c>Content</c> property element; else <c>null</c>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>What a presenter in a template holds as markup content is not where the template writes it.</b>
        /// Measured on Avalonia 12.1.3 under Fluent, Simple and Semi, a <c>Border</c> written inside a
        /// <c>ContentPresenter</c> in a row's template had no templated parent, so a <c>TemplateBinding</c> or a
        /// binding to the templated parent there took nothing, and inside one named <c>PART_ContentPresenter</c>
        /// its logical parent was the row itself, past the presenter and what holds it, so
        /// <c>$parent[Control;1]</c> reached the list. The template sets its templated parent down the logical
        /// tree as it is built, before the presenter makes its content a logical child, of the control it
        /// presents for when that control takes it as its own, by <c>TemplatedControl.ApplyTemplatedParent</c>
        /// and <c>ContentPresenter.UpdateChild</c> at 12.1.3. A <c>TransitioningContentControl</c>'s content
        /// takes none either: unlike every other <c>ContentControl</c>, it does not add its content to its
        /// logical children when it is set, but hands it to a presenter of its own template, as its
        /// <c>OnPropertyChanged</c> returns before <c>ContentControl</c>'s runs, by their source at 12.1.3.
        /// </remarks>
        private XElement? PresenterHolding(XElement element)
        {
            XElement held = element;
            foreach (XElement ancestor in element.Ancestors())
            {
                string name = ancestor.Name.LocalName;
                if (name == "ControlTemplate")
                {
                    return null;
                }

                if (!name.Contains('.', StringComparison.Ordinal)
                    && IdentityOf(ancestor).Chain.Any(type => type is "ContentPresenter" or "TransitioningContentControl")
                    && (!held.Name.LocalName.Contains('.', StringComparison.Ordinal) || ResourceScope.PropertyName(held.Name.LocalName) == "Content"))
                {
                    return ancestor;
                }

                held = ancestor;
            }

            return null;
        }

        /// <summary>
        /// How many of <paramref name="element"/>'s ancestors in its template, nearer than the control whose
        /// template it is, are a <paramref name="type"/>: of that type or of one derived from it, as Avalonia
        /// matches an ancestor; <c>null</c> when markup cannot say of one, since its type does not resolve, it
        /// is a template other than the control's, or, in the visual tree, it is a templated control, whose
        /// own template lies between them.
        /// </summary>
        /// <remarks>
        /// ⚠ The logical ancestors of an element in a template are those its markup writes around it, up to
        /// the template's root, whose logical parent is the templated control. A templated control's content
        /// is its logical child, and in the visual tree its own template stands between them. So may a
        /// visual of an element's own, with no template, as a <c>Viewbox</c> puts one between itself and its
        /// child, by its source at 12.1.3; so in the visual tree a type every element is, such as
        /// <c>Control</c>, is not counted at all.
        /// </remarks>
        private int? NearerAncestors(XElement element, string type, bool visual)
        {
            if (visual && EveryElementMayBe(type))
            {
                return null;
            }

            int count = 0;
            foreach (XElement ancestor in element.Ancestors())
            {
                string name = ancestor.Name.LocalName;
                if (name == "ControlTemplate")
                {
                    return count;
                }

                if (name.Contains('.', StringComparison.Ordinal))
                {
                    continue;
                }

                Identity id = IdentityOf(ancestor);
                if (name.EndsWith("Template", StringComparison.Ordinal) || id.Type is null || (visual && ElementTypes.IsTemplated(id.Type)))
                {
                    return null;
                }

                if (id.Chain.Contains(type, StringComparer.Ordinal))
                {
                    count++;
                }
            }

            return null;
        }

        /// <summary>
        /// How an element paints that takes the templated control's <c>Background</c> through
        /// <paramref name="value"/>: as the control's, when the binding hands on what the control has; with a
        /// brush of its own, when its <c>TargetNullValue</c> is not null, which stands in for a null; else not
        /// decided, with how the binding changes the value, when it passes it through anything else, a
        /// converter above all.
        /// </summary>
        /// <remarks>
        /// ⚠ Measured on Avalonia 12.1.3: rows whose <c>Background</c> was null took the click across their
        /// width where their template's <c>Border</c> bound it with a <c>TargetNullValue</c> of
        /// <c>Transparent</c>, through <c>TemplatedParent</c> or <c>$parent[ListBoxItem]</c>, and where it took
        /// it by a <c>TemplateBinding</c> through a converter that turned null into <c>Transparent</c>.
        /// </remarks>
        private static (ElementPaint Paint, string? Changed) ThroughTheBinding(Setting value)
        {
            (string Name, string? Value)[] arguments = [.. BindingArguments(value)];
            if (arguments.Any(argument => argument.Name == "TargetNullValue" && OfBinding(argument.Value).Paint == Paint.Draws))
            {
                return (ElementPaint.OwnBrush, null);
            }

            return arguments.FirstOrDefault(argument => !HandsOn(argument)) is { Name: { } changes }
                ? (ElementPaint.Unknown, $"through a binding with {changes}, which this rule does not evaluate")
                : (ElementPaint.TakesTheRows, null);
        }

        /// <summary>
        /// A binding's named arguments: an element's attributes and property elements, or an extension's
        /// <c>name=value</c> arguments.
        /// </summary>
        private static IEnumerable<(string Name, string? Value)> BindingArguments(Setting value)
        {
            if (value.Element is { } element)
            {
                string prefix = element.Name.LocalName + ".";
                return element.Attributes()
                    .Where(attribute => !attribute.IsNamespaceDeclaration)
                    .Select(attribute => (attribute.Name.LocalName, (string?)attribute.Value))
                    .Concat(element.Elements()
                        .Where(child => child.Name.LocalName.StartsWith(prefix, StringComparison.Ordinal))
                        .Select(child => (child.Name.LocalName[prefix.Length..], ElementArgument(element, child.Name.LocalName[prefix.Length..]))));
            }

            return value.Text is { } text && MarkupExtension.Parse(text) is { } extension
                ? extension.Named().Select(argument => (argument.Name, (string?)argument.Value))
                : [];
        }

        /// <summary>
        /// Whether a binding argument hands on the source's value as it is: it names the path or the source,
        /// a fallback for a failed binding, a mode that reads the source, or how the binding writes back.
        /// </summary>
        private static bool HandsOn((string Name, string? Value) argument) =>
            argument.Name switch
            {
                "Path" or "Property" or "RelativeSource" or "FallbackValue" or "TargetNullValue" or "UpdateSourceTrigger" or "Delay"
                    or "DataType" or "ConverterParameter" or "ConverterCulture" => true,
                "Mode" => argument.Value?.Trim() is not "OneWayToSource",
                _ => false,
            };

        /// <summary>Whether <paramref name="type"/> is one a <c>Border</c> is, so that any element may be one; so it is when no <c>Border</c> resolves to say.</summary>
        private bool EveryElementMayBe(string type) =>
            _types.Resolve("Border").Type is not { } border || ElementTypes.NamesOf(border).Contains(type, StringComparer.Ordinal);

        /// <summary>
        /// The row's property a value takes: by a <c>TemplateBinding</c>, or a binding relative to the
        /// templated parent, or to the ancestor whose type is in the row's <paramref name="chain"/> that the
        /// row is, however Avalonia writes it; <c>null</c> when it takes none. <paramref name="nearer"/> counts
        /// the ancestors of a type that come before the row, in the visual tree or the logical one, and
        /// <paramref name="holds"/> says whether a property written after an owner, as written, is the row's own.
        /// </summary>
        private static string? RowsProperty(
            Setting value, IReadOnlyCollection<string> chain, Func<string, bool, int?> nearer, Func<string, bool> holds)
        {
            if (value.Element is { } element)
            {
                return element.Name.LocalName switch
                {
                    "TemplateBinding" => PropertyNamed(element.Attribute("Property")?.Value, holds),
                    "Binding" or "CompiledBinding" or "ReflectionBinding" =>
                        RowsPath(element.Attribute("Path")?.Value, element.Attribute("RelativeSource")?.Value, chain, nearer, holds),
                    _ => null,
                };
            }

            if (value.Text is not { } text || MarkupExtension.Parse(text) is not { } extension)
            {
                return null;
            }

            return LocalPart(extension.Name) switch
            {
                "TemplateBinding" => PropertyNamed(extension.Positional(0) ?? extension.Argument("Property"), holds),
                "Binding" or "CompiledBinding" or "ReflectionBinding" =>
                    RowsPath(extension.Positional(0) ?? extension.Argument("Path"), extension.Argument("RelativeSource"), chain, nearer, holds),
                _ => null,
            };
        }

        /// <summary>
        /// The row's property a binding's path and relative source name: the path, relative to the
        /// templated parent or to the ancestor of a type in <paramref name="chain"/> that the row is, or
        /// <c>P</c> in <c>$parent[T].P</c> or <c>$parent[T;N].P</c> with <c>T</c> in <paramref name="chain"/>,
        /// where the row is that ancestor; <c>null</c> when they name none.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A binding to an ancestor counts the ancestors of its type from the nearest.</b> At 12.1.3,
        /// <c>$parent[T;N]</c> takes the logical ancestor at index N, from 0, among those a <c>T</c> or derived
        /// from one, by <c>BindingExpressionGrammar</c> and <c>ControlLocator</c>, compiled or not; and a
        /// <c>RelativeSource</c> with an <c>AncestorType</c> takes the one at <c>AncestorLevel</c>, from 1, in
        /// its <c>Tree</c>, the visual one unless it says otherwise, by <c>ExpressionNodeFactory</c> and
        /// <c>VisualLocator</c>. Measured, <c>$parent[ListBoxItem;1]</c> and <c>AncestorLevel=2</c> in a row's
        /// template drew an outer row's background, or none. So the row is the one at the index past every
        /// ancestor of that type that <paramref name="nearer"/> counts before it.
        /// </remarks>
        private static string? RowsPath(
            string? path, string? relativeSource, IReadOnlyCollection<string> chain, Func<string, bool, int?> nearer, Func<string, bool> holds)
        {
            string written = path?.Trim() ?? string.Empty;
            if (written.StartsWith("$parent[", StringComparison.Ordinal))
            {
                int close = written.IndexOf(']', StringComparison.Ordinal);
                if (close <= 0 || written.Length <= close + 2 || written[close + 1] != '.')
                {
                    return null;
                }

                string[] arguments = written[8..close].Split(';');
                string type = LocalPart(arguments[0].Trim());
                int level = 0;
                if (arguments.Length > 2
                    || (arguments.Length == 2 && !int.TryParse(arguments[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out level)))
                {
                    return null;
                }

                return chain.Contains(type, StringComparer.Ordinal) && nearer(type, false) == level ? PathProperty(written[(close + 2)..], holds) : null;
            }

            if (written.Length == 0 || relativeSource is null || MarkupExtension.Parse(relativeSource) is not { } source)
            {
                return null;
            }

            // The mode, written first or by name: the templated parent is the row, and Self or DataContext
            // is not; an AncestorType counts only for FindAncestor, which it is with no mode written.
            string? mode = (source.Positional(0) ?? source.Argument("Mode"))?.Trim();
            if (mode == "TemplatedParent")
            {
                return PathProperty(written, holds);
            }

            if (mode is not (null or "FindAncestor"))
            {
                return null;
            }

            string? ancestor = source.Argument("AncestorType");
            if (ancestor is not null && MarkupExtension.Parse(ancestor) is { } typeExtension)
            {
                ancestor = typeExtension.Positional(0) ?? typeExtension.Argument("TypeName");
            }

            if (ancestor is null || !chain.Contains(LocalPart(ancestor.Trim()), StringComparer.Ordinal))
            {
                return null;
            }

            int ancestorLevel = 1;
            if (source.Argument("AncestorLevel") is { } levelWritten
                && !int.TryParse(levelWritten.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ancestorLevel))
            {
                return null;
            }

            bool visual = !string.Equals(source.Argument("Tree")?.Trim(), "Logical", StringComparison.OrdinalIgnoreCase);
            return nearer(LocalPart(ancestor.Trim()), visual) == ancestorLevel - 1 ? PathProperty(written, holds) : null;
        }

        /// <summary>
        /// The property a <c>TemplateBinding</c> names, in parentheses or not, written alone or after an owner that
        /// <paramref name="holds"/> says names the row's own; <c>null</c> when it names none of the row's.
        /// </summary>
        /// <remarks>
        /// ⚠ An owner names the property its field holds: <c>Border</c> registers <c>BackgroundProperty</c>, and a
        /// templated control, a panel, a <c>ContentPresenter</c> and a <c>TextBlock</c> each add themselves as its
        /// owners, by their source at 12.1.3, so <c>{TemplateBinding Border.Background}</c> takes a row's
        /// <c>Background</c>, and one naming another owner's attached <c>Background</c> does not.
        /// </remarks>
        private static string? PropertyNamed(string? written, Func<string, bool> holds) =>
            written is null ? null : Held(written.Trim().Trim('(', ')'), holds);

        /// <summary>
        /// The property a binding path names where it is one property written in parentheses after an owner, as
        /// <c>(Border.Background)</c>, when <paramref name="holds"/> says that owner names the row's own, else
        /// <c>null</c>; else the path as written, which names the row's own property, or a property of a property,
        /// which no row's <c>Background</c> is.
        /// </summary>
        private static string? PathProperty(string path, Func<string, bool> holds)
        {
            string written = path.Trim();
            return written.Length > 2 && written[0] == '(' && written.IndexOf(')', StringComparison.Ordinal) == written.Length - 1
                ? Held(written[1..^1], holds)
                : written;
        }

        /// <summary>The property <paramref name="written"/> names, without its owner, where it has none or <paramref name="holds"/> takes it; else <c>null</c>.</summary>
        private static string? Held(string written, Func<string, bool> holds) =>
            !written.Contains('.', StringComparison.Ordinal) || holds(written) ? ResourceScope.PropertyName(written) : null;

        /// <summary>
        /// The reason to stop when a top-level style sets one of <paramref name="properties"/> on one of
        /// <paramref name="names"/>, or <c>null</c>.
        /// </summary>
        private string? Styled(IReadOnlyCollection<string> names, string subject, params string[] properties) =>
            _scope.StylesSetting(names, properties) is [{ } style, ..]
                ? $"A Style at {style.Where} sets {style.Property} on a selector naming {subject}, and styles are not "
                  + "evaluated, so what draws the rows' background is not decided from markup."
                : null;

        private static string LocalPart(string name) => name[(name.LastIndexOf(':') + 1)..];

        /// <summary>A type name with its indefinite article: "a Border", "an Image", "a UserControl".</summary>
        /// <remarks>
        /// A <c>U</c> read as "you" takes "a": before a capital, as in <c>UIElement</c>, or before a
        /// consonant and a vowel, as in <c>UserControl</c>; <c>Underline</c> keeps "an".
        /// </remarks>
        private static string A(string name)
        {
            static bool Vowel(char letter) => "AEIOU".Contains(char.ToUpperInvariant(letter), StringComparison.Ordinal);

            bool an = name.Length > 0 && Vowel(name[0])
                      && !(name[0] == 'U' && name.Length > 2 && (char.IsUpper(name[1]) || (!Vowel(name[1]) && Vowel(name[2]))));
            return (an ? "an " : "a ") + name;
        }
    }
}
