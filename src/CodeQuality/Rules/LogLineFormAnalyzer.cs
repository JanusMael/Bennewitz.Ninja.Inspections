using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality.Rules;

/// <summary>
/// BNCQ1012: a log template is not in the family's one form, by plans/00014.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>One form for every line the family writes</b>, Bennewitz.Ninja.Templates' plans/00014:
/// <c>[Backup.Tab] index={Index} tab="{Tab}"</c>, a tag and then key=value pairs, so one search finds every line
/// of a kind and every value by its key. <see cref="LogTemplates"/> holds the grammar; the finding names the first
/// place a template leaves it.
/// </para>
/// <para>
/// ⚠ <b>The calls it reads</b>, each by its template parameter, so an overload added later is read too:
/// <c>LoggerExtensions.LogTrace</c> to <c>LogCritical</c> and <c>Log</c>, by <c>message</c>;
/// <c>LoggerMessage.Define</c> in every arity, by <c>formatString</c>; the <c>Message</c> of
/// <c>[LoggerMessage]</c>; and Serilog's <c>Verbose</c> to <c>Fatal</c> and <c>Write</c>, by
/// <c>messageTemplate</c>, on <c>Serilog.Log</c>, on <c>Serilog.ILogger</c> and on every type that implements it,
/// since a call binds to the member on the type the compiler chose. <c>BeginScope</c> writes no line, and
/// <c>ILogger.Log&lt;TState&gt;</c> with a formatter takes no template.
/// </para>
/// <para>
/// ⚠ <b>Also reported:</b> a template that is not a constant, whose form cannot be read; a hole whose format is
/// <c>l</c>, which the family's <c>{Message:lj}</c> sinks already apply and which on an enum throws or drops the
/// line; and an enum, or a nullable one, given to a hole, which those sinks print quoted. A hole is matched to its
/// argument by position, by number for a numbered hole, by parameter name, ignoring case, for
/// <c>[LoggerMessage]</c>, and to its type argument for <c>Define</c>; where holes and arguments do not pair, the
/// compiler's own analyzers say so, and no enum is placed.
/// </para>
/// <para>
/// ⛔ <b>Roslyn takes a consumer's own partial method as generated code where its other half carries
/// <c>[GeneratedCode]</c>.</b> Measured in plans/00014 steps 2 and 3: the two halves share their attributes, so an
/// analyzer that analyses no generated code never sees a <c>[LoggerMessage]</c> method, and Templates' three
/// templates, which log only through such methods, would read as having no line at all; and a body written by hand
/// in an MVVM Toolkit <c>On…Changed</c> hook reads as generated too, which hid 8 of ClaudeForge's templates. So this
/// rule analyses generated code, and leaves out only a file a tool wrote, by its name or its header, as Roslyn's own
/// heuristic reads a file. The attribute decides nothing.
/// </para>
/// <para>
/// ⚠ <b>No template is exempt by its shape.</b> A line that passes on one already written, as a sink that
/// forwards a rendered line as <c>"{RawLine}"</c>, is suppressed where it is, with its reason: an exempt
/// <c>"{X}"</c> would let any sentence through as its argument.
/// </para>
/// <para>
/// ⛔ <b>Inert where neither logger can be named.</b> A compilation that names neither
/// <c>Microsoft.Extensions.Logging.ILogger</c> nor <c>Serilog.ILogger</c> registers nothing.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class LogLineFormAnalyzer : ScopedAnalyzer<LogLineFormAnalyzer.Scope>
{
    /// <summary>The rule's ID. Public API: a consumer's suppressions name it.</summary>
    public const string Id = "BNCQ1012";

    /// <summary>A line in the form, which every message shows.</summary>
    private const string Example = "[Backup.Tab] index={Index} tab=\"{Tab}\"";

    // ⚠ Sets built from arrays: ImmutableHashSet.Create given loose names binds to its params ReadOnlySpan overload,
    // which references System.Memory, an assembly the compiler host does not load beside an analyzer.
    private static readonly HashSet<string> MelMethods = new(
        new[] { "LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical", "Log" }, StringComparer.Ordinal);

    private static readonly HashSet<string> SerilogMethods = new(
        new[] { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal", "Write" }, StringComparer.Ordinal);

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A log template is not in the family's one form",
        messageFormat: "{0}",
        Catalog.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every log line the family writes takes one form, a tag and then key=value pairs, as " + Example + ", so one search finds every line of a kind and every value by its key. The rule reads the constant template of every Microsoft.Extensions.Logging LoggerExtensions call, LoggerMessage.Define and [LoggerMessage], and every Serilog call, and reports the first place it leaves the form, a template that is not a constant, a hole whose format is l, and an enum given to a hole. A line that passes on one already written is suppressed where it is, with its reason.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override GeneratedCodeAnalysisFlags GeneratedCode =>
        GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics;

    /// <inheritdoc />
    protected override Scope? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        INamedTypeSymbol? melLogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
        INamedTypeSymbol? serilogLogger = compilation.GetTypeByMetadataName("Serilog.ILogger");
        if (melLogger is null && serilogLogger is null)
        {
            return null;
        }

        return new Scope(
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessage"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessageAttribute"),
            serilogLogger,
            compilation.GetTypeByMetadataName("Serilog.Log"));
    }

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Scope scope)
    {
        context.RegisterOperationAction(operationContext => CheckCall(operationContext, scope), OperationKind.Invocation);
        if (scope.LoggerMessageAttribute is not null)
        {
            context.RegisterSyntaxNodeAction(nodeContext => CheckDeclaration(nodeContext, scope), SyntaxKind.MethodDeclaration);
        }
    }

    private static void CheckCall(OperationAnalysisContext context, Scope scope)
    {
        var call = (IInvocationOperation)context.Operation;
        if (IsGeneratedFile(call.Syntax.SyntaxTree, context.CancellationToken))
        {
            return;
        }

        IMethodSymbol method = call.TargetMethod;
        string? parameter = TemplateParameterOf(method, scope);
        IArgumentOperation? template = parameter is null
            ? null
            : call.Arguments.FirstOrDefault(argument => argument.Parameter?.Name == parameter);
        if (template is null)
        {
            return;
        }

        Location location = template.Value.Syntax.GetLocation();
        if (template.Value.ConstantValue is not { HasValue: true } constant)
        {
            Report(context.ReportDiagnostic, location,
                "This log template is not a constant, so its form cannot be read, and a template built at run time, an interpolated one above all, gives a structured sink no holes to keep. Write it as a constant in the form, as " + Example + ", with its values as arguments.");
            return;
        }

        if (constant.Value is not string text)
        {
            return;
        }

        IReadOnlyList<ITypeSymbol?> values = parameter == "formatString"
            ? method.TypeArguments.Cast<ITypeSymbol?>().ToList()
            : ValuesAfter(call, template);
        CheckTemplate(context.ReportDiagnostic, location, text, holes => Placed(holes, values));
    }

    private static void CheckDeclaration(SyntaxNodeAnalysisContext context, Scope scope)
    {
        var declaration = (MethodDeclarationSyntax)context.Node;
        if (declaration.AttributeLists.Count == 0 || IsGeneratedFile(declaration.SyntaxTree, context.CancellationToken)
            || context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not IMethodSymbol method)
        {
            return;
        }

        foreach (AttributeData attribute in method.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, scope.LoggerMessageAttribute)
                || attribute.ApplicationSyntaxReference is not { } reference
                || reference.SyntaxTree != declaration.SyntaxTree
                || !declaration.Span.Contains(reference.Span)
                || reference.GetSyntax(context.CancellationToken) is not AttributeSyntax syntax
                || MessageOf(attribute, syntax) is not { } message)
            {
                continue;
            }

            CheckTemplate(context.ReportDiagnostic, message.Location, message.Text, holes => holes
                .Select(hole => method.Parameters.FirstOrDefault(p => string.Equals(p.Name, hole.Name, StringComparison.OrdinalIgnoreCase))?.Type)
                .ToList());
        }
    }

    private static void CheckTemplate(
        Action<Diagnostic> report,
        Location location,
        string template,
        Func<IReadOnlyList<LogTemplates.Hole>, IReadOnlyList<ITypeSymbol?>> place)
    {
        if (LogTemplates.FirstDeparture(template) is { } departure)
        {
            Report(report, location,
                "This log template is not in the family's one form: " + departure.What + ", at \"" + departure.Near + "\". A line is a tag, then key=value pairs only, camelCase keys and free text quoted, as " + Example + ", so one search finds every line of a kind. Rewrite it in the form; a line that passes on one already written is suppressed where it is, with its reason.");
        }

        IReadOnlyList<LogTemplates.Hole> holes = LogTemplates.HolesOf(template);
        foreach (LogTemplates.Hole hole in holes)
        {
            if (hole.Format == "l")
            {
                Report(report, location,
                    "The hole " + hole.Text + " carries the format l, which the family's {Message:lj} sinks already apply to every string, and which on an enum throws or drops the line. Remove the :l.");
            }
        }

        IReadOnlyList<ITypeSymbol?> types = place(holes);
        for (int i = 0; i < holes.Count && i < types.Count; i++)
        {
            if (types[i] is { } type && IsEnum(type))
            {
                Report(report, location,
                    "The hole " + holes[i].Text + " is given an enum, " + type.ToDisplayString() + ", which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().");
            }
        }
    }

    /// <summary>
    /// The type each hole is given, by position, or by number where every hole is numbered; none where holes and
    /// values do not pair.
    /// </summary>
    private static IReadOnlyList<ITypeSymbol?> Placed(IReadOnlyList<LogTemplates.Hole> holes, IReadOnlyList<ITypeSymbol?> values)
    {
        if (holes.Count > 0 && holes.All(hole => hole.Number >= 0))
        {
            int distinct = holes.Select(hole => hole.Number).Distinct().Count();
            return distinct == values.Count && holes.All(hole => hole.Number < values.Count)
                ? holes.Select(hole => values[hole.Number]).ToList()
                : Array.Empty<ITypeSymbol?>();
        }

        return holes.Count == values.Count ? values : Array.Empty<ITypeSymbol?>();
    }

    /// <summary>
    /// The types of the values given after the template, the elements of a <c>params</c> array made at the call one by
    /// one; an array made elsewhere is one value, of its array type.
    /// </summary>
    private static IReadOnlyList<ITypeSymbol?> ValuesAfter(IInvocationOperation call, IArgumentOperation template)
    {
        var values = new List<ITypeSymbol?>();
        foreach (IArgumentOperation argument in call.Arguments
                     .Where(a => a.Parameter is not null && a.Parameter.Ordinal > template.Parameter!.Ordinal)
                     .OrderBy(a => a.Parameter!.Ordinal))
        {
            if (argument.ArgumentKind == ArgumentKind.DefaultValue)
            {
                continue;
            }

            if (argument.Parameter!.IsParams && Unconverted(argument.Value) is IArrayCreationOperation { Initializer: { } initializer })
            {
                values.AddRange(initializer.ElementValues.Select(TypeOf));
                continue;
            }

            values.Add(TypeOf(argument.Value));
        }

        return values;
    }

    /// <summary>The template parameter's name of a call the rule reads, or null for any other call.</summary>
    private static string? TemplateParameterOf(IMethodSymbol method, Scope scope)
    {
        INamedTypeSymbol? type = method.ContainingType;
        if (type is null)
        {
            return null;
        }

        if (MelMethods.Contains(method.Name) && SymbolEqualityComparer.Default.Equals(type, scope.LoggerExtensions))
        {
            return "message";
        }

        if (method.Name == "Define" && SymbolEqualityComparer.Default.Equals(type, scope.LoggerMessage))
        {
            return "formatString";
        }

        if (SerilogMethods.Contains(method.Name) && scope.SerilogLogger is { } serilog
            && (SymbolEqualityComparer.Default.Equals(type, scope.SerilogLog)
                || SymbolEqualityComparer.Default.Equals(type, serilog)
                || type.AllInterfaces.Contains(serilog, SymbolEqualityComparer.Default)))
        {
            return "messageTemplate";
        }

        return null;
    }

    /// <summary>The <c>Message</c> of a <c>[LoggerMessage]</c>, by name or by its constructor's <c>message</c>, and where it is written.</summary>
    private static Message? MessageOf(AttributeData attribute, AttributeSyntax syntax)
    {
        IReadOnlyList<AttributeArgumentSyntax> arguments = syntax.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<AttributeArgumentSyntax>);
        foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
        {
            if (named.Key == "Message" && named.Value.Value is string text)
            {
                AttributeArgumentSyntax? written = arguments.FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == "Message");
                return new Message(text, (written?.Expression ?? (SyntaxNode)syntax).GetLocation());
            }
        }

        if (attribute.AttributeConstructor is not { } constructor)
        {
            return null;
        }

        var positional = arguments.Where(a => a.NameEquals is null).ToList();
        for (int i = 0; i < constructor.Parameters.Length && i < attribute.ConstructorArguments.Length; i++)
        {
            if (constructor.Parameters[i].Name == "message" && attribute.ConstructorArguments[i].Value is string text)
            {
                AttributeArgumentSyntax? written = positional.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == "message")
                    ?? (i < positional.Count && positional[i].NameColon is null ? positional[i] : null);
                return new Message(text, (written?.Expression ?? (SyntaxNode)syntax).GetLocation());
            }
        }

        return null;
    }

    /// <summary>
    /// A file a tool wrote, by the names and the header Roslyn's own heuristic reads; the attribute it also reads
    /// is not read here, since a partial method carries it through its generated half.
    /// </summary>
    private static bool IsGeneratedFile(SyntaxTree tree, CancellationToken cancellationToken)
    {
        string name = Path.GetFileName(tree.FilePath ?? string.Empty);
        if (name.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (SyntaxTrivia trivia in tree.GetRoot(cancellationToken).GetLeadingTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                string comment = trivia.ToString();
                if (comment.IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0
                    || comment.IndexOf("<autogenerated", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsEnum(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Enum
        || (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            && nullable.TypeArguments[0].TypeKind == TypeKind.Enum);

    /// <summary>A value's own type, seen through its conversion to <c>object</c> or any implicit one.</summary>
    private static ITypeSymbol? TypeOf(IOperation value) => Unconverted(value).Type;

    private static IOperation Unconverted(IOperation value)
    {
        IOperation current = value;
        while (current is IConversionOperation conversion
               && (conversion.IsImplicit || conversion.Type?.SpecialType == SpecialType.System_Object))
        {
            current = conversion.Operand;
        }

        return current;
    }

    private static void Report(Action<Diagnostic> report, Location location, string message) =>
        report(Diagnostic.Create(Rule, location, message));

    /// <summary>A <c>[LoggerMessage]</c>'s template and where it is written.</summary>
    private sealed class Message
    {
        public Message(string text, Location location)
        {
            Text = text;
            Location = location;
        }

        public string Text { get; }

        public Location Location { get; }
    }

    /// <summary>The types the rule compares against, resolved once per compilation.</summary>
    internal sealed class Scope
    {
        public Scope(
            INamedTypeSymbol? loggerExtensions,
            INamedTypeSymbol? loggerMessage,
            INamedTypeSymbol? loggerMessageAttribute,
            INamedTypeSymbol? serilogLogger,
            INamedTypeSymbol? serilogLog)
        {
            LoggerExtensions = loggerExtensions;
            LoggerMessage = loggerMessage;
            LoggerMessageAttribute = loggerMessageAttribute;
            SerilogLogger = serilogLogger;
            SerilogLog = serilogLog;
        }

        /// <summary><c>Microsoft.Extensions.Logging.LoggerExtensions</c>, which declares <c>LogInformation</c> and its kin.</summary>
        public INamedTypeSymbol? LoggerExtensions { get; }

        /// <summary><c>Microsoft.Extensions.Logging.LoggerMessage</c>, which declares <c>Define</c>.</summary>
        public INamedTypeSymbol? LoggerMessage { get; }

        /// <summary><c>Microsoft.Extensions.Logging.LoggerMessageAttribute</c>.</summary>
        public INamedTypeSymbol? LoggerMessageAttribute { get; }

        /// <summary><c>Serilog.ILogger</c>.</summary>
        public INamedTypeSymbol? SerilogLogger { get; }

        /// <summary><c>Serilog.Log</c>, the static logger.</summary>
        public INamedTypeSymbol? SerilogLog { get; }
    }
}
