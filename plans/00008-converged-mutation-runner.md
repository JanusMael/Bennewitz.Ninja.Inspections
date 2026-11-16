# 00008 — One mutation runner for the family, as a dotnet tool

> Status: **approved 2026-11-16**. Supersedes nothing.

The developer decided on 2026-10-22 that the family has one mutation runner, shipped as a dotnet tool from
this repository. It converges DiffView's `scripts/mutate-behaviour.cs` with this repository's
`tools/mutate.cs`, and then each repository retires its own copy. DiffView sent its half on 2026-10-30, and an
addendum on 2026-11-09, in `PROGRESS.md`'s requests table. The developer placed it on 2026-11-06, as a plan of
its own after plans 00006 and 00007, and on 2026-11-13 put it next.

## What was read

| Runner | At | What it brings |
|---|---|---|
| DiffView's `scripts/mutate-behaviour.cs`, 2,676 lines, with `docs/mutation-proofs.md`, `AGENTS.md` §5, `DECISIONS.md` and `tests/DiffView.Avalonia.Tests/MutateBehaviourTests.cs` | DiffView `main`, `9b367894d5d0f210be38f6d604689ae05669b87a` | **Sets.** Committed sets beside the tests, `tests/<project>/Mutations/<subject>.json`, read with unknown keys refused: each mutation's edit, its `killer` and the `says` its failure carries, or `expect: green` with a `because`. A set with red mutations and no green control is refused. **Verdicts.** Each is compared, not printed: `survived`, `aborted`, `wrong-killer`, `wrong-assertion`, `unexpected-red`, `no-op`, `build`. Only `Test run summary: Passed!` reads as green. **Completeness.** A full run fails while a test the set's `tests` declare, `[Fact]`, `[Theory]`, `[AvaloniaFact]` or `[AvaloniaTheory]`, is killed by nothing. `// inert-at-pin` markers excuse a test while the pins hold a version. **Selection.** `--owed <ref>` runs only the sets a change owes; `--parallel <n>` runs them in detached worktrees loaded from a git tree written through a temporary index, and merges the shards, refusing anything but every mutation exactly once over one tree. **Builds.** No restore unless a build file was edited; one class for a mutation that names its killer; every child given `AVALONIA_TELEMETRY_OPTOUT`, `MSBUILDDISABLENODEREUSE` and one `BuildTimestamp` for the run. 48 tests of its own, and the two rules learned the hard way: the harness's own set never runs from the harness, and no test reads that set |
| This repository's `tools/mutate.cs`, 724 lines | `main`, `00d1698` | **The tree is never edited.** Each worker holds a copy, and a stopped run cannot leave a defect behind. **Assembly swap.** A defect in `src/XamlQuality/` rebuilds the library alone and swaps its DLL into the test project's output. **Budget.** A run estimated past 30 minutes is refused. **Exact sets.** A defect is held to the exact set of tests it must fail. A baseline that runs fewer tests than the class declares is refused |

DiffView also keeps a third runner, `scripts/mutate-gates.cs`, which judges a mutation by the first assertion
to fail. The decision of 2026-10-22 names only the two above, so this plan converges those two.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **DiffView's runner is the base, and its set format, verdicts, completeness, markers, `--owed`, `--parallel` and command line are kept as they are**, moved into a project here and made general, decision 4 | It is the larger and more tested of the two, and 17 committed sets of 515 mutations are written against it. Every change to its format is a migration DiffView pays for. What `tools/mutate.cs` adds, decisions 2, 3 and 5 carry |
| 2 | **The checkout is never edited.** Every run, `--parallel 1` included, plants its mutations in detached worktrees loaded from the tree DiffView's `--parallel` writes, and checks each worktree holds that tree before anything builds. A worktree left by a run that died is removed by the next, once its process, by pid and start time, is gone. The in-place journal and its recovery go, with the flags that test them, `--journal-path`, `--recover` and `--edit-in-flight`, and the stray-file guard runs in each worktree | `tools/mutate.cs`'s rule, kept: a run killed while it edited the tree once left a defect behind. DiffView measured one worktree at 32.8 minutes against 40.2 in place for the same 176 mutations, so the copy costs nothing it measured |
| 3 | **A run is estimated after its first baseline, and one estimated past a budget is refused; the budget is 30 minutes, `--budget <minutes>` sets another**, and `--check`, `--list` and `--kills` are never refused | The developer's rule of 2026-10-09, that no verification run goes past 30 minutes without a reason, held by `tools/mutate.cs`. DiffView's longer runs, such as 365 mutations in 24.8 minutes at `--parallel 4`, pass a budget explicitly |
| 4 | **What was DiffView's is read from the repository.** The root is `git rev-parse --show-toplevel`, not the folder holding `DiffView.slnx`. A worktree's tree is every tracked file and every untracked one git does not ignore, not a list of folders. The configuration is `Debug` unless `--configuration` says otherwise. A trace line is any `.cs` path with a line number. MSBuild properties a repository's tests need, such as DiffView's `SkipReferenceSources`, are passed with `--property`. There is no built-in set | DiffView's `Source.Scope` names its own folders, and an untracked file outside it silently misses the worktrees. The other five are that repository's names and settings |
| 5 | **The assembly swap is not carried over; one `BuildTimestamp` for the run makes the test project's build incremental instead.** This is measured before it is decided: step 4 times `tools/mutate.cs`'s smoke and #138's six defects, swap against incremental build | The swap names this repository's library, and DiffView measured the stamp taking a build from 9.8 to 4.1 seconds, with every project here built under AutoVersioning too. If the incremental build here costs more than the swap by a margin step 4 records, the swap returns as a general option, `--swap <project>`, in this plan's pull request |
| 6 | **A mutation is held to its `killer`, not to the exact set of tests it fails**, and `tools/mutate.cs`'s catalogs, written per change and never committed but for its smoke, become sets the same way. A catalog's `expect` list becomes a `killer` and, where the list named more than one test, the others are left to completeness | Completeness already fails a set whose tests no mutation kills, and a defect that fails one test more than expected is not a defect of the tests. Holding both formats would keep two meanings of a pass |
| 7 | **The tool is a fourth family, `testing`, with one package, `Bennewitz.Ninja.Testing.Mutate`, a dotnet tool whose command is `mutate`**, the developer's choice of name of 2026-11-13, released from `testing/v…` tags like the others. The trusted-publishing policy gains the pattern `Bennewitz.Ninja.Testing*`, which only the developer can add on nuget.org | It is not static analysis, so it belongs to none of the three, and a family releases alone, so a change to the runner never makes another family release. The name says it is for testing, and the family's leaves room for another testing tool, which would release with it. A tool, rather than a file-based app, is what another repository can pin |
| 8 | **The tool's own mutation set runs only from a built copy of the tool outside the tree it mutates, and no test reads that set** | DiffView's two rules, learned the hard way. A dotnet tool does not rebuild itself as `dotnet run` does, but a test that reads its own set still dies under every mutation of it |

### Dismissed

- **This repository's runner as the base.** It runs one class per run from a file-based app, with no
  committed sets, no completeness and no `--owed`, so every DiffView set would need rewriting into it.
- **Two formats, a set and a catalog.** Decision 6.
- **A family of its own repository.** The decision of 2026-10-22 ships it from this one.
- **Converging `mutate-gates.cs` too.** Not in that decision. DiffView can ask once the tool exists.

## Scope

In: `src/Testing.Mutate/`, the tool; `tests/Testing.Mutate.Tests/`, its tests, DiffView's 48 moved with their fixtures but
the five on the journal decision 2 retires, and one for each of decisions 2, 3, 4 and 8; the `testing` family
in `.github/repository.json`, `packages.push`, `docs/testing.md`, a section in `AGENTS.md` and in `README.md`,
and the openings of both and the repository's description, which count three families; this
repository's use of it, decision 6's smoke set, CI's `Verification tools` job, `tools/AGENTS.md` and the root
`AGENTS.md`'s commands and checklists; `tools/mutate.cs` and `tools/mutate/` removed; the two follow-ups the
tool closes, a `[Theory]` no defect fails and a defect that changes what restore resolves, moved out of
`PROGRESS.md`'s follow-ups.

Out: DiffView's move to the tool and the retirement of its script, which are DiffView's change, made once a
release carries the tool. The first release, on the developer's word. Committing this repository's per-change
catalogs as sets, which is a practice of its own. `mutate-gates.cs`.

## Steps

One pull request, its first commit this plan, approved.

| # | Step | Verified by |
|---|---|---|
| 1 | This plan, approved, in a commit of its own | The pull request's required checks pass |
| 2 | **The family:** the project, packed as a dotnet tool, its id in `packages.push` and `families`, `docs/testing.md`, the `AGENTS.md` and `README.md` sections and the description, decision 7 | `repo-conventions check` conforms; `assert-packages` finds the id packed; `dotnet tool install` from the packed folder runs `mutate --help` |
| 3 | **DiffView's runner moved and made general:** decisions 1, 2, 4 and 8, with its tests and their fixtures | Its 43 tests that do not test the journal pass here on Linux, Windows and macOS in CI; new tests for a run that leaves the checkout's bytes and `git status` as they were, a worktree left by a dead run removed, an untracked file reaching the worktrees, and a run refused that reads its own set |
| 4 | **The budget, and the swap measured:** decisions 3 and 5 | A test for a run refused past its budget and one allowed by `--budget`; the smoke and #138's six defects, from the scratch catalog of that change, timed both ways and their verdicts compared, recorded in `PROGRESS.md` |
| 5 | **This repository moves to it:** decision 6 and the Scope's documents, `tools/mutate.cs` removed | CI's `Verification tools` job runs the tool over the smoke set; every mention of `tools/mutate.cs` is gone but `plans/00001`'s, which is frozen |
| 6 | **Parity with DiffView's runner:** at DiffView `9b36789`, in a scratch clone never pushed, sets chosen so the run is estimated under 30 minutes, run by both runners | Every mutation's verdict and every set's completeness the same from both, recorded in `PROGRESS.md`, and any difference explained there before the pull request merges |

Each step updates `PROGRESS.md` in the same change.
