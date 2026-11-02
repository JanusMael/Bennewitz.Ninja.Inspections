using Bennewitz.Ninja.CodeQuality.Rules;
using Microsoft.CodeAnalysis;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ3001 is off until enabled; enabled, it fires on an Avalonia <c>DispatcherTimer</c>'s <c>Tick</c> handler that is not wholly
/// inside one try/catch, given by <c>+=</c> or to the constructor, and on nothing guarded or empty.
/// </summary>
public sealed class DispatcherTimerTickTests
{
    private static Task Run(string source) => AvaloniaReferences.Run<DispatcherTimerTickAnalyzer>(source);

    [Fact]
    public Task A_Tick_handler_fires_given_by_either_way() =>
        Run("""
            using System;
            using System.IO;
            using Avalonia.Threading;

            public sealed class Autosave
            {
                public void Start()
                {
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    timer.Tick += (_, _) {|BNCQ3001:=>|} Save();
                    timer.Tick += {|BNCQ3001:OnTick|};
                    var built = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (_, _) {|BNCQ3001:=>|} Save());
                    timer.Start();
                    built.Start();
                }

                private void OnTick(object? sender, EventArgs e)
                {
                    Save();
                }

                private static void Save() => File.WriteAllText("draft.txt", "");
            }
            """);

    /// <summary>The fix, one try/catch around the whole body, and a body with nothing in it.</summary>
    [Fact]
    public Task Guarded_and_empty_handlers_stay_silent() =>
        Run("""
            using System;
            using System.IO;
            using Avalonia.Threading;

            public sealed class Autosave
            {
                public void Start()
                {
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    timer.Tick += (_, _) =>
                    {
                        try { Save(); } catch (Exception error) { Log(error); }
                    };
                    timer.Tick += (_, _) => { };
                    timer.Tick -= (_, _) => Save();
                    timer.Start();
                }

                private static void Save() => File.WriteAllText("draft.txt", "");

                private static void Log(Exception error) { }
            }
            """);

    /// <summary>
    /// <c>DispatcherTimer.Run</c> and <c>RunOnce</c> were not measured, so they are not read, and a
    /// <c>System.Threading.Timer</c> is <c>BNCQ1009</c>'s.
    /// </summary>
    [Fact]
    public Task Unmeasured_timer_forms_stay_silent() =>
        Run("""
            using System;
            using System.IO;
            using Avalonia.Threading;

            public static class Later
            {
                public static void Wire()
                {
                    DispatcherTimer.RunOnce(() => Save(), TimeSpan.FromSeconds(1));
                    DispatcherTimer.Run(() => { Save(); return true; }, TimeSpan.FromSeconds(1));
                    var plain = new System.Threading.Timer(_ => Save(), null, 0, 1000);
                }

                private static void Save() => File.WriteAllText("draft.txt", "");
            }
            """);

    /// <summary>
    /// The scanner corpus's cases of this shape, by plans/00005 decision 6, in this repository's own
    /// names: the constructor's callback and a <c>Tick +=</c>. The corpus has no safe form.
    /// </summary>
    [Fact]
    public Task The_corpus_cases_fire() =>
        Run("""
            using System;
            using System.IO;
            using Avalonia.Threading;

            public sealed class Clock
            {
                public void Begin()
                {
                    var ticking = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (_, _) {|BNCQ3001:=>|} Refresh());
                    var bare = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    bare.Tick += (_, _) {|BNCQ3001:=>|} Refresh();
                    ticking.Start();
                    bare.Start();
                }

                private static void Refresh() => File.ReadAllText("clock.txt");
            }
            """);

    /// <summary>What a consumer writes to turn the rule on.</summary>
    private const string Enabled = """
        root = true

        [*.cs]
        dotnet_diagnostic.BNCQ3001.severity = warning
        """;

    private const string Unguarded = """
        using System;
        using Avalonia.Threading;

        public static class Autosave
        {
            public static void Start(DispatcherTimer timer) => timer.Tick += (_, _) => Console.WriteLine();
        }
        """;

    /// <summary>
    /// Off until enabled: the one handler it reported on real code could not throw, so plans/00005
    /// decision 3 ships the rule off.
    /// </summary>
    /// <remarks>
    /// ⚠ On a plain compilation, not the testing framework, which enables every diagnostic of the
    /// analyzer under test.
    /// </remarks>
    [Fact]
    public async Task Until_enabled_an_unguarded_Tick_handler_is_not_reported()
    {
        Plain.Run run = await Plain.Analyze(new DispatcherTimerTickAnalyzer(), Unguarded, await AvaloniaReferences.References());

        Assert.Empty(run.Errors);
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Enabled_in_editorconfig_an_unguarded_Tick_handler_is_reported()
    {
        Plain.Run run = await Plain.Analyze(new DispatcherTimerTickAnalyzer(), Unguarded, await AvaloniaReferences.References(), Enabled);

        Assert.Empty(run.Errors);
        Diagnostic finding = Assert.Single(run.Diagnostics);
        Assert.Equal(DispatcherTimerTickAnalyzer.Id, finding.Id);
        Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
    }

    [Fact]
    public Task Generated_code_is_not_analyzed() =>
        Run("""
            // <auto-generated/>
            using System;
            using Avalonia.Threading;

            public static class Autosave
            {
                public static void Start(DispatcherTimer timer) => timer.Tick += (_, _) => Console.WriteLine();
            }
            """);

    /// <summary>Where the compilation cannot name <c>Avalonia.Threading.DispatcherTimer</c>, nothing is registered.</summary>
    [Fact]
    public async Task Without_Avalonia_the_rule_registers_nothing()
    {
        const string Source = """
            using System;
            using Avalonia.Threading;

            public static class Autosave
            {
                public static void Start(DispatcherTimer timer) => timer.Tick += (_, _) => Console.WriteLine();
            }
            """;

        Plain.Run inert = await Plain.Analyze(new DispatcherTimerTickAnalyzer(), Source, await Plain.Net80(), Enabled);
        Plain.Run control = await Plain.Analyze(new DispatcherTimerTickAnalyzer(), Source, await AvaloniaReferences.References(), Enabled);

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
