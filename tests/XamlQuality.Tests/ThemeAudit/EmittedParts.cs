using System.Reflection;
using System.Reflection.Emit;

namespace XamlQuality.Tests.ThemeAudit;

/// <summary>
/// A control library on disk for <c>theme-audit parts</c> to load: <c>PartsFixture.dll</c>, emitted at test time.
/// </summary>
/// <remarks>
/// <para>
/// <c>Fixtures.Gauge</c> declares the constant <c>PART_Needle</c>, and its <c>Apply</c> looks <c>PART_Dial</c> up through
/// a <c>Find</c> of its own. It references nothing but the core library, so it loads from its file anywhere.
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

        string file = Path.Combine(Directory.CreateTempSubdirectory("xq-partsfixture-").FullName, "PartsFixture.dll");

        PersistedAssemblyBuilder fixture = new(new AssemblyName("PartsFixture"), typeof(object).Assembly);
        TypeBuilder gauge = fixture.DefineDynamicModule("PartsFixture")
            .DefineType("Fixtures.Gauge", TypeAttributes.Public | TypeAttributes.Class);
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

        fixture.Save(file);
        return file;
    }
}
