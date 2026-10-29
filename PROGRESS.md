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
AssemblyQuality's and CodeQuality's own policies stay until each family's first release from here.

Step 7 is done too, on 2026-10-29, by the maintainer's decisions of that day: AssemblyQuality and
CodeQuality are archived, each with a final README commit pushed to its `main` that names this
repository above the README as it was, kept whole because released versions link to its rule
sections. CodeQuality#3, its one open issue, asked for conventions this repository already carries,
and was closed as done rather than transferred. Neither has an open issue or pull request.

What remains of the plan for this repository:

| Step | What | Waits on |
|---|---|---|
| 9 | Each family's first release from here, when it next changes (decision 21: moving is not a reason to release). Each id read back from nuget.org's flat container with `RepositoryUrl` naming Inspections; the release's assets that family's ids alone. After AssemblyQuality's and CodeQuality's, the policy of the repository each left is deleted. **AssemblyQuality's is done**, `assemblyquality/v2026.4.1029`, below; its old policy is the maintainer's to delete. Until Templates' `plans/00011` scopes a family's generated notes to the family, replace them by hand after the run: GitHub lists every pull request merged in the repository since the family's previous tag | XamlQuality and CodeQuality: a change in that family |

Each family releases alone, from a tag `<family>/vYYYY.Q.MMDD`; [docs/publishing.md](docs/publishing.md)
is the runbook. Whether a family has anything to release is in its section below.

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
  IL2065, IL2070, IL2075). The exemption goes when Templates' `plans/00008` side 2 lands for it.
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
| Published | `2026.4.1026`: `Bennewitz.Ninja.XamlQuality`, `Bennewitz.Ninja.XamlQuality.ThemeAudit` and `Bennewitz.Ninja.XamlQuality.ThemeRuntime`, released 2026-10-26, called for by the developer that day for issue #92. What it changed for a consumer is in its [release notes](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/releases/tag/v2026.4.1026) |
| `main` | carries nothing unreleased |
| Next release | Not scheduled; the developer calls for one, from an `xamlquality/vYYYY.Q.MMDD` tag; its notes, finding no earlier tag of the family, say it is the family's first. One release per calendar day |
| Order of work | The developer's order of 2026-10-15 is done: the brand catalogue's generator, as a `theme-audit` output, by [plan 00004](plans/00004-brand-catalogue.md), in PR #84 and PR #85, released in `2026.4.1026`. Replies and entries for other repositories go out as small pull requests from `main` |
| What a release publishes | The family's ids, the three packages above, as `families` in `.github/repository.json` names them: `scripts/release-target.cs` resolves them from the tag, and `scripts/assert-packages.cs --family xamlquality` checks the pack against them. `packages.push` lists every family's ids, and `packages.local` is empty. XamlQuality gained `packages.push` in PR #94, step 2 of Templates' `plans/00009`; step 5 brought AssemblyQuality and CodeQuality here, under *Merged into Inspections* above |
| Agent skill | `drivable-ui`, served from `main` as a Claude Code plugin since PR #47, by the developer's decision of 2026-09-29: `claude plugin marketplace add JanusMael/Bennewitz.Ninja.Inspections`, then `claude plugin install drivable-ui@xamlquality`. It is in neither package, so it ships on merge, not on a tag |
| Verification tooling | Under `tools/`, by [`plans/00001`](plans/00001-verification-tooling.md), complete: the real-code comparison, `tools/real-code.cs`, since PR #53; the mutation runner, `tools/mutate.cs`, since PR #54; and the headless probe, `tools/probe.cs` over `tools/probe/`, since PR #56. CI's required `Verification tools` job runs the first two and builds the third. Where they depart from the plan is listed below |

#### Where the verification tooling departs from plan 00001

- `--from` reads a local clone with `git clone --shared` and a checkout, not with `git archive`, so
  that an input fetched and one taken from a clone are both git checkouts when they build.
- `tools/mutate.cs` keeps the shared compiler server on, which a run can leave running as any
  `dotnet build` does, where the plan said no process a run started is left: turning it off took
  each defect from 2.7 s to 10 s. It also refuses a baseline that runs fewer tests than the class's
  `[Fact]` tests, and its `[AvaloniaFact]` ones since PR #75, and reports a run that ran no test or
  exited non-zero naming none, where `pmutate_fast.py` read both as clean.
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

#### On `main`, not yet released

Every change after the `v2026.4.1026` tag (`7fd3306`) that a consumer can see; this repository's own
working documents are left out. A change that makes a rule stricter or adds API goes here with its
effect on a consumer, so the release that carries it can say so. `main` takes squash and rebase
merges only, and both give a branch's commits new hashes, so a row cites its pull request.

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
| 2026-10-09 | Templates | Trimming, side 2: when a repository trims, every `Bennewitz.Ninja.*` package in its restore graph carries `IsTrimmable` | Accepted, and the approach agreed on 2026-10-10; waits for Templates' side 1 to merge |
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
| 2026-10-20 | The WPF-to-Avalonia port | Its answer to the question the outcome of PR #87 put, how a `StaticResource` inside a theme's own dictionary resolves, read in the source of Avalonia 12.1.3, of the XamlX it pins and of Semi.Avalonia 12.1.0.1 by a subagent of its own, five of whose citations the port read again at the tag, each claim marked read or inferred. Read: an alias in a dictionary is a deferred item, which takes a snapshot of the resource nodes on the XAML parent stack when it is added, a stack that runs through the theme's include chain and ends with `Application.Current`, and which at its first lookup walks that snapshot innermost first and keeps the first hit for good. Inferred: Semi finds every token its aliases name in its own dictionaries before the walk reaches the application, as a text check of the 557 `StaticResource` references in its 51 dark theme files found. Its inference of 2026-10-15 holds, the port finds, with two corrections: the lookup happens at first access, not at load, and the application is on the stack, as its last node. With the edge cases an audit check would meet, and two corollaries, inferred and not measured | Received, and a receipt sent on 2026-10-20. It is to land in the entry on Semi's palette keys as the port read it, by the developer's decision of 2026-10-14; whether the audit check held for it is written waits on the developer, a question below |
| 2026-10-21 | Issue #92, from the WPF-to-Avalonia port | A release carrying PRs #84, #85 and #88 before its plan's step 7, where its brand catalogue moves to `theme-audit brand` and its own generator retires; until there is one it would run the tool from `main` at a pinned commit. With six defects of its generator and where `theme-audit brand` stands on each, checked against plan 00004's decisions and PR #85's description, not by running the tool | The five defects the tool could carry are each absent as built, checked here against `main`'s tests: an opacity in a `{{val}}` is a problem, `BrandRunnerTests.Val_takes_no_opacity`; a palette is read in the variant its document names, and its chart is framed in that variant's colours, `A_palette_with_light_and_dark_variants_is_filled_in_each_and_its_chart_framed_in_that_variants_colours`, a variant the theme does not declare being a configuration error unless it is `Default`, `Light` or `Dark`, `A_variant_the_theme_does_not_declare_is_a_configuration_error_unless_it_is_one_of_Avalonias_own`; problems exit 1 and a configuration error 2, `ThemeAuditToolTests`; and every problem is listed with its line and nothing is written, `Every_problem_is_listed_together_with_its_line_and_no_document_is_given`. The sixth is the port's console output alone. The developer, who had held releases since 2026-10-15, called for one on 2026-10-26, and `2026.4.1026` carries all three. The outcome went to the port's live session that day |
| 2026-10-21 | Issue #93, from the WPF-to-Avalonia port | `FindResource(key)` passes no theme variant, so in a dark application a control gets Semi's light `FlyoutBackground`: measured once, headless on Linux on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1, the cause read in the source at the tag; it extends the entry on a themed resource looked up with a null variant | Landed in PR #96 as the port measured or read it, not reproduced here, by the developer's decision of 2026-10-14, in the entry it extends, whose heading stays. Its static part, a call to the overloads that pass no variant, went on the rule backlog by the developer's decision of 2026-10-26. The outcome went to the port's live session that day |
| 2026-10-26 | Issue #95, from the WPF-to-Avalonia port | A `VirtualizingPanel` that prepares a container again deselects it and leaves it drawing its old item: measured headless on Linux on Avalonia 12.1.3, each case red in a fixture before the fix and green after it, the causes read in the source at the tag; with an item removed from the source being deselected by design, read there too | Landed in PR #96 as the port measured or read it, not reproduced here, by the developer's decision of 2026-10-14, as an entry under *Virtualization / perf*. It has no static part: whether a panel prepares a container again is decided by its code as it runs. The outcome went to the port's live session that day |

### Rule backlog

Ids are permanent, and the README's rules table is generated from them, so an id is assigned only
when a rule is written.

The rules were written in the order decided on 2026-09-23, and all of them are now: `BNXQ1005`,
`BNXQ1006`, the peer check, `BNXQ1007`, the explicit `AutomationId`, `BNXQ1008`, the zero-size slot,
and `BNXQ1009`, the `ToString()` containers. One candidate came in since, the row that takes no
click where it draws nothing, from the gotchas entry on a row whose `Background` is null, and it is
written as `BNXQ1010`, by the developer's decision of 2026-09-28. Six candidates are open:

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

**Not a fit:** the guide's rule 10, keeping popups in the window's tree. Avalonia's `OverlayPopups` is
set in C# at startup, where no markup scan can see it. That check belongs in the application, as a
startup assertion or a headless test. Nor is the `LogToTrace()` call the logging entries warn
against, by the developer's decision of 2026-10-14: it is C# start-up code too, and the family's
template already leaves it out.

### Questions for the developer

- **Is the theme audit check held on 2026-10-19 written?** It waited on a reading of how a
  `StaticResource` inside a theme's own dictionary resolves, which the port sent on 2026-10-20, in the
  requests above. The follow-up below says what the check would report.

### Follow-ups

- **A theme audit check for a key a theme's controls read only through its own aliases, held (the
  developer, 2026-10-19).** A consumer that redefines such a key, as Semi's `SemiColorPrimary`, which
  Semi's control themes read only through `StaticResource` aliases in its own dictionaries, changes
  nothing they draw, as the entry on Semi's palette keys says the port measured. The audit could report
  it. Why the aliases ignore the redefinition was inferred when the check was held; the port read it in
  Avalonia's source on 2026-10-20, in the requests above: an alias resolves at its first lookup through
  the dictionaries around it when it was added, innermost first and the application last. Whether the
  check is written is the developer's question above. Not yet measured on real code.
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
- **`tools/mutate.cs` builds a defect without a restore**, so a defect that changes what restore
  resolves reads as caught by nothing. Measured over `SourceOnlyPackageTests`: `PrivateAssets="all"` on
  the library reference failed no test in the runner, and once restored it drops the dependency, which
  `The_package_depends_only_on_the_library_at_a_minimum_of_its_own_version` requires. Restoring for a
  defect in a project or MSBuild file is a change of its own.
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
- **`tools/mutate.cs` names only a class's `[Fact]` and `[AvaloniaFact]` tests among those no
  defect failed**, as `pmutate_fast.py` did, so a `[Theory]` that no defect fails goes unnamed, and
  `ItemContainerNameRuleTests` holds seventeen. Discovering theories too is a change of its own.
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
- **The environment probe and two Linux entries may take a Wayland session for the Wayland
  backend.** Found by PR #64's review, each a change of its own, measured under WSL first.
  `Probe-UiTestEnvironment.ps1` answers no to synthetic input under Wayland, "Wayland has no XTEST",
  where the `xdotool` entry measured clicks and plain keys reaching an application on the X11
  backend under Xwayland in a GNOME Wayland session. "`Window.Icon` is a no-op on Wayland" may mean
  the backend, since under a Wayland session `UsePlatformDetect()` runs the application on X11, where
  that entry's own fix says `_NET_WM_ICON` is written. And "`XDG_SESSION_TYPE` distinguishes Wayland
  from X11" names the session, where `TryGetPlatformHandle()?.HandleDescriptor`, `XID` or none,
  tells the backend, as PR #64's measurement printed.

## AssemblyQuality

Pull request numbers in this section are AssemblyQuality's own, in
JanusMael/Bennewitz.Ninja.AssemblyQuality.

### Status

| | |
|---|---|
| Published | `2026.4.1029`, released 2026-10-29 from this repository, the family's first here: tag `assemblyquality/v2026.4.1029` on `3092aee`, release run `40939268891`. Verified from the feed: the flat container lists it, the served nuspec names Inspections at `3092aee`, its DLL is byte-identical to the GitHub release asset's, and a project restoring from nuget.org alone gets it and finds all six rules. It carries #17, Templates' solution-wide friend grants (`plans/00006`): the DLL grants its internals to this repository's sixteen assemblies, matched by name, and every fixture opts out; and the move: the package names Inspections and packs `docs/assemblyquality.md` as its README. No rule, dependency or public API changed. The release's notes were written by hand, the generated ones listing XamlQuality's pull requests. The previous release, `2026.3.929`, added `BNAQ1005` (#12) and `BNAQ1006` (#13), by [plan 00001](plans/assemblyquality/00001-friend-grant-rules.md), now complete |
| Not yet released | Nothing |
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
| Published | `2026.3.928`, released 2026-09-28 and verified from the feed, its tag now `codequality/v2026.3.928`. `BNCQ1001`, `BNCQ1002` and `BNCQ1004`; each rule's help link resolves to its section of the archived repository's README |
| Not yet released | Nothing of its own since `2026.3.928`. The move, under *Merged into Inspections* above |
| Next release | On the maintainer's word, from a `codequality/vYYYY.Q.MMDD` tag, once the family has a change of its own |

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
| Rule numbers | **Mirror `BNAQ`**: `BNCQ1004` is `BNAQ1004`'s counterpart. `BNCQ1003` is reserved. Decided by the maintainer in this repository's first session | A consumer who knows one rule knows the other |
| Where a rule comes from | `helpLinkUri` (its README section), `Category` `Bennewitz.Ninja.CodeQuality`, and the package id, not a longer ID | |
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
| Seven family repositories, copied, with the analyzer injected into every project and `BNCQ1001` enabled | AssemblyQuality: 8 findings, all in its own `BNAQ` fixtures; `Orphaned.cs` is a shadow its reflection rule is documented as unable to see. DiffView: 2 `BNCQ1001`, `DiffDocumentBuilder.Build` and `DiffSearch.Find`. JsonC: 2 `BNCQ1002`, `JsonNode` in a JSON editor's API by design. XamlQuality, AppServices, ScopedEditors and FileServer: none, with the analyzer confirmed in every project. No `AD0001` anywhere. DiffView's build stopped at a test project's step that clones reference sources, on a path too long for the copy, after all five of its projects had compiled; GeoHash was not built, because it needs a package that is not on nuget.org |

### Waiting on others

| Sent to | What |
|---|---|
| The Templates session, 2026-09-25 | The measured differences from `bbpkg` for a third template, an analyzer's: the diff `e457b5d..v2026.3.925`, with what each change is for. Acknowledged the same day; the analyzer template is a candidate for a later plan there. Nothing here waits on it |
