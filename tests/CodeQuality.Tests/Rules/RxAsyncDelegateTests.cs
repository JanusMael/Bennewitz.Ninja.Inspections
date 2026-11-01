using Bennewitz.Ninja.CodeQuality.Rules;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ1006 fires on an async delegate given to a System.Reactive void delegate, and on a
/// <c>Select</c> whose tasks nothing awaits; it stays silent on the operators that await, and on
/// async delegates given to anything that is not Rx's.
/// </summary>
public sealed class RxAsyncDelegateTests
{
    private static Task Run(string source) => Rx.Run<RxAsyncDelegateAnalyzer>(source);

    [Fact]
    public Task An_async_lambda_given_to_Subscribe_fires_at_its_async_keyword() =>
        Run("""
            using System;
            using System.Threading.Tasks;

            public static class Saver
            {
                public static IDisposable Wire(IObservable<int> source) =>
                    source.Subscribe({|BNCQ1006:async|} value => await Task.Delay(value));
            }
            """);

    /// <summary>Each void delegate is judged on its own: here only the error handler is async.</summary>
    [Fact]
    public Task An_async_error_handler_fires_and_its_synchronous_sibling_does_not() =>
        Run("""
            using System;
            using System.Threading.Tasks;

            public static class Saver
            {
                public static IDisposable Wire(IObservable<int> source) =>
                    source.Subscribe(value => { }, {|BNCQ1006:async|} error => await Task.Yield());
            }
            """);

    [Fact]
    public Task Async_delegates_given_to_Do_and_Finally_fire() =>
        Run("""
            using System;
            using System.Reactive.Linq;
            using System.Threading.Tasks;

            public static class Saver
            {
                public static IObservable<int> Wire(IObservable<int> source) =>
                    source
                        .Do({|BNCQ1006:async|} delegate (int value) { await Task.Delay(value); })
                        .Finally({|BNCQ1006:async|} () => await Task.Yield());
            }
            """);

    /// <summary>A Select reports every selector that yields a task, async or not, lambda or method group.</summary>
    [Fact]
    public Task A_Select_that_yields_tasks_fires_at_its_name() =>
        Run("""
            using System;
            using System.Reactive.Linq;
            using System.Threading.Tasks;

            public static class Saver
            {
                private static Task<int> Save(int value) => Task.FromResult(value);

                public static void Wire(IObservable<int> source)
                {
                    source.{|BNCQ1006:Select|}(async value => { await Task.Yield(); return value; }).Subscribe(_ => { });
                    source.{|BNCQ1006:Select|}(value => Task.Delay(value)).Subscribe(_ => { });
                    source.{|BNCQ1006:Select|}(Save).Subscribe(_ => { });
                    source.{|BNCQ1006:Select|}((value, index) => Save(index)).Subscribe(_ => { });
                }
            }
            """);

    /// <summary>
    /// <c>Concat</c>, <c>Merge</c> and <c>Switch</c> take the observable of tasks and await each,
    /// which is the idiom for running them in order, at once, or latest only.
    /// </summary>
    [Fact]
    public Task A_Select_whose_tasks_an_operator_awaits_stays_silent() =>
        Run("""
            using System;
            using System.Reactive.Linq;
            using System.Threading.Tasks;

            public static class Saver
            {
                private static Task<int> Save(int value) => Task.FromResult(value);

                public static void Wire(IObservable<int> source)
                {
                    source.Select(Save).Concat().Subscribe(_ => { });
                    source.Select(async value => { await Task.Yield(); return value; }).Merge().Subscribe(_ => { });
                    Observable.Switch(source.Select(Save)).Subscribe(_ => { });
                }
            }
            """);

    /// <summary>The fix, and the other forms that await or never were async.</summary>
    [Fact]
    public Task The_fix_and_synchronous_handlers_stay_silent() =>
        Run("""
            using System;
            using System.Reactive.Linq;
            using System.Threading.Tasks;

            public static class Saver
            {
                private static Task Save(int value) => Task.Delay(value);

                public static void Wire(IObservable<int> source)
                {
                    source.SelectMany(value => Observable.FromAsync(() => Save(value))).Subscribe(_ => { }, _ => { });
                    source.SelectMany(async value => { await Save(value); return value; }).Subscribe(_ => { });
                    source.Select(value => value + 1).Do(value => { }).Subscribe(value => { }, error => { }, () => { });
                    Observable.Create<int>(async observer => { await Save(1); observer.OnNext(1); }).Subscribe(_ => { });
                }
            }
            """);

    /// <summary>
    /// An async lambda given to a void delegate that is not Rx's is VSTHRD101's ground, and an async
    /// void method passed as a method group is VSTHRD100's, at its declaration.
    /// </summary>
    [Fact]
    public Task Async_delegates_given_outside_Rx_and_async_void_method_groups_stay_silent() =>
        Run("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public static class Saver
            {
                private static async void Save(int value) => await Task.Delay(value);

                public static void Wire(IObservable<int> source, List<int> values)
                {
                    values.ForEach(async value => await Task.Delay(value));
                    Action later = async () => await Task.Yield();
                    source.Subscribe(Save);
                }
            }
            """);

    /// <summary>
    /// The scanner corpus's cases of this shape, by plans/00005 decision 6, in this repository's own
    /// names. The corpus expects four sites and two safe forms; every one gives its async lambda to
    /// something that is not System.Reactive's, which plans/00005 decision 5 leaves to VSTHRD101, so
    /// none is reported.
    /// </summary>
    [Fact]
    public Task The_corpus_cases_are_outside_Rx_and_stay_silent() =>
        Run("""
            using System;
            using System.Threading.Tasks;

            public sealed class Button
            {
                private readonly Action _press;
                public Button(Action press) => _press = press;
                public void Press() => _press();
            }

            public sealed class Screen
            {
                public event EventHandler? Shown;

                public void Wire()
                {
                    // Expected sites.
                    var button = new Button(async () => await LoadAsync());
                    Invoke(async () => await LoadAsync());
                    Action later = async () => await LoadAsync();
                    Shown += async (_, _) => await LoadAsync();

                    // Safe forms.
                    _ = Task.Run(async () => await LoadAsync());
                    Func<Task> awaited = async () => await LoadAsync();

                    button.Press();
                    later();
                    _ = awaited();
                }

                private static void Invoke(Action action) => action();

                private static Task LoadAsync() => Task.Delay(1);
            }
            """);

    /// <summary>
    /// A real application's nine sites of this shape, by plans/00005 decision 9, in this repository's
    /// own names, each labelled with the verdict its audit gave it. Every one gives its async lambda
    /// to the application's own command, whose constructor takes an <see cref="Action"/>: async void,
    /// and not System.Reactive's, so none is in this rule's form.
    /// </summary>
    [Fact]
    public Task Real_sites_given_to_a_command_stay_silent_whatever_their_verdict() =>
        Run("""
            using System;
            using System.Threading.Tasks;

            public sealed class Command
            {
                private readonly Action _execute;
                public Command(Action execute) => _execute = execute;
                public void Execute() => _execute();
            }

            public sealed class Commands
            {
                public Commands(Func<Task<string?>> pickSave, Func<Task<string?>> pickOpen, Func<Task> show, Action add, Action open, Func<Task> openFrom)
                {
                    Export = new Command(async () => { var path = await pickSave(); });          // unsure: the picker sits outside the try
                    Import = new Command(async () => { var path = await pickOpen(); });          // unsure
                    Add = new Command(async () => { await Task.Run(add); });                     // THROWS: the work throws on a bad pattern
                    Icons = new Command(async () => await show());                               // SAFE
                    ExportSearches = new Command(async () => { var path = await pickSave(); });  // unsure
                    ImportSearches = new Command(async () => { var path = await pickOpen(); });  // unsure
                    ExportLines = new Command(async () => { var path = await pickSave(); });     // unsure
                    Options = new Command(async () => { await Task.Run(open); });                // SAFE
                    OpenFile = new Command(async () => await openFrom());                        // SAFE
                }

                public Command Export { get; }
                public Command Import { get; }
                public Command Add { get; }
                public Command Icons { get; }
                public Command ExportSearches { get; }
                public Command ImportSearches { get; }
                public Command ExportLines { get; }
                public Command Options { get; }
                public Command OpenFile { get; }
            }
            """);

    [Fact]
    public Task Generated_code_is_not_analyzed() =>
        Run("""
            // <auto-generated/>
            using System;
            using System.Threading.Tasks;

            public static class Saver
            {
                public static IDisposable Wire(IObservable<int> source) =>
                    source.Subscribe(async value => await Task.Delay(value));
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
            using System.Threading.Tasks;

            public static class Saver
            {
                public static IDisposable Wire(IObservable<int> source) =>
                    source.Subscribe(async value => await Task.Delay(value));
            }
            """;

        Plain.Run inert = await Plain.Analyze(new RxAsyncDelegateAnalyzer(), Source, await Plain.Net80());
        Plain.Run control = await Plain.Analyze(new RxAsyncDelegateAnalyzer(), Source, await Rx.References());

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
