using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality;

/// <summary>
/// What the rules that read calls and the delegates passed to them share: the delegate an argument
/// passes, and where a finding on a call is placed.
/// </summary>
internal static class Calls
{
    /// <summary>
    /// The lambda or anonymous method an argument passes, seen through the delegate creation and
    /// any conversion around it; null for a method group or any other value.
    /// </summary>
    public static IAnonymousFunctionOperation? Function(IOperation value) =>
        Unwrap(value) as IAnonymousFunctionOperation;

    /// <summary>
    /// The method a method group passes, seen through the delegate creation and any conversion
    /// around it; null for a lambda or any other value.
    /// </summary>
    public static IMethodReferenceOperation? MethodGroup(IOperation value) =>
        Unwrap(value) as IMethodReferenceOperation;

    /// <summary>
    /// The called method's name in the source, without its type arguments, or the whole call
    /// where it has no name of its own.
    /// </summary>
    public static Location NameOf(IInvocationOperation invocation)
    {
        if (invocation.Syntax is InvocationExpressionSyntax call)
        {
            switch (call.Expression)
            {
                case MemberAccessExpressionSyntax member:
                    return member.Name.Identifier.GetLocation();
                case MemberBindingExpressionSyntax binding:
                    return binding.Name.Identifier.GetLocation();
                case SimpleNameSyntax name:
                    return name.Identifier.GetLocation();
            }
        }

        return invocation.Syntax.GetLocation();
    }

    private static IOperation Unwrap(IOperation value)
    {
        IOperation current = value;
        while (true)
        {
            switch (current)
            {
                case IConversionOperation conversion:
                    current = conversion.Operand;
                    continue;
                case IDelegateCreationOperation creation:
                    current = creation.Target;
                    continue;
                default:
                    return current;
            }
        }
    }
}
