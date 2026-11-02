using Bennewitz.Ninja.CodeQuality.Rules;
using Microsoft.CodeAnalysis;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ1009 is off until enabled; enabled, it fires on each measured callback whose body is not wholly inside one try/catch that takes
/// everything, and on no guarded body, no empty one, no unmeasured callback and no body it cannot read.
/// </summary>
public sealed class UnguardedCallbackTests
{
    private static Task Run(string source) => RuleTest<UnguardedCallbackAnalyzer>.Run(source);

    [Fact]
    public Task Every_measured_callback_fires_at_its_body() =>
        Run("""
            using System.Diagnostics;
            using System.IO;
            using System.Threading;

            public sealed class Background
            {
                public void Wire(FileSystemWatcher watcher, Process process)
                {
                    var poll = new Timer(_ {|BNCQ1009:=>|} Work(), null, 0, 1000);
                    var elapsed = new System.Timers.Timer(1000);
                    elapsed.Elapsed += (_, _) {|BNCQ1009:=>|} Work();
                    new Thread(() {|BNCQ1009:=>|} Work()).Start();
                    new Thread(state {|BNCQ1009:=>|} Work()).Start(1);
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|} Work());
                    ThreadPool.QueueUserWorkItem(state {|BNCQ1009:=>|} Work(), 1, preferLocal: false);
                    watcher.Created += (_, _) {|BNCQ1009:=>|} Work();
                    watcher.Changed += (_, _) {|BNCQ1009:=>|} Work();
                    watcher.Deleted += (_, _) {|BNCQ1009:=>|} Work();
                    watcher.Renamed += (_, _) {|BNCQ1009:=>|} Work();
                    process.OutputDataReceived += (_, _) {|BNCQ1009:=>|} Work();
                    process.ErrorDataReceived += (_, _) {|BNCQ1009:=>|} Work();
                    process.Exited += (_, _) {|BNCQ1009:=>|} Work();
                }

                private static void Work() => File.ReadAllText("state.txt");
            }
            """);

    /// <summary>
    /// A body is read wherever it is written: an anonymous method, a method group naming a method or a
    /// local function, and a finalizer.
    /// </summary>
    [Fact]
    public Task Anonymous_methods_method_groups_local_functions_and_finalizers_fire() =>
        Run("""
            using System.IO;
            using System.Threading;

            public sealed class Background
            {
                public void Wire()
                {
                    new Thread({|BNCQ1009:delegate|} () { Work(); }).Start();
                    new Thread({|BNCQ1009:Loop|}).Start();
                    ThreadPool.QueueUserWorkItem({|BNCQ1009:Local|});

                    void Local(object? state)
                    {
                        Work();
                    }
                }

                private void Loop()
                {
                    Work();
                }

                ~{|BNCQ1009:Background|}()
                {
                    Work();
                }

                private static void Work() => File.ReadAllText("state.txt");
            }
            """);

    /// <summary>
    /// Not wholly guarded: a statement outside the try, a catch narrower than everything, a catch with
    /// a filter, a catch that ends in a throw, and a try with no catch at all.
    /// </summary>
    [Fact]
    public Task Bodies_that_are_not_wholly_guarded_fire() =>
        Run("""
            using System;
            using System.IO;
            using System.Threading;

            public sealed class Background
            {
                public void Wire()
                {
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|}
                    {
                        string path = Path.GetFullPath("state.txt");
                        try { File.ReadAllText(path); } catch (Exception error) { Log(error); }
                    });
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|}
                    {
                        try { Work(); } catch (IOException error) { Log(error); }
                    });
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|}
                    {
                        try { Work(); } catch (Exception error) when (error is not OutOfMemoryException) { Log(error); }
                    });
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|}
                    {
                        try { Work(); } catch (Exception error) { Log(error); throw; }
                    });
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|}
                    {
                        try { Work(); } finally { Log(null); }
                    });
                }

                private static void Work() => File.ReadAllText("state.txt");

                private static void Log(Exception? error) { }
            }
            """);

    /// <summary>The fix, one try/catch around the whole body, in each way it is written.</summary>
    [Fact]
    public Task Wholly_guarded_and_empty_bodies_stay_silent() =>
        Run("""
            using System;
            using System.IO;
            using System.Threading;

            public sealed class Background
            {
                public void Wire(FileSystemWatcher watcher)
                {
                    var poll = new Timer(_ =>
                    {
                        try { Work(); } catch (Exception error) { Log(error); }
                    }, null, 0, 1000);
                    watcher.Created += (_, _) =>
                    {
                        try { Work(); } catch { Log(null); }
                    };
                    watcher.Changed += (_, _) =>
                    {
                        try { Work(); } catch (IOException error) { Log(error); } catch (System.Exception error) { Log(error); } finally { Log(null); }
                    };
                    new Thread(Guarded).Start();
                    watcher.Deleted += (_, _) => { };
                }

                private void Guarded()
                {
                    try
                    {
                        Work();
                    }
                    catch (Exception error)
                    {
                        Log(error);
                    }
                }

                ~Background()
                {
                }

                private static void Work() => File.ReadAllText("state.txt");

                private static void Log(Exception? error) { }
            }
            """);

    /// <summary>
    /// Callbacks that were not measured, or are not this rule's: a watcher's <c>Error</c> handler, a
    /// cancellation callback, a task, and a method group from a reference, whose body cannot be read.
    /// </summary>
    [Fact]
    public Task Unmeasured_callbacks_and_unreadable_bodies_stay_silent() =>
        Run("""
            using System;
            using System.IO;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Background
            {
                public void Wire(FileSystemWatcher watcher, CancellationToken token)
                {
                    watcher.Error += (_, _) => Work();
                    token.Register(() => Work());
                    _ = Task.Run(() => Work());
                    ThreadPool.QueueUserWorkItem(Console.WriteLine);
                }

                private static void Work() => File.ReadAllText("state.txt");
            }
            """);

    /// <summary>
    /// The scanner corpus's cases of these shapes, by plans/00005 decision 6, in this repository's own
    /// names. The corpus has no safe form for them. It marks a <c>System.Timers.Timer</c>'s creation
    /// as well as its <c>Elapsed</c> handler; the rule reports the handler, where the fix goes.
    /// </summary>
    [Fact]
    public Task The_corpus_cases_fire() =>
        Run("""
            using System;
            using System.Diagnostics;
            using System.IO;
            using System.Threading;

            public sealed class Refresher : IDisposable
            {
                private readonly Timer _tick;
                private readonly System.Timers.Timer _interval;

                public Refresher()
                {
                    _tick = new Timer(_ {|BNCQ1009:=>|} Refresh(), null, 0, 1000);
                    _interval = new System.Timers.Timer(1000);
                    _interval.Elapsed += (_, _) {|BNCQ1009:=>|} Refresh();
                }

                public void Spawn()
                {
                    new Thread(() {|BNCQ1009:=>|} Refresh()).Start();
                    ThreadPool.QueueUserWorkItem(_ {|BNCQ1009:=>|} Refresh());
                }

                public void Watch(FileSystemWatcher watcher) => watcher.Created += (_, e) {|BNCQ1009:=>|} Refresh();

                public void Follow(Process process) => process.OutputDataReceived += (_, e) {|BNCQ1009:=>|} Refresh();

                public void Dispose()
                {
                    _tick.Dispose();
                    _interval.Dispose();
                }

                private static void Refresh() => File.ReadAllText("refresh.txt");
            }

            public sealed class Handle
            {
                ~{|BNCQ1009:Handle|}()
                {
                    Free();
                }

                private static void Free() { }
            }
            """);

    /// <summary>
    /// A real application's sites of these shapes, by plans/00005 decision 9, in this repository's own
    /// names, each labelled with the verdict its audit gave it. All five in the rule's form are SAFE:
    /// each body only posts to a dispatcher, or calls a method that catches first. The rule cannot
    /// read that, so it reports all five. The watcher's <c>Error</c> handler is not one of the
    /// measured events, and stays silent.
    /// </summary>
    [Fact]
    public Task Real_sites_fire_whatever_their_verdict() =>
        Run("""
            using System;
            using System.Collections.Concurrent;
            using System.IO;
            using System.Threading;

            public sealed class Ui
            {
                public static void Post(Action action) { }
            }

            public sealed class Capture
            {
                private static readonly ConcurrentDictionary<Timer, bool> Pending = new();
                private readonly FileSystemWatcher _watcher = new();
                private Timer? _poll;
                private Timer? _retry;

                // SAFE: removes the timer from a set and posts; a post after shutdown returns and does nothing.
                public void Schedule(Action action, TimeSpan due)
                {
                    Timer? timer = null;
                    timer = new Timer(_ {|BNCQ1009:=>|} { Pending.TryRemove(timer!, out bool _); Ui.Post(action); }, null, due, Timeout.InfiniteTimeSpan);
                    Pending[timer] = true;
                }

                // SAFE: the poll catches its folder read's failures, then posts.
                public void Failed(Exception reason) => _poll = new Timer(_ {|BNCQ1009:=>|} Poll(), null, 500, 500);

                // SAFE: it only posts.
                public void Correct() => _retry ??= new Timer(_ {|BNCQ1009:=>|} Ui.Post(Correct));

                public void Watch()
                {
                    _watcher.Created += (_, _) {|BNCQ1009:=>|} Ui.Post(Grab);   // SAFE: it only posts
                    _watcher.Renamed += (_, _) {|BNCQ1009:=>|} Ui.Post(Grab);   // SAFE: it only posts
                    _watcher.Error += (_, e) => Failed(e.GetException());       // SAFE: not a measured event
                }

                private void Poll()
                {
                    try { Directory.GetFiles("."); } catch (IOException) { return; } catch (UnauthorizedAccessException) { return; }
                    Ui.Post(Grab);
                }

                private void Grab() { }
            }
            """);

    /// <summary>What a consumer writes to turn the rule on.</summary>
    private const string Enabled = """
        root = true

        [*.cs]
        dotnet_diagnostic.BNCQ1009.severity = warning
        """;

    private const string Unguarded = """
        using System.Threading;

        public static class Background
        {
            public static void Wire() => ThreadPool.QueueUserWorkItem(_ => System.IO.File.Delete("x"));
        }
        """;

    /// <summary>
    /// Off until enabled: on real code most bodies it reports could not throw, so plans/00005
    /// decision 3 ships the rule off.
    /// </summary>
    /// <remarks>
    /// ⚠ On a plain compilation, not the testing framework, which enables every diagnostic of the
    /// analyzer under test.
    /// </remarks>
    [Fact]
    public async Task Until_enabled_an_unguarded_callback_is_not_reported()
    {
        Plain.Run run = await Plain.Analyze(new UnguardedCallbackAnalyzer(), Unguarded, await Plain.Net80());

        Assert.Empty(run.Errors);
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Enabled_in_editorconfig_an_unguarded_callback_is_reported()
    {
        Plain.Run run = await Plain.Analyze(new UnguardedCallbackAnalyzer(), Unguarded, await Plain.Net80(), Enabled);

        Assert.Empty(run.Errors);
        Diagnostic finding = Assert.Single(run.Diagnostics);
        Assert.Equal(UnguardedCallbackAnalyzer.Id, finding.Id);
        Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
    }

    [Fact]
    public Task Generated_code_is_not_analyzed() =>
        Run("""
            // <auto-generated/>
            using System.IO;
            using System.Threading;

            public sealed class Background
            {
                public void Wire() => ThreadPool.QueueUserWorkItem(_ => File.ReadAllText("state.txt"));
            }
            """);

    /// <summary>Where the compilation can name none of the callback types, nothing is registered, finalizers included.</summary>
    [Fact]
    public async Task Without_the_callback_types_the_rule_registers_nothing()
    {
        const string Source = """
            public sealed class Handle
            {
                ~Handle()
                {
                    System.IO.File.Delete("x");
                }
            }
            """;

        Plain.Run inert = await Plain.Analyze(new UnguardedCallbackAnalyzer(), Source, [], Enabled);
        Plain.Run control = await Plain.Analyze(new UnguardedCallbackAnalyzer(), Source, await Plain.Net80(), Enabled);

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
