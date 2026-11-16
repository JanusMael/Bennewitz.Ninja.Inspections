# AGENTS.md — `tools/`

What a change to a rule is checked with before it merges, by
[`plans/00001`](../plans/00001-verification-tooling.md). Nothing here ships: `Directory.Build.props`
imports the root's and sets `IsPackable` to `false`, and no project here is in `Inspections.slnx`.

| Path | What it is | Run by |
|---|---|---|
| `real-code.cs` | Runs every rule over the inputs `real-code/inputs.json` pins, and compares what they print with `real-code/expected/<input>.txt` | `dotnet run --file tools/real-code.cs`; CI's `Verification tools` job |
| `real-code/inputs.json` | Each input's repository and commit, the folder its markup is read from, the project its assemblies are built from, and the assemblies the rules read | `real-code.cs` |
| `real-code/expected/` | What `main`'s rules print over each input, the library's hash left out: the record a change is compared with | written by `real-code.cs --update` |
| `Runner/` | Prints what the rules say about markup and build output, built against this tree's library; `--each` reads every folder in a folder as markup of its own, in one process | `real-code.cs`; `probe.cs` |
| `probe.cs` | Measures the probe's cases headless under Avalonia's Fluent, Simple and Semi themes, runs `BNXQ1010`, `BNXQ1009` and `BNXQ1005` over each, and judges every verdict against what was measured | `dotnet run --file tools/probe.cs -- <pattern>`; CI's `Verification tools` job builds it |
| `probe/Probe/` | The headless probe, built as `FpProbe6`: the cases the reviews of those three rules, and the changes to them, wrote, each a list, combo or tab control, and the passes that measure them | `probe.cs`; CI's `Verification tools` job builds it |
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

It moved out of `tools/` by `plans/00008`, and is the testing family's `mutate`, `src/Testing.Mutate`:
the root `AGENTS.md`'s `Testing` section says how to change it, and [`docs/testing.md`](../docs/testing.md)
how to run it. CI's `Verification tools` job runs its smoke set,
`tests/XamlQuality.Tests/Mutations/smoke.json`.

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
| A probe case keeps its id and what it measures; a new question is a new case | The reviews' verdicts cite cases by id, so an edited case silently changes what they say was measured | `probe/Probe/`; the root `AGENTS.md`'s invariants |
| The probe's assembly and namespace stay `FpProbe6` | Every case's markup names it, and the measurements and the rules' messages quote it, so under another name the records stop comparing | `probe/Probe/Probe.csproj` |
| The probe measures with `J16_CCTOR_OK` set and `J15_CCTOR_OK` unset, and the rules run with neither | Two reviews' cases hold a class constructor that fails unless its review's switch is set, as one that needs what a scan's process lacks does. Judge 16 measured its cases with its switch set and judge 15 its own with it unset, so `U5B5` measures its constructor failing; set either otherwise and a measurement moves from the one its review judged, and read with either, the rules never meet the failure the cases are for | `measuring` and `scanning` in `probe.cs` |
| The rules read every case in one runner process | A process a case loads the library, the probe and Avalonia once for each case; one process prints what one a case printed, which the change that moved the probe here checked against the reviews' records | `--each` in `Runner/Program.cs`; `probe.cs` |
| A compiled view's markup is copied beside the probe's build output, and a case's dump reads it there | A case that names a compiled view must dump the markup that was compiled, and the build output is the one place the probe can find it wherever it runs | the `Content` item in `probe/Probe/Probe.csproj`; `SourceView` in `probe/Probe/Program.cs` |
| The probe builds with `AVALONIA_TELEMETRY_OPTOUT` set, and `probe.cs` builds it without node reuse | As for the real-code inputs: Avalonia's build telemetry starts a process that outlives the build, and so does a reused MSBuild node | `building` in `probe.cs`; the `Verification tools` job's step that builds it |
