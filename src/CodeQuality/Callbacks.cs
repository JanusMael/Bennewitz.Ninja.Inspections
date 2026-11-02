using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality;

/// <summary>
/// What the rules about callbacks no caller catches share: the body a delegate runs, where it can be
/// read, and whether one try statement guards all of it.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Guarded means wholly inside one try statement whose catch takes everything.</b> The body is
/// that try statement and nothing else, and one of its catch clauses takes <see cref="Exception"/>,
/// or no type, with no filter, and does not end in a <c>throw</c>. A statement before or after the
/// try can throw where nothing catches it, a narrower catch lets the rest through, a filter decides
/// at run time, and a catch that ends in a throw sends the exception on to the same end.
/// </para>
/// <para>
/// ⚠ <b>Read from the syntax.</b> The catch's type is compared by name, <c>Exception</c>,
/// <c>System.Exception</c> or <c>global::System.Exception</c>, because a method group's body may
/// sit in another file, whose semantic model an analyzer should not ask for. A type of one's own
/// named <c>Exception</c> would pass for the real one.
/// </para>
/// <para>
/// ⚠ <b>Only a body written in this compilation is read.</b> A lambda, an anonymous method, or a
/// method group naming a method or local function declared in source; a method from a reference is
/// not opened, and is not reported.
/// </para>
/// </remarks>
internal static class Callbacks
{
    /// <summary>
    /// The body the delegate <paramref name="value"/> runs and where a finding about it goes: the
    /// lambda's arrow, the anonymous method's <c>delegate</c> keyword, or the method group's name at
    /// the call. Null where the body cannot be read.
    /// </summary>
    public static Callback? Of(IOperation value)
    {
        if (Calls.Function(value) is { } function)
        {
            return function.Syntax switch
            {
                LambdaExpressionSyntax lambda => new Callback(lambda.Block ?? (SyntaxNode?)lambda.ExpressionBody, lambda.ArrowToken.GetLocation()),
                AnonymousMethodExpressionSyntax method => new Callback(method.Block, method.DelegateKeyword.GetLocation()),
                _ => null,
            };
        }

        if (Calls.MethodGroup(value) is { } group)
        {
            foreach (SyntaxReference reference in group.Method.OriginalDefinition.DeclaringSyntaxReferences)
            {
                SyntaxNode? body = reference.GetSyntax() switch
                {
                    BaseMethodDeclarationSyntax method => method.Body ?? (SyntaxNode?)method.ExpressionBody?.Expression,
                    LocalFunctionStatementSyntax local => local.Body ?? (SyntaxNode?)local.ExpressionBody?.Expression,
                    _ => null,
                };

                if (body is not null)
                {
                    return new Callback(body, NameLocation(group.Syntax));
                }
            }
        }

        return null;
    }

    /// <summary>Whether a finding is due: the body is not empty, and not wholly guarded.</summary>
    public static bool IsUnguarded(SyntaxNode? body)
    {
        if (body is not BlockSyntax block)
        {
            return body is not null;
        }

        List<StatementSyntax> statements = [];
        foreach (StatementSyntax statement in block.Statements)
        {
            if (statement is not EmptyStatementSyntax)
            {
                statements.Add(statement);
            }
        }

        if (statements.Count == 0)
        {
            return false;
        }

        return !(statements.Count == 1 && statements[0] is TryStatementSyntax tryStatement && CatchesEverything(tryStatement));
    }

    private static bool CatchesEverything(TryStatementSyntax tryStatement)
    {
        foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
        {
            if (catchClause.Filter is null
                && (catchClause.Declaration is null || IsException(catchClause.Declaration.Type))
                && !EndsInThrow(catchClause.Block))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsException(TypeSyntax type) =>
        type.ToString().Replace(" ", string.Empty) is "Exception" or "System.Exception" or "global::System.Exception";

    private static bool EndsInThrow(BlockSyntax block) =>
        block.Statements.Count > 0 && block.Statements[block.Statements.Count - 1] is ThrowStatementSyntax;

    private static Location NameLocation(SyntaxNode syntax) => syntax switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.GetLocation(),
        SimpleNameSyntax name => name.Identifier.GetLocation(),
        _ => syntax.GetLocation(),
    };

    /// <summary>A callback's body, and where a finding about it goes.</summary>
    internal sealed class Callback
    {
        public Callback(SyntaxNode? body, Location location)
        {
            Body = body;
            Location = location;
        }

        /// <summary>A block, or the expression of an expression body.</summary>
        public SyntaxNode? Body { get; }

        public Location Location { get; }
    }
}
