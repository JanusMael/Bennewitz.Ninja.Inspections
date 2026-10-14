# Progress

The work state of `Bennewitz.Ninja.XamlQuality`: what is published, what is on `main` and not yet
released, who depends on what, and the rule backlog with any questions open for the developer.

## Status

| | |
|---|---|
| Published | `2026.3.928`: `Bennewitz.Ninja.XamlQuality` and `Bennewitz.Ninja.XamlQuality.ThemeAudit`, released 2026-09-28. What it changed for a consumer is in its [release notes](https://github.com/JanusMael/Bennewitz.Ninja.XamlQuality/releases/tag/v2026.3.928) |
| `main` | carries `BNXQ1010`, a new rule, `BNXQ1003`'s public reading, the theme audit's parts report and drift check, the runtime theme check and its source-only package, and the docs changes listed below |
| Next release | Once `BNXQ1003`'s public reading and `BNXQ1009`'s `ItemsSource` follow-up have landed, not on `BNXQ1010` alone, by the developer's decision of 2026-10-12. One release per calendar day |
| Order of work | [Plan 00003](plans/00003-runtime-theme-check.md), the runtime check of the themes, in PR #75; then whether `BNXQ1003` should change now that its reading is public, then `BNXQ1009`'s `ItemsSource` reading, by the developer's order of 2026-10-13, which put plan 00002's two steps first |
| Agent skill | `drivable-ui`, served from `main` as a Claude Code plugin since PR #47, by the developer's decision of 2026-09-29: `claude plugin marketplace add JanusMael/Bennewitz.Ninja.XamlQuality`, then `claude plugin install drivable-ui@xamlquality`. It is in neither package, so it ships on merge, not on a tag |
| Verification tooling | Under `tools/`, by [`plans/00001`](plans/00001-verification-tooling.md), complete: the real-code comparison, `tools/real-code.cs`, since PR #53; the mutation runner, `tools/mutate.cs`, since PR #54; and the headless probe, `tools/probe.cs` over `tools/probe/`, since PR #56. CI's required `Verification tools` job runs the first two and builds the third. Where they depart from the plan is listed below |

### Where the verification tooling departs from plan 00001

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

### On `main`, not yet released

Every change after the `v2026.3.928` tag (`067a509`) that a consumer can see; this repository's own
working documents are left out. A change that makes a rule stricter or adds API goes here with its
effect on a consumer, so the release that carries it can say so. `main` takes squash and rebase
merges only, and both give a branch's commits new hashes, so a row cites its pull request.

| Commit or PR | Change | Effect on a consumer |
|---|---|---|
| PR #43 | `docs/avalonia-gotchas.md` gains an AvaloniaEdit entry from DiffView's session: a zero-length element that a host's generator places at a line start is lost where a built-in generator claims the line's first character, because `TextView`'s constructor appends the built-ins first and the first element with a length ends a round of construction. Checked in AvaloniaEdit's source at the `12.0.0` tag, and measured headless on Avalonia.AvaloniaEdit 12.0.0 with Avalonia 12.1.3. The cause held as sent, and the loss is wider: under the default options, lines beginning with a URL or an e-mail address lose the element too | Docs only |
| PR #47 | `docs/ai-drivable-ui.md` says how an agent loads the method as a skill: this repository now serves the `drivable-ui` skill, moved from bb-skills, as a Claude Code plugin from `main` | Docs only. The plugin is in neither package; it reaches a machine through `claude plugin marketplace add JanusMael/Bennewitz.Ninja.XamlQuality`, then `claude plugin install drivable-ui@xamlquality` |
| PR #49 | `docs/avalonia-gotchas.md` corrects its entry on a control's own `Styles`, which said they never match the control. At 12.1.3 `StyledElement.ApplyStyles` applies each ancestor's styles and then the control's own, and measured headless under Fluent, Simple and Semi, a `Border` took the `Background` its own `Border.Styles` gave it. The entry is now "A control's `Styles` apply to the control itself, as well as to its descendants" | Docs only. The heading changed, so OpenForge2k and a WPF-to-Avalonia port, which cite the entry, are told |
| PR #50 | `BNXQ1010`, `ItemContainerBackgroundRule`: every list, combo or tab row generated from `ItemsSource` draws a background of its own: its template draws one, and where it draws the row's `Background`, that is never null. It reports rows whose `Background` can be null where their template draws it, a style or theme that sets `{x:Null}`, a binding with no non-null `TargetNullValue`, or a theme `BasedOn` nothing that sets none where the row's type registers no brush, and rows whose template draws no background at all. What draws the row's background is a `Border`, a panel, a `ContentPresenter` or a `TextBlock` taking it, by a `TemplateBinding` or a binding to the row, counted from the nearest ancestor of the type it names in a `RelativeSource` that finds an ancestor, a shape's `Fill` taking it, or a templated control taking it whose own template draws it, where the `Background` a type's own markup, or a base's, gives it, found by its `x:Class`, outranks a template's but not a binding at its usage; what a `ContentPresenter` or a `TransitioningContentControl` in the template holds as markup content is not followed; a theme that sets no template leaves the framework's or `ContentControl`'s default, both of which draw it. What the rows present decides nothing, nor what hides an element, how much of a row it covers, or what a `Popup` holds, by the developer's decision of 2026-10-04: a row whose content paints across it is still reported. A style whose selector holds of every row whatever the data sets them all, and a `:not()` of a state may not; `:disabled` may hold from the start wherever markup in the scan sets `IsEnabled` to anything but `True` or gives a control a `Command`, in any spelling, by the developer's decision of 2026-10-08, and `:selected` where a list, its theme or its type always selects a row or markup may select one, when it paints only the rows selected; a value a template's markup gives an element ranks below a style with a condition and below its own code, and a style that binds a part of the template may draw there. What markup cannot decide is named in `Skipped`. It leaves tree rows out. The gotchas entry on a null row background points at it | A new rule, so nothing changes until a consumer constructs it. On a WPF-to-Avalonia port it reports the row theme that the port's `e8e851c` fixed, and nothing after the fix, inspecting 12 lists each time; on ClaudeForge `70dc713` it inspects 19 and reports nothing. `BNXQ1009`'s selector and peer reading moved to shared helpers unchanged, and its output on both is identical to `main`'s, as are `BNXQ1004`'s, `BNXQ1005`'s and `BNXQ1008`'s |
| PR #50 | `BNXQ1005` and `BNXQ1009`, through the `ResourceScope` and `ThemeReader` they share with `BNXQ1010`, read a property in every spelling the XAML compiler takes, by the property its owner names: a setter's name alone, after an owner or in parentheses, in its `Property` attribute or a `Setter.Property` element; and an element's own as an attribute or a property element, where two spellings that set it or may decide nothing, because the framework applies each in turn. An owner is the type its prefix names, found by its name only in the framework's own namespace, Avalonia's, or WPF's or MAUI's in markup written for that framework; one in a URI that only an assembly's `XmlnsDefinition` maps is not decided. On an element, the control's type or a base names its own, and another owner the property held by the field the compiler takes for it: after an owner with an instance property of the name only a binding sets it, and after one with static `Set` or `Get` accessors any value does. In a setter, a `TemplateBinding` or a path, an owner names what its most-derived public field holds, the control's own type included, and where two types in the control's chain carry its name, it is decided only where they hold one property. A theme, named or implicit, applies only where its `TargetType`, the type its prefix names, is the control's style key or a base of it, and a style key a `StyleKeyOverride` returns other than by a `typeof` is not read, and `BNXQ1009` takes only Avalonia's `AutomationProperties` to name a row. Where an owner or a field cannot be read, a value after another owner may or may not be a binding, or an owner or a theme is not decided, the rule says so in `Skipped` | Either rule's answer changes only where markup uses one of these spellings or such a theme. A theme or a style that set `Focusable`, `ItemContainerTheme`, `ItemTemplate`, `DisplayMemberBinding`, `AutomationProperties.Name` or a `Theme` so was read as setting nothing, and is now followed; a list's `ItemsSource` or `ItemContainerTheme`, or a `TextBlock`'s `Text`, bound after another owner that holds it, as `ComboBox.ItemContainerTheme` on a list, or given after a consumer's attached alias of it, is now the element's own; a setter or a property element after an owner whose attached property it names, or after a subclass that hides the field, read as the control's own before, now is not; a consumer's own `AutomationProperties` no longer names rows; and an element that gives one property two ways, an owner that cannot be read or sits in a consumer's own URI, WPF's or MAUI's in Avalonia's markup included, one that two types in the control's chain are called after and that hold different properties, and a theme whose `TargetType` is not the control's style key are named in `Skipped`. So a finding can appear or go. On the WPF-to-Avalonia port and ClaudeForge, which write none of them, both rules' output is identical to `main`'s. No consumer in the family runs either rule |
| PR #57 | `docs/avalonia-gotchas.md` gains seven Linux entries, five on Avalonia's AT-SPI bridge and two on its X11 windows: a node keeps the children it was first asked for, a list that had rows when first asked has no scroll bars in the tree, `SelectChild` adds to a multiple-selection list's selection, `GetActions` names each action by its localised name, every window's frame is `active`, under GNOME a window moved and resized together is clamped against its old size, and a window's `FrameSize` can be null at `Opened`. Each was measured on Avalonia 12.1.3 under GNOME Shell 50.1, run headless with a virtual monitor, the application under Xwayland, with its cause read in the source at the tag, and each fix measured but the active frame's, since no window became active in the headless session. `docs/ai-drivable-ui.md` says a harness can drive the peers over AT-SPI, as measured, where it said that had not been verified, and so does `Probe-UiTestEnvironment.ps1` under GNOME on Wayland | Docs only |
| PR #59 | `docs/avalonia-gotchas.md` gains three entries measured on Windows and corrects one sentence. A hit-test answers from the last frame drawn, so right after a layout change `InputHitTest` finds what was there before: headless until a frame is rendered, and on a real window until the next one is composed. A `MenuItem` offers UI Automation no `Invoke` or `ExpandCollapse`, and on Windows a checkable one no `Toggle`, since the bridge asks for providers off the UI thread and the peer reads `ToggleType` to decide; a peer that caches it works around that. A `PrintWindow` capture of an Avalonia window is solid black, and reports success, without `PW_RENDERFULLCONTENT`. The AT-SPI scroll bars entry's sentence on UI Automation's control view, which rested on the source, is now measured. Measured on Avalonia 12.1.3 in Windows Sandbox and headless, the hit-test's and the `MenuItem`'s causes read in the source at the tag | Docs only |
| PR #60 | `docs/ai-drivable-ui.md`, and the `drivable-ui` skill that condenses it, take in what the two Windows Sandbox sessions measured about driving an application on Windows. Rule 10 and its trap row: only a modal dialog is a top-level element of its own, and a popup drawn in a window of its own is not necessarily outside the main window's tree, since Avalonia's menus sit under the main window's element. Section 4 asks for a survey of every pattern the running application advertises, and says an Avalonia menu needs real input to open. Section 5: a harness that sends real input guards every event, a click against the window on top at its pixel, compared with the element's own window walked in the raw view, a key against what holds focus, a gesture by its effect, and a run that stops early is not a pass. Section 8 adds an element hidden with `IsVisible=false`. Measured on Avalonia 12.1.3 with the Fluent theme | Docs only. The skill reaches every install on merge, since the plugin is served from `main` |
| PR #62 | `docs/ai-drivable-ui/SandboxHarnessJournal.md` gains a dated section: `<VGpu>Disable</VGpu>`, as Microsoft's sample configuration spells it, left Windows Sandbox's GPU sharing on, and `<vGPU>Disable</vGPU>`, as its reference spells it, turned it off, by the adapters the guest listed in the two sessions of 2026-10-13. So the journal's own comparison of the two settings, and the freeze that followed it, may have had sharing on, and GPU sharing stays a candidate for the host takedowns, not their settled cause; the setting is written `<vGPU>` and checked in the guest. Two dated notes mark passages those sessions measured against: Windows granted the foreground to a process that had just sent input, and the first application to start may not keep it; and Avalonia 12.1.3's menus sit under the main window's element, where the section on the Apps key drove the WPF build's. The guide's pointer to the journal says to spell the setting `<vGPU>` and check it in the guest | Docs only |
| PR #64 | `docs/avalonia-gotchas.md` gains a Linux entry: on the Wayland backend, an application is not on the AT-SPI bus, since only `Avalonia.X11` starts the bridge; `UsePlatformDetect()` chooses X11 on Linux, under a Wayland session too. Measured on Avalonia 12.1.3 under GNOME Shell 50.1, run headless with a virtual monitor: a window on the Wayland backend did not appear on the bus in 3 runs of 3, where one on X11 did, with its cause read in the source at the tag. `docs/ai-drivable-ui.md` scopes its Linux paragraph to the X11 backend, and so does `Probe-UiTestEnvironment.ps1`'s verdict under Wayland | Docs only |
| PR #65 | `docs/avalonia-gotchas.md` corrects "There is no render tier", whose fix treated rendering as accelerated unless the session is remote. `AngleEgl` is no sign that a GPU draws: in an x64 process, Avalonia creates its Direct3D 11 device on DXGI adapter 0 unless the application's `GraphicsAdapterSelectionCallback` picks another, read in the source at 12.1.3, and in a Windows Sandbox guest with no GPU, adapter 0 was the Basic Render Driver and an Avalonia 12.1.3 window drew through ANGLE on WARP. The fix reads that adapter and treats rendering as software when it is the Basic Render Driver, by its name or by vendor and device, or carries the software flag; a hardware adapter proves nothing, since Avalonia falls back to software where ANGLE does not start. The heading is unchanged | Docs only |
| PR #67 | `docs/ai-drivable-ui.md` section 4, and the `drivable-ui` skill that condenses it, name a fifth layer of verification: attended runs that send real input on the person's own desktop, at the person's go and with the person at the machine, the window checked unobstructed and focused before every event, the run refused when the check fails and every event guarded as section 5 says, never part of an unattended sweep. The heading counts five layers where it said four | Docs only. The skill reaches every install on merge, since the plugin is served from `main` |
| PR #70 | `BNXQ1003`'s reading is public, by plan 00002's step A. `TemplatePartRule.ReadParts` returns what the rule reads from a scan: each control's `PART_` constants, each part looked up on its template with the code that looks it up, `Own`, `BaseClass` or `Helper`, by its type's full name, its method's compiled name and its assembly, and the parts each of its themes declares, with the element's local name and its automation id, in an order that compares as text. `Analyze` derives its findings from it. Six public records carry it: `TemplatePartReading`, `ControlTemplateParts`, `TemplatePartLookup`, `TemplatePartOrigin`, `ThemeParts` and `DeclaredPart` | New API, whose names bind once released. `BNXQ1003`'s output is unchanged, and so is every rule's on the real-code inputs. Over DiffView's `bf6b734`, given AvaloniaEdit's assembly too, it lists the 34 lookups in DiffView's code and the 11 in AvaloniaEdit's that DiffView's plan 00030 measured |
| PR #72 | The theme audit's second report, by plan 00002's step B. `TemplatePartReading.ToMarkdown` renders `BNXQ1003`'s reading as the parts report: a section per control, its rows in the order its themes declare its parts, an Element and an AutomationId column per theme, **missing** where a theme lacks a part the code declares or looks up, and who looks each part up. `theme-audit.json` gains an optional `parts` section, which `PartsRunner.Read` reads with the assemblies loaded apart from the tool's own, and `theme-audit parts [--check]` writes and checks the report. The drift check moves into the library as `OutputDrift.Check`, and `report --check` and `compat --check` run on it | New API and a new command; the report's format is a contract, so a change to it is recorded here. `report --check` and `compat --check` keep their exit codes and messages, and on drift now also write the fresh output beside the stale one as `name.received.ext`: ignore `*.received.*`, and remove a received `.axaml` before a build, which compiles it. Over DiffView's `bf6b734` with AvaloniaEdit's assembly, `theme-audit parts` writes a report of 11 controls and 59 lookups |
| PR #73 | `docs/ai-drivable-ui.md` section 5, and the `drivable-ui` skill that condenses it, say an open menu lasts only while its window holds the foreground. When another application took the foreground, by a call or by a click, the window was deactivated and its menu closed at once, so raising the window back found the menu closed; a window that held the foreground kept its menu open through `BringWindowToTop`, `SetForegroundWindow`, `SetWindowPos(HWND_TOP)` and `AttachThreadInput` with both, and a click on its own title bar closed it; a click on the menu header of a window behind another activated it and opened the menu in one click. Measured on Avalonia 12.1.3 in Windows Sandbox | Docs only. The skill reaches every install on merge, since the plugin is served from `main` |
| PR #74 | The theme audit's follow-ups from PR #72's review. The parts report fences a code span past any backtick in it and reads a line break in a value as a space, so a generic type's name, or an automation id holding a line break, no longer breaks a heading or a table row. `OutputDrift.Write` writes an output and removes the received copy a stale check left, and the tool writes every output through it; the audit's scans no longer read a received copy, which a failing check can leave in a theme's or a consumer's folder, where it changed the next report. `PartsRunner` reads a markup folder inside another the configuration names, or one named twice, once, where it listed each of its themes twice. A test now fails when the parts runner's load context cannot find a dependency in the assemblies' folder. `--check`'s help, and the README, no longer say it writes nothing | New API, `OutputDrift.Write`. The report's format changes only where a name or an id holds a backtick or a line break, and no release carries the report yet |
| PR #75 | The runtime theme check, by plan 00003. The library gains `RuntimeCheckPlan`, `RuntimeSetup`, `RuntimeLookup`, `RuntimeAnswer`, `RuntimeCheckResult`, `RuntimeDifference` and `RuntimeDifferenceKind`, public in its `ThemeAudit` namespace: the plan works out from the audit's result, and the names an adopting project's map holds, what to ask Avalonia, and compares the answers. A third package, `Bennewitz.Ninja.XamlQuality.ThemeRuntime`, ships as source only: `ThemeRuntimeCheck` and `ThemeRuntimeMap`, internal, which an Avalonia test project compiles against its own Avalonia; it depends on the library at a minimum of its release. Over fixture themes the check finds what the audit's model cannot see: a resource a theme adds in its code, a key held only in a `ControlTheme`'s resources, and an alias whose target its variant lacks | New API and a new package, whose names bind once released; nothing changes until a project adopts the package. A file that imports both `Avalonia.Styling` and the audit's namespace aliases `ThemeVariant`, which both declare, as the README shows. Over DiffView's `bf6b734`, on Avalonia 12.1.2, with both packages packed from this branch in place of its `2026.3.928` pin and the check added with `dotnet add package`, it compiled in DiffView's test project and read the whole configuration, every theme, merged dictionary and variant mapped, 16 setups in all: 10354 lookups, no difference and no skip |
| PR #75 | `theme-audit parts` leaves out a received copy among the markup it reads, as the audit's other scans have since PR #74: a received copy a failing `compat --check` left in a folder the parts section names, `Compat.received.axaml`, read as a second theme of the controls it declares | A parts report written beside a received copy no longer lists it as a theme. No release carries the report yet |
## Consumers inside the family

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

## Requests from other repositories

Every request another repository has made of this one, with the date it arrived, so that its age is
seen. A lesson for `docs/avalonia-gotchas.md` or `docs/ai-drivable-ui.md` is checked before it lands,
and its sender hears landed, changed or declined. One that needs a real window and real input is
measured in Windows Sandbox, a desktop of its own, in a batch launched when the developer says so, by
the developer's decision of 2026-10-12. One that needs X11 or a Linux desktop is measured under WSL,
in a headless GNOME session no one sees, or on a Linux machine on the tailnet, and lands like any
other, by the developer's decision of 2026-10-13. One that needs an ARM64 machine is not reproduced
here, and stays out of the documents, by the developer's decision of 2026-10-12.

| Arrived | From | Request | State |
|---|---|---|---|
| 2026-09-28 | DiffView | Five lessons from its plan 00029: raising a menu item's `Click` does not toggle it, key bindings run before `KeyDown`, and three facts about driving X11 | Received, and a receipt sent on 2026-10-10. Not checked |
| 2026-09-30 | DiffView | A headless hit-test reads the last composed frame, not the layout | Received, and a receipt sent on 2026-10-10. Measured on 2026-10-13, headless on Avalonia 12.1.3 and on a real window in Windows Sandbox, and landed in PR #59: it holds on a real window too, until the next frame is composed, in 10 rounds of 10; headless, `CaptureRenderedFrame()`, or `RunJobs` followed by a forced render tick, settles it, and neither `RunJobs` nor a forced tick alone does. The outcome went to DiffView on 2026-10-13 |
| 2026-10-03 | DiffView | A public `BNXQ1003` reading: per control, the parts it looks up as distinct from the `PART_` constants it only declares, and which code makes each lookup | Accepted. [Plan 00002](plans/00002-template-part-reading.md), approved on 2026-10-13, takes it in two steps: the reading, with the parts each theme declares, then a parts report the theme audit writes and checks. Step A, the reading, landed in PR #70, and step B, the parts report and its drift check, in PR #72. DiffView's plan 00030 waits for the release that carries them |
| 2026-10-08 | DiffView | Twelve findings from driving an Avalonia app on Windows through UI Automation | Received, and a receipt sent on 2026-10-10. Items 1, 2, 3, 5, 6, 7, 8 and 11 measured in Windows Sandbox on 2026-10-13, on Avalonia 12.1.3: 1, 2, 3, 5 and 11 hold; 6 holds for a process that has sent no input, refused in the second session, but not for one that has just sent input, which took the foreground in both; 8 is narrowed, keys reaching a window with nothing focused but only its own bindings; 7 did not reproduce, in either session, and is left out. Items 1 and 2 landed in PR #59; 3, 5, 6, 8 and 11 in the guide in PR #60, 3 as a sentence. 4, 9, 10 and 12 were not measured here. Its suggestion of a layer for a by-hand driver with a person watching landed in section 4 in PR #67, by the developer's decision of 2026-10-13. The outcome went to DiffView on 2026-10-13 |
| 2026-10-08 | The WPF-to-Avalonia port | A correction to the render-tier entry, the cost of the software renderer, and a headless click that lands where a control used to be | Received, and a receipt sent on 2026-10-10. The correction measured on x64 in Windows Sandbox on 2026-10-13, and landed in PR #65, changed: with no GPU the guest's adapter 0 was the Basic Render Driver and Avalonia drew through ANGLE on WARP, and the fix tells that adapter by its name, or its vendor and device, as well as the software flag, which two of its three entries in the guest lacked. The correction's ARM64 rule and measurement, and the renderer's cost, measured on ARM64 alone, are left out, by the developer's decision of 2026-10-12; its x64 comparison of a forced `Software` against the default, made with a GPU and not repeated here, is not landed either, since the entry is about telling software rendering apart, not about its cost. The headless click is not checked yet. The outcome went to the port's live session on 2026-10-13, not to the retired one that sent this, by the developer's decision of that day |
| 2026-10-09 | DiffView | Two driving lessons: a drag's success is not its effect, and a menu's automation window is not the popup it is drawn in | Received, and a receipt sent on 2026-10-10. Measured in Windows Sandbox on 2026-10-13: the drag's holds; the menu's is changed, its items being under the main window's element and nearest the popup's window, which is where they are drawn, alike for a menu in XAML, one built in code and a context menu. Both landed in the guide in PR #60, the menu's as changed. The outcome went to DiffView on 2026-10-13 |
| 2026-10-09 | Issue #48 | Two AT-SPI bridge behaviours: a list keeps the children it first reported, and a list's scroll bars are not in the tree | Measured on Linux on 2026-10-13, and both landed with their fixes in PR #57, each narrowed: a list keeps the nodes it first reported, which a scroll renames after the rows their containers hold, and its scroll bars are missing only where it had rows when first asked. The bars' fix gains a warning, measured: a bar written through it stops following the list. Where the surfaced bars sit in the tree, which depends on the application's own layout, is left out. The outcome went to the port's live session on 2026-10-13, by the developer's decision of that day |
| 2026-10-09 | Templates | Trimming, side 2: when a repository trims, every `Bennewitz.Ninja.*` package in its restore graph carries `IsTrimmable` | Accepted, and the approach agreed on 2026-10-10; waits for Templates' side 1 to merge |
| 2026-10-10 | DiffView | Three lessons for the guide: a harness that counts silence as success, the automation patterns of stock template parts, and asking before borrowing the desktop | Received, and a receipt sent on 2026-10-10. The second measured in Windows Sandbox on 2026-10-13: it holds but for the `ExpandCollapse` it reported on a `MenuItem`, found only on the window's System menu item, which is Windows' own; and a checkable `MenuItem` offers no Toggle pattern on Windows, a defect in Avalonia's automation that a peer of the application's own works around, measured headless and, in the second session, on a real window. That part landed in PR #59, and the survey in the guide in PR #60. The first landed there too, as method: a run that stops early is not a pass. The third the guide already says, in sections 3, 4 and 7. The outcome went to DiffView on 2026-10-13 |
| 2026-10-12 | Issue #55 | Four more AT-SPI bridge behaviours, two X11 window findings, and a correction to the guide: driving over AT-SPI is verified | Measured on Linux on 2026-10-13, and landed in PR #57, with changes: `SelectChild` adds only where the list allows more than one selection; an open combo's items are missing from its children for the reason a list's late rows are, the bridge keeping the children it was first asked for, so the two are one entry; the move's fix may instead lay the window out before moving it, measured, and the frame's waits for the frame to change rather than polling, measured through `FrameSizeProperty`; the active frame's fix names `_NET_ACTIVE_WINDOW` and the bridge's own events rather than `_NET_WM_STATE_FOCUSED`, none of them measured, since no window became active in the headless session; and the move's claims about Windows are left out, unmeasured. The outcome went to the port's live session on 2026-10-13, by the developer's decision of that day |
| 2026-10-13 | The WPF-to-Avalonia port | Avalonia.Wayland 12.1.3 and 12.1.4 do not start the AT-SPI bridge, so a window on the Wayland backend has no accessibility tree; issue #55's verification scoped to the X11 backend; and four facts about the Wayland backend: window methods that do nothing, a screen scaling fixed at 1.0, two options it lacks, and no attention request | Received, and a receipt sent on 2026-10-13. The first two measured on 2026-10-13, on Avalonia 12.1.3 in a headless GNOME session under WSL: a window on the Wayland backend, which only `UseWayland()` or `UseWaylandWithFallback()` chooses, opened without the bridge's assembly loaded and did not appear on the AT-SPI bus in 20 seconds of watching, where one on X11, which `UsePlatformDetect()` chooses on Linux, appeared there with its children, in 3 runs of 3 each; in the source at the tag only `Avalonia.X11` references the bridge. 12.1.4 not measured, and the four facts not checked. The first two landed in PR #64 for 12.1.3: the first as an entry of the gotchas, the second as the guide's Linux paragraph and the environment probe's verdict under Wayland, scoped to the X11 backend. The outcome went to the port's live session on 2026-10-13 |
| 2026-10-13 | DiffView | Two follow-ups to its earlier lessons. The X11 tooltip re-measured: `xdotool mousemove` raised one in ten trials of ten, on Avalonia 12.1.2 under GNOME Shell 48.7 with Xwayland 24.1.6, where the `xdotool` entry says a tooltip never appears; the same Avalonia had raised none on 2026-09-13, for a reason not found. And item 7 of 2026-10-08 back with a qualifier: raising a window closes a menu it opened when the raise changes the foreground | Received, and a receipt sent on 2026-10-13. The qualifier measured in Windows Sandbox on 2026-10-13, on Avalonia 12.1.3, and landed in PR #73, changed: when another application took the foreground, by a call or by a click, the menu closed at once, before any raise, so raising the window back found it closed; a window holding the foreground kept its menu open through four ways of raising it, and a click on its own title bar closed it. No way was found to leave a menu open on a window without the foreground; a window covered by an always-on-top one, which keeps the foreground, was not measured. The outcome went to DiffView on 2026-10-13. The tooltip is not checked |
| 2026-10-13 | The WPF-to-Avalonia port | Two headless findings on Avalonia 12.1.3 and Semi.Avalonia 12.1.0.1: a `ScrollContentPresenter` loses a wheel notch when an `ILogicalScrollable`'s position moved before it raised `ScrollInvalidated`, in 7 runs of 30 of a test that turned the wheel straight after `RunJobs`; and Semi's `SplitView` pane slides open, so a fixture that measures right after setting `IsPaneOpen` reads it part way. A correction the same day withdrew the sentence that a render tick before the notch prevented the loss | Received, and a receipt sent on 2026-10-13. Not checked |
| 2026-10-13 | The WPF-to-Avalonia port | Two findings, headless on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1. A subclass that borrows its base's theme through `StyleKeyOverride` is not selected by a style that names its own type, only by one that names the base, its cause inferred and not read. And Semi's `TextBox` template puts `InnerRightContent` in an `Auto` column beside the text's star column, so wide inner content squeezes the text to nothing: a 183 px block left the text 0 px wide when the box was offered 159 to 220 px | Received, and a receipt sent on 2026-10-13. Not checked || 2026-10-13 | DiffView | Two lessons for the guide's section 5, from verifying its click guard on a real desktop. A one-off harness written beside a DPI-aware driver was itself unaware, and at 200% fed a 2200x1440 window's virtualised 1113x756 back into `SetWindowPos`. And a negative result about a guard is evidence only once the condition it guards against is shown present: measure `WindowFromPoint` at the click point first, and report a run whose premise failed as unread, never as a failure | Received, offered to use or discard. Not checked |

## Rule backlog

Ids are permanent, and the README's rules table is generated from them, so an id is assigned only
when a rule is written.

The rules were written in the order decided on 2026-09-23, and all of them are now: `BNXQ1005`,
`BNXQ1006`, the peer check, `BNXQ1007`, the explicit `AutomationId`, `BNXQ1008`, the zero-size slot,
and `BNXQ1009`, the `ToString()` containers. One candidate came in since, the row that takes no
click where it draws nothing, from the gotchas entry on a row whose `Background` is null, and it is
written as `BNXQ1010`, by the developer's decision of 2026-09-28. No candidate is open.

**Not a fit:** the guide's rule 10, keeping popups in the window's tree. Avalonia's `OverlayPopups` is
set in C# at startup, where no markup scan can see it. That check belongs in the application, as a
startup assertion or a headless test.

## Questions for the developer

None open.

## Follow-ups

- **The runtime check of the themes (the developer's decisions of 2026-10-13 and 2026-10-14)** is in
  PR #75, by [plan 00003](plans/00003-runtime-theme-check.md). DiffView's test is its first
  adopter, told when a release carries the package. The release that first carries
  `Bennewitz.Ninja.XamlQuality.ThemeRuntime` checks on nuget.org that the publishing policy still
  permits a new id, and names the package in the repository's description, by the plan's decision 9.
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
- **`BNXQ1003` follows a template through direct calls only.** A call through an interface or a
  virtual method names the method declared, not the one that runs, so an implementation reached only
  that way keeps its lookups as its own type's. In DiffView, `SideBySideDiffView`'s
  `IDiffSurface.OnPartsAttached` is reached so, and its lookup is credited to `SideBySideDiffView`,
  where it belongs; following the dispatch would credit it to `DiffViewer` too, which hands the same
  controller its template. Nor is a template a closure captures followed, so a themed subclass of a
  base whose `OnApplyTemplate` looks a part up inside a lambda reads as having no parts, as it did
  before the reading was public. A question for the assessment of `BNXQ1003` after plan 00003.
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
- **`BNXQ1009` reads an item type only from an item template's `x:DataType`.** A list with no item
  template, or a template that declares none, is named in `Skipped`, as all 6 of ClaudeForge's skips
  are. Reading the type from the `ItemsSource` binding's path, through the view's own `x:DataType`,
  would decide most of them. It can add findings, so it is a change of its own. By the developer's
  decision of 2026-09-28 it follows the row-background rule, and by the developer's order of
  2026-10-10, the verification tooling and `BNXQ1003`'s public reading come before it.
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
