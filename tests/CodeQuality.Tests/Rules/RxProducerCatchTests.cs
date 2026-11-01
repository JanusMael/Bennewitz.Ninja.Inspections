using Bennewitz.Ninja.CodeQuality.Rules;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ1007 fires on the <c>OnNext</c> of <c>Observable.Create</c>'s observer inside a try whose catch
/// carries on, and on no catch that cannot, no other observer and no subject.
/// </summary>
public sealed class RxProducerCatchTests
{
    private static Task Run(string source) => Rx.Run<RxProducerCatchAnalyzer>(source);

    [Fact]
    public Task A_catch_that_logs_around_OnNext_fires_at_OnNext() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<int> Poll(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        try
                        {
                            observer.{|BNCQ1007:OnNext|}(read());
                        }
                        catch (Exception error)
                        {
                            Console.Error.WriteLine(error);
                        }

                        return () => { };
                    });
            }
            """);

    /// <summary>
    /// Handing the caught exception to <c>OnError</c> is the same defect: the observer it is handed
    /// to was detached when the subscriber threw.
    /// </summary>
    [Fact]
    public Task A_catch_that_hands_the_error_to_OnError_fires() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<string> Read(Func<string> read) =>
                    Observable.Create<string>(observer =>
                    {
                        try
                        {
                            string text = read();
                            observer.{|BNCQ1007:OnNext|}(text);
                            observer.OnCompleted();
                        }
                        catch (Exception error)
                        {
                            observer.OnError(error);
                        }

                        return () => { };
                    });
            }
            """);

    /// <summary>A catch that continues its loop carries on as surely as one that falls through.</summary>
    [Fact]
    public Task A_catch_that_continues_a_polling_loop_fires() =>
        Run("""
            using System;
            using System.IO;
            using System.Reactive.Linq;
            using System.Threading.Tasks;

            public static class Readings
            {
                public static IObservable<int> Poll(Func<int> read) =>
                    Observable.Create<int>(async (observer, token) =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            await Task.Delay(50, token);
                            try
                            {
                                observer.{|BNCQ1007:OnNext|}(read());
                            }
                            catch (IOException)
                            {
                                continue;
                            }
                        }
                    });
            }
            """);

    /// <summary>The observer captured by a lambda inside the producer is still the producer's.</summary>
    [Fact]
    public Task A_catch_in_a_lambda_inside_the_producer_fires() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<int> Forward(IObservable<int> source) =>
                    Observable.Create<int>(observer =>
                        source.Subscribe(value =>
                        {
                            try { observer.{|BNCQ1007:OnNext|}(value); }
                            catch (Exception) { }
                        }));
            }
            """);

    /// <summary>The fix: the catch covers only the read, and <c>OnNext</c> is outside it.</summary>
    [Fact]
    public Task A_catch_around_the_read_alone_stays_silent() =>
        Run("""
            using System;
            using System.IO;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<string> Read(Func<string> read) =>
                    Observable.Create<string>(observer =>
                    {
                        string text;
                        try
                        {
                            text = read();
                        }
                        catch (IOException error)
                        {
                            observer.OnError(error);
                            return () => { };
                        }

                        observer.OnNext(text);
                        return () => { };
                    });
            }
            """);

    /// <summary>A catch that rethrows, returns or breaks out cannot carry on.</summary>
    [Fact]
    public Task Catches_that_cannot_carry_on_stay_silent() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<int> Rethrow(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        try { observer.OnNext(read()); }
                        catch (Exception error) { Console.Error.WriteLine(error); throw; }
                        return () => { };
                    });

                public static IObservable<int> Return(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        try { observer.OnNext(read()); }
                        catch (Exception) { return () => { }; }
                        return () => { };
                    });

                public static IObservable<int> Break(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            try { observer.OnNext(read()); }
                            catch (Exception) { break; }
                        }

                        return () => { };
                    });
            }
            """);

    /// <summary>
    /// <c>OnNext</c> in the catch or the finally, and a try around the whole <c>Create</c>, are not
    /// a catch of what <c>OnNext</c> throws inside the producer.
    /// </summary>
    [Fact]
    public Task OnNext_outside_a_try_block_of_the_producer_stays_silent() =>
        Run("""
            using System;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<int> Fallback(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        try { read(); }
                        catch (Exception) { observer.OnNext(-1); }
                        finally { observer.OnNext(0); }
                        return () => { };
                    });

                public static IObservable<int> Outer(Func<int> read)
                {
                    try
                    {
                        return Observable.Create<int>(observer => { observer.OnNext(read()); return () => { }; });
                    }
                    catch (Exception)
                    {
                        return Observable.Empty<int>();
                    }
                }
            }
            """);

    /// <summary>A catch around a subject's <c>OnNext</c> is a guard: the subject's other subscribers kept every value.</summary>
    [Fact]
    public Task A_catch_around_a_subject_or_another_observer_stays_silent() =>
        Run("""
            using System;
            using System.Reactive.Linq;
            using System.Reactive.Subjects;

            public static class Readings
            {
                public static void Publish(Subject<int> subject, IObserver<int> other, int value)
                {
                    try { subject.OnNext(value); }
                    catch (Exception) { }

                    Observable.Create<int>(observer =>
                    {
                        try { other.OnNext(value); }
                        catch (Exception) { }
                        observer.OnCompleted();
                        return () => { };
                    });
                }
            }
            """);

    /// <summary>
    /// The scanner corpus's one case of this shape, by plans/00005 decision 6, in this repository's
    /// own names: a catch of one exception type that hands it to <c>OnError</c>. The corpus has no
    /// safe form; the safe forms are the tests above.
    /// </summary>
    [Fact]
    public Task The_corpus_case_fires() =>
        Run("""
            using System;
            using System.IO;
            using System.Reactive.Linq;

            public static class Counter
            {
                public static IObservable<int> Count(string path) => Observable.Create<int>(observer =>
                {
                    try
                    {
                        observer.{|BNCQ1007:OnNext|}(File.ReadAllText(path).Length);
                    }
                    catch (IOException error)
                    {
                        observer.OnError(error);
                    }

                    return () => { };
                });
            }
            """);

    /// <summary>
    /// A real application's one site of this shape, by plans/00005 decision 9, in this repository's
    /// own names. THROWS: a subscriber's failed read lands in the catch after Rx has disposed the
    /// subscription, the catch's own <c>OnNext</c> goes to a dead observer, and the file is watched
    /// no more for any subscriber. The <c>OnNext</c> in the catch is outside a try block, so only the
    /// first is reported.
    /// </summary>
    [Fact]
    public Task The_real_site_fires_at_the_OnNext_in_its_try() =>
        Run("""
            using System;
            using System.IO;
            using System.Reactive.Concurrency;
            using System.Reactive.Linq;

            public sealed record Notice(string Path, long Length, Exception? Error = null);

            public static class Watching
            {
                public static IObservable<Notice> Watch(FileInfo file, TimeSpan period, IScheduler scheduler) =>
                    Observable.Create<Notice>(observer =>
                        scheduler.SchedulePeriodic(period, () =>
                        {
                            try
                            {
                                file.Refresh();
                                Notice notice = new(file.FullName, file.Length);
                                observer.{|BNCQ1007:OnNext|}(notice);
                            }
                            catch (Exception error)
                            {
                                observer.OnNext(new Notice(file.FullName, 0, error));
                            }
                        }));
            }
            """);

    [Fact]
    public Task Generated_code_is_not_analyzed() =>
        Run("""
            // <auto-generated/>
            using System;
            using System.Reactive.Linq;

            public static class Readings
            {
                public static IObservable<int> Poll(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        try { observer.OnNext(read()); }
                        catch (Exception) { }
                        return () => { };
                    });
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

            public static class Readings
            {
                public static IObservable<int> Poll(Func<int> read) =>
                    Observable.Create<int>(observer =>
                    {
                        try { observer.OnNext(read()); }
                        catch (Exception) { }
                        return () => { };
                    });
            }
            """;

        Plain.Run inert = await Plain.Analyze(new RxProducerCatchAnalyzer(), Source, await Plain.Net80());
        Plain.Run control = await Plain.Analyze(new RxProducerCatchAnalyzer(), Source, await Rx.References());

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
