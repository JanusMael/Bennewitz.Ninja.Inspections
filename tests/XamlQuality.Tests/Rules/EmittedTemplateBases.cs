using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace XamlQuality.Tests.Rules;

/// <summary>
/// A base class whose <c>OnApplyTemplate</c> looks a part up, in an assembly of its own, and a control derived from it
/// in another: <c>PartBases.dll</c> and <c>PartHosts.dll</c>, emitted at test time against
/// <see cref="HandedTemplatedControl"/>, as a consumer's control derives from a library's.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>Fixtures.EmittedLookingBase</c>, in <c>PartBases</c>, overrides <c>OnApplyTemplate</c> to find
/// <c>PART_Emitted</c> on the template it is given.</item>
/// <item><c>Fixtures.EmittedHost</c>, in <c>PartHosts</c>, derives from it and looks nothing up itself.</item>
/// </list>
/// Emitted, since C# cannot derive from a type written at test time, and in two assemblies, so that a lookup the base
/// makes is told apart from one the control makes by the assembly it names. The files stay locked until the process
/// exits, so each run writes its own folder and clears the ones earlier runs left.
/// </remarks>
internal static class EmittedTemplateBases
{
    private static readonly Lazy<(Assembly Bases, Assembly Hosts)> Built = new(Build);

    /// <summary>The loaded <c>PartBases</c> assembly, built once per test run.</summary>
    public static Assembly Bases => Built.Value.Bases;

    /// <summary>The loaded <c>PartHosts</c> assembly, built once per test run.</summary>
    public static Assembly Hosts => Built.Value.Hosts;

    private static (Assembly Bases, Assembly Hosts) Build()
    {
        foreach (string stale in Directory.EnumerateDirectories(Path.GetTempPath(), "xq-parts-*"))
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

        string folder = Directory.CreateTempSubdirectory("xq-parts-").FullName;
        AssemblyLoadContext context = new("xq-parts");

        PersistedAssemblyBuilder bases = new(new AssemblyName("PartBases"), typeof(object).Assembly);
        TypeBuilder looking = bases.DefineDynamicModule("PartBases")
            .DefineType("Fixtures.EmittedLookingBase", TypeAttributes.Public | TypeAttributes.Class, typeof(HandedTemplatedControl));
        looking.DefineDefaultConstructor(MethodAttributes.Public);
        ILGenerator apply = looking
            .DefineMethod(
                "OnApplyTemplate",
                MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(void),
                [typeof(HandedTemplateArgs)])
            .GetILGenerator();
        apply.Emit(OpCodes.Ldarg_1);
        apply.Emit(OpCodes.Callvirt, typeof(HandedTemplateArgs).GetProperty(nameof(HandedTemplateArgs.NameScope))!.GetMethod!);
        apply.Emit(OpCodes.Ldstr, "PART_Emitted");
        apply.Emit(OpCodes.Callvirt, typeof(HandedNameScope).GetMethod(nameof(HandedNameScope.Find))!);
        apply.Emit(OpCodes.Pop);
        apply.Emit(OpCodes.Ret);
        looking.CreateType();

        string basesFile = Path.Combine(folder, "PartBases.dll");
        bases.Save(basesFile);
        Assembly loadedBases = context.LoadFromAssemblyPath(basesFile);
        context.Resolving += (_, name) => name.Name == "PartBases" ? loadedBases : null;

        PersistedAssemblyBuilder hosts = new(new AssemblyName("PartHosts"), typeof(object).Assembly);
        TypeBuilder host = hosts.DefineDynamicModule("PartHosts").DefineType(
            "Fixtures.EmittedHost",
            TypeAttributes.Public | TypeAttributes.Class,
            loadedBases.GetType("Fixtures.EmittedLookingBase", throwOnError: true));
        host.DefineDefaultConstructor(MethodAttributes.Public);
        host.CreateType();

        string hostsFile = Path.Combine(folder, "PartHosts.dll");
        hosts.Save(hostsFile);

        return (loadedBases, context.LoadFromAssemblyPath(hostsFile));
    }
}
