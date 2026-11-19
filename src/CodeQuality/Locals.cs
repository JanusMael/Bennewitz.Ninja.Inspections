using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality;

/// <summary>
/// What the rules that follow a value through a local share: the one value a local can hold.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A local is followed only to the one value it can hold.</b> That is the initializer of its
/// declaration, where nothing in the block declaring it assigns it again, passes it by <c>ref</c> or
/// <c>out</c>, or takes a reference to it. <c>BNCQ1009</c> and <c>BNCQ3001</c> read a delegate this way,
/// and <c>BNCQ3002</c> a task, so the rules cannot follow one local two ways.
/// </para>
/// </remarks>
internal static class Locals
{
    /// <summary>
    /// The one value a local holds wherever it is read: the initializer of its declaration, where
    /// nothing in the block declaring it assigns it again, passes it by <c>ref</c> or <c>out</c>, or
    /// takes a reference to it. Null otherwise.
    /// </summary>
    public static IOperation? OnlyValueOf(ILocalReferenceOperation held)
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
    /// counts too, which only ever leaves a value unread.
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
}
