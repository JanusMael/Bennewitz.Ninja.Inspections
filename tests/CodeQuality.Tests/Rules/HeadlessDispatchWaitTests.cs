using Bennewitz.Ninja.CodeQuality.Rules;
using Microsoft.CodeAnalysis;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ3002 fires on a wait that blocks on the task <c>HeadlessUnitTestSession.Dispatch</c> returns,
/// directly or through a local, in every form plans/00010 step 2 measured to hang, and on nothing that
/// awaits or returns the task.
/// </summary>
public sealed class HeadlessDispatchWaitTests
{
    private static Task Run(string source) => AvaloniaReferences.RunHeadless<HeadlessDispatchWaitAnalyzer>(source);

    /// <summary>Every form measured to hang after an awaited dispatch: each is reported at the wait.</summary>
    [Fact]
    public async Task Every_measured_wait_fires() =>
        await Run("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Avalonia.Headless;

            public sealed class Waits
            {
                private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Waits).Assembly);

                public void Block()
                {
                    int a = Session.Dispatch(() => 1, CancellationToken.None).GetAwaiter().{|BNCQ3002:GetResult|}();
                    int b = Session.Dispatch(() => 2, CancellationToken.None).{|BNCQ3002:Result|};
                    Session.Dispatch(() => { }, CancellationToken.None).{|BNCQ3002:Wait|}();
                    bool c = Session.Dispatch(() => { }, CancellationToken.None).{|BNCQ3002:Wait|}(TimeSpan.FromSeconds(3));
                    Session.Dispatch(() => { }, CancellationToken.None).{|BNCQ3002:Wait|}(CancellationToken.None);
                    Task.{|BNCQ3002:WaitAll|}(Task.Delay(1), Session.Dispatch(() => { }, CancellationToken.None));
                    int d = Task.{|BNCQ3002:WaitAny|}(new[] { Session.Dispatch(() => { }, CancellationToken.None) });
                    int e = Session.Dispatch(() => 3, CancellationToken.None).ConfigureAwait(false).GetAwaiter().{|BNCQ3002:GetResult|}();
                }
            }
            """);

    /// <summary>
    /// The measured case, a helper that runs its body in one dispatch and waits for it, written here in
    /// this repository's own names; and a wait inside a dispatched action, which blocks the session's
    /// thread on itself every time.
    /// </summary>
    [Fact]
    public async Task A_helper_that_waits_for_its_dispatch_fires_and_so_does_a_wait_inside_one() =>
        await Run("""
            using System;
            using System.Threading;
            using Avalonia.Headless;

            public sealed class Helpers
            {
                private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Helpers).Assembly);

                private static int OnTheUiThread(Func<int> body) =>
                    Session.Dispatch(body, CancellationToken.None).GetAwaiter().{|BNCQ3002:GetResult|}();

                public static System.Threading.Tasks.Task Nested() =>
                    Session.Dispatch(() => Session.Dispatch(() => 1, CancellationToken.None).{|BNCQ3002:Result|}, CancellationToken.None);
            }
            """);

    /// <summary>A task a local holds is followed, through another local and a conversion to <c>Task</c> too.</summary>
    [Fact]
    public async Task A_wait_on_a_local_holding_a_dispatch_fires() =>
        await Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using Avalonia.Headless;

            public sealed class Held
            {
                private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Held).Assembly);

                public void Block()
                {
                    Task<int> pending = Session.Dispatch(() => 1, CancellationToken.None);
                    Task again = pending;
                    again.{|BNCQ3002:Wait|}();
                    int value = pending.{|BNCQ3002:Result|};
                    Task.{|BNCQ3002:WaitAll|}(new[] { again });
                }
            }
            """);

    /// <summary>
    /// The fix, awaiting the dispatch or returning its task; a local the block assigns again or passes
    /// by <c>ref</c>, which may hold another task; and a wait on any other task, or on another type's
    /// <c>Dispatch</c>.
    /// </summary>
    [Fact]
    public async Task Awaited_returned_and_other_tasks_stay_silent() =>
        await Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using Avalonia.Headless;

            public sealed class Fine
            {
                private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Fine).Assembly);

                public async Task Awaited()
                {
                    int a = await Session.Dispatch(() => 1, CancellationToken.None);
                    await Session.Dispatch(() => { }, CancellationToken.None).ConfigureAwait(false);
                }

                public Task Returned() => Session.Dispatch(() => { }, CancellationToken.None);

                public void Reassigned()
                {
                    Task pending = Session.Dispatch(() => { }, CancellationToken.None);
                    pending = Task.CompletedTask;
                    pending.Wait();

                    Task byRef = Session.Dispatch(() => { }, CancellationToken.None);
                    Swap(ref byRef);
                    byRef.Wait();
                }

                public void Others()
                {
                    Task.Run(() => { }).Wait();
                    int b = Task.FromResult(1).Result;
                    Task.WaitAll(Task.Delay(1));
                    new Queue().Dispatch().Wait();
                }

                private static void Swap(ref Task task) => task = Task.CompletedTask;

                private sealed class Queue
                {
                    public Task Dispatch() => Task.CompletedTask;
                }
            }
            """);

    /// <summary>
    /// On .NET 10, <c>Task.WaitAll</c> and <c>WaitAny</c> given loose tasks bind to a
    /// <c>params ReadOnlySpan&lt;Task&gt;</c> overload, which the rule reads as it reads an array.
    /// </summary>
    [Fact]
    public async Task The_span_overloads_of_WaitAll_and_WaitAny_fire() =>
        await AvaloniaReferences.RunHeadless<HeadlessDispatchWaitAnalyzer>(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using Avalonia.Headless;

            public sealed class Spans
            {
                private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Spans).Assembly);

                public void Block()
                {
                    Task.{|BNCQ3002:WaitAll|}(Session.Dispatch(() => { }, CancellationToken.None), Task.Delay(1));
                    int first = Task.{|BNCQ3002:WaitAny|}(Task.Delay(1), Session.Dispatch(() => { }, CancellationToken.None));
                }
            }
            """,
            AvaloniaReferences.HeadlessNet100);

    private const string Blocking = """
        using System.Threading;
        using Avalonia.Headless;

        public static class Blocks
        {
            public static int Now(HeadlessUnitTestSession session) => session.Dispatch(() => 1, CancellationToken.None).Result;
        }
        """;

    /// <summary>
    /// On by default, as a warning, by plans/00010 decision 4.
    /// </summary>
    /// <remarks>
    /// ⚠ On a plain compilation, not the testing framework, which enables every diagnostic of the
    /// analyzer under test and so cannot show the default.
    /// </remarks>
    [Fact]
    public async Task By_default_a_wait_on_a_dispatch_is_a_warning()
    {
        Plain.Run run = await Plain.Analyze(new HeadlessDispatchWaitAnalyzer(), Blocking, await AvaloniaReferences.HeadlessReferences());

        Assert.Empty(run.Errors);
        Diagnostic finding = Assert.Single(run.Diagnostics);
        Assert.Equal(HeadlessDispatchWaitAnalyzer.Id, finding.Id);
        Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
    }

    [Fact]
    public async Task Generated_code_is_not_analyzed() =>
        await Run("""
            // <auto-generated/>
            using System.Threading;
            using Avalonia.Headless;

            public static class Blocks
            {
                public static int Now(HeadlessUnitTestSession session) => session.Dispatch(() => 1, CancellationToken.None).Result;
            }
            """);

    /// <summary>
    /// Where the compilation cannot name <c>Avalonia.Headless.HeadlessUnitTestSession</c>, nothing is
    /// registered, though Avalonia itself is referenced: the gate reads the headless session, not Avalonia.
    /// </summary>
    [Fact]
    public async Task Without_Avalonia_Headless_the_rule_registers_nothing()
    {
        Plain.Run inert = await Plain.Analyze(new HeadlessDispatchWaitAnalyzer(), Blocking, await AvaloniaReferences.References());
        Plain.Run control = await Plain.Analyze(new HeadlessDispatchWaitAnalyzer(), Blocking, await AvaloniaReferences.HeadlessReferences());

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
