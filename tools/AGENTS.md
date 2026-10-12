# AGENTS.md — `tools/`

What a change to a rule is checked with before it merges, by
[`plans/00001`](../plans/00001-verification-tooling.md). Nothing here ships: `Directory.Build.props`
imports the root's and sets `IsPackable` to `false`, and no project here is in `XamlQuality.slnx`.

| Path | What it is | Run by |
|---|---|---|
| `real-code.cs` | Runs every rule over the inputs `real-code/inputs.json` pins, and compares what they print with `real-code/expected/<input>.txt` | `dotnet run --file tools/real-code.cs`; CI's `Verification tools` job |
| `real-code/inputs.json` | Each input's repository and commit, the folder its markup is read from, the project its assemblies are built from, and the assemblies the rules read | `real-code.cs` |
| `real-code/expected/` | What `main`'s rules print over each input, the library's hash left out: the record a change is compared with | written by `real-code.cs --update` |
| `Runner/` | Prints what the rules say about markup and build output, built against this tree's library | `real-code.cs` |
| `mutate.cs` | Plants each defect a catalog lists in a copy of the working tree, one at a time, runs one test class, and reports whether exactly the tests the catalog expects failed | `dotnet run --file tools/mutate.cs -- <TestClass> <catalog.json>`; CI's `Verification tools` job, over `mutate/smoke.json` |
| `mutate/smoke.json` | Two defects in code that rarely changes, each failing one known test of `ExpanderAutomationNameRuleTests`: the catalog CI runs to show the runner still works | `mutate.cs` |

## The real-code comparison

- `dotnet run --file tools/real-code.cs` exits 0 when every input's output is its record, and 1, with
  the difference, when one is not. Read every line that moved: a finding that appears is a claim
  about someone else's code. Where the change means it, `-- --update` writes the record, and the
  record's diff goes in the same pull request.
- Each input is fetched at its commit and built once into a cache outside the repository,
  `XQ_REAL_CODE_CACHE` or else under the user's local application data, and reused while its commit,
  project and build stay the same. `-- --from NAME=DIR` takes the repository whose URL ends in NAME
  from a local clone instead.
- Its output lands in `artifacts/real-code/`; nothing else it does writes a tracked file.
- A cold run fetches and builds the three inputs in about a minute, and a warm one takes seconds.

## The mutation runner

- `dotnet run --file tools/mutate.cs -- <TestClass> <catalog.json>` exits 0 when every defect failed
  exactly the tests its catalog entry expects, and 1 when one did not; the output, one line a defect,
  lands in `artifacts/mutate/<TestClass>.txt`, and ends by naming the class's tests no defect failed.
- A catalog is a JSON array of `{ "id", "file", "old", "new", "expect" }`. `old` must occur exactly
  once in `file`, a path from the repository root, and `expect` names the tests of the class the
  defect should fail. A catalog belongs to the change it checks and stays outside the repository;
  `mutate/smoke.json` is the one committed.
- Each worker copies the working tree, uncommitted changes included, into `artifacts/mutate/<pool>/`,
  and the tree itself is never edited. Runs of different classes can go at once, since the pool is
  named after the class unless `--pool` says otherwise; `--workers N` sets how many copies run.
- After the first worker's baseline, the run is estimated from one library rebuild and one class run,
  and a run estimated past 30 minutes is refused unless given `--over-30-minutes`.
- Ctrl+C stops every process the run started and puts each planted file back; nothing is written.

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| An input is pinned by its commit's full hash, and nothing of it is committed here | Another repository's code is not this one's to republish, and a commit pins its markup exactly | `real-code.cs`, which refuses a short hash and a checkout at any other commit |
| An input is built with its packages' assemblies copied beside its own (`CopyLocalLockFileAssemblies`) | A library's own build output holds none of them, so every type a rule reads fails to load and is named in `Skipped`: a scan that checks nothing and looks clean | `Inputs.BuildProperties` in `real-code.cs`; the `BNXQ1003` and `BNXQ1006` counts in DiffView's record |
| git runs with `core.longpaths` | Windows refuses a path past 260 characters unless git is told to accept it, and an input's deepest files sit far below its cache folder: ClaudeForge's checkout failed with "Filename too long" under a deep cache until it was set | `Inputs.Prepare` in `real-code.cs` |
| An input is built with `AVALONIA_TELEMETRY_OPTOUT` set, and without node reuse | Avalonia's build telemetry otherwise starts a process that outlives the build with its working directory in the project's folder, so the folder cannot be deleted when the input is prepared again | `Inputs.BuildEnvironment` in `real-code.cs` |
| An input restores from nuget.org alone, and only the project the manifest names is built | CI fetches each input into a clean folder, and a sibling feed exists only on the machine that filled it | the `Verification tools` job, which fails when an input does not build |
| The record is what `main` prints | A record that drifts from `main` fails every pull request after it, and one rewritten unread hides a false finding | the `Verification tools` job, a required check |
| The runner finds the rules as `RulesCatalogTests` does | A rule that has to be added to a list will one day be missing from it | `Runner/Program.cs` |
| The runner writes UTF-8 | A redirected console on Windows writes in its code page, so a record made there would differ from one made on Linux | `Runner/Program.cs`, `Console.OutputEncoding` |
| `mutate.cs` plants one defect a run, in a worker's copy, and puts the file back byte for byte, checked by hash | Two defects at once can mask each other; a defect left in a file is a regression nobody wrote; and a run killed while it edited the tree itself once left one behind | `Worker.Mutate`; the hash check that fails the run when a file does not go back |
| A baseline must exit 0, fail nothing, and run at least the class's `[Fact]` tests | A `-class` filter that matches nothing exits 0 with "Total: 0", and every defect would then read as caught by nothing | `Worker.Prepare` |
| A run that ran no test, or exited non-zero without naming a failed one, is reported, never read as clean | Either would otherwise read as a defect no test caught, or one a test caught, by accident | `Worker.Mutate` |
| Its builds take no MSBuild node reuse, and keep the shared compiler server | A reused node outlives the build, so a run, or one stopped with Ctrl+C, would leave processes behind; the compiler server serves every build on the machine, and turning it off took each defect from 2.7 s to 10 s | `Worker.Build` |
| "Tests never failed" names a class's `[Fact]` tests only, as `pmutate_fast.py` did | A `[Theory]` that no defect fails is not named; `ItemContainerNameRuleTests` holds seventeen | `Worker.DiscoverTests`; `PROGRESS.md`'s follow-up |
