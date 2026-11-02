using Bennewitz.Ninja.CodeQuality.Rules;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ1008 fires on a gate released outside a finally clause in a body that takes it, and on no
/// release in a finally, no release whose gate the body does not take, and no other body's.
/// </summary>
public sealed class GateReleaseTests
{
    private static Task Run(string source) => RuleTest<GateReleaseAnalyzer>.Run(source);

    [Fact]
    public Task A_semaphore_released_after_the_work_fires_at_the_release() =>
        Run("""
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Saver
            {
                private readonly SemaphoreSlim _gate = new(1, 1);

                public async Task SaveAsync()
                {
                    await _gate.WaitAsync();
                    Write();
                    _gate.{|BNCQ1008:Release|}();
                }

                private static void Write() { }
            }
            """);

    /// <summary>Every gate type, each taken and released in the open.</summary>
    [Fact]
    public Task Every_gate_type_fires() =>
        Run("""
            using System.Threading;

            public sealed class Gates
            {
                private readonly object _lock = new();

                public void Run(SemaphoreSlim slim, Semaphore semaphore, Mutex mutex, ReaderWriterLockSlim rw)
                {
                    slim.Wait();
                    Work();
                    slim.{|BNCQ1008:Release|}();

                    semaphore.WaitOne();
                    Work();
                    semaphore.{|BNCQ1008:Release|}();

                    mutex.WaitOne();
                    Work();
                    mutex.{|BNCQ1008:ReleaseMutex|}();

                    Monitor.Enter(_lock);
                    Work();
                    Monitor.{|BNCQ1008:Exit|}(_lock);

                    rw.EnterWriteLock();
                    Work();
                    rw.{|BNCQ1008:ExitWriteLock|}();

                    if (rw.TryEnterReadLock(100))
                    {
                        Work();
                        rw.{|BNCQ1008:ExitReadLock|}();
                    }
                }

                private static void Work() { }
            }
            """);

    /// <summary>A lambda is a body of its own, held to the same rule when it takes the gate itself.</summary>
    [Fact]
    public Task A_release_in_a_lambda_that_takes_the_gate_fires() =>
        Run("""
            using System;
            using System.Threading;

            public static class Jobs
            {
                public static Action Job(SemaphoreSlim gate) => () =>
                {
                    gate.Wait();
                    Console.WriteLine("work");
                    gate.{|BNCQ1008:Release|}();
                };
            }
            """);

    /// <summary>The fix: the release in the finally of a try that starts once the gate is taken.</summary>
    [Fact]
    public Task A_release_in_a_finally_stays_silent() =>
        Run("""
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Saver
            {
                private readonly SemaphoreSlim _gate = new(1, 1);
                private readonly object _lock = new();

                public async Task SaveAsync()
                {
                    await _gate.WaitAsync();
                    try
                    {
                        Write();
                    }
                    finally
                    {
                        _gate.Release();
                    }
                }

                public void Locked()
                {
                    bool taken = false;
                    try
                    {
                        Monitor.Enter(_lock, ref taken);
                        Write();
                    }
                    finally
                    {
                        if (taken)
                        {
                            Monitor.Exit(_lock);
                        }
                    }
                }

                private static void Write() { }
            }
            """);

    /// <summary>
    /// A release whose gate this body does not take: one released for a caller, and one taken in
    /// the method and released in a continuation it schedules, each body judged on its own.
    /// </summary>
    [Fact]
    public Task A_release_in_a_body_that_does_not_take_the_gate_stays_silent() =>
        Run("""
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Handoff
            {
                private readonly SemaphoreSlim _gate = new(1, 1);
                private readonly SemaphoreSlim _other = new(1, 1);

                public void Done() => _gate.Release();

                public Task Start()
                {
                    _gate.Wait();
                    return Task.Run(() =>
                    {
                        Work();
                        _gate.Release();
                    });
                }

                public void Mixed()
                {
                    _other.Wait();
                    try { Work(); } finally { _other.Release(); }
                    _gate.Release();
                }

                private static void Work() { }
            }
            """);

    /// <summary>
    /// The scanner corpus's cases of this shape, by plans/00005 decision 6, in this repository's own
    /// names: the one expected site, whose mark the corpus puts on the line that takes the gate and
    /// the rule reports at the release two lines on, and the one safe form.
    /// </summary>
    [Fact]
    public Task The_corpus_cases_get_the_corpus_verdicts() =>
        Run("""
            using System.IO;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Store
            {
                private readonly SemaphoreSlim _lock = new(1, 1);

                // Expected site.
                public async Task Put()
                {
                    await _lock.WaitAsync();
                    Flush();
                    _lock.{|BNCQ1008:Release|}();
                }

                // Safe form.
                public async Task PutSafely()
                {
                    await _lock.WaitAsync();
                    try
                    {
                        Flush();
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }

                private static void Flush() => File.ReadAllText("store.txt");
            }
            """);

    [Fact]
    public Task Generated_code_is_not_analyzed() =>
        Run("""
            // <auto-generated/>
            using System.Threading;

            public sealed class Saver
            {
                private readonly SemaphoreSlim _gate = new(1, 1);

                public void Save()
                {
                    _gate.Wait();
                    _gate.Release();
                }
            }
            """);

    /// <summary>Where the compilation can name none of the gate types, nothing is registered.</summary>
    [Fact]
    public async Task Without_the_gate_types_the_rule_registers_nothing()
    {
        const string Source = """
            public sealed class Saver
            {
                private readonly System.Threading.SemaphoreSlim _gate = new(1, 1);

                public void Save()
                {
                    _gate.Wait();
                    _gate.Release();
                }
            }
            """;

        Plain.Run inert = await Plain.Analyze(new GateReleaseAnalyzer(), Source, []);
        Plain.Run control = await Plain.Analyze(new GateReleaseAnalyzer(), Source, await Plain.Net80());

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Single(control.Diagnostics);
    }
}
