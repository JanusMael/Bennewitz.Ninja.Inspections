using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality.ThemeAudit;

/// <summary>What filling a brand document made of it.</summary>
/// <param name="Markdown">The filled document; empty whenever <paramref name="Problems"/> is not, so it is never written.</param>
/// <param name="Problems">
/// Every claim that no longer holds and every guard that failed: the theme's files that cannot be read, then the
/// template's problems in its order, each naming its line, then the blocks'.
/// </param>
public sealed record BrandResult(string Markdown, IReadOnlyList<string> Problems);

/// <summary>
/// Fills a brand document from its template: a Markdown file whose placeholders write what nobody should type by hand,
/// a theme's palette, every colour it composites and every contrast figure, and a chart of its swatches, and state the
/// claims a sentence rests on.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The language.</b> <c>{{tok KEY}}</c> writes the colour a token resolves to in the document's variant, as
/// <c>#AARRGGBB</c>, and <c>{{at KEY}}</c> where the token is declared, as its path from the theme's base directory and
/// its line. <c>{{val COLOUR}}</c> writes a colour, an opaque one as <c>#RRGGBB</c>. <c>{{mix STACK}}</c> writes the
/// opaque colour a stack draws as, <c>{{mix STACK =#RRGGBB}}</c> claims it, and <c>{{lab STACK}}</c> writes it for a
/// mermaid label, its <c>#</c> as <c>#35;</c>. <c>{{cr FG on STACK}}</c> writes the WCAG 2.2 contrast of FG drawn over
/// the stack, to one decimal place, and <c>{{wc FG on STACK}}</c> its verdict for normal text, AAA, AA, AA large only or
/// fails; either claims a bound with <c>&lt;N</c> or <c>&gt;=N</c>. <c>{{near PATH "phrase"}}</c> writes
/// <c>PATH:LINE</c> of the one line holding the phrase, and a second phrase makes a range; a phrase holding a double
/// quote is written in single quotes. <c>{{pin PACKAGE VERSION}}</c> claims the version the
/// <c>Directory.Packages.props</c> nearest the configuration's folder, at or above it, pins, and writes it.
/// <c>{{swatches}}</c> draws every token as a mermaid chart in the frame's colours.
/// </para>
/// <para>
/// A COLOUR is <c>#RRGGBB</c>, <c>#AARRGGBB</c> or a token's key, aliases followed: a token is a keyed colour or solid
/// colour brush of the palette's files, or a <c>StaticResource</c> or <c>DynamicResource</c> alias of one. A STACK is
/// colours top first, separated by <c>/</c>, ending in one that draws opaque, and in a stack each colour may carry
/// <c>@opacity</c> as often as it has opacities: a literal's first is its brush's and the rest its elements', and a
/// token's are its elements', its brush's own being the token's.
/// </para>
/// <para>
/// ⚠ <b>An alpha is Avalonia's.</b> A colour's alpha is its own times every opacity on the way, truncated to a whole byte
/// once, as Avalonia 12.1.3's Skia renderer makes it: the brush's opacity times the product of its elements', as
/// <c>DrawingContextImpl.CreatePaint</c> multiplies them. Layers are then composited exactly, as the audit's contrast
/// figures are. A figure is written to one decimal place, except where rounding would carry it onto a threshold it does
/// not reach, 3, 4.5 or 7, which keeps two, rounded down, so a figure and its verdict never disagree.
/// </para>
/// <para>
/// ⚠ <b>A placeholder the language does not define is a block's.</b> A consumer's program renders what only its own
/// code knows and passes it as a block, named by the placeholder it fills, as <c>HUE_TABLE</c> fills
/// <c>{{HUE_TABLE}}</c>. A block replaces its placeholder verbatim and is never read as the language. A placeholder
/// nothing fills, a block no placeholder names, and a block named as one of the language's own are problems.
/// </para>
/// <para>
/// ⭐ <b>Every problem at once, and no document.</b> A claim that no longer holds; a placeholder the language does not
/// know and no block fills, or one it knows written another way; a key that names no token or resolves to no colour; a
/// <c>{{near}}</c> phrase on no line or on more than one; and an opacity in a <c>{{val}}</c>, are each listed with the
/// template's line, and the result then carries no document. So is a file of the theme that cannot be read, missing or
/// not well-formed, whose tokens the document would otherwise leave out unseen; an include of another assembly's theme,
/// or of a dictionary written in code, which the audit does not follow, is not, since a token that needs one resolves
/// to no colour where it is used.
/// </para>
/// <para>
/// ⭐ <b>The document's format is a contract.</b> LF, invariant culture, no timestamp, no absolute path, and a first line
/// naming the template by its path from the configuration's folder, so <see cref="OutputDrift.Check"/> holds a committed
/// document to a fresh one alike on every platform.
/// </para>
/// </remarks>
public static class BrandRunner
{
    private static readonly string[] Keywords = ["tok", "at", "val", "mix", "lab", "cr", "wc", "near", "pin", "swatches"];

    /// <summary>A placeholder: <c>{{</c>, then anything but a brace or a line break, quoted phrases whole, then <c>}}</c>.</summary>
    private static readonly Regex Placeholder = new("""\{\{((?:"[^"\n]*"|'[^'\n]*'|[^"'{}\n])*)\}\}""", RegexOptions.CultureInvariant);

    private static readonly Regex LiteralColour = new("^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.CultureInvariant);

    private static readonly Regex ClaimedColour = new("^=#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    private static readonly Regex Bound = new("""^(<|>=)(\d+(?:\.\d+)?)$""", RegexOptions.CultureInvariant);

    /// <summary>
    /// Fills <paramref name="document"/>'s template from its theme's palette, read in its variant, and from
    /// <paramref name="blocks"/>.
    /// </summary>
    /// <param name="config">The configuration the document is an entry of, which every path is relative to.</param>
    /// <param name="document">The document to fill.</param>
    /// <param name="blocks">
    /// Text the caller renders, each filling the placeholder its name is the inside of, as <c>HUE_TABLE</c> fills
    /// <c>{{HUE_TABLE}}</c>; none when omitted.
    /// </param>
    /// <returns>The document, or every problem that stopped it.</returns>
    /// <exception cref="AuditConfigException">
    /// The theme is unknown or its entry is missing, it has no such variant, the template is missing, or a token file is
    /// missing or not one the theme reads.
    /// </exception>
    public static BrandResult Fill(AuditConfig config, BrandConfig document, IReadOnlyDictionary<string, string>? blocks = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(document);

        string template = config.Resolve(document.Template);
        if (!File.Exists(template))
        {
            throw new AuditConfigException($"brand '{document.Output}': template not found: {template}");
        }

        ThemeConfig theme = config.Theme(document.Theme);
        ThemeInventory inventory = AuditRunner.BuildTheme(config, theme).Inventory;

        // A variant the theme does not declare would be served as Default, as Avalonia serves it, so a misspelt one
        // would fill a document of another variant's colours.
        if (document.Variant is not (ThemeInventory.DefaultVariant or "Light" or "Dark")
            && !inventory.Variants.Any(variant => variant.DisplayName == document.Variant)
            && theme.Inheritance?.ContainsKey(document.Variant) != true)
        {
            throw new AuditConfigException(
                $"brand '{document.Output}': theme '{theme.Name}' declares no variant '{document.Variant}', which is not Default, Light or Dark either");
        }

        Filling filling = new(config, document, theme, inventory, blocks ?? new Dictionary<string, string>(StringComparer.Ordinal));
        return filling.Run(Lf(File.ReadAllText(template)), config.Relative(template));
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    /// <summary>A colour as written: an opaque one as <c>#RRGGBB</c>, a translucent one as <c>#AARRGGBB</c>.</summary>
    private static string Show(AuditColor colour) => colour.A == 255 ? Hex(colour) : colour.ToString();

    /// <summary>An opaque colour as <c>#RRGGBB</c>.</summary>
    private static string Hex(AuditColor colour) => $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";

    /// <summary>Text inside a mermaid label, where <c>#…;</c> is an entity code: <c>#</c> as <c>#35;</c>, <c>"</c> as <c>#quot;</c>.</summary>
    private static string Label(string text) => text.Replace("#", "#35;", StringComparison.Ordinal).Replace("\"", "#quot;", StringComparison.Ordinal);

    /// <summary>
    /// A contrast figure to one decimal place, except where rounding would carry it up onto a threshold it does not
    /// reach, as 6.97 onto 7.0, which keeps two decimals, rounded down.
    /// </summary>
    private static string FormatRatio(double ratio)
    {
        decimal exact = (decimal)ratio;
        decimal rounded = Math.Round(exact, 1, MidpointRounding.AwayFromZero);
        if (new[] { 3m, 4.5m, 7m }.Any(threshold => rounded >= threshold && exact < threshold))
        {
            return (Math.Floor(exact * 100) / 100).ToString("0.00", CultureInfo.InvariantCulture) + ":1";
        }

        return rounded.ToString("0.0", CultureInfo.InvariantCulture) + ":1";
    }

    /// <summary>The WCAG 2.2 verdict for normal text.</summary>
    private static string Verdict(double ratio) => ratio >= 7 ? "AAA" : ratio >= 4.5 ? "AA" : ratio >= 3 ? "AA large only" : "fails";

    /// <summary>Black or white, whichever contrasts more with <paramref name="fill"/>: the text of a swatch's label.</summary>
    private static string BestTextOn(AuditColor fill)
    {
        AuditColor black = new(255, 0, 0, 0);
        AuditColor white = new(255, 255, 255, 255);
        return Contrast.Ratio(black, fill) >= Contrast.Ratio(white, fill) ? "#000000" : "#FFFFFF";
    }

    /// <summary>The product of <paramref name="opacities"/>, taken in order from 1, as Avalonia multiplies the opacities its elements push.</summary>
    private static double Product(IEnumerable<double> opacities)
    {
        double product = 1.0;
        foreach (double opacity in opacities)
        {
            product *= opacity;
        }

        return product;
    }

    /// <summary>Whether two paths name one file, as the platform compares them.</summary>
    private static bool SamePath(string a, string b)
    {
        StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), comparison);
    }

    /// <summary>A file's path from <paramref name="directory"/>, with forward slashes.</summary>
    private static string RelativeTo(string directory, string file) => Path.GetRelativePath(directory, file).Replace('\\', '/');

    /// <summary>A placeholder's words: a quoted phrase whole, without its quotes, and otherwise runs of anything but white space.</summary>
    private static List<Word> Words(string content)
    {
        List<Word> words = [];
        int i = 0;
        while (i < content.Length)
        {
            if (char.IsWhiteSpace(content[i]))
            {
                i++;
                continue;
            }

            if (content[i] is '"' or '\'')
            {
                // A placeholder's quotes pair up, which the pattern that finds it requires; a block's name need not.
                int close = content.IndexOf(content[i], i + 1);
                close = close < 0 ? content.Length : close;
                words.Add(new Word(content[(i + 1)..close], Quoted: true));
                i = close + 1;
                continue;
            }

            int start = i;
            while (i < content.Length && !char.IsWhiteSpace(content[i]))
            {
                i++;
            }

            words.Add(new Word(content[start..i], Quoted: false));
        }

        return words;
    }

    /// <summary>One word of a placeholder, and whether it was a quoted phrase.</summary>
    private readonly record struct Word(string Text, bool Quoted);

    /// <summary>A token of the palette: its key and the definition the document's variant takes.</summary>
    private sealed record Token(string Key, ResourceDefinition Definition);

    /// <summary>One document being filled: its palette, and the problems found so far.</summary>
    private sealed class Filling
    {
        private readonly AuditConfig _config;
        private readonly BrandConfig _document;
        private readonly string _baseDirectory;
        private readonly VariantInventory _variant;
        private readonly IReadOnlyDictionary<string, string> _blocks;
        private readonly List<Token> _tokens;
        private readonly Dictionary<string, Token> _byKey;
        private readonly List<string> _unreadable;
        private readonly List<(int Position, string Message)> _problems = [];
        private int _position;
        private string _where = string.Empty;
        private string _subject = string.Empty;

        public Filling(AuditConfig config, BrandConfig document, ThemeConfig theme, ThemeInventory inventory,
                       IReadOnlyDictionary<string, string> blocks)
        {
            _config = config;
            _document = document;
            _blocks = blocks;
            _baseDirectory = config.Resolve(theme.BaseDirectory!);
            _variant = inventory.ForVariant(document.Variant);

            // The palette's tokens are the keyed brushes and colours of its files, ordered as the theme reads its files
            // and then as each declares them, which is the order its chart draws them in. A definition's file is the
            // walker's own spelling of a file it read, so the two compare as strings.
            Dictionary<string, int> order = new(StringComparer.Ordinal);
            foreach (string file in inventory.Files)
            {
                order.TryAdd(file, order.Count);
            }

            HashSet<string> tokenFiles = new(
                document.Tokens is { } listed ? listed.Select(path => FileTheThemeReads(config, document, theme, inventory, path)).OfType<string>() : inventory.Files,
                StringComparer.Ordinal);
            _tokens =
            [
                .. _variant.Keys
                    .Select(key => new Token(key, _variant.Lookup(key)!))
                    .Where(token => token.Definition.File is { } file && tokenFiles.Contains(file) && IsToken(token.Definition))
                    .OrderBy(token => order[token.Definition.File!])
                    .ThenBy(token => token.Definition.Line)
                    .ThenBy(token => token.Key, StringComparer.Ordinal),
            ];
            _byKey = _tokens.ToDictionary(token => token.Key, StringComparer.Ordinal);

            // A file of the theme that cannot be read would leave its tokens out of the document unseen. An include of
            // another assembly's theme, or of a dictionary with no markup of its own, the audit does not follow, and a
            // token that needs one resolves to no colour where it is used; anything else unresolved is a problem.
            _unreadable =
            [
                .. inventory.Unresolved
                    .Where(unresolved => !unresolved.Reason.StartsWith("cross-assembly:", StringComparison.Ordinal)
                                         && !unresolved.Reason.StartsWith("no x:Class type named", StringComparison.Ordinal))
                    .Select(unresolved => unresolved.Source == unresolved.SourceFile
                        ? $"theme '{theme.Name}': {config.Relative(unresolved.SourceFile)} cannot be read: {unresolved.Reason}"
                        : $"theme '{theme.Name}': {config.Relative(unresolved.SourceFile)} includes {unresolved.Source}, which cannot be read: {unresolved.Reason}"),
            ];
        }

        public BrandResult Run(string text, string template)
        {
            _problems.AddRange(_unreadable.Select(message => (-1, message)));
            List<Match> placeholders = Placeholder.Matches(text).ToList();
            HashSet<string> filled = new(StringComparer.Ordinal);
            StringBuilder document = new();
            int last = 0;
            foreach (Match placeholder in placeholders)
            {
                document.Append(text, last, placeholder.Index - last);
                _position = placeholder.Index;
                _where = $"{template}:{LineAt(text, placeholder.Index)}: {placeholder.Value}";
                document.Append(Fill(placeholder.Groups[1].Value, filled) ?? placeholder.Value);
                last = placeholder.Index + placeholder.Length;
            }

            document.Append(text, last, text.Length - last);

            // A '{{' that opens no placeholder closing on its line would be left in the document unread.
            for (int open = text.IndexOf("{{", StringComparison.Ordinal); open >= 0; open = text.IndexOf("{{", open + 1, StringComparison.Ordinal))
            {
                if (!placeholders.Any(placeholder => open >= placeholder.Index && open < placeholder.Index + placeholder.Length))
                {
                    _problems.Add((open, $"{template}:{LineAt(text, open)}: '{{{{' opens no placeholder that closes on its line"));
                }
            }

            foreach (string name in _blocks.Keys.Order(StringComparer.Ordinal))
            {
                if (IsLanguage(name))
                {
                    _problems.Add((int.MaxValue, $"block '{name}' is named as one of the language's own placeholders, which the language fills"));
                }
                else if (!filled.Contains(name))
                {
                    _problems.Add((int.MaxValue, $"block '{name}' fills nothing: the template has no {{{{{name}}}}}"));
                }
            }

            if (_problems.Count > 0)
            {
                return new BrandResult(string.Empty, [.. _problems.OrderBy(problem => problem.Position).Select(problem => problem.Message)]);
            }

            return new BrandResult(
                $"<!-- Generated from {template} by the theme audit's brand output. Edit the template, then regenerate this file. -->\n{document}",
                []);
        }

        /// <summary>What one placeholder writes, or <c>null</c> where a problem stops it.</summary>
        private string? Fill(string content, HashSet<string> filled)
        {
            List<Word> words = Words(content);
            string keyword = words.Count > 0 && !words[0].Quoted ? words[0].Text : string.Empty;
            bool plain = words.All(word => !word.Quoted);
            switch (keyword)
            {
                case "tok" when plain && words.Count == 2:
                    return TokenColour(words[1].Text, 1.0)?.ToString();

                case "at" when plain && words.Count == 2:
                    if (!_byKey.TryGetValue(words[1].Text, out Token? token))
                    {
                        Problem($"{words[1].Text} names no token");
                        return null;
                    }

                    return $"{RelativeTo(_baseDirectory, token.Definition.File!)}:{token.Definition.Line.ToString(CultureInfo.InvariantCulture)}";

                case "val" when plain && words.Count == 2:
                    if (words[1].Text.Contains('@', StringComparison.Ordinal) || words[1].Text.Contains('/', StringComparison.Ordinal))
                    {
                        Problem("{{val}} writes one colour, with no opacity: composite a colour with {{mix}}");
                        return null;
                    }

                    return Layer(words[1].Text) is { } colour ? Show(colour) : null;

                case "mix" when plain && (words.Count == 2 || (words.Count == 3 && ClaimedColour.IsMatch(words[2].Text))):
                {
                    if (Flatten(words[1].Text) is not { } drawn)
                    {
                        return null;
                    }

                    string hex = Hex(drawn);
                    if (words.Count == 3 && !string.Equals(hex, words[2].Text[1..], StringComparison.OrdinalIgnoreCase))
                    {
                        Claim($"draws as {hex}");
                    }

                    return hex;
                }

                case "lab" when plain && words.Count == 2:
                    return Flatten(words[1].Text) is { } mixed ? Label(Hex(mixed)) : null;

                case "cr" or "wc" when plain && words.Count is 4 or 5 && words[2].Text == "on"
                                       && (words.Count == 4 || Bound.IsMatch(words[4].Text)):
                {
                    if (RatioOf(words[1].Text, words[3].Text) is not { } ratio)
                    {
                        return null;
                    }

                    if (words.Count == 5)
                    {
                        Match bound = Bound.Match(words[4].Text);
                        double limit = double.Parse(bound.Groups[2].Value, CultureInfo.InvariantCulture);
                        bool holds = bound.Groups[1].Value == "<" ? ratio < limit : ratio >= limit;
                        if (!holds)
                        {
                            Claim($"is {ratio.ToString("0.00", CultureInfo.InvariantCulture)}:1");
                        }
                    }

                    return keyword == "cr" ? FormatRatio(ratio) : Verdict(ratio);
                }

                case "near" when words.Count is 3 or 4 && !words[1].Quoted && words.Skip(2).All(word => word.Quoted && word.Text.Length > 0):
                    return Near(words[1].Text, words[2].Text, words.Count == 4 ? words[3].Text : null);

                case "pin" when plain && words.Count == 3:
                    return Pin(words[1].Text, words[2].Text);

                case "swatches" when words.Count == 1:
                    return Swatches();

                default:
                    if (IsLanguage(content))
                    {
                        Problem(keyword switch
                        {
                            "tok" => "expected {{tok KEY}}",
                            "at" => "expected {{at KEY}}",
                            "val" => "expected {{val COLOUR}}",
                            "mix" => "expected {{mix STACK}}, or {{mix STACK =#RRGGBB}}",
                            "lab" => "expected {{lab STACK}}",
                            "cr" or "wc" => $"expected {{{{{keyword} FG on STACK}}}}, with an optional <N or >=N",
                            "near" => "expected {{near PATH \"phrase\"}}, with an optional second phrase",
                            "pin" => "expected {{pin PACKAGE VERSION}}",
                            _ => "expected {{swatches}}",
                        });
                        return null;
                    }

                    if (_blocks.TryGetValue(content, out string? block))
                    {
                        filled.Add(content);
                        return Lf(block);
                    }

                    Problem("the language has no such placeholder, and no block fills it");
                    return null;
            }
        }

        /// <summary>Whether a placeholder or a block's name starts with one of the language's keywords.</summary>
        private static bool IsLanguage(string content)
        {
            List<Word> words = Words(content);
            return words.Count > 0 && !words[0].Quoted && Keywords.Contains(words[0].Text, StringComparer.Ordinal);
        }

        private void Problem(string message)
        {
            _problems.Add((_position, $"{_where}: {_subject}{message}"));
        }

        private void Claim(string outcome)
        {
            _problems.Add((_position, $"{_where} {outcome}"));
        }

        /// <summary>
        /// The colour a token resolves to, drawn inside elements whose opacities multiply to <paramref name="opacity"/>;
        /// <c>null</c>, as a problem, where the key names no token or the token resolves to no colour.
        /// </summary>
        private AuditColor? TokenColour(string key, double opacity)
        {
            if (!_byKey.ContainsKey(key))
            {
                Problem($"{key} names no token");
                return null;
            }

            AuditColor? colour = _variant.Resolve(key, opacity);
            if (colour is null)
            {
                Problem($"{key} resolves to no colour");
            }

            return colour;
        }

        /// <summary>One colour of a stack, with its opacities: <c>#RRGGBB</c>, <c>#AARRGGBB</c> or a token's key, then any <c>@opacity</c>.</summary>
        private AuditColor? Layer(string layer)
        {
            string[] parts = layer.Split('@');
            List<double> opacities = [];
            bool read = true;
            foreach (string part in parts.Skip(1))
            {
                if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out double opacity) && opacity is >= 0 and <= 1)
                {
                    opacities.Add(opacity);
                }
                else
                {
                    Problem($"'{part}' is no opacity, which is a number from 0 to 1");
                    read = false;
                }
            }

            string colour = parts[0];
            if (colour.Length == 0)
            {
                Problem($"'{layer}' names no colour");
                return null;
            }

            if (colour.StartsWith('#'))
            {
                if (!LiteralColour.IsMatch(colour) || !AuditColor.TryParse(colour, out AuditColor literal))
                {
                    Problem($"{colour} is no colour, which is written #RRGGBB or #AARRGGBB");
                    return null;
                }

                // A literal's first opacity is its brush's, and the rest are the elements' it is drawn inside.
                return read
                    ? VariantInventory.WithOpacity(literal, (opacities.Count > 0 ? opacities[0] : 1.0) * Product(opacities.Skip(1)))
                    : null;
            }

            AuditColor? token = TokenColour(colour, Product(opacities));
            return read ? token : null;
        }

        /// <summary>The colours of a stack, top first; <c>null</c> where any of them is a problem.</summary>
        private List<AuditColor>? Layers(string stack)
        {
            List<AuditColor> layers = [];
            bool read = true;
            foreach (string layer in stack.Split('/'))
            {
                if (Layer(layer) is { } colour)
                {
                    layers.Add(colour);
                }
                else
                {
                    read = false;
                }
            }

            return read ? layers : null;
        }

        /// <summary>The opaque colour a stack draws as: each colour composited over those below it, from the bottom up.</summary>
        private AuditColor? Flatten(string stack)
        {
            if (Layers(stack) is not { } layers)
            {
                return null;
            }

            AuditColor drawn = layers[^1];
            if (drawn.A != 255)
            {
                Problem($"the bottom of a stack must draw opaque, and {stack.Split('/')[^1]} draws as {Show(drawn)}");
                return null;
            }

            for (int i = layers.Count - 2; i >= 0; i--)
            {
                drawn = layers[i].CompositeOver(drawn);
            }

            return drawn;
        }

        /// <summary>The contrast of <paramref name="foreground"/>'s colours drawn over what <paramref name="stack"/> draws as, with it.</summary>
        private double? RatioOf(string foreground, string stack)
        {
            AuditColor? ground = Flatten(stack);
            List<AuditColor>? layers = Layers(foreground);
            if (ground is null || layers is null)
            {
                return null;
            }

            AuditColor drawn = ground.Value;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                drawn = layers[i].CompositeOver(drawn);
            }

            return Contrast.Ratio(drawn, ground.Value);
        }

        /// <summary><c>PATH:LINE</c> of the one line holding <paramref name="first"/>, or <c>PATH:FIRST-LAST</c> with <paramref name="last"/>.</summary>
        private string? Near(string path, string first, string? last)
        {
            if (Path.IsPathRooted(path))
            {
                Problem($"{path} is not relative, and the document holds no absolute path");
                return null;
            }

            List<string> folders = _document.Near is { } near ? [.. near.Select(_config.Resolve)] : [_config.ConfigDirectory];
            string? file = folders.Select(folder => Path.GetFullPath(Path.Combine(folder, path))).FirstOrDefault(File.Exists);
            if (file is null)
            {
                Problem($"there is no {path} in {string.Join(" or ", folders.Select(_config.Relative))}");
                return null;
            }

            string[] lines = File.ReadAllLines(file);
            int? from = LineHolding(lines, path, first);
            int? to = last is null ? from : LineHolding(lines, path, last);
            if (from is null || to is null)
            {
                return null;
            }

            if (last is null)
            {
                return $"{path}:{from.Value.ToString(CultureInfo.InvariantCulture)}";
            }

            if (to < from)
            {
                Problem($"in {path}, '{last}' comes before '{first}'");
                return null;
            }

            return $"{path}:{from.Value.ToString(CultureInfo.InvariantCulture)}-{to.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        private int? LineHolding(string[] lines, string path, string phrase)
        {
            int[] holding = [.. Enumerable.Range(0, lines.Length).Where(i => lines[i].Contains(phrase, StringComparison.Ordinal))];
            if (holding.Length == 1)
            {
                return holding[0] + 1;
            }

            Problem($"{path} holds '{phrase}' on {holding.Length.ToString(CultureInfo.InvariantCulture)} lines, where it must be one");
            return null;
        }

        /// <summary>The version the nearest <c>Directory.Packages.props</c> pins <paramref name="package"/> at, which must be <paramref name="version"/>.</summary>
        private string? Pin(string package, string version)
        {
            string? props = null;
            for (string? folder = _config.ConfigDirectory; folder is not null && props is null; folder = Path.GetDirectoryName(folder))
            {
                string candidate = Path.Combine(folder, "Directory.Packages.props");
                props = File.Exists(candidate) ? candidate : null;
            }

            if (props is null)
            {
                Problem($"no Directory.Packages.props is at or above {_config.ConfigDirectory}");
                return null;
            }

            string relative = _config.Relative(props);
            XDocument pins;
            try
            {
                pins = XDocument.Load(props);
            }
            catch (XmlException ex)
            {
                Problem($"{relative} is not well-formed XML: {ex.Message}");
                return null;
            }

            string[] versions =
            [
                .. pins.Descendants()
                    .Where(element => element.Name.LocalName == "PackageVersion"
                                      && string.Equals(element.Attribute("Include")?.Value.Trim(), package, StringComparison.OrdinalIgnoreCase))
                    .Select(element => element.Attribute("Version")?.Value.Trim() ?? string.Empty),
            ];
            if (versions.Length != 1)
            {
                Problem(versions.Length == 0
                    ? $"{relative} pins no {package}"
                    : $"{relative} pins {package} {versions.Length.ToString(CultureInfo.InvariantCulture)} times, where it must be once");
                return null;
            }

            if (!string.Equals(versions[0], version, StringComparison.Ordinal))
            {
                Problem($"{relative} pins {package} {versions[0]}");
            }

            return versions[0];
        }

        /// <summary>
        /// Every token as a swatch, filled with what it draws as over the frame's ground and labelled with its key and its
        /// colour, its text black or white, whichever contrasts more: file by file, four to a row, each row titled with the
        /// lines it comes from, and rows linked in order, since unlinked rows stack in whatever order the layout picks.
        /// </summary>
        private string? Swatches()
        {
            if (_document.Frame is not { } frame)
            {
                Problem("the document declares no frame to draw its chart in");
                return null;
            }

            if (_tokens.Count == 0)
            {
                Problem("the palette has no tokens to draw");
                return null;
            }

            AuditColor? ground = Framed("the frame's ground: ", frame.Ground);
            if (ground is null)
            {
                return null;
            }

            AuditColor? stroke = Framed("the frame's stroke: ", frame.Stroke + "/" + Hex(ground.Value));
            AuditColor? title = Framed("the frame's text: ", frame.Text + "/" + Hex(ground.Value));
            List<AuditColor?> colours = [.. _tokens.Select(token => TokenColour(token.Key, 1.0))];
            if (stroke is null || title is null || colours.Any(colour => colour is null))
            {
                return null;
            }

            string groundHex = Hex(ground.Value);
            string strokeHex = Hex(stroke.Value);
            string titleHex = Hex(title.Value);
            List<string> ids = Ids();
            List<(string File, int[] Members)> rows =
            [
                .. Enumerable.Range(0, _tokens.Count)
                    .GroupBy(i => _tokens[i].Definition.File!, (file, members) => (File: file, Members: members.ToArray()))
                    .SelectMany(group => group.Members.Chunk(4).Select(row => (group.File, row))),
            ];

            StringBuilder chart = new();
            chart.Append("```mermaid\n---\nconfig:\n  themeVariables:\n    titleColor: \"").Append(titleHex).Append("\"\n---\n");
            chart.Append("flowchart TB\n");
            chart.Append("    subgraph palpanel [\"the palette's tokens,<br/>on the window's ground\"]\n");
            chart.Append("        direction TB\n");
            for (int r = 0; r < rows.Count; r++)
            {
                (string file, int[] members) = rows[r];
                string first = _tokens[members[0]].Definition.Line.ToString(CultureInfo.InvariantCulture);
                string lines = members.Length > 1 ? first + "-" + _tokens[members[^1]].Definition.Line.ToString(CultureInfo.InvariantCulture) : first;
                chart.Append("        subgraph prow").Append(Row(r)).Append(" [\"").Append(Label(RelativeTo(_baseDirectory, file) + ":" + lines)).Append("\"]\n");
                chart.Append("            direction LR\n");
                chart.Append("            ")
                    .AppendJoin(" ~~~ ", members.Select(i => ids[i] + "[\"" + Label(Shown(_tokens[i].Key) + "<br/>" + Show(colours[i]!.Value)) + "\"]"))
                    .Append('\n');
                chart.Append("        end\n");
            }

            for (int r = 1; r < rows.Count; r++)
            {
                chart.Append("        prow").Append(Row(r - 1)).Append(" ~~~ prow").Append(Row(r)).Append('\n');
            }

            chart.Append("    end\n");
            chart.Append("    style palpanel fill:").Append(groundHex).Append(",stroke:").Append(strokeHex).Append(",color:").Append(titleHex).Append('\n');
            for (int r = 0; r < rows.Count; r++)
            {
                chart.Append("    style prow").Append(Row(r)).Append(" fill:").Append(groundHex).Append(",stroke:").Append(groundHex).Append(",color:").Append(titleHex).Append('\n');
            }

            for (int i = 0; i < _tokens.Count; i++)
            {
                AuditColor fill = colours[i]!.Value.CompositeOver(ground.Value);
                chart.Append("    style ").Append(ids[i]).Append(" fill:").Append(Hex(fill)).Append(",stroke:").Append(strokeHex).Append(",color:").Append(BestTextOn(fill)).Append('\n');
            }

            return chart.Append("```").ToString();
        }

        /// <summary>A frame colour, its problems naming which.</summary>
        private AuditColor? Framed(string subject, string stack)
        {
            _subject = subject;
            try
            {
                return Flatten(stack);
            }
            finally
            {
                _subject = string.Empty;
            }
        }

        /// <summary>Each token's node id in the chart: its key, every character a mermaid id does not take as <c>_</c>, made unique.</summary>
        private List<string> Ids()
        {
            HashSet<string> taken = new(StringComparer.Ordinal);
            List<string> ids = [];
            foreach (Token token in _tokens)
            {
                string id = "t_" + string.Concat(token.Key.Select(c => char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_'));
                string unique = id;
                for (int n = 2; !taken.Add(unique); n++)
                {
                    unique = id + "_" + n.ToString(CultureInfo.InvariantCulture);
                }

                ids.Add(unique);
            }

            return ids;
        }

        private static string Row(int r) => r.ToString(CultureInfo.InvariantCulture);

        /// <summary>A key as a swatch shows it: without the <c>Brush</c> every brush's key ends in.</summary>
        private static string Shown(string key) => key.Length > 5 && key.EndsWith("Brush", StringComparison.Ordinal) ? key[..^5] : key;

        private static int LineAt(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        /// <summary>
        /// Whether a definition is one of the palette's tokens: a keyed colour or solid colour brush, or a
        /// <c>StaticResource</c> or <c>DynamicResource</c> alias whose chain ends at a colour, or at nothing or in a cycle,
        /// which makes it a token whose colour is a problem where it is used. An alias of anything else, a number or a
        /// font, is no token.
        /// </summary>
        private bool IsToken(ResourceDefinition definition)
        {
            HashSet<string> visiting = new(StringComparer.Ordinal);
            ResourceDefinition? at = definition;
            while (at is not null)
            {
                // A provider supplies its colours with no element of their own.
                if (IsBrushOrColour(at.Element) || at.Value is ResourceValue.ColorLiteral)
                {
                    return true;
                }

                if (at.Value is not ResourceValue.Alias alias || at.Element?.Name.LocalName is not (null or "StaticResource" or "DynamicResource"))
                {
                    return false;
                }

                if (!visiting.Add(alias.TargetKey))
                {
                    return true;
                }

                at = _variant.Lookup(alias.TargetKey);
            }

            return true;
        }

        /// <summary>Whether an element is a keyed colour or solid colour brush.</summary>
        private static bool IsBrushOrColour(XElement? element)
        {
            return element?.Name.LocalName is { } name && (name == "Color" || name.EndsWith("SolidColorBrush", StringComparison.Ordinal));
        }

        /// <summary>
        /// The walker's spelling of a token file, which must be one the theme reads; <c>null</c> for one it names and
        /// cannot read, which is a problem of its own.
        /// </summary>
        private static string? FileTheThemeReads(AuditConfig config, BrandConfig document, ThemeConfig theme, ThemeInventory inventory, string path)
        {
            string file = config.Resolve(path);
            if (inventory.Files.FirstOrDefault(read => SamePath(read, file)) is { } read)
            {
                return read;
            }

            if (inventory.Unresolved.Any(unresolved => unresolved.Source == unresolved.SourceFile && SamePath(unresolved.SourceFile, file)))
            {
                return null;
            }

            throw new AuditConfigException(File.Exists(file)
                ? $"brand '{document.Output}': token file {path} is not a file theme '{theme.Name}' reads"
                : $"brand '{document.Output}': token file {path} does not exist");
        }
    }
}
