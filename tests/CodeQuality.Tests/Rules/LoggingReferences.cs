using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// The .NET 8 reference assemblies with Microsoft.Extensions.Logging.Abstractions and Serilog beside them, for
/// <c>BNCQ1012</c>, by plans/00014.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>The real packages, at the versions the family's code takes.</b> Serilog 4.3.1, which AppKit and the
/// templates pin, and the Abstractions 10.0.12 AppKit resolves, as plans/00014 step 2 read them; a stub could
/// declare an overload the real library does not. Resolved from nuget.org on first use and cached, as test-time
/// references that no project restores.
/// </para>
/// <para>
/// ⚠ <b>The logging generator does not run.</b> A reference assembly brings no analyzer, so a test that declares a
/// <c>[LoggerMessage]</c> method writes the generator's half itself, in a <c>.g.cs</c> file and carrying
/// <c>[GeneratedCode]</c>, as the generator writes it.
/// </para>
/// </remarks>
internal static class LoggingReferences
{
    private static readonly PackageIdentity Abstractions = new("Microsoft.Extensions.Logging.Abstractions", "10.0.12");

    private static readonly PackageIdentity Serilog = new("Serilog", "4.3.1");

    /// <summary>Both loggers.</summary>
    public static readonly ReferenceAssemblies Both = ReferenceAssemblies.Net.Net80.AddPackages([Abstractions, Serilog]);

    /// <summary>Microsoft.Extensions.Logging alone.</summary>
    public static readonly ReferenceAssemblies MelOnly = ReferenceAssemblies.Net.Net80.AddPackages([Abstractions]);

    /// <summary>Serilog alone.</summary>
    public static readonly ReferenceAssemblies SerilogOnly = ReferenceAssemblies.Net.Net80.AddPackages([Serilog]);

    public static Task<ImmutableArray<MetadataReference>> Resolve(ReferenceAssemblies assemblies) =>
        assemblies.ResolveAsync(LanguageNames.CSharp, TestContext.Current.CancellationToken);
}
