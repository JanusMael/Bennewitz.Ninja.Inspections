namespace Bennewitz.Ninja.CodeQuality;

/// <summary>
/// A message template as Serilog and Microsoft.Extensions.Logging read one, and the family's one form for a log line,
/// by plans/00014 decision 5.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The form.</b> A tag, <c>[Area.Kind]</c>, at the very start, each part an upper-case letter and then letters
/// or digits; then nothing, or pairs, each one space, a camelCase key, <c>=</c> and a value; and nothing after the
/// last pair. A value is quoted, <c>"</c>, anything but <c>"</c>, <c>"</c>, or a run with no space and no
/// <c>"</c>, a hole, a literal or both, as <c>elapsedMs={Ms}</c>. <c>[Backup.Tab] index={Index} tab="{Tab}"</c> is in
/// it.
/// </para>
/// <para>
/// ⚠ <b>A hole is read as both libraries read one.</b> <c>{</c>, an optional <c>@</c> or <c>$</c>, a name of
/// letters, digits and underscores, an optional alignment, <c>,</c> and a number, an optional format, <c>:</c> and
/// anything but a brace, and <c>}</c>. <c>{{</c> and <c>}}</c> are literal braces, and a brace that opens no hole is
/// text, as Serilog reads it.
/// </para>
/// <para>
/// ⚠ <b>Whether a bare hole holds free text is not read.</b> A string hole may hold a name or an id, so quoting it is
/// review's, by the plan's dismissed list.
/// </para>
/// </remarks>
internal static class LogTemplates
{
    /// <summary>The holes of <paramref name="template"/>, in the order they are written.</summary>
    public static IReadOnlyList<Hole> HolesOf(string template)
    {
        var holes = new List<Hole>();
        foreach (Part part in Parse(template))
        {
            if (part.Hole is not null)
            {
                holes.Add(part.Hole);
            }
        }

        return holes;
    }

    /// <summary>
    /// The first place <paramref name="template"/> leaves the form, or null where it is in it.
    /// </summary>
    public static Departure? FirstDeparture(string template)
    {
        List<Part> parts = Parse(template);
        Departure Leaves(int index, string what) =>
            new(what, index < parts.Count ? parts[index].Start : template.Length, template);

        if (!IsChar(parts, 0, '['))
        {
            return Leaves(0, "no tag, [Area.Kind], first");
        }

        int i = 1;
        if (!Pascal(parts, ref i))
        {
            return Leaves(1, "a tag part that is not PascalCase");
        }

        if (IsChar(parts, i, ']'))
        {
            return Leaves(0, "a tag of one part, [Area] alone");
        }

        if (!IsChar(parts, i, '.'))
        {
            return Leaves(i, "a tag part that is not PascalCase");
        }

        int kind = ++i;
        if (!Pascal(parts, ref i) || !IsChar(parts, i, ']'))
        {
            return Leaves(kind, "a tag part that is not PascalCase");
        }

        int tagEnd = ++i;
        while (i < parts.Count)
        {
            if (!IsChar(parts, i, ' '))
            {
                return i == tagEnd
                    ? Leaves(i, "text against the tag, with no space before it")
                    : Leaves(i, "text after the last pair");
            }

            int space = i++;
            if (i >= parts.Count)
            {
                return Leaves(space, "a space at the end");
            }

            if (IsChar(parts, i, ' '))
            {
                return Leaves(space, "two spaces");
            }

            int key = i;
            while (i < parts.Count && parts[i].Hole is null && parts[i].Char != '=' && parts[i].Char != ' ')
            {
                i++;
            }

            if (i < parts.Count && parts[i].Hole is not null)
            {
                return Leaves(key, "a key that is not camelCase");
            }

            if (i >= parts.Count || parts[i].Char != '=')
            {
                return Leaves(key, "a sentence, where only key=value pairs may follow the tag");
            }

            if (!Camel(parts, key, i))
            {
                return Leaves(key, "a key that is not camelCase");
            }

            int value = ++i;
            if (IsChar(parts, i, '"'))
            {
                i++;
                while (i < parts.Count && !IsChar(parts, i, '"'))
                {
                    i++;
                }

                if (i >= parts.Count)
                {
                    return Leaves(value, "a quote that does not close");
                }

                i++;
            }
            else
            {
                while (i < parts.Count && !IsChar(parts, i, ' ') && !IsChar(parts, i, '"'))
                {
                    i++;
                }

                if (i == value)
                {
                    return Leaves(key, "a key with no value");
                }
            }
        }

        return null;
    }

    private static bool IsChar(List<Part> parts, int index, char c) =>
        index < parts.Count && parts[index].Hole is null && parts[index].Char == c;

    /// <summary>An upper-case letter and then letters or digits, from <paramref name="index"/>, which it moves past them.</summary>
    private static bool Pascal(List<Part> parts, ref int index)
    {
        if (index >= parts.Count || parts[index].Hole is not null || !char.IsUpper(parts[index].Char))
        {
            return false;
        }

        index++;
        while (index < parts.Count && parts[index].Hole is null && char.IsLetterOrDigit(parts[index].Char))
        {
            index++;
        }

        return true;
    }

    /// <summary>A lower-case letter and then letters or digits, the parts from <paramref name="start"/> to <paramref name="end"/>.</summary>
    private static bool Camel(List<Part> parts, int start, int end)
    {
        if (end <= start || !char.IsLower(parts[start].Char))
        {
            return false;
        }

        for (int i = start + 1; i < end; i++)
        {
            if (!char.IsLetterOrDigit(parts[i].Char))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The template as its characters and holes, a literal brace one character.</summary>
    private static List<Part> Parse(string template)
    {
        var parts = new List<Part>(template.Length);
        for (int i = 0; i < template.Length; i++)
        {
            char c = template[i];
            if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
            {
                parts.Add(new Part(c, null, i));
                i++;
                continue;
            }

            if (c == '{' && HoleAt(template, i) is { } hole)
            {
                parts.Add(new Part('\0', hole, i));
                i += hole.Text.Length - 1;
                continue;
            }

            parts.Add(new Part(c, null, i));
        }

        return parts;
    }

    /// <summary>The hole that opens at <paramref name="start"/>, or null where the brace opens none.</summary>
    private static Hole? HoleAt(string template, int start)
    {
        int i = start + 1;
        if (i < template.Length && (template[i] == '@' || template[i] == '$'))
        {
            i++;
        }

        int nameStart = i;
        while (i < template.Length && (char.IsLetterOrDigit(template[i]) || template[i] == '_'))
        {
            i++;
        }

        if (i == nameStart)
        {
            return null;
        }

        string name = template.Substring(nameStart, i - nameStart);
        if (i < template.Length && template[i] == ',')
        {
            i++;
            if (i < template.Length && template[i] == '-')
            {
                i++;
            }

            int digits = i;
            while (i < template.Length && char.IsDigit(template[i]))
            {
                i++;
            }

            if (i == digits)
            {
                return null;
            }
        }

        string? format = null;
        if (i < template.Length && template[i] == ':')
        {
            int formatStart = ++i;
            while (i < template.Length && template[i] != '}' && template[i] != '{')
            {
                i++;
            }

            format = template.Substring(formatStart, i - formatStart);
        }

        return i < template.Length && template[i] == '}'
            ? new Hole(template.Substring(start, i - start + 1), name, format)
            : null;
    }

    /// <summary>One hole of a template.</summary>
    internal sealed class Hole
    {
        public Hole(string text, string name, string? format)
        {
            Text = text;
            Name = name;
            Format = format;
        }

        /// <summary>The hole as written, its braces included.</summary>
        public string Text { get; }

        /// <summary>Its name, without <c>@</c> or <c>$</c>.</summary>
        public string Name { get; }

        /// <summary>What follows its <c>:</c>, or null where it has none.</summary>
        public string? Format { get; }

        /// <summary>Its number, where its name is one, as <c>{0}</c>; otherwise -1.</summary>
        public int Number => int.TryParse(Name, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int n) ? n : -1;
    }

    /// <summary>The first place a template leaves the form, and what is there.</summary>
    internal sealed class Departure
    {
        private const int NearLength = 32;

        public Departure(string what, int at, string template)
        {
            What = what;
            At = at;
            string rest = template.Substring(Math.Min(at, template.Length));
            Near = rest.Length > NearLength ? rest.Substring(0, NearLength) + "..." : rest;
        }

        /// <summary>What leaves the form, as the finding names it.</summary>
        public string What { get; }

        /// <summary>Where, as an index into the template.</summary>
        public int At { get; }

        /// <summary>The template from there, cut short.</summary>
        public string Near { get; }
    }

    private sealed class Part
    {
        public Part(char c, Hole? hole, int start)
        {
            Char = c;
            Hole = hole;
            Start = start;
        }

        public char Char { get; }

        public Hole? Hole { get; }

        public int Start { get; }
    }
}
