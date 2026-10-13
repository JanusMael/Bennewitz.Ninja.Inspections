using System.Globalization;
using System.Text;

namespace Bennewitz.Ninja.XamlQuality.Rules;

/// <summary>
/// Renders a <see cref="TemplatePartReading"/> as the Markdown report a repository commits: one section per control, then
/// what was not read.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A pure function of the reading.</b> Its order, invariant numbers, LF line endings, paths as the reading gives
/// them relative to the scan root, and no timestamp, so a fresh report equals the committed one until the code or a
/// theme changes, on every platform. The format is a contract: a consumer commits the report and holds it to a fresh
/// run, so a change here regenerates every committed copy.
/// </para>
/// <para>
/// ⚠ <b>A theme's column marks what <c>BNXQ1003</c> reports.</b> A part the control's code declares or looks up that a
/// theme does not declare reads <b>missing</b> there, which is exactly a finding of the rule; a part only a theme
/// declares, which a style selector may be all that needs, is shown and marks nothing. Two themes that declare one part
/// differently show both, side by side, and fail nothing.
/// </para>
/// </remarks>
internal static class TemplatePartReport
{
    private const string Missing = "**missing**";

    private const string Nothing = "—";

    /// <summary>Renders the report.</summary>
    internal static string Render(TemplatePartReading reading)
    {
        StringBuilder sb = new();

        sb.Append("# Template parts\n\n");
        sb.Append("Each control's template parts as `BNXQ1003` reads them: the `PART_` constants its code declares, ")
          .Append("the parts code looks up on its template, and the parts each of its themes declares. A part the code ")
          .Append("declares or looks up that a theme does not declare reads ").Append(Missing).Append(" in that ")
          .Append("theme's column, which is what `BNXQ1003` reports. *Looked up by* reads ✓ for the control's own code, ")
          .Append("a base class's assembly for that base class's code, a helper's type for code the control hands its ")
          .Append("template to, and ").Append(Nothing).Append(" where nothing looks the part up.\n\n");

        foreach (ControlTemplateParts control in reading.Controls)
        {
            RenderControl(sb, control);
        }

        sb.Append("## Not read\n\n");
        if (reading.Skipped.Count == 0)
        {
            sb.Append("Nothing.\n");
        }
        else
        {
            foreach (XamlSkip skip in reading.Skipped)
            {
                sb.Append("- ").Append(Code(skip.Subject)).Append(": ").Append(skip.Reason).Append('\n');
            }
        }

        return sb.ToString();
    }

    private static void RenderControl(StringBuilder sb, ControlTemplateParts control)
    {
        sb.Append("## ").Append(Code(control.Name)).Append("\n\n");

        IReadOnlyList<ThemeParts> themes = control.Themes;
        switch (themes.Count)
        {
            case 0:
                sb.Append("No theme for it is in the scanned markup.\n\n");
                break;
            case 1:
                sb.Append("Theme: ").Append(Where(themes[0])).Append(".\n\n");
                break;
            default:
                sb.Append("Themes: ")
                  .Append(string.Join("; ", themes.Select((theme, index) => Number(index + 1) + " " + Where(theme))))
                  .Append(".\n\n");
                break;
        }

        List<string> header = ["Part"];
        for (int index = 1; index <= themes.Count; index++)
        {
            string suffix = themes.Count == 1 ? string.Empty : " " + Number(index);
            header.Add("Element" + suffix);
            header.Add("AutomationId" + suffix);
        }

        header.AddRange(["Constant", "Looked up by"]);
        Row(sb, header);
        Row(sb, header.Select(_ => "---"));

        HashSet<string> wanted = new([.. control.Constants, .. control.Lookups.Select(lookup => lookup.Part)], StringComparer.Ordinal);
        foreach (string part in RowsOf(control))
        {
            List<string> cells = [Code(part)];
            foreach (ThemeParts theme in themes)
            {
                DeclaredPart[] declared = [.. theme.Declared.Where(candidate => candidate.Name == part)];
                cells.Add(declared.Length == 0
                    ? wanted.Contains(part) ? Missing : string.Empty
                    : Joined(declared.Select(candidate => Code(candidate.ElementType))));
                cells.Add(Joined(declared.Select(candidate => candidate.AutomationId).OfType<string>().Select(Id)));
            }

            cells.Add(control.Constants.Contains(part) ? "✓" : string.Empty);
            cells.Add(LookedUpBy(control.Lookups.Where(lookup => lookup.Part == part)));
            Row(sb, cells);
        }

        sb.Append('\n');
    }

    /// <summary>A table row of <paramref name="cells"/>.</summary>
    private static void Row(StringBuilder sb, IEnumerable<string> cells) =>
        sb.Append("| ").Append(string.Join(" | ", cells)).Append(" |\n");

    /// <summary>An automation id in a code span, a blank one as <c>""</c>, since an empty span does not render.</summary>
    private static string Id(string id) => string.IsNullOrWhiteSpace(id) ? "`\"\"`" : Code(id);

    /// <summary>
    /// The control's parts, as its themes declare them: the first theme's in its order, then each later theme's that an
    /// earlier one does not declare; then the parts no theme declares, in ordinal order.
    /// </summary>
    private static List<string> RowsOf(ControlTemplateParts control)
    {
        List<string> rows = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (DeclaredPart part in control.Themes.SelectMany(theme => theme.Declared))
        {
            if (seen.Add(part.Name))
            {
                rows.Add(part.Name);
            }
        }

        rows.AddRange(control.Constants.Concat(control.Lookups.Select(lookup => lookup.Part))
            .Where(seen.Add)
            .Order(StringComparer.Ordinal));
        return rows;
    }

    /// <summary>What looks a part up, each distinct cell once, in the reading's order; <see cref="Nothing"/> when nothing does.</summary>
    private static string LookedUpBy(IEnumerable<TemplatePartLookup> lookups)
    {
        string[] cells = [.. lookups.Select(Cell).Distinct(StringComparer.Ordinal)];
        return cells.Length == 0 ? Nothing : string.Join(", ", cells);
    }

    /// <summary>
    /// A lookup as its cell reads: ✓ for the control's own code, the assembly for a base class's, the type for a
    /// helper's, each with the method a closure is written in.
    /// </summary>
    private static string Cell(TemplatePartLookup lookup)
    {
        string who = lookup.Origin switch
        {
            TemplatePartOrigin.Own => "✓",
            TemplatePartOrigin.BaseClass => lookup.Assembly,
            _ => Code(TypeName(lookup.Type)),
        };

        return Closure(lookup.Method) is { } closure ? who + " " + Code(closure) : who;
    }

    /// <summary>
    /// A closure's method as a reader names it, such as <c>OnApplyTemplate (lambda)</c> for the compiler's
    /// <c>&lt;OnApplyTemplate&gt;b__12_0</c>; <c>null</c> for a method that is not a closure's.
    /// </summary>
    private static string? Closure(string method)
    {
        int close = method.IndexOf('>', StringComparison.Ordinal);
        if (!method.StartsWith('<') || close < 2 || close + 1 >= method.Length)
        {
            return null;
        }

        string declaring = method[1..close];
        return method[close + 1] switch
        {
            'b' => declaring + " (lambda)",
            'g' => declaring + " (local function)",
            _ => null,
        };
    }

    /// <summary>A type's full name as C# writes it without its namespace: <c>Outer.Inner</c> for <c>Ns.Outer+Inner</c>.</summary>
    private static string TypeName(string fullName)
    {
        string[] nesting = fullName.Split('+');
        nesting[0] = nesting[0][(nesting[0].LastIndexOf('.') + 1)..];
        return string.Join('.', nesting);
    }

    private static string Where(ThemeParts theme) =>
        theme.Line is { } line
            ? Code(theme.File) + ", line " + line.ToString(CultureInfo.InvariantCulture)
            : Code(theme.File);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Joined(IEnumerable<string> cells) => string.Join(", ", cells.Distinct(StringComparer.Ordinal));

    /// <summary>Text in a code span, its pipes escaped so a table cell stays one cell.</summary>
    private static string Code(string text) => "`" + text.Replace("|", "\\|", StringComparison.Ordinal) + "`";
}
