namespace Bennewitz.Ninja.XamlQuality.Rules;

/// <summary>
/// What <see cref="TemplatePartRule"/> reads: each control's template parts, the code that looks each up, and what
/// each of its themes declares.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The rule's own reading, not a second one.</b> <see cref="TemplatePartRule.Analyze"/> derives its findings
/// from it: for each theme of a control, the constants and looked-up parts that the theme does not declare. A consumer
/// that read the compiled code again would read less than the rule does, or differently, and a reference built from
/// that reading would drift from what the rule checks.
/// </para>
/// <para>
/// ⚠ <b>A constant is not a lookup.</b> A part a control only declares as a constant, which a style selector may be
/// all that needs, is in <see cref="ControlTemplateParts.Constants"/> and in no lookup. The rule checks both against
/// each theme, so a consumer that wants only the parts some code asks for reads
/// <see cref="ControlTemplateParts.Lookups"/>.
/// </para>
/// <para>
/// ⚠ <b>Ordered, so two readings compare as text</b>: controls by name, constants by name, and lookups by part, then
/// type, then method, all ordinal; themes by file, then line; a theme's declared parts in the order it declares them;
/// and what was not read by subject, then reason.
/// </para>
/// </remarks>
/// <param name="Controls">Every type the rule credits a part to, by name.</param>
/// <param name="Skipped">
/// What could not be read, and why: a type whose code could be read only in part, a type a scanned assembly defines that
/// did not load, and, when the scan was given no assemblies, that.
/// </param>
public sealed record TemplatePartReading(IReadOnlyList<ControlTemplateParts> Controls, IReadOnlyList<XamlSkip> Skipped);

/// <summary>One type's template parts: the constants it declares, the lookups made on its template, and its themes.</summary>
/// <remarks>
/// ⚠ <b>Named as the rule names it</b>: by its simple name, the one a <c>ControlTheme</c>'s <c>TargetType</c> gives.
/// Two scanned types of one simple name are one entry, as they are one control to the rule. An entry is usually a
/// control; it is a helper where no control hands it the template its lookups are made on, since the rule then keeps
/// those lookups as the helper's own, and names it in a skip when no theme targets it.
/// </remarks>
/// <param name="Name">The type's simple name.</param>
/// <param name="Constants">Its <c>PART_</c> string constants, public or not, whether or not anything looks them up.</param>
/// <param name="Lookups">Each part looked up on its template, with the code that looks it up.</param>
/// <param name="Themes">Each <c>ControlTheme</c> in the scanned markup that targets it.</param>
public sealed record ControlTemplateParts(
    string Name,
    IReadOnlyList<string> Constants,
    IReadOnlyList<TemplatePartLookup> Lookups,
    IReadOnlyList<ThemeParts> Themes);

/// <summary>One part looked up on a control's template, and the code that looks it up.</summary>
/// <param name="Part">The part's name, <c>PART_</c> included.</param>
/// <param name="Origin">Whose code makes the lookup: the control's own, a base class's, or a helper's.</param>
/// <param name="Type">
/// The full name of the type whose code makes the lookup; for a lambda, a local function or an iterator, which the
/// compiler nests in a type of its own, the type that declares it. A type nested in another is named as the runtime
/// names it, <c>Outer+Inner</c>.
/// </param>
/// <param name="Method">
/// The method that makes it, by its compiled name, so a lambda's is the compiler's, such as
/// <c>&lt;OnApplyTemplate&gt;b__12_0</c>, and a constructor's is <c>.ctor</c>.
/// </param>
/// <param name="Assembly">The simple name of the assembly that holds <paramref name="Type"/>.</param>
public sealed record TemplatePartLookup(string Part, TemplatePartOrigin Origin, string Type, string Method, string Assembly);

/// <summary>Whose code makes a lookup, as seen from the control whose template it is made on.</summary>
public enum TemplatePartOrigin
{
    /// <summary>
    /// The control's own code, a lambda or local function it declares included; or code that no control hands its
    /// template to, whose lookups the rule keeps as its own type's, and a nested type's as the outermost type's.
    /// </summary>
    Own,

    /// <summary>
    /// A base class of the control, in whichever scanned assembly: its <c>OnApplyTemplate</c> runs on the control's
    /// template, and so does what the control hands the template to there.
    /// </summary>
    BaseClass,

    /// <summary>
    /// Another type, which the control hands its template, or the template's name scope, to: a type the control or a
    /// base class nests included, since it is a type of its own.
    /// </summary>
    Helper,
}

/// <summary>The parts one <c>ControlTheme</c> declares.</summary>
/// <param name="File">
/// The markup file that holds the theme, relative to the scan root and forward-slashed, as
/// <see cref="XamlFile.RelativePath"/> is.
/// </param>
/// <param name="Line">The 1-based line the theme starts on, or <c>null</c>.</param>
/// <param name="Declared">Each element inside the theme that is named as a part.</param>
public sealed record ThemeParts(string File, int? Line, IReadOnlyList<DeclaredPart> Declared);

/// <summary>A part a theme declares: an element inside it named <c>PART_…</c>, in either naming spelling.</summary>
/// <param name="Name">The part's name, as the element's <c>Name</c> or <c>x:Name</c> gives it.</param>
/// <param name="ElementType">The element's local name, such as <c>ScrollViewer</c>, without its prefix.</param>
/// <param name="AutomationId">
/// The automation id the element declares, in either spelling of the attached property or MAUI's plain
/// <c>AutomationId</c>: an attribute's value as written, or a property element's text, or, where it holds an object
/// such as a binding, that object's type in braces, <c>{Binding}</c>. A blank attribute's value is kept, since on
/// Avalonia it hides the id the framework would derive. <c>null</c> when it declares none.
/// </param>
public sealed record DeclaredPart(string Name, string ElementType, string? AutomationId);
