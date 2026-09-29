using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using XamlQuality.Tests.Rules.FocusFakes;
using XamlQuality.Tests.Rules.ItemFakes;

namespace XamlQuality.Tests.Rules;

/// <summary>
/// A consumer's build output that <c>BNXQ1010</c>'s tests cannot write in C#: <c>Views.dll</c>,
/// <c>Avalonia.Fixture.dll</c> and <c>Shadows.dll</c>, emitted at test time against this assembly's fakes, which
/// a scan is given one at a time.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>DerivedCodeView</c> derives from <see cref="CodePaintedView"/>, whose constructor sets its
/// background, so the code that paints it is in an assembly the scan was not given, as a library's base
/// view's is.</item>
/// <item><c>CompiledView</c> has a static <c>!XamlIlPopulate</c> that sets the background of the view it
/// is handed, as the XAML compiler writes a view's markup, which a C# method cannot be named, and a
/// nested <c>XamlClosure_1</c> whose <c>Build_1</c> sets the background of what it is handed, as XamlX
/// writes the content of a template in that markup.</item>
/// <item><c>CompiledBorder</c>, a <c>Border</c> that is not a view, has a <c>!XamlIlPopulate</c> too, as
/// a library's control compiled with markup of its own has, and <c>DerivedCompiledBorder</c> derives from it
/// with none.</item>
/// <item><c>FrameworkDrawn</c>, in an assembly named as the framework's are, draws itself in a
/// <c>Render</c> of its own, as the framework's own controls do.</item>
/// <item><c>FrameworkList</c>, in that assembly too, is a framework list whose code sets a background, as
/// Avalonia's <c>ComboBox</c> sets a fill, on something that is not a row.</item>
/// <item><c>Fixtures.ContentControl</c>, in <c>Shadows.dll</c>, is a consumer's row named as the framework's
/// <see cref="ContentControl"/> is, which it derives from through <see cref="ListBoxItem"/>, and hides its
/// <c>BackgroundProperty</c> with a field of another property; <c>Fixtures.ContentControlList</c> generates those
/// rows. Written in C#, the name would resolve to neither type in every other test.</item>
/// </list>
/// The files stay locked until the process exits, so each run writes its own folder and clears the ones
/// earlier runs left.
/// </remarks>
internal static class EmittedViews
{
    private static readonly Lazy<(Assembly Views, Assembly Framework, Assembly Shadows)> Built = new(Build);

    /// <summary>The loaded <c>Views</c> assembly, built once per test run.</summary>
    public static Assembly Views => Built.Value.Views;

    /// <summary>The loaded <c>Avalonia.Fixture</c> assembly, built once per test run.</summary>
    public static Assembly Framework => Built.Value.Framework;

    /// <summary>The loaded <c>Shadows</c> assembly, built once per test run.</summary>
    public static Assembly Shadows => Built.Value.Shadows;

    private static (Assembly Views, Assembly Framework, Assembly Shadows) Build()
    {
        foreach (string stale in Directory.EnumerateDirectories(Path.GetTempPath(), "xq-views-*"))
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

        string folder = Directory.CreateTempSubdirectory("xq-views-").FullName;
        MethodInfo setBackground = typeof(Control).GetProperty(nameof(Control.Background))!.SetMethod!;

        PersistedAssemblyBuilder views = new(new AssemblyName("Views"), typeof(object).Assembly);
        ModuleBuilder module = views.DefineDynamicModule("Views");

        TypeBuilder derived = module.DefineType("Fixtures.DerivedCodeView", TypeAttributes.Public | TypeAttributes.Class, typeof(CodePaintedView));
        derived.DefineDefaultConstructor(MethodAttributes.Public);
        derived.CreateType();

        TypeBuilder compiled = module.DefineType("Fixtures.CompiledView", TypeAttributes.Public | TypeAttributes.Class, typeof(UserControl));
        compiled.DefineDefaultConstructor(MethodAttributes.Public);
        ILGenerator populate = compiled
            .DefineMethod("!XamlIlPopulate", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(object), compiled])
            .GetILGenerator();
        populate.Emit(OpCodes.Ldarg_1);
        populate.Emit(OpCodes.Ldstr, "Red");
        populate.Emit(OpCodes.Callvirt, setBackground);
        populate.Emit(OpCodes.Ret);

        TypeBuilder closure = compiled.DefineNestedType("XamlClosure_1", TypeAttributes.NestedPrivate | TypeAttributes.Class | TypeAttributes.Sealed);
        ILGenerator build = closure
            .DefineMethod("Build_1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(Control)])
            .GetILGenerator();
        build.Emit(OpCodes.Ldarg_0);
        build.Emit(OpCodes.Ldstr, "Red");
        build.Emit(OpCodes.Callvirt, setBackground);
        build.Emit(OpCodes.Ret);
        compiled.CreateType();
        closure.CreateType();

        TypeBuilder compiledBorder = module.DefineType("Fixtures.CompiledBorder", TypeAttributes.Public | TypeAttributes.Class, typeof(Border));
        ConstructorBuilder borderConstructor = compiledBorder.DefineDefaultConstructor(MethodAttributes.Public);
        ILGenerator populateBorder = compiledBorder
            .DefineMethod("!XamlIlPopulate", MethodAttributes.Private | MethodAttributes.Static, typeof(void), [typeof(object), compiledBorder])
            .GetILGenerator();
        populateBorder.Emit(OpCodes.Ldarg_1);
        populateBorder.Emit(OpCodes.Ldstr, "Transparent");
        populateBorder.Emit(OpCodes.Callvirt, setBackground);
        populateBorder.Emit(OpCodes.Ret);
        compiledBorder.CreateType();

        TypeBuilder derivedBorder = module.DefineType("Fixtures.DerivedCompiledBorder", TypeAttributes.Public | TypeAttributes.Class, compiledBorder);
        ILGenerator derivedConstructor = derivedBorder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes).GetILGenerator();
        derivedConstructor.Emit(OpCodes.Ldarg_0);
        derivedConstructor.Emit(OpCodes.Call, borderConstructor);
        derivedConstructor.Emit(OpCodes.Ret);
        derivedBorder.CreateType();

        string viewsFile = Path.Combine(folder, "Views.dll");
        views.Save(viewsFile);

        PersistedAssemblyBuilder framework = new(new AssemblyName("Avalonia.Fixture"), typeof(object).Assembly);
        ModuleBuilder frameworkModule = framework.DefineDynamicModule("Avalonia.Fixture");
        TypeBuilder drawn = frameworkModule.DefineType("Fixtures.FrameworkDrawn", TypeAttributes.Public | TypeAttributes.Class, typeof(Control));
        drawn.DefineDefaultConstructor(MethodAttributes.Public);
        drawn.DefineMethod("Render", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot, typeof(void), [typeof(DrawingContext)])
            .GetILGenerator()
            .Emit(OpCodes.Ret);
        drawn.CreateType();

        TypeBuilder list = frameworkModule.DefineType("Fixtures.FrameworkList", TypeAttributes.Public | TypeAttributes.Class, typeof(ListBox));
        list.DefineDefaultConstructor(MethodAttributes.Public);
        ILGenerator paint = list.DefineMethod("Paint", MethodAttributes.Public, typeof(void), [typeof(Control)]).GetILGenerator();
        paint.Emit(OpCodes.Ldarg_1);
        paint.Emit(OpCodes.Ldstr, "Transparent");
        paint.Emit(OpCodes.Callvirt, setBackground);
        paint.Emit(OpCodes.Ret);
        list.CreateType();

        string frameworkFile = Path.Combine(folder, "Avalonia.Fixture.dll");
        framework.Save(frameworkFile);

        PersistedAssemblyBuilder shadows = new(new AssemblyName("Shadows"), typeof(object).Assembly);
        ModuleBuilder shadowsModule = shadows.DefineDynamicModule("Shadows");
        TypeBuilder shadowRow = shadowsModule.DefineType("Fixtures.ContentControl", TypeAttributes.Public | TypeAttributes.Class, typeof(ListBoxItem));
        FieldBuilder shadowField = shadowRow.DefineField(
            "BackgroundProperty", typeof(FakeBackgroundProperty), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
        ILGenerator shadowStatic = shadowRow.DefineTypeInitializer().GetILGenerator();
        shadowStatic.Emit(OpCodes.Newobj, typeof(FakeBackgroundProperty).GetConstructor(Type.EmptyTypes)!);
        shadowStatic.Emit(OpCodes.Stsfld, shadowField);
        shadowStatic.Emit(OpCodes.Ret);
        ConstructorBuilder shadowRowConstructor = shadowRow.DefineDefaultConstructor(MethodAttributes.Public);
        shadowRow.CreateType();

        TypeBuilder shadowList = shadowsModule.DefineType("Fixtures.ContentControlList", TypeAttributes.Public | TypeAttributes.Class, typeof(ListBox));
        shadowList.DefineDefaultConstructor(MethodAttributes.Public);
        ILGenerator create = shadowList
            .DefineMethod("CreateContainerForItemOverride", MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(Control), Type.EmptyTypes)
            .GetILGenerator();
        create.Emit(OpCodes.Newobj, shadowRowConstructor);
        create.Emit(OpCodes.Ret);
        shadowList.CreateType();

        string shadowsFile = Path.Combine(folder, "Shadows.dll");
        shadows.Save(shadowsFile);

        AssemblyLoadContext context = new("xq-views");
        return (context.LoadFromAssemblyPath(viewsFile), context.LoadFromAssemblyPath(frameworkFile), context.LoadFromAssemblyPath(shadowsFile));
    }
}
