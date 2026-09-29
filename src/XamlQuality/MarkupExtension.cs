namespace Bennewitz.Ninja.XamlQuality;

/// <summary>A markup extension as an attribute writes it: its name and its arguments.</summary>
/// <param name="Name">The extension's name as written, with its prefix: <c>Binding</c>, <c>x:Static</c>.</param>
/// <param name="Arguments">Its arguments, split at commas outside braces and quotes, each trimmed.</param>
/// <remarks>
/// ⚠ <b>Read, never evaluated.</b> A nested extension is kept as its text, so
/// <c>TargetNullValue={x:Static Brushes.Transparent}</c> is the argument <c>TargetNullValue</c> with the
/// value <c>{x:Static Brushes.Transparent}</c>, and what that value is at runtime is not asked.
/// </remarks>
internal sealed record MarkupExtension(string Name, IReadOnlyList<string> Arguments)
{
    /// <summary>The extension <paramref name="text"/> writes, or <c>null</c> when it writes none.</summary>
    /// <remarks>A value starting <c>{}</c> is a literal that happens to start with a brace.</remarks>
    internal static MarkupExtension? Parse(string text)
    {
        string written = text.Trim();
        if (written.Length < 2 || written[0] != '{' || written[^1] != '}' || written.StartsWith("{}", StringComparison.Ordinal))
        {
            return null;
        }

        string inner = written[1..^1].Trim();
        int space = inner.IndexOfAny([' ', '\t', '\r', '\n']);
        string name = space < 0 ? inner : inner[..space];
        string rest = space < 0 ? string.Empty : inner[(space + 1)..];
        return name.Length == 0 ? null : new MarkupExtension(name, [.. Split(rest)]);
    }

    /// <summary>The value of the argument written <c>name=value</c>, trimmed, or <c>null</c> when there is none.</summary>
    internal string? Argument(string name)
    {
        foreach (string argument in Arguments)
        {
            int equals = EqualsAtDepthZero(argument);
            if (equals > 0 && argument[..equals].Trim() == name)
            {
                return argument[(equals + 1)..].Trim();
            }
        }

        return null;
    }

    /// <summary>The argument at <paramref name="index"/> when it is positional rather than named, or <c>null</c>.</summary>
    internal string? Positional(int index) =>
        index < Arguments.Count && EqualsAtDepthZero(Arguments[index]) < 0 ? Arguments[index] : null;

    /// <summary>Every argument written <c>name=value</c>, in order, each name and value trimmed.</summary>
    internal IEnumerable<(string Name, string Value)> Named()
    {
        foreach (string argument in Arguments)
        {
            int equals = EqualsAtDepthZero(argument);
            if (equals > 0)
            {
                yield return (argument[..equals].Trim(), argument[(equals + 1)..].Trim());
            }
        }
    }

    private static IEnumerable<string> Split(string text)
    {
        int depth = 0;
        bool quoted = false;
        int start = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (character == '\'')
            {
                quoted = !quoted;
            }
            else if (!quoted)
            {
                depth += character switch { '{' => 1, '}' => -1, _ => 0 };
                if (depth == 0 && character == ',')
                {
                    yield return text[start..index].Trim();
                    start = index + 1;
                }
            }
        }

        if (text[start..].Trim() is { Length: > 0 } last)
        {
            yield return last;
        }
    }

    private static int EqualsAtDepthZero(string argument)
    {
        int depth = 0;
        for (int index = 0; index < argument.Length; index++)
        {
            depth += argument[index] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0 && argument[index] == '=')
            {
                return index;
            }
        }

        return -1;
    }
}
