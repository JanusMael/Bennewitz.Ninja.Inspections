# Progress

The work state of `Bennewitz.Ninja.XamlQuality`: what is published, what is on `main` and not yet
released, who depends on what, and the rule backlog with any questions open for the developer.

## Status

| | |
|---|---|
| Published | `2026.4.1014`: `Bennewitz.Ninja.XamlQuality`, `Bennewitz.Ninja.XamlQuality.ThemeAudit` and, for the first time, `Bennewitz.Ninja.XamlQuality.ThemeRuntime`, released 2026-10-14. What it changed for a consumer is in its [release notes](https://github.com/JanusMael/Bennewitz.Ninja.XamlQuality/releases/tag/v2026.4.1014) |
| `main` | carries one unreleased change, `BNXQ1003`'s from its assessment, and a docs change, both listed below |
| Next release | Not scheduled; the developer decides. One release per calendar day |
| Order of work | `BNXQ1009`'s `ItemsSource` reading, by the developer's order of 2026-10-13, now that `BNXQ1003`'s assessment is decided |
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

### Where `BNXQ1003` departs from plan 00002

- Decision 5 made two scanned types of one simple name one entry, as they were to the rule. Since
  PR #79, by the developer's assessment of 2026-10-14, such a name is no entry: the rule reads a
  theme's target by its name alone, so it cannot say which of the types a theme is for. It names each
  theme of the name in `Skipped`, and the reading names the name in its own `Skipped` where parts were
  found. Decision 7's list of what the reading's `Skipped` holds gains that kind.

### On `main`, not yet released

Every change after the `v2026.4.1014` tag (`4a55440`) that a consumer can see; this repository's own
working documents are left out. A change that makes a rule stricter or adds API goes here with its
effect on a consumer, so the release that carries it can say so. `main` takes squash and rebase
merges only, and both give a branch's commits new hashes, so a row cites its pull request.

| Commit or PR | Change | Effect on a consumer |
|---|---|---|
| PR #79 | `BNXQ1003`, by the developer's assessment of 2026-10-14, now that its reading is public. A name that more than one top-level type the scan was given carries is no control's: the rule reads a theme's target by its name alone, so each theme of such a name is named in `Skipped`, as `BNXQ1006` names one, rather than checked against both classes' parts, and so is the name where parts were found in its types' code and nothing themes it. The reading has no entry for it and names it in its own `Skipped`, where before it merged both classes' parts into one entry, as plan 00002's decision 5 had it. A template is still followed through direct calls only, as the developer decided, and the two skips that leaves, a themed control with no part found and parts with no theme, now name a call through an interface or a virtual method, and a lambda, among their causes. A `PART_` constant nothing looks up is still checked against its control's theme, as plan 00002's decision 9 left it, by the developer's decision too | Where two scanned types carry one name, its themes are skipped rather than checked, so the findings they gave go. Some were false: they compared one class's theme with both classes' parts. Some were true: where only one of the two has parts, as a control that shares its name with a model, PR #79's review measured a true finding lost, and the developer kept the skip. A themed name that partless types share is skipped as such, where it was a control with no part found. The parts report loses that name's merged entry and names it among what was not read, so `theme-audit parts` counts one more not read. Two skips read differently. On real code nothing else moves: over DiffView's `bf6b734`, AvaloniaEdit's assembly included, the parts report is as before byte for byte, and the real-code record changes only in the wording of DiffView's two skips |
| PR #80 | `docs/avalonia-gotchas.md` gains a section on logging, with three entries from the WPF-to-Avalonia port, landed as the port measured or read them and not measured again here, by the developer's decision of 2026-10-14, each saying it was not reproduced here: `LogToTrace()`, the project template's default, writes Avalonia's warnings nowhere on Linux, where with no debugger attached the only trace listener has no file; and `LogToTrace`, `LogToTextWriter` and `LogToDelegate` all drop the level, since all three go through `StringLogSink`, so the fix is an `ILogSink` of the application's own; and a compiled binding through a null intermediate logs `Value is null` for every binding on every container where it is null, which a null-conditional step, `?.`, stops, a value-type target with no converter then needing a `TargetNullValue`. The port's sink is described as the case measured: 13 headless tests held it, and in the port's running application its first run logged 352 warnings no run had seen. The first entry carries a candidate rule, the one the trim analyser's entry already names, and the third a candidate of its own | Docs only |

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
other, by the developer's decision of 2026-10-13. One from the WPF-to-Avalonia port lands as the port
measured it, without being measured again here, its own marks of measured and inferred kept and its
entry saying it was not reproduced here, by the developer's decision of 2026-10-14. One that needs an
ARM64 machine is not reproduced here, and stays out of the documents, by the developer's decision of
2026-10-12.

| Arrived | From | Request | State |
|---|---|---|---|
| 2026-09-28 | DiffView | Five lessons from its plan 00029: raising a menu item's `Click` does not toggle it, key bindings run before `KeyDown`, and three facts about driving X11 | Received, and a receipt sent on 2026-10-10. Not checked |
| 2026-09-30 | DiffView | A headless hit-test reads the last composed frame, not the layout | Received, and a receipt sent on 2026-10-10. Measured on 2026-10-13, headless on Avalonia 12.1.3 and on a real window in Windows Sandbox, and landed in PR #59: it holds on a real window too, until the next frame is composed, in 10 rounds of 10; headless, `CaptureRenderedFrame()`, or `RunJobs` followed by a forced render tick, settles it, and neither `RunJobs` nor a forced tick alone does. The outcome went to DiffView on 2026-10-13 |
| 2026-10-03 | DiffView | A public `BNXQ1003` reading: per control, the parts it looks up as distinct from the `PART_` constants it only declares, and which code makes each lookup | Accepted. [Plan 00002](plans/00002-template-part-reading.md), approved on 2026-10-13, takes it in two steps: the reading, with the parts each theme declares, then a parts report the theme audit writes and checks. Step A, the reading, landed in PR #70, and step B, the parts report and its drift check, in PR #72. `2026.4.1014` carries both, released on 2026-10-14, which DiffView's plan 00030 was waiting for, and DiffView was told that day. The assessment of `BNXQ1003` that the developer's order of work of 2026-10-13 put after plan 00003 was decided on 2026-10-14, in PR #79 |
| 2026-10-08 | DiffView | Twelve findings from driving an Avalonia app on Windows through UI Automation | Received, and a receipt sent on 2026-10-10. Items 1, 2, 3, 5, 6, 7, 8 and 11 measured in Windows Sandbox on 2026-10-13, on Avalonia 12.1.3: 1, 2, 3, 5 and 11 hold; 6 holds for a process that has sent no input, refused in the second session, but not for one that has just sent input, which took the foreground in both; 8 is narrowed, keys reaching a window with nothing focused but only its own bindings; 7 did not reproduce, in either session, and is left out. Items 1 and 2 landed in PR #59; 3, 5, 6, 8 and 11 in the guide in PR #60, 3 as a sentence. 4, 9, 10 and 12 were not measured here. Its suggestion of a layer for a by-hand driver with a person watching landed in section 4 in PR #67, by the developer's decision of 2026-10-13. The outcome went to DiffView on 2026-10-13 |
| 2026-10-08 | The WPF-to-Avalonia port | A correction to the render-tier entry, the cost of the software renderer, and a headless click that lands where a control used to be | Received, and a receipt sent on 2026-10-10. The correction measured on x64 in Windows Sandbox on 2026-10-13, and landed in PR #65, changed: with no GPU the guest's adapter 0 was the Basic Render Driver and Avalonia drew through ANGLE on WARP, and the fix tells that adapter by its name, or its vendor and device, as well as the software flag, which two of its three entries in the guest lacked. The correction's ARM64 rule and measurement, and the renderer's cost, measured on ARM64 alone, are left out, by the developer's decision of 2026-10-12; its x64 comparison of a forced `Software` against the default, made with a GPU and not repeated here, is not landed either, since the entry is about telling software rendering apart, not about its cost; on 2026-10-14 the developer kept both out again. The outcome went to the port's live session on 2026-10-13, not to the retired one that sent this, by the developer's decision of that day. The headless click landed in PR #75 as the port measured it, not reproduced here, by the developer's decision of 2026-10-14 |
| 2026-10-09 | DiffView | Two driving lessons: a drag's success is not its effect, and a menu's automation window is not the popup it is drawn in | Received, and a receipt sent on 2026-10-10. Measured in Windows Sandbox on 2026-10-13: the drag's holds; the menu's is changed, its items being under the main window's element and nearest the popup's window, which is where they are drawn, alike for a menu in XAML, one built in code and a context menu. Both landed in the guide in PR #60, the menu's as changed. The outcome went to DiffView on 2026-10-13 |
| 2026-10-09 | Issue #48 | Two AT-SPI bridge behaviours: a list keeps the children it first reported, and a list's scroll bars are not in the tree | Measured on Linux on 2026-10-13, and both landed with their fixes in PR #57, each narrowed: a list keeps the nodes it first reported, which a scroll renames after the rows their containers hold, and its scroll bars are missing only where it had rows when first asked. The bars' fix gains a warning, measured: a bar written through it stops following the list. Where the surfaced bars sit in the tree, which depends on the application's own layout, is left out. The outcome went to the port's live session on 2026-10-13, by the developer's decision of that day |
| 2026-10-09 | Templates | Trimming, side 2: when a repository trims, every `Bennewitz.Ninja.*` package in its restore graph carries `IsTrimmable` | Accepted, and the approach agreed on 2026-10-10; waits for Templates' side 1 to merge |
| 2026-10-10 | DiffView | Three lessons for the guide: a harness that counts silence as success, the automation patterns of stock template parts, and asking before borrowing the desktop | Received, and a receipt sent on 2026-10-10. The second measured in Windows Sandbox on 2026-10-13: it holds but for the `ExpandCollapse` it reported on a `MenuItem`, found only on the window's System menu item, which is Windows' own; and a checkable `MenuItem` offers no Toggle pattern on Windows, a defect in Avalonia's automation that a peer of the application's own works around, measured headless and, in the second session, on a real window. That part landed in PR #59, and the survey in the guide in PR #60. The first landed there too, as method: a run that stops early is not a pass. The third the guide already says, in sections 3, 4 and 7. The outcome went to DiffView on 2026-10-13 |
| 2026-10-12 | Issue #55 | Four more AT-SPI bridge behaviours, two X11 window findings, and a correction to the guide: driving over AT-SPI is verified | Measured on Linux on 2026-10-13, and landed in PR #57, with changes: `SelectChild` adds only where the list allows more than one selection; an open combo's items are missing from its children for the reason a list's late rows are, the bridge keeping the children it was first asked for, so the two are one entry; the move's fix may instead lay the window out before moving it, measured, and the frame's waits for the frame to change rather than polling, measured through `FrameSizeProperty`; the active frame's fix names `_NET_ACTIVE_WINDOW` and the bridge's own events rather than `_NET_WM_STATE_FOCUSED`, none of them measured, since no window became active in the headless session; and the move's claims about Windows are left out, unmeasured. The outcome went to the port's live session on 2026-10-13, by the developer's decision of that day |
| 2026-10-13 | The WPF-to-Avalonia port | Avalonia.Wayland 12.1.3 and 12.1.4 do not start the AT-SPI bridge, so a window on the Wayland backend has no accessibility tree; issue #55's verification scoped to the X11 backend; and four facts about the Wayland backend: window methods that do nothing, a screen scaling fixed at 1.0, two options it lacks, and no attention request | Received, and a receipt sent on 2026-10-13. The first two measured on 2026-10-13, on Avalonia 12.1.3 in a headless GNOME session under WSL: a window on the Wayland backend, which only `UseWayland()` or `UseWaylandWithFallback()` chooses, opened without the bridge's assembly loaded and did not appear on the AT-SPI bus in 20 seconds of watching, where one on X11, which `UsePlatformDetect()` chooses on Linux, appeared there with its children, in 3 runs of 3 each; in the source at the tag only `Avalonia.X11` references the bridge. 12.1.4 not measured. The first two landed in PR #64 for 12.1.3: the first as an entry of the gotchas, the second as the guide's Linux paragraph and the environment probe's verdict under Wayland, scoped to the X11 backend. The outcome went to the port's live session on 2026-10-13. The four facts landed in PR #75 as the port read them in Avalonia.Wayland 12.1.3's package, not reproduced here, by the developer's decision of 2026-10-14 |
| 2026-10-13 | DiffView | Two follow-ups to its earlier lessons. The X11 tooltip re-measured: `xdotool mousemove` raised one in ten trials of ten, on Avalonia 12.1.2 under GNOME Shell 48.7 with Xwayland 24.1.6, where the `xdotool` entry says a tooltip never appears; the same Avalonia had raised none on 2026-09-13, for a reason not found. And item 7 of 2026-10-08 back with a qualifier: raising a window closes a menu it opened when the raise changes the foreground | Received, and a receipt sent on 2026-10-13. The qualifier measured in Windows Sandbox on 2026-10-13, on Avalonia 12.1.3, and landed in PR #73, changed: when another application took the foreground, by a call or by a click, the menu closed at once, before any raise, so raising the window back found it closed; a window holding the foreground kept its menu open through four ways of raising it, and a click on its own title bar closed it. No way was found to leave a menu open on a window without the foreground; a window covered by an always-on-top one, which keeps the foreground, was not measured. The outcome went to DiffView on 2026-10-13. The tooltip is not checked |
| 2026-10-13 | The WPF-to-Avalonia port | Two headless findings on Avalonia 12.1.3 and Semi.Avalonia 12.1.0.1: a `ScrollContentPresenter` loses a wheel notch when an `ILogicalScrollable`'s position moved before it raised `ScrollInvalidated`, in 7 runs of 30 of a test that turned the wheel straight after `RunJobs`; and Semi's `SplitView` pane slides open, so a fixture that measures right after setting `IsPaneOpen` reads it part way. A correction the same day withdrew the sentence that a render tick before the notch prevented the loss | Received, and a receipt sent on 2026-10-13. Both landed in PR #75 as the port measured them, the withdrawn sentence left out, not reproduced here, by the developer's decision of 2026-10-14 |
| 2026-10-13 | The WPF-to-Avalonia port | Two findings, headless on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1. A subclass that borrows its base's theme through `StyleKeyOverride` is not selected by a style that names its own type, only by one that names the base, its cause inferred and not read. And Semi's `TextBox` template puts `InnerRightContent` in an `Auto` column beside the text's star column, so wide inner content squeezes the text to nothing: a 183 px block left the text 0 px wide when the box was offered 159 to 220 px | Received, and a receipt sent on 2026-10-13. Both landed in PR #75 as the port measured them, the first one's cause as inferred, not reproduced here, by the developer's decision of 2026-10-14 |
| 2026-10-13 | The WPF-to-Avalonia port | Four findings, headless on Avalonia 12.1.3 with Semi.Avalonia 12.1.0.1. Semi's `TextBox` pads its `InnerLeftContent` whenever the content is not null, a hidden one included, by `TextBoxInnerLeftContentPadding`, and its `InnerRightContent` likewise; Semi's `Button` sets a `MinHeight` of 32, which wins over a local `Height`; a `MeasureOverride` decision taken from a template's inner layout is not revisited when content inside the template changes and the control's desired size does not; and measuring a template twice inside one `MeasureOverride` after changing a descendant's `MaxWidth` changes nothing unless every visual ancestor between is invalidated too. All four measured, the last two with their causes inferred | Received, and a receipt sent on 2026-10-13. Sent again on 2026-10-14, and a second receipt sent. All four landed in PR #75 as the port measured them, the last two causes as inferred, not reproduced here, by the developer's decision of 2026-10-14 |
| 2026-10-13 | DiffView | Two lessons for the guide's section 5, from verifying its click guard on a real desktop. A one-off harness written beside a DPI-aware driver was itself unaware, and at 200% fed a 2200x1440 window's virtualised 1113x756 back into `SetWindowPos`. And a negative result about a guard is evidence only once the condition it guards against is shown present: measure `WindowFromPoint` at the click point first, and report a run whose premise failed as unread, never as a failure | Received, offered to use or discard. Not checked |
| 2026-10-14 | The WPF-to-Avalonia port | Two findings on Avalonia 12.1.3 and .NET 10, headless on Linux: `LogToTrace()`, the project template's default, writes Avalonia's warnings nowhere, since with no debugger attached the only trace listener has no file, and `LogToTrace`, `LogToTextWriter` and `LogToDelegate` all drop the level. Later the same day, the fix it made, a sink of its own held by 13 headless tests, each seen to fail against a deliberate break of it; then that sink in its running application, under Xwayland, whose first run logged 352 warnings no run had seen, and where a `{ReflectionBinding}` to a missing property, planted on a window, built green and failed the UI run on its log. That evening a third, measured in the same application and a headless fixture: a compiled binding through a null intermediate logs `Value is null` for every binding on every container where it is null, which `?.` stops, but for a value-type target with no converter, which then needs a `TargetNullValue` | Received, and a receipt sent on 2026-10-14. All three landed in PR #80 as the port measured or read them, its fix and its running application's measurement with them, not reproduced here, by the developer's decision of 2026-10-14, but for two things read here in the source at 12.1.3. The port read `StringLogSink` writing an event's source as `(Type #hash)`, and it writes a named element's as `(Type #Name)`, so the entry says only that the source is written in parentheses. And the port's rule of a `TargetNullValue` for any value-type target with no converter gains a line, not measured, that a `Nullable<T>` target takes the null, as `TypeUtilities.AcceptsNull` says. The developer put two rule candidates on the backlog the same day, below, one from the first finding and one from the third. The outcome went to the port's live session on 2026-10-14 |

## Rule backlog

Ids are permanent, and the README's rules table is generated from them, so an id is assigned only
when a rule is written.

The rules were written in the order decided on 2026-09-23, and all of them are now: `BNXQ1005`,
`BNXQ1006`, the peer check, `BNXQ1007`, the explicit `AutomationId`, `BNXQ1008`, the zero-size slot,
and `BNXQ1009`, the `ToString()` containers. One candidate came in since, the row that takes no
click where it draws nothing, from the gotchas entry on a row whose `Background` is null, and it is
written as `BNXQ1010`, by the developer's decision of 2026-09-28. Two candidates are open:

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

**Not a fit:** the guide's rule 10, keeping popups in the window's tree. Avalonia's `OverlayPopups` is
set in C# at startup, where no markup scan can see it. That check belongs in the application, as a
startup assertion or a headless test. Nor is the `LogToTrace()` call the logging entries warn
against, by the developer's decision of 2026-10-14: it is C# start-up code too, and the family's
template already leaves it out.

## Questions for the developer

None open.

## Follow-ups

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
