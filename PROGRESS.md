# Progress

The work state of `Bennewitz.Ninja.XamlQuality`: what is published, what is on `main` and not yet
released, who depends on what, and the rule backlog with any questions open for the developer.

## Status

| | |
|---|---|
| Published | `2026.3.928`: `Bennewitz.Ninja.XamlQuality` and `Bennewitz.Ninja.XamlQuality.ThemeAudit`, released 2026-09-28. What it changed for a consumer is in its [release notes](https://github.com/JanusMael/Bennewitz.Ninja.XamlQuality/releases/tag/v2026.3.928) |
| `main` | carries `BNXQ1010`, a new rule, and the docs changes listed below |
| Next release | Once `BNXQ1003`'s public reading and `BNXQ1009`'s `ItemsSource` follow-up have landed, not on `BNXQ1010` alone, by the developer's decision of 2026-10-12. One release per calendar day |
| Agent skill | `drivable-ui`, served from `main` as a Claude Code plugin since PR #47, by the developer's decision of 2026-09-29: `claude plugin marketplace add JanusMael/Bennewitz.Ninja.XamlQuality`, then `claude plugin install drivable-ui@xamlquality`. It is in neither package, so it ships on merge, not on a tag |

### On `main`, not yet released

Every change after the `v2026.3.928` tag (`067a509`) that a consumer can see; this repository's own
working documents are left out. A change that makes a rule stricter or adds API goes here with its
effect on a consumer, so the release that carries it can say so. `main` takes squash and rebase
merges only, and both give a branch's commits new hashes, so a row cites its pull request.

| Commit or PR | Change | Effect on a consumer |
|---|---|---|
| PR #43 | `docs/avalonia-gotchas.md` gains an AvaloniaEdit entry from DiffView's session: a zero-length element that a host's generator places at a line start is lost where a built-in generator claims the line's first character, because `TextView`'s constructor appends the built-ins first and the first element with a length ends a round of construction. Checked in AvaloniaEdit's source at the `12.0.0` tag, and measured headless on Avalonia.AvaloniaEdit 12.0.0 with Avalonia 12.1.3. The cause held as sent, and the loss is wider: under the default options, lines beginning with a URL or an e-mail address lose the element too | Docs only |
| PR #47 | `docs/ai-drivable-ui.md` says how an agent loads the method as a skill: this repository now serves the `drivable-ui` skill, moved from bb-skills, as a Claude Code plugin from `main` | Docs only. The plugin is in neither package; it reaches a machine through `claude plugin marketplace add JanusMael/Bennewitz.Ninja.XamlQuality`, then `claude plugin install drivable-ui@xamlquality` |
| PR #49 | `docs/avalonia-gotchas.md` corrects its entry on a control's own `Styles`, which said they never match the control. At 12.1.3 `StyledElement.ApplyStyles` applies each ancestor's styles and then the control's own, and measured headless under Fluent, Simple and Semi, a `Border` took the `Background` its own `Border.Styles` gave it. The entry is now "A control's `Styles` apply to the control itself, as well as to its descendants" | Docs only. The heading changed, so TailBlazer and OpenForge2k, which cite the entry, are told |
| PR #50 | `BNXQ1010`, `ItemContainerBackgroundRule`: every list, combo or tab row generated from `ItemsSource` draws a background of its own: its template draws one, and where it draws the row's `Background`, that is never null. It reports rows whose `Background` can be null where their template draws it, a style or theme that sets `{x:Null}`, a binding with no non-null `TargetNullValue`, or a theme `BasedOn` nothing that sets none where the row's type registers no brush, and rows whose template draws no background at all. What draws the row's background is a `Border`, a panel, a `ContentPresenter` or a `TextBlock` taking it, by a `TemplateBinding` or a binding to the row, counted from the nearest ancestor of the type it names in a `RelativeSource` that finds an ancestor, a shape's `Fill` taking it, or a templated control taking it whose own template draws it, where the `Background` a type's own markup, or a base's, gives it, found by its `x:Class`, outranks a template's but not a binding at its usage; what a `ContentPresenter` or a `TransitioningContentControl` in the template holds as markup content is not followed; a theme that sets no template leaves the framework's or `ContentControl`'s default, both of which draw it. What the rows present decides nothing, nor what hides an element, how much of a row it covers, or what a `Popup` holds, by the developer's decision of 2026-10-04: a row whose content paints across it is still reported. A style whose selector holds of every row whatever the data sets them all, and a `:not()` of a state may not; `:disabled` may hold from the start wherever markup in the scan sets `IsEnabled` to anything but `True` or gives a control a `Command`, in any spelling, by the developer's decision of 2026-10-08, and `:selected` where a list, its theme or its type always selects a row or markup may select one, when it paints only the rows selected; a value a template's markup gives an element ranks below a style with a condition and below its own code, and a style that binds a part of the template may draw there. What markup cannot decide is named in `Skipped`. It leaves tree rows out. The gotchas entry on a null row background points at it | A new rule, so nothing changes until a consumer constructs it. On TailBlazer's Avalonia port it reports the row theme that the port's `e8e851c` fixed, and nothing after the fix, inspecting 12 lists each time; on ClaudeForge `70dc713` it inspects 19 and reports nothing. `BNXQ1009`'s selector and peer reading moved to shared helpers unchanged, and its output on both is identical to `main`'s, as are `BNXQ1004`'s, `BNXQ1005`'s and `BNXQ1008`'s |
| PR #50 | `BNXQ1005` and `BNXQ1009`, through the `ResourceScope` and `ThemeReader` they share with `BNXQ1010`, read a property in every spelling the XAML compiler takes, by the property its owner names: a setter's name alone, after an owner or in parentheses, in its `Property` attribute or a `Setter.Property` element; and an element's own as an attribute or a property element, where two spellings that set it or may decide nothing, because the framework applies each in turn. An owner is the type its prefix names, found by its name only in the framework's own namespace, Avalonia's, or WPF's or MAUI's in markup written for that framework; one in a URI that only an assembly's `XmlnsDefinition` maps is not decided. On an element, the control's type or a base names its own, and another owner the property held by the field the compiler takes for it: after an owner with an instance property of the name only a binding sets it, and after one with static `Set` or `Get` accessors any value does. In a setter, a `TemplateBinding` or a path, an owner names what its most-derived public field holds, the control's own type included, and where two types in the control's chain carry its name, it is decided only where they hold one property. A theme, named or implicit, applies only where its `TargetType`, the type its prefix names, is the control's style key or a base of it, and a style key a `StyleKeyOverride` returns other than by a `typeof` is not read, and `BNXQ1009` takes only Avalonia's `AutomationProperties` to name a row. Where an owner or a field cannot be read, a value after another owner may or may not be a binding, or an owner or a theme is not decided, the rule says so in `Skipped` | Either rule's answer changes only where markup uses one of these spellings or such a theme. A theme or a style that set `Focusable`, `ItemContainerTheme`, `ItemTemplate`, `DisplayMemberBinding`, `AutomationProperties.Name` or a `Theme` so was read as setting nothing, and is now followed; a list's `ItemsSource` or `ItemContainerTheme`, or a `TextBlock`'s `Text`, bound after another owner that holds it, as `ComboBox.ItemContainerTheme` on a list, or given after a consumer's attached alias of it, is now the element's own; a setter or a property element after an owner whose attached property it names, or after a subclass that hides the field, read as the control's own before, now is not; a consumer's own `AutomationProperties` no longer names rows; and an element that gives one property two ways, an owner that cannot be read or sits in a consumer's own URI, WPF's or MAUI's in Avalonia's markup included, one that two types in the control's chain are called after and that hold different properties, and a theme whose `TargetType` is not the control's style key are named in `Skipped`. So a finding can appear or go. On TailBlazer and ClaudeForge, which write none of them, both rules' output is identical to `main`'s. No consumer in the family runs either rule |

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
  2026-09-24. TailBlazer retired its copy of the guide (`8e46a8f`) and points its `CLAUDE.md` at both
  documents here. Templates' `PROGRESS.md` links the gotchas. ClaudeForge retired its pre-move
  `docs/AVALONIA-GOTCHAS.md` and repointed its links here in JanusMael/ClaudeForge#80, and its UI
  style guide's section 14 points here since JanusMael/ClaudeForge#87. The drivable-ui skill, which
  condenses the guide, lives here too since PR #47, moved from bb-skills, whose `feat/drivable-ui`
  branch the same decision retired once it merged; its history is kept there as the tag
  `archive/feat-drivable-ui`. Moving either file, or retitling a section or entry someone cites,
  means telling them, TailBlazer excepted: by the developer's decision of 2026-10-12 it is told
  nothing more.
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
measured in Windows Sandbox, a desktop of its own, in a batch launched when the developer says so; one
that needs X11 or an ARM64 machine is not reproduced here, and stays out of the documents. Both are
the developer's decisions of 2026-10-12.

| Arrived | From | Request | State |
|---|---|---|---|
| 2026-09-28 | DiffView | Five lessons from its plan 00029: raising a menu item's `Click` does not toggle it, key bindings run before `KeyDown`, and three facts about driving X11 | Received, and a receipt sent on 2026-10-10. Not checked |
| 2026-09-30 | DiffView | A headless hit-test reads the last composed frame, not the layout | Received, and a receipt sent on 2026-10-10. Not checked |
| 2026-10-03 | DiffView | A public `BNXQ1003` reading: per control, the parts it looks up as distinct from the `PART_` constants it only declares, and which code makes each lookup | Accepted; second in the code lane, after the verification tooling |
| 2026-10-08 | DiffView | Twelve findings from driving an Avalonia app on Windows through UI Automation | Received, and a receipt sent on 2026-10-10. Not checked |
| 2026-10-08 | TailBlazer | A correction to the render-tier entry, the cost of the software renderer, and a headless click that lands where a control used to be | Received, and a receipt sent on 2026-10-10. Not checked. What lands gets no reply: TailBlazer is told nothing more, by the developer's decision of 2026-10-12 |
| 2026-10-09 | DiffView | Two driving lessons: a drag's success is not its effect, and a menu's automation window is not the popup it is drawn in | Received, and a receipt sent on 2026-10-10. Not checked |
| 2026-10-09 | Templates | Trimming, side 2: when a repository trims, every `Bennewitz.Ninja.*` package in its restore graph carries `IsTrimmable` | Accepted, and the approach agreed on 2026-10-10; waits for Templates' side 1 to merge |
| 2026-10-10 | DiffView | Three lessons for the guide: a harness that counts silence as success, the automation patterns of stock template parts, and asking before borrowing the desktop | Received, and a receipt sent on 2026-10-10. Not checked |

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

- **The verification tooling moves into the repository**, first in the code lane, by the developer's
  decisions of 2026-10-09 and 2026-10-10, as [`plans/00001`](plans/00001-verification-tooling.md),
  approved on 2026-10-12. The real-code comparison, the mutation runner and the headless probe move
  under `tools/`, one command each, in that order and a pull request each, and a required CI job,
  `Verification tools`, runs the real-code comparison over ClaudeForge, DiffView and ScopedEditors.
  None of the plan's steps 2 to 4 has landed, so the tools still live in working sessions' folders.
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
  Fluent's combo and tab rows carry one, TailBlazer's real finding becomes a skip, and the markup
  that makes `:disabled` a start state there is a `KeyBinding`'s `Command`, which disables nothing.
  Two narrower readings were set aside, each a change of its own since each can add findings:
  - Count a `Command` only on a control whose type overrides `IsEnabledCore`, as `Button`,
    `SplitButton` and `MenuItem` do at 12.1.3, or a consumer's own does, and on a type that does not
    resolve. Still sound, and a skip would no longer cite a `KeyBinding`; it recovers no finding on
    TailBlazer or ClaudeForge, since both bind `IsEnabled` on buttons and panels.
  - Read `IsEnabled` and `Command` only where markup can be a visual ancestor of the rows or of a part:
    the list and its ancestors, its items panel and template, the rows' template, where its view is
    used by its `x:Class`, and setters anywhere, falling back to anywhere where a view's placement
    cannot be traced. It recovers TailBlazer's finding under the plant; a view shown through a data
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
