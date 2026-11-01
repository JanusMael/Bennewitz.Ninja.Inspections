# 00005 — CodeQuality rules for the exception shapes a compiler can see

> Status: **approved 2026-11-01**. Supersedes nothing.

QuailTail's catalogue of 2026-10-30 named 32 shapes in which one exception from ordinary input kills a
feature, vanishes or ends the process, with a text scanner that lists the candidate sites of each. The
developer decided on 2026-10-31 that the scanner is not taken, and that a plan turns the catalogue into
CodeQuality rules instead, held to the scanner's self-test corpus. The shapes and what was measured of
each are in [`docs/exception-resilience.md`](../docs/exception-resilience.md), merged in PR #114.

Most of the shapes are not rules. Whether a subscription's handler, a `ReplaySubject`'s subscriber or a
`Lazy` factory can throw on real input is decided by reading what it calls, which is why the scanner
lists sites for a person to judge rather than findings. A rule is right where the defect is in the
shape itself, so that the fix can be seen in the source and a finding is never a guess about what some
callee might do. Eleven of the 32 shapes are like that, in six rules: `RX-SCHEDULER`, the Rx half of
`ASYNC-LAMBDA`, `RX-PRODUCER-CATCH`, `GATE-LEFT-SHUT`, `TIMER-THREADING`, `TIMER-TIMERS`, `THREAD`,
`WATCHER-HANDLER`, `PROCESS-HANDLER` and `FINALIZER` together, and `TIMER-DISPATCHER`, which is
Avalonia's. Three more, `ASYNC-VOID`, `ASYNC-VOID-HANDLER` and `FIRE-AND-FORGET`, wait for decision 5.
Measured on 2026-10-31 with `AnalysisMode=All`, no analyzer of the .NET SDK reports a site of any of the
ten that are not Avalonia's.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Six rules, `BNCQ1005` to `BNCQ1009` and `BNCQ3001`, below.** Each reports a shape whose fix is visible at the site, and each links to its entry in `docs/exception-resilience.md`, or `BNCQ3001` to its entry in `docs/avalonia-gotchas.md` | Those entries hold the measurement behind each rule, so the rule's section can stay short and point there |
| 2 | **A rule with no AssemblyQuality counterpart takes the next free number, as the checklist says, and a rule about Avalonia takes the next free number from `BNCQ3001`, the developer's decisions of 2026-11-01.** The promise that a rule carries its counterpart's number becomes: a rule's section names its counterpart, if it has one. `docs/codequality.md`'s opening, `Catalog`'s remarks and the checklist say so, and name the Avalonia range. `BNCQ1001`, `BNCQ1002` and `BNCQ1004` keep their counterparts' numbers | `BNAQ1005` and `BNAQ1006` have no source counterpart, so `BNCQ1005` and `BNAQ1005` share a number and nothing else, which the old wording would have promised otherwise. A later `BNAQ` rule that gains a source counterpart may get a different `BNCQ` number, and its section says which. A range of their own lets a project see, and turn off, the rules about a framework it may not use |
| 3 | **All six are on by default, as warnings, unless real code says otherwise, the developer's decision of 2026-11-01.** A rule more than half of whose findings on real code, the family repositories and the fixtures of decision 9, are judged SAFE ships off by default instead, and `PROGRESS.md` records the counts | Each reports a site where one exception ends the process, swallows itself, stops a timer or leaves a gate shut, and the fix is local. `BNCQ1001` is off because it is a policy the BCL itself does not follow; none of these is. A rule that is mostly noise on real code teaches people to suppress it |
| 4 | **The analyzer stays as it is built**: `netstandard2.0`, no package dependency, Roslyn `RoslynVersion`, every rule a `ScopedAnalyzer<TScope>`. System.Reactive's and Avalonia's types are found by metadata name. The three Rx rules are inert in a compilation that cannot name `System.Reactive.Linq.Observable`, `BNCQ3001` in one that cannot name `Avalonia.Threading.DispatcherTimer`, and the other two in one that cannot name the types they look for | The family's invariants. A package reference to System.Reactive or Avalonia would not be packed beside the analyzer and would fail as `AD0001` |
| 5 | **Nothing on the ground `Microsoft.VisualStudio.Threading.Analyzers` covers**: async void (VSTHRD100), an async lambda given to a void delegate outside Rx (VSTHRD101), a task nobody awaits (VSTHRD110). They are taken up after that package is measured, by the developer's decisions of 2026-10-31 and 2026-11-01 | Two analyzers reporting one site under two IDs is a cost every consumer pays in suppressions. `BNCQ1006` covers only the Rx operators, which VSTHRD101 reads as ordinary delegates |
| 6 | **The corpus becomes fixtures of this repository's own, the developer's decision of 2026-11-01.** The corpus is asked of QuailTail and read. For each expected site and each safe form of a shape a rule covers, the rule's tests gain a case written here that expresses the same shape in this repository's own code and names, firing or silent as the corpus expects, and proven to exercise the rule as decision 9's fixtures are. The corpus itself is run through the rules once, in a scratch project, to show that each of its cases has a counterpart among the fixtures and gets the same verdict; none of it is committed | The checklist lets another project's code be read for ideas and traps and never copied, so the shapes come in and the text does not. Committed, the fixtures hold the rules to the corpus in every pull request, not once |
| 7 | **An operator or callback a rule names is one measured here to end the process, swallow a throw or stop the feature.** `BNCQ1005`'s operators and `BNCQ1009`'s callbacks are the ones `docs/exception-resilience.md` records; one not yet measured is measured on Windows and under WSL 2 before a rule names it, and its result goes into that document in the same pull request | A rule that reports a site is a claim about what a throw there does, and the document's own rule is that a claim is measured here before it lands |
| 8 | **Avalonia's `DispatcherTimer.Tick` is a rule of its own, `BNCQ3001`, the developer's decisions of 2026-11-01.** It reports what `BNCQ1009` reports, for a `Tick` handler | Measured on Windows on 2026-10-30: one throw in `Tick` stops the timer for good while `IsEnabled` reads true, and `Dispatcher.UnhandledException` never hears of it. It is the first framework type CodeQuality names, and its own ID lets a project turn it off without touching `BNCQ1009` |
| 9 | **QuailTail's real code reaches the tests the way the corpus does, the developer's decision of 2026-11-01.** No family repository uses System.Reactive or DynamicData, so QuailTail is asked for its own sites of each shape a rule covers, with the verdict its audit gave each, THROWS or SAFE, and enough of the surrounding code to show the shape. Each becomes a fixture written here in the same shape, in this repository's names, labelled with that verdict and committed with the rule's tests. Every fixture is proven to exercise its rule: a firing one fails its test when the rule's gate is forced shut, and a silent one when the rule is made to report every candidate, each seen once | The real code stays QuailTail's and only its shapes come in, as decision 6 holds for the corpus. Committed, the fixtures keep the rules honest against a real application's code in every pull request, and the verdicts give decision 3 a count for the Rx rules |

### The rules

| Id | Reports | Fix | Entry |
|---|---|---|---|
| `BNCQ1005` | A call to `Observable.Throttle`, `Sample`, `Interval`, `Timer`, `Delay`, `Buffer` or `Window` by time, or `Timeout`, through an overload with no `IScheduler` parameter. These are the operators measured to deliver on Rx's default scheduler, where a throw, or `Timeout`'s own exception, ends the process; another joins the list when it is measured, by decision 7 | Pass a scheduler | the time operator |
| `BNCQ1006` | An async lambda, async anonymous method or `Task`-returning method group given to a System.Reactive operator that does not await it: an `Action` parameter of `ObservableExtensions` or `Observable` (`Subscribe`, `Do`, `Finally`), and a `Select` whose selector returns `Task` or `Task<T>` | `SelectMany` with `Observable.FromAsync`, or a synchronous handler | the async lambda |
| `BNCQ1007` | A call to `OnNext` on the observer `Observable.Create` hands its delegate, inside the `try` block of a try statement with a catch clause whose end is reachable | Catch only the producer's own work, outside the `OnNext` call | the producer that catches |
| `BNCQ1008` | A release of a `SemaphoreSlim`, `Semaphore`, `Mutex`, `Monitor` or `ReaderWriterLockSlim` outside a `finally` clause, in a body that acquires the same field, local or parameter | `try { … } finally { release }` | the semaphore |
| `BNCQ1009` | A body given to a callback no caller catches, and not wholly inside one try statement whose catch takes `Exception` or everything: a `System.Threading.Timer` callback, a `Thread`'s start, a `ThreadPool.QueueUserWorkItem` callback, a `System.Timers.Timer.Elapsed` handler, a `FileSystemWatcher` `Created` handler, a `Process.OutputDataReceived` handler and a finalizer, plus those step 2 measures. An empty body is not reported | One try/catch that logs, around the whole body | the timer, thread, watcher, process and finalizer entries |
| `BNCQ3001` | A `DispatcherTimer.Tick` handler's body not wholly inside one try statement whose catch takes `Exception` or everything. An empty body is not reported | One try/catch that logs, around the whole body | the `DispatcherTimer` entry in `docs/avalonia-gotchas.md` |

`BNCQ1007` is held to `Observable.Create`'s observer because that is where the catch is the defect:
there one subscriber's throw detaches the producer from every subscriber, as measured. A catch around a
`Subject`'s `OnNext` is a guard, since the subject's other subscribers kept every later value.
`BNCQ1007` judges a catch with Roslyn's control-flow analysis, so one that ends in `throw` is never
reported. `BNCQ1008` judges each lambda, anonymous method and local function as a body of its own, so
a gate acquired in a method and released in a continuation it schedules is not reported. `BNCQ1009` and
`BNCQ3001` read a lambda, an anonymous method, a local function or a method group naming a method
declared in source; a method from a reference is not opened.

### Dismissed

- **A rule per shape.** The other eighteen are decided by what a call can throw on real input, which a
  rule cannot read, or are Avalonia's dispatcher and binding shapes; a rule would report every
  subscription, as the scanner does, and be suppressed wholesale.
- **Every `Observable` overload with a time and no scheduler.** `Replay`, `Skip` and `Take` by time
  deliver their values as they arrive, not on the scheduler, and none was measured.
- **A catch around any `OnNext`.** It is a guard for a `Subject`, above.
- **Taking the scanner.** The developer's decision of 2026-10-31.
- **A `Subscribe` with no `onError`.** An `onError` does not catch the handler's own throw, as measured,
  so a rule demanding one would push people toward a fix that does not fix.
- **An explicit `ObserveOn(Scheduler.Default)` or a pool scheduler.** A scheduler someone chose. It ends
  the process on a throw too, and its entry says so, but a rule against choosing one is a policy no
  measurement makes.
- **`Lazy<T>` in its default mode, a static constructor, a busy flag, `Task.WhenAll`, `ContinueWith`, an
  empty catch.** Whether the factory or the work can fail is a reading; `ContinueWith` has CA2008 and the
  catch CA1031 already, each noisy in its own way, and a busy flag has no type to find.
- **A throw in `Dispose` or `finally`.** CA2219 and CA1065 report the throw statement; a call that can
  throw there is a reading.
- **`DispatcherTimer.Tick` inside `BNCQ1009`.** Decision 8.
- **Rules on the threading package's ground.** Decision 5.

## Scope

**In:**
- the six analyzers under `src/CodeQuality/Rules/`, their lines in `AnalyzerReleases.Unshipped.md`, and
  their tests under `tests/CodeQuality.Tests/Rules/`;
- `Catalog.cs`'s remarks, and the CodeQuality section of the root `AGENTS.md`: what the family is, and
  the numbering in the checklist;
- `docs/codequality.md`: its opening, a section per rule, the generated table;
- `docs/exception-resilience.md`: each entry a rule covers gains a line naming it, and step 2's
  measurements land in their entries; and `docs/avalonia-gotchas.md`'s `DispatcherTimer` entry gains a
  line naming `BNCQ3001`, as an entry promoted to a rule does;
- `PROGRESS.md`.

**Out:**
- the shapes of decision 5, until the threading package is measured;
- the scanner, its samples, its probes and its source, which stay QuailTail's: only their shapes come in, as fixtures written here;
- adopting the rules in any repository, and the release.

## Steps

Three pull requests. The approved plan's own commit comes first, before any implementation.

| # | Step | Verified by |
|---|---|---|
| 1 | This plan, approved, in a commit of its own | The pull request's required checks pass |
| 2 | **The callbacks `BNCQ1009` would name that are not measured yet:** `FileSystemWatcher` `Changed`, `Deleted` and `Renamed`, `Process.ErrorDataReceived` and `Exited`. A probe of this repository's own, kept in scratch and never committed, throws once in each, twice per case in its own process, on Windows and under WSL 2; each result lands in its entry of `docs/exception-resilience.md`, and only a callback measured to end the process or swallow the throw joins the rule | Each case's exit code and output, twice per platform, recorded in the pull request; a control without the throw for each, as the `Created` case has |
| 3 | **The Rx rules, `BNCQ1005` to `BNCQ1007`**, with decision 2's numbering, the README's opening and sections, `Catalog`, `AGENTS.md` and `PROGRESS.md` | Each rule's tests fire on a violation, stay silent on clean code and the fixes, and are inert outside scope, counted as registered actions; the gate forced shut fails the firing tests and forced open fails the inert test, each seen once. `RulesCatalogTests` passes with the table regenerated. The corpus's cases of `RX-SCHEDULER`, `RX-PRODUCER-CATCH` and `ASYNC-LAMBDA` become fixtures by decision 6, and the corpus itself, run through the three rules in a scratch project, gets the fixtures' verdicts: the pull request lists each expected site, its fixture, whether a rule reports it, and for each one not reported the decision above that leaves it out; no safe form is reported. QuailTail's own sites of the three shapes, asked of it with the verdict its audit gave each, become fixtures by decision 9; the rule's findings over them, and the count of each verdict among them, are recorded in the pull request, and decision 3 applied |
| 4 | **`BNCQ1008`, `BNCQ1009` and `BNCQ3001`**, their sections, the gotchas' line and `PROGRESS.md` | The same tests and gate checks. The corpus as fixtures and measured as in step 3, for `GATE-LEFT-SHUT`, `TIMER-THREADING`, `TIMER-TIMERS`, `TIMER-DISPATCHER`, `THREAD`, `WATCHER-HANDLER`, `PROCESS-HANDLER` and `FINALIZER`. QuailTail's own sites of these shapes as fixtures by decision 9. In scratch clones of every family repository at its `main`, never pushed, built against this branch: each finding judged THROWS or SAFE by the guide's table, the counts and every SAFE finding's reason in the pull request, and decision 3 applied over both |

Each step updates `PROGRESS.md` in the same change.
