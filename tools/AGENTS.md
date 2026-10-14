# AGENTS.md — `tools/`

What a change to a rule is checked with before it merges, by
[`plans/00001`](../plans/00001-verification-tooling.md). Nothing here ships: `Directory.Build.props`
imports the root's and sets `IsPackable` to `false`, and no project here is in `XamlQuality.slnx`.

| Path | What it is | Run by |
|---|---|---|
| `real-code.cs` | Runs every rule over the inputs `real-code/inputs.json` pins, and compares what they print with `real-code/expected/<input>.txt` | `dotnet run --file tools/real-code.cs`; CI's `Verification tools` job |
| `real-code/inputs.json` | Each input's repository and commit, the folder its markup is read from, the project its assemblies are built from, and the assemblies the rules read | `real-code.cs` |
| `real-code/expected/` | What `main`'s rules print over each input, the library's hash left out: the record a change is compared with | written by `real-code.cs --update` |
| `Runner/` | Prints what the rules say about markup and build output, built against this tree's library; `--each` reads every folder in a folder as markup of its own, in one process | `real-code.cs`; `probe.cs` |
| `mutate.cs` | Plants each defect a catalog lists in a copy of the working tree, one at a time, runs one test class from the test project that holds it, and reports whether exactly the tests the catalog expects failed | `dotnet run --file tools/mutate.cs -- <TestClass> <catalog.json>`; CI's `Verification tools` job, over `mutate/smoke.json` |
| `mutate/smoke.json` | Two defects in code that rarely changes, each failing one known test of `ExpanderAutomationNameRuleTests`: the catalog CI runs to show the runner still works | `mutate.cs` |
| `probe.cs` | Measures the probe's cases headless under Avalonia's Fluent, Simple and Semi themes, runs `BNXQ1010`, `BNXQ1009` and `BNXQ1005` over each, and judges every verdict against what was measured | `dotnet run --file tools/probe.cs -- <pattern>`; CI's `Verification tools` job builds it |
| `probe/Probe/` | The headless probe, built as `FpProbe6`: the cases the reviews of those three rules wrote, each a list, combo or tab control, and the passes that measure them | `probe.cs`; CI's `Verification tools` job builds it |
| `probe/ProbeLib/` | A consumer's control library, which the probe references and the rules are not given | `probe/Probe/` |

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
- The class runs from the test project its file is in, the folder under `tests/` that holds it, so a
  class of `XamlQuality.ThemeRuntime.Tests` runs headless under Avalonia, and its `[AvaloniaFact]`
  tests are counted with the `[Fact]` ones. A defect in the library swaps the rebuilt DLL into that
  project's output; any other rebuilds that project.
- A defect is built without a restore, so one that changes what restore resolves, such as a
  reference's `PrivateAssets`, reads as caught by nothing: measure it by hand in a restored copy.
- Each worker copies the working tree, uncommitted changes included, into `artifacts/mutate/<pool>/`,
  and the tree itself is never edited. Runs of different classes can go at once, since the pool is
  named after the class unless `--pool` says otherwise; `--workers N` sets how many copies run.
- After the first worker's baseline, the run is estimated from one library rebuild and one class run,
  and a run estimated past 30 minutes is refused unless given `--over-30-minutes`.
- Ctrl+C stops every process the run started and puts each planted file back; nothing is written.

## The headless probe

- `dotnet run --file tools/probe.cs -- <pattern>` takes the cases whose id is the pattern or, where it
  ends in `*`, every case whose id starts with what precedes it; `'*'` alone takes them all. It builds
  the probe and the runner, measures the cases in two passes under each of Fluent, Simple and Semi,
  dumps their markup, runs the three rules over every case in one process, and judges each verdict. It
  exits 0 when no verdict is FALSE or MISSED, and 1, naming them, when one is. Over every case it
  exits 1 today, on verdicts `PROGRESS.md` records, so what a change should leave as it was is the
  list of flagged verdicts, not the exit code.
- A verdict is judged as the reviews of these rules judged it, which `probe.cs`'s header spells out:
  a finding holds where the measurements show what it claims under every theme, a clean verdict is
  MISSED where they show the defect, and a skip, or any verdict on markup that does not load under
  some theme, claims nothing.
- Its output lands in `artifacts/probe/`: `facts.txt` and `bound.txt`, the two passes; `cases/`, each
  case's markup as the rules read it; `rules.txt`; and `table.txt`, every verdict beside what was
  measured, then the counts and the verdicts the measurements refute.
- The `U6` cases take under two minutes, and every case under ten, well inside the 30 minutes a
  verification run is allowed.
- A claim about what Avalonia does is measured by a case. A new question is a new case, with an id
  of its own, in a `Cases*.cs` file whose array `Cases.Every` lists.

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
| A baseline must exit 0, fail nothing, and run at least the class's `[Fact]` and `[AvaloniaFact]` tests | A `-class` filter that matches nothing exits 0 with "Total: 0", and every defect would then read as caught by nothing | `Worker.Prepare` |
| A run that ran no test, or exited non-zero without naming a failed one, is reported, never read as clean | Either would otherwise read as a defect no test caught, or one a test caught, by accident | `Worker.Mutate` |
| Its builds take no MSBuild node reuse, keep the shared compiler server, and turn Avalonia's build telemetry off | A reused node outlives the build, so a run, or one stopped with Ctrl+C, would leave processes behind; the compiler server serves every build on the machine, and turning it off took each defect from 2.7 s to 10 s; and a test project here takes Avalonia, whose build telemetry starts a process that outlives the build | `Worker.Build`, `Worker.BuildEnvironment` |
| "Tests never failed" names a class's `[Fact]` and `[AvaloniaFact]` tests only, as `pmutate_fast.py` named its `[Fact]` ones | A `[Theory]` that no defect fails is not named; `ItemContainerNameRuleTests` holds seventeen | `Worker.DiscoverTests`; `PROGRESS.md`'s follow-up |
| A probe case keeps its id and what it measures; a new question is a new case | The reviews' verdicts cite cases by id, so an edited case silently changes what they say was measured | `probe/Probe/`; the root `AGENTS.md`'s invariants |
| The probe's assembly and namespace stay `FpProbe6` | Every case's markup names it, and the measurements and the rules' messages quote it, so under another name the records stop comparing | `probe/Probe/Probe.csproj` |
| The probe measures with `J16_CCTOR_OK` set and `J15_CCTOR_OK` unset, and the rules run with neither | Two reviews' cases hold a class constructor that fails unless its review's switch is set, as one that needs what a scan's process lacks does. Judge 16 measured its cases with its switch set and judge 15 its own with it unset, so `U5B5` measures its constructor failing; set either otherwise and a measurement moves from the one its review judged, and read with either, the rules never meet the failure the cases are for | `measuring` and `scanning` in `probe.cs` |
| The rules read every case in one runner process | A process a case loads the library, the probe and Avalonia once for each case; one process prints what one a case printed, which the change that moved the probe here checked against the reviews' records | `--each` in `Runner/Program.cs`; `probe.cs` |
| A compiled view's markup is copied beside the probe's build output, and a case's dump reads it there | A case that names a compiled view must dump the markup that was compiled, and the build output is the one place the probe can find it wherever it runs | the `Content` item in `probe/Probe/Probe.csproj`; `SourceView` in `probe/Probe/Program.cs` |
| The probe builds with `AVALONIA_TELEMETRY_OPTOUT` set, and `probe.cs` builds it without node reuse | As for the real-code inputs: Avalonia's build telemetry starts a process that outlives the build, and so does a reused MSBuild node | `building` in `probe.cs`; the `Verification tools` job's step that builds it |
