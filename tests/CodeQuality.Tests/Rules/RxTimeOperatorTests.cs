using Bennewitz.Ninja.CodeQuality.Rules;
using Microsoft.CodeAnalysis;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ1005 is off until enabled; enabled, it fires on each measured time operator called with no
/// scheduler, and on nothing given a scheduler, nothing that waits on no timer, and no operator that
/// was not measured.
/// </summary>
public sealed class RxTimeOperatorTests
{
    private static Task Run(string source) => Rx.Run<RxTimeOperatorAnalyzer>(source);

    [Fact]
    public Task Every_measured_time_operator_fires_without_a_scheduler() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static void Wire(IObservable<int> source)
                {
                    TimeSpan step = TimeSpan.FromMilliseconds(50);
                    source.{|BNCQ1005:Throttle|}(step).Subscribe(_ => { });
                    source.{|BNCQ1005:Sample|}(step).Subscribe(_ => { });
                    Observable.{|BNCQ1005:Interval|}(step).Subscribe(_ => { });
                    Observable.{|BNCQ1005:Timer|}(step).Subscribe(_ => { });
                    Observable.{|BNCQ1005:Timer|}(DateTimeOffset.Now, step).Subscribe(_ => { });
                    source.{|BNCQ1005:Delay|}(step).Subscribe(_ => { });
                    source.{|BNCQ1005:Buffer|}(step).Subscribe(_ => { });
                    source.{|BNCQ1005:Buffer|}(step, 10).Subscribe(_ => { });
                    source.{|BNCQ1005:Window|}(step).Subscribe(_ => { });
                    source.{|BNCQ1005:Timeout|}(step).Subscribe(_ => { }, _ => { });
                    source.{|BNCQ1005:Timeout|}(DateTimeOffset.Now.AddSeconds(5)).Subscribe(_ => { }, _ => { });
                }
            }
            """);

    /// <summary>The operator called as a static method is the same call.</summary>
    [Fact]
    public Task A_time_operator_called_as_a_static_method_fires() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static IObservable<int> Quiet(IObservable<int> source) =>
                    Observable.{|BNCQ1005:Throttle|}(source, TimeSpan.FromMilliseconds(50));
            }
            """);

    /// <summary>A generic call is reported at the name, not its type arguments.</summary>
    [Fact]
    public Task A_time_operator_with_explicit_type_arguments_fires_at_its_name() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static IObservable<int> Quiet(IObservable<int> source) =>
                    source.{|BNCQ1005:Delay|}<int>(TimeSpan.FromMilliseconds(50));
            }
            """);

    /// <summary>The fix: every operator given a scheduler, the default one included.</summary>
    [Fact]
    public Task Every_time_operator_given_a_scheduler_stays_silent() =>
        Run("""
            using System;
            using System.Reactive.Concurrency;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static void Wire(IObservable<int> source, IScheduler ui)
                {
                    TimeSpan step = TimeSpan.FromMilliseconds(50);
                    source.Throttle(step, ui).Subscribe(_ => { });
                    source.Sample(step, ui).Subscribe(_ => { });
                    Observable.Interval(step, ui).Subscribe(_ => { });
                    Observable.Timer(step, ui).Subscribe(_ => { });
                    source.Delay(step, Scheduler.Default).Subscribe(_ => { });
                    source.Buffer(step, ui).Subscribe(_ => { });
                    source.Buffer(step, 10, ui).Subscribe(_ => { });
                    source.Window(step, ui).Subscribe(_ => { });
                    source.Timeout(step, ThreadPoolScheduler.Instance).Subscribe(_ => { }, _ => { });
                }
            }
            """);

    /// <summary>The same operators by count, by a sampler or by a selector wait on no timer.</summary>
    [Fact]
    public Task The_same_operators_not_by_time_stay_silent() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static void Wire(IObservable<int> source, IObservable<long> ticks)
                {
                    source.Buffer(10).Subscribe(_ => { });
                    source.Window(10, 5).Subscribe(_ => { });
                    source.Sample(ticks).Subscribe(_ => { });
                    source.Throttle(_ => ticks).Subscribe(_ => { });
                    source.Delay(_ => ticks).Subscribe(_ => { });
                    source.Timeout(_ => ticks).Subscribe(_ => { }, _ => { });
                }
            }
            """);

    /// <summary>
    /// Operators that take a time but were not measured to deliver on the scheduler, and a scheduler
    /// chosen by <c>ObserveOn</c>, are not this rule's.
    /// </summary>
    [Fact]
    public Task Unmeasured_operators_and_a_chosen_scheduler_stay_silent() =>
        Run("""
            using System;
            using System.Reactive.Concurrency;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static void Wire(IObservable<int> source)
                {
                    TimeSpan window = TimeSpan.FromSeconds(1);
                    source.Replay(window).RefCount().Subscribe(_ => { });
                    source.Skip(window).Subscribe(_ => { });
                    source.Take(window).Subscribe(_ => { });
                    source.ObserveOn(ThreadPoolScheduler.Instance).Subscribe(_ => { });
                }
            }
            """);

    /// <summary>What a consumer writes to turn the rule on.</summary>
    private const string Enabled = """
        root = true

        [*.cs]
        dotnet_diagnostic.BNCQ1005.severity = warning
        """;

    private const string Throttled = """
        using System;
        using System.Reactive.Linq;

        public static class Feeds
        {
            public static IObservable<int> Quiet(IObservable<int> source) => source.Throttle(TimeSpan.FromMilliseconds(50));
        }
        """;

    /// <summary>
    /// Off until enabled: on a real application's code most findings had nothing downstream that
    /// could throw, so plans/00005 decision 3 ships the rule off.
    /// </summary>
    /// <remarks>
    /// ⚠ On a plain compilation, not the testing framework, which enables every diagnostic of the
    /// analyzer under test.
    /// </remarks>
    [Fact]
    public async Task Until_enabled_a_time_operator_without_a_scheduler_is_not_reported()
    {
        Plain.Run run = await Plain.Analyze(new RxTimeOperatorAnalyzer(), Throttled, await Rx.References());

        Assert.Empty(run.Errors);
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Enabled_in_editorconfig_a_time_operator_without_a_scheduler_is_reported()
    {
        Plain.Run run = await Plain.Analyze(new RxTimeOperatorAnalyzer(), Throttled, await Rx.References(), Enabled);

        Assert.Empty(run.Errors);
        Diagnostic finding = Assert.Single(run.Diagnostics);
        Assert.Equal(RxTimeOperatorAnalyzer.Id, finding.Id);
        Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
    }

    /// <summary>
    /// The scanner corpus's cases of this shape, by plans/00005 decision 6, in this repository's own
    /// names. The corpus expects five sites; the rule reports the three that are operators given no
    /// scheduler. <c>ObserveOn(Scheduler.Default)</c> and a <c>?? Scheduler.Default</c> fallback are
    /// schedulers someone chose, which the plan's dismissed list leaves out. The three safe forms stay
    /// silent.
    /// </summary>
    [Fact]
    public Task The_corpus_cases_get_the_corpus_verdicts() =>
        Run("""
            using System;
            using System.Reactive.Concurrency;
            using System.Reactive.Linq;
            using System.Reactive.Subjects;
            using System.Threading.Tasks;

            public sealed class Keys
            {
                private readonly Subject<int> _pressed = new();
                private readonly IScheduler _pool = TaskPoolScheduler.Default;

                // Expected sites.
                public IDisposable Debounced() => _pressed.{|BNCQ1005:Throttle|}(TimeSpan.FromMilliseconds(250)).Subscribe(Use);
                public IDisposable OnDefault() => _pressed.ObserveOn(Scheduler.Default).Subscribe(Use);
                public IDisposable Ticking() => Observable.{|BNCQ1005:Interval|}(TimeSpan.FromSeconds(1)).Subscribe(_ => Use(0));
                public IDisposable Bounded() => _pressed.{|BNCQ1005:Timeout|}(TimeSpan.FromSeconds(5)).Subscribe(Use);
                public IScheduler Chosen(IScheduler? scheduler) => scheduler ?? Scheduler.Default;

                // Safe forms.
                public IDisposable DebouncedOnPool() => _pressed.Throttle(TimeSpan.FromMilliseconds(250), _pool).Subscribe(Use);
                public IDisposable Batched() => _pressed.Buffer(10).Subscribe(batch => Use(batch.Count));
                public Task Waited() => Task.Delay(TimeSpan.FromSeconds(1));

                private static void Use(int value) { }
            }
            """);

    /// <summary>
    /// A real application's five sites in this rule's form, by plans/00005 decision 9, in this
    /// repository's own names, each labelled with the verdict its audit gave it. The rule reports all
    /// five: two whose downstream throws on real input and ended the process, and three whose
    /// downstream cannot throw.
    /// </summary>
    [Fact]
    public Task Real_sites_by_time_without_a_scheduler_fire_whatever_their_verdict() =>
        Run("""
            using System;
            using System.Linq;
            using System.Reactive.Linq;

            public sealed record Page(int Top, int Count);
            public sealed record Change(string Text);

            public static class Screens
            {
                // SAFE: the page read downstream retries, and the subscriber edits a cache.
                public static IObservable<Page> Paged(IObservable<int> lines, IObservable<int> scroll) =>
                    lines.CombineLatest(scroll, (count, top) => new Page(top, count))
                        .{|BNCQ1005:Sample|}(TimeSpan.FromMilliseconds(50))
                        .Select(page => page with { Count = Math.Min(page.Count, 100) })
                        .Retry();

                // THROWS: the writer at the end throws on a control character in the text, and the process ended.
                public static IDisposable Saved(IObservable<string[]> terms, Action<string[]> write) =>
                    terms.Select(all => all.ToArray())
                        .{|BNCQ1005:Throttle|}(TimeSpan.FromMilliseconds(250))
                        .Select(all => all.Select(term => term.Trim()).ToArray())
                        .Subscribe(write);

                // SAFE: downstream builds a predicate in memory.
                public static IObservable<Func<string, bool>> Filter(IObservable<string> typed) =>
                    typed.{|BNCQ1005:Throttle|}(TimeSpan.FromMilliseconds(250))
                        .Select(text => (Func<string, bool>)(name => name.Contains(text)));

                // SAFE: the hint built downstream catches its own failure.
                public static IObservable<string> Hints(IObservable<string> requests, Func<string, string> describe) =>
                    requests.Publish(shared => shared.Take(1).Merge(shared.Skip(1).{|BNCQ1005:Throttle|}(TimeSpan.FromMilliseconds(250))))
                        .Select(describe);

                // THROWS: the writer behind the subscriber throws on a control character, and the process ended.
                public static IDisposable Marked(IObservable<int> icon, IObservable<int> colour, IObservable<int> casing, string text, Action<Change> mark) =>
                    icon.Merge(colour).Merge(casing)
                        .{|BNCQ1005:Throttle|}(TimeSpan.FromMilliseconds(250))
                        .Select(_ => new Change(text))
                        .Subscribe(mark);
            }
            """);

    /// <summary>
    /// The same application's sites that take a scheduler: four that fall back to
    /// <c>Scheduler.Default</c> when none is passed, one of them THROWS, and eighteen on a named
    /// pool scheduler, seven of them THROWS. A scheduler someone chose is not this rule's, whatever
    /// the verdict, so all stay silent.
    /// </summary>
    [Fact]
    public Task Real_sites_given_a_scheduler_stay_silent_whatever_their_verdict() =>
        Run("""
            using System;
            using System.Reactive.Concurrency;
            using System.Reactive.Linq;

            public sealed class Watching
            {
                private readonly IScheduler _scheduler;

                // SAFE: the periodic poll's whole body is one try/catch.
                public static IObservable<long> Watch(TimeSpan period, IScheduler? scheduler = null)
                {
                    scheduler = scheduler ?? Scheduler.Default;
                    return Observable.Interval(period, scheduler);
                }

                // SAFE: the scheduler goes to Watch.
                public Watching(IScheduler? scheduler = null)
                {
                    scheduler = scheduler ?? Scheduler.Default;
                    _scheduler = scheduler;
                }

                // THROWS: the search downstream reads a file that can go mid-search.
                public IObservable<int> Search(IObservable<string> segments, Func<string, int> read, IScheduler? scheduler = null)
                {
                    IScheduler chosen = scheduler ?? Scheduler.Default;
                    return segments.ObserveOn(chosen).Select(read);
                }

                // SAFE: the scan runs inside a try/catch that logs.
                public IDisposable Index(Action scan, IScheduler? scheduler = null)
                {
                    scheduler = scheduler ?? Scheduler.Default;
                    return scheduler.Schedule(scan);
                }
            }

            public sealed class Pooled
            {
                private readonly IScheduler _background = TaskPoolScheduler.Default;
                private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

                public IObservable<int> Marks(IObservable<int> counts) => counts.Sample(Interval, _background).Select(count => count * 2);   // SAFE
                public IDisposable Newest(Func<string> find) => _background.Schedule(() => find());                                       // SAFE
                public IDisposable Requested(IObservable<string> requests, Action<string> add) => requests.ObserveOn(_background).Subscribe(add); // THROWS
                public IObservable<int> Alerts(IObservable<int> matched) => matched.Sample(Interval, _background).Scan(0, (sum, x) => sum + x); // SAFE
                public IObservable<int> Inline(IObservable<int> requests) => requests.Do(_ => { }).ObserveOn(_background);                  // SAFE
                public IDisposable FindNext(Action find) => _background.Schedule(find);                                                    // THROWS
                public IDisposable FindPrevious(Action find) => _background.Schedule(find);                                                // THROWS
                public IDisposable Export(Action export) => _background.Schedule(export);                                                  // SAFE
                public IDisposable Summarise(Action summarise) => _background.Schedule(summarise);                                         // SAFE
                public IDisposable AsYouType(IObservable<string> typed, Action find) => typed.Throttle(Interval, _background).Subscribe(_ => find()); // THROWS
                public IObservable<string> Preview(IObservable<string> typed) => typed.Throttle(Interval, _background);                    // SAFE
                public IObservable<int> Selected(IObservable<int> latest) => latest.ObserveOn(_background);                                // SAFE
                public IObservable<int> Unread(IObservable<int> counts) => counts.Scan(0, (sum, x) => sum + x).Sample(Interval, _background); // SAFE
                public IDisposable Restore(Action restore) => _background.Schedule(() => { try { restore(); } finally { } });              // THROWS
                public IObservable<string> States(IObservable<string> changes) => changes.Throttle(Interval, _background);                 // THROWS
                public IDisposable Written(IObservable<string> states, Action<string> write) => states.ObserveOn(_background).Subscribe(write); // THROWS
                public IDisposable Open(Action open) => _background.Schedule(() => { try { open(); } catch (Exception) { } });             // SAFE
                public IDisposable Views(Action open) => _background.Schedule(() => { try { open(); } catch (Exception) { } });            // SAFE
            }
            """);

    [Fact]
    public Task Generated_code_is_not_analyzed() =>
        Run("""
            // <auto-generated/>
            using System;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static IObservable<int> Quiet(IObservable<int> source) =>
                    source.Throttle(TimeSpan.FromMilliseconds(50));
            }
            """);

    /// <summary>
    /// Where the compilation cannot name <c>System.Reactive.Linq.Observable</c>, nothing is
    /// registered.
    /// </summary>
    [Fact]
    public async Task Without_System_Reactive_the_rule_registers_nothing()
    {
        const string Source = """
            using System;
            using System.Reactive.Linq;

            public static class Feeds
            {
                public static IObservable<long> Ticks() => Observable.Interval(TimeSpan.FromSeconds(1));
            }
            """;

        Plain.Run inert = await Plain.Analyze(new RxTimeOperatorAnalyzer(), Source, await Plain.Net80(), Enabled);
        Plain.Run control = await Plain.Analyze(new RxTimeOperatorAnalyzer(), Source, await Rx.References(), Enabled);

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
