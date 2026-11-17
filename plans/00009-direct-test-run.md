# 00009 — mutate runs the test assembly directly

> Status: **approved 2026-11-17**. Supersedes nothing; changes code [plan 00008](00008-converged-mutation-runner.md)
> moved from DiffView's runner, which `src/AGENTS.md` lets change only by a plan.

`mutate` runs each mutation's tests through `dotnet test --project <project> --no-build --filter-class <class>`, as
DiffView's runner did, and `dotnet test` evaluates the project before the test host starts. Measured on 2026-11-16 over
the smoke set's class, `XamlQuality.Tests.Rules.ExpanderAutomationNameRuleTests`, five rounds each way, alternating:
that took **5.40 s** on average (3.16 to 7.29), where running the built test assembly itself,
`dotnet <tests>.dll -class <class> -noColor`, took **1.43 s** (1.06 to 1.87). A mutation takes about 14 s with the
swap, so this is most of a third of it.

Run directly, an xUnit v3 test assembly does not speak Microsoft.Testing.Platform: its generated entry point starts the
platform only when given `--server`, and otherwise starts xUnit's own in-process console runner, which takes `-class`,
not `--filter-class`, and prints its own log. A failed test is `<full name> [FAIL]` rather than `failed <full name>`,
the summary is `Total: N, Errors: N, Failed: N, Skipped: N, Not Run: N` rather than `Test run summary: Passed!`, and a
stack frame is `File.cs(142,0): at …` rather than `… in File.cs:142`.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | The direct run is xUnit v3's in-process console runner: `dotnet <TargetPath> -class <class> … -noColor`, in the working directory `dotnet test` gives the test host, with the environment and the bound a test run gets now | The platform needs `--server`, which starts a JSON-RPC server the tool would have to be a client of, or `UseMicrosoftTestingPlatformRunner` in every consumer's project. The console runner is in every xUnit v3 test assembly as it is built. A test that reads a file relative to its working directory must find what it finds under `dotnet test` |
| 2 | Which way a run takes is decided once, at the baseline: a direct run whose log is the in-process runner's and green makes it direct. Anything else, another log or a red one, is run again through `dotnet test --project`, as today, and that way is kept for the baseline, its verdict, the estimate's time and every mutation | What is judged is what ran. A test project on another framework, one built to start the platform itself, or one whose tests pass only under `dotnet test`, keeps working as it does now, at the cost of one extra run |
| 3 | `TargetPath` is read once per run, in the worktree the run is in, through `dotnet msbuild -getProperty` with the run's configuration and properties, as the swap already reads it | The assembly's path depends on the configuration, the target framework and the project's own properties, and each `--parallel` shard has its own worktree |
| 4 | `Judge` reads both logs, and its rule stands: only an explicit success is green. In the console runner's log that is a summary line with `Errors: 0` and `Failed: 0` and a `Total` above 0 | A host that dies prints no summary, and a filter that matches nothing must not read as a pass, as `Zero tests ran` does not today |
| 5 | A failed test is named from its `[FAIL]` line, by its method's name, the last segment before any argument list, as now; a trace line is read in both spellings | `wrong-killer` and `aborted` read the failed names, and the report prints the lines |
| 6 | Nothing else changes: the set format, the verdicts, the budget's formula, `--swap`, `--parallel`, `--owed` | DiffView's committed sets must read as they did |

Dismissed: `--server` with a protocol client in the tool, a second implementation of the platform's host to keep in
step; asking consumers to set `UseMicrosoftTestingPlatformRunner`, a change to every repository for a tool's speed;
`dotnet exec` with the platform's own options, which the generated entry point never routes to it.

## Scope

In: `Dotnet.cs`, `Judge.cs`, `Program.cs` where the baseline chooses, the tool's tests and its own set, the documents.
Out: DiffView's move to the tool, which is DiffView's change; any other test framework's direct run.

## Steps

1. **Choose at the baseline.** First measure the working directory `dotnet test` gives the test host, on Windows and
   on Linux, and give the direct run the same. After the baseline builds, read `TargetPath`, run the baseline's filter
   directly, and keep the direct way when its log is the in-process runner's and green; otherwise run it through
   `dotnet test --project` and keep that. Print which, as `test run: direct, <assembly>` or
   `test run: dotnet test, <why>`. *Verified by* runs over repositories a test makes: an xUnit v3 project that prints
   `direct` and kills as declared, a mutation whose filter names two classes among them; one whose project sets
   `UseMicrosoftTestingPlatformRunner`, so a direct run takes no `-class`, that prints `dotnet test` and still kills as
   declared; one whose test passes only under `dotnet test`, which falls back after its red direct baseline; and a
   test that reads its working directory, which reads the same both ways.
2. **Read the console runner's log.** `Judge.Read` takes the summary, the `[FAIL]` names and both trace spellings.
   *Verified by* `MutateTests` over committed logs of the console runner: a green run, a kill, a host that printed no
   summary (`aborted`), a filter that matched nothing (red), a red run with `Errors` and no failed test
   (`aborted`), and a failed theory row, named by its method, each read as its `dotnet test` twin is.
3. **The tool's own set** gains a mutation for each new branch: the direct way never chosen, a red direct baseline
   kept, the summary's `Failed` ignored, the `Total` floor dropped, a `[FAIL]` line unread; each is killed by a
   declared test. *Verified by* the
   set run as `AGENTS.md` says, every mutation as declared.
4. **Measure the gain and the parity.** The smoke set run before and after, three times each, its mutations' seconds
   compared; the same verdicts both ways; the self set's time against its 2026-11-16 run. *Verified by* the figures,
   recorded in `PROGRESS.md`.
5. **The documents.** The first line of `Dotnet.cs` and `Judge.cs` names this plan beside 00008; the root `AGENTS.md`'s
   Testing invariants gain the direct run and its fallback; `docs/testing.md` says how a run chooses; `PROGRESS.md`
   records the step results and any departure.

The testing family's first release waits for this, by the developer's decision of 2026-11-16.
