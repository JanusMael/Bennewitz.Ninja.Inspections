using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// The .NET 8 reference assemblies with System.Reactive beside them, for the rules that read Rx.
/// </summary>
/// <remarks>
/// ⚠ <b>The real package, at the version docs/exception-resilience.md measured.</b> The rules find
/// Rx's types by metadata name and choose between its overloads, so a stub written here could agree
/// with a rule that real Rx would prove wrong. The package is resolved from nuget.org on first use
/// and cached, like the reference pack.
/// </remarks>
internal static class Rx
{
    public static readonly ReferenceAssemblies Assemblies =
        ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("System.Reactive", "6.1.0")]);

    public static Task<ImmutableArray<MetadataReference>> References() =>
        Assemblies.ResolveAsync(LanguageNames.CSharp, TestContext.Current.CancellationToken);

    /// <summary>Runs <typeparamref name="TAnalyzer"/> over <paramref name="source"/>, with Rx referenced.</summary>
    public static Task Run<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        new RuleTest<TAnalyzer>(source) { ReferenceAssemblies = Assemblies }.Run();
}
