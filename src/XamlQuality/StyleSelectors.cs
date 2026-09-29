using System.Text;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality;

/// <summary>How far a style's selector picks a type's controls.</summary>
internal enum Pick
{
    /// <summary>It does not name the type.</summary>
    No,

    /// <summary>One of its alternatives is the type alone, so it applies to every one of them.</summary>
    Every,

    /// <summary>It names the type with a state, a class, a name or a context, so it may apply to some.</summary>
    Some,
}

/// <summary>How far a style picks the rows an items control generates, in the state rows start in.</summary>
internal enum RowsPick
{
    /// <summary>No row: it asks for a class, a name or a state, or names a type no row is.</summary>
    None,

    /// <summary>The rows the data picks, row by row, from what the rows' theme binds to each row's item, or the rows that start selected.</summary>
    Some,

    /// <summary>Every row, whatever the data.</summary>
    All,

    /// <summary>Markup cannot say whether it picks every row.</summary>
    Unknown,
}

/// <summary>What a style's selector picks, read the way Avalonia matches one, as far as markup shows it.</summary>
/// <remarks>
/// ⭐ <b>Shared because two rules reading one selector is how their answers drift.</b> <c>BNXQ1009</c>
/// asks whether a style names every row an items control generates, and <c>BNXQ1010</c> whether it
/// paints every row. <see cref="Picks"/> began as <c>BNXQ1009</c>'s own reading and moved here
/// unchanged.
/// </remarks>
internal static class StyleSelectors
{
    private const string Template = "/template/";

    /// <summary>
    /// The pseudo-classes a control takes only while someone points at it, presses it, selects it or
    /// focuses it, or while it is disabled: none of them holds in the state a control starts in, but
    /// <c>disabled</c> where markup may disable the control or what holds it, and <c>selected</c> where
    /// markup may select a row from the start.
    /// </summary>
    private static readonly HashSet<string> States = new(StringComparer.Ordinal)
    {
        "disabled", "focus", "focus-visible", "focus-within", "pointerover", "pressed", "selected",
    };

    /// <summary>The states a control starts out of: <see cref="States"/>, but not those in <paramref name="startStates"/>, which it may start in.</summary>
    private static HashSet<string> StatesOutOf(IReadOnlySet<string> startStates) =>
        startStates.Count == 0 ? States : new HashSet<string>(States.Where(state => !startStates.Contains(state)), StringComparer.Ordinal);

    /// <summary>
    /// How far a style picks containers whose style key is <paramref name="styleKey"/> and whose type
    /// chain is <paramref name="chain"/>. A type selector matches the style key exactly, as Avalonia's
    /// does, and <c>:is()</c> matches any type in the chain; a nested style's <c>^</c> is its parent's.
    /// </summary>
    internal static Pick Picks(XElement style, string styleKey, IReadOnlyCollection<string> chain)
    {
        if (style.Attribute("Selector")?.Value is not { } selector)
        {
            return Pick.No;
        }

        Pick pick = Pick.No;
        foreach (string alternative in AtDepthZero(selector, ','))
        {
            List<string> compounds = [.. Compounds(alternative)];
            if (compounds.Count == 0)
            {
                continue;
            }

            string last = compounds[^1];
            bool picks;
            bool alone;
            if (last.StartsWith('^'))
            {
                picks = style.Parent is { Name.LocalName: "Style" } parent && Picks(parent, styleKey, chain) != Pick.No;
                alone = false;
            }
            else
            {
                (string? type, bool isIs, string qualifiers) = Split(last);
                picks = type is not null && (isIs ? chain.Contains(type, StringComparer.Ordinal) : type == styleKey);
                alone = qualifiers.Length == 0 && compounds.Count == 1;
            }

            if (picks)
            {
                if (alone)
                {
                    return Pick.Every;
                }

                pick = Pick.Some;
            }
        }

        return pick;
    }

    /// <summary>
    /// How far a style picks a part of the template of the controls <see cref="Picks"/> asks about:
    /// <see cref="Pick.Every"/> when an alternative is the type alone, then <c>/template/</c> and the
    /// part, and <see cref="Pick.Some"/> when the type is qualified or has a context. The
    /// <c>/template/</c> combinator is read as Avalonia's own themes write it, with a space either side.
    /// </summary>
    internal static Pick PicksTemplateOf(XElement style, string styleKey, IReadOnlyCollection<string> chain)
    {
        if (style.Attribute("Selector")?.Value is not { } selector)
        {
            return Pick.No;
        }

        Pick pick = Pick.No;
        foreach (string alternative in AtDepthZero(selector, ','))
        {
            List<string> parts = [.. Compounds(alternative)];
            int template = parts.LastIndexOf(Template);
            if (template < 1 || template == parts.Count - 1)
            {
                continue;
            }

            // The control whose template the part is in.
            string owner = parts[template - 1];
            bool picks;
            bool alone;
            if (owner.StartsWith('^'))
            {
                Pick parent = style.Parent is { Name.LocalName: "Style" } holder ? Picks(holder, styleKey, chain) : Pick.No;
                picks = parent != Pick.No;
                alone = owner == "^" && template == 1 && parent == Pick.Every;
            }
            else
            {
                (string? type, bool isIs, string qualifiers) = Split(owner);
                picks = type is not null && (isIs ? chain.Contains(type, StringComparer.Ordinal) : type == styleKey);
                alone = qualifiers.Length == 0 && template == 1;
            }

            if (picks)
            {
                if (alone)
                {
                    return Pick.Every;
                }

                pick = Pick.Some;
            }
        }

        return pick;
    }

    /// <summary>
    /// Whether a style applies only while what it styles is in one of the states it starts out of: every
    /// alternative of its selector asks for one, on the control it styles or on the control whose
    /// template holds it, or a style it is nested in does. <paramref name="startStates"/> are the states
    /// that control may start in, such as <c>disabled</c>, which are then not among them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>A state asked of a context does not count.</b> <c>ListBox:pointerover ListBoxItem</c>
    /// applies to a row in its first state whenever the list is pointed at, so only the compound a
    /// style applies to, and the one whose template it reaches into, are read. A state inside
    /// <c>:not()</c> is the opposite of asking for one, and does not count either.
    /// </para>
    /// <para>
    /// ⚠ <b>A control starts disabled where markup disables it or what holds it.</b> Measured on
    /// Avalonia 12.1.3 under Fluent, Simple and Semi: the rows of a list whose <c>IsEnabled</c> was
    /// <c>False</c>, and rows whose theme set it so, took a <c>^:disabled</c> style's background from the
    /// start.
    /// </para>
    /// </remarks>
    internal static bool RequiresState(XElement style, IReadOnlySet<string> startStates)
    {
        HashSet<string> states = StatesOutOf(startStates);
        if (style.Attribute("Selector")?.Value is { } selector
            && AtDepthZero(selector, ',').All(alternative => AsksForState(alternative, states)))
        {
            return true;
        }

        return style.Parent is { Name.LocalName: "Style" } parent && RequiresState(parent, startStates);
    }

    /// <summary>
    /// Whether a style applies under a condition, so that what it sets ranks as a style trigger: some
    /// alternative of its selector, or of a style it is nested in, asks for a class, a pseudo-class, a
    /// property, <c>:not()</c> or <c>:nth-child()</c>, on what it styles or on a context. A type,
    /// <c>:is()</c> and a name are not conditions.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A style trigger outranks what a template's markup gives an element, and a plain style does
    /// not.</b> By <c>BindingPriority</c> at 12.1.3, <c>StyleTrigger</c> ranks above <c>Template</c> and
    /// <c>Style</c> below it, and <c>StyleInstance</c> takes <c>StyleTrigger</c> for a style whose selector
    /// has an activator, which a type, an <c>:is()</c> and a name do not make.
    /// </remarks>
    internal static bool IsConditional(XElement style)
    {
        if (style.Attribute("Selector")?.Value is { } selector
            && AtDepthZero(selector, ',').SelectMany(Compounds).Any(HasCondition))
        {
            return true;
        }

        return style.Parent is { Name.LocalName: "Style" } parent && IsConditional(parent);
    }

    /// <summary>Whether a compound selector asks for a class, a pseudo-class other than <c>:is()</c>, or a property.</summary>
    private static bool HasCondition(string compound)
    {
        for (int index = 0; index < compound.Length; index++)
        {
            switch (compound[index])
            {
                case '.' or '[':
                    return true;
                case ':' when !compound.AsSpan(index).StartsWith(":is(", StringComparison.Ordinal):
                    return true;
                case ':':
                    index = PastGroup(compound, index + 3, '(', ')') - 1;
                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a style may pick, in the state it starts in, an element whose style key is
    /// <paramref name="styleKey"/> and whose type chain is <paramref name="chain"/>, carrying
    /// <paramref name="classes"/> and named <paramref name="name"/>: an alternative's subject agrees with
    /// its style key, its classes and its name, and asks for no state the element starts out of, which
    /// those in <paramref name="startStates"/> are not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>A type selector matches the style key, as <see cref="Picks"/> does</b>, so a control whose
    /// <c>StyleKeyOverride</c> borrows <c>Border</c>'s is picked by <c>Border</c> and not by its own name,
    /// by Avalonia's <c>TypeNameAndClassSelector</c> at 12.1.3; measured, a <c>Border</c> style painted
    /// such a control. <c>:is()</c> matches any type in the chain.
    /// </para>
    /// <para>
    /// ⚠ <b>Taken to hold when markup cannot say.</b> A context, a property selector, a pseudo-class that
    /// is not a state and <c>:not()</c> may each hold or not, so they are taken to. An alternative that
    /// reaches into a template picks the parts of that control's template, and is left to
    /// <see cref="PicksTemplateOf"/>.
    /// </para>
    /// </remarks>
    internal static bool MayPick(
        XElement style, string styleKey, IReadOnlyCollection<string> chain, IReadOnlyCollection<string> classes, string? name, IReadOnlySet<string> startStates)
    {
        if (style.Attribute("Selector")?.Value is not { } selector)
        {
            return false;
        }

        foreach (string alternative in AtDepthZero(selector, ','))
        {
            List<string> parts = [.. Compounds(alternative)];
            if (parts.Count == 0 || parts.Contains(Template))
            {
                continue;
            }

            string subject = parts[^1];
            string qualifiers;
            if (subject.StartsWith('^'))
            {
                if (style.Parent is not { Name.LocalName: "Style" } parent || !MayPick(parent, styleKey, chain, classes, name, startStates))
                {
                    continue;
                }

                qualifiers = subject[1..];
            }
            else
            {
                (string? subjectType, bool isIs, string rest) = Split(subject);
                if (subjectType is not null && !(isIs ? chain.Contains(subjectType, StringComparer.Ordinal) : subjectType == styleKey))
                {
                    continue;
                }

                qualifiers = rest;
            }

            if (MayHold(qualifiers, classes, name, StatesOutOf(startStates)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// How far a style picks, in the state rows start in, the rows whose style key is
    /// <paramref name="styleKey"/> and whose type chain is <paramref name="chain"/>, generated by a host whose
    /// style key is <paramref name="hostKey"/> and whose type chain is <paramref name="hostChain"/>.
    /// <paramref name="boundPerItem"/> says whether the rows' theme binds a property, named without its
    /// owner, to each row's item, and <paramref name="startStates"/> are the states the rows may start in,
    /// <c>disabled</c> or <c>selected</c>. A style nested in a <c>ControlTheme</c> takes its <c>^</c> as the
    /// rows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⭐ <b>A selector that holds of every row, whatever the data, picks them all.</b> Nothing in markup
    /// gives a generated row a class or a name, so a <c>:not()</c> of either holds of every row; and a
    /// row's logical parent is the list, so a context that is the host alone holds of every row through
    /// either combinator. Measured on Avalonia 12.1.3, <c>ListBox ListBoxItem</c>,
    /// <c>ListBox &gt; ListBoxItem</c> and <c>ListBoxItem:not(.nope)</c> each painted every row.
    /// </para>
    /// <para>
    /// ⚠ <b>A <c>:not()</c> of a state holds only while a row is out of that state.</b> A row pointed at,
    /// pressed, selected or focused leaves it, and a list whose <c>SelectionMode</c> is
    /// <c>AlwaysSelected</c> starts with a row selected, so such a selector is
    /// <see cref="RowsPick.Unknown"/>. Measured on Avalonia 12.1.3 under Fluent, Simple and Semi, rows whose
    /// own background was null and that <c>ListBoxItem:not(:pointerover)</c> made <c>Transparent</c> missed a
    /// click that followed a hover in up to four runs of ten. Rows that may start disabled may start in
    /// <c>:disabled</c>, so it too is <see cref="RowsPick.Unknown"/> for them, since a disabled list disables
    /// every row. Rows that may start selected start with the selected ones in <c>:selected</c>, which is
    /// <see cref="RowsPick.Some"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>Taken as picking only some rows, not proved:</b> a property selector on a property the rows'
    /// theme binds to each row's item, which the data decides row by row; measured, a style asking for one
    /// that every item made true painted every row. And <c>:selected</c> where rows may start selected,
    /// which a list of one row, or one that selects every row, makes every row. Any other property selector, a pseudo-class that is not
    /// one of the states, <c>:nth-child()</c>, a context that is not the host alone, and alternatives
    /// that may together cover every row are <see cref="RowsPick.Unknown"/>. An alternative that reaches
    /// into a template picks a part of the rows' template, not the rows.
    /// </para>
    /// </remarks>
    internal static RowsPick RowsPicked(
        XElement style, string styleKey, IReadOnlyCollection<string> chain, string hostKey, IReadOnlyCollection<string> hostChain,
        Func<string, bool> boundPerItem, IReadOnlySet<string> startStates)
    {
        if (style.Attribute("Selector")?.Value is not { } selector)
        {
            return RowsPick.None;
        }

        RowsPick picked = RowsPick.None;
        foreach (string alternative in AtDepthZero(selector, ','))
        {
            picked = Or(picked, AlternativePicks(style, alternative, styleKey, chain, hostKey, hostChain, boundPerItem, startStates));
        }

        return picked;
    }

    /// <summary>How far one alternative of a selector picks the rows, as <see cref="RowsPicked"/> reads it.</summary>
    private static RowsPick AlternativePicks(
        XElement style, string alternative, string styleKey, IReadOnlyCollection<string> chain, string hostKey, IReadOnlyCollection<string> hostChain,
        Func<string, bool> boundPerItem, IReadOnlySet<string> startStates)
    {
        List<(string Compound, bool Child)> steps = [.. Steps(alternative)];
        if (steps.Count == 0 || steps.Any(step => step.Compound == Template))
        {
            return RowsPick.None;
        }

        HashSet<string> states = StatesOutOf(startStates);
        (string subject, bool child) = steps[^1];
        RowsPick picked;
        if (subject.StartsWith('^'))
        {
            RowsPick parent = style.Parent switch
            {
                { Name.LocalName: "ControlTheme" } => RowsPick.All,
                { Name.LocalName: "Style" } holder => RowsPicked(holder, styleKey, chain, hostKey, hostChain, boundPerItem, startStates),
                _ => RowsPick.None,
            };
            picked = And(parent, QualifiersPick(subject[1..], styleKey, chain, boundPerItem, states));
        }
        else
        {
            (string? type, bool isIs, string qualifiers) = Split(subject);
            if (type is not null && !(isIs ? chain.Contains(type, StringComparer.Ordinal) : type == styleKey))
            {
                return RowsPick.None;
            }

            picked = QualifiersPick(qualifiers, styleKey, chain, boundPerItem, states);
        }

        if (steps.Count == 1 || picked == RowsPick.None)
        {
            return picked;
        }

        if (steps.Count > 2 || steps[0].Compound.StartsWith('^'))
        {
            return And(picked, RowsPick.Unknown);
        }

        // One context. The host alone is every row's logical parent, through either combinator; a type that
        // is not the host's is no row's parent, and any other context may hold of an ancestor or not.
        (string? contextType, bool contextIs, string contextQualifiers) = Split(steps[0].Compound);
        bool host = contextType is not null && (contextIs ? hostChain.Contains(contextType, StringComparer.Ordinal) : contextType == hostKey);
        return host && contextQualifiers.Length == 0 ? picked
            : !host && contextType is not null && child ? RowsPick.None
            : And(picked, RowsPick.Unknown);
    }

    /// <summary>How far a compound's qualifiers hold of every row in the state it starts in, out of <paramref name="states"/>.</summary>
    private static RowsPick QualifiersPick(
        string qualifiers, string styleKey, IReadOnlyCollection<string> chain, Func<string, bool> boundPerItem, HashSet<string> states)
    {
        RowsPick picked = RowsPick.All;
        int index = 0;
        while (index < qualifiers.Length && picked != RowsPick.None)
        {
            char character = qualifiers[index];
            if (character is '.' or '#')
            {
                // Nothing in markup gives a generated row a class or a name.
                return RowsPick.None;
            }

            if (character == '[')
            {
                int close = PastGroup(qualifiers, index, '[', ']');
                string asked = qualifiers[(index + 1)..Math.Max(index + 1, close - 1)];
                picked = And(picked, boundPerItem(PropertyAsked(asked)) ? RowsPick.Some : RowsPick.Unknown);
                index = close;
                continue;
            }

            if (character != ':')
            {
                picked = And(picked, RowsPick.Unknown);
                index++;
                continue;
            }

            int end = index + 1;
            while (end < qualifiers.Length && (char.IsLetterOrDigit(qualifiers[end]) || qualifiers[end] == '-'))
            {
                end++;
            }

            string token = qualifiers[(index + 1)..end];
            string? argument = null;
            int next = end;
            if (end < qualifiers.Length && qualifiers[end] == '(')
            {
                next = PastGroup(qualifiers, end, '(', ')');
                argument = qualifiers[(end + 1)..Math.Max(end + 1, next - 1)].Trim();
            }

            picked = And(picked, (token, argument) switch
            {
                // A row leaves the state it starts in, and a list may start with one selected.
                ("not", { } inner) when InState(inner, States) => RowsPick.Unknown,
                ("not", { } inner) => Not(InnerPick(inner, styleKey, chain, boundPerItem, states)),
                ("is", { } type) => chain.Contains(Unprefixed(type), StringComparer.Ordinal) ? RowsPick.All : RowsPick.None,
                _ when states.Contains(token) => RowsPick.None,

                // Rows that may start selected start with the selected ones selected, and no more.
                ("selected", null) => RowsPick.Some,
                _ => RowsPick.Unknown,
            });
            index = next;
        }

        return picked;
    }

    /// <summary>How far the selector inside a <c>:not()</c> holds of every row: one compound, with or without a type.</summary>
    private static RowsPick InnerPick(
        string inner, string styleKey, IReadOnlyCollection<string> chain, Func<string, bool> boundPerItem, HashSet<string> states)
    {
        if (inner.Length == 0 || Steps(inner).Count() != 1 || AtDepthZero(inner, ',').Count() != 1)
        {
            return RowsPick.Unknown;
        }

        (string? type, bool isIs, string qualifiers) = Split(inner);
        return type is not null && !(isIs ? chain.Contains(type, StringComparer.Ordinal) : type == styleKey)
            ? RowsPick.None
            : QualifiersPick(qualifiers, styleKey, chain, boundPerItem, states);
    }

    /// <summary>The property a property selector asks about, without its parentheses, its namespace and its owner.</summary>
    private static string PropertyAsked(string asked)
    {
        int depth = 0;
        int equals = asked.Length;
        for (int index = 0; index < asked.Length; index++)
        {
            depth += asked[index] switch { '(' => 1, ')' => -1, _ => 0 };
            if (depth == 0 && asked[index] is '=' or '!')
            {
                equals = index;
                break;
            }
        }

        return ResourceScope.PropertyName(Unprefixed(asked[..equals].Trim().Trim('(', ')').Trim()));
    }

    private static RowsPick And(RowsPick first, RowsPick second) =>
        first == RowsPick.None || second == RowsPick.None ? RowsPick.None
        : first == RowsPick.Some || second == RowsPick.Some ? RowsPick.Some
        : first == RowsPick.Unknown || second == RowsPick.Unknown ? RowsPick.Unknown
        : RowsPick.All;

    private static RowsPick Or(RowsPick first, RowsPick second) =>
        first == RowsPick.All || second == RowsPick.All ? RowsPick.All
        : first == RowsPick.None ? second
        : second == RowsPick.None ? first
        : RowsPick.Unknown;

    private static RowsPick Not(RowsPick picked) => picked switch
    {
        RowsPick.All => RowsPick.None,
        RowsPick.None => RowsPick.All,
        _ => picked,
    };

    /// <summary>A selector alternative's compounds, outside parentheses and brackets, each with whether a child combinator comes before it.</summary>
    private static IEnumerable<(string Compound, bool Child)> Steps(string alternative)
    {
        StringBuilder current = new();
        int depth = 0;
        bool child = false;
        foreach (char character in alternative)
        {
            depth += character switch { '(' or '[' => 1, ')' or ']' => -1, _ => 0 };
            if (depth == 0 && (char.IsWhiteSpace(character) || character == '>'))
            {
                if (current.Length > 0)
                {
                    yield return (current.ToString(), child);
                    current.Clear();
                    child = false;
                }

                child |= character == '>';
                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            yield return (current.ToString(), child);
        }
    }

    /// <summary>
    /// Whether a compound's qualifiers may hold of an element: each class it asks for is one of
    /// <paramref name="classes"/>, the name it asks for is <paramref name="name"/>, and it asks for none of
    /// the <paramref name="states"/> the element starts out of.
    /// </summary>
    private static bool MayHold(string qualifiers, IReadOnlyCollection<string> classes, string? name, HashSet<string> states)
    {
        int index = 0;
        while (index < qualifiers.Length)
        {
            char character = qualifiers[index];
            if (character is '.' or '#' or ':')
            {
                int end = index + 1;
                while (end < qualifiers.Length && (char.IsLetterOrDigit(qualifiers[end]) || qualifiers[end] is '_' or '-'))
                {
                    end++;
                }

                string token = qualifiers[(index + 1)..end];
                bool holds = character switch
                {
                    '.' => classes.Contains(token, StringComparer.Ordinal),
                    '#' => name == token,
                    _ => !states.Contains(token),
                };
                if (!holds)
                {
                    return false;
                }

                index = end < qualifiers.Length && qualifiers[end] == '(' ? PastGroup(qualifiers, end, '(', ')') : end;
            }
            else if (character == '[')
            {
                index = PastGroup(qualifiers, index, '[', ']');
            }
            else
            {
                index++;
            }
        }

        return true;
    }

    /// <summary>The index just past the group that opens at <paramref name="start"/>, however deeply it nests.</summary>
    private static int PastGroup(string text, int start, char open, char close)
    {
        int depth = 0;
        for (int index = start; index < text.Length; index++)
        {
            depth += text[index] == open ? 1 : text[index] == close ? -1 : 0;
            if (depth == 0)
            {
                return index + 1;
            }
        }

        return text.Length;
    }

    /// <summary>Whether a selector alternative asks for one of <paramref name="states"/> of what it styles, or of the control whose template holds it.</summary>
    private static bool AsksForState(string alternative, HashSet<string> states)
    {
        List<string> parts = [.. Compounds(alternative)];
        if (parts.Count == 0)
        {
            return false;
        }

        int template = parts.LastIndexOf(Template);
        return InState(parts[^1], states) || (template > 0 && InState(parts[template - 1], states));
    }

    /// <summary>Whether a compound selector asks for one of <paramref name="states"/>, outside parentheses.</summary>
    private static bool InState(string compound, HashSet<string> states)
    {
        int depth = 0;
        for (int index = 0; index < compound.Length; index++)
        {
            char character = compound[index];
            if (character is '(' or ')')
            {
                depth += character == '(' ? 1 : -1;
                continue;
            }

            if (depth != 0 || character != ':')
            {
                continue;
            }

            int end = index + 1;
            while (end < compound.Length && (char.IsLetterOrDigit(compound[end]) || compound[end] == '-'))
            {
                end++;
            }

            if (states.Contains(compound[(index + 1)..end]))
            {
                return true;
            }

            index = end - 1;
        }

        return false;
    }

    /// <summary>A compound selector's type, whether it is <c>:is()</c>, and what qualifies it.</summary>
    private static (string? Type, bool IsIs, string Qualifiers) Split(string compound)
    {
        if (compound.StartsWith(":is(", StringComparison.Ordinal))
        {
            int close = compound.IndexOf(')', StringComparison.Ordinal);
            return close < 0
                ? (null, true, compound)
                : (Unprefixed(compound[4..close].Trim()), true, compound[(close + 1)..]);
        }

        int end = 0;
        while (end < compound.Length && (char.IsLetterOrDigit(compound[end]) || compound[end] is '_' or '|'))
        {
            end++;
        }

        return end == 0 ? (null, false, compound) : (Unprefixed(compound[..end]), false, compound[end..]);
    }

    /// <summary>A selector type without its <c>ns|</c> prefix.</summary>
    private static string Unprefixed(string type) => type[(type.LastIndexOf('|') + 1)..];

    /// <summary>A selector's compound selectors: split at combinators outside parentheses.</summary>
    private static IEnumerable<string> Compounds(string alternative)
    {
        StringBuilder current = new();
        int depth = 0;
        foreach (char character in alternative)
        {
            depth += character switch { '(' => 1, ')' => -1, _ => 0 };
            if (depth == 0 && (char.IsWhiteSpace(character) || character == '>'))
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <summary><paramref name="text"/> split at <paramref name="separator"/> outside parentheses.</summary>
    private static IEnumerable<string> AtDepthZero(string text, char separator)
    {
        int depth = 0;
        int start = 0;
        for (int index = 0; index < text.Length; index++)
        {
            depth += text[index] switch { '(' => 1, ')' => -1, _ => 0 };
            if (depth == 0 && text[index] == separator)
            {
                yield return text[start..index];
                start = index + 1;
            }
        }

        yield return text[start..];
    }
}
