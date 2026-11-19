# 00010 — BNCQ3002: a headless test never blocks on `HeadlessUnitTestSession.Dispatch`

> Status: **approved 2026-11-19**. Supersedes nothing.

AppKit reported on 2026-11-16 that a headless test that blocks on `HeadlessUnitTestSession.Dispatch` can
deadlock on Avalonia 12.1.3, depending on test order. The deadlock comes from the session's own source, read
here at the 12.1.3 tag. `DispatchCore` makes its `TaskCompletionSource` with no options. That means it is not
`RunContinuationsAsynchronously`. It also completes that source on the session's one consumer thread, a
task that loops over `queue.Take`. So when a test awaits a dispatch, or hands the dispatch's task back to
xUnit, the rest of that test can run inline on the consumer thread. So can xUnit's start of the next test.
If that next test then blocks on another dispatch, it waits on the only thread that could run it, and never
returns.

AppKit measured it in CI. The Test step hung on Linux and Windows. A worktree at the Windows runner's path
reproduced CI's test order. A stack dump showed the test blocked in
`Session.Dispatch(body, CancellationToken.None).GetAwaiter().GetResult()`, inside a helper, with no thread
left in the session's loop. MTP's `--timeout` did not end the run. Its fix, commit `048f4a0`, made the tests
await the dispatch. The developer asked on 2026-11-16 for the static half to become a CodeQuality rule, held
in a plan of its own. The deadlock itself goes into `docs/avalonia-gotchas.md` in a separate pull request,
from `main`.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **One rule, `BNCQ3002`. It reports a wait that blocks the calling thread on a task that `HeadlessUnitTestSession.Dispatch` returns.** The waits are the task's `Wait` in any overload, its `Result`, and `GetAwaiter().GetResult()`. It also reports each of these through `ConfigureAwait(…)`, and `Task.WaitAll` or `Task.WaitAny` given such a call as an argument. Every `Dispatch` overload counts. The finding is placed at the wait | Each of these blocks the thread until the session's consumer completes the task. A timeout on `Wait` only bounds the hang: the dispatch still cannot run while its thread waits. The measured case is `GetAwaiter().GetResult()`. Step 2 measures every other form before it joins |
| 2 | **It also reports a wait on a local that holds such a task.** The local's declaration must give it the call as its initializer. The local is not followed if the block that declares it assigns it again, passes it by `ref` or `out`, or takes a reference to it. This is how `BNCQ1009` reads a delegate local since PR #138, and the same code does it. That code moves out of `Callbacks` into a shared helper, unchanged | `var task = Session.Dispatch(…); task.Wait();` blocks the same way. Following it costs a helper that already exists. The move changes nothing that `BNCQ1009` or `BNCQ3001` reports, and their tests hold it |
| 3 | **It reports everywhere in a compilation that can name `Avalonia.Headless.HeadlessUnitTestSession`.** That includes a helper method, a fixture, and the inside of a dispatched action. Elsewhere it registers nothing | The measured block sat in a helper, not in a test method. A wait inside a dispatched action blocks the consumer on itself, so it deadlocks every time. Whether a given site deadlocks depends on test order, which the code does not decide. One site is known to be safe: a wait inside a delegate that `Task.Run` runs, before any `await` in that delegate, which is never on the consumer thread. That site is still reported, and is suppressed where it is, by decision 4 |
| 4 | **On by default, as a warning,** following the developer's decision of 2026-11-03 on `BNCQ3001`: catch the shape everywhere, and suppress a safe site where it is, with its reason. If real code shows more than half of its findings safe, the developer decides again, with the counts | The failure is a run that never ends, which CI shows only as a timeout. The fix is local: make the test async, and await or return the dispatch |
| 5 | **The fix the message gives: await the dispatch or return its task, and never block on it.** The rule's section in `docs/codequality.md` names Avalonia 12.1.3, the version measured, and the message does not. No code fix ships | AppKit's rule, and its fix. Making a test async changes its signature and its callers', which no mechanical rewrite can do safely. The package ships no code fixes today |
| 6 | **The tests compile against the real `Avalonia.Headless` 12.1.3**, which joins `Avalonia` in `AvaloniaReferences` as a test-time reference assembly. No project restores it. The inert test compiles against `Avalonia` without `Avalonia.Headless`. A control in scope, with both referenced, must register actions and report | A stub could agree with a rule that the real overloads prove wrong. With Avalonia referenced and Headless not, the inert test also shows that the gate reads the headless type, not Avalonia as a whole. `AvaloniaConfinementTests` reads restore graphs, so it is untouched |
| 7 | **A mutation set of the rule's own, `tests/CodeQuality.Tests/Mutations/bncq3002.json`, committed and run with `mutate --swap src/CodeQuality/CodeQuality.csproj`.** It holds the gate shut, the gate open, each wait form dropped, the local left unfollowed, the `ConfigureAwait` unwrap dropped, and a green control | The checklist's "watch it fail" is then a set that anyone can run again, not a claim in a pull request. The family's runner now does this |

### Dismissed

- **Following a task through a field, a parameter or a return value.** That needs flow analysis across
  methods. The measured shape, and every shape in the family's code, is the direct chain inside one method.
- **Reporting only inside a method that a test attribute marks.** The measured block was in a helper that
  the tests called. The helper is where the fix goes.
- **`Dispatcher.UIThread.Invoke`, or a blocking wait on `InvokeAsync`, in a headless test.** Neither was
  measured. Each would need its own measurement, and its own decision.
- **A version gate.** Analyzers do not read package versions. The `TaskCompletionSource` is still made
  with no options at the 12.1.6 tag, read on 2026-11-19. The section says the rule was measured on
  12.1.3, and a later Avalonia that completes the task asynchronously is a reason to revisit the rule.
- **The isolation rule (ClaudeForge's checks (a) and (b)).** That is plan 00011, by the developer's decision
  of 2026-11-16.

## Scope

**In:**

- the analyzer;
- the shared local reading it moves into;
- its tests and mutation set;
- the extra reference assembly in `AvaloniaReferences`;
- its line in `AnalyzerReleases.Unshipped.md`;
- its section in `docs/codequality.md`, with the regenerated rules table;
- a "Checked by `BNCQ3002`" line in the deadlock's entry in `docs/avalonia-gotchas.md`;
- `PROGRESS.md`.

**Out:**

- the gotcha entry itself, which lands first in its own pull request from `main`, beside the UIThread
  entry;
- the isolation rule;
- a release, which happens only on the developer's word;
- any reply to AppKit. An outcome message needs the developer's approval of its text.

## Steps

Two pull requests. The approved plan's own commit comes first, before any implementation.

| # | Step | Verified by |
|---|---|---|
| 1 | This plan, approved, in a commit of its own | The pull request's required checks pass |
| 2 | **Measure each wait form.** The probe is a file-based app in scratch, never committed, referencing `Avalonia.Headless` 12.1.3. It starts a session, awaits one dispatch from a thread with no synchronization context, so the continuation runs on the consumer thread. Then it blocks on a second dispatch in one form, bounded at 5 s. The forms are `GetAwaiter().GetResult()`, `Result`, `Wait()`, `Wait(TimeSpan)`, `Task.WaitAll`, `Task.WaitAny`, and `GetResult` through `ConfigureAwait(false)`. Each form has two controls. In one, the same wait comes before any await. In the other, it comes after `await Task.Yield()` | Each form twice, on Windows and under WSL 2: the thread ids show the continuation on the consumer thread, every form is bounded out, and both controls return. The results go into step 3's pull request and `PROGRESS.md`. A form that does not deadlock leaves decision 1 |
| 3 | **The rule:** <ul><li>`src/CodeQuality/Rules/HeadlessDispatchWaitAnalyzer.cs`</li><li>the local reading moved to a shared helper</li><li>`AvaloniaReferences` with `Avalonia.Headless`</li><li>`tests/CodeQuality.Tests/Rules/HeadlessDispatchWaitTests.cs`</li><li>the mutation set</li><li>its line in `AnalyzerReleases.Unshipped.md`</li><li>its section in `docs/codequality.md`</li><li>the gotchas' line</li><li>`PROGRESS.md`</li></ul> | **Tests.** The tests fire on every form step 2 kept, and on AppKit's helper written here in this repository's own names. They stay silent on an awaited dispatch, a returned one, and a local assigned again. The rule registers nothing in a compilation with `Avalonia` but no `Avalonia.Headless`, and a control does register there. A default-severity test reads warning on a plain compilation. `BNCQ1009`'s and `BNCQ3001`'s tests pass unchanged. `RulesCatalogTests` passes with the table regenerated.<br><br>**Mutation set.** It kills as declared, in a run of at most 30 minutes.<br><br>**Real code.** Scratch clones are made at `main`, never pushed, and built with this branch's analyzer added. The clones are AppServices, ClaudeForge, DiffView, ScopedEditors and Templates, and AppServices again at `048f4a0^`. AppServices at `048f4a0^` reports its helper once, at the wait. Every other finding is judged safe only where the wait can never run on the session's consumer thread, and otherwise a deadlock. Its reason is recorded, and decision 4 is applied |

Each step updates `PROGRESS.md` in the same change. Step 3 merges after the gotchas pull request, so that
its section and the gotchas' line link to an entry that exists.
