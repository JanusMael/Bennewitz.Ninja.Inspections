using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Bennewitz.Ninja.CodeQuality;

/// <summary>
/// What the System.Reactive rules share: its types, found by metadata name, and where a finding on
/// a call or a delegate is placed.
/// </summary>
/// <remarks>
/// ⛔ <b>No reference to System.Reactive.</b> The analyzer ships with no dependency, so the types are
/// looked up in the consumer's compilation. One that cannot name <see cref="Observable"/> does not use
/// Rx, and the rules register nothing there.
/// </remarks>
internal static class Reactive
{
    /// <summary>The class that declares the operators.</summary>
    public const string Observable = "System.Reactive.Linq.Observable";

    /// <summary>The class that declares the <c>Subscribe</c> overloads taking delegates.</summary>
    public const string ObservableExtensions = "System.ObservableExtensions";

    /// <summary>What an operator that waits is given to wait on.</summary>
    public const string Scheduler = "System.Reactive.Concurrency.IScheduler";

    /// <summary>Whether <paramref name="method"/> is declared by <paramref name="type"/>.</summary>
    public static bool IsDeclaredBy(IMethodSymbol method, INamedTypeSymbol? type) =>
        type is not null && SymbolEqualityComparer.Default.Equals(method.ContainingType, type);

    /// <summary>
    /// The lambda or anonymous method an argument passes, seen through the delegate creation and
    /// any conversion around it; null for a method group or any other value.
    /// </summary>
    public static IAnonymousFunctionOperation? Function(IOperation value)
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
                case IAnonymousFunctionOperation function:
                    return function;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// The method group an argument passes, seen through the delegate creation and any conversion
    /// around it; null for a lambda or any other value.
    /// </summary>
    public static IMethodSymbol? MethodGroup(IOperation value)
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
                case IMethodReferenceOperation reference:
                    return reference.Method;
                default:
                    return null;
            }
        }
    }

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
}
