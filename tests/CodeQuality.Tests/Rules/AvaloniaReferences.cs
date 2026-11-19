using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// The .NET 8 reference assemblies with Avalonia beside them, for the rules that read Avalonia.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>The real package, at the version the gotchas were measured on.</b> A stub written here could
/// agree with a rule that the real types would prove wrong. Resolved from nuget.org on first use and
/// cached, like the reference pack.
/// </para>
/// <para>
/// ⚠ <b>Resolved as reference assemblies for a test compilation, never restored into a project</b>,
/// so <c>AvaloniaConfinementTests</c>, which reads each project's restore graph, still finds Avalonia
/// in its one project alone, and nothing this suite builds carries it.
/// </para>
/// </remarks>
internal static class AvaloniaReferences
{
    public static readonly ReferenceAssemblies Assemblies =
        ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Avalonia", "12.1.3")]);

    /// <summary>Avalonia and Avalonia.Headless, for the rules that read the headless session.</summary>
    public static readonly ReferenceAssemblies Headless =
        ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Avalonia", "12.1.3"), new PackageIdentity("Avalonia.Headless", "12.1.3")]);

    /// <summary>
    /// The same on .NET 10's reference assemblies, whose <c>Task.WaitAll</c> and <c>WaitAny</c> take a
    /// <c>params ReadOnlySpan&lt;Task&gt;</c>, which a call with loose tasks binds to.
    /// </summary>
    public static readonly ReferenceAssemblies HeadlessNet100 =
        ReferenceAssemblies.Net.Net100.AddPackages([new PackageIdentity("Avalonia", "12.1.3"), new PackageIdentity("Avalonia.Headless", "12.1.3")]);

    public static Task<ImmutableArray<MetadataReference>> References() =>
        Assemblies.ResolveAsync(LanguageNames.CSharp, TestContext.Current.CancellationToken);

    public static Task<ImmutableArray<MetadataReference>> HeadlessReferences() =>
        Headless.ResolveAsync(LanguageNames.CSharp, TestContext.Current.CancellationToken);

    /// <summary>Runs <typeparamref name="TAnalyzer"/> over <paramref name="source"/>, with Avalonia referenced.</summary>
    public static Task Run<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        new RuleTest<TAnalyzer>(source) { ReferenceAssemblies = Assemblies }.Run();

    /// <summary>Runs <typeparamref name="TAnalyzer"/> over <paramref name="source"/>, with Avalonia and Avalonia.Headless referenced.</summary>
    public static Task RunHeadless<TAnalyzer>(string source, ReferenceAssemblies? assemblies = null)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        new RuleTest<TAnalyzer>(source) { ReferenceAssemblies = assemblies ?? Headless }.Run();
}
