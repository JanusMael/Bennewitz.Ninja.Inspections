using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
/// or no type, with no filter or one that lets only fatal exceptions through, and does not end in a
/// <c>throw</c>. A statement before or after the try can throw where nothing catches it, a narrower
/// catch lets the rest through, any other filter decides at run time, and a catch that ends in a
/// throw sends the exception on to the same end.
/// </para>
/// <para>
/// ⭐ <b>A filter that lets only fatal exceptions through still catches everything a callback can
/// survive.</b> It tests the caught variable with <c>is not</c> against fatal types alone:
/// <c>OutOfMemoryException</c>, <c>StackOverflowException</c> and <c>AccessViolationException</c>,
/// written <c>e is not F</c>, <c>e is not (F or G)</c>, <c>e is not F and not G</c>,
/// <c>e is not F &amp;&amp; e is not G</c>, <c>!(e is F)</c> or <c>!(e is F or G)</c>. Measured on
/// .NET 10: a stack overflow cannot be caught, and an access violation ended the process with no
/// catch, finally or handler running, so the one such exception a filter lets past is an
/// <c>OutOfMemoryException</c>, which ends the process wherever it surfaces. Static analysis that
/// reports a catch-all with no filter, as CodeQL's <c>cs/catch-of-all-exceptions</c> does, accepts
/// this one, so a consumer can satisfy both.
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
/// <para>
/// ⚠ <b>A local is followed only to the one value it can hold.</b> A delegate read from a local is
/// the body its declaration initializes it with, followed through up to eight locals, where nothing
/// in the block declaring it assigns it again, passes it by <c>ref</c> or <c>out</c>, or takes a
/// reference to it, and the finding goes on the local's name at the call. A local assigned anywhere
/// else, by name in that block, is not read, and is not reported. Measured: a lambda subscribed
/// through a local of type <c>DataReceivedEventHandler</c> went unreported where the same body as a
/// local function was reported.
/// </para>
/// </remarks>
internal static class Callbacks
{
    /// <summary>How many locals <see cref="Of(IOperation)"/> follows, one holding the next, before it stops.</summary>
    private const int MaxLocalHops = 8;

    /// <summary>
    /// The body the delegate <paramref name="value"/> runs and where a finding about it goes: the
    /// lambda's arrow, the anonymous method's <c>delegate</c> keyword, or the method group's or the
    /// local's name at the call. Null where the body cannot be read.
    /// </summary>
    public static Callback? Of(IOperation value) => Of(value, 0);

    private static Callback? Of(IOperation value, int hops)
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

        if (hops < MaxLocalHops
            && Calls.LocalReference(value) is { } held
            && OnlyValueOf(held) is { } initial
            && Of(initial, hops + 1) is { } callback)
        {
            return new Callback(callback.Body, NameLocation(held.Syntax));
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
            if ((catchClause.Declaration is null || IsException(catchClause.Declaration.Type))
                && (catchClause.Filter is null || LetsOnlyFatalThrough(catchClause))
                && !EndsInThrow(catchClause.Block))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a catch's filter holds of every exception but a fatal one: the caught variable tested
    /// with <c>is not</c> against fatal types alone, in any of the forms that say so.
    /// </summary>
    private static bool LetsOnlyFatalThrough(CatchClauseSyntax catchClause)
    {
        string? caught = catchClause.Declaration?.Identifier.ValueText;
        return !string.IsNullOrEmpty(caught) && ExcludesOnlyFatal(catchClause.Filter!.FilterExpression, caught!);
    }

    // `e is not F`, `e is not (F or G)`, `e is not F and not G`, `e is not F && e is not G`,
    // `!(e is F)` and `!(e is F or G)`, F and G each a fatal type.
    private static bool ExcludesOnlyFatal(ExpressionSyntax filter, string caught) => Unparenthesized(filter) switch
    {
        IsPatternExpressionSyntax test => IsCaught(test.Expression, caught) && NotOfFatal(test.Pattern),
        BinaryExpressionSyntax both when both.IsKind(SyntaxKind.LogicalAndExpression) =>
            ExcludesOnlyFatal(both.Left, caught) && ExcludesOnlyFatal(both.Right, caught),
        PrefixUnaryExpressionSyntax negation when negation.IsKind(SyntaxKind.LogicalNotExpression) =>
            Unparenthesized(negation.Operand) switch
            {
                IsPatternExpressionSyntax test => IsCaught(test.Expression, caught) && OfFatal(test.Pattern),
                BinaryExpressionSyntax test when test.IsKind(SyntaxKind.IsExpression) =>
                    IsCaught(test.Left, caught) && test.Right is TypeSyntax type && IsFatal(type),
                _ => false,
            },
        _ => false,
    };

    private static bool NotOfFatal(PatternSyntax pattern) => pattern switch
    {
        ParenthesizedPatternSyntax inner => NotOfFatal(inner.Pattern),
        UnaryPatternSyntax not when not.IsKind(SyntaxKind.NotPattern) => OfFatal(not.Pattern),
        BinaryPatternSyntax both when both.IsKind(SyntaxKind.AndPattern) => NotOfFatal(both.Left) && NotOfFatal(both.Right),
        _ => false,
    };

    private static bool OfFatal(PatternSyntax pattern) => pattern switch
    {
        ParenthesizedPatternSyntax inner => OfFatal(inner.Pattern),
        BinaryPatternSyntax either when either.IsKind(SyntaxKind.OrPattern) => OfFatal(either.Left) && OfFatal(either.Right),
        TypePatternSyntax type => IsFatal(type.Type),

        // A type in a pattern may parse as a constant: a dotted one as a member access.
        ConstantPatternSyntax { Expression: NameSyntax or MemberAccessExpressionSyntax } constant => IsFatal(constant.Expression),
        _ => false,
    };

    private static ExpressionSyntax Unparenthesized(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static bool IsCaught(ExpressionSyntax expression, string caught) =>
        Unparenthesized(expression) is IdentifierNameSyntax name && name.Identifier.ValueText == caught;

    private static bool IsException(TypeSyntax type) => IsSystemType(type, "Exception");

    private static bool IsFatal(ExpressionSyntax type) =>
        IsSystemType(type, "OutOfMemoryException") || IsSystemType(type, "StackOverflowException") || IsSystemType(type, "AccessViolationException");

    private static bool IsSystemType(ExpressionSyntax type, string name) =>
        type.ToString().Replace(" ", string.Empty) is var written
        && (written == name || written == "System." + name || written == "global::System." + name);

    private static bool EndsInThrow(BlockSyntax block) =>
        block.Statements.Count > 0 && block.Statements[block.Statements.Count - 1] is ThrowStatementSyntax;

    /// <summary>
    /// The one value a local holds wherever it is read: the initializer of its declaration, where
    /// nothing in the block declaring it assigns it again, passes it by <c>ref</c> or <c>out</c>, or
    /// takes a reference to it. Null otherwise.
    /// </summary>
    private static IOperation? OnlyValueOf(ILocalReferenceOperation held)
    {
        ILocalSymbol local = held.Local;
        if (local.RefKind != RefKind.None
            || local.DeclaringSyntaxReferences.Length != 1
            || local.DeclaringSyntaxReferences[0].GetSyntax() is not VariableDeclaratorSyntax { Initializer.Value: { } initial } declarator
            || held.SemanticModel is not { } model
            || declarator.SyntaxTree != model.SyntaxTree
            || IsWrittenAgain(declarator, local.Name))
        {
            return null;
        }

        return model.GetOperation(initial);
    }

    /// <summary>
    /// Whether a name the local's declaration gives is written anywhere in the block that declares it:
    /// on the left of an assignment, compound or deconstructing ones included, passed by <c>ref</c> or
    /// <c>out</c>, or taken by reference. Read by name, so a local of the same name in a nested scope
    /// counts too, which only ever leaves a callback unread.
    /// </summary>
    private static bool IsWrittenAgain(VariableDeclaratorSyntax declarator, string name)
    {
        SyntaxNode scope = (SyntaxNode?)declarator.FirstAncestorOrSelf<BlockSyntax>() ?? declarator.SyntaxTree.GetRoot();
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            if (node is not IdentifierNameSyntax identifier || identifier.Identifier.ValueText != name)
            {
                continue;
            }

            switch (identifier.Parent)
            {
                case ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword):
                case RefExpressionSyntax:
                case PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.AddressOfExpression):
                    return true;
            }

            foreach (SyntaxNode ancestor in identifier.Ancestors())
            {
                if (ancestor is StatementSyntax)
                {
                    break;
                }

                if (ancestor is AssignmentExpressionSyntax assignment && assignment.Left.Span.Contains(identifier.Span))
                {
                    return true;
                }
            }
        }

        return false;
    }

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
