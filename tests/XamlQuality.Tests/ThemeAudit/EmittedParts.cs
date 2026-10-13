using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// A control library on disk for <c>theme-audit parts</c> to load: <c>PartsFixture.dll</c>, emitted at test time, and
/// the base class it derives from, in <c>PartsBase.dll</c> beside it.
/// </summary>
/// <remarks>
/// <para>
/// <c>Fixtures.Gauge</c> declares the constant <c>PART_Needle</c>, and its <c>Apply</c> looks <c>PART_Dial</c> up through
/// a <c>Find</c> of its own. It derives from <c>Fixtures.GaugeBase</c>, which nothing in this process loads into the
/// default context, so it loads only where the tool's load context finds <c>PartsBase.dll</c> in the folder: a lookup
/// that fails leaves the control unread, and the tests that expect nothing unread say so.
/// </para>
/// <para>
/// ⚠ The tool loads it from its file and keeps it loaded until the process exits, so it is written once per run to a
/// folder of its own, which the next run clears, and never to a test's own folder, which the test deletes.
/// </para>
/// </remarks>
internal static class EmittedParts
{
    private static readonly Lazy<string> Built = new(Build);

    /// <summary>The full path of <c>PartsFixture.dll</c>, built once per test run.</summary>
    public static string Assembly => Built.Value;

    private static string Build()
    {
        foreach (string stale in Directory.EnumerateDirectories(Path.GetTempPath(), "xq-partsfixture-*"))
        {
            try
            {
                Directory.Delete(stale, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Still loaded by a run in progress.
            }
        }

        string folder = Directory.CreateTempSubdirectory("xq-partsfixture-").FullName;

        PersistedAssemblyBuilder bases = new(new AssemblyName("PartsBase"), typeof(object).Assembly);
        TypeBuilder gaugeBase = bases.DefineDynamicModule("PartsBase")
            .DefineType("Fixtures.GaugeBase", TypeAttributes.Public | TypeAttributes.Class);
        gaugeBase.DefineDefaultConstructor(MethodAttributes.Public);
        gaugeBase.CreateType();
        string basesFile = Path.Combine(folder, "PartsBase.dll");
        bases.Save(basesFile);

        // Loaded apart from the default context, only to name the base class while the control is emitted.
        Type baseType = new AssemblyLoadContext("xq-partsfixture-emit")
            .LoadFromAssemblyPath(basesFile)
            .GetType("Fixtures.GaugeBase", throwOnError: true)!;

        PersistedAssemblyBuilder fixture = new(new AssemblyName("PartsFixture"), typeof(object).Assembly);
        TypeBuilder gauge = fixture.DefineDynamicModule("PartsFixture")
            .DefineType("Fixtures.Gauge", TypeAttributes.Public | TypeAttributes.Class, baseType);
        gauge.DefineDefaultConstructor(MethodAttributes.Public);
        gauge.DefineField("NeedlePart", typeof(string), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal)
            .SetConstant("PART_Needle");

        MethodBuilder find = gauge.DefineMethod("Find", MethodAttributes.Public | MethodAttributes.Static, typeof(object), [typeof(string)]);
        ILGenerator findBody = find.GetILGenerator();
        findBody.Emit(OpCodes.Ldnull);
        findBody.Emit(OpCodes.Ret);

        ILGenerator apply = gauge.DefineMethod("Apply", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes)
            .GetILGenerator();
        apply.Emit(OpCodes.Ldstr, "PART_Dial");
        apply.Emit(OpCodes.Call, find);
        apply.Emit(OpCodes.Pop);
        apply.Emit(OpCodes.Ret);
        gauge.CreateType();

        string file = Path.Combine(folder, "PartsFixture.dll");
        fixture.Save(file);
        return file;
    }
}
