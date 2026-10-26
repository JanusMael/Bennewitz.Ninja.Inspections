# AGENTS.md — Bennewitz.Ninja.Inspections

> For anyone changing this repository, human or agent. This top half is the repository as a whole:
> the three families it holds, how each releases, the invariants they share, the commands and the
> checklists. Each family's own contract follows under its name. Each top-level directory has an
> `AGENTS.md` of its own for what only its files show.
>
> Work state lives in [`PROGRESS.md`](./PROGRESS.md), updated in the same change as the work it
> describes. What every repository in this family carries, and how it is checked, is prescribed in
> [`docs/repository-conventions.md`](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md)
> in Bennewitz.Ninja.Templates, whose `plans/00009` brought the three families here.

## What this repository is

Static analysis for .NET, read three ways: one product, three families, each with its own packages,
version line and releases.

| Family | Reads | Packages | Projects | Its README |
|---|---|---|---|---|
| `xamlquality` | XAML markup, and the consumer's compiled controls by reflection | `Bennewitz.Ninja.XamlQuality`, `.XamlQuality.ThemeAudit`, `.XamlQuality.ThemeRuntime` | `src/XamlQuality*` | [`docs/xamlquality.md`](docs/xamlquality.md) |
| `assemblyquality` | The shipped assembly, by reflection | `Bennewitz.Ninja.AssemblyQuality` | `src/AssemblyQuality` | [`docs/assemblyquality.md`](docs/assemblyquality.md) |
| `codequality` | The source, as a Roslyn analyzer | `Bennewitz.Ninja.CodeQuality` | `src/CodeQuality` | [`docs/codequality.md`](docs/codequality.md) |

Each family's README is its rules catalog and its packages' nuget.org page: each package packs its
family's file as `README.md` (`FamilyReadme` in its csproj). The root [`README.md`](README.md) is the
overview. `BNCQ` rules are the compile-time counterparts of the `BNAQ` rules of the same number.

The repository grew from Bennewitz.Ninja.XamlQuality, renamed; AssemblyQuality's and CodeQuality's
histories were imported whole, their tags renamed `assemblyquality/v…` and `codequality/v…`, and
AssemblyQuality's approved plans moved unchanged to `plans/assemblyquality/`.

## Layout

| Path | What it holds |
|---|---|
| `src/` | The shipped projects, each folder named for its family |
| `tests/` | One test project per family; `tests/fixtures/` holds the assemblies AssemblyQuality's tests reflect over |
| `tools/` | XamlQuality's verification tooling, never shipped |
| `docs/` | Each family's README, the release runbook, and XamlQuality's Avalonia and drivable-UI documents |
| `plans/` | Numbered plans, continuing XamlQuality's sequence; `plans/assemblyquality/` holds that family's, unchanged |
| `scripts/` | File-based apps: the release resolution, the packaging guard, the conventions check, and XamlQuality's reference fetcher |
| `.claude-plugin/` | The marketplace manifest serving the `drivable-ui` plugin from `docs/ai-drivable-ui/plugin/` |
| `Inspections.slnx` | The one solution: every family's `src` and `tests` projects |
| `AssemblyInfo.InternalsVisibleTo.cs` / `.External.cs`, `Directory.Build.targets` | Templates' solution-wide friend grants: every project grants its internals to every assembly the solution builds |

## Families and releases

Each family releases alone, from its own tag, `<family>/vYYYY.Q.MMDD`: `xamlquality/v2026.4.1101`.
`scripts/release-target.cs` resolves the tag into the family's ids from `.github/repository.json`'s
`families`; the release packs only those projects, publishes only those ids, and starts its notes
at the family's previous tag. A bare `v…` tag, or a dispatch without a family, is refused before
anything builds. A family is released when it changes, never because another did.
[`docs/publishing.md`](docs/publishing.md) is the runbook.

⛔ **A packable project never references another family's packable project.** A family's release
packs at that family's version, so the reference would pack a dependency on a version of the other
family that need not exist. Across families, a package references the other's package at a
published version. A test project may reference any family's project, as XamlQuality's tests
reference `src/AssemblyQuality`, so they are gated by its head. `repo-conventions check` fails the
packable case.

## Invariants every family shares

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| Every packable project's id is in exactly one of `packages.push` and `packages.local`, and the ids of `families` are exactly `packages.push`'s | A package nobody chose is published permanently, or a declared id is never released | Each family's `PackagingTests`; `scripts/assert-packages.cs`; `repo-conventions check` |
| A release publishes the ids `release-target.cs` resolves for its family, each by its exact file, and never globs `*.nupkg` | A package of another family, or a new packable project, is published by the next tag | `.github/workflows/release.yml`; `release-target.cs`; `assert-packages.cs --family`; `repo-conventions check` |
| The version is the tag's, `YYYY.Q.MMDD` with the quarter of its date | The package and the tag disagree. A published version can never be replaced | `release-target.cs`, before anything builds |
| `NUGET_USER` is a repository **variable**, not a secret | A masked value hides why a trusted-publishing login fails | `release.yml`, step `Refuse to release without NUGET_USER` |
| Packages resolve from nuget.org only | A second source added later silently starts supplying packages | `NuGet.config`, `packageSourceMapping` |
| Versions are pinned centrally, one pin per package for the whole repository | Two projects drift to different versions of one dependency | `Directory.Packages.props` |
| Warnings are errors | A warning ships | `Directory.Build.props`; `ci.yml` builds with `-warnaserror` |
| Every library is trimmable, or exempted with its reason | A consumer's trimmed publish keeps an unmarked assembly whole | `src/Directory.Build.props`; `repo-conventions check`; `XamlQuality`'s exemption in `.github/repository.json` |
| The repository meets the family conventions | Documentation or settings go missing unnoticed | `scripts/repo-conventions.cs`, run by CI and by the release |

## Commands

```bash
dotnet build Inspections.slnx -c Release -warnaserror
dotnet test --solution Inspections.slnx -c Release
dotnet pack Inspections.slnx -c Release --output ./packages/Release
dotnet run --file scripts/assert-packages.cs -- ./packages/Release
dotnet run --file scripts/repo-conventions.cs -- check
dotnet run --file scripts/repo-conventions.cs -- grants
```

- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` takes `--solution` and
  rejects VSTest-only switches such as `--nologo`.
- Write `-p:` rather than `/p:`: Git Bash on Windows rewrites a leading-slash argument into a path.
- A local build wants `AVALONIA_TELEMETRY_OPTOUT=1`; XamlQuality's commands say why.
- Each family's regenerating and verification commands are in its section below.

## Checklists every family shares

**Adding a package:** add the project under `src/`, in a folder named for its family; its id to
`packages.push` and to its family in `.github/repository.json`, or to `packages.local` with the
reason; `FamilyReadme` and `AssemblyProduct` in its csproj; widen the trusted-publishing policy's
pattern for its family (`docs/publishing.md`); record it in `PROGRESS.md`.

**Adding a family:** its stem in `families`, lower-case; its ids in `packages.push`; its README in
`docs/<family>.md`; its tests in a project of their own; a section in this file and in `README.md`.

**Adding, removing or renaming a project:** run `repo-conventions grants`, which rewrites
`AssemblyInfo.InternalsVisibleTo.cs`. Never declare a grant in a project; grants to another
repository go in `AssemblyInfo.InternalsVisibleTo.External.cs`. A project that must stay out sets
`<SolutionFriendGrants>false</SolutionFriendGrants>`, as every fixture under `tests/fixtures/` does.

**Adding a top-level directory:** give it an `AGENTS.md` and a `CLAUDE.md` containing `@AGENTS.md`,
or exempt it in `.github/repository.json` under `undocumented`, with the reason.

**Releasing a family:** only on the maintainer's word for that release. `docs/publishing.md`; tag
`<family>/vYYYY.Q.MMDD`, one release per calendar day per family; verify against the feed. The
family's own section adds its steps.

**Avalonia and drivable-UI lessons** for the whole family of repositories live here, in
`docs/avalonia-gotchas.md` and `docs/ai-drivable-ui.md`; XamlQuality's section says how they are kept.

**Every change:** update `PROGRESS.md` in the same commit. Commits are Conventional Commits.

## XamlQuality

### What this family is

Static analysis for XAML markup, shipped as three packages from one tag, `xamlquality/v…`:

| Package | Project | What it is |
|---|---|---|
| `Bennewitz.Ninja.XamlQuality` | `src/XamlQuality` | The rules library: `IXamlRule` implementations a consumer runs from its own tests |
| `Bennewitz.Ninja.XamlQuality.ThemeAudit` | `src/XamlQuality.ThemeAudit` | The `theme-audit` dotnet tool: argument parsing and reporting over the library's `ThemeAudit` namespace |
| `Bennewitz.Ninja.XamlQuality.ThemeRuntime` | `src/XamlQuality.ThemeRuntime` | The runtime theme check's Avalonia half, as source files an adopting test project compiles against its own Avalonia; the project packs them and ships no assembly |

It also serves one Claude Code plugin from `main`, `drivable-ui`: the method in
`docs/ai-drivable-ui.md`, condensed into a skill an agent loads. The plugin is not in either package,
and it ships on merge rather than on a tag.

The rules began as guards hand-rolled inside applications and copied from one to the next, each copy
having learned a different subset of the cases. Extracting them was the point. `BNXQ1001` and `BNXQ1002`
each turned out stricter than the guard it replaced, in ways nobody predicted: the guard behind
`BNXQ1001` had passed `AutomationProperties.Name=""` for months.

### Three design commitments

**A rule reports; it never asserts.** `IXamlRule.Analyze` returns an `XamlRuleResult`, findings
rather than exceptions, so nothing in the library references a test framework. The consumer picks
the runner and the severity: the same finding can fail an xUnit test, warn in CI or print in a
report.

**No dependencies, Avalonia included.** Rules read markup as text or XML. Where a contract has a
compiled half, a rule reads the consumer's own assemblies through `System.Reflection`. The library
works against WPF and MAUI markup as it does against Avalonia's. Only the notes in `docs/` are
Avalonia-specific, and that split is deliberate. The one check that needs a running Avalonia, the
runtime theme check, ships as source files that compile in the adopting project against its own
Avalonia, so nothing published references Avalonia; in the solution, only its test project takes it.

**Zero findings is not a result on its own.** A rule whose selector stops matching reports nothing
and looks exactly like a clean codebase. So every result carries `Inspected`, how much the rule
examined, and `Skipped`, which names what it saw but could not check. A consumer asserts on all
three.

### How a scan works

`XamlScanContext.Load(root)` reads every `.axaml` and `.xaml` file under a root once, skips `bin`
and `obj`, and sorts the files so findings are stable from run to run. Each `XamlFile` offers its raw
`Text` and its parsed `Document`, because some rules ask about structure and others about spelling.
A file that does not parse carries a `ParseError` instead of disappearing. `WithAssemblies` returns
a new context that also carries compiled assemblies, for the rules that need them.

The rules live in `src/XamlQuality/Rules`, and the generated table in `docs/xamlquality.md` lists every one. These
are the ones that shaped the design:

- **`BNXQ1001` and `BNXQ1002`** require a non-empty `AutomationProperties.Name`, in either spelling,
  through the shared `AutomationName` helper. `BNXQ1002` covers what a keyboard can reach and leaves
  `Expander` to `BNXQ1001`, so a consumer runs both.
- **`BNXQ1003`** reads the `PART_` names a control's compiled code looks up, and requires each in that
  control's own `ControlTheme`. A misspelt part fails silently at runtime, and no compiler can see
  it, because the two halves are in different languages. The literals come from IL, through
  `CompiledStrings`, because a string passed straight to a lookup exists only as an instruction in
  a method body. Its reading is public, by `plans/00002`: `TemplatePartRule.ReadParts` returns each
  control's constants, each lookup with the code that makes it, the control's own, a base class's or
  a helper's, and the parts each of its themes declares, and `Analyze` derives its findings from that
  reading, so a consumer's parts reference lists what the rule checked.
- **`BNXQ1004`** reports a control whose declared size exceeds the fixed Grid row or column it sits in.
  It asks about the automation tree, which clipping never changes, and that is what keeps it
  decidable from markup. It counts only what it measured: a control in an `Auto` or `*` slot, or
  declaring no size, is neither inspected nor skipped, and one whose fit rests on a binding or a
  resource is named in `Skipped`.
- **`BNXQ1005`** reports a key binding on an items control where nothing in its focus path can take
  focus: the control, its item containers, or what its item template puts in them. It is the first
  rule to read what the framework does by default, from its property metadata after running each
  type's class constructors, and the first to follow resources the way the framework's lookup does,
  through themes, `BasedOn` chains, ancestors' resources and includes. What it cannot decide from
  markup, it names in `Skipped`.
- **`BNXQ1006`** reports a themed custom control that keeps the framework's empty automation peer,
  which a control-view search never reaches, so the names `BNXQ1001` and `BNXQ1002` require reach
  nobody. It reads the peer from the type without running it: the control has one when its
  `OnCreateAutomationPeer` resolves to an override anywhere in its chain rather than the root
  declaration. An override that returns the empty peer on purpose is how a decorative control opts
  out, in its own code.
- **`BNXQ1007`** requires the id a test or an agent searches by: an explicit, non-blank
  `AutomationId` on the controls `BNXQ1002` covers and on `Expander`, through the `AutomationIdentifier`
  helper. An `x:Name` does not count, although Avalonia derives an id from it, and a blank id is
  reported as empty, because on Avalonia it hides the derived one.
- **`BNXQ1008`** is `BNXQ1004`'s other half. A control that asks for no size, in fixed slots that add
  up to none, is hidden from people and not from the automation tree, so it must be hidden by
  `IsVisible` as well. Both rules read a grid through the shared `GridLayout` helper, which places a
  control where the framework does. `BNXQ1008` also reads a slot's own bounds, which `BNXQ1004` does
  not yet.
- **`BNXQ1009`** asks what names the rows an items control generates from `ItemsSource`. A list or tab
  row that nothing names falls back to `ToString()` on its item, and a tree row to nothing at all.
  So the rule asks, in the framework's order: a style for the container type, the
  `ItemContainerTheme`, `DisplayMemberBinding`, the item template's root, or with none the data
  template the rows' presenters choose by the items' type, and last the items' `ToString()`. The
  items' type is the template's `x:DataType`, else what the `ItemsSource` binding's path reads, as the
  XAML compiler types a compiled binding. It recognises a row by the peer its container
  creates, Avalonia's `ListItemAutomationPeer` or `TreeViewItemAutomationPeer`, read from IL. It
  reads themes through `ThemeReader`, which it shares with `BNXQ1005`, so the two cannot follow one
  theme differently.
- **`BNXQ1010`** asks whether those rows draw a background of their own. A control is hit-tested
  where it draws, and a null `Background` draws nothing. It reports two things markup decides about
  the rows' own template: rows whose `Background` can be null where their template draws it, and a
  template that draws no background at all. It reads the state a row starts in: a style for the row
  type alone, else the row's theme down its `BasedOn` chain, where a binding with no non-null
  `TargetNullValue` counts as null, and a theme that sets no template leaves the framework's or the
  default a `ContentControl` has, both of which draw it. What draws the row's background is an
  element that draws its own, a `Border`, a panel, a `ContentPresenter` or a `TextBlock`, or a shape's
  fill, taking the row's, or a templated control taking it whose own template draws it; where nothing
  does, anything in the template that paints a brush of its own draws one. What the rows present
  decides nothing, nor what hides an element, nor what a `Popup` holds, so a row whose content paints
  across it is still reported, by design: the fix costs nothing. A style that paints rows only in
  an interaction state changes nothing, `:disabled` and `:selected` included unless markup may disable
  or select them from the start: any `IsEnabled` or `Command` in the scan, in any spelling, may disable
  them, since a control disabled anywhere disables all it holds, and a list that always selects a row,
  as a `TabControl` does, or the selection markup gives it in any spelling, selects one. A `:selected` style paints only the
  rows selected, so a null background the rest draw is still reported. One whose selector holds of every
  row whatever the data,
  a `:not()` of a class or a name or the host as its context included, sets them all; a `:not()` of a
  state may not. One that clears them in a state, or picks some rows by a class, a place, a property or
  the absence of a state and may clear them, is named in `Skipped`, as is a template element that
  something the rule does not read may paint where nothing draws the row's background, and one that a
  style with a condition or its own code may repaint, since either outranks what the template's markup
  gives it. It reads every element's theme, the implicit one by its style key included, a templated
  control's by what its template draws, and any type with an `x:Class` of its own or a base's, a view
  or not, by that markup, whose background outranks the one a template gives it, but for a binding. A
  binding to the row as an ancestor counts the ancestors of its type from the nearest, as the framework
  does, and only in a `RelativeSource` that finds an ancestor; what a `ContentPresenter` or a
  `TransitioningContentControl` in the template holds as markup content, which has no templated parent,
  is not followed. A background no markup sets is
  the default the type registers, read once its class constructors have run, and one its own code
  sets, read from IL anywhere in its chain, leaves the rows undecided. It shares `BNXQ1009`'s reading
  of rows, in `RowPeers`, and of selectors, in `StyleSelectors`, and its themes with both earlier rules,
  through `ThemeReader`. It leaves tree rows out, because Fluent's and Simple's templates give a tree
  row's header a background of its own.

There is no rule registry: a consumer constructs each rule by name. That makes `docs/xamlquality.md`'s rules
table the only enumeration of the rules anywhere, which is why a test generates it from the rule
types rather than trusting anyone to add the row.

### ThemeAudit

The `ThemeAudit` namespace inventories the resource keys a theme defines and scans consumers for the
keys they reference. It reports what a theme leaves undefined, and which colour pairs fall below a
contrast floor. Its colours are what Avalonia draws, by `plans/00004`: a brush's alpha truncated once, as
Avalonia's Skia renderer paints it, every spelling of a brush based on another resource that Avalonia draws
read, and the one it does not load given no colour; `DrawnColourTests`, in the runtime check's test project,
holds them to the pixels. It also generates compat dictionaries: the keys one theme defines and another lacks,
mapped onto the latter's tokens. The reviewed mappings ship as `Mappings/*.json` beside the library.
`AuditRunner.Run` is the entry point. Each theme and consumer carries a content digest, so a
committed report changes when what was audited changes, and only then.

The audit's second report is the parts report, by `plans/00002`'s step B: `PartsRunner.Read` takes
`BNXQ1003`'s reading of the assemblies a configuration's `parts` section names, loaded apart from the
tool's own, against its markup folders, and `TemplatePartReading.ToMarkdown` renders it. Every output
the tool writes is held to a fresh run by one drift check, `OutputDrift.Check`, which a consumer's test
can run too.

Its third output is the brand document, by `plans/00004`'s step 3: `BrandRunner.Fill` fills a Markdown
template from a configured theme's palette, read in one variant, with what nobody should type by hand,
each token's colour and the line declaring it, the colours stacks of them draw as, contrast figures and
a mermaid chart of the swatches, and checks the claims its placeholders state. The language is the one
the WPF-to-Avalonia port's generator wrote, its alphas made as the audit's are; what only a consumer's
own code knows reaches the document as blocks its own program renders. A document with a claim that
fails, or a placeholder nothing fills, is not given at all, and every problem is listed instead.

The audit is a model of Avalonia's resource lookup, and the runtime theme check, by `plans/00003`,
reads it against the real one. `RuntimeCheckPlan.For` works out from `AuditRunner.Run`'s result, and
the names a map holds, what to ask: for each consumer, under each theme and variant, every key it
references and does not define, with the audit's answer. The source package sets each theme and
variant up afresh in the running application, its own styles and resources set aside, asks
`Application.TryGetResource`, and hands the answers to `RuntimeCheckPlan.Compare`, whose result names
each difference, counts the lookups, and names in `Skipped` what was not checked. What needs no
running framework is the library's, public and tested beside the audit; `src/XamlQuality.ThemeRuntime`
holds the rest.

This analysis moved here from DiffView, and its surface shipped public. DiffView binds to it, so
narrowing it is a breaking change rather than a tidy-up. `src/XamlQuality/Properties/AssemblyInfo.cs`
records which types are public and which stay internal.

### The documents in `docs/`

| File | What it is |
|---|---|
| `docs/avalonia-gotchas.md` | Measured Avalonia foot-guns, each with symptom, cause and fix. Moved here from ClaudeForge |
| `docs/ai-drivable-ui.md` | The method for a desktop UI an agent can drive and verify, begun in a WPF-to-Avalonia port. This is its one living copy; the port points here instead of keeping its own |
| `docs/ai-drivable-ui/` | The tools and journals the guide links to, moved from that port |
| `docs/ai-drivable-ui/plugin/` | The guide condensed into the `drivable-ui` skill, served as a Claude Code plugin through `.claude-plugin/marketplace.json` at the root |
| `docs/publishing.md` | Trusted publishing, the version rule, and verifying a release against the feed |

The gotchas document lives beside the rules on purpose. An entry that can be checked mechanically is
promoted to a rule, and the entry keeps a line pointing at the rule's id, as `BNXQ1004`'s and
`BNXQ1005`'s do.
`BNXQ1001` and `BNXQ1002` began as guards hand-rolled in application test suites, not as entries.
Other repositories contribute to both documents by message to the session working here, or by an
issue when none is running, and cite them by path. Each document's header says so.

### The verification tooling

`tools/` holds what a change to a rule is checked with, by `plans/00001`. The real-code comparison
runs every rule over ClaudeForge's, DiffView's and ScopedEditors' markup and build output, at the
commits `tools/real-code/inputs.json` pins, and compares what they print with a committed record of
what `main` printed. A change that alters what a rule says about real code shows it in its own diff,
and CI's `Verification tools` job holds every pull request to the record. The mutation runner plants
each defect a catalog lists in a copy of the working tree, one at a time, and reports whether exactly
the tests the catalog expects failed. The headless probe measures what Avalonia does with the cases
the reviews of `BNXQ1005`, `BNXQ1009` and `BNXQ1010`, and the changes to them, wrote, under its
Fluent, Simple and Semi themes, and judges what each of those rules says about each case against the
measurements; CI builds it and does not run it. `tools/AGENTS.md` says how.

### Who consumes this

Other repositories in the same family consume the library and the tool. ScopedEditors' tests run the
rules, and DiffView runs the audit and commits its report. That is why the public surface is treated as a
contract. `PROGRESS.md` lists which members each consumer uses.

### Releases

The release workflow publishes through nuget.org trusted publishing (OIDC), so no API key exists.
The version is the tag, in the family's `YYYY.Q.MMDD` form. That allows one release per calendar
day, and a published version can never be replaced. `docs/publishing.md` is the runbook.

### Operational rules

This half is **fact-shaped**: every claim cites a file, a type, a member or a test, so drift surfaces
as a missing symbol under `grep` rather than as stale prose. No source-line numbers, no dates, no
counts.

#### 1. Hard invariants

| Invariant | Failure signature if broken | Canonical source |
|---|---|---|
| The rules library takes no package reference and no framework dependency | A consumer's test project inherits dependencies it did not choose, and the rules stop working against WPF or MAUI markup | `src/XamlQuality/XamlQuality.csproj`; `Directory.Packages.props`, whose `Tool` group is for the CLI only; test `AssemblyQualityTests.BNAQ1003_no_assembly_references_a_framework_or_another_family_package` |
| Every shipped assembly passes the family's AssemblyQuality rules with nothing skipped, and the scan finds each project under `src` by the assembly name its csproj declares, but one that declares `SourceOnlyPackage`, which ships no assembly and whose package `SourceOnlyPackageTests` holds instead | A package ships a violation of a rule nothing ran, a type a rule could not read passes as clean, a scan that silently drops `ThemeAudit.dll` reads as clean, or the exemption hides a project that ships an assembly | `AssemblyQualityTests`, one test per rule and `Every_shipped_assembly_is_in_the_scan`; `SourceOnlyPackageTests.DeclaresSourceOnly`; `tests/XamlQuality.Tests`, which references `src/AssemblyQuality` as a project |
| A source-only package holds its project's `.cs` files as compile content for the library's framework, and the README, and no assembly; depends on the library alone, at a minimum of its own version; is no development dependency; and each file starts `// <auto-generated/>`, then `#nullable enable`, has no using directive, names every type outside it from `global::`, declares no public type and names no test framework. It is packed without a build | A file fails to compile in an adopting project that turns implicit usings off or declares a global alias of a name it uses; `dotnet add package` leaves the library's compile assets out, and the files do not compile (CS0103); a stray using of a test framework passes here, where the files compile only in a test project, and fails in an adopter; or a pack that builds rebuilds the library at another version between a release's build and its pack | `src/XamlQuality.ThemeRuntime/XamlQuality.ThemeRuntime.csproj`; `SourceOnlyPackageTests`; `tests/XamlQuality.ThemeRuntime.Tests`, which compiles the files with implicit usings off and, in `HostileAliases.cs`, a global alias of each Avalonia and library type name they use |
| The runtime check's names, the library's half and the source half's alike, are permanent from the release that first carries them, and a member added later is an `init` property, never a new positional parameter | Every adopting test is written against them, and a source half compiled against a newer library, which the package's minimum allows, stops compiling | `RuntimeCheckPlan` remarks; `src/XamlQuality/Properties/AssemblyInfo.cs` |
| No project in `Inspections.slnx` but `tests/XamlQuality.ThemeRuntime.Tests` takes a package whose id names Avalonia, directly or through another package, as read from each project's restore graph | Avalonia reaches the library's tests or a shipped assembly unannounced, or a project whose graph is missing passes for one that takes nothing | `AvaloniaConfinementTests` |
| A rule returns findings; it never throws or asserts on a violation | The library chooses the consumer's test runner and severity for them | `IXamlRule`, `XamlRuleResult`, `XamlFinding` |
| Every result reports `Inspected`, and a rule names what it saw but could not check in `Skipped` | A rule that checked nothing returns zero findings, which reads as a clean codebase | `XamlRuleResult.Inspected`, `XamlRuleResult.Skipped`, `XamlSkip`; tests `ExpanderAutomationNameRuleTests.MarkupWithNoExpanders_InspectsNothingAndReportsNothing`, `GridSlotOverflowRuleTests.MarkupWithNoGrid_InspectsNothing`, `TemplatePartRuleTests.WithoutAssemblies_EveryThemedControlIsNamedAsSkipped` |
| Elements are matched by local name, never by qualified name | A rule goes silently inert on the frameworks whose markup namespace it did not name | `InteractiveAutomationNameRule` remarks; test `ExpanderAutomationNameRuleTests.WpfMarkup_IsScannedToo` |
| An attached property is read in both spellings, attribute and property element | Whatever is written in the unread spelling passes | `AutomationName.IsDeclaredOn`; `AutomationIdentifier.Of`; `GridLayout` (its private `DefinitionsOf`); tests `InteractiveAutomationNameRuleTests.ThePropertyElementSpelling_Counts`, `InteractiveAutomationIdRuleTests.ThePropertyElementSpelling_Counts`, `GridSlotOverflowRuleTests.ThePropertyElementSpellingOfRowDefinitions_Counts`, `ZeroSizeSlotRuleTests.ThePropertyElementSpelling_Counts` |
| An empty automation name is not a name, in either spelling | `Name=""` or an empty `<AutomationProperties.Name/>` passes, and a screen reader announces nothing | `AutomationName.IsDeclaredOn` and its private `HasContent`; tests `InteractiveAutomationNameRuleTests.AnEmptyAutomationName_IsNotAName`, `InteractiveAutomationNameRuleTests.AnEmptyPropertyElement_IsNotAName`, `ExpanderAutomationNameRuleTests.AnExpanderNamedByAnEmptyPropertyElement_IsReported` |
| A scan never reads `bin` or `obj` | Every violation is reported twice, and one keeps being reported after the source is fixed | `XamlScanContext.Load` (`excludedDirectories`); test `ExpanderAutomationNameRuleTests.MarkupUnderObj_IsNotScanned` |
| Unparseable markup is surfaced, never skipped | A broken file full of violations reports zero | `XamlFile.ParseError`, `XamlScanContext.ParsedFiles` |
| A scan context is immutable; `WithAssemblies` returns a new one | A context handed to two rules changes under the second | `XamlScanContext.WithAssemblies` |
| `BNXQ1002` leaves `Expander` to `BNXQ1001` | One unnamed Expander is reported twice | `InteractiveAutomationNameRule.FrameworkInteractiveElements`; test `InteractiveAutomationNameRuleTests.Expander_IsLeftToItsOwnRule` |
| Adding a name to `FrameworkInteractiveElements` is a behaviour change for every consumer, in `BNXQ1002` and `BNXQ1007` alike | Markup that passed turns red on a consumer's next upgrade, unannounced | `InteractiveAutomationNameRule.FrameworkInteractiveElements` remarks; tests `InteractiveAutomationNameRuleTests.TheFrameworkSet_IsNotSilentlyEmpty`, `InteractiveAutomationIdRuleTests.EveryFrameworkInteractiveElementAndExpander_IsInspected` |
| `BNXQ1007` counts only an explicit, non-blank id: `AutomationProperties.AutomationId` in either spelling, or MAUI's plain `AutomationId`. An `x:Name` is not one, a blank attribute is reported as empty, and an empty property element sets nothing | A control found only by the id a framework derives from its field name passes, or a blank id that hides the derived one passes | `AutomationIdentifier` remarks and `AutomationIdentifier.Of`; tests `InteractiveAutomationIdRuleTests.XName_IsNotAnExplicitId`, `InteractiveAutomationIdRuleTests.AnEmptyOrBlankAttribute_IsReportedAsEmpty`, `InteractiveAutomationIdRuleTests.AnEmptyPropertyElement_IsNotAnId`, `InteractiveAutomationIdRuleTests.AnAutomationName_IsNotAnId`, `InteractiveAutomationIdRuleTests.MauisPlainAutomationId_Counts` |
| `BNXQ1007` covers `Expander`, which `BNXQ1002` leaves to `BNXQ1001` | An Expander's id is checked by no rule | `InteractiveAutomationIdRule` remarks; test `InteractiveAutomationIdRuleTests.AnExpander_IsCovered` |
| `BNXQ1003` counts a `PART_` literal only when the next call is a lookup whose name starts with `Find` or `Get`, and never the bare prefix | Compiled XAML's element-name registrations read as parts, and compiled theme classes show up as controls | `TemplatePartRule` (its private `IsLookup` and `IsPartName`), `CompiledStrings.LoadedBy`; tests `TemplatePartRuleTests.ALiteralThatNamesAnElement_IsNotALookup`, `TemplatePartRuleTests.ANameBuiltAtRuntime_IsNotReadAsAPart` |
| `BNXQ1003` reads every type an assembly loads, public or not, and credits a lambda's literals to the control that declares it | A part looked up from an internal control, or inside a lambda, goes unchecked | `TemplatePartRule` (its private `ReadControls`), `LoadedTypes.Of`; tests `TemplatePartRuleTests.AnInternalControlWithAPrivateConstant_IsChecked`, `TemplatePartRuleTests.ALiteralInsideALambda_IsCreditedToItsControl` |
| `BNXQ1003` credits a lookup to each control that hands the method making it its template or the template's name scope: from the `OnApplyTemplate` the control resolves to, its base's included, through calls and constructors in the scanned assemblies. A method no control hands its template to keeps its lookups as its own type's, and a parameter of type `object` does not take a template. A call through an interface or a virtual method, which names the method declared, and a lambda, which is created and not called, are not followed, and the skips that name a control with no part found, or parts with no theme, say so | A helper's lookups are checked against no theme and reach a consumer only as a skip's prose; a handler on another control's template is charged to a control that never handed it one; following every implementation of an interface charges one control with another's lookups; or a skip tells a consumer that a control has no parts where the rule did not follow a call or a lambda to them | `TemplatePartRule` (its private `ReadControls`, `HandedTemplate` and `Takes`), `CompiledStrings.CalledBy`; tests `TemplatePartRuleTests.AHelperHandedTheTemplate_HasItsLookupsCheckedAgainstTheControlsTheme`, `TemplatePartRuleTests.AHelperHandedTheTemplate_IsNamedOnlyForTheLookupsNoControlHandsIt`, `TemplatePartRuleTests.TwoControlsHandingOneHelper_AreEachChecked`, `TemplatePartRuleTests.AHandedNameScope_IsFollowed`, `TemplatePartRuleTests.AChainOfHelpers_IsFollowed`, `TemplatePartRuleTests.ABaseClassesLookups_AreCheckedAgainstTheSubclassesTheme`, `TemplatePartRuleTests.AnObjectParameter_DoesNotTakeTheTemplate`, `TemplatePartRuleTests.AHelperConstructedWithTheTemplate_IsFollowed`, `TemplatePartRuleTests.ALookupPastAnInterfaceCall_IsNamedInBothSkips`, `TemplatePartRuleTests.ALookupInABaseClassesLambda_IsNamedInBothSkips` |
| `BNXQ1003` matches parts inside their own `ControlTheme`, and reports only a part the code looks up and the theme omits | One theme satisfies another's lookups, or a name that exists only for a style selector is reported as a defect | `TemplatePartRule` remarks; tests `TemplatePartRuleTests.OneThemeDoesNotSatisfyAnother_InTheSameFile`, `TemplatePartRuleTests.APartNamedOnlyForAStyleSelector_IsNotAViolation` |
| `BNXQ1003` derives its findings from its public reading, `TemplatePartRule.ReadParts`: for each theme of a control, the constants and looked-up parts the reading lists that the theme does not declare. A lookup names the type whose code makes it, a closure's being the type that declares it, and whose code that is: `Own` for the control's, a closure it declares included, or for code that no control hands its template to; `BaseClass` for a base class in any scanned assembly; `Helper` for another type the control hands its template or name scope to, one it nests included. Controls, constants and lookups are ordered ordinally, themes by file and line, and a theme's parts as it declares them | The rule and a consumer's parts reference read one fact two ways and drift apart, as DiffView's own reading did; a base class's lookup reads as the control's own; a nested helper's lookup is named after a type with no such method; or two readings of one build differ as text | `TemplatePartRule.ReadParts` remarks, `TemplatePartReading` remarks, `TemplatePartRule` (its private `ReadControls`, `OriginOf`, `CodeTypeOf` and `Wanted`); tests `TemplatePartReadingTests.TheRulesFindings_AreThePartsTheReadingSaysEachThemeLacks`, `TemplatePartReadingTests.ALookupInABaseClassInAnotherAssembly_NamesThatAssembly`, `TemplatePartReadingTests.ALookupInAHelperTheControlHandsItsTemplate_IsTheHelpers`, `TemplatePartReadingTests.ALookupInATypeTheControlNests_IsAHelpers_UnderThatTypesName`, `TemplatePartReadingTests.TheReading_IsInTheOrderItComparesIn` |
| `BNXQ1003` reads a name more than one top-level type the scan was given carries as no control's: each theme that targets it is named in `Skipped`, as `BNXQ1006` names it, and so is the name where parts were found in its types' code and no theme targets it; the reading has no entry for it, and names it in its own `Skipped` where parts were found, lookups alone included. Its types are listed once each, in ordinal order, so one assembly passed twice makes no name its own twin. The rule reads a theme's target by its name alone, so a theme whose prefix names the one type with parts is skipped too | One class's theme is checked against another class's parts, a false finding for each part only the other declares; the reading merges two classes' parts into one control's; or one assembly passed twice turns every theme into a skip | `TemplatePartRule` (its private `ReadControls` and `FullNames`), `TemplatePartRule.ReadParts` remarks, `ControlTemplateParts` remarks; tests `TemplatePartRuleTests.ANameTwoScannedTypesCarry_IsSkipped`, `TemplatePartRuleTests.ANameTwoScannedTypesCarry_WithNoTheme_IsSkippedOnce`, `TemplatePartRuleTests.ANameTwoTypesWithLookupsOnlyCarry_IsSkippedOnce`, `TemplatePartRuleTests.ANameTwoPartlessTypesCarry_IsSkippedOnlyWhereThemed`, `TemplatePartRuleTests.AnAssemblyPassedTwice_MakesNoNameATwin`, `TemplatePartReadingTests.ANameTwoScannedTypesCarry_HasNoEntry`, `TemplatePartReadingTests.ASharedName_IsNamedOnlyWherePartsWereFound` |
| IL is decoded instruction by instruction, each operand stepped over by its declared size | An operand byte that happens to equal the `ldstr` opcode is misread as a string load | `CompiledStrings` remarks |
| `BNXQ1004` checks fixed slot sizes only | False positives on `*` and `Auto` rows, which markup cannot resolve | `GridSlotOverflowRule` remarks; tests `GridSlotOverflowRuleTests.AStarRow_IsNotDecidableAndIsNotReported`, `GridSlotOverflowRuleTests.AnAutoRow_IsNotDecidableAndIsNotReported` |
| `BNXQ1004` measures a control where the framework's `Grid` places it: an index past the last definition in the last one, and a span across every fixed slot it crosses plus the spacing between them, clamped at the edge | A control that fits its span is reported against its first slot, or one placed past the end escapes the check while counting as inspected | `GridSlotOverflowRule` remarks; tests `GridSlotOverflowRuleTests.AChildThatFitsTheColumnsItSpans_IsNotReported`, `GridSlotOverflowRuleTests.AnIndexPastTheLastColumn_IsMeasuredAgainstTheLastColumn`, `GridSlotOverflowRuleTests.ASpanPastTheLastColumn_IsClampedToTheColumnsThatRemain` |
| `BNXQ1004` counts a control in `Inspected` only when it measured it against fixed slots; a value markup cannot evaluate is named in `Skipped` unless no fixed slot could depend on it; a control in an `Auto` or `*` slot, or declaring no size, counts as neither | A grid sized by bindings reads as inspected and clean, and a consumer's floor on `Inspected` guards a check that never ran | `GridSlotOverflowRule` remarks; tests `GridSlotOverflowRuleTests.BoundColumnDefinitions_AreNamedInSkipped`, `GridSlotOverflowRuleTests.AChildThatDeclaresNoSize_IsNotInspected`, `GridSlotOverflowRuleTests.ABoundColumnAmongOnlyAutoAndStarColumns_IsNotSkipped`, `GridSlotOverflowRuleTests.AControlMeasuredBothWays_CountsOnce` |
| `BNXQ1004` and `BNXQ1008` read a grid through one helper, `GridLayout`, and place a control where the framework does | Two rules place one control in different slots, so one reports what the other passes | `GridLayout` remarks, `GridLayout.Axis.Of`, `GridLayout.Attached`; tests `GridSlotOverflowRuleTests.AnIndexPastTheLastColumn_IsMeasuredAgainstTheLastColumn`, `ZeroSizeSlotRuleTests.AnIndexPastTheLastRow_LandsInIt` |
| `BNXQ1008` reads a slot's size as the framework arranges it: its own `MinHeight` first, then a `MaxHeight` of 0, then a star of weight 0, and a span has the spacing between its slots | A row that its `MinHeight` gives room is reported, or one that a `MaxHeight` of 0 empties passes | `ZeroSizeSlotRule` remarks and its private `SizeOfSlot`; `GridLayout.SlotBounds`; tests `ZeroSizeSlotRuleTests.ARowsMinHeight_GivesItRoom`, `ZeroSizeSlotRuleTests.AMaxHeightOfZero_EmptiesAnyRow`, `ZeroSizeSlotRuleTests.MinHeightWinsOverMaxHeight`, `ZeroSizeSlotRuleTests.AStarOfWeightZero_IsEmpty`, `ZeroSizeSlotRuleTests.ASpanOfZeroRows_HasTheSpacingBetweenThem` |
| `BNXQ1008` leaves a control that asks for a size to `BNXQ1004`, and any `IsVisible` but a literal `True`, or WPF's `Visibility` but `Visible`, answers it | One control is reported by both rules, or a collapsing pane that binds `IsVisible` is reported | `ZeroSizeSlotRule` remarks; tests `ZeroSizeSlotRuleTests.AControlThatAsksForASize_IsLeftToTheOverflowRule`, `ZeroSizeSlotRuleTests.ABoundIsVisible_HidesIt`, `ZeroSizeSlotRuleTests.IsVisibleTrue_DoesNotHideIt`, `ZeroSizeSlotRuleTests.WpfVisibility_HidesItUnlessVisible` |
| `BNXQ1005` and `BNXQ1010` run every class constructor in a type's chain before reading its `Focusable` or `Background` default | Read cold, every control reports unfocusable, and every correct list is reported; or a type that registers a brush as its default reads as painting nothing | `ElementTypes` remarks, `ElementTypes.PaintsByDefault`; tests `KeyBindingFocusRuleTests.AListWithStockRows_IsLive_BecauseItsRowsTakeFocus`, `ItemContainerBackgroundRuleTests.ADefaultBackgroundTheTypeRegisters_Paints` |
| `BNXQ1005` reports a binding only when the control, its containers and everything its item template holds cannot take focus; a style, a missing item template, rows that present items of their own, and content with a template of its own are named in `Skipped` | A binding that works is reported, and people stop reading the rule | `KeyBindingFocusRule` remarks; tests `KeyBindingFocusRuleTests.AStyleThatSetsFocusableOnTheRows_StopsTheRule`, `KeyBindingFocusRuleTests.UnfocusableRowsWithNoItemTemplate_AreSkippedNotReported`, `KeyBindingFocusRuleTests.ATreeViewWithUnfocusableRows_IsSkipped_BecauseItsRowsCarryAHeader`, `KeyBindingFocusRuleTests.ATemplatedControlInTheItemTemplate_IsSkipped` |
| An implicit theme is keyed `{x:Type T}`, and reaches an element from its own resources, an ancestor's, what they include, or the application's; one beside it in the same file does not, and one in another file is skipped | A theme is applied where it never reaches, or ignored where it does | `ResourceScope` remarks; tests `KeyBindingFocusRuleTests.AnImplicitThemeInASiblingsResources_DoesNotReachTheList`, `KeyBindingFocusRuleTests.AnImplicitThemeInAnotherViewsResources_IsSkipped`, `KeyBindingFocusRuleTests.AnImplicitThemeInADictionaryTheApplicationIncludes_IsReported` |
| A control's implicit theme is looked up under the type its `StyleKeyOverride` names with a `typeof`, read from IL, else under its own type; and a theme, named or implicit, for a control or its containers, whose `TargetType`, the type its prefix names, is not that style key or a base of it, or does not resolve to one type, or whose control's `StyleKeyOverride` returns its key other than by a `typeof`, is not applied, and is named in `Skipped` | A custom control that borrows its base's theme, as a WPF-to-Avalonia port's `LinesListBox` does, is judged by a theme that never reaches it, or by one the framework never applies to it, as a list whose style key is `ListBox`, given a theme for its own type, measured taking none of its setters, whether its key was a `typeof` or a field; or a theme whose prefixed `TargetType` names a type whose name another scanned type carries is followed | `ElementTypes.StyleKeyOf` remarks, `ElementTypes.StyleKeyKnown` remarks; `ThemeReader` (its private `Applied` and `TargetTypeOf`); `ControlThemes.TargetTypePrefixOf`; tests `KeyBindingFocusRuleTests.AListThatBorrowsItsBasesThemeKey_GetsItsBasesImplicitTheme`, `KeyBindingFocusRuleTests.AThemeThatIsNotTheStyleKeys_IsNotFollowed`, `ItemContainerBackgroundRuleTests.ATargetTypeIsTheTypeItsPrefixNames` |
| `BNXQ1003`, `BNXQ1005`, `BNXQ1006`, `BNXQ1009` and `BNXQ1010` never throw on a consumer's build output. A type whose dependencies do not load is unknown, never unfocusable, and one a scanned assembly defines is named in `Skipped` with what stopped it, never taken for a framework type or for one the scan was not given | One missing dependency fails the whole scan, a consumer's own control passes for a framework control, or a skip tells the consumer to pass the assembly they passed | `LoadedTypes`; `ElementTypes.IsUnreadable`, `ElementTypes.WhyUnresolved`; `TemplatePartRule` (its private `PartNamesIn` and `OwnerOf`); `KeyBindingFocusRule.Analyze`; `CustomControlPeerRule.Analyze`; `ItemContainerNameRule.Analyze`; `ItemContainerBackgroundRule.Analyze`; tests `UnloadableBuildOutputTests.BNXQ1003_AControlThatDoesNotLoad_IsNamedWithWhatStoppedIt`, `UnloadableBuildOutputTests.BNXQ1003_AMethodWhoseLocalsDoNotLoad_CostsOnlyItself`, `UnloadableBuildOutputTests.BNXQ1003_ANestedTypeWhoseOwnerDoesNotLoad_DoesNotThrow`, `UnloadableBuildOutputTests.BNXQ1005_AControlThatDoesNotLoad_SaysSo_NotThatItWasNotPassed`, `UnloadableBuildOutputTests.BNXQ1006_AControlThatDoesNotLoad_IsNamedWithWhatStoppedIt`, `UnloadableBuildOutputTests.BNXQ1006_APeerMethodThatCannotBeRead_IsNamedNotThrown`, `UnloadableBuildOutputTests.BNXQ1009_AControlThatDoesNotLoad_SaysSo_NotThatItWasNotPassed`, `UnloadableBuildOutputTests.BNXQ1010_AControlThatDoesNotLoad_SaysSo_NotThatItWasNotPassed` |
| `BNXQ1006` reports a themed control only when the `OnCreateAutomationPeer` its type resolves to is the root declaration. Any override in its chain is a peer: the control's own, a base's, or an empty one returned on purpose, which is how a decorative control opts out | A control with a peer is reported, or a decorative control can be silenced only from outside its code | `CustomControlPeerRule` remarks and its private `PeerOf`; tests `CustomControlPeerRuleTests.APeerlessThemedControl_IsReported`, `CustomControlPeerRuleTests.AControlDerivedFromOneWithAPeer_IsClean`, `CustomControlPeerRuleTests.AControlOnAFrameworkBaseWithAPeer_IsClean`, `CustomControlPeerRuleTests.ADecorativeControlThatSaysSoInItsCode_IsClean` |
| `BNXQ1006` checks only a control the scanned assemblies define, once however often it is themed, and names in `Skipped` every themed control when given no assemblies, a name two scanned types carry, and a type with no `OnCreateAutomationPeer` | A framework control's theme is blamed, one defect is reported twice, or a control nothing read passes as clean | `CustomControlPeerRule.Analyze`; tests `CustomControlPeerRuleTests.AThemeForATypeTheScanWasNotGiven_IsNeitherCheckedNorNamed`, `CustomControlPeerRuleTests.AControlThemedTwice_IsReportedOnce`, `CustomControlPeerRuleTests.WithoutAssemblies_EveryThemedControlIsNamedAsSkipped`, `CustomControlPeerRuleTests.ANameTwoScannedTypesCarry_IsSkipped`, `CustomControlPeerRuleTests.ATypeWithNoPeerMethod_IsSkippedNotReported` |
| `BNXQ1005`, `BNXQ1009` and `BNXQ1010` follow a control's themes through one helper, `ThemeReader`, which reads a setter's value in all three spellings: its `Value` attribute, a `Setter.Value` element, or its content | Two rules follow one theme to different answers, or a theme that writes a template or a value as the setter's content, as Avalonia's own themes do, reads as setting nothing | `ThemeReader` remarks, `ThemeReader.ValueOf`, `ThemeReader.ChainOf`, `ThemeReader.InBaseState`; tests `ItemContainerBackgroundRuleTests.TheSettersOtherSpellings_Count`, `ItemContainerBackgroundRuleTests.ARowThemeBasedOnAnotherInTheScan_IsFollowed`, `KeyBindingFocusRuleTests.TheSetterContentSpelling_Counts`, `KeyBindingFocusRuleTests.AnItemContainerThemeWrittenAsASettersContent_IsFollowed`, `ItemContainerNameRuleTests.TheSettersOtherSpellings_Count`, `ItemContainerNameRuleTests.AnItemTemplateTheListsThemeSets_IsFollowed` |
| `BNXQ1005`, `BNXQ1009` and `BNXQ1010` read a property in every spelling the XAML compiler takes, by the property its owner names: a setter's name alone, after an owner or in parentheses, in its `Property` attribute or a `Setter.Property` element, through `ResourceScope.SetterProperty`; and an element's own as an attribute or a property element, through `ThemeReader.Local`, where two spellings that set it or may decide nothing, because the framework applies each in turn. An owner is the type its prefix's namespace names, exactly in a `using:` or `clr-namespace:` one and by name in the framework's, Avalonia's, or WPF's or MAUI's in markup whose root is in it, through `ThemeReader.Owner` and `ResourceScope.OwnerOf`; one in any other URI, which only an assembly's `XmlnsDefinition` maps, is not decided. On an element the control's type or a base names its own, and another owner the property held by the field the compiler's route takes, through `ElementTypes.OwnerPropertyOn`: after an `AvaloniaObject` outside the chain with an instance property of the name, the field beside that property, which only a binding sets; after an owner with static `Set` or `Get` accessors, its own field, which any value sets; else the markup does not load. In a setter, a `TemplateBinding` or a path an owner names what its most-derived public field holds, the control's type or a base included, through `ElementTypes.PublicPropertyOf` and `ElementTypes.InChain`, and where two types in the chain carry its name, through `ElementTypes.AllInChain`, it is decided only where they hold one property. Either is a control's own only where it is the property the control's own spelling sets, through `ElementTypes.OwnPropertyOf`, as `AvaloniaProperty.Equals` compares two. A theme's setter is read against the type the theme targets, a style's for the rows against the rows', and a `TemplateBinding`'s or a path's against the control whose template it is. Where an owner or a field cannot be read, a class constructor failing among them, or a value after an owner that takes only a binding may or may not be one, or an owner is not decided, it is named in `Skipped`, never read as another property. A read that only adds doubt takes a setter after any owner, and `BNXQ1009` takes the last setter of Avalonia's `AutomationProperties`, by its owner's namespace, which another owner's attached property of the same name, set after it, does not hide | A setter written `(InputElement.Focusable)` or with a `Setter.Property` element, or an attribute written `Border.Background`, reads as setting nothing where the compiler sets it; a binding written `ComboBox.ItemsSource` on a list, or `Border.Background` on a template's `Panel`, each measured setting the element's own, reads as another owner's; an attached property of the same name, a consumer's `Paint.Background`, in a setter, a `TemplateBinding` or a path, measured setting or reading another property, is read as the control's own; a literal after a consumer's static alias with accessors, measured painting, reads as setting nothing; a field the compiler does not take, a subclass's that hides its base's or a protected one, decides what an owner names, or a setter after a subclass that hides it, measured setting another property, is read as the control's own; an owner read by its name without its prefix, `alt:TemplatedControl` or one in a consumer's own URI, measured setting another property, is read as the row's; an owner whose class constructor fails is read as another property's; the first of two spellings decides where the framework is left with another; another owner's `Name`, or a consumer's own `AutomationProperties.Name`, set after Avalonia's, hides it; or one rule follows a spelling another does not | `ThemeReader.OwnershipOf` remarks, `ThemeReader.Local` remarks, `ThemeReader.Owner` remarks, `ThemeReader.LastSetting`, `ThemeReader.MayGive`, `ThemeReader.SetsProperty`, `ThemeReader.MaySet`, `ElementTypes.OwnerPropertyOn` remarks, `ElementTypes.PublicPropertyOf` remarks, `ElementTypes.OwnPropertyOf` remarks, `ElementTypes.InChain`, `ResourceScope.SetterProperty` remarks, `ResourceScope.OwnerOf` remarks, `ItemContainerNameRule` (its private `AutomationOwner`); tests `ItemContainerBackgroundRuleTests.TheBackgroundsOtherSpellings_Count`, `ItemContainerBackgroundRuleTests.AnElementsPropertiesAfterABaseOfItsType_Count`, `ItemContainerBackgroundRuleTests.AnItemsSourceAfterAnOwner_IsTheListsWhereThatOwnerHoldsItsOwn`, `ItemContainerBackgroundRuleTests.ABackgroundAfterAnotherOwner_IsTheElementsOwnOnlyByABinding`, `ItemContainerBackgroundRuleTests.ABindingToAnotherOwnersBackground_IsNotTheRows`, `ItemContainerBackgroundRuleTests.ARowThemeAfterAnotherOwner_IsTheListsByABinding`, `ItemContainerBackgroundRuleTests.ASetterForAnotherOwnersBackground_LeavesTheRowsOwn`, `ItemContainerBackgroundRuleTests.AnOwnerOrAValueThatCannotBeRead_IsSkipped`, `ItemContainerBackgroundRuleTests.AnElementsPropertyInTwoSpellings_DecidesNothing`, `ItemContainerBackgroundRuleTests.AnAttachedOwnerOfTheElementsOwnProperty_TakesAnyValue`, `ItemContainerBackgroundRuleTests.TheFieldTheCompilerTakes_NamesTheProperty`, `ItemContainerBackgroundRuleTests.AnOwnerIsTheTypeItsPrefixNames`, `ItemContainerBackgroundRuleTests.AnOwnerTwoTypesInTheChainAreCalled_IsDecidedOnlyWhereTheyAgree`, `ItemContainerNameRuleTests.AnItemContainerThemeInTheSettersOtherSpellings_NamesThem`, `ItemContainerNameRuleTests.PropertiesWrittenAfterAnOwner_CountWhereThatOwnerHoldsThem`, `ItemContainerNameRuleTests.AnOwnerThatCannotBeRead_IsSkipped`, `ItemContainerNameRuleTests.ATextGivenTwoWays_DecidesNothing`, `ItemContainerNameRuleTests.AnotherOwnersNameAfterTheAutomationName_DoesNotHideIt`, `ItemContainerNameRuleTests.ATextAfterAnOwner_IsTheTextBlocksWhereTheCompilersFieldHoldsIt`, `KeyBindingFocusRuleTests.TheOtherSpellingsOfFocusable_Count`, `KeyBindingFocusRuleTests.PropertiesAfterAnotherOwner_AreTheListsByABinding`, `KeyBindingFocusRuleTests.AnOwnerThatCannotBeRead_IsSkipped`, `KeyBindingFocusRuleTests.FocusableGivenTwoWays_DecidesNothing`, `KeyBindingFocusRuleTests.AFocusableAfterAnAttachedOwnerOfTheListsOwn_TakesAnyValue`, `KeyBindingFocusRuleTests.ASettersOwnerThatHidesTheField_NamesAnotherProperty` |
| `BNXQ1009` checks a row only when the peer that its container's `OnCreateAutomationPeer` creates, read from IL, is Avalonia's `ListItemAutomationPeer` or `TreeViewItemAutomationPeer`. A row whose peer writes its own `GetNameCore` is named in `Skipped` | Another framework's rows, or an `ItemsControl`'s presenters, are judged by Avalonia's fallback, or a row that names itself is reported | `ItemContainerNameRule` remarks, `RowPeers.Of`; tests `ItemContainerNameRuleTests.AnItemsControlsPresenters_AreNotSubjects`, `ItemContainerNameRuleTests.RowsWhosePeerNamesThemItself_AreSkipped` |
| `BNXQ1009` takes a list or tab row as named by: a style reaching the host whose selector is the container type alone, by Avalonia's `AutomationProperties`, where one whose owner is not decided leaves the rows in `Skipped`; its `ItemContainerTheme` or implicit theme; `DisplayMemberBinding`; a `TextBlock` root with text, of its item template or, with none, of the data template chosen for its items; or any other such root that declares `AutomationProperties.Name` or `LabeledBy`. A `TextBlock`'s own name does not count. A type selector matches the container's style key exactly, and `:is()` matches any type in its chain | A row that a style or a named root names is reported; a row a `TextBlock`'s ignored name leaves to `ToString()` passes; or a style for `ListBoxItem` is taken to name a `ComboBoxItem` | `ItemContainerNameRule` remarks; tests `ItemContainerNameRuleTests.AStyleForTheRowTypeInTheListsStyles_NamesThem`, `ItemContainerNameRuleTests.AStyleNamesADerivedRowOnlyWhenItsSelectorPicksIt`, `ItemContainerNameRuleTests.AnItemContainerTheme_NamesThem`, `ItemContainerNameRuleTests.DisplayMemberBinding_NamesTheRows`, `ItemContainerNameRuleTests.ATextBlockRootWithNoText_LeavesTheRowsToToString`, `ItemContainerNameRuleTests.ARootThatDeclaresAName_NamesTheRows` |
| `BNXQ1009` reports a row left to `ToString()` only when the items' type, the one its item template's `x:DataType` names, else the one its `ItemsSource` binding reads, resolves to `object`'s, `ValueType`'s or a record's compiler-written `ToString()`. It names in `Skipped` a type markup does not name, one that is abstract, an interface or a control, and one with a subclass that writes its own | A view model whose own `ToString()` names its rows is reported, or one whose items may be a subclass that does | `ItemContainerNameRule` (its private `WriterOf`, `SubclassWriting` and `ByItemType`); tests `ItemContainerNameRuleTests.AnItemTypeWhoseToStringNamesIt_IsClean`, `ItemContainerNameRuleTests.ARecord_IsReported_BecauseItsToStringListsEveryProperty`, `ItemContainerNameRuleTests.AnItemTypeThatSaysNothingOfTheItems_IsSkipped`, `ItemContainerNameRuleTests.ASubclassThatWritesToString_IsSkipped`, `ItemContainerNameRuleTests.AnItemTypeIsFoundInItsOwnNamespace`, `ItemContainerNameRuleTests.WithNoItemTemplate_ItemsWhoseToStringNamesThem_AreClean`, `ItemContainerNameRuleTests.WithNoItemTemplate_ItemsWhoseTypeSaysNothingOfThem_AreSkipped` |
| Where no item template's `x:DataType` names the items' type, `BNXQ1009` reads it as the XAML compiler types a compiled binding: the `ItemsSource` binding's path against the nearest `x:DataType`, a data template's `DataType`, or, for an item template that declares neither, the items of the control it templates, a cast in the path starting it again at the cast's type; each step is the most derived public instance property of its name, an interface's inherited ones included, and the items are the `T` of the one `IEnumerable<T>` the last step's type is. A plain `{Binding}` is read as compiled. A `ReflectionBinding`, a `{Binding}` that `x:CompileBindings="False"` makes one, a binding that sets anything but its path and its mode, a path the rule does not read, a `DataContext` set on the way, and a template, a style or a resource around the binding are named in `Skipped` | A reflection binding is typed by an `x:DataType` it ignores, a converter's or a fallback's items are read as the path's, a subclass's property that hides its base's is read as the base's, or a list whose items' type is in the markup stays skipped, as ClaudeForge's six were | `ItemContainerNameRule` remarks and its private `ItemsOf`, `DataTypeAt`, `PathOf` and `PropertyOf`; tests `ItemContainerNameRuleTests.AnItemTemplateWithNoDataType_TakesTheItemsSourcesType`, `ItemContainerNameRuleTests.TheBindingsSpellingsAndPaths_ReadTheItems`, `ItemContainerNameRuleTests.ABindingWrittenAsAnElement_ReadsTheItems`, `ItemContainerNameRuleTests.ACastInThePath_StartsItAtTheCastsType`, `ItemContainerNameRuleTests.APathsStep_IsThePropertyTheCompilerTakes`, `ItemContainerNameRuleTests.TheNearestDataType_IsRead`, `ItemContainerNameRuleTests.AListInTheItemTemplateOfAnother_ReadsThatOnesItems`, `ItemContainerNameRuleTests.AnItemsSourceTheRuleCannotRead_IsSkipped`, `ItemContainerNameRuleTests.CompileBindingsOff_MakesAPlainBindingAReflectionOne`, `ItemContainerNameRuleTests.WhereTheDataTypeIsNotRead_TheRowsAreSkipped`, `ItemContainerNameRuleTests.AnXDataTypeNoScannedAssemblyDefines_LeavesTheItemsUnread` |
| With no item template, `BNXQ1009` takes a list or tab row as presented by the first data template that matches every item, searched as `FindDataTemplate` searches at 12.1.3: the list's own `DataTemplates`, each ancestor's in its file, nearest first and each in order, then the application's; else by a `TextBlock` of the item's `ToString()`. A `DataTemplate` matches an item that is an instance of its `DataType`, which an `x:DataType` gives it where none is written. One that may match only some of the items, one whose matching its own code decides, one with no data type, which a `DataTemplates` collection refuses, one whose `DataType` does not resolve but may be the items' own type, and one in another view that may match are named in `Skipped`, and so is one that a control holds under the owner of the collection the list's template is in, which is asked before that owner's own, or one anywhere in its file where the search leaves the markup's tree; one beside the list in its file does not reach it. A content, a header or a template for either that the rows' theme or a style gives them, which the framework leaves where it finds it set, data templates in their own template, and controls as items, which a row presents as they are, are named in `Skipped` | Rows a data template chosen by type names are reported, or ones it leaves to `ToString()` pass; a template for another type, or one an `x:DataType` gives a type, is taken to present them; a template for the items' own type written through a prefix the rule does not read is passed over; or a view locator in the application silently decides | `ItemContainerNameRule` remarks and its private `ByDataTemplates`, `Chosen`, `Matches`, `MayBe` and `LeavesTheTree`; `ResourceScope.Applications`; tests `ItemContainerNameRuleTests.WithNoItemTemplate_ItemsWhoseTypeWritesNoToString_AreReported`, `ItemContainerNameRuleTests.ADataTemplateThatMatchesEveryItem_PresentsTheRows`, `ItemContainerNameRuleTests.ADataTemplateWithNoDataType_IsSkipped_AsMarkupThatDoesNotLoad`, `ItemContainerNameRuleTests.ADataTemplateWhoseRootDeclaresNoName_LeavesTheRowsToToString`, `ItemContainerNameRuleTests.ADataTemplateForAnotherType_IsPassedOver`, `ItemContainerNameRuleTests.TheFirstDataTemplateThatMatches_PresentsTheRows`, `ItemContainerNameRuleTests.ADataTemplateThatMayMatchSomeItems_IsSkipped`, `ItemContainerNameRuleTests.ADataTemplateForATypeTheScanDoesNotHold_MatchesNoItemOfASealedType`, `ItemContainerNameRuleTests.ADataTemplateForTheItemsOwnTypeThatDoesNotResolve_MayMatch`, `ItemContainerNameRuleTests.TheNearestDataTemplates_AreSearchedFirst`, `ItemContainerNameRuleTests.AnAncestorsOrTheApplicationsDataTemplate_PresentsTheRows`, `ItemContainerNameRuleTests.ADataTemplateInAnotherView_MattersWhereItMayMatch`, `ItemContainerNameRuleTests.ADataTemplateBesideTheListInTheSameFile_DoesNotReachIt`, `ItemContainerNameRuleTests.ADataTemplateBesideTheListsOwnTemplate_PresentsTheRows`, `ItemContainerNameRuleTests.AListInAnItemTemplate_IsReachedByItsHostsDataTemplates`, `ItemContainerNameRuleTests.ADataTemplateWhereTheListsTemplateMayBePresented_MayBeSearchedFirst`, `ItemContainerNameRuleTests.ADataTemplateAControlTheOwnerHolds_IsAskedBeforeTheOwnersOwn`, `ItemContainerNameRuleTests.WhatTheRowsThemeGivesThemToPresent_IsSkipped`, `ItemContainerNameRuleTests.AStyleThatGivesTheRowsWhatTheyPresent_IsSkipped`, `ItemContainerNameRuleTests.ControlsAsItems_ArePresentedAsTheyAre_WhateverDataTemplateMatches` |
| `BNXQ1009` reports a tree row that no style or container theme names, whatever its template shows | A tree passes because its template holds a `TextBlock`, which names only itself | tests `ItemContainerNameRuleTests.ATreeViewsRows_AreReported_WhateverTheyPresent`, `ItemContainerNameRuleTests.ATreeViewWithNamedRows_IsClean` |
| `BNXQ1010` checks a row only when its container's peer is Avalonia's `ListItemAutomationPeer`, read through `RowPeers` as `BNXQ1009` reads it: a list, combo or tab row. A tree row is left out | A tree row that Fluent's and Simple's header presenter makes clickable is reported, or another framework's rows, whose styling the rule does not read, count as inspected and clean | `ItemContainerBackgroundRule` remarks, `RowPeers` remarks; tests `ItemContainerBackgroundRuleTests.ATreeView_IsNotASubject`, `ItemContainerBackgroundRuleTests.AnItemsControlsPresenters_AreNotSubjects`, `ItemContainerBackgroundRuleTests.AComboBoxsRows_AreChecked`, `ItemContainerBackgroundRuleTests.ATabControlsTabs_AreChecked` |
| `BNXQ1010` takes a row's `Background` as null when it is `{x:Null}` or a binding with no non-null `TargetNullValue`, in any of a setter's spellings; `FallbackValue` does not count, and a resource or a static member is taken as a brush. A style for the row type alone wins over the row theme, and a theme `BasedOn` nothing that sets none leaves the default the row's type registers, null unless its static constructor makes it a brush | The converter case that arose passes, or a binding made safe with `TargetNullValue` is reported | `ItemContainerBackgroundRule` (its private `Read` and `OfBinding`); tests `ItemContainerBackgroundRuleTests.ABoundBackgroundWithNoTargetNullValue_IsReported`, `ItemContainerBackgroundRuleTests.ABackgroundBoundThroughAConverter_IsReported`, `ItemContainerBackgroundRuleTests.ABoundBackgroundWithATargetNullValue_IsClean`, `ItemContainerBackgroundRuleTests.ATargetNullValueOfNull_IsReported`, `ItemContainerBackgroundRuleTests.AFallbackValueAlone_IsReported`, `ItemContainerBackgroundRuleTests.TheSettersOtherSpellings_Count`, `ItemContainerBackgroundRuleTests.AStyleOverTheRowTheme_Wins`, `ItemContainerBackgroundRuleTests.ARowThemeBasedOnNothingWithNoBackground_IsReported` |
| `BNXQ1010` reads the state a row starts in. A style that paints the rows only in an interaction state of the row, or of the control whose template it styles, changes nothing; but wherever markup in the scan sets `IsEnabled` to anything but `True`, or gives a control a `Command`, as an attribute, a property element or a setter, in any spelling the XAML compiler takes, after an owner that names the property, as `ThemeReader` reads one, in parentheses or in a setter's `Setter.Property` element included, the rows and what their template holds may start in `:disabled`, and where their list, its theme or its type always selects a row, or markup may select one in any such spelling, a `SelectionMode` by name or by number included, the rows selected may start in `:selected`, which are then not such states. A `:selected` style paints only the rows selected: a null background the rest draw is still reported, and, where it paints a part of their template, rows whose template draws nothing are named in `Skipped`. One that clears them in such a state, or picks some rows by a class, a name, a place, a property or the absence of a state and may clear them, is named in `Skipped`. A style whose selector holds of every row in that state, whatever the data, sets every row's `Background` however it is written: the row type alone, the host alone as its context through either combinator, or a `:not()` of a class or a name. A `:not()` of a state holds only while a row is out of that state, so it may not hold of every row. One that may hold of every row and paints leaves a null background in `Skipped`, and so does, either way, a row theme's nested style that may give rows another template or theme. One asking for a property the rows' theme binds to each row's item is taken to paint only some rows, and one that paints only some rows changes no other | A null row that a hover style paints passes, a row that a hover style clears passes as clean although one measured losing the click, or a class that clears some rows passes; a style written `ListBox ListBoxItem`, `ListBox > ListBoxItem` or with a `:not()` of a class, each measured painting every row, is reported; a `:not(:pointerover)` style that paints a null background, measured missing a click after a hover, passes; or a nested style that gives the rows a template drawing nothing, measured leaving them missing the click, passes; or the rows of a disabled list, or of one whose theme disables them, that a `^:disabled` style paints, measured drawing from the start, are reported; or rows a disabled items panel or presenter in the list's template disables, a part a view's markup or a templated control's theme disables, or the first row of a list that always selects one, each measured taking a style for that state from the start, are reported; or rows disabled or selected by markup written after an owner, a binding after another owner that holds the property included, in a setter's parentheses or its `Setter.Property` element, or, for a `SelectionMode`, as a number, each measured taking such a style from the start, are reported; or the null background of every row not selected passes wherever a list binds its selection and a row theme paints a part while selected, as Fluent's do | `StyleSelectors.RequiresState` remarks, `StyleSelectors.RowsPicked` remarks; `ItemContainerBackgroundRule` (its private `Classify`, `Nested`, `DisabledAtStart`, `DisablingIn`, `MayDisable`, `GivesACommand`, `GivenInAnySpelling`, `SetsInAnySpelling`, `SelectedAtStart`, `MaySelect`, `ModeSelects` and `OnlySelected`); `ElementTypes.DefaultText`; tests `ItemContainerBackgroundRuleTests.RowsThatMarkupMayDisable_MayStartInDisabled`, `ItemContainerBackgroundRuleTests.WhateverMarkupDisables_MayStartInDisabled`, `ItemContainerBackgroundRuleTests.RowsThatMarkupMaySelect_MayStartInSelected`, `ItemContainerBackgroundRuleTests.ASelectionAfterAnotherOwner_SelectsByABinding`, `ItemContainerBackgroundRuleTests.APartPaintedWhileSelected_LeavesTheOtherRowsAsTheyWere`, `ItemContainerBackgroundRuleTests.WhatTheRowsTemplateHolds_MayStartDisabledWithThem`, `ItemContainerBackgroundRuleTests.ATemplatedControlInRowsThatMayStartDisabled_MayBePainted`, `ItemContainerBackgroundRuleTests.AStyleThatHoldsOfEveryRow_SetsEveryRowsBackground`, `ItemContainerBackgroundRuleTests.AStyleThatMayHoldOfEveryRow_LeavesANullBackgroundUndecided`, `ItemContainerBackgroundRuleTests.ANotOfAStateThatClearsTheRows_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleAskingForAPropertyBoundToEachItem_PicksOnlySomeRows`, `ItemContainerBackgroundRuleTests.ANestedStyleThatMayHoldOfEveryRow_MayPaintThem`, `ItemContainerBackgroundRuleTests.ANestedStyleThatGivesRowsAnotherTemplate_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleThatPaintsRowsInAState_LeavesTheStartingStateAsItWas`, `ItemContainerBackgroundRuleTests.AStyleThatClearsRowsInAState_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleNestedInAState_IsAStateStyle`, `ItemContainerBackgroundRuleTests.EveryInteractionState_IsAState`, `ItemContainerBackgroundRuleTests.AStyleThatClearsRowsWithAClass_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleThatPaintsRowsWithAClass_LeavesTheRestAsTheyWere`, `ItemContainerBackgroundRuleTests.AStateOfTheList_IsNotAStateOfTheRows`, `ItemContainerBackgroundRuleTests.AStateInsideNot_IsNotAskedFor`, `ItemContainerBackgroundRuleTests.StatesAndPropertyStyles_LeaveANullBaseNull`, `ItemContainerBackgroundRuleTests.AStateThatClearsTheBackground_IsSkipped`, `ItemContainerBackgroundRuleTests.ANestedStyleThatClearsSomeRows_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleNestedInANestedStyle_IsRead` |
| `BNXQ1010` reports rows whose `Background` can be null where their template draws it, and rows whose template draws no background. What draws the row's `Background` is an element of the template that draws its own, a `Border`, a `Panel`, a `ContentPresenter`, a `TextPresenter` or a `TextBlock`, or a shape's `Fill`, taking the row's by `TemplateBinding`, named alone, after an owner that holds the row's own property, or in parentheses, a binding to the templated parent, or one to the ancestor of the row's type that the row is, through `$parent[T]` or an `AncestorType` in a `RelativeSource` that finds an ancestor, as one with no `Mode` or `FindAncestor` does and one with `Self` or `DataContext` does not, counted from the nearest at the level it names, past any of that type the template puts between, where one with a templated control between in the visual tree, one in the visual tree to a type every element is, or one past an ancestor that does not resolve, is not the row's; or a templated control taking it whose own template draws its own, read through its own theme, where the `Background` that a view's, or any type's, own markup, or a base's, gives it outranks the one a template gives it, but for a binding. What a `ContentPresenter` or a `TransitioningContentControl` in the template holds as markup content, which has no templated parent and whose logical parent may not be where the template writes it, is not followed. A binding that takes it with a `TargetNullValue` that is not null paints whatever the row's is, and one through a converter, or anything but its path, its source, its mode, its fallback and what only writes back, is not decided. A theme that sets no `Template` down its chain leaves the framework's template, or with a chain `BasedOn` nothing the default a `ContentControl` has, both of which draw it; only a `Template` of `{x:Null}` presents nothing. Where something draws the row's `Background`, that value alone decides. Where nothing does, an element that paints a brush of its own, however little of the row it covers, makes the rows clean, and one that may paint leaves them in `Skipped`: one its theme, the one it names or its implicit one, may paint, or a style that may set its background, theme or template, its type selector matched against its style key, among the scan's styles or declared in the `Styles` of what holds it in the template; one given a `Template` of its own, which is not followed; a templated control with no theme in the scan but a `ContentControl` or a `UserControl`; one outside the framework that draws itself in a `Render` of its own; one whose own markup, or a base's, two files declare, a type and a base both have, or its assembly holds compiled where the scan does not; and one whose type the scan does not resolve. A value given to an element that is not known to draw it is not decided. A value the template's markup gives an element that a style with a condition, a nested style with one in its theme, or its own code may replace is not decided, and one only a plain style or a theme's own setter would replace stands. A style that paints, clears or binds a part of the rows' template, or gives it a fill, a theme or a template, where that could change the answer, leaves the rows in `Skipped` | A theme that sets `Transparent` over a template that draws nothing passes, as the measured one did; a template painted through a class style or a style declared in it, bound to the row as an ancestor, or left to the default by a theme that sets none, or an element an implicit theme paints, a style paints through a borrowed style key, or that draws itself, each measured taking the click, is reported; a template whose binding gives a null row `Transparent`, by a `TargetNullValue` or a converter, or a control given a template of its own that paints, each measured taking the click, is reported; a templated control handed the row's `Background` by a template that ignores it passes as drawing it; a style with a condition that repaints what draws the row's `Background` is ignored; a binding to an outer row, `$parent[ListBoxItem;1]` or `AncestorLevel=2`, measured drawing that row's background or none, is taken as the row's; a view whose own markup is `Transparent`, measured drawing it whatever the template gives it, is reported; or a part a style binds to the row's background, measured drawing it, is reported as drawing nothing; a binding by `RelativeSource Mode=Self` with an `AncestorType`, measured binding the element itself, or a `TemplateBinding` or a `$parent` binding in a presenter's markup content, measured taking nothing or the list's, is taken as the row's; or a `Border` or `Panel` subclass whose own markup, or its base's, paints, measured drawing it, is reported, or is read as having none where its assembly holds that markup compiled and the scan does not | `ItemContainerBackgroundRule` (its private `TemplateDrawing`, `PaintOf`, `Draws`, `MayOverride`, `TemplatedPaint`, `OwnTemplate`, `OwnMarkupOf`, `OwnMarkupUnread`, `MarkupDownTheChain`, `OwnMarkupPaint`, `IsBinding`, `ThroughTheBinding`, `PropertyNamed`, `PathProperty`, `PartPresenting`, `StylesDeclaredAround`, `RowsProperty`, `RowsPath`, `PresenterHolding` and `NearerAncestors`); `StyleSelectors.MayPick`, `StyleSelectors.IsConditional`; `ElementTypes.DrawsItself`, `ElementTypes.HasCompiledMarkup`; tests `ItemContainerBackgroundRuleTests.WhatATemplatesBindingDoesWithANull_Decides`, `ItemContainerBackgroundRuleTests.ALocalTemplate_IsNotFollowed`, `ItemContainerBackgroundRuleTests.AStyleThatReachesIntoTheRowsTemplate_MattersOnlyForWhatAPartDraws`, `ItemContainerBackgroundRuleTests.AnImplicitThemeReachesAnElementWithNoTemplate`, `ItemContainerBackgroundRuleTests.ATypeSelector_MatchesTheStyleKey`, `ItemContainerBackgroundRuleTests.AControlThatDrawsItself_MayPaint`, `ItemContainerBackgroundRuleTests.AnElementWhoseTypeDoesNotResolve_MayPaint`, `ItemContainerBackgroundRuleTests.ANullTheme_LeavesTheImplicitOne`, `ItemContainerBackgroundRuleTests.ARowTemplateThatDrawsNoBackground_IsReported`, `ItemContainerBackgroundRuleTests.ARowTemplateWithABrushOfItsOwn_IsClean`, `ItemContainerBackgroundRuleTests.ATemplatedParentBinding_DrawsTheRowsBackground`, `ItemContainerBackgroundRuleTests.ABackgroundAfterAnotherOwner_IsTheElementsOwnOnlyByABinding`, `ItemContainerBackgroundRuleTests.ABindingToAnotherOwnersBackground_IsNotTheRows`, `ItemContainerBackgroundRuleTests.ABindingToTheRowAsAnAncestor_DrawsItsBackground`, `ItemContainerBackgroundRuleTests.ABindingToAnotherAncestor_IsNotTheRows`, `ItemContainerBackgroundRuleTests.ARelativeSourceThatFindsNoAncestor_IsNotTheRows`, `ItemContainerBackgroundRuleTests.WhatAPresenterHoldsAsMarkupContent_IsNotFollowed`, `ItemContainerBackgroundRuleTests.ARowTemplateHeldInAResource_IsFollowed`, `ItemContainerBackgroundRuleTests.ARowThemeBasedOnNothingWithNoTemplate_TakesTheDefaultTemplate`, `ItemContainerBackgroundRuleTests.ANullTemplate_DrawsNoBackground`, `ItemContainerBackgroundRuleTests.WhereTheTemplateDrawsTheRowsBackground_ThatValueDecides`, `ItemContainerBackgroundRuleTests.ATemplateWhoseOnlyPaintCannotBeEvaluated_IsSkipped`, `ItemContainerBackgroundRuleTests.ATemplateElementBoundToData_IsSkipped`, `ItemContainerBackgroundRuleTests.ATemplateElementAStyleMayPaint_IsSkipped`, `ItemContainerBackgroundRuleTests.ATemplateElementNoStylePicks_IsReported`, `ItemContainerBackgroundRuleTests.WhatATemplateGivesAnElement_IsOutrankedOnlyByAConditionOrCode`, `ItemContainerBackgroundRuleTests.ABackgroundOnAnElementNotKnownToDrawIt_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleDeclaredInTheTemplate_MayPaintWhatItReaches`, `ItemContainerBackgroundRuleTests.ATemplateElementsTheme_IsRead`, `ItemContainerBackgroundRuleTests.ATemplatedControlWithNoThemeInTheScan_IsSkipped`, `ItemContainerBackgroundRuleTests.ATemplatedControlsOwnTheme_IsRead`, `ItemContainerBackgroundRuleTests.ATemplatedControlTakingTheRowsBackground_DrawsItOnlyThroughItsOwnTemplate`, `ItemContainerBackgroundRuleTests.AContentControlOrAUserControl_DrawsOnlyItsOwnBackground`, `ItemContainerBackgroundRuleTests.AViewInTheRowTemplate_IsReadFromItsOwnMarkup`, `ItemContainerBackgroundRuleTests.AnItemsControlInTheRowTemplate_IsReadByItsOwnTheme`, `ItemContainerBackgroundRuleTests.AStyleThatPaintsAPartOfTheTemplate_MattersOnlyWhereNothingElseDraws`, `ItemContainerBackgroundRuleTests.AStyleThatClearsAPartOfTheTemplate_IsSkipped`, `ItemContainerBackgroundRuleTests.ARowThemePaintingAPartOfItsTemplate_IsSkipped`, `ItemContainerBackgroundRuleTests.ARowThemePaintingAPartInAState_ChangesNothing`, `ItemContainerBackgroundRuleTests.ARowThemeClearingAPartOfItsTemplate_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleThatBindsAPartOfTheTemplate_MayDrawThere`, `ItemContainerBackgroundRuleTests.ARowThemeBindingAPartOfItsTemplate_MayDrawThere`, `ItemContainerBackgroundRuleTests.AViewsOwnBackground_OutranksTheTemplates`, `ItemContainerBackgroundRuleTests.ATypesOwnMarkup_IsReadWhateverItDerivesFrom` |
| `BNXQ1010` decides nothing by what the rows present: the item template, the list's, its theme's or one chosen by the item's type, a view locator or a template that chooses in code, and whatever a presenter in the rows' template shows through a content template or a data template, the row's content, the item or content it is given. Nor does it by what hides an element from the pointer, how much of the row an element covers, or what a `Popup`, a tooltip, a flyout, a menu, styles or resources hold. A row whose content paints across it is still reported, by design | Rows whose item template paints are silenced, so the rule rests again on spans and presenters no markup decides; or a template drawn only by what a `Popup` holds, shown only while it is open, passes | `ItemContainerBackgroundRule` remarks and its private `Presented`; tests `ItemContainerBackgroundRuleTests.AnItemTemplateThatPaints_DecidesNothing`, `ItemContainerBackgroundRuleTests.WhatAStyleOrAThemeGivesTheRowsToPresent_DecidesNothing`, `ItemContainerBackgroundRuleTests.ATabsHeader_DecidesNothing`, `ItemContainerBackgroundRuleTests.WhatChoosesInCodeWhatTheRowsPresent_DecidesNothing`, `ItemContainerBackgroundRuleTests.WhatAPresenterInTheTemplateShows_DecidesNothing`, `ItemContainerBackgroundRuleTests.WhatHidesWhatDraws_DecidesNothing`, `ItemContainerBackgroundRuleTests.WhatAPropertyElementOrAPopupHolds_IsNotPresented` |
| `BNXQ1010` takes a background, or a shape's fill, no markup sets as the default the type registers, read once every class constructor in its chain has run: a row type's decides its rows, and an element's in the rows' template paints. A value its own code sets through the `Background` or the `Fill` setter, or a list's own code outside the framework sets on its rows, is a local value markup does not show, so it leaves the rows in `Skipped`, as does a default that cannot be read. That code is read from IL in every type of the chain, in any assembly, with the closures and state machines the compiler nests in each; compiled markup, the methods whose names begin with `!` and the types XamlX nests in a view for its templates, named `XamlClosure_`, is read as markup | A row whose type registers a brush is reported; a control or a view whose static constructor makes its background a brush, or whose code sets one in a constructor, a lambda, after an `await` or in a library's base class, or a shape whose static constructor or code fills it, or a list whose code paints the rows it prepares, each measured taking the click, leaves its rows reported; or a compiled view whose markup paints nothing is skipped with a false reason | `ElementTypes.PaintsByDefault`, `ElementTypes.CodeSetsPaint`, `ElementTypes.ConsumerCodeSetsPaint` and their private `LinkSetsPaint`; `ItemContainerBackgroundRule` (its private `PaintOf`, `TemplatedPaint` and `ByTheme`); tests `ItemContainerBackgroundRuleTests.AFillATypeOrItsCodeGives_Paints`, `ItemContainerBackgroundRuleTests.ADefaultBackgroundTheTypeRegisters_Paints`, `ItemContainerBackgroundRuleTests.ADefaultBackgroundThatCannotBeRead_IsSkipped`, `ItemContainerBackgroundRuleTests.ABackgroundSetInCode_IsSkipped`, `ItemContainerBackgroundRuleTests.CodeOutsideTheScan_IsRead_AndCompiledMarkup_IsReadAsMarkup` |
| `BNXQ1009` and `BNXQ1010` read a selector through one helper, `StyleSelectors`, and recognise a row through one, `RowPeers`. A type selector matches the style key, for a row and for any element alike | One style picks every row for one rule and some rows for the other, one rule checks rows the other leaves alone, or a style that paints a control borrowing another's style key is passed over | `StyleSelectors` remarks, `StyleSelectors.MayPick` remarks, `RowPeers` remarks; tests `ItemContainerNameRuleTests.AStyleNamesADerivedRowOnlyWhenItsSelectorPicksIt`, `ItemContainerNameRuleTests.AnItemsControlsPresenters_AreNotSubjects`, `ItemContainerBackgroundRuleTests.StylesThatDisagree_AreSkipped`, `ItemContainerBackgroundRuleTests.AnItemsControlsPresenters_AreNotSubjects`, `ItemContainerBackgroundRuleTests.ATypeSelector_MatchesTheStyleKey` |
| A style reaches an element from its own `Styles`, an ancestor's, the application's, and the files a `StyleInclude` among them names. `BNXQ1009` names a style in another file that none of them includes in `Skipped`, `BNXQ1010` does so where the style could change its answer, and both ignore one beside the element in the same file | A style that never reaches a list silences it, or one that may reach it is ignored | `ResourceScope.StylesReaching` remarks; tests `ItemContainerNameRuleTests.AStyleInAnAncestorsStyles_NamesThem`, `ItemContainerNameRuleTests.AStyleTheApplicationIncludes_NamesThem`, `ItemContainerNameRuleTests.AStyleInAnotherView_IsSkipped`, `ItemContainerNameRuleTests.AStyleBesideTheListInTheSameFile_DoesNotReachIt`, `ItemContainerBackgroundRuleTests.AStyleInAnotherFile_IsSkipped`, `ItemContainerBackgroundRuleTests.AStyleInAnotherFileThatPaints_MattersOnlyWhereTheBackgroundCanBeNull` |
| Every rule that asks which controls are themed reads `ControlThemes.In`, which takes a `ControlTheme`'s `TargetType` in both spellings | A theme written `{x:Type local:Thing}` is read by one rule and missed by another | `ControlThemes` remarks; tests `TemplatePartRuleTests.TheXTypeSpelling_IsUnderstood`, `CustomControlPeerRuleTests.TheXTypeSpelling_IsUnderstood` |
| A rule id is permanent public API. Its number never changes, and its prefix, `BNXQ`, the family's `BN` and the product's initials, never changes once an id carrying it has shipped | Any suppression keyed on the old id silently stops applying | `IXamlRule.Id`; tests `RulesCatalogTests.EveryRuleId_IsUnique`, `RulesCatalogTests.EveryRuleId_CarriesTheFamilyPrefix` |
| Every rule type is public and has a parameterless constructor | The rule vanishes from the README table, or generating the table throws | tests `RulesCatalogTests.EveryRuleType_IsPublic`, `RulesCatalogTests.TheReadmeRulesTable_IsWhatTheRuleTypesSay` |
| The rules table in `docs/xamlquality.md`, between the `BEGIN GENERATED RULES` and `END GENERATED RULES` markers, is generated; the prose outside them is hand-written | A hand edit to the table fails the build, and a generator that owned the whole section would delete the prose about how rules overlap | `RulesCatalogTests` |
| The rule API and the whole ThemeAudit audit surface are public contracts | Narrowing a member breaks a consumer the next time it moves off its pinned version | `src/XamlQuality/Properties/AssemblyInfo.cs`; the consumers, and the members they use, in `PROGRESS.md` |
| The content digest is rooted at the deepest directory the scanned files share, never at the configuration's directory | The same files hash differently on a developer machine and in CI, so a committed report can never pass in both | `AuditRunner.CommonRoot`, `AuditRunner.Digest`; tests `DigestLocationTests.TheSameContentInTwoLocations_HashesTheSame`, `DigestLocationTests.ARenameThatDoesNotReorder_StillMovesTheDigest` |
| The content digest reads each CRLF pair as LF, and a consumer's files are read in the order of their path below the scanned directory, written with `/` | A Windows checkout, where git writes LF as CRLF and `\` sorts sibling directories differently, hashes differently from Linux and macOS, so a committed report fails on one platform | `AuditRunner.Digest` remarks, `XamlFiles` remarks; tests `DigestPlatformTests.ACrlfCheckout_HashesAsAnLfOne`, `DigestPlatformTests.AnLfCheckout_HashesAsItAlwaysHas`, `DigestPlatformTests.AConsumersFiles_AreOrderedByTheirPathWithForwardSlashes` |
| An audited colour is what Avalonia 12.1.3 draws: a brush's alpha is its colour's times every opacity on the way, truncated to a whole byte once; a brush's `Color` and `Opacity` are read as an attribute, a whole `{StaticResource}` or `{DynamicResource}`, or a property element holding a literal or a `<StaticResource>`, an `Opacity` naming an `x:Double`; and a brush whose `Color` names a brush, directly or through an alias, or whose `Color` or `Opacity` the audit cannot read, has no colour. `AuditColor.CompositeOver` is the exact blend, which Skia's CPU raster, blending in integers of its own alike on every CPU, falls short of by up to two levels or exceeds by one; the pixel test predicts each pixel with the raster's arithmetic applied to the audit's colour, and holds it to the level | A translucent token scores a level off what is drawn; a brush whose opacity is a resource, a binding or a word scores as opaque; a brush Avalonia never loads is given a colour; or a pixel test that compares a pixel with the exact blend fails where the raster falls short of it | `VariantInventory.Resolve`; `ThemeDefinitionScanner` (its private `BrushValue` and `WrittenProperty`); `ResourceValue.ColorLiteral.OpacityKey`, `ResourceValue.Alias.FromColor`, `ResourceReferenceScanner.TryReadWholeReference`; `DrawnColourTests` (its private `Rastered`); tests `ThemeInventoryTests.An_opacity_scales_the_alpha_truncated_once_as_Avalonia_draws_it`, `ThemeInventoryTests.An_opacity_a_brush_takes_from_a_resource_is_the_number_that_resource_holds`, `ThemeInventoryTests.A_brush_whose_Color_names_a_brush_has_no_colour_since_it_does_not_load`, `ThemeDefinitionScannerTests.Reads_a_brushs_Color_and_Opacity_written_as_a_resource_or_as_a_property_element`, `ThemeDefinitionScannerTests.A_brush_whose_Color_or_Opacity_it_cannot_read_is_not_colour_scored`, `DrawnColourTests` |
| The parts report's format is a contract: `TemplatePartReading.ToMarkdown` writes LF, invariant culture, no timestamp and no absolute path; a code span's fence is longer than any run of backticks in it, and a line break in a value reads as a space; a control's rows follow the order its themes declare its parts, then the parts no theme declares, ordinally; and a part the code declares or looks up that a theme lacks reads **missing** in that theme's column, exactly `BNXQ1003`'s findings. A change to the format is recorded under "Effect on a consumer" | Every consumer's committed report fails its drift check on the next upgrade, unannounced; one generated on Windows differs from one generated on Linux; or a generic type's name, or an id holding a line break, breaks a heading or a table row | `TemplatePartReport` remarks; tests `TemplatePartReportTests.TheReport_IsTheGoldenText_ByteForByte`, `TemplatePartReportTests.TheReport_HasNoCarriageReturn`, `TemplatePartReportTests.OneTheme_TakesPlainColumns_AndNothingSkipped_IsSaid`, `TemplatePartReportTests.ACodeSpan_IsFencedPastAnyBacktickInIt_AndKeepsItsRowOnOneLine` |
| The drift check is one call, `OutputDrift.Check`: CRLF reads as LF; a stale output's fresh text is written beside it as `name.received.ext`, which a check that finds the output up to date removes, and so does `OutputDrift.Write`, through which the tool writes every output; the audit's scans never read a received copy; `theme-audit`'s `report`, `compat`, `parts` and `brand` all run it, and exit 0 up to date, 1 stale, missing or unreadable, 2 on a configuration error | A Windows checkout reads as drift; a consumer's test compares differently from the tool; a CI step that branches on an exit code or a message breaks; or a received copy left where the audit scans changes the next report | `OutputDrift` remarks; `PartsRunner` remarks; `src/XamlQuality.ThemeAudit/Program.cs`; tests `OutputDriftTests`, `ThemeAuditToolTests`, `ReceivedCopyTests`, `PartsRunnerTests.AReceivedCopyAmongTheMarkup_IsNotRead` |
| A `null` in place of a value the audit needs, in `theme-audit.json` or in a contrast pairs file or compat mapping file it names, is an `AuditConfigException` naming the field, and the file for those two, when it loads, so `theme-audit` exits 2; where a field may be left out with nothing in its place, a `null` leaves it out, and a key a provider supplies may still be `null` | A null escapes the tool as an exception it does not expect, and it exits 1, which a consumer's CI reads as drift, or is read silently, as a variant mapped to `null` was; or a null that means "left out" is refused | `AuditConfig` remarks and its private `Validate`; `ContrastPairsFile.Load`; `CompatMapping.Load`; tests `AuditConfigNullTests`, `PairsAndMappingNullTests`, `ThemeAuditToolTests.A_configuration_that_sets_a_section_to_null_exits_2_and_names_it`, `ThemeAuditToolTests.A_pairs_or_mapping_file_that_sets_a_value_to_null_exits_2_and_names_the_file` |
| The brand output's alpha is Avalonia's: a colour in a stack takes its brush's opacity, a token's own or a literal's first `@opacity`, times the product of the elements' opacities after it, truncated to a whole byte once, through `VariantInventory.WithOpacity`, as the audit's colours are; layers composite exactly, as `AuditColor.CompositeOver` does | A figure a brand document states is a level off what Avalonia draws, as the port's generator, which truncated after each opacity, was | `BrandRunner` remarks and its private `Layer` and `Product`; `VariantInventory.Resolve(string, double)`; tests `BrandRunnerTests.An_alpha_is_every_opacity_on_the_way_truncated_once`, `DrawnColourTests.A_brush_inside_an_element_at_an_opacity_is_drawn_at_the_alpha_the_brand_output_makes` |
| A brand document is filled whole or not at all: every claim that no longer holds and every guard that fails is listed with the template's line, a file of the theme that cannot be read among them, `BrandResult.Markdown` is then empty, and `theme-audit brand` writes no document and no received copy and exits 1 | A document whose claim failed is committed, one whose palette lost a file it could not read is committed as complete, a run stops at its first problem, or a check leaves a received copy holding a document that was never true | `BrandResult`, `BrandRunner` remarks; `src/XamlQuality.ThemeAudit/Program.cs`, the `brand` action; tests `BrandRunnerTests.Every_problem_is_listed_together_with_its_line_and_no_document_is_given`, `BrandRunnerTests.A_theme_file_that_cannot_be_read_is_a_problem_and_an_include_of_another_assembly_is_not`, `ThemeAuditToolTests.Brand_whose_claims_fail_lists_every_problem_exits_1_and_writes_nothing`, `ThemeAuditToolTests.Brand_with_one_document_that_cannot_be_filled_writes_none_of_them` |
| A placeholder the language does not define is a block's: a block replaces the placeholder whose inside is its name, verbatim, and is never read as the language; a placeholder nothing fills, a block nothing names, and a block named as one of the language's own are problems; the tool passes no block | A consumer's rendering is read as placeholders, a misspelt placeholder is left in a committed document, or a block stands in for the language unannounced | `BrandRunner` remarks; tests `BrandRunnerTests.A_block_fills_the_placeholder_it_names_verbatim`, `BrandRunnerTests.A_block_nothing_names_and_one_named_as_the_languages_own_are_problems` |
| The brand document's format is a contract: LF, invariant culture, no timestamp, no absolute path, a first line naming the template by its path from the configuration's folder, and a chart framed in the colours of the document's variant, its rows linked in order; a change to it is recorded under "Effect on a consumer" | Every consumer's committed document fails its drift check on the next upgrade; one generated on Windows differs from one generated on Linux; or a light variant's chart is drawn in a dark frame, whose titles vanish on its ground, or its rows stack in whatever order the layout picks | `BrandRunner` remarks and its private `Swatches`; tests `BrandRunnerTests.The_document_starts_with_a_line_naming_its_template_and_holds_no_carriage_return`, `BrandRunnerTests.Swatches_draw_each_token_over_the_frames_ground_labelled_with_its_key_and_colour`, `BrandRunnerTests.A_palette_with_light_and_dark_variants_is_filled_in_each_and_its_chart_framed_in_that_variants_colours`, `BrandRunnerTests.Swatches_put_four_to_a_row_file_by_file_in_the_order_the_theme_reads_them` |
| The theme mappings reach a consumer's output as `Mappings/<name>.json` | `"mapping": "FluentToSemi"` is a file-not-found for every `PackageReference` consumer | The `None` item in `src/XamlQuality/XamlQuality.csproj`, where `Link`, `PackageCopyToOutput` and `PackagePath` are all load-bearing |
| Every project under `src` that packs is named in `packages.push` or `packages.local`, never both; the release's preflight list, push and GitHub Release read their ids from `packages.push` and spell none out; what was packed is checked against both lists before the push; and no step globs `*.nupkg` | A future packable project is published permanently, or attached to a GitHub Release nobody chose it for, or a second list of ids in the workflow drifts from the first | `packages.push`; `packages.local`; `scripts/assert-packages.cs`, run by `.github/workflows/release.yml` and CI's `pack` job; tests `PackagingTests.Every_packable_project_is_classified`, `PackagingTests.No_id_is_both_published_and_private`, `PackagingTests.The_release_workflow_globs_nothing_and_publishes_what_is_declared`, `PackagingTests.The_release_workflow_checks_what_was_packed_before_it_pushes`, `PackagingTests.The_release_workflow_never_names_a_private_package`, `PackagingTests.The_release_workflow_still_has_both_publishing_steps` |
| `NUGET_USER` is a repository variable holding the nuget.org profile name, and a release refuses to run without it | A masked value hides why a login fails, and a tag with it unset creates a GitHub Release for a package that never shipped | `.github/workflows/release.yml`, step `Refuse to release without NUGET_USER`; `docs/publishing.md` |
| The marketplace lists the `drivable-ui` plugin by a relative path that holds its manifest and skill, the guide gives the commands that install it, and neither manifest pins a `version` | Every `claude plugin install drivable-ui@xamlquality` fails or installs a plugin with no skill, the guide's commands name a plugin nobody serves, or every install stays on a stale copy of the skill | `.claude-plugin/marketplace.json`; `docs/ai-drivable-ui/plugin/`; tests `PluginMarketplaceTests.The_marketplace_lists_the_plugin_by_a_path_that_holds_its_manifest`, `PluginMarketplaceTests.The_skill_is_where_the_plugin_loads_skills_and_is_named_for_its_folder`, `PluginMarketplaceTests.No_version_is_pinned_so_an_update_picks_up_every_change`, `PluginMarketplaceTests.The_skill_sends_its_reader_to_the_guide_that_exists`, `PluginMarketplaceTests.The_guide_gives_the_commands_the_marketplace_answers_to` |
| Nothing under `tools/` ships: it is never packed, no project of it is in `Inspections.slnx`, and nothing in `src` or `tests` references it | A measuring tool lands in the folder beside the two packages a release names, or the library gains a dependency through a tool | `tools/Directory.Build.props` (`IsPackable`); `Inspections.slnx` |
| `tools/real-code/expected/` is what `main`'s rules print over the inputs `tools/real-code/inputs.json` pins, and a pull request that changes it says why | A change to what a rule says about real code merges unseen, or a record `main` no longer prints fails every pull request after it | `tools/real-code.cs`; CI job `Verification tools`, a required check in `.github/repository.json` |
| A case of the headless probe keeps its id and what it measures; a new question is a new case | A review's verdict, which cites a case by its id, silently comes to describe another measurement | `tools/probe/Probe`, whose `Cases.Every` lists every case; `tools/AGENTS.md`'s rules |

#### 2. Commands

```bash
dotnet build Inspections.slnx -c Release
dotnet test --solution Inspections.slnx
XQ_UPDATE_DOCS=1 dotnet test --solution Inspections.slnx
dotnet pack Inspections.slnx -c Release -p:Version=0.0.0-local --output ./packages/local
dotnet run --file scripts/assert-packages.cs -- ./packages/local
dotnet run --file tools/real-code.cs
dotnet run --file tools/real-code.cs -- --update
dotnet run --file tools/mutate.cs -- <TestClass> <catalog.json>
dotnet run --file tools/probe.cs -- <pattern>
dotnet run --file scripts/fetch-reference.cs
dotnet run --file scripts/fetch-reference.cs -- --status
```

- The third regenerates the rules table in `docs/xamlquality.md` (`RulesCatalogTests`). Commit the result; the build
  never writes a tracked file.
- The fifth checks that what the fourth packed is exactly what `packages.push` and `packages.local`
  declare, reading each id from its `.nuspec`, as the release does before it pushes.
- The sixth compares what every rule prints over the pinned real code with `main`'s record, and the
  seventh rewrites the record from this tree. Read every line that moved before committing it.
- The eighth plants each defect of a catalog in a copy of the tree and runs one test class, from the
  test project that holds it; it refuses a run estimated past 30 minutes.
- The ninth measures the headless probe's cases whose id matches the pattern, a trailing `*` taking
  a prefix, and judges what `BNXQ1005`, `BNXQ1009` and `BNXQ1010` say about each against the
  measurements; it exits 1 when a measurement refutes a verdict.
- The tenth fetches the real themes' sources into `reference/`, at the versions
  `Directory.Packages.props` pins, for work by hand, and the eleventh reports whether each is there at
  its pin. No test, build or CI job reads them.
- A local build of the solution wants `AVALONIA_TELEMETRY_OPTOUT=1`: `tests/XamlQuality.ThemeRuntime.Tests`
  takes Avalonia, whose build telemetry otherwise starts a process that outlives the build. Every job
  of `ci.yml` and `release.yml` sets it.
- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` takes `--solution` and
  rejects VSTest-only switches such as `--nologo`.
- Write `-p:` rather than `/p:`. The workflows use `/p:` because they run on Linux; Git Bash on
  Windows rewrites a leading-slash argument into a path.
- Warnings are errors (`Directory.Build.props`), and CI builds and tests on Linux, Windows and macOS
  because the rules compare paths (`.github/workflows/ci.yml`).

#### 3. Checklists

##### Budgeting a change, before it starts

- **Set the review budget first.** A new rule, or a change to one, is ready when the suite passes, the
  mutations planted in the code it touches each fail a test (`tools/mutate.cs`), the real-code comparison holds
  (`tools/real-code.cs`, over ClaudeForge, DiffView and ScopedEditors), its record rewritten in the
  same pull request wherever the change means to alter it, what it claims the framework does is
  measured headless under Fluent, Simple and Semi (`tools/probe.cs`), and at most two independent
  reviews have run. After that, only a false claim on real code holds the merge back; anything else
  a review finds goes into `PROGRESS.md`'s backlog with its measurement. A rule is opt-in and a wrong
  finding is fixed by the next release, while a review with no end holds back everything queued
  behind it.
- **One concern per pull request.** A change to shared code that alters another rule's output ships in
  a pull request of its own, not inside a new rule's, so that each is reviewed on its own ground.
- **Two lanes.** While a rule is in review, replies to other repositories, documentation entries and
  patches for other repositories go out as small pull requests from `main`, and none of them waits for
  the rule. `PROGRESS.md`'s requests table lists every request from another repository with the date it
  arrived.
- **A document states the contract; the tests hold the cases.** A remark, a README paragraph or a row
  here says what a rule requires, what it skips and why; the edge cases live in tests, which run. Each
  sentence a document adds is a claim a review can refute and a fix must rewrite. Keep a pull request's
  description to about a page.

##### Adding a rule

1. A public sealed class in `src/XamlQuality/Rules/` implementing `IXamlRule`, with a parameterless
   constructor. Take the next free `BNXQ` id. Ids are permanent and assigned only when a rule is
   written: candidates in `PROGRESS.md`'s backlog have none.
2. `Summary` states what the rule requires, not the violation (`IXamlRule.Summary`).
3. Count what was examined in `Inspected`. Name in `Skipped` what the rule saw but could not check.
4. Tests in `tests/XamlQuality.Tests/Rules/`: the violation, the clean case, both spellings wherever
   a property is involved, the cases where the rule must stay silent, and one asserting `Inspected`
   is zero.
5. Prove each test can fail. Remove the behaviour it covers, watch exactly that test fail, then
   restore it by the inverse edit. `tools/mutate.cs` does this for a catalog of such defects, each in
   a copy of the tree, which it never edits.
6. Regenerate the README table. Prose about how the rule overlaps another goes outside the generated
   markers.
7. Run it on real code before it lands: a consumer's markup and, for a rule that reads assemblies,
   its build output. Fixtures written from the report that motivated a rule reproduce the report,
   not the codebase. Where it claims what the framework does, measure the claim headless under
   Fluent, Simple and Semi, as the cases in `tools/probe/Probe` measure the claims of `BNXQ1005`,
   `BNXQ1009` and `BNXQ1010`.
8. If the rule began as an entry in `docs/avalonia-gotchas.md`, leave a line in that entry naming the
   id.
9. Add it to `PROGRESS.md`'s unreleased list, with its effect on a consumer.

##### Changing a rule or the audit

- A rule that becomes stricter turns a consumer's green markup red on their next upgrade. Record it
  in `PROGRESS.md`'s unreleased list, under "Effect on a consumer".
- A change to how the digest is computed rebases every committed report once, and each consumer must
  regenerate theirs. Record that too.
- Keep `src/XamlQuality/Properties/AssemblyInfo.cs` accurate when a type's visibility changes.

##### Editing the documents

- `docs/ai-drivable-ui.md` is the one living copy of the guide. The port it began in retired its own
  and points here, so it is changed here and nowhere else; retitling or renumbering a section someone
  cites means telling them.
- `docs/avalonia-gotchas.md` is cited by path from other repositories, listed in `PROGRESS.md`.
  Moving it, or retitling an entry they cite, means updating them.
- An entry that becomes mechanically checkable is promoted to a rule, as that document's header
  requires.

##### Releasing

- A tag publishes permanently. Tag only when the developer has decided to release.
- Follow `docs/publishing.md`: tag `xamlquality/vYYYY.Q.MMDD`, one release per calendar day, verify against the
  nuget.org flat-container API rather than the workflow, and never re-tag to fix a failed run.
- Afterwards, move the released rows out of `PROGRESS.md`'s unreleased list and update its
  published version.

## AssemblyQuality

### What this family is

`Bennewitz.Ninja.AssemblyQuality`: static analysis rules for the shape a .NET assembly ships, read
by reflection over the compiled output rather than from source. The assembly-shaped companion to
the XamlQuality family, which reads markup. [docs/assemblyquality.md](docs/assemblyquality.md)
describes each rule for a consumer; this section covers how to change them.

### Layout

| Path | What it holds |
|---|---|
| `src/AssemblyQuality/` | The library, file by file in [src/AGENTS.md](src/AGENTS.md) |
| `tests/AssemblyQuality.Tests/` | xunit v3 tests; `Fixtures/` holds real types that break the rules |
| `tests/fixtures/` | Fixture assemblies the tests reflect over. They have their own `Directory.Build.props`, so they are plain libraries, not test projects. Each opts out of the friend grants with `SolutionFriendGrants=false`, since its metadata is what a test reads |

### The contract every rule keeps

- **A rule reports and never asserts.** It returns an `AssemblyRuleResult`, and the consumer
  decides how severe a finding is.
- **`Inspected` counts only candidates that could have produced a finding.** A rule whose check
  can't be satisfied (it forbids nothing, or nothing it could match is in reach) reports `0`,
  never the number of things it walked past.
- **`Skipped` names what the rule could not examine**, such as a reference that would not load or a
  type whose signature names a missing assembly. Record the item and move on; one missing
  dependency must never crash the whole scan. Use `AssemblyScanContext.IsLoadFailure` and
  `Unreadable`.
- **Load references through `AssemblyScanContext.ReferencedNamespaces`**, which also reads forwarded
  types. A facade like `System.Runtime` exports nothing and forwards everything.
- **Every reflection entry point carries
  `[RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]`.** `IsAotCompatible` turns on the
  trim analyzers, so a missing attribute fails the build. The library is not trim-safe, and the
  attribute is what says so to consumers.
- **Every rule has a public parameterless constructor.** `RulesCatalogTests` creates each rule that
  way. Put configuration in extra constructors or static factories, such as
  `SurfaceLeakRule.Only(...)` and `NamespaceShadowRule.IncludingInternalTypes()`.
- **A rule's `Id` is public API.** Changing it silently removes every suppression a consumer wrote
  against it.

### Tests

- **Test both directions, always.** Each rule is shown finding a real violation AND clearing a clean
  subject. A test that only asserts "no findings" passes just as well on a rule that has stopped
  matching anything.
- **Fixtures are real compiled types, never mocks**, because the rules read metadata. When a test
  needs a runtime condition, such as a dependency that won't load, build a real assembly under
  `tests/fixtures/` that has it, and assert the condition itself before relying on it (see
  `CoverageTests.TheOrphansReference_ReallyWillNotLoad`).
- **Watch a new test fail without the fix before trusting it.** Put the old code back, confirm the
  new test fails, then restore.
- **The rules table in `docs/assemblyquality.md` is generated.** `RulesCatalogTests` fails if it drifts from the rule
  types. To regenerate it, set `AQ_UPDATE_DOCS=1` and run the tests.

### Code style

- XML doc remarks follow a house style: one `<para>` per point, each opening with a marker and a
  bold claim: ⭐ key idea, ⚠ caution, ⛔ trap. Explain *why*, and say "Measured:" when a claim
  comes from an observed failure.
- The root namespace is `Bennewitz.Ninja.<ProjectName>`. Assembly names stay unprefixed
  (`AssemblyQuality`); the package id carries the prefix.
- Finding messages state the defect, why it bites, and every valid fix. Never prescribe one fix when
  a common convention needs a different one (see BNAQ1004's two renames).

## CodeQuality

### What this family is

`Bennewitz.Ninja.CodeQuality`: Roslyn analyzers for the shape of a .NET library's source, each the
compile-time counterpart of a Bennewitz.Ninja.AssemblyQuality rule that reads the shipped assembly,
with the same number. It ships one package, `Bennewitz.Ninja.CodeQuality`, consumed as a
development dependency by any C# project whose compiler is Roslyn `RoslynVersion` (in
`Directory.Packages.props`) or later. [docs/codequality.md](docs/codequality.md) describes each rule
for a consumer; this section covers how to change them.

### Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| Every analyzer derives from `ScopedAnalyzer<TScope>`, whose gate cannot register actions | The compiler loads the assembly whole, so one analyzer that registers unconditionally runs in every compilation that references the package | `AnalyzerConventionTests` |
| The analyzer targets `netstandard2.0` and its compiled assembly references only what the compiler host loads beside it | A `net10.0` analyzer fails to load in Visual Studio's MSBuild; a package dependency is not packed beside the DLL and fails as `AD0001` | `AnalyzerShapeTests`; `repo-conventions check`, analyzer role |
| The DLL is packed under `analyzers/dotnet/cs` with `IncludeBuildOutput` off and no dependencies, as a `DevelopmentDependency` | Under `lib/` a consumer gets a runtime reference and the compiler never loads the analyzer | `AnalyzerShapeTests.Every_analyzer_packs_only_itself_under_analyzers_dotnet_cs`; the consumer builds under "Measured before the first release" in `PROGRESS.md` |
| `Microsoft.CodeAnalysis.CSharp` is pinned to the oldest compiler the package supports, `RoslynVersion` in `Directory.Packages.props`, and the tests resolve the same version | An API newer than the pin loads in no older compiler; a test on a newer Roslyn proves nothing about the pin | `Directory.Packages.props`; `CodeQuality.Tests.csproj`, the Workspaces reference |
| Every rule ID is `BNCQ` plus four digits, its category is `Bennewitz.Ninja.CodeQuality`, its help link is its section of `docs/codequality.md`, and that file's rules table is generated from the descriptors | A consumer's suppressions name the ID; a help link to a missing section is a 404 on the rule they were just told about | `RulesCatalogTests` |
| Every rule is recorded in `AnalyzerReleases.Unshipped.md`, and moved to `AnalyzerReleases.Shipped.md` under its release in the same change as the tag | RS2000 and RS2008 fail the build; a release that leaves the shipped file empty has no record of what it shipped | `Microsoft.CodeAnalysis.Analyzers`, release tracking; the releasing checklist |
| Every rule is tested both ways: firing on a real violation, silent on clean code, and inert outside its scope, which is measured as registered actions and not as silence | A rule whose gate is wrong fails as silence, and a clean build cannot tell inert from checked | The rule test classes under `tests/CodeQuality.Tests/Rules/` |
| The analyzer assembly exports no public type of its own | A public type is API to keep compatible for no reader; a consumer configures the package through `.editorconfig` | `AnalyzerConventionTests` |

### Commands

- The rule tests resolve the .NET reference assemblies from nuget.org on first run and cache them.
- `BNCQ_UPDATE_DOCS=1 dotnet test --solution Inspections.slnx` regenerates the rules table in `docs/codequality.md`.

### Checklists

**Adding a rule**
1. Its number is its `BNAQ` counterpart's, or the next free number for a rule with no counterpart.
   `BNCQ1003` stays reserved. The code is written here: analyzers the maintainer wrote for another
   project may be read for ideas and traps, but none of their code, names, IDs or links is copied.
2. `src/CodeQuality/Rules/<Name>Analyzer.cs`, internal and sealed, deriving from
   `ScopedAnalyzer<TScope>`. The gate returns null wherever the rule has nothing to compare against.
   The descriptor takes its category and help link from `Catalog`.
3. Its line in `src/CodeQuality/AnalyzerReleases.Unshipped.md`.
4. `tests/CodeQuality.Tests/Rules/<Name>Tests.cs`: firing on a violation, silent on clean code,
   inert outside its scope. Inert is a count of registered actions through `Plain`, zero beside a
   control in scope that is not. Make the gate return null and watch the firing tests fail; force
   it open and watch the inert test fail; then put the gate back. A test that never failed proves
   nothing.
5. Its `### BNCQ####` section in `docs/codequality.md`, linking the counterpart; regenerate the table.
6. Its entry in `PROGRESS.md`, and a line in the counterpart's section of `docs/assemblyquality.md`
   linking back.

**Raising the Roslyn pin:** only for an API the rules cannot do without. Measure the lowest version
that builds and passes the tests, set `RoslynVersion`, and change the compiler named in
the install section of `docs/codequality.md` in the same change.

**Releasing:** in the same change as the tag, move the rules from
`AnalyzerReleases.Unshipped.md` into `AnalyzerReleases.Shipped.md` under `## Release <version>`.
