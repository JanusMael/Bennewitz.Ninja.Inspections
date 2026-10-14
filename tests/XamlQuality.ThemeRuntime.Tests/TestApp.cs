[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(XamlQuality.ThemeRuntime.Tests.TestApp))]

namespace XamlQuality.ThemeRuntime.Tests;

using Avalonia;
using Avalonia.Headless;

/// <summary>
/// The application every test runs under, headless. It holds a style and two resources of its own,
/// which every run of the check must leave as it found them.
/// </summary>
public sealed class TestApp : Application
{
    /// <summary>The key of the resource the application holds itself.</summary>
    public const string ApplicationSentinel = "ApplicationSentinel";

    /// <summary>The key of the resource its <c>Styles</c> collection holds itself.</summary>
    public const string StylesSentinel = "StylesSentinel";

    /// <summary>The style the application holds: its own, since each test runs in an application of its own.</summary>
    public Avalonia.Styling.Styles OwnStyle { get; } = new();

    public override void Initialize()
    {
        Styles.Add(OwnStyle);
        Styles.Resources[StylesSentinel] = "styles";
        Resources[ApplicationSentinel] = "application";
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
