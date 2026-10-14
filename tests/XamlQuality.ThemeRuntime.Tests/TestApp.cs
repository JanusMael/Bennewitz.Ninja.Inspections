[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(XamlQuality.ThemeRuntime.Tests.TestApp))]

namespace XamlQuality.ThemeRuntime.Tests;

using Avalonia;
using Avalonia.Headless;

/// <summary>
/// The application every test runs under, headless, one for each test. Its own style, the resources its
/// <c>Styles</c> collection holds and its own resources each hold a key the consumer references and the
/// audit finds undefined in some setup, so a check that did not set them aside would find them there.
/// Every run of the check must leave them as it found them.
/// </summary>
public sealed class TestApp : Application
{
    /// <summary>The key of the resource the application holds itself, undefined under Light.</summary>
    public const string ApplicationSentinel = "OnlyDark";

    /// <summary>The key of the resource its <c>Styles</c> collection holds itself, undefined under Light and Dark.</summary>
    public const string StylesSentinel = "OnlyContrast";

    /// <summary>The key of the resource its own style holds, undefined under the theme no aliases are merged into.</summary>
    public const string StyleSentinel = "GoodAlias";

    /// <summary>The style the application holds: its own, since each test runs in an application of its own.</summary>
    public Avalonia.Styling.Styles OwnStyle { get; } = new();

    public override void Initialize()
    {
        OwnStyle.Resources[StyleSentinel] = "style";
        Styles.Add(OwnStyle);
        Styles.Resources[StylesSentinel] = "styles";
        Resources[ApplicationSentinel] = "application";
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
