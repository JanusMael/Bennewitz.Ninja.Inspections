# Progress

The work state of `Bennewitz.Ninja.Inspections`: where the repository stands, then each family's
own state: what is published, what is on `main` and not yet released, who depends on what, and what
is open. What has stopped changing moves out rather than piling up.

## Resume

This repository is Bennewitz.Ninja.XamlQuality renamed, with AssemblyQuality and CodeQuality moved
in, by Bennewitz.Ninja.Templates' `plans/00009`, approved 2026-10-22. Step 5 of that plan, the
merge, is done: the repository was renamed on 2026-10-26, `repo-conventions.cs apply` ran from the
merge's branch, and PR #98 merged as `a18a303` with every check passing, by a merge commit, allowed
for that one pull request by the maintainer's decision of that day, since a squash or a rebase would
have dropped the imported histories off `main`. Merge commits were disallowed again after it, and
`check --admin` conforms. What the merge changed is under *Merged into Inspections* below.

Step 6, the policy, is done too, on 2026-10-29. By decision 20, XamlQuality's existing policy was
edited, never a second one added: it is named `Bennewitz.Ninja.Inspections — release.yml`, names this
repository, and has the patterns `Bennewitz.Ninja.XamlQuality*`, `Bennewitz.Ninja.AssemblyQuality*`
and `Bennewitz.Ninja.CodeQuality*`, workflow `release.yml`, no environment. `NUGET_USER` carried over
the rename. The first blank-version preflight failed with a 401: the policy still named
`Bennewitz.Ninja.XamlQuality` beside the repository's unchanged id, so nuget.org matches on the name,
which answers the question decision 20 left open. Edited to the new name, the preflight logged in
(run `40933076378`), and the policy as the maintainer showed it names Inspections with a pattern per
family.
AssemblyQuality's own policy went after its first release from here. CodeQuality's was deleted by the
maintainer on 2026-10-30 without one: it named the archived CodeQuality repository, which can run no
workflow, so it could never publish again, and decision 21 leaves CodeQuality nothing to release yet.

Step 7 is done too, on 2026-10-29, by the maintainer's decisions of that day: AssemblyQuality and
CodeQuality are archived, each with a final README commit pushed to its `main` that names this
repository above the README as it was, kept whole because released versions link to its rule
sections. CodeQuality#3, its one open issue, asked for conventions this repository already carries,
and was closed as done rather than transferred. Neither has an open issue or pull request.

ScopedEditors moved into `Bennewitz.Ninja.AppKit` on 2026-11-06. `docs/avalonia-gotchas.md` links
there now. `tools/real-code/inputs.json` keeps its old repository URL on purpose: the commit it pins
was rewritten by the import, so it exists only in the archived repository, which can still be cloned.
DiffView stays in Bennewitz.Ninja.DiffView, which is not archived, so its input is unchanged.

What remains of the plan for this repository:

| Step | What | Waits on |
|---|---|---|
| 9 | Each family's first release from here, when it next changes (decision 21: moving is not a reason to release). Each id read back from nuget.org's flat container with `RepositoryUrl` naming Inspections; the release's assets that family's ids alone. After AssemblyQuality's and CodeQuality's, the policy of the repository each left is deleted. **AssemblyQuality's is done**, `assemblyquality/v2026.4.1029`, below; its old policy was deleted by the maintainer the same day. **CodeQuality's is done**, `codequality/v2026.4.1104`, in its Status below; its old policy had gone already, deleted by the maintainer on 2026-10-30 ahead of that release, as the Resume above says. A family's notes are written by `release-target.cs notes` since the sync below, so they no longer need replacing by hand. **XamlQuality's is done**, `xamlquality/v2026.4.1109`, in its Status below; XamlQuality's policy was this repository's from the start, so none is left to delete | Nothing: every family has released from here |

Each family releases alone, from a tag `<family>/vYYYY.Q.MMDD`; [docs/publishing.md](docs/publishing.md)
is the runbook. Whether a family has anything to release is in its section below.

**Synced to Bennewitz.Ninja.Templates `2026.4.1114`** on 2026-11-16, on the maintainer's word.

`Bennewitz.Ninja.CodeQuality` `2026.4.1113`, the published package, is referenced privately, every
rule at its default, the family's decision of 2026-11-10. It was `2026.4.1111` until this sync.
`2026.4.1113` is this repository's own release of 2026-11-13, in CodeQuality's Status below:
`BNCQ1009` and `BNCQ3001` follow a delegate held in a local to its body. `docs/publishing.md` names
the version floor `release-target.cs` has enforced since the last sync, after the quarter rule and
in two rows of the failure table, in the template's words. `repo-conventions.cs` and
`release-target.cs` did not change in this release, and both are already its, byte for byte.

- **It stays out of the two projects that hold its source**, `src/CodeQuality` and
  `tests/CodeQuality.Tests`. One restore never holds a project and a package of the same id, as for
  AssemblyQuality. So the published release gates this build, not the head being changed.
- **It reports nothing new.** Neither `BNCQ1009` nor `BNCQ3001` reports a delegate held in a local here.
  The six findings `2026.4.1104` reported are each in a fixture that holds that defect on purpose,
  and each is suppressed where it stands with that reason:
  - `BNCQ1004` in `tests/fixtures/Orphan`'s `FacadeShadow.cs` and `Orphaned.cs`. `Orphaned.cs`'s own
    comment already said BNCQ1004 would report it.
  - `BNCQ1004` in `tests/AssemblyQuality.Tests/Fixtures`' `Shadowed.cs` and `HiddenShadow.cs`.
  - `BNCQ1002` on `Subjects.cs`' `Offender.Leaked` and `LeakedNested`.
- **Verified on Windows:**
  - the build passed with warnings as errors, 0 warnings, and every project but those two restored
    CodeQuality `2026.4.1113`;
  - 1074 of 1074 tests passed, on `main` at `96ae274`;
  - pack and `assert-packages` passed, 5 packed and all declared, and no packed nuspec gains a
    dependency;
  - `check` conforms;
  - `tools/real-code.cs`: every input's output is its record; the mutation runner's smoke: 2 of 2
    mutations failed exactly as expected;
  - both probe builds passed.

No family's package changes.

**Synced to Bennewitz.Ninja.Templates `2026.4.1113`** on 2026-11-13. `repo-conventions.cs` and
`release-target.cs` became that release's: `resolve` refuses a version below the newest nuget.org
lists for one of the family's ids, unless nuget.org already lists it, which `resolve` against
nuget.org bore out for all three families; `check` asks for a CodeQuality pin beside
AssemblyQuality, skipping an id the repository publishes. CodeQuality moved from `2026.4.1104` to
`2026.4.1111` and reported nothing new. Build with warnings as errors, 1074 of 1074 tests at
`a2688f7`, pack, `assert-packages`, `check`, which conforms, `tools/real-code.cs`, the mutation
runner's smoke and both probe builds passed on Windows. No family's package changes.

**Synced to Bennewitz.Ninja.Templates `2026.4.1111`** on 2026-11-11. `repo-conventions.cs` began
failing a repository whose `packages.push` names an id whose release does not resolve through
`release-target.cs`, and one that does not pin AssemblyQuality. CodeQuality `2026.4.1104` was first
referenced privately, and its six findings, above, were suppressed. Build with warnings as errors,
1003 of 1003 tests at `4012b08`, pack, `assert-packages`, `check`, which conforms,
`tools/real-code.cs`, the mutation runner's smoke and both probe builds passed on Windows. No
family's package changes.

**Synced to Bennewitz.Ninja.Templates `2026.4.1109`** on 2026-11-09. The scripts were already that
release's; it moves AutoVersioning `2026.3.916` to `2026.4.1031`, whose diagnostics are `BNAV00` to
`BNAV04` from `2026.3.928` on. xunit.v3 stays `3.2.2`, as below, and Avalonia `12.1.3`. Build with
warnings as errors, 987 of 987 tests, pack, `assert-packages`, `check`, which conforms, and
`tools/real-code.cs` pass on Windows. No family's package changes.

**Synced to Bennewitz.Ninja.Templates `2026.4.1103`** on 2026-11-03. `repo-conventions.cs` is that
release's, which also checks that markdown is checked out as LF; the root `.gitattributes` already
does. `release-target.cs` is that release's too: it evaluates a level's projects together, and
`projects --family` names the same projects for all three families as the `2026.4.1031` copy did, in
4 to 7 s where that took 14 to 17. `Microsoft.VisualStudio.Threading.Analyzers` `18.12.1` is
referenced privately, with only `VSTHRD101` and `VSTHRD110` on: the family's decision of 2026-11-03,
taken on the DiffView measurement below. Neither reports anything here, and no packed nuspec gains a
dependency. Build, 978 of 978 tests, pack, `assert-packages`, `check`, which conforms, and
`tools/real-code.cs` pass on Windows. No family's package changes.

The syncs before it, to `2026.4.1031` and to `2026.4.1029` (Templates' `plans/00011` step 5), brought
the scripts as they stand: a family's GitHub release notes are its own commits since its previous
tag, from `release-target.cs notes`, and it is marked "Latest" unless a tag of any family is newer.
`"bareTags": "xamlquality"` makes the bare `v…` tags XamlQuality's earlier releases, so its first
family release starts from `v2026.4.1026`. `check` fails a family without its README, a relative
link in one, and a package whose `FamilyReadme` or `AssemblyProduct` is not its family's;
`assert-packages --family` compares each packed README with `docs/<family>.md`. The root props'
README items are the template's, by the maintainer's decision: a packable project without
`FamilyReadme` would pack the root README rather than fail with `NU5039`, and `check` fails it first.

## Merged into Inspections (2026-10-26)

The histories of AssemblyQuality, from its first commit, `bd44084` before the rewrite, through
JanusMael/Bennewitz.Ninja.AssemblyQuality#17, and of CodeQuality were imported whole with `git filter-repo`. Their tags were renamed `assemblyquality/v…` and
`codequality/v…`; XamlQuality's own `v…` tags were left as they are, by decision 15. AssemblyQuality's
approved plans moved unchanged to `plans/assemblyquality/`. The build files, workflows and scripts are
Templates' canonical ones from `2026.4.1023`, the release that ships multi-family support.

### What changes for a consumer, in each family's first release from here

| Family | Change |
|---|---|
| Every family | `RepositoryUrl` and `PackageProjectUrl` name `Bennewitz.Ninja.Inspections`. The package's README is the family's `docs/<family>.md`; its links to the repository are absolute now, and the maintainer's sections, releasing, versioning and publishing, moved to the root README and `docs/publishing.md` |
| XamlQuality | The library's friend grants are the generated, solution-wide ones, in place of its hand-written grants to its own tests. `Bennewitz.Ninja.XamlQuality.ThemeAudit` now carries `IsTrimmable`, which every `src` project gets from `src/Directory.Build.props`, AssemblyQuality's; the library sets it false, as below. The drivable-ui marketplace is added as `JanusMael/Bennewitz.Ninja.Inspections`, its name, `xamlquality`, unchanged |
| AssemblyQuality | Released in `2026.4.1029`. The solution-wide friend grants of JanusMael/Bennewitz.Ninja.AssemblyQuality#17, unreleased when it moved. Its README links its CodeQuality counterparts in `docs/codequality.md` here |
| CodeQuality | Each rule's help link names its section in `docs/codequality.md` here; `2026.3.928`'s links name the archived repository's README, which keeps those sections. The hand-written grant to its tests gives way to the generated ones. Its README links its AssemblyQuality counterparts in `docs/assemblyquality.md` here |

### Where the merge departs from plans/00009, or decides what it left open

- **The root `AssemblyProduct` is `Inspections`**, and each package sets its family's, as it was:
  AutoVersioning fails the build without a product (`BAUTOVERSIONING03`).
- **Each family's README is kept whole, at `docs/<family>.md`**, and packed as its packages'
  `README.md` through a `FamilyReadme` property in each package's project. The root README condenses
  each into a section and links it, as the plan's merging table asks; the rules catalogs, which
  tests read and help links target, stay whole in the family files.
- **CI is XamlQuality's workflow, its jobs renamed** `build` (the three-OS matrix), `pack`,
  `conventions` and `Verification tools`, and `requiredChecks` follows.
- **`XamlQuality.csproj` sets `IsTrimmable` and `EnableTrimAnalyzer` false**, with a `Trimming`
  exemption in `.github/repository.json`, by decision 19: under AssemblyQuality's
  `src/Directory.Build.props` the trim analyser reports 106 errors in the library (IL2026, IL2059,
  IL2065, IL2070, IL2075). Templates' `plans/00008` side 2, JanusMael/Bennewitz.Ninja.Templates#58,
  does not lift it, as issue #106 found: it holds what a repository takes, while side 1 still holds
  this repository's own libraries to trimming. The exemption goes when the library's trim warnings
  are fixed and it sets both properties, or when it stops being a library here. The developer kept it
  on 2026-11-13, the library reading assemblies by reflection by design.
- **`System.Security.Cryptography.Pkcs` is pinned at `10.0.14`** in `Directory.Packages.props`.
  `Microsoft.CodeAnalysis.Analyzer.Testing` brings it in through `NuGet.Packaging` `7.0.3` at
  `9.0.6`, which GHSA-2fmv-rv2c-59x7 (moderate) and GHSA-846f-f764-76mx (high), published
  2026-10-13, make a restore error here. CodeQuality's own `main` has failed restore the same way
  since then, unseen because nothing was pushed there.
- **The hand-written `InternalsVisibleTo` grants are gone** from XamlQuality's
  `Properties/AssemblyInfo.cs` and CodeQuality's project; the generated
  `AssemblyInfo.InternalsVisibleTo.cs` covers both, and the conventions check fails on a hand-written
  grant beside it.
- **`PublicVersion` is set in `Directory.Build.targets`**, Templates' canonical file, not in the
  root props.
- **XamlQuality's earlier tags stay `v…`**, by decision 15, so its first `xamlquality/v…` release
  finds no earlier tag of the family, and its notes say it is the family's first release.
- **Each family's test that reads "every shipped assembly" reads its own family's `src` folders**,
  and the packaging tests assert the canonical release: the ids from `release-target.cs`, never named
  by hand.
- **The drivable-ui marketplace keeps its name, `xamlquality`**: renaming it would break every
  machine that added it. Only the repository its install command names changed.

## XamlQuality

Pull request and issue numbers in this section are this repository's own, from before it was renamed.

### Status

| | |
|---|---|
| Published | `2026.4.1111`, released 2026-11-11 on the maintainer's word: tag `xamlquality/v2026.4.1111` on `03d4767`, release run `43092577880`, which pushed `Bennewitz.Ninja.XamlQuality`, `.ThemeAudit` and `.ThemeRuntime` alone. It carries PR #130, the audit resolving an alias where Avalonia resolves it, by plan 00006; PR #131, the redefinitions a theme's own reads never see, by plan 00007; and PR #134, `BNXQ1011`. Verified from the feed: the flat container lists all three, each served nuspec names Inspections at `03d4767`, the GitHub release's assets are the three packages, and a project restoring from nuget.org alone runs `InputTooltipRule`, which reports a button with no tooltip and not one with a tooltip. Before it, `2026.4.1109`, released 2026-11-09, the family's first from here: tag `xamlquality/v2026.4.1109` on `827c54e`, carrying PR #123 and the move, under *Merged into Inspections* above |
| `main` | Nothing unreleased |
| Next release | On the developer's word, from an `xamlquality/vYYYY.Q.MMDD` tag; its notes start at the family's previous tag. One release per calendar day |
| Order of work | The developer's order of 2026-10-15 is done: the brand catalogue's generator, as a `theme-audit` output, by [plan 00004](plans/00004-brand-catalogue.md), in PR #84 and PR #85, released in `2026.4.1026`. Replies and entries for other repositories go out as small pull requests from `main`. The shared mutation runner is taken on by the developer's decision of 2026-11-06, as a plan of its own, after plans 00006 and 00007: plan 00006, the audit's aliases resolved where Avalonia resolves them, is PR #130, and plan 00007, the redefinitions a theme's own reads never see reported, is PR #131, both released in `2026.4.1111`; its request is below, and it is plan 00008, under *Testing* at the end |
| What a release publishes | The family's ids, the three packages above, as `families` in `.github/repository.json` names them: `scripts/release-target.cs` resolves them from the tag, and `scripts/assert-packages.cs --family xamlquality` checks the pack against them. `packages.push` lists every family's ids, and `packages.local` is empty. XamlQuality gained `packages.push` in PR #94, step 2 of Templates' `plans/00009`; step 5 brought AssemblyQuality and CodeQuality here, under *Merged into Inspections* above |
| Agent skill | `drivable-ui`, served from `main` as a Claude Code plugin since PR #47, by the developer's decision of 2026-09-29: `claude plugin marketplace add JanusMael/Bennewitz.Ninja.Inspections`, then `claude plugin install drivable-ui@xamlquality`. It is in neither package, so it ships on merge, not on a tag |
| Verification tooling | Under `tools/`, by [`plans/00001`](plans/00001-verification-tooling.md), complete: the real-code comparison, `tools/real-code.cs`, since PR #53, and the headless probe, `tools/probe.cs` over `tools/probe/`, since PR #56. Its mutation runner, PR #54's, gave way in PR #144 to the testing family's `mutate`, under *Testing* at the end. CI's required `Verification tools` job runs the real-code comparison and `mutate` over `tests/XamlQuality.Tests/Mutations/smoke.json`, and builds the probe. Where they depart from the plan is listed below |

#### Where the verification tooling departs from plan 00001

- `--from` reads a local clone with `git clone --shared` and a checkout, not with `git archive`, so
  that an input fetched and one taken from a clone are both git checkouts when they build.
- A probe case that names a compiled view reads the view's markup from beside the probe's build
  output, where `tools/probe/Probe/Probe.csproj` copies it, not from a folder beside the build. The
  plan allowed the probe to change only where warnings as errors stopped it compiling, and none did;
  the folder the cases read existed only in the scratch layout the probe came from.
- `BNXQ1010` over judge 12's 391 cases prints judge 16's fix record for 390 of them, where the plan
  asked for all. `W03b` reads as a skip, because the probe is now one assembly holding every
  review's types; the follow-up on `W03b` below says how.

#### Where `BNXQ1003` departs from plan 00002

- Decision 5 made two scanned types of one simple name one entry, as they were to the rule. Since
  PR #79, by the developer's assessment of 2026-10-14, such a name is no entry: the rule reads a
  theme's target by its name alone, so it cannot say which of the types a theme is for. It names each
  theme of the name in `Skipped`, and the reading names the name in its own `Skipped` where parts were
  found. Decision 7's list of what the reading's `Skipped` holds gains that kind.
- Decision 5 made an entry only of a type the rule credits a part to, and left a themed control with
  none to the rule's skip. Since PR #123, at DiffView's request and by the developer's decision of
  2026-11-06, a top-level, non-generic type the scan loaded that a theme targets has an entry too, its
  themes and no constants or lookups, so a reference built from the reading lists that theme. The rule
  has nothing of it to check, and still names it in a skip. A theme for a nested or a generic type has
  neither an entry nor a skip, in the reading or the rule, as before: a target's name does not give
  either. Decision 7's list gains a shared name where a theme targets it.

#### Where PR #84 departs from plan 00004

- Decision 2's reason for leaving `AuditColor.CompositeOver` as it is, that a blend cannot differ from
  the renderer's, does not hold. Headless on Avalonia 12.1.3, a translucent solid brush takes Skia's
  `SkARGB32_Blitter`, which premultiplies a channel as `(c * (a + 1)) >> 8`, and `blit_row_color32`
  blends it as `(d * inv + (s << 8) + 128) >> 8`, `inv` being `255 - a` plus its top bit: integers,
  alike on every CPU, in the Skia that SkiaSharp 3.119.4 carries. That arithmetic reproduced all 390
  cells of a sweep, six grey levels at thirteen opacities over five grounds, where the alpha drawn was
  `(byte)(A * opacity)` in every one and the blend a level off the exact blend in 101; over every input
  it falls short of the exact blend by up to two levels or exceeds it by one. `CompositeOver` stays the
  exact blend, as the decision has it, and decision 4's test predicts each pixel with the raster's
  arithmetic applied to the audit's colour, to the level, by the developer's decision of 2026-10-15. A
  GPU backend, which blends in floating point, was not measured.
- Decision 4's test draws none of the measurement's cases with an element's own opacity: the audit
  has no element opacity until the brand output's stacks, whose tests take them.
- The test project takes `Avalonia.Markup.Xaml.Loader` and `Avalonia.Themes.Simple` as well as
  `Avalonia.Skia`, all three already pinned: each case's markup is loaded at test time, as the
  measurement loaded it, so a case that does not load fails in its test rather than the build, and
  Simple's window template presents the cell.
- Decision 11's `ResourceValue.ColorLiteral.OpacityKey`, `ResourceValue.Alias.OpacityKey` and
  `ResourceValue.Alias.FromColor` land in PR #84, with decision 3, which reads through them; the rest
  of decision 11 is the brand output's.

#### Where PR #85 departs from plan 00004

- Step 3's check renamed nine of the port's placeholders, not two. Besides `{{SEMI …}}` and
  `{{PALETTE_SWATCHES}}`, the seven placeholders of the language that name one of the port's search
  colours, as `{{mix hue:dark =#FFEA00}}` and `{{val huefg:dark}}` do, became blocks the check's program
  renders with the port's own code: the language has no form for a colour only the port's C# knows, and
  decision 10 lets no block take a placeholder of the language. The port's own template will need the
  same when it adopts the library, and those seven are then figured by the port's arithmetic, not the
  library's, their one claim, `=#FFEA00`, checked by the port's program: none of them carries an
  opacity, so here the two agree, but a search colour drawn at an opacity would take the port's
  truncation in turn. Filled so, through `BrandRunner.Fill`, the document is the port's
  `Brand.md` but for its first line and the order of three style lines in its palette chart, the
  `Lines.axaml` tokens', which the library writes in the order the theme reads its files, as it draws
  the rows, where the port's generator wrote them in its files' alphabetical order. No figure moves: the
  template's one stack with two opacities, `#F9F9F9@0.6@0.56`, truncates to 85 either way.
- Decision 5's `frame` takes a literal colour as well as a key: the port frames its charts in Semi's
  `#5B5B5B` and `#F9F9F9`, which its palette does not hold.
- Decision 8 leaves the chart's title and labels open. They are the port's: the panel is titled "the
  palette's tokens, on the window's ground", and a swatch's label drops the `Brush` its key ends in.
- Decision 7's list of problems gains a file of the theme that cannot be read, missing or not
  well-formed, whose tokens the document would leave out unseen, though not an include of another
  assembly's theme, which the audit does not follow; a placeholder of the language written another way;
  a `{{` that opens no placeholder closing on its line; a stack in a `{{val}}`, as it refuses an opacity;
  an opacity that is not a number from 0 to 1; a stack whose bottom does not draw opaque; a `{{near}}`
  path that is absolute or in none of the folders; a package that no `Directory.Packages.props` at or
  above the configuration's folder pins once; and a `{{swatches}}` with no frame. A token file that does
  not exist or that the theme does not read, a variant the theme does not declare that is not
  `Default`, `Light` or `Dark`, which would be served as `Default`, and an empty path in `tokens` or
  `near` or an empty colour in `frame` are configuration errors, as an unknown theme or a missing
  template is, and exit 2.
- A file-based program that calls the library, as the check's did, needs `PublishAot` off: a
  file-based program's default, native AOT, turns off the reflection `AuditConfig.Load` reads its JSON
  with. The README says so.

#### Where PR #130 departs from plan 00006

- The plan was approved as 00005 on 2026-10-20, on a branch, before this repository became Inspections.
  CodeQuality's plan 00005 took that number on `main` first, so on 2026-11-11 the developer renumbered it
  00006, and plan 00006 00007; its text changed in those numbers alone. The section it names in `README.md`
  is in `docs/xamlquality.md` since the merge, and the solution it builds is `Inspections.slnx`.
- Decision 2 has every alias reach the application past its own dictionaries. One in a dictionary with a
  class of its own, which a theme holds by its element as Semi holds its palettes, does not. Measured
  headless on 2026-10-20: constructing such a dictionary throws where its alias names a key outside it,
  and constructed while the application's resources hold the key, it finds it. So its aliases resolve while
  it is constructed, before the application holds the theme, or, as the runtime check sets one up, the
  merged files, and the audit gives such an alias no colour where its own dictionary lacks the target. The
  inferred claim that it is a root of its own holds: it does not see the dictionary that holds it. A
  compiled theme's own aliases, and those in the files it includes, resolve at their first lookup, as read.
- Decision 1 has every alias search its own dictionaries first. A `DynamicResource`, an alias element
  written so or a brush's `Color` or `Opacity`, searches the application under the variant asked instead,
  as the port inferred and `DrawnColourTests` draws: a theme brush whose `Color` is a dynamic resource that a
  merged file redefines is drawn in the merged file's colour, and one outside every theme dictionary in the
  application's variant. A theme's dynamic resources follow the application's variant, not a window's, so the
  drawn cases set the application's.
- Decision 2 names a consumer's tokens and the merged files as the application's resources, with no order
  between them. The audit searches the tokens first, as it preferred them before.
- Decision 6's four cases are drawn, beside others the model rests on: an alias of its own key, which
  Avalonia does not find in its own dictionary while it builds it, so that it searches past it, as read; a
  dictionary's own entry over its Dark dictionary's under Dark, as read; and two dynamic resources. A
  `<DynamicResource>` alias element that a control takes by `{DynamicResource}` draws nothing, which the
  audit does not model; the follow-up below says why.
- The walk builds a dictionary for each include, so a file included in two places is read in each, its
  chain its own each time. What it cannot resolve is still reported once for each variant it is walked
  under, and a definition is still one object for each variant, which `CompatGenerator` compares.

#### Where PR #131 departs from plan 00007

- The plan was approved as 00006 on 2026-10-22 and renumbered 00007 on 2026-11-11, with plan 00006, as the
  section above says; its text changed in those numbers alone, its plan 00005 now 00006. The section it names
  in `README.md` is in `docs/xamlquality.md` since the merge.
- Decision 6 has each finding followed by every read and every key to redefine instead. A Markdown table holds no list
  under a row, so the table lists the findings, and after it a line for each finding lists its reads and its keys.
- Decision 2 counts an entry that one of the consumer's references finds "as the variant's lookup finds it". Step 3
  drew otherwise for a static reference: headless on Avalonia 12.1.3, a view's `{StaticResource T11}` of a control theme
  only the theme's Dark dictionary holds set no theme under Dark and drew nothing, where `{DynamicResource T11}` drew the
  theme's colour. So a consumer's static reference finds its key as plans/00006 has a static read search, in the variant
  of the theme dictionary around it, where the variant reaches that dictionary, or in `Default` alone, and a dynamic one
  under the variant asked. Over DiffView's `bf6b734` and the Semi consumer the reports do not move.
- Step 3 drew every kind decision 8 names, each finding drawing the theme's colour and each read that reaches the
  application the consumer's, in either order, so no kind becomes decision 5's first or third and `OrderDependentKinds`
  stays empty. Decision 5's first kind drew as it says: a palette held by its class whose chain lacks the key took the
  consumer's colour with the consumer's resources held first and threw `KeyNotFoundException` with them held after. A
  static read outside every theme dictionary of a key the theme holds only under Dark, with the consumer's
  redefinition held only under Dark too, finds nothing, by the model and in the drawing, which throws; the check calls
  it a finding, since nothing sees the redefinition. Semi's `SolidButton` drew `#54A9FF` under the palette key's
  redefinition and the consumer's colour under the token's.

#### On `main`, not yet released

Every change after the `xamlquality/v2026.4.1111` tag (`03d4767`) that a consumer can see; this
repository's own working documents are left out. A change that makes a rule stricter or adds API goes
here with its effect on a consumer, so the release that carries it can say so. `main` takes squash and
rebase merges only, and both give a branch's commits new hashes, so a row cites its pull request.

Nothing yet. `2026.4.1111` carried the last three rows. PR #130 and PR #131 change the theme audit: every committed report drifts once, in its opening sentence and by the section "Redefinitions the theme does not see", and is regenerated on upgrading; `UnseenRedefinition`, `ConsumerThemeFindings.Unseen` and
`ConsumerThemeFindings.UnseenNotDecided` are new and permanent; `theme-audit report`'s summary line gains the
unseen-redefinition count before the low-contrast one; and a compat dictionary or a contrast pair moves where an
alias now resolves as Avalonia resolves it, as DiffView's `FluentKeys.Semi.axaml` moves
`TextControlBorderThemeThicknessFocused` from 0 to 2. PR #134 adds `BNXQ1011`, which changes nothing until a
consumer constructs it.

### Consumers inside the family

`2026.3.925` renamed the rule ids: what `2026.3.924` shipped as `XQ1001` to `XQ1004` is `BNXQ1001`
to `BNXQ1004` from it on. Each entry below keeps the ids of the release it pins, and a consumer that
moves its pin past `2026.3.924` renames them in its test names, messages and comments.

- **ScopedEditors' tests** pin `2026.3.928`, test-only, since JanusMael/Bennewitz.Ninja.ScopedEditors#10,
  which records the rules new in it as the owner's call. They run BNXQ1001 and BNXQ1002, through
  `InteractiveAutomationNameRule(additionalElements)`, `ExpanderAutomationNameRule`,
  `XamlScanContext.Load`, `XamlFile.RelativePath` / `.Text` / `.ParseError`, and
  `XamlRuleResult.Inspected` / `.Findings`; narrowing any of them breaks ScopedEditors. By its own
  report its move to `2026.3.925` was clean but for the renames: its markup writes no empty
  `<AutomationProperties.Name>`, so `2026.3.924`'s stricter names found nothing there.
- **ScopedEditors cites `docs/avalonia-gotchas.md` by path**, in `AppSeverityToGlyphConverter.cs` and
  its tests, `PropertyEditorWrapper.axaml` twice, and `DangerSurfaceMarkupTests.cs`. They rely on the
  entries about emoji-font fallback and about `AutomationProperties.Name` being ignored on a
  `TextBlock`. Moving or renaming the file or those entries means updating them.
- **The documents in `docs/` are the only copies of themselves**, by the owner's decision of
  2026-09-24. The WPF-to-Avalonia port the guide began in retired its copy of it (`8e46a8f`) and
  points its `CLAUDE.md` at both documents here. Templates' `PROGRESS.md` links the gotchas.
  ClaudeForge retired its pre-move `docs/AVALONIA-GOTCHAS.md` and repointed its links here in
  JanusMael/ClaudeForge#80, and its UI style guide's section 14 points here since
  JanusMael/ClaudeForge#87. The drivable-ui skill, which condenses the guide, lives here too since
  PR #47, moved from bb-skills, whose `feat/drivable-ui` branch the same decision retired once it
  merged; its history is kept there as the tag `archive/feat-drivable-ui`. Moving either file, or
  retitling a section or entry someone cites, means telling them: the port through its live session
  alone, by the developer's decision of 2026-10-13, never through the retired one. The port's name
  appears nowhere in this repository's documents, by the same decision; `plans/00001`, approved
  before it, keeps its own.
- **OpenForge2k's tests**, jmui's `ClaudeForge.Tests`, pin `2026.3.925` on `main`, test-only, since
  JanusMael/ClaudeForge#91 merged on 2026-09-25, and run BNXQ1001 alone. They use
  `XamlScanContext.Load`, `ExpanderAutomationNameRule`, and `XamlRuleResult.Inspected` /
  `.Findings`.
- **Templates' `bbavalonia` template** pins `2026.3.928` for the apps it generates, test-only, since
  JanusMael/Bennewitz.Ninja.Templates#17, published in Templates `2026.3.928`. Their
  `AutomationNameTests` use `XamlScanContext.Load` and `.Files`, `InteractiveAutomationNameRule()`,
  `InteractiveAutomationIdRule()`, `ExpanderAutomationNameRule`, and `XamlRuleResult.Inspected` /
  `.Findings`, and name BNXQ1001, BNXQ1002 and BNXQ1007 in their test names and messages. Its
  `AGENTS.md` sends Avalonia and drivable-UI lessons here.
- **DiffView**'s tests pin `2026.3.928` on its `main` (`4aef7a9`), test-only, beside
  `Bennewitz.Ninja.AssemblyQuality` `2026.3.928`, the two moved together by its plan 00028. They run
  `BNXQ1002`, with DiffView's own element names beside the stock set, `BNXQ1003`, `BNXQ1004`,
  `BNXQ1006`, `BNXQ1007` and `BNXQ1008`, the last adopted in its plan 00026, and decline `BNXQ1001`.
  DiffView also runs the audit and commits its report. Its measurements against `2026.3.924`
  proposed the peer check, written as `BNXQ1006` in PR #27, and the reading of a helper's lookups that
  `BNXQ1003` gained in PR #30.

### Requests from other repositories

Every request another repository has made of this one, with the date it arrived, so that its age is
seen. A lesson for `docs/avalonia-gotchas.md` or `docs/ai-drivable-ui.md` is checked before it lands,
and its sender hears landed, changed or declined. One that needs a real window and real input is
measured in Windows Sandbox, a desktop of its own, in a batch launched when the developer says so, by
the developer's decision of 2026-10-12. One that needs X11 or a Linux desktop is measured under WSL,
in a headless GNOME session no one sees, or on a Linux machine on the tailnet, and lands like any
other, by the developer's decision of 2026-10-13. One from the WPF-to-Avalonia port lands as the port
measured it, without being measured again here, its own marks of measured and inferred kept and its
entry saying it was not reproduced here, by the developer's decision of 2026-10-14. So did the lessons
DiffView had sent and nobody here had checked, measured in its own work, by the developer's decision
of 2026-10-15. One that needs an ARM64 machine is not reproduced here, and stays out of the
documents, by the developer's decision of 2026-10-12.

| Arrived | From | Request | State |
|---|---|---|---|
| 2026-09-28 | DiffView | Five lessons from its plan 00029: raising a menu item's `Click` does not toggle it, key bindings run before `KeyDown`, and three facts about driving X11 | Received, and a receipt sent on 2026-10-10. Landed in PR #83 as DiffView measured or read them, at Avalonia 12.1.2 under GNOME Shell 48.7 with XWayland 24.1.6, not reproduced here, by the developer's decision of 2026-10-15: the first two as entries of their own, and the three X11 facts in the `xdotool` entry, beside the others DiffView's notes hold for this document: a popup lays itself out a pass after it opens, a grab cannot say whether an action took, `xdotool getwindowgeometry` counts a reparenting frame twice, and `xwininfo` lists windows in free text. The outcome went to DiffView on 2026-10-15 |
| 2026-09-30 | DiffView | A headless hit-test reads the last composed frame, not the layout | Received, and a receipt sent on 2026-10-10. Measured on 2026-10-13, headless on Avalonia 12.1.3 and on a real window in Windows Sandbox, and landed in PR #59: it holds on a real window too, until the next frame is composed, in 10 rounds of 10; headless, `CaptureRenderedFrame()`, or `RunJobs` followed by a forced render tick, settles it, and neither `RunJobs` nor a forced tick alone does. The outcome went to DiffView on 2026-10-13 |
| 2026-10-03 | DiffView | A public `BNXQ1003` reading: per control, the parts it looks up as distinct from the `PART_` constants it only declares, and which code makes each lookup | Accepted. [Plan 00002](plans/00002-template-part-reading.md), approved on 2026-10-13, takes it in two steps: the reading, with the parts each theme declares, then a parts report the theme audit writes and checks. Step A, the reading, landed in PR #70, and step B, the parts report and its drift check, in PR #72. `2026.4.1014` carries both, released on 2026-10-14, which DiffView's plan 00030 was waiting for, and DiffView was told that day. The assessment of `BNXQ1003` that the developer's order of work of 2026-10-13 put after plan 00003 was decided on 2026-10-14, in PR #79 |
| 2026-10-08 | DiffView | Twelve findings from driving an Avalonia app on Windows through UI Automation | Received, and a receipt sent on 2026-10-10. Items 1, 2, 3, 5, 6, 7, 8 and 11 measured in Windows Sandbox on 2026-10-13, on Avalonia 12.1.3: 1, 2, 3, 5 and 11 hold; 6 holds for a process that has sent no input, refused in the second session, but not for one that has just sent input, which took the foreground in both; 8 is narrowed, keys reaching a window with nothing focused but only its own bindings; 7 did not reproduce, in either session, and is left out. Items 1 and 2 landed in PR #59; 3, 5, 6, 8 and 11 in the guide in PR #60, 3 as a sentence. 4, 9, 10 and 12 were not measured here, and none lands: DiffView's own notes keep 4 and 9 as its own and leave 12 out, and record 10 as already in the guide's journal. Its suggestion of a layer for a by-hand driver with a person watching landed in section 4 in PR #67, by the developer's decision of 2026-10-13. The outcome went to DiffView on 2026-10-13, and what became of items 4, 9, 10 and 12 on 2026-10-15 |
| 2026-10-08 | The WPF-to-Avalonia port | A correction to the render-tier entry, the cost of the software renderer, and a headless click that lands where a control used to be | Received, and a receipt sent on 2026-10-10. The correction measured on x64 in Windows Sandbox on 2026-10-13, and landed in PR #65, changed: with no GPU the guest's adapter 0 was the Basic Render Driver and Avalonia drew through ANGLE on WARP, and the fix tells that adapter by its name, or its vendor and device, as well as the software flag, which two of its three entries in the guest lacked. The correction's ARM64 rule and measurement, and the renderer's cost, measured on ARM64 alone, are left out, by the developer's decision of 2026-10-12; its x64 comparison of a forced `Software` against the default, made with a GPU and not repeated here, is not landed either, since the entry is about telling software rendering apart, not about its cost; on 2026-10-14 the developer kept both out again. The outcome went to the port's live session on 2026-10-13, not to the retired one that sent this, by the developer's decision of that day. The headless click landed in PR #75 as the port measured it, not reproduced here, by the developer's decision of 2026-10-14. Its outcome went to the port's live session on 2026-10-14 |
| 2026-10-09 | DiffView | Two driving lessons: a drag's success is not its effect, and a menu's automation window is not the popup it is drawn in | Received, and a receipt sent on 2026-10-10. Measured in Windows Sandbox on 2026-10-13: the drag's holds; the menu's is changed, its items being under the main window's element and nearest the popup's window, which is where they are drawn, alike for a menu in XAML, one built in code and a context menu. Both landed in the guide in PR #60, the menu's as changed. The outcome went to DiffView on 2026-10-13 |
| 2026-10-09 | Issue #48 | Two AT-SPI bridge behaviours: a list keeps the children it first reported, and a list's scroll bars are not in the tree | Measured on Linux on 2026-10-13, and both landed with their fixes in PR #57, each narrowed: a list keeps the nodes it first reported, which a scroll renames after the rows their containers hold, and its scroll bars are missing only where it had rows when first asked. The bars' fix gains a warning, measured: a bar written through it stops following the list. Where the surfaced bars sit in the tree, which depends on the application's own layout, is left out. The outcome went to the port's live session on 2026-10-13, by the developer's decision of that day |
| 2026-10-09 | Templates | Trimming, side 2: when a repository trims, every `Bennewitz.Ninja.*` package in its restore graph carries `IsTrimmable` | Accepted, and the approach agreed on 2026-10-10. Side 2 landed in Templates in JanusMael/Bennewitz.Ninja.Templates#58 on 2026-10-30, and its `TrimmablePackages` rule finds nothing here, since only test projects take a family package that adds an assembly, as issue #106 measured. Nothing here waits on it |
| 2026-10-10 | DiffView | Three lessons for the guide: a harness that counts silence as success, the automation patterns of stock template parts, and asking before borrowing the desktop | Received, and a receipt sent on 2026-10-10. The second measured in Windows Sandbox on 2026-10-13: it holds but for the `ExpandCollapse` it reported on a `MenuItem`, found only on the window's System menu item, which is Windows' own; and a checkable `MenuItem` offers no Toggle pattern on Windows, a defect in Avalonia's automation that a peer of the application's own works around, measured headless and, in the second session, on a real window. That part landed in PR #59, and the survey in the guide in PR #60. The first landed there too, as method: a run that stops early is not a pass. The third the guide already says, in sections 3, 4 and 7. The outcome went to DiffView on 2026-10-13 |
| 2026-10-12 | Issue #55 | Four more AT-SPI bridge behaviours, two X11 window findings, and a correction to the guide: driving over AT-SPI is verified | Measured on Linux on 2026-10-13, and landed in PR #57, with changes: `SelectChild` adds only where the list allows more than one selection; an open combo's items are missing from its children for the reason a list's late rows are, the bridge keeping the children it was first asked for, so the two are one entry; the move's fix may instead lay the window out before moving it, measured, and the frame's waits for the frame to change rather than polling, measured through `FrameSizeProperty`; the active frame's fix names `_NET_ACTIVE_WINDOW` and the bridge's own events rather than `_NET_WM_STATE_FOCUSED`, none of them measured, since no window became active in the headless session; and the move's claims about Windows are left out, unmeasured. The outcome went to the port's live session on 2026-10-13, by the developer's decision of that day |
| 2026-10-13 | The WPF-to-Avalonia port | Avalonia.Wayland 12.1.3 and 12.1.4 do not start the AT-SPI bridge, so a window on the Wayland backend has no accessibility tree; issue #55's verification scoped to the X11 backend; and four facts about the Wayland backend: window methods that do nothing, a screen scaling fixed at 1.0, two options it lacks, and no attention request | Received, and a receipt sent on 2026-10-13. The first two measured on 2026-10-13, on Avalonia 12.1.3 in a headless GNOME session under WSL: a window on the Wayland backend, which only `UseWayland()` or `UseWaylandWithFallback()` chooses, opened without the bridge's assembly loaded and did not appear on the AT-SPI bus in 20 seconds of watching, where one on X11, which `UsePlatformDetect()` chooses on Linux, appeared there with its children, in 3 runs of 3 each; in the source at the tag only `Avalonia.X11` references the bridge. 12.1.4 not measured. The first two landed in PR #64 for 12.1.3: the first as an entry of the gotchas, the second as the guide's Linux paragraph and the environment probe's verdict under Wayland, scoped to the X11 backend. The outcome went to the port's live session on 2026-10-13. The four facts landed in PR #75 as the port read them in Avalonia.Wayland 12.1.3's package, not reproduced here, by the developer's decision of 2026-10-14. Their outcome went to the port's live session on 2026-10-14 |
| 2026-10-13 | DiffView | Two follow-ups to its earlier lessons. The X11 tooltip re-measured: `xdotool mousemove` raised one in ten trials of ten, on Avalonia 12.1.2 under GNOME Shell 48.7 with Xwayland 24.1.6, where the `xdotool` entry says a tooltip never appears; the same Avalonia had raised none on 2026-09-13, for a reason not found. And item 7 of 2026-10-08 back with a qualifier: raising a window closes a menu it opened when the raise changes the foreground | Received, and a receipt sent on 2026-10-13. The qualifier measured in Windows Sandbox on 2026-10-13, on Avalonia 12.1.3, and landed in PR #73, changed: when another application took the foreground, by a call or by a click, the menu closed at once, before any raise, so raising the window back found it closed; a window holding the foreground kept its menu open through four ways of raising it, and a click on its own title bar closed it. No way was found to leave a menu open on a window without the foreground; a window covered by an always-on-top one, which keeps the foreground, was not measured. The outcome went to DiffView on 2026-10-13. The tooltip landed in PR #83 as DiffView measured it, not reproduced here, by the developer's decision of 2026-10-15, and the `xdotool` entry's heading no longer says a tooltip never appears. The tooltip's outcome went to DiffView on 2026-10-15 |
| 2026-10-13 | The WPF-to-Avalonia port | Two headless findings on Avalonia 12.1.3 and Semi.Avalonia 12.1.0.1: a `ScrollContentPresenter` loses a wheel notch when an `ILogicalScrollable`'s position moved before it raised `ScrollInvalidated`, in 7 runs of 30 of a test that turned the wheel straight after `RunJobs`; and Semi's `SplitView` pane slides open, so a fixture that measures right after setting `IsPaneOpen` reads it part way. A correction the same day withdrew the sentence that a render tick before the notch prevented the loss | Received, and a receipt sent on 2026-10-13. Both landed in PR #75 as the port measured them, the withdrawn sentence left out, not reproduced here, by the developer's decision of 2026-10-14. The outcome went to the port's live session on 2026-10-14 |
| 2026-10-13 | The WPF-to-Avalonia port | Two findings, headless on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1. A subclass that borrows its base's theme through `StyleKeyOverride` is not selected by a style that names its own type, only by one that names the base, its cause inferred and not read. And Semi's `TextBox` template puts `InnerRightContent` in an `Auto` column beside the text's star column, so wide inner content squeezes the text to nothing: a 183 px block left the text 0 px wide when the box was offered 159 to 220 px | Received, and a receipt sent on 2026-10-13. Both landed in PR #75 as the port measured them, the first one's cause as inferred, not reproduced here, by the developer's decision of 2026-10-14. The outcome went to the port's live session on 2026-10-14 |
| 2026-10-13 | The WPF-to-Avalonia port | Four findings, headless on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1. Semi's `TextBox` pads its `InnerLeftContent` whenever the content is not null, a hidden one included, by `TextBoxInnerLeftContentPadding`, and its `InnerRightContent` likewise; Semi's `Button` sets a `MinHeight` of 32, which wins over a local `Height`; a `MeasureOverride` decision taken from a template's inner layout is not revisited when content inside the template changes and the control's desired size does not; and measuring a template twice inside one `MeasureOverride` after changing a descendant's `MaxWidth` changes nothing unless every visual ancestor between is invalidated too. All four measured, the last two with their causes inferred | Received, and a receipt sent on 2026-10-13. Sent again on 2026-10-14, and a second receipt sent. All four landed in PR #75 as the port measured them, the last two causes as inferred, not reproduced here, by the developer's decision of 2026-10-14. The outcome went to the port's live session on 2026-10-14 |
| 2026-10-13 | DiffView | Two lessons for the guide's section 5, from verifying its click guard on a real desktop. A one-off harness written beside a DPI-aware driver was itself unaware, and at 200% fed a 2200x1440 window's virtualised 1113x756 back into `SetWindowPos`. And a negative result about a guard is evidence only once the condition it guards against is shown present: measure `WindowFromPoint` at the click point first, and report a run whose premise failed as unread, never as a failure | Received, offered to use or discard. Both landed in the guide's section 5, in its steps 7 and 11, in PR #83, as DiffView measured them, not reproduced here, by the developer's decision of 2026-10-15. The outcome went to DiffView on 2026-10-15 |
| 2026-10-14 | The WPF-to-Avalonia port | Two findings on Avalonia 12.1.3 and .NET 10, headless on Linux: `LogToTrace()`, the project template's default, writes Avalonia's warnings nowhere, since with no debugger attached the only trace listener has no file, and `LogToTrace`, `LogToTextWriter` and `LogToDelegate` all drop the level. Later the same day, the fix it made, a sink of its own held by 13 headless tests, each seen to fail against a deliberate break of it; then that sink in its running application, under Xwayland, whose first run logged 352 warnings no run had seen, and where a `{ReflectionBinding}` to a missing property, planted on a window, built green and failed the UI run on its log. That evening a third, measured in the same application and a headless fixture: a compiled binding through a null intermediate logs `Value is null` for every binding on every container where it is null, which `?.` stops, but for a value-type target with no converter, which then needs a `TargetNullValue` | Received, and a receipt sent on 2026-10-14. All three landed in PR #80 as the port measured or read them, its fix and its running application's measurement with them, not reproduced here, by the developer's decision of 2026-10-14, but for two things read here in the source at 12.1.3. The port read `StringLogSink` writing an event's source as `(Type #hash)`, and it writes a named element's as `(Type #Name)`, so the entry says only that the source is written in parentheses. And the port's rule of a `TargetNullValue` for any value-type target with no converter gains a line, not measured, that a `Nullable<T>` target takes the null, as `TypeUtilities.AcceptsNull` says. The developer put two rule candidates on the backlog the same day, below, one from the first finding and one from the third. The outcome went to the port's live session on 2026-10-14 |
| 2026-10-14 | The WPF-to-Avalonia port | Its brand catalogue tools, sent at the developer's request for a home where other repositories can use them: a generator that fills a brand document from a theme's palette, compositing opacities, computing WCAG 2.2 contrast, drawing mermaid swatch charts, and stopping the build where a claim a placeholder carries fails; an exporter that splits a document's mermaid blocks into files and renders them; and, the same day, ten fixes for them | Received, and a receipt sent on 2026-10-14 for the tools and for the fixes. Placed by the developer on 2026-10-15. The generator's general half becomes a `theme-audit` output, for XAML palettes only, by a plan of its own, `plans/00004`, which is to ask too whether it can read a brush based on or read from another resource, where the port's generator stops; the fixes that concern the generator go to that plan. The exporter goes to the `tools/` of the mermaid-charts skill in bb-skills, and that skill covers swatch charts, gaining the port's lessons on drawing them. What is wired to the port stays there. Plan 00004, approved on 2026-10-15, took the generator in two pull requests: the audit's colours in PR #84, measured headless, and the brand output in PR #85, which fills the port's own template, its search colours passed as blocks, into the port's `Brand.md` but for its first line and the order of three style lines. The exporter is ported to the mermaid-charts skill's `tools/` as `export_blocks.py`, with a PowerShell twin beside it, `Export-MermaidBlocks.ps1`, held to the same tests, and the skill gains lessons on drawing swatches, the port's among them, each measured with the skill's pinned renderer, all approved by the developer on 2026-10-19, and merged into bb-skills' `main` and installed on 2026-10-20. The outcome went to the port's live session on 2026-10-20: where each piece landed, what became of each of its ten fixes, and that the seven placeholders of its template naming a search colour become blocks of its own. `2026.4.1026`, released on 2026-10-26, carries the brand output, and the port heard so that day |
| 2026-10-15 | The WPF-to-Avalonia port | Two findings, headless on Linux on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1, dark variant. Redefining Semi's palette keys, the colour `SemiBlue5Color` or the brush `SemiColorPrimary`, in the application's resources or in a merged dictionary's dark theme dictionary does not recolour Semi's controls, though `Application.TryGetResource` returns the redefinition, and redefining the component tokens the controls read does: measured, with the tokens' aliases read in Semi's source and the scope they resolve in inferred. And a `TemplateBinding` holds a part's value at `Template` priority, so an unconditional style aimed at the part does not beat it and a conditional one does: measured, with the order read in `BindingPriority`, and which priority a style takes in `StyleInstance`, which corrects the precedence the entry on local values gave | Received, and a receipt sent on 2026-10-15. Both landed in PR #87 as the port measured or read them, its inference marked, not reproduced here, by the developer's decision of 2026-10-14, and the entry on local values gives the precedence as read. Of their static parts, the developer put the first's on the rule backlog on 2026-10-19, a style aimed into a template that a `TemplateBinding` outranks, and held the second's, an audit check, among the follow-ups. The outcome went to the port's live session on 2026-10-19 |
| 2026-10-16 | Issue #86, from the WPF-to-Avalonia port | Avalonia 12.1.3 never asks `IDataErrorInfo`, so a WPF `ValidatesOnDataErrors` binding ports to nothing: measured headless on Linux against `INotifyDataErrorInfo`, and read in the `Avalonia` package, none of whose assemblies names it. With what the leftover cost the port: a deadlock in Rx, set off by a re-raise of `PropertyChanged` kept only to make WPF ask again, which hid there since under Avalonia the re-raise drew nothing | Landed in PR #87 as the port measured or read it, the deadlock with it as Rx's and not Avalonia's, not reproduced here, by the developer's decision of 2026-10-14. Its static part, a view model that implements `IDataErrorInfo`, went on the rule backlog by the developer's decision of 2026-10-19. The outcome went to the port's live session on 2026-10-19, with that of the findings of 2026-10-15 |
| 2026-10-19 | The WPF-to-Avalonia port | A focus ring is built once, when keyboard focus arrives, so a `FocusAdorner` a style varies by state goes stale, headless on Linux on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1, dark variant: measured in a row's theme and in Semi's own `ToggleButton`, stale after Space both ways, with the cause read in `Control.cs`; the fix, one ring bound to a property of the control it adorns, measured; and a pair of application styles that replaces Semi's ring, measured, with a plain style alone below Semi's inferred, as a correction the same day marked it | Received, and a receipt sent on 2026-10-19. Landed in PR #90 as the port measured or read it, its inference marked, not reproduced here, by the developer's decision of 2026-10-14. Its static part, a `FocusAdorner` setter in a style with an activator, went on the rule backlog by the developer's decision of 2026-10-19. The outcome went to the port's live session on 2026-10-19 |
| 2026-10-20 | The WPF-to-Avalonia port | Its answer to the question the outcome of PR #87 put, how a `StaticResource` inside a theme's own dictionary resolves, read in the source of Avalonia 12.1.3, of the XamlX it pins and of Semi.Avalonia 12.1.0.1 by a subagent of its own, five of whose citations the port read again at the tag, each claim marked read or inferred. Read: an alias in a dictionary is a deferred item, which takes a snapshot of the resource nodes on the XAML parent stack when it is added, a stack that runs through the theme's include chain and ends with `Application.Current`, and which at its first lookup walks that snapshot innermost first and keeps the first hit for good. Inferred: Semi finds every token its aliases name in its own dictionaries before the walk reaches the application, as a text check of the 557 `StaticResource` references in its 51 dark theme files found. Its inference of 2026-10-15 holds, the port finds, with two corrections: the lookup happens at first access, not at load, and the application is on the stack, as its last node. With the edge cases an audit check would meet, and two corollaries, inferred and not measured | Received, and a receipt sent on 2026-10-20. Landed in PR #139 in the entry on Semi's palette keys as the port read it, its inference marked, by the developer's decision of 2026-10-14, with the order inside one dictionary as the audit's model reads it and what `DrawnColourTests` draws of it. The developer asked for the audit check held for it on 2026-10-20, its model corrected first: plan 00006, PR #130, which drew the readings and inferences it rests on, and the check, plan 00007, PR #131, both released in `2026.4.1111` |
| 2026-10-21 | Issue #92, from the WPF-to-Avalonia port | A release carrying PRs #84, #85 and #88 before its plan's step 7, where its brand catalogue moves to `theme-audit brand` and its own generator retires; until there is one it would run the tool from `main` at a pinned commit. With six defects of its generator and where `theme-audit brand` stands on each, checked against plan 00004's decisions and PR #85's description, not by running the tool | The five defects the tool could carry are each absent as built, checked here against `main`'s tests: an opacity in a `{{val}}` is a problem, `BrandRunnerTests.Val_takes_no_opacity`; a palette is read in the variant its document names, and its chart is framed in that variant's colours, `A_palette_with_light_and_dark_variants_is_filled_in_each_and_its_chart_framed_in_that_variants_colours`, a variant the theme does not declare being a configuration error unless it is `Default`, `Light` or `Dark`, `A_variant_the_theme_does_not_declare_is_a_configuration_error_unless_it_is_one_of_Avalonias_own`; problems exit 1 and a configuration error 2, `ThemeAuditToolTests`; and every problem is listed with its line and nothing is written, `Every_problem_is_listed_together_with_its_line_and_no_document_is_given`. The sixth is the port's console output alone. The developer, who had held releases since 2026-10-15, called for one on 2026-10-26, and `2026.4.1026` carries all three. The outcome went to the port's live session that day |
| 2026-10-21 | Issue #93, from the WPF-to-Avalonia port | `FindResource(key)` passes no theme variant, so in a dark application a control gets Semi's light `FlyoutBackground`: measured once, headless on Linux on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1, the cause read in the source at the tag; it extends the entry on a themed resource looked up with a null variant | Landed in PR #96 as the port measured or read it, not reproduced here, by the developer's decision of 2026-10-14, in the entry it extends, whose heading stays. Its static part, a call to the overloads that pass no variant, went on the rule backlog by the developer's decision of 2026-10-26. The outcome went to the port's live session that day |
| 2026-10-22 | The WPF-to-Avalonia port | Two findings from its plan's step 3, headless on Linux on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1, dark variant. A dangling `StaticResource` alias compiles with no `AVLN` diagnostic, and `Application.TryGetResource` throws `KeyNotFoundException` for its key: measured. And at run time an application's merged dictionaries are plain `ResourceDictionary` objects, with no `ResourceInclude` and no `Source`: measured, the compiler putting each included dictionary in its include's place inferred | Received, and a receipt sent on 2026-10-22. Both landed in PR #139 under *Styling / theming* as the port measured them, its inference marked, not reproduced here, by the developer's decision of 2026-10-14. The throw is measured here too, by the runtime theme check's `ThemeRuntimeCheckTests` |
| 2026-10-22 | The WPF-to-Avalonia port | Three findings from its plan's step 4, on the same versions. Headless `KeyPressQwerty` presses a key and never releases it, and a button acts on Space at the release: measured on four toggles, the key-up handling inferred. A `ResourceInclude` of a file whose root is `Styles` is a build error, `AVLN2000`: measured. And a `Border`'s translucent stroke lies half over its own `Background` and half over what is beneath it: measured to the pixel, `CenterBorder` being the default inferred | Received, and a receipt sent on 2026-10-22. All three landed in PR #139 as the port measured them, its inferences marked, not reproduced here, by the developer's decision of 2026-10-14: the first under *Testing*, the second under *Styling / theming* and the third under *XAML / layout*. The third's static part, scoring a translucent stroke over both its grounds in the audit's contrast check, went to the backlog below by the developer's decision of 2026-11-12 |
| 2026-10-22 | The WPF-to-Avalonia port | A finding from its plan's step 5, headless on Linux x64 on Avalonia 12.1.3 with xUnit v3: between dispatched jobs, a thread that asks for the UI thread can become it, so a guard on `Dispatcher.UIThread.CheckAccess()` passes on a pool thread: measured, with its fix, a thread started and joined inside a dispatched job; the cause read in the source, the headless session's reset between jobs inferred | Received, and a receipt sent on 2026-10-22. Landed in PR #139 under *Testing* as the port measured or read it, its inference marked, not reproduced here, by the developer's decision of 2026-10-14 |
| 2026-10-26 | Issue #95, from the WPF-to-Avalonia port | A `VirtualizingPanel` that prepares a container again deselects it and leaves it drawing its old item: measured headless on Linux on Avalonia 12.1.3, each case red in a fixture before the fix and green after it, the causes read in the source at the tag; with an item removed from the source being deselected by design, read there too | Landed in PR #96 as the port measured or read it, not reproduced here, by the developer's decision of 2026-10-14, as an entry under *Virtualization / perf*. It has no static part: whether a panel prepares a container again is decided by its code as it runs. The outcome went to the port's live session that day |
| 2026-10-26 | The WPF-to-Avalonia port | An adorner whose root has a `Margin` is clipped short by that margin on its right and bottom, headless on Linux on Avalonia 12.1.3: measured, the cause read in the source at the tag, and the fix, a `Panel` root with the margin on a child, measured. With three companions: a templated control clips a ring that reaches past it, read, a case measured; a `ScrollViewer` brings a focused control into view by its own bounds, read, its fix measured; and a `ToggleButton` style does not reach a `CheckBox`, measured, whose ring adorns its template's focus target, read | Received, and a receipt sent on 2026-10-26. Landed in PR #139 under *Styling / theming*, the companions in the same entry, as the port measured or read them, its marks kept, not reproduced here, by the developer's decision of 2026-10-14; the entry on layout clipping gains the line that its `ClipToBounds` default is `TemplatedControl`'s. Its static part, a `FocusAdornerTemplate` whose root sets a `Margin`, went on the rule backlog by the developer's decision of 2026-10-26 |
| 2026-10-29 | Issue #105, from the WPF-to-Avalonia port | Five findings met fixing one defect, a `TabControl` whose tabs share one content view, each measured on Avalonia 12.1.3 and .NET 10, headless on Linux with Skia, red in a fixture before its fix and green after: a `TabControl` gives every tab one content view and only its `DataContext` changes, the cause read; `RemoveHandler` removes every subscription of an equal delegate, read and measured both ways; in the tunnel phase one element's handlers run last added first, read; an item added to a `ListBox`'s `SelectedItems` while its `DataContext` changes is listed twice, read with the stack traces; and inside `OnDataContextBeginUpdate`, `DataContext` already reads the new value, measured, where it is written not read | All five landed in PR #140 as the port measured or read them, not reproduced here, by the developer's decision of 2026-10-14: the first under *Templates / controls* beside the entry on a `TabControl`'s names, the second and third beside the entries on key routing, the fourth beside the entries on a `ListBox`'s selection, and the fifth under *Bindings / view-model*. None has a static part a rule could decide: each turns on what code does as it runs |
| 2026-10-30 | Issue #106, from Templates | A correction: Templates' `plans/00008` side 2, landed in JanusMael/Bennewitz.Ninja.Templates#58, does not lift XamlQuality's `Trimming` exemption, since side 1 still holds this repository's own libraries to trimming; measured with its new script over `main`, no `TrimmablePackages` finding here | The record is corrected in PR #140, under *Where the merge departs from plans/00009* and in the side-2 row above, and the exemption stays, by the developer's decision of 2026-11-13 |
| 2026-10-30 | One of our WPF-to-Avalonia ports | Exception resilience, which that port says the developer asked of it: a catalogue of 32 shapes in which one exception from ordinary input silently kills a feature, is swallowed, or ends the process, each with its mechanism, fix and probe cases, measured on System.Reactive 6.1.0, DynamicData 9.4.33, Avalonia 12.1.3 headless and .NET 10.0.14 on Linux, each case twice in its own process; a guide to judging whether a flagged site can really throw; a text-based scanner for the 32 shapes with a self-test corpus of 70 expected sites and 22 safe forms; and 113 probe cases | Received, and a receipt sent on 2026-10-30, the catalogue and the guide pasted whole and the rest listed. Its Avalonia probe cases were sent too and measured here on Windows 11 on 2026-10-30, each twice, and every result held: one throw from `MeasureOverride` or `ArrangeOverride` stops a window's layout; a two-way binding whose `ConvertBack` throws writes the source's default over it; a converter's or setter's throw never reaches the application; with no dispatcher handler a posted throw ends the process, `0xE0434352` here; a `DispatcherTimer` stops after one throw in `Tick`; and `InvokeAsync`'s exception reaches only `UnobservedTaskException`. Two fixes for the `ConvertBack` case, `Mode=OneWay` and a `ConvertBack` returning `DoNothing`, were measured here and not there. All six landed in PR #111, the first under *XAML / layout*, two under *Bindings / view-model*, and three in a new section, *Dispatcher / exceptions*; its Linux-only results for a new window, `UpdateLayout()` and `Render` are in the layout entry, marked not reproduced here. The outcome went to that port on 2026-10-30. The rest the developer decided on 2026-10-31, under *Follow-ups* below: the shapes that are not Avalonia's go into `docs/exception-resilience.md`, measured again here first, the scanner becomes a plan for CodeQuality rules, and the `ConvertBack` candidate is on the rule backlog. That port keeps its scanner, samples, probes and proof until they are asked for |
| 2026-10-30 | DiffView | One mutation runner for the family, the developer's decision of 2026-10-22: a dotnet tool from this repository converging DiffView's `scripts/mutate-behaviour.cs` with `tools/mutate.cs`, after which each repository retires its copy. DiffView's brings committed sets beside the tests, each mutation naming its killer and the words its failure must carry; a full run failing while a declared test is killed by nothing; wrong verdicts compared, not printed; a green control in every set; markers that excuse a test no mutation can reach while the pins hold; `--owed`, which runs what a change owes; and `--parallel` over a tree written through a temporary index. This repository's brings a catalog of defects run in copies of the tree, each held to the exact set of tests that must fail, rebuilding only what a defect touches and swapping the assembly, and the refusal of a run estimated past thirty minutes | Received, and a receipt sent on 2026-10-30 correcting two details of DiffView's record of `tools/mutate.cs`. Taken on by the developer on 2026-11-06, as a numbered plan of its own after plans 00006 and 00007, and built by plan 00008, approved 2026-11-16, in PR #144: the testing family's `Bennewitz.Ninja.Testing.Mutate`, under *Testing* at the end, not yet released. DiffView does nothing until a release carries it, then moves its sets to it and retires its script |
| 2026-10-31 | One of our WPF-to-Avalonia ports | Two findings on Avalonia 12.1.3, headless on Linux with Skia drawing: a `TextBlock`'s `LineHeight` above the face's natural height centres the glyphs in each line box, where `LineSpacing` puts the extra below each line, the last included; and a type selector naming `Control`, as `Control.row-line[…]`, matches no real control, since every one is a subclass, where `:is(Control)` does, a sharper form of the note under `BNXQ1009` | Received, with both probes and their output, which build only inside that port. Measured here on Windows on 2026-10-31 by a probe of this repository's own, headless with Skia drawing, each twice, and both held: `LineHeight` at the natural height plus 4 moved every baseline down by 2, in Consolas and in Cascadia Mono, and `LineSpacing` 4 left it in place; `Control.x` left a `TextBlock` at `Stretch` where `:is(Control).x` and `TextBlock.x` set `Top`. Landed in PR #113: the first as an entry under *XAML / layout*, the second as a sentence in the `BNXQ1009` entry's warning that a type selector matches exactly. The outcome went to that port the same day |
| 2026-10-31 | One of our WPF-to-Avalonia ports | At this repository's request, its Rx and plain .NET probes for the catalogue of 2026-10-30, whole: `probes/rx`, 62 cases on System.Reactive 6.1.0 and DynamicData 9.4.33, and `probes/dotnet`, 29 cases with no packages, with the SHA-256 of each file | Received, each file matching its SHA-256 once two blank lines lost at part edges were put back. Built and run here on Windows 11 on 2026-10-31, .NET 10.0.14, each case twice in its own process, and every result held both times but one: a `FileSystemWatcher` handler's throw **ends the process** on Windows, `0xE0434352`, where on Linux it goes to the watcher's `Error` event and silences it, which a run here under WSL 2, Ubuntu 26.04.1 on .NET 10.0.13, reproduced. The catalogue's claims about the built-in analyzers held against a sample of this repository's own, built with `AnalysisMode=All`, and `TransformSafe` and `Debug.WriteLine` were read in the DynamicData 9.4.33 documentation and the 10.0.14 runtime. Landed in PR #114 as `docs/exception-resilience.md`, 27 entries, with the watcher's on both platforms; the Avalonia shapes stay in the gotchas and are linked. The probes are not kept here, so re-measuring needs them from that port again. The outcome went to that port the same day |
| 2026-11-01 | DiffView | `TemplatePartRule.ReadParts` to give an entry to a themed control whose code declares no `PART_` constant and looks none up, with its themes and their declared parts: measured over DiffView with `theme-audit parts`, 11 controls and no entry for `DiffStatusStrip`, whose theme declares 14 parts, or `DiffPaneHeader`, whose theme declares 6, while the reading's own `Skipped` said nothing of them. Its plan 00030 builds a parts reference from the reading, and reads those two themes itself until a release carries this | Accepted by the developer on 2026-11-06, and read here in the source: the reading's `Skipped` held only a type that did not load, code partly read, and a shared name with parts. Landed in PR #123, which also takes each markup folder's controls and skips in `PartsRunner.Read`, where a reading's controls had been taken to come from the assemblies alone |
| 2026-11-06 | DiffView | Three lessons for `docs/ai-drivable-ui.md`, measured driving an Avalonia 12.1.2 application on a real Windows desktop at 200%, started from SSH: a scheduled task can run a driven pass in the console session; `AppliedDPI` lags Settings, so it cannot say what scale a run is at; and the foreground lock can be effectively permanent, which matters to anything a task starts | Received, offered to place, reword or decline, with no reply asked for. All three landed in section 5 of the guide in PR #124 as DiffView measured them, its open causes kept open, not reproduced here, by the developer's decision of 2026-11-06, and the `drivable-ui` skill gains a sentence |
| 2026-11-10 | ClaudeForge | `BNCQ1009` and `BNCQ3001` to accept a catch-all whose filter excludes only fatal exceptions, at least `when (ex is not OutOfMemoryException)`: GitHub CodeQL's `cs/catch-of-all-exceptions` flags the unfiltered catch-alls these rules ask for, five on ClaudeForge, and accepts a filtered one, which the rules then reported again. It measured on .NET 10 that a stack overflow cannot be caught and an access violation ends the process with nothing running, so the difference a filter makes is `OutOfMemoryException`. Then a CodeQuality release, and its version sent back | Landed in PR #132: the forms `docs/codequality.md` lists, for those three fatal types; any other filter still reported. Released in CodeQuality `2026.4.1111` on 2026-11-11 |
| 2026-11-10 | DiffView | A rule that every input control written in markup carries a `ToolTip.Tip`, the owner's standing rule, which nothing in the family enforced: a review window shipped seven buttons with an automation name and id and no tooltip. Its interim guard, `TooltipCoverageTests`, reads every `.axaml`, takes 17 input controls by local name, and counts the attribute and property element spellings; it left open a tooltip a style gives, controls built in code, `MenuItem`, and the parts of a `ControlTheme` | Landed as `BNXQ1011`, PR #134: its 17 controls, both spellings and WPF's and MAUI's, a theme's parts counted, `MenuItem` left out, a style that may give the tooltip named in `Skipped`, code not read. DiffView swaps its test for the rule at the release that ships it: XamlQuality `2026.4.1111`, released 2026-11-11 |
| 2026-11-11 | Templates, relaying ida-dump-py's session | A lesson for `docs/ai-drivable-ui.md`: run a GUI application on a hidden Windows desktop, made with `CreateDesktopW` and given as `STARTUPINFOW.lpDesktop`, so no window reaches the developer's screen. Measured with IDA Pro 9.3, a Qt 6 application, on Windows 11 on 2026-11-11: no real input reaches it, so it is driven from inside; a native file dialog blocks invisibly, so every run is bounded; and the toolkit's offscreen platform comes first | Receipt sent the same day. Landed in PR #137 in section 5 of the guide as measured, not reproduced here, by the developer's decision of 2026-11-12, and the `drivable-ui` skill gains a sentence |
| 2026-11-11 | DiffView | Two findings from adopting CodeQuality `2026.4.1104`, no reply asked. `BNCQ1009` reported a handler written as a local function and subscribed to `Process.OutputDataReceived` and `ErrorDataReceived`, and not the same body as a lambda held in a `DataReceivedEventHandler` local and subscribed through it: the rule never read a local. And `BNCQ1001` reports nothing at DiffView's `c3beea1`, where the measurement before CodeQuality's first release recorded `DiffDocumentBuilder.Build` and `DiffSearch.Find` | The second was DiffView's own fix, `f99aa4d` of 2026-09-24, as the CodeQuality section's measurement row now says. The first, by the developer's decision of 2026-11-12, is fixed in PR #138 and released in CodeQuality `2026.4.1113` on 2026-11-13, by the developer's decision of that day. The outcome went to DiffView the same day |
| 2026-11-12 | AppServices | Every exception a `Dispatcher.Post` job throws is reported twice on Avalonia 12.1.3: to `Dispatcher.UnhandledException`, and at a later garbage collection, wrapped, as `TaskScheduler.UnobservedTaskException`, handled or not. The cause read in `DispatcherOperation.InvokeCore` at the tag; a doubled log and a late report inside an unrelated test measured in its own suite; and its fix, a handler that remembers what it handled and drops the second report, with a collection at the end of each test that throws on the dispatcher | Receipt sent the same day. Landed in PR #139 under *Dispatcher / exceptions* as measured or read, not reproduced here, by the developer's decision of 2026-11-12; the entry on `InvokeAsync` gains the line that a posted job's operation keeps its exception on a task too. The same day AppServices corrected where it measured, in an application's run hosting a headless session and not on a real platform, which the entry says, and sent three more findings on the dispatcher at 12.1.3, landed in the same pull request as measured or read, not reproduced here, by the developer's decision of 2026-11-12: a cancellation thrown from a posted job is reported as a failure twice, read and measured, in the same entry; a `Post` to a dispatcher that has shut down is dropped, read only, an entry of its own; and reading `Dispatcher.UIThread` off the UI thread before any dispatcher exists makes that thread the UI thread, read, its headless half measured, in the entry on a thread that asks for the UI thread |
| 2026-11-16 | AppKit | A headless test that blocks on `HeadlessUnitTestSession.Dispatch` can deadlock on Avalonia 12.1.3, depending on test order: read, `DispatchCore`'s `TaskCompletionSource` takes no options and is completed on the session's one consumer thread, so an awaiting test's continuation, and xUnit's start of the next test, can run inline there, where a next test that blocks on another dispatch waits on the only thread that could run it. Measured with xUnit v3 4.0.3 on Microsoft.Testing.Platform: CI hung on Linux and Windows, a worktree at the Windows runner's path reproduced CI's order, a stack dump showed the blocked test and no thread in the session's loop, and MTP's `--timeout` did not end it. Its rule, await the dispatch or return its task and never block on it, passed the same order | Received, and a receipt sent the same day. Landed in the gotchas under *Testing*, in the headless gotchas pull request of 2026-11-19: its source claims read at the 12.1.3 tag, unchanged at 12.1.6, and the mechanism measured here on 2026-11-19, each form twice on Windows 11 and under WSL 2 (plan 00010 step 2): after an awaited dispatch the rest ran on the session's consumer thread every time, and `GetAwaiter().GetResult()`, `Result`, `Wait()`, `Task.WaitAll`, `Task.WaitAny` and `ConfigureAwait(false).GetAwaiter().GetResult()` never returned within 5 s, `Wait(3 s)` returned false, and the same waits with no await first, or after `await Task.Yield()`, returned. Outcome sent 2026-11-19, its text approved by the developer. Its static part, a headless test that blocks on `Session.Dispatch`, the developer asked on 2026-11-16 to become a CodeQuality rule, `BNCQ3002`, by a plan that holds that rule alone, by the developer's decision of that day; it is plan 00010, since plan 00009 went to the testing family's direct test run, by the developer's decision of 2026-11-16, and the explicit-isolation rule's plan is 00012, renumbered from 00011 by the developer's decision of 2026-11-19, which gave 00011 to AppKit's IL reader |
| 2026-11-16 | Templates, forwarding ClaudeForge | Under the default per-test headless isolation, a background thread can claim `Dispatcher.UIThread` while the session rebuilds the application, and the next test fails with `VerifyAccess`: read in Avalonia 12.1.6, measured in a repro, 1,815 of 2,000 dispatches failing per test, 0 per assembly with a warm-up dispatch. With four checks offered, (a) to (d). Two corrections the same day: per-assembly isolation is no drop-in fix, since it gives every test in the process one `Application`, which broke three ordinary tests in ClaudeForge's assembly, and leaks queued dispatcher work between tests; the mechanism, check (c) and wording (d) stand | Received, and receipts sent the same day for it and for both corrections. ClaudeForge's verdict arrived that day: the headless tests in an assembly of their own, under `PerAssembly` with a warm-up, passed 4 runs of 4 on each of Windows, Linux and macOS CI. Landed in the headless gotchas pull request of 2026-11-19 as an entry of its own beside the one on a thread that asks for the UI thread, whose cause now reads the reset in the source. Read at 12.1.3, and measured here on 2026-11-19, each mode twice on Windows 11 and under WSL 2, 2,000 empty dispatches beside a thread looping `CheckAccess()`: `PerTest` 1,989 to 1,998 failed with the reported stack, `PerAssembly` with the reader first 2,000, `PerAssembly` after one dispatch 0, `PerTest` with no reader 0. The mixed-assembly failures and the CI runs are ClaudeForge's, on 12.1.6, not reproduced here. Outcome sent 2026-11-19, its text approved by the developer. Checks (a) and (b), an explicit isolation choice, become a rule by a plan of their own after that verdict, by the developer's decision of 2026-11-16 |
| 2026-11-16 | AppKit | One reference fetcher across the family: its `reference/fetch-reference.cs`, at AppKit `d3bdafe`, kept in `reference/` beside its manifest, its README and three MSBuild shields, in place of `scripts/fetch-reference.cs`; each source pinned at the commit its packages' nuspecs record, the tag a fallback. It measured Semi.Avalonia's tag `v12.1.0.1` at `5c1fa38`, one commit past the `dc75ad4` its 12.1.0.1 package records, and Avalonia 12.1.3's tag and nuspec agreeing at `8eeda4f` | Taken on by the developer on 2026-11-16, after PR #144. Landed in PR #145: the script and shields as AppKit has them, this repository's two sources in its manifest, `.gitignore` ignoring `/reference/*/`, and `scripts/fetch-reference.cs` and its manifest gone. Run here, both checkouts read `off-pin` from the old script's records, and `--update` fetched Avalonia at `8eeda4f` and Semi.Avalonia at `dc75ad4`. The Semi difference is recorded in `reference/README.md` and is no Avalonia gotcha, by the developer's decision of 2026-11-16 |
| 2026-11-19 | Templates | Its template's choice on the headless UI thread: every headless test in a project of its own under `PerAssembly`, with an assembly fixture that dispatches once and reads the UI thread's owner from a new thread, and a stress guard of 200 dispatches beside a background `CheckAccess` reader. `BNCQ1009` rejects an uncaught thread body, so both guard threads catch and hand the exception back, and the stress guard asserts its reader did not die early. Measured on Avalonia 12.1.6 on Windows: without `PerAssembly`, all 5 headless tests fail with the fixture's message. No reply asked | Received after PR #148 landed the entry; a receipt sent the same day. Its guard-thread point and its measurement were added to the UIThread entry in the follow-up pull request of 2026-11-19, as measured there, not reproduced here |
| 2026-11-19 | AppKit | An IL reader in AssemblyQuality, the developer's own words relayed from AppKit's session, "the most feature rich, rather than the lightest", on ICSharpCode.Decompiler 11.2.0.10050, for AppKit's `plans/00007` step 5: four sweeps over `AppServices.AvaloniaUI`, a foreign window, a colour or brush outside one type, a lookup outside one helper, and keys through `DialogColors.Brush`, each planted defect to turn red, with an empty `Skipped` and `Inspected` above zero, and the same sweeps over ScopedEditors for its `plans/00006` | Received, and a receipt sent the same day. Plan 00011, by the developer's decision of 2026-11-19, which moved the explicit-isolation rule's plan to 00012; approved that day after four reviews and merged as #151, with the helpers found by an attribute, `Bennewitz.Ninja.ResourceKeyAttribute`, which AppKit declares, and the compile-time analyzers left to plan 00013. Step 2 measured 2026-11-23. A note to AppKit on what the plan asks of it was sent 2026-11-24, its text approved by the developer, and AppKit recorded it for its `plans/00007` step 5 and its revision of `plans/00006` |
| 2026-11-23 | Templates | A CodeQuality rule for the one log-line form its `plans/00014` sets, approved by the developer on 2026-11-23: in the constant template of every `ILogger.Log*`, `LoggerMessage` and Serilog call, a tag `[Area.Kind]` first, both parts PascalCase, then only `key=value` pairs with camelCase keys, free text quoted; a hole carrying `:l`, and an enum passed to a hole, reported; on by default, at warning. One line per action and redaction stay with review. DiffView's `plans/00037` waits on the rule's id and release | Received, and a receipt sent the same day. It needs a plan of its own here, whose place in the queue is the developer's to decide; Plan 00014, approved 2026-11-23 and merged as #153, gives it `BNCQ1012`, and the id was sent 2026-11-24, its text approved by the developer. AppKit's count, revised 2026-11-24, is 24 templates outside the form, and its `plans/00009` suppresses both pass-through sites; the developer decided on 2026-11-24 that `AvaloniaDiagnostics`' `{EventLine}` is written in the form, as plan 00014's decision 8 has it, and that outcome waits on the developer's approval of its text. Templates is told the release once it is out |

### Rule backlog

Ids are permanent, and the README's rules table is generated from them, so an id is assigned only
when a rule is written.

The rules were written in the order decided on 2026-09-23, and all of them are now: `BNXQ1005`,
`BNXQ1006`, the peer check, `BNXQ1007`, the explicit `AutomationId`, `BNXQ1008`, the zero-size slot,
and `BNXQ1009`, the `ToString()` containers. One candidate came in since, the row that takes no
click where it draws nothing, from the gotchas entry on a row whose `Background` is null, and it is
written as `BNXQ1010`, by the developer's decision of 2026-09-28. Eight candidates are open:

- **A binding the XAML compiler does not check:** `{ReflectionBinding …}`, or a `{Binding}` that
  `x:CompileBindings="False"` makes one, put on the backlog by the developer on 2026-10-14, when the logging
  entries of `docs/avalonia-gotchas.md` arrived. A broken one shows only at run time, as a warning
  that `LogToTrace()`, the project template's default, writes nowhere on Linux: in the port's
  application, one planted on a window built green and only the run's log showed it. And the trim
  analyser never sees it, only ILLink does, as that document's entry on the analyser says. Whether a
  plain `{Binding}` compiles depends on the project's MSBuild default, which no markup scan reads;
  at 12.1.3 Avalonia's own `packages/Avalonia/Avalonia.props` sets
  `AvaloniaUseCompiledBindingsByDefault` to true where a project leaves it unset, so a plain one
  goes unchecked only where a project sets it to false. On the real code the comparison pins, no
  markup writes either form, and ClaudeForge, DiffView and ScopedEditors all set the property to
  true; every mention of `ReflectionBinding` there is in a comment. So the rule would guard against
  a regression, and report nothing on them today. No id until it is written.
- **A compiled binding through a nullable step, written without `?.`:** a path that steps through a
  property its type declares nullable, and a value-type target that takes no null, with no converter
  and no `TargetNullValue`, after such a step. Put on the backlog by the developer on 2026-10-14, from
  the last logging entry of `docs/avalonia-gotchas.md`: each logs a warning on every container where
  the step is null, 351 in one run of the port's application. A rule would read the path against the
  template's `x:DataType`, as `BNXQ1009` reads an item type, and each property's nullability from the
  scanned assemblies; where a project has no nullable annotations it can decide nothing. Not yet
  measured on real code. No id until it is written.
- **A style aimed into a template that a `TemplateBinding` outranks:** a style whose selector reaches a
  template part through `/template/` with no activator, in it or in a style it is nested in, as
  `StyleSelectors.IsConditional` reads one, setting a property the template gives that part by
  `TemplateBinding`. Put on the backlog by the developer on 2026-10-19, from the `TemplateBinding` entry
  of `docs/avalonia-gotchas.md`: such a style sets at `Style` priority, below the binding's `Template`,
  so its value never shows while the binding holds the part's. A rule would read the selector through
  `StyleSelectors` and the part in the control's theme through `ThemeReader`; where the template is not
  in the scan, as the framework's are not, it can decide nothing. On the real code the comparison pins,
  every style aimed into a template has a class or a pseudo-class but six of ScopedEditors', which set
  only `AutomationProperties.Name` on parts of the framework's templates, so the rule would report
  nothing there today. No id until it is written.
- **A view model that implements `IDataErrorInfo`:** a type the scanned assemblies define that
  implements `IDataErrorInfo`, which Avalonia never asks, where markup names it as an `x:DataType`. Put
  on the backlog by the developer on 2026-10-19, from the entry on `IDataErrorInfo` in
  `docs/avalonia-gotchas.md`. A model shared with a front end that does ask, as WPF does, would be
  reported too. On the real code the comparison pins, no type implements `IDataErrorInfo` or
  `INotifyDataErrorInfo`, so the rule would report nothing there today. No id until it is written.
- **A `FocusAdorner` setter in a style with an activator:** a style whose selector has an activator, in
  it or in a style it is nested in, as `StyleSelectors.IsConditional` reads one, setting `FocusAdorner`.
  Put on the backlog by the developer on 2026-10-19, from the entry on focus rings in
  `docs/avalonia-gotchas.md`: Avalonia builds the ring once, when keyboard focus arrives, so such a
  setter takes effect only the next time focus arrives, never while the control keeps it. A ring that a
  theme outside the scan varies, as Semi's `ToggleButton`'s, it cannot see. On the real code the
  comparison pins, no markup sets `FocusAdorner`, so the rule would report nothing there today. No id
  until it is written.
- **A resource looked up by name with no variant:** a call in a consumer's compiled code to
  `FindResource(IResourceHost, object)` or `TryFindResource(IResourceHost, object, out object)`, the
  overloads that pass a null variant, with a literal key, read from IL as `CompiledStrings` reads a
  lookup's, whose key the scanned markup defines only inside a theme dictionary. Put on the backlog by
  the developer on 2026-10-26, from issue #93 and the entry on a themed resource looked up with a null
  variant in `docs/avalonia-gotchas.md`: such a call finds nothing, or another variant's value, where
  the control draws its own. A key a theme outside the scan defines, as Semi's `FlyoutBackground`, it
  cannot see. On the real code the comparison pins, DiffView's source makes one such call, for a control
  theme defined outside any theme dictionary, so the rule would report nothing there today. No id until
  it is written.
- **A converter whose `ConvertBack` only throws, on a target two-way by default:** a binding that names
  a converter on a target property registered two-way by default, `TextBox.Text` among them, with no
  `Mode`, where the converter's `ConvertBack` does nothing but throw, read from its IL. Put on the
  backlog by the developer on 2026-10-31, from the entry on such a binding in
  `docs/avalonia-gotchas.md`: it writes the source type's default over the source at binding time and
  after every change, with only a Warning in Avalonia's log. A converter that converts some values back
  and throws on others it cannot see. Not yet measured on real code. No id until it is written.
- **A `FocusAdornerTemplate` whose root sets a `Margin`:** a focus ring's template whose root element
  sets `Margin`. Put on the backlog by the developer on 2026-10-26, from the entry on an adorner whose
  root has a `Margin` in `docs/avalonia-gotchas.md`: Avalonia applies the adorner's clip, worked out in
  the adorned element's coordinates, in the adorner's own, so such a ring is cut short by its margin on
  its right and bottom wherever a clipping ancestor's edge is near. A ring a theme outside the scan
  gives, as Semi's default one, it cannot see. Not yet measured on real code. No id until it is written.

**Not a rule, kept here:** the contrast check scoring a translucent stroke over both its grounds. A `Border`'s
stroke lies half over its own `Background` and half over what is beneath the border, as the entry on such a stroke in
`docs/avalonia-gotchas.md` says, so a pair whose foreground is a `BorderBrush` with an alpha below 255 is drawn as two
colours, and the audit, which composites it over the background alone, scores only the inner half. Put here by the
developer on 2026-11-12. Which ground lies beneath a border is decided where the border is laid out, not by its theme,
so a pair would have to name it.

**Also not a rule, kept here:** a `theme-audit` setting that lists only the unseen redefinitions read through aliases and
brushes, deferred by the developer on 2026-10-22 until a report needs it, by plan 00007. It may filter only what the
report lists, and say how many it leaves out, never what the check reads: setting a read aside can make a finding of a
key another read sees.

**Not a fit:** the guide's rule 10, keeping popups in the window's tree. Avalonia's `OverlayPopups` is
set in C# at startup, where no markup scan can see it. That check belongs in the application, as a
startup assertion or a headless test. Nor is the `LogToTrace()` call the logging entries warn
against, by the developer's decision of 2026-10-14: it is C# start-up code too, and the family's
template already leaves it out.

### Questions for the developer

None open.

### Follow-ups

- **The exception catalogue from one of our WPF-to-Avalonia ports, decided by the developer on 2026-10-31.** The shapes that are not
  Avalonia's, its Rx, DynamicData, task, timer, thread, event, teardown and `Lazy` shapes, are in a
  document of their own here, `docs/exception-resilience.md`, each claim measured again here on Windows
  before it landed, in PR #114. Its scanner is not taken: plan 00005, approved on 2026-11-01, turns
  the catalogue into CodeQuality rules, with the scanner's self-test corpus of 70 expected sites and 22
  safe forms, and that port's own sites, as fixtures written here; plan 00005 is complete, under
  *CodeQuality* below. `Microsoft.VisualStudio.Threading.Analyzers` was measured on DiffView on
  2026-11-02, the "VSTHRD on DiffView" row under *CodeQuality*: no defect among its findings. The
  developer decided on 2026-11-03 to adopt it family-wide with only `VSTHRD101` and `VSTHRD110` on,
  which Templates `2026.4.1103` carries and this repository has synced to. The `ConvertBack` candidate went on
  the rule backlog above.

- **A theme audit check for a key a theme's controls read only through its own aliases, planned (the
  developer, 2026-10-20).** A consumer that redefines such a key, as Semi's `SemiColorPrimary`, which
  Semi's control themes read only through `StaticResource` aliases in its own dictionaries, changes
  nothing they draw, as the entry on Semi's palette keys says the port measured. The check was held on
  2026-10-19 until someone read why; the port read it on 2026-10-20, in the requests above, and the
  developer asked for the check that day, the audit's model corrected first. PR #130 corrects it, by plan
  00006; plan 00007, the check, approved on 2026-10-22, is PR #131: its reading and check, its step 2, and its step 3,
  which draws each verdict and each kind of read headless, Semi's `Button` among them. Both released in
  `2026.4.1111`.
- **A `MergeResourceInclude` whose file has a theme dictionary keyed as one the including dictionary includes replaces
  it (step 3 of plan 00007's measurement).** Headless on Avalonia 12.1.3, an entry whose `ThemeDictionaries` held
  `<ResourceInclude x:Key="Dark" Source="Dark.axaml" />` and whose merged `MergeResourceInclude` file held an inline
  `<ResourceDictionary x:Key="Dark">` kept only the merged file's Dark dictionary: none of `Dark.axaml`'s keys was
  found. Fluent merges several such files whose inline theme dictionaries share their keys, and they all hold, so the
  replacement seems to be of an include by an inline dictionary, not measured further. Plan 00006's model searches both,
  so the audit would give a key of the replaced include a colour Avalonia never draws. Fluent, Simple and Semi never
  mix the two in one dictionary, so no report the family commits moves; a fix is a correction of plan 00006's model.
- **A `<DynamicResource>` alias element that a control takes by `{DynamicResource}` draws nothing (PR
  #130's measurement).** Headless on Avalonia 12.1.3, a theme dictionary's
  `<DynamicResource x:Key="B" ResourceKey="T" />`, painted with `{DynamicResource B}`, throws an
  `InvalidCastException`: the dictionary holds the `DynamicResourceExtension` itself, which is no brush.
  Taken by `{StaticResource B}` it draws `T`, as `DrawnColourTests` holds. The audit resolves both as an
  alias, since a key's definition does not say how it is taken; a consumer's reference does, so plan
  00007's reading of references could tell them apart.
- **The brand language takes no colour a caller supplies (PR #85's review).** A block replaces a
  placeholder whole, so a figure that composites a colour only a consumer's own code knows, as the
  port's search colours, is worked out by that consumer's program, with its own arithmetic, and its
  claim checked there. Letting a caller pass named colours that the language composites as it does a
  token's would keep every figure the library's. It changes the API decision 11 fixed, so it waits for
  the developer, and for a second consumer that needs it.
- **The runtime check of the themes (the developer's decisions of 2026-10-13 and 2026-10-14)**
  shipped in `2026.4.1014`, from PR #75, by [plan 00003](plans/00003-runtime-theme-check.md): the
  publishing policy was read on nuget.org before the tag, permitting new packages, and the
  repository's description names the package, by the plan's decision 9. DiffView's test is its first
  adopter, and DiffView was told on 2026-10-14 that the release carries the package.
- **Two gaps in the runtime check's tests, found by PR #75's review.** Plan 00003's step 2 asks the
  test project to show an unmapped merge path in `Skipped`; only the library's
  `RuntimeCheckPlanTests.A_theme_whose_merged_dictionary_the_map_does_not_name_is_not_checked_without_it`
  does. And no test fails when a shipped file names a type by its namespace without `global::`, as
  `System.Uri`: that compiles in the test project, whose aliases are named after the Avalonia and
  library types the files use. A `SourceOnlyPackageTests` fact could read the files for one.
- **The audit's public `ThemeVariant` shares its name with Avalonia's.** A file that imports both
  `Avalonia.Styling` and `Bennewitz.Ninja.XamlQuality.ThemeAudit` fails with CS0104 until it aliases
  one, as the README's example and DiffView's files do. The shipped source files name every type from
  `global::`. Renaming the audit's type would break its consumers.
- **`BNXQ1003` reads a theme's target by its name alone (the developer, 2026-10-14).** Since PR #79 a
  name that two scanned types carry is skipped, so where a `using:` or `clr-namespace:` prefix names
  the one with parts, a check the prefix would allow is lost: the pull request's review measured a
  true finding lost so, for a control that shares its name with a type with no parts. Reading the
  prefix, as `ThemeReader.TargetTypeOf` does for `BNXQ1005`, `BNXQ1009` and `BNXQ1010`, would check
  the type it names and skip only a theme whose prefix names no one type. The reading would then hold
  two types of one name, so the parts report's names would change. A change of its own, kept for
  later by the developer's decision of that day.
- **Avalonia-specific rules in a package of their own, for later (the developer, 2026-10-13).** The
  library is compile-time and references no framework, and `BNXQ1005`, `BNXQ1009` and `BNXQ1010`
  already read Avalonia's behaviour by reflection. Moving them, with `ThemeReader`, `ResourceScope`,
  `RowPeers`, `StyleSelectors` and the Avalonia parts of `ElementTypes`, into a package of their own
  would make the core framework-neutral in fact and pair the package with `docs/avalonia-gotchas.md`.
  It changes no dependency and keeps every rule id, but type forwarding cannot soften it, since the
  two packages would reference each other, so a consumer of those rules adds the package. With or
  without it, an explicit check that a scan is Avalonia's would let those rules say so in `Skipped`
  rather than match nothing. Revisit when a second framework's nuances arrive.
- **The probe's run over every case flags 18 `BNXQ1009` verdicts nobody has read.** `tools/probe.cs`
  judges all three rules on every case it runs, and over all 603 it flags 23 verdicts. Three are
  `U6F1`, `U6F2` and `U6F3`, which judge 16 flagged and declared. Two are limits `BNXQ1010` declares:
  `K11c`, whose rows' code sets their `Background` through `SetValue`, which
  `ElementTypes.CodeSetsPaint` does not see, and `S11b`, whose style asks for a property the rows'
  theme binds to each item and so is taken to paint only some rows. The other 18 are `BNXQ1009` clean
  verdicts on cases written to measure `BNXQ1010`, where every row's peer reports its item's
  `ToString()`: `J08`, `J08b`, `J08c`, `K24`, `Q05`, `Q05c`, `Q15`, `Q16`, `R03`, `R04`, `R20`,
  `R20b`, `V908`, `V911`, `V911b`, `X9`, `Z17` and `Z32`. None has been read against the rule;
  reading them is a change of its own, since a fix can add findings.
- **One case reads differently now that every case shares one assembly.** `W03b`, a row template
  whose `ContentControl` holds a `Border` bound to the `ListBoxItem` ancestor in the logical tree, was
  a `BNXQ1010` finding when the scan was given judge 12's build of the probe, and is a skip in
  `tools/probe`'s. That build also holds judge 16's `FpProbe6.Alt16.ContentControl`, a `ListBoxItem`,
  and `ElementTypes.Resolve` takes a type the scan is given over the framework's of the same local
  name, so the rule counts the `ContentControl` as a nearer `ListBoxItem` and cannot say the binding
  reaches the row. Elements are matched by local name by design, so wherever the scan is given an
  input element named after a framework one, every element markup writes by that name is read as the
  consumer's type; here it costs a skip.
- **BNXQ1004 reads five things more narrowly than the framework, and `BNXQ1008` shares three of
  them through `GridLayout`.** The first four were found while building PR #20, and each is left
  for a change of its own, because each can add findings or skips on a consumer's markup:
  - The shorthand is split at commas only, in both rules. Avalonia's parser also splits at
    whitespace (`GridLength.ParseLengths`, read from source), so `ColumnDefinitions="Auto 16 *"`
    reads as one star column.
  - `Grid.Row`, `Grid.Column` and the spans are read only as attributes, in both rules, so one
    written as a property element is placed in row or column 0.
  - A control that sets a literal `MinWidth` and a larger literal `Width` is measured by its
    `MinWidth`. The framework arranges it at the larger, unless a `MaxWidth` caps it.
  - WPF's unit suffixes (`px`, `in`, `cm`, `pt`) are not read, in both rules, so a size written with
    one is named in `Skipped`.
  - A definition's own `MinHeight` and `MaxHeight`, and a star of weight 0, are read by `BNXQ1008`
    and not by `BNXQ1004`. Measured on Avalonia 12.1.3 in PR #35, a row of `Height="0"
    MinHeight="20"` is arranged 20 tall, a `MaxHeight` of 0 empties any row, and `0*` gets nothing.
    `GridLayout.SlotBounds` already carries them, so the change is `BNXQ1004` measuring against
    them too.
- **Trimming is being made a family policy, with Templates.** The developer's model, 2026-09-28: a
  repository that trims sets `IsTrimmable` and `EnableTrimAnalyzer`; a family library it depends on
  must then be trimmable too; with neither property set, nothing is enforced. Today the shared
  conventions checker, copied here from Templates and never edited here, notes every library
  without them. XamlQuality is used only from test projects, so it owes nothing under the policy,
  and the note goes when the agreed change copies back. The developer confirmed the split on
  2026-09-29. Templates enforces a repository's own libraries in the checker. This repository's
  session writes the other side: when a repository trims, every `Bennewitz.Ninja.*` package in its
  restore graph, transitive ones included, carries `IsTrimmable` metadata, read from
  `project.assets.json` without loading anything. Templates lands it in a plan of its own after its
  `00006`, which also decides whether `"trimming": "required"` stays.
- **Under Semi, a tab is named by its item's `ToString()` whatever its header shows.** Semi's
  `TabItem` template names its header presenter `PART_HeaderPresenter`, where Fluent's and Simple's
  name it `PART_ContentPresenter`, and a tab's peer reads the presenter registered under the second
  name, so under Semi it finds none and falls to `Content.ToString()`. Measured with the probe's tab
  cases: `TB01` to `TB03`, whose item template's root is a `TextBlock` with the label, were named
  `Alpha` under Fluent and Simple and `FpProbe6.Row`, the type's full name, under Semi; `TB04` and
  `TB05`, whose tab theme's template names a `PART_ContentPresenter`, were named `Alpha` under all
  three. `BNXQ1009` takes such tabs as named, so it misses them under Semi, and the probe flags
  nothing, since it judges a clean verdict missed only where every theme shows the defect. Since PR
  #81 the same holds of a tab with no item template that a data template chosen by type presents:
  `ZJ04`, measured alike. Deciding it means reading which theme the application loads; a change of
  its own.
- **A row's own template whose presenter is not `PART_ContentPresenter` names the rows by
  `Content.ToString()`, which `BNXQ1009` does not read.** A style or a row theme that gives the rows
  such a template leaves their peer no presenter to read: `ZJ05`, a list whose rows a style gives a
  template with a presenter named `Other`, and a data template for the items with a `TextBlock` root,
  measured `FpProbe6.Row` under all three themes, where the rule says clean. The probe flags it as
  `MISSED`. It holds of an item template as much, and so predates PR #81. Reading the presenter's name
  in the rows' template is a change of its own.
- **With an item template, what a style or a row theme gives the rows to present is not read.**
  `PrepareContainerForItemOverride` sets a row's content, a tab's header and the template for either
  only where nothing has set them, by `SetIfUnset` at 12.1.3, so a style or a theme that sets one keeps
  it. Since PR #81, `BNXQ1009` names such a list in `Skipped` where it has no item template, as its
  review measured four of them naming the rows (`ZJ06` to `ZJ09`); with an item template it still
  reads the template's root. Asking there too would turn verdicts into skips, a change of its own.
- **`BNXQ1009` reads a type only through a `using:` or `clr-namespace:` prefix, or the XAML
  language's own.** A type written through a URI an assembly's `XmlnsDefinition` maps, or through the
  default namespace a consumer maps its own into, is a type the scan may hold. Since PR #81 the new
  readings say so, and a data template for such a type is taken to match where it may be the items'
  own (`ZJ01`, `ZJ01x`, `ZJ02`, now skipped); the item template's `x:DataType` keeps the older
  reason, "is not a type in the scanned assemblies". Reading `XmlnsDefinition` would decide them, a
  change of its own.
- **`BNXQ1009` does not follow a `DataContext` set on the way to an `ItemsSource` binding.** The XAML
  compiler types a compiled `DataContext` binding by what its path reads, by
  `AvaloniaXamlIlDataContextTypeTransformer` at 12.1.3, and the bindings beneath it read against that
  type; the rule names such a list in `Skipped`. Following it would decide more lists, and can add
  findings, so it is a change of its own.
- **The probe's bound pass realises no combo row.** It does not open a `ComboBox`'s drop-down, so
  under every theme a combo case's rows are not realised, and `BNXQ1009`'s verdict on one agrees for
  want of a measurement: measured over `CB01` to `CB05`. Opening the drop-down, as the facts pass
  does, would measure them; a change of its own, since it moves what the pass measures for every combo
  case.
- **`BNXQ1009` checks Avalonia's rows only.** WPF's `ListBoxItem` is named through a different peer,
  whose fallback was not measured. Measuring it is what would let the rule check WPF markup.
- **`BNXQ1010` reads `:disabled` as a state rows may start in wherever markup in the scan disables
  anything**, any `IsEnabled` but `True` or any `Command`, by the developer's decision of 2026-10-08.
  It is sound, and its cost was measured: given a `^:disabled` background in its row theme, as
  Fluent's combo and tab rows carry one, the WPF-to-Avalonia port's real finding becomes a skip, and
  the markup that makes `:disabled` a start state there is a `KeyBinding`'s `Command`, which disables
  nothing.
  Two narrower readings were set aside, each a change of its own since each can add findings:
  - Count a `Command` only on a control whose type overrides `IsEnabledCore`, as `Button`,
    `SplitButton` and `MenuItem` do at 12.1.3, or a consumer's own does, and on a type that does not
    resolve. Still sound, and a skip would no longer cite a `KeyBinding`; it recovers no finding on
    the port or ClaudeForge, since both bind `IsEnabled` on buttons and panels.
  - Read `IsEnabled` and `Command` only where markup can be a visual ancestor of the rows or of a part:
    the list and its ancestors, its items panel and template, the rows' template, where its view is
    used by its `x:Class`, and setters anywhere, falling back to anywhere where a view's placement
    cannot be traced. It recovers the port's finding under the plant; a view shown through a data
    template or a view locator is its weak spot.
- **`BNXQ1010` matches a style's property selector by the property's name, whatever owner it is
  written after.** So `[(p|Paint.Shade)=True]` is taken for a property the rows' theme binds to each
  item wherever the theme binds any `Shade`, the row's own or an attached one. Found while fixing the
  owner reading for the rule's other spellings, and declared in its remarks; reading the selector's
  owner as the setters' is read, by the property its field holds, is a change of its own, since it can
  add findings.
- **`BNXQ1010` takes four readings as true without proving them**, each named in its remarks: a
  `:selected` style picks only some rows, which a list of one row, or one that selects every row,
  makes every row; a type's own markup, or its base's, applies, which it does only where its
  constructor, or its base's, calls `InitializeComponent`; a `RelativeSource` with an `AncestorLevel` or `Tree=Logical` reads as a
  reflection binding does, where a compiled binding, the default where a project says nothing else,
  takes the nearest ancestor whatever the level and drops a search of the logical tree, by
  `AvaloniaXamlIlBindingPathParser` at 12.1.3; and rows no markup in the scan disables or selects
  start enabled and unselected. Each could be read or measured in a change of its own.
- **`BNXQ1005`, `BNXQ1009` and `BNXQ1010` read a design-time attribute as the element's own.** A
  `d:Background` in a view whose root sets `mc:Ignorable="d"` is read as `Background`, although the
  XAML compiler ignores it, so beside the element's own `Background` the rules may take the
  design-time value. Found while checking the spellings, where the markup did not load in the
  run-time loader; reading `mc:Ignorable` and skipping what it names is a change of its own, since it
  can add findings.
- **A theme `BasedOn` one the framework does not apply to the control is read with that base.**
  `ThemeReader.Applied` checks the theme it finds, where the framework checks each link of a `BasedOn`
  chain on its own, by `StyledElement.ApplyControlTheme` and `ControlTheme.TryAttach` at 12.1.3:
  measured under Fluent, Simple and Semi, a list theme `BasedOn` one for another list type that set
  `Focusable` to `True`, and row themes `BasedOn` ones for another row type that cleared the rows'
  `Background` or named them, took nothing from those bases, while `BNXQ1005`, `BNXQ1010` and `BNXQ1009`
  read them. Applying the check to each link is a change of its own.
- **Where a theme does not apply, the rules skip rather than read what applies instead.** In the last
  review's cases the last fixes turned thirteen true verdicts into skips this way, and in the review
  before, a true `BNXQ1005` finding. Reading the theme the framework falls back to would recover them,
  in a change of its own.
- **A control whose `StyleKeyOverride` returns its key other than by a `typeof` is keyed by its own
  type outside `ThemeReader.Applied`.** `Applied` names such a control's themes in `Skipped`, but the
  type selectors `BNXQ1009` and `BNXQ1010` match, and the implicit theme `BNXQ1005` looks up for an item
  written as its own container, still take the control's own type for its key.
- **An owner with no prefix that only a consumer's type in the control's chain is named after is read
  as that type**, although the XAML compiler takes the framework's type of the name, outside the
  chain. It decides wrongly only where the consumer's type hides the property's field with another.
  Not measured; found while fixing the case where both types are in the chain.

## AssemblyQuality

Pull request numbers in this section are AssemblyQuality's own, in
JanusMael/Bennewitz.Ninja.AssemblyQuality.

### Status

| | |
|---|---|
| Published | `2026.4.1031`, released 2026-10-31 on the maintainer's word: tag `assemblyquality/v2026.4.1031` on `04dcb06`, release run `41211729501`, which pushed `Bennewitz.Ninja.AssemblyQuality` alone, and the flat container lists it. It carries #110: **a scan of an assembly whose collectible load context is unloading throws** `InvalidOperationException`, naming the context, where it recorded the reference as one that would not load and compared against nothing (#109). A collectible context nothing holds starts to unload at the next collection; measured on .NET 10, `GetLoadContext` still answers it and every load through it fails with HRESULT `0x80131509`. `BNAQ1004` (through `AssemblyScanContext.ReferencedNamespaces`) and `BNAQ1006` both resolve through it; each has a test that failed against the previous library. `docs/assemblyquality.md`'s BNAQ1006 example had the same unheld collectible context and holds a non-collectible one now. Before it, `2026.4.1029`, released 2026-10-29 from this repository, the family's first here: tag `assemblyquality/v2026.4.1029` on `3092aee`, release run `40939268891`. Verified from the feed: the flat container lists it, the served nuspec names Inspections at `3092aee`, its DLL is byte-identical to the GitHub release asset's, and a project restoring from nuget.org alone gets it and finds all six rules. It carries #17, Templates' solution-wide friend grants (`plans/00006`): the DLL grants its internals to this repository's sixteen assemblies, matched by name, and every fixture opts out; and the move: the package names Inspections and packs `docs/assemblyquality.md` as its README. No rule, dependency or public API changed. The release's notes were written by hand, the generated ones listing XamlQuality's pull requests. The previous release, `2026.3.929`, added `BNAQ1005` (#12) and `BNAQ1006` (#13), by [plan 00001](plans/assemblyquality/00001-friend-grant-rules.md), now complete |
| Not yet released | `BNAQ1010` and `AssemblyFinding.Source`, by plans/00011 step 3, and the package's first dependency, ICSharpCode.Decompiler 11.2.0.10050. Effect on a consumer: the package brings the decompiler, with System.Collections.Immutable and System.Reflection.Metadata 9.0.0, which .NET 10 ships; `AssemblyFinding.ToString` names the file and line where a finding has one; no other rule's findings change |
| Plan 00011 | [An IL reader, and `BNAQ1010` and `BNAQ1011` on it](plans/00011-il-reader.md), approved 2026-11-19 and merged as #151 (in Inspections). Step 2, the measurement, done 2026-11-23: no decision refuted, recorded below. Step 3, the reader and `BNAQ1010`, done 2026-11-24, measured below. Next, step 4: `BNAQ1011` and `BNAQ1010`'s option that allows a use by its key |
| Next release | On the maintainer's word, from an `assemblyquality/vYYYY.Q.MMDD` tag. One release per calendar day, three-part `YYYY.Q.MMDD`: a fourth part is AutoVersioning's `HHmm` build stamp, not a patch counter |

### Next

1. **A later Templates plan** sets the provider-side `BNAQ1006` convention: which repositories declare
   their published consumers, and where the release check lives. The wiring it builds on is in
   `docs/assemblyquality.md`'s `BNAQ1006` section, and plan 00001's step 2 findings below. Nothing to
   do here until it exists.

### Friend grants across the family

Decided 2026-09-28, with Templates' `plans/00006`.

| Decision | Choice |
|---|---|
| Split | Templates owns the policy; AssemblyQuality provides configurable checks, the `BNAQ1003` / `LayeringTests.Tiers` shape |
| Within a solution | Every project grants to every other, test projects included; grants between product projects are encouraged. `internal` then means solution-internal, so a member no other assembly may reach is `private` |
| Across repositories | Internals may be granted to the maintainer's other repositories wherever that reduces friction, declared in the External file |
| Signing | None. A grant is matched by assembly name, and on modern .NET public signing satisfies a keyed grant without the private key, so neither is a security boundary. Grants name only assemblies the family ships, and public consumers get none |
| Name collisions | Accepted and documented (2026-09-28). Family assembly names are unprefixed, so a public assembly that happens to share one would receive its grants; names such as `AppServices.Tests` make that unlikely |
| Version skew | An internal another repository uses is an informal promise: its owner does not change it without releasing the consumer. **`BNAQ1006` runs at the provider's release** (revised 2026-09-28), loading the already-published consumers beside the new build; in the consumer's own tests a removed internal is a compile error first, so the rule could almost never fail there |
| `BNAQ1005`'s allowed names | The solution's own assemblies plus the External file's names, supplied by the repository's test. No family registry for now |
| Existing repositories | Each moves over when it next syncs the Templates conventions. AppServices, FileServer and ScopedEditors document "grants to tests only" and rewrite those rules then |
| Order | Plans, then the Templates convention, then `BNAQ1005`, then `BNAQ1006` |

### Plan 00001's step 2 findings, for the provider-side convention

Measured with a throwaway provider and consumer in the scratchpad, not in this repository. The consumer
was compiled against provider v1 and loaded beside each variant in its own `AssemblyLoadContext`, and its
references were resolved and then **called**, so the runtime's verdict was the ground truth:

| Provider variant | `Module.ResolveMember` / `ResolveType` | Calling the consumer |
|---|---|---|
| Unchanged | every reference resolves; internals report as `internal` | runs |
| A member removed | `MissingMethodException` on that member only | the same `MissingMethodException` |
| A type removed | `TypeLoadException` on the type **and on every member reference through it** | the same `TypeLoadException` |
| The grant withdrawn | **every reference resolves**: resolution does not check access | `MethodAccessException` |

- **The resolver fails exactly as the runtime does**, so no signature is decoded by hand.
- **A removed type is one finding, not one per member.** Every member reference under it throws the
  same `TypeLoadException`; report the type once and skip its members.
- **A withdrawn grant is found by the rule itself**: a reference that resolves to a non-public member
  of an assembly whose `InternalsVisibleTo` no longer names the scanned assembly.
- **The shared framework** is recognised by location under the runtime's shared-frameworks folder,
  the parent of `RuntimeEnvironment.GetRuntimeDirectory()`, so ASP.NET Core and WindowsDesktop count
  as framework too, not only `Microsoft.NETCore.App`.
- **The provider-side wiring works.** A provider's test project references the provider's source and
  fetches each published consumer with `<PackageDownload Include="…" Version="[x.y.z]" />`, which
  downloads without joining the restore graph: its output held exactly one provider, the source build
  at `2.0.0.0`, with the consumer's own packaged dependency on `1.0.0` absent. A target copies the
  consumer's `lib/<tfm>/*.dll` from the packages folder, joining the path with `Path.Combine`, since
  `NuGetPackageRoot` has no trailing slash when `RestorePackagesPath` is overridden. Loaded beside the
  source build, the unchanged provider passed, the removed member failed, and the withdrawn grant
  failed.

### Plan 00011 step 2, measured

Measured 2026-11-23 in the scratchpad, by a file-based app on ICSharpCode.Decompiler 11.2.0.10050, over
`AppServices.AvaloniaUI` and `ScopedEditors.AvaloniaUI` built from AppKit at `a0f43fe`, later than `3d42b4a`,
with `Bennewitz.Ninja.ResourceKeyAttribute` declared in each, `DialogColors.Brush` marked, and `BrushHelper`'s three
methods marked with a scratch class of the six keys its literals spell. Each was built in Debug, with a portable PDB
beside the DLL, and in Release, with an embedded one. A fixture library with a stand-in `Avalonia.Media.Color`
measured the constructions neither assembly makes. Every reference resolved, and no body failed to read or transform.

| Question | Answer |
|---|---|
| Which ILAst | The transformed one. It credits each lambda, captured lambda, local function, and async and iterator body to the method the source wrote, with its line; in Debug the raw tree had no sequence point for a struct construction into a local and for `default`, where the transformed tree has the line |
| A struct construction | `newobj`, wherever it is stored, a local, a field, an array element or a static field, raw and transformed, Debug and Release. No `.ctor` called on an address appeared |
| `initobj` | `default` and `new Color()` are `stobj(ldloca, default.value Color)` raw and `default.value Color` transformed |
| A base-constructor chain | `call .ctor(ldloc this)`, at IL offset 1, or 8 after a field initializer. All eight of `AppServices.AvaloniaUI`'s window classes chain to `Window`'s |
| Generated bodies | Reached through their source method: lambdas, display classes' lambdas, local functions, async and iterator state machines. Not reached: their plumbing, the constructors, `<>c`'s static constructor, `SetStateMachine`, an iterator's enumerator members, and the `<>z__ReadOnlyArray` type a collection expression makes; `<PrivateImplementationDetails>::ComputeStringHash`; compiled XAML; and the Regex source generator's `file` types, named `<RegexGenerator_g>F…__Utilities` and the like |
| Compiled XAML | `!XamlIlPopulate`, `XamlClosure_N::Build_N`, `!AvaloniaResources+XamlClosure_N::Build_N`, `XamlDynamicSetters::<>XamlDynamicSetter_N` and `!XamlLoader::TryLoad`. Its sequence points name the markup file and line, `EditorColors.axaml:58`, though that file has no `x:Class`. A hex colour is `Color.FromUInt32` and `new SolidColorBrush`; `{DynamicResource}` is `new DynamicResourceExtension` |
| Sequence points | A file and line for every use in both configurations, C# and markup |
| Case 4's paths | The ternary of tuples in `AppendCategoryHeader` reaches the IL, Debug and Release alike, as two stores of a constant to the local `key`, with no tuple. `PaintedWhen`'s `fill` and `GlyphButton`'s `colour` are their parameters. A static field read is `ldobj(ldsflda F)` |
| Lookups | `BrushHelper`'s `TryGetResource` calls bind to `Avalonia.Application::TryGetResource`, the class's own. `ScopeToBrushConverter.Convert` calls `IResourceNode::TryGetResource`, a lookup outside `BrushHelper` that AppKit's list of negatives does not name |
| The attribute | Read from each marked parameter by its full name, its argument a resolved type |
| Time | One assembly, type system included: `AppServices.AvaloniaUI` 1.2 s in Debug and 0.7 s in Release, `ScopedEditors.AvaloniaUI` 0.9 s and 0.7 s |

Counts, the same in Debug and Release:

| Case | `AppServices.AvaloniaUI` | `ScopedEditors.AvaloniaUI` |
|---|---|---|
| 1, windows | 8 chains to `Window`, 9 constructions of its own windows | none |
| 2, colours | 3, all in `DialogColors`: `Color.Parse`, `new ImmutableSolidColorBrush`, `Brushes.Transparent` | 57: 31 in C#, `JsonHighlightBlock` 16, `BrushHelper` 10, `EnvVarNameToBrushConverter` 2, `ScopeToBrushConverter` 2 and `TipCell` 1, and 26 in compiled XAML |
| 3, lookups | 1, `new DynamicResourceExtension` in `DialogColors.Brush` | 12: 4 in C#, 3 in `BrushHelper` and 1 in `ScopeToBrushConverter`, and 8 `{DynamicResource}` in compiled XAML |
| 4, keys | 11 calls to `Brush`: 8 constants, 3 through a local or a parameter | 8 calls: 6 literals, 2 `KeyFor(severity)` |

### Where step 2 departs from plan 00011

- **The Regex source generator's `file` types are generated code the plan did not name.** Decision 7 reads
  `<PrivateImplementationDetails>` as methods of its own and sends to `Skipped` only a generated name the reader
  cannot take apart. A `file` type's name, `<File>F<hash>__Name`, is a form the reader takes apart, so step 3 reads
  its methods as their own, with a fixture of its own; otherwise `ScopedEditors.AvaloniaUI` would skip on clean code.
- **Compiled XAML is credited to its file through its sequence points**, which name the `.axaml` file whatever its
  `x:Class`. Without symbols, only its type and method names are left.
- **No construction took decision 5's second form, a `.ctor` called on an address,** in either configuration; it
  stays in the rule and its tests, since another compiler may emit it.
- **`{DynamicResource}` in markup is a lookup**, `new DynamicResourceExtension` in compiled XAML. A consumer that
  confines case 3 to one helper must also allow its markup, by file as decision 7 says; step 3 settles, with the
  constructor's signature, whether one place may also be all compiled XAML.
- **Two of ScopedEditors' negatives are not in AppKit's list**: `ScopeToBrushConverter`'s direct `TryGetResource`, a
  lookup outside `BrushHelper`, and `TipCell`'s `Brushes.Transparent`, read in its static constructor.

### Plan 00011 step 3, measured

Measured 2026-11-24 in the scratchpad, with `BNAQ1010` as its pull request builds it, over AppKit at `a0f43fe` with the
key attribute declared and the helpers marked as in step 2, each assembly built in Debug and in Release with its
packages copied beside it, as a test project's output holds them. AppKit's cases 1 to 3 were configured as it names
them: constructing `Window`, with its subtypes, but for the types the scan declares; constructing `Color`, and `IBrush`
with its subtypes, `Color`'s parse and factory methods, `Brush.Parse`, and every member of `Colors`, `Brushes` and
`KnownColors`, each allowed in `DialogColors`; and constructing `DynamicResourceExtension`, `ResourceNodeExtensions`'
`GetResourceObservable`, `FindResource` and `TryFindResource`, and `IResourceNode.TryGetResource` with its subtypes,
each allowed in `DialogColors::Brush`.

| | Debug | Release |
|---|---|---|
| `AppServices.AvaloniaUI` | 376 bodies read in 1.8 s, no finding, nothing skipped | 359 bodies in 1.9 s, no finding, nothing skipped |
| Its positive controls, in the reading | The eight window classes' chains to `Window`; `DialogColors`' `Color.Parse`, `new ImmutableSolidColorBrush`, `Brushes.Transparent` and `new DynamicResourceExtension`; each with its file and line | The same |
| Five defects planted in a scratch copy: a foreign `Window`, a `Color.Parse` and a colour made in a lambda outside `DialogColors`, `Brushes.Red`, and an `Application.TryGetResource` outside `Brush` | Each reported, with its file and line, nothing skipped | The same |
| `ScopedEditors.AvaloniaUI`, colours allowed nowhere and lookups in `BrushHelper` | 476 bodies, 66 findings, nothing skipped | 473 bodies, 66 findings, nothing skipped |

ScopedEditors' 66 findings: `JsonHighlightBlock`'s eight literal brushes, each a `Color.Parse` and a
`new SolidColorBrush`; `BrushHelper`'s hex fallback, four `Color.Parse`, five brushes and a `Color.FromArgb`;
`EnvVarNameToBrushConverter`'s brush and `ScopeToBrushConverter`'s, two uses each; `TipCell`'s `Brushes.Transparent`;
`ScopeToBrushConverter`'s own `TryGetResource`; and in compiled XAML, `EditorColors.axaml`'s twelve brushes, each a
`Color.FromUInt32` and a `new SolidColorBrush`, and `PropertyEditorWrapper.axaml`'s two brushes and eight
`{DynamicResource}`.

The mutation set `tests/AssemblyQuality.Tests/Mutations/il.json`, run 2026-11-24 with `--swap
src/AssemblyQuality/AssemblyQuality.csproj`: 21 mutations, every one as declared, each of the 14 declared tests killed
by one, in about 2 minutes.

### Where step 3 departs from plan 00011

- **Ancestry is read from metadata, not from the decompiler's type system.** The type system loads the scanned
  assembly's own references and follows type forwarders, but not a reference one of them makes. Measured: every
  Avalonia control in `AppServices.AvaloniaUI` implements `INotifyPropertyChanged`, which lives in
  `System.ObjectModel`, which only Avalonia references, so the first build skipped 35 subtype matches on clean code.
  `IL/Ancestry.cs` follows each base type and interface through the same resolver, across the references' closure.
- **`BNAQ1010`'s constructor, settled in its pull request by the developer's choice of 2026-11-24:**
  `new ConfinedUseRule(IEnumerable<ConfinedUse>)`, each entry made by `ConfinedUse.Constructing(type)`,
  `AnyMemberOf(type)` or `Member(type, member)` and shaped with `init` options, `IncludingSubtypes`,
  `ExceptTypesTheScanDeclares`, `AllowedInTypes`, `AllowedInMethods` and `AllowedInMarkup`. `ConfinedUse` is a
  record, so `with` shapes one. Markup is allowed by its file, or by its `x:Class`, a type; there is no place that is
  all compiled XAML.
- **A generated body is read on its own as well as through its source method**, and a use is kept once by the token
  of the body it was compiled into and its IL offset. Decision 7 read it once, through its method; the uses are the
  same, and a body the transforms do not fold is not lost.
- **A name the compiler gives a member the source wrote, `<Main>$` or `<Clone>$`, is credited to itself**; only a
  mangled name that names no method is skipped. The plumbing of a closure class or a display class is credited to the
  type that holds it, with no method.
- **The fixtures' stand-in Avalonia types are an assembly of their own**, `ConfinedStandIns`, as Avalonia's are: in the
  scanned assembly they were types the scan declares, which `ExceptTypesTheScanDeclares` rightly left out.
  `ConfinedDebug` builds Release with its PDB beside the DLL, which `.github/repository.json` exempts from the family's
  embedded symbols.
- **Two existing tests changed their subject.** `FriendReferenceTests.AnAssemblyThatReferencesOnlyTheFramework_InspectsNothing`
  scanned the library itself, which now references the decompiler, and scans the stand-ins; and
  `CoverageTests.AnAssemblyWithATypeThatCannotLoad_IsScannedNotFatal` leaves `BNAQ1010` out with `BNAQ1003` and
  `BNAQ1005`, since unconfigured it reads nothing; `ConfinedUseRuleTests` holds its skip against the same fixture.
- **`AssemblyFinding.ToString` names the file and line** where a finding has one.
- **A real-code check needs the assembly's dependencies beside it**, as a test project's output has them; a library's
  own `bin` folder does not.

### Decisions

| Decision | Why |
|---|---|
| **Rule IDs are `BN` + the product's initials, with the rule number kept**: `BNAQ` here, `BNXQ` for XamlQuality, `BNCQ` for the CodeQuality analyzers | An analyzer's ID shares one flat namespace with every analyzer a project loads (`#pragma`, `NoWarn`, `.editorconfig`), and a two-letter prefix is the likeliest to collide. Four letters is unique enough and short enough to type; where a rule comes from is also shown by `helpLinkUri`, `Category` and the package id, so the ID need not spell it out |
| **A prefix never changes once one ID in it has shipped** | Every rename breaks consumers' suppressions. `AQ` → `BNAQ` is the one exception, taken while only `2026.3.922` was published and used only as a test dependency |
| **Each `BNAQ` rule's section in `docs/assemblyquality.md` links its `BNCQ` counterpart, and keeps its heading** | CodeQuality's README and its shipped help links target those anchors; `RulesCatalogTests.EveryRule_HasAReadmeSectionOfItsOwn` fails if a rule loses its heading |

## CodeQuality

Commits and pull request numbers in this section are CodeQuality's own, in
JanusMael/Bennewitz.Ninja.CodeQuality.

### Status

| | |
|---|---|
| Published | `2026.4.1113`, released 2026-11-13 on the maintainer's word: tag `codequality/v2026.4.1113` on `00d1698`, release run `43486995927`, which pushed `Bennewitz.Ninja.CodeQuality` alone. It carries PR #138: `BNCQ1009` and `BNCQ3001` read a delegate passed through a local as the body its declaration gives it, a lambda, an anonymous method, a method group or another such local, and report it at the local's name, unless the block declaring the local assigns it again, passes it by `ref` or `out`, or takes a reference to it. **Effect on a consumer:** a callback subscribed through such a local, unguarded, is newly reported, at each use of the local; nothing that was reported stops being reported. No rule was added, so `AnalyzerReleases.Unshipped.md` had nothing to move. Verified from the feed: the flat container lists it, the served nuspec names Inspections at `00d1698`, the GitHub release's one asset is that package, and a project restoring from nuget.org alone, with `BNCQ1009` at warning, reports a `DataReceivedEventHandler` local holding an unguarded lambda at its subscription, and not one whose lambda catches, where `2026.4.1111` reports neither. DiffView, which asked for it, was told that day. Before it, `2026.4.1111`, released 2026-11-11: tag `codequality/v2026.4.1111` on `47ca716`, release run `43092124003`, which pushed `Bennewitz.Ninja.CodeQuality` alone. It carries PR #132: `BNCQ1009` and `BNCQ3001` take a catch-all whose filter lets only a fatal exception through as guarding the body, and `AnalyzerReleases.Shipped.md` records `2026.4.1104`. Verified from the feed: the flat container lists it, the served nuspec names Inspections at `47ca716`, the GitHub release's one asset is that package, and a project restoring from nuget.org alone, with `BNCQ1009` at warning, reports a thread body whose catch-all is filtered `is not IOException` and not one filtered `is not OutOfMemoryException`, where `2026.4.1104` reports both. Templates was told the version that day; its next release raises every template's pin to it. Before it, `2026.4.1104`, released 2026-11-04, the family's first from here: tag `codequality/v2026.4.1104` on `6fdd3cf`, plan 00005's six rules, `BNCQ1005` to `BNCQ1009` and `BNCQ3001`; and before that `2026.3.928`, its tag now `codequality/v2026.3.928`: `BNCQ1001`, `BNCQ1002` and `BNCQ1004` |
| Not yet released | **`BNCQ3002`**, by plan 00010: a wait that blocks on the task `HeadlessUnitTestSession.Dispatch` returns, by `Wait`, `Result`, `GetAwaiter().GetResult()` (through `ConfigureAwait` too), `Task.WaitAll` or `Task.WaitAny`, on the call or on a local that holds it; on, as a warning. **Effect on a consumer:** a project that references `Avalonia.Headless` and blocks on a dispatch gets a new warning, an error where warnings are errors; elsewhere it registers nothing. Its line is in `AnalyzerReleases.Unshipped.md`, moved to `Shipped` with the tag |
| Next release | On the maintainer's word, from a `codequality/vYYYY.Q.MMDD` tag. One release per calendar day |
| Plan 00005 | **Complete.** [Plan 00005](plans/00005-codequality-exception-rules.md), approved 2026-11-01 in PR #115: six rules for the exception shapes a compiler can see, `BNCQ1005` to `BNCQ1009` and `BNCQ3001`. Step 2 done in PR #116: `FileSystemWatcher` `Changed`, `Deleted` and `Renamed`, and `Process.ErrorDataReceived` and `Exited`, measured on Windows and under WSL 2, each case twice with a control, all five as their entries in `docs/exception-resilience.md` now say, so all five join `BNCQ1009`. A first `Exited` probe called `WaitForExit`, which raises the event on its own caller's thread, and was rerun without it; the entry records that too. Step 3 done: `BNCQ1005` (a time operator given no scheduler, off by default), `BNCQ1006` (an async delegate given to an Rx operator that does not await it) and `BNCQ1007` (`Observable.Create`'s `OnNext` inside a catch that carries on), the numbering reworded, and fixtures from one of our WPF-to-Avalonia ports' corpus and its judged sites, received 2026-11-01 with every checksum matching; merged in PR #117. Step 4, the last: `BNCQ1008` (a gate released outside `finally` in a body that takes it, on), `BNCQ1009` (a callback no caller catches, not wholly inside one try/catch, off by default) and `BNCQ3001` (an Avalonia `DispatcherTimer` `Tick` handler, the same, off by default), with the gotchas' line and a "Checked by" line in each covered entry of `docs/exception-resilience.md` |
| Plan 00005 step 3, measured | **Gates:** shut, every firing test failed; open, every inert test failed; made to report every candidate, every silent test failed and every member of every silent fixture was reported. **Corpus, built once in scratch with the three rules:** of its 10 expected sites for the three shapes, `BNCQ1005` reports Throttle, Interval and Timeout given no scheduler and `BNCQ1007` the producer's `OnNext`; not reported are `ObserveOn(Scheduler.Default)` and `scheduler ?? Scheduler.Default`, schedulers someone chose (the plan's dismissed list), and the 4 async lambdas given to delegates that are not Rx's (decision 5). None of its 5 safe forms, and no unmarked line, is reported. **That port's sites, decision 3:** `BNCQ1005` reports all 5 in its form, 2 THROWS (both ending the process) and 3 SAFE, 60% SAFE, so it ships **off by default**; it stays silent on that port's 4 `?? Scheduler.Default` fallbacks and 18 named pool-scheduler sites. `BNCQ1006` has no site in its form: that port's 9 async lambdas all go to its own `Action` command (1 THROWS, 3 SAFE, 5 unsure), and stay silent. `BNCQ1007` reports that port's one site, THROWS. No family repository references System.Reactive, so these are the only real-code counts |
| Plan 00005 step 4, measured | **Gates:** shut, every firing test and both opt-in tests failed; open, every inert test failed; made to report every candidate, every silent test failed and every member of every silent fixture was reported. **Corpus, built once in scratch with the three rules:** every expected site of `GATE-LEFT-SHUT`, `TIMER-THREADING`, `TIMER-TIMERS`, `TIMER-DISPATCHER`, `THREAD`, `WATCHER-HANDLER`, `PROCESS-HANDLER` and `FINALIZER` is reported. Two are reported one line from the corpus's mark: `BNCQ1008` at the `Release` the corpus marks at its `WaitAsync`, and `BNCQ1009` at the `Elapsed` handler the corpus also marks at the timer's creation. The safe form, and every unmarked line, is silent. **Family repositories**, shallow scratch clones at `main` built with the rules injected (AppServices 7fe414e, AutoVersioning 06bb660, DiffView fc747eb, FileServer b40a424, Inspections e12b50b, JsonC 765968e, ScopedEditors b8c6f69, Templates 861528a), plus the three file-based scripts that hold candidate sites: 9 findings. `BNCQ1009`: AppServices' web-probe test thread (SAFE: `Invoke` wraps what the entry point throws, and it is caught), AppServices' probe thread that throws on purpose to measure the ending (THROWS), Templates' `verify-release.cs` four process handlers (SAFE: `StringBuilder.AppendLine` under a lock), DiffView's `mutate-behaviour.cs` two (SAFE: a log line and a regex match). `BNCQ3001`: DiffView's caret blink (SAFE: flips a flag and invalidates a layer). `BNCQ1008`: none. DiffView was first built from a checkout cut short by a snapshot path too long for Windows, which also failed its test project's pre-build step; rebuilt whole on 2026-11-02 with `core.longpaths`, every project built and the count was the same single finding. Not analysed: Templates' template content, which holds none of the APIs. **That port's sites:** `BNCQ1009` reports its five timer and watcher bodies, all SAFE, and stays silent on its watcher `Error` handler; nothing for the other two. **Decision 3:** `BNCQ1009` 13 findings, 12 SAFE, and `BNCQ3001` 1 finding, SAFE, shipped off in PR #118; the developer turned both on on 2026-11-03, under *Decisions*. `BNCQ1008` no finding, on |
| VSTHRD on DiffView | **Measured 2026-11-02**: `Microsoft.VisualStudio.Threading.Analyzers` 18.12.1, the latest stable, injected as a package reference at its default severities into a scratch clone of DiffView at `main` (fc747eb), every project built. 54 findings at 49 sites, **none a defect**. `VSTHRD100` ×4 in `src`: async void click handlers, which an event's signature requires, each catching the failures it expects and leaving the rest to the dispatcher's handler on purpose. `VSTHRD003` ×2 in `src`: a `Task` property passed through, a concern of `JoinableTaskFactory`, which DiffView does not use. In tests: `VSTHRD002` ×5 sites (twice each), waits on output reads that `WaitForExit` has already completed; `VSTHRD103` ×28, `File.ReadAllBytes` and its kin inside async tests; `VSTHRD200` ×10, helpers without the `Async` suffix. `VSTHRD101` and `VSTHRD110`, the async lambda given to a void delegate and the task nobody awaits, found nothing. **Decided 2026-11-03:** the family adopts it with only `VSTHRD101` and `VSTHRD110` on, under *Decisions*; the adoption is Templates' change and a sync, after the `eol=lf` markdown convention |
| Plan 00010 | **Complete.** Merged in PR #149 on 2026-11-19, unreleased. [Plan 00010](plans/00010-headless-dispatch-wait.md), approved 2026-11-19 in PR #147, with decision 2 (follow a local) and decision 7 (a committed mutation set) kept: `BNCQ3002`. Step 2's measurement is in the gotchas, merged in PR #148, and in the requests table's row of 2026-11-16. Step 3: `HeadlessDispatchWaitAnalyzer`, the local reading moved unchanged from `Callbacks` to `Locals`, `AvaloniaReferences.Headless` and `HeadlessNet100`, `HeadlessDispatchWaitTests`, `Mutations/bncq3002.json` |
| Plan 00010 step 3, measured | **Tests:** 8, all green; `UnguardedCallbackTests` and `DispatcherTimerTickTests` 20 of 20 after the move. **Mutation set**, `mutate --swap src/CodeQuality/CodeQuality.csproj`, direct run, about 1.5 min: 14 of 14 as declared, every one of the 8 tests killed by one: the gate shut, and opened without Avalonia.Headless, each wait form, the local, an array's and a .NET 10 span's tasks, any `Dispatch` taken for the session's, the severity, generated code, and a green control. **Real code**, 2026-11-19, scratch clones built with the analyzer added through `CustomAfterMicrosoftCommonTargets`, which reached the compiler of every headless test project (8): AppServices `3a25858`, ClaudeForge `2bb00cb`, DiffView `b863932`, ScopedEditors `add5abb` and Templates' `bbavalonia` at `b034d00`, no finding; AppServices at `89307bf`, before its fix `048f4a0`, one, at the `GetResult` in `InADispatch`, the measured hang. No SAFE finding, so decision 4 stands |
| Where step 3 departs from plan 00010 | Decision 7's "off by default" mutation does not compile: release tracking (RS2001) refuses a default that disagrees with `AnalyzerReleases.Unshipped.md`. The set lowers the severity a finding is reported at instead, which `By_default_a_wait_on_a_dispatch_is_a_warning` reads. The tests are written `public async Task … => await …` rather than the suite's usual `public Task … =>`, because `mutate` reads only `public void` and `public async Task` as test methods (Testing's follow-ups) |

### Decisions

Settled with the maintainer on 2026-09-25, first recorded in AssemblyQuality's `PROGRESS.md`, except
where a row says otherwise.

| Decision | Choice | Why |
|---|---|---|
| Scope | Roslyn analyzers only. Code generators go elsewhere, such as beside Bennewitz.Ninja.AutoVersioning | One kind of package, one kind of test |
| Source | A clean-room rewrite. Analyzers the maintainer wrote for another project were read for ideas, patterns and the traps their documentation records; no code, names, IDs or links were carried over | |
| Base class | `ScopedAnalyzer<TScope>`, internal: a sealed `Initialize` that enables concurrent execution and sets generated-code handling, a gate that returns null to stay inert and receives nothing it could register on, and a hook that registers only in scope. `AnalyzerConventionTests` fails on any analyzer that does not derive from it | The compiler loads the assembly whole and runs every analyzer in it in every compilation. Generic, because every analyzer here is internal, so its scope type can be internal too and reaches the hook without a cast; a public analyzer would force its scope type public |
| Scaffolding | Generated with `bbpkg`, adapted to an analyzer, released, and only then upstreamed to Bennewitz.Ninja.Templates as a third template beside `bbpkg` and `bbavalonia` | The shape is proved by a release before it is copied |
| First rules | Source-side counterparts of `BNAQ1004`, `BNAQ1001` (opt-in: a policy, not a defect) and `BNAQ1002` (with a transitive member walk). Not `BNAQ1003` | A forbidden use site at compile time is `Microsoft.CodeAnalysis.BannedApiAnalyzers`' job already |
| Rule IDs | Prefix `BNCQ`: `BN` plus the product's initials, the family scheme. Once one ID ships, the prefix never changes. Each rule's README section links its `BNAQ` counterpart, and AssemblyQuality's README links back | One flat namespace of IDs across every analyzer a project loads |
| Rule numbers | **Mirror `BNAQ`**: `BNCQ1004` is `BNAQ1004`'s counterpart. `BNCQ1003` is reserved. Decided by the maintainer in this repository's first session. **Since 2026-11-01**, plan 00005 decision 2: a rule with no counterpart takes the next free number, a rule about Avalonia the next from `BNCQ3001`, and a rule's section names its counterpart, if it has one; the documents say so with step 3 | A consumer who knows one rule knows the other. `BNAQ1005` and `BNAQ1006` have no source counterpart, so a shared number no longer promises one |
| Where a rule comes from | `helpLinkUri` (its README section), `Category` `Bennewitz.Ninja.CodeQuality`, and the package id, not a longer ID | |
| Plan 00005 step 3, beyond the plan's words | `BNCQ1006` does not report a `Select` whose tasks go straight to a System.Reactive operator that takes an observable of tasks (`Concat`, `Merge`, `Switch`), nor an async void method group given to an `Action`. `BNCQ1007` counts a catch that `continue`s its loop as carrying on, as one whose end can be reached is. Rx is referenced in the tests as the real `System.Reactive` 6.1.0 package, not a stub. The root `README.md` and `AGENTS.md`'s overview said every `BNCQ` rule mirrors a `BNAQ` number, and were reworded with the CodeQuality section | Those operators await the tasks, which is the idiom for running them in order; the method group is VSTHRD100's, at the method. A `continue` carries on as surely as falling through, and the plan's "end is reachable" would miss it. The rules choose between Rx's overloads, which a stub could get wrong |
| Rule defaults, 2026-11-03 | **`BNCQ1005`, `BNCQ1009` and `BNCQ3001` are on by default**, the developer's decision of 2026-11-03, over plan 00005 decision 3's counts (3 of 5, 12 of 13 and 1 of 1 findings SAFE), which had shipped them off. All six of plan 00005's rules are on | Catch the shape everywhere, and suppress a safe site where it is, with its reason |
| VSTHRD in the family, 2026-11-03 | **Adopt `Microsoft.VisualStudio.Threading.Analyzers` with only `VSTHRD101` and `VSTHRD110` on**, the developer's decision of 2026-11-03 after the DiffView measurement. Plan 00005 decision 5's ground is then covered: an async lambda given to a void delegate, and a task nobody awaits. `VSTHRD100` stays off | On DiffView, the family's most async code, both found nothing, so they cost nothing today and guard the two shapes measured to end the process or vanish. `VSTHRD100` reported only click handlers an event's signature makes async void, and the rest are `JoinableTaskFactory` or style rules |
| Plan 00005 step 4, beyond the plan's words | `BNCQ3001` reads the `DispatcherTimer` constructor's callback as well as `Tick +=`, because the constructor attaches it to `Tick`; `Run` and `RunOnce` were not measured and are not read. A catch counts as guarding only with no filter and not ending in `throw`, and its type is compared by name (`Exception`, `System.Exception`, `global::System.Exception`). `BNCQ1008` reports at the release. The helpers for reading a delegate argument and a call's name moved from `Reactive` to a shared `Calls`. The tests reference the real Avalonia 12.1.3 as test-time reference assemblies, which no project restores, so `AvaloniaConfinementTests` is untouched | The constructor's callback is the `Tick` handler, and the corpus marks it. A filtered catch decides at run time and one that rethrows sends the exception on. A method group's body may sit in another file, whose semantic model an analyzer should not ask for. The release is the line the fix moves |
| Roslyn pin | **`4.14.0`**, matching AutoVersioning. Decided by the maintainer in this repository's first session | Since `BNCQ1002` reads extension-block receivers, 4.14 is also the oldest the rules can use: the members it reads first appear there, and `4.13.0` cannot compile them. Before that change they built and passed on `4.0.1`, `4.4.0`, `4.8.0` and `4.14.0`, and `3.11.0` could not compile them. The suite also passes against `5.9.0`. A consumer needs Visual Studio 2022 17.14 or a .NET 9.0.3xx SDK. Measured on SDK `8.0.425` (Roslyn 4.11): `CS9057`, the analyzer is skipped, and the build succeeds |
| Extension blocks in one build | Read through the members Roslyn 4.14 declares on `ITypeSymbol`, not a second build for Roslyn 5. Decided by the maintainer on 2026-09-25 | Measured: those members survive in 5.9 beside the new `INamedTypeSymbol` pair, and the shipped 4.14 build reads receivers correctly inside the .NET 10 compiler. Dual `roslynX.Y` folders, measured working with CommunityToolkit.Mvvm on SDKs 9 and 10, are for an API the pin lacks entirely |
| Trimming | The analyzer's csproj sets `IsTrimmable` and `EnableTrimAnalyzer` false | The SDK warns `NETSDK1212` for both on `netstandard2.0`, and CI builds with `-warnaserror`; `repo-conventions` holds only libraries to trimming |
| Tests on the pin | The tests reference `Microsoft.CodeAnalysis.CSharp.Workspaces` at `RoslynVersion` | The testing package floors it at `1.0.1`; without the pin the restore fails with `NU1701` |
| No package dependencies | `SuppressDependenciesWhenPacking` | Every reference is build-only, so pack wrote an empty dependency group and failed with `NU5128`; an analyzer cannot load a package dependency anyway |
| Release tracking | `AnalyzerReleases.Unshipped.md` and `AnalyzerReleases.Shipped.md` from the first release | AutoVersioning left its shipped file empty across three releases |
| Help links | Name the rule's section in `docs/codequality.md` here, since the move. The sections in the archived repository's README stay, for the links `2026.3.928` shipped | A help link is compiled into every consumer's build of a released version and cannot be changed after |

### Measured before the first release

On `af80b7b`, after an independent verification pass that found five rule defects, each fixed with
a test that failed first.

| What | Result |
|---|---|
| The rules against older Roslyn | Build and pass on `4.0.1`, `4.4.0`, `4.8.0` and `4.14.0`; `3.11.0` cannot compile them |
| The compiler each installed SDK ships | `5.0.408`: 3.11; `6.0.428`: 4.3; `7.0.410`: 4.7; `8.0.425`: 4.11; `9.0.318`: 4.14; `10.0.401`: 5.9 |
| A consumer of the packed package | SDK `10.0.401` and `9.0.318`: all three rules fire. SDK `8.0.425`: `CS9057`, the analyzer is skipped, and the build succeeds |
| Seven family repositories, copied, with the analyzer injected into every project and `BNCQ1001` enabled | AssemblyQuality: 8 findings, all in its own `BNAQ` fixtures; `Orphaned.cs` is a shadow its reflection rule is documented as unable to see. DiffView: 2 `BNCQ1001`, `DiffDocumentBuilder.Build` and `DiffSearch.Find`, each an optional token after an optional `options`; DiffView's own `f99aa4d`, dated 2026-09-24, had already put the token first in both, so the copy measured here was of an earlier tree, and DiffView measured none at `c3beea1` on 2026-11-11. JsonC: 2 `BNCQ1002`, `JsonNode` in a JSON editor's API by design. XamlQuality, AppServices, ScopedEditors and FileServer: none, with the analyzer confirmed in every project. No `AD0001` anywhere. DiffView's build stopped at a test project's step that clones reference sources, on a path too long for the copy, after all five of its projects had compiled; GeoHash was not built, because it needs a package that is not on nuget.org |

### Waiting on others

| Sent to | What |
|---|---|
| The Templates session, 2026-09-25 | The measured differences from `bbpkg` for a third template, an analyzer's: the diff `e457b5d..v2026.3.925`, with what each change is for. Acknowledged the same day; the analyzer template is a candidate for a later plan there. Nothing here waits on it |

## Testing

The fourth family, by [plan 00008](plans/00008-converged-mutation-runner.md), approved 2026-11-16: the family's one
mutation runner, converged from DiffView's `scripts/mutate-behaviour.cs` at `9b36789` and the runner this
repository kept under `tools/`.

### Status

| | |
|---|---|
| Published | Nothing yet |
| Not yet released | `Bennewitz.Ninja.Testing.Mutate`, the dotnet tool `mutate`, merged in PR #144, steps 2 to 6 of plan 00008; and its tests run directly where they can, by [plan 00009](plans/00009-direct-test-run.md), approved 2026-11-17. **Effect on a consumer:** a new package. A run over xUnit v3 tests prints `test run: direct, <assembly>` and runs them through the in-process runner xUnit builds into the assembly; any other prints `test run: dotnet test, since …` and runs as before. DiffView moves its sets to it and retires its script once a release carries it, which is DiffView's change |
| Next release | On the developer's word, from a `testing/vYYYY.Q.MMDD` tag, once the trusted-publishing policy has the pattern `Bennewitz.Ninja.Testing*`, which only the developer can add on nuget.org (`docs/publishing.md`). The direct test run it waited for, by the developer's decision of 2026-11-16, is plan 00009, below |
| GitHub description | Four families, applied by `repo-conventions apply` on 2026-11-16 at the developer's word |

### Plan 00008, measured

| Step | Result |
|---|---|
| 2, the family | `repo-conventions check` finds only the description above. Pack and `assert-packages --family testing` pass; the packed tool, installed from outside the repository, answers `mutate --help` with exit 0 |
| 3, DiffView's runner moved | DiffView's tests that do not test the journal pass here, with the new tests: a run from a worktree that kills as declared and leaves the checkout's bytes and `git status` as they were, an untracked killer reaching the worktree, a dead run's worktree removed, a set that edits itself refused, help outside a checkout. The tool's own set, `tests/Testing.Mutate.Tests/Mutations/worktrees.json`, run from a built copy: 12 mutations as declared, its 8 declared tests each killed, in three worktrees at once, since in one it is estimated past its budget |
| 4, the budget and the swap | A run past its budget is refused after its baseline, and one given `--budget` is held to it. Over the smoke set, a mutation of the rules library took 3.9 to 6.2 s by the old runner's swap and 10.3 to 16.1 s by the test project's incremental build, warm, with another session's builds alongside in some runs, so the swap is back as `--swap <project>`, which CI's smoke step uses. In the tool it saves less: nine mutations each way, with other sessions holding the processor at 100%, took 14.0 s on average swapped and 17.9 s built; the old runner also ran the test assembly directly, under *Follow-ups* below. #138's six defects and a control ran as declared, 12.2 to 19.6 s each; the old runner could not run them at all, below |
| 5, this repository moves | CI's `Verification tools` job runs the tool over `tests/XamlQuality.Tests/Mutations/smoke.json` with the rules library swapped; `tools/mutate.cs` and `tools/mutate/` are gone, and with them the follow-ups on a `[Theory]` no defect fails, which the tool counts, and on a defect to what restore reads, which the tool restores for |
| 6, parity with DiffView's runner | At DiffView `9b36789`, in a scratch clone never pushed, three of its sets were run by both: `AgentBaseline.Tests`' `staleness`, 17 mutations, and `DiffView.Avalonia.Tests`' `header-alignment`, 6, and `received-files`, 5. DiffView's runner ran with `--parallel 1`, so in a worktree with `SkipReferenceSources` as its shards are, and the tool with `--property SkipReferenceSources=true`. Every one of the 28 mutations had the same verdict from both, `ok`, and each set's completeness was the same, 8, 3 and 1 declared tests all killed. Their times are not compared: another session's builds and test runs held the processor at 100% throughout |

### Where PR #144 departs from plan 00008

- **Decision 8 is held by a setup fault, not by a rule about where a set lives.** A mutation whose file is the set
  that declares it is refused before anything runs (`Setup.EditsItsOwnSet`), in place of DiffView's check that
  named its own script. So 41 of DiffView's 48 tests moved, not 43: its two tests of that check gave way to
  `OwnSetTests`, beside the five on the journal.
- **A set that names no pins, in a repository with no `Directory.Packages.props`, holds none**, where DiffView's
  runner could not run there at all. The worktree test's repository found it.
- **The tool generates no documentation file.** The moved code's partial `<param>` tags fail `CS1573` with warnings
  as errors, and every type is internal.
- **`--help` answers outside a checkout**, and every progress line ends with the mutation's seconds, and with
  `swapped` where it was.
- **A dead run's worktree folders are deleted whole**, beyond `git worktree remove`, so that one git no longer lists
  is swept too.
- **Two faults the pull request's first CI run found, which DiffView's runner never met, never having run on a CI
  runner or on macOS.** There the test host colours its output, so the line naming a failed test went unread and
  every kill read as `aborted`: every log is now read without escape sequences (`Judge.Plain`). And on macOS the temp
  folder is reached through a link, so a run did not know the worktree cut for it as its own and cut another, and so
  on until its test's bound: the worktree is now named as git names it there, and a run started in one that still
  does not know it stops rather than cut another (`Worktrees.Mark`), and a sweep finds a dead run's worktree by its
  path with every link resolved, as git names it (`Worktrees.Real`). Each has a test. Run in three worktrees, the
  tool's own set then found its worktree tests sharing one temp folder, where one shard's sweep removed another's
  dead worktree first; each run over a test's repository now has a temp folder of its own.
- **The old runner could not time #138.** It rebuilds `src/XamlQuality` for its swap in every pool, and a pool for a
  CodeQuality class never restores that project, so it stopped with `NETSDK1004`; #138 was timed with the tool alone.
- **This file's history keeps the old runner's name** where it records what was, in the syncs above and in the
  requests table.

### Plan 00009, measured

| Step | Result |
|---|---|
| 1, choosing at the baseline | `dotnet test` starts the test host in the assembly's folder on Windows, measured 2026-11-17 by a test that wrote its working directory, and the in-process runner does too, started from anywhere; the direct run is given that folder. `WorktreeRunTests` runs the subject's tests directly (`test run: direct, Calculator.Tests.dll`), falls back for an assembly built with `UseMicrosoftTestingPlatformRunner`, and falls back after a direct baseline its test makes red; a test of the subject's that passes only beside its assembly runs in every baseline, so each way is held to that folder on every CI platform. 8 of 8 pass, locally on Windows |
| 2, reading the runner's log | Six pairs under `tests/Testing.Mutate.Tests/fixtures/runs/`, each one run captured from the in-process runner and from `dotnet test` on xUnit v3 3.2.2: a kill, a failed theory row, a host that exited, a class fixture whose cleanup threw, a green run and a filter that matched nothing. Each pair reads alike, verdict, message, trace lines and failed names, and judges alike; `InProcessLogTests`, 15 of 15 |
| 3, the tool's own set | Nine mutations added, the set now held to `InProcessLogTests.cs` too. Run in three worktrees, 21 mutations in 15.0 min: 19 as declared and 13 of 13 declared tests killed. The other two were the set's faults: one declared words xUnit shortens out of its failure message, and one was killed by no test, below; corrected, both ran as declared alone, 70.1 s and 66.0 s |
| 4, the gain and parity | The smoke set, alternating `main`'s tool and this one, three runs each, on an idle machine, 2026-11-17: a swapped mutation took 3.1 to 3.3 s, mean 3.2, by `dotnet test --project`, and 2.0 to 3.0 s, mean 2.3, directly; a whole run 26.5 s on average, and 21.8. Every verdict was `ok` both ways. The 5.40 s against 1.43 s that made the case for the plan was measured on a busy machine, the day before |

### Where the direct run departs from plan 00009

- **Step 1 measured the working directory on Windows only.** Linux and macOS are measured by CI instead, through the
  subject's test that passes only beside its assembly, which every baseline in `WorktreeRunTests` runs both ways.
- **Decision 4's floor on `Total` is held by a summary line written for it**, not one captured: the in-process
  runner, given a filter that matches nothing, prints `Total: 0` without its other counts, which the reading already
  refuses as a summary missing a count. `InProcessLogTests.A_summary_that_prints_every_count_for_no_tests_is_red`
  gives the green capture's line every count at none, so the floor alone decides it.

### Follow-ups

- **`mutate` declares no test written `public Task Name() =>`.** `Setup`'s `TestMethod` pattern takes only
  `public void` and `public async Task`, as DiffView's runner did, so a set over CodeQuality's rule tests, which
  return `Task` without `async`, is refused for killers its files "do not declare". Found 2026-11-19 by plan 00010's
  set, whose tests were written `async` instead. The pattern is code moved from DiffView, changed only by a plan
  (`src/AGENTS.md`), so widening it waits for the developer.
