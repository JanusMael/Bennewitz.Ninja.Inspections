# Bennewitz.Ninja.XamlQuality

Static analysis for XAML markup quality — accessibility names, theme-token discipline, resource
keys a theme leaves undefined, contrast floors, and the conventions a XAML codebase drifts away
from silently.

| Package | What it is |
|---|---|
| `Bennewitz.Ninja.XamlQuality` | The **rules library**. Zero dependencies |
| `Bennewitz.Ninja.XamlQuality.ThemeAudit` | The **`theme-audit` CLI**, for CI and for generating theme-compat dictionaries |
| `Bennewitz.Ninja.XamlQuality.ThemeRuntime` | The **runtime theme check**, as source files an Avalonia test project compiles: the audit's answers read against Avalonia's own resource lookup |

## Why a library and not a test package

Each rule takes a scan context and returns findings. Nothing in the library references a test
framework, so the same rules drive MSTest, xUnit, a CLI, or an MSBuild target — the consumer
chooses the runner, and the severity.

```csharp
var context = XamlScanContext.Load("src");
var result  = new ExpanderAutomationNameRule().Analyze(context);

Assert.AreEqual(0, result.Findings.Count, string.Join("\n", result.Findings));
Assert.AreNotEqual(0, result.Inspected, "The rule matched nothing at all — it is inert.");
```

That second assertion is the point of `Inspected`. A rule whose selector stops matching — a
renamed element, a changed namespace, a folder that moved — returns zero findings and looks
exactly like a clean codebase. Findings alone is a number that cannot rise, so it can never fail.

## No Avalonia dependency, on purpose

Every rule reads markup as text or as XML, so this works against WPF and MAUI XAML as well as
Avalonia's. A quality analyser that drags dependencies into a consumer's test project is one
people stop referencing.

The one check that needs a running Avalonia, the runtime theme check, ships as source files that
compile against whatever Avalonia the adopting project pins, so nothing published here references
Avalonia either.

## Rules

<!-- BEGIN GENERATED RULES -->
| Id | Requires |
|---|---|
| `BNXQ1001` | Every Expander declares AutomationProperties.Name. |
| `BNXQ1002` | Every interactive control declares AutomationProperties.Name. |
| `BNXQ1003` | Every template part a control looks up is declared in its own theme. |
| `BNXQ1004` | Every control fits the fixed Grid slot it is placed in. |
| `BNXQ1005` | Every key binding on an items control sits where keyboard focus can reach it. |
| `BNXQ1006` | Every themed custom control gets an automation peer, or declares in its own code that it has none. |
| `BNXQ1007` | Every interactive control declares an explicit AutomationId. |
| `BNXQ1008` | Every control in a zero-size Grid slot is hidden by IsVisible, not by the slot alone. |
| `BNXQ1009` | Every item container generated from ItemsSource is named by what it shows, not by its item's type. |
| `BNXQ1010` | Every list, combo or tab row generated from ItemsSource draws a background of its own: its template draws one, and where it draws the row's Background, that is never null. |
<!-- END GENERATED RULES -->

The table above is rendered from the rule types — `Id` and `Summary` on each `IXamlRule` — and a
test fails if it drifts. Regenerate it with `XQ_UPDATE_DOCS=1 dotnet test --solution
XamlQuality.slnx` and commit the result. Everything below the markers is written by hand, because
none of it is a property of any single rule.

**The ids carry the family's prefix.** Every id is `BN`, for the family, then `XQ`, this product's
initials, then its number. Up to `2026.3.924` the ids shipped as `XQ1001` to `XQ1004`: only the
prefix changed, and `BNXQ1005` and `BNXQ1006` never shipped under the old one. A filter or
suppression keyed on an old id stops matching until it names the new one. The prefix does not
change again.

**Run both.** `BNXQ1002` deliberately leaves `Expander` out of its element set, because both rules
firing on one element would report a single defect twice. So `BNXQ1002` alone is not a superset:
adopt it on its own and Expanders go unchecked — which is the failure this library exists to
catch, a gate that reads as comprehensive and is not.

`BNXQ1002` covers a curated framework set, not every focusable type in any one framework. Pass your
own control names to the constructor — a control you wrote is exactly the one no framework list
will ever name, and leaving it out means the rule reports clean over the markup least likely to
have been reviewed. Assert on `Inspected` to tell that apart from a clean result.

**`BNXQ1003` reads part names from compiled code.** A control's parts are the `PART_` names it
declares as string constants, public or not, and the `PART_` string literals its code passes to a
lookup: a call whose name starts with `Find` or `Get`, such as `NameScope.Find("PART_Foo")` or
`GetTemplateChild`. Lambdas and controls that are not public are read too. A lookup made on a template the control hands
on, to a helper, down a chain of calls or into a constructor, or made by a base class's
`OnApplyTemplate`, is the control's, and is checked against its theme. A literal passed to
anything else is ignored, which is what keeps compiled XAML out: it passes every element name to
`set_Name` and `Register`. Four limits. A name built at runtime (`"PART_" + name`) is invisible,
and so is a lookup through a helper named otherwise. A lookup no control hands its template to is
taken to be on its own type's template, so one that reaches into a child control's template is
reported against that type. And a template is followed through direct calls only: code it reaches
through an interface or a virtual call, or in a lambda another type declares, keeps its lookups as
that type's, and the skips that name a control with no part found, or parts with no theme, say so.

A name that two scanned types carry, in different namespaces, is no control's to `BNXQ1003`. The
rule reads a `ControlTheme`'s target by its name alone, not by the namespace its prefix gives, so it
cannot say which of them a theme is for. It names each such theme in `Skipped`, as `BNXQ1006` does,
rather than check it against both classes' parts. That holds where only one of the two has parts,
such as a control that shares its name with a model, so a check the theme's prefix would allow is
skipped too.

**`BNXQ1003`'s reading is public.** `TemplatePartRule.ReadParts(context)` returns what the rule reads
from the same scan: for each control, the `PART_` constants it declares, each part looked up on its
template with the code that looks it up, the control's own, a base class's or a helper's it hands its
template to, by type, method and assembly, and the parts each of its themes declares, with the element
and the automation id. The rule's findings are exactly the constants and looked-up parts a theme does
not declare, so a parts reference built from the reading lists what the rule checked, in an order that
compares as text. A constant nothing looks up, which a style selector may be all that needs, is listed
apart from the lookups. A name two scanned types carry has no entry, and the reading's `Skipped`
names it. A base class in an assembly the scan was not given is not read: pass the library that
defines it to `WithAssemblies` as well.

**`BNXQ1004` counts only what it measured.** A control in an `Auto` or `*` row or column, or one that
declares no size, has nothing to measure, so it is neither inspected nor skipped. Markup whose grids
are all `Auto` and `*` therefore reads zero inspected, and that is a true answer, not an inert rule:
a floor on `Inspected` suits markup with fixed rows or columns, or a fixture that has them.

**`BNXQ1005` needs the assemblies the markup belongs to.** A key binding fires only while focus is on
its control or inside it, so the rule asks, of every key binding on an items control, whether
anything in that path can take focus: the control, its item containers, or what its item template
puts in them. Whether a type takes focus by default, which container an items control generates,
and whether an element presents items at all are read from compiled types. Pass the application's
own assembly to `WithAssemblies`; the framework is reached through its references. The rule reports
only what it can decide, and key bindings on other controls are not checked.

**`BNXQ1006` is what makes the names count.** A name `BNXQ1001` or `BNXQ1002` requires on a custom control
reaches nobody when the control has no automation peer: the framework's default gives an empty one,
which a control-view search never finds. So the rule asks, of every control that a `ControlTheme` in
the scan themes and the scanned assemblies define, whether anything in its chain overrides
`OnCreateAutomationPeer`: the control itself, a base of your own, or a framework base that gives a
peer, as `Button` does and `TemplatedControl` and `ContentControl` do not. A decorative control opts
out by overriding it to return the empty peer, which says so in the control's own code. Pass the
application's assembly to `WithAssemblies`, from a process that can load its framework.

**`BNXQ1007` is the id a test searches by.** `BNXQ1002` asks for the name a person reads; this asks
for the `AutomationProperties.AutomationId` a test or an agent finds a control by, in either
spelling, or MAUI's plain `AutomationId`. An `x:Name` does not count, although Avalonia derives an id
from it: renaming the field would rename what every test searches for. An empty or blank id is
reported as empty, because on Avalonia it replaces the id the framework would have derived. The rule
covers `BNXQ1002`'s framework set and `Expander`, which no other rule checks for an id, and takes
your own control names as `BNXQ1002` does. It checks that an id is present, not that it is unique: a
control in an item template declares one id for every row.

**`BNXQ1008` is the other half of `BNXQ1004`.** A control that asks for a size in a slot too small
for it is `BNXQ1004`'s. One that asks for none, in slots that add up to no size, is this rule's: a
zero-height row or zero-width column hides it from people and not from the automation tree, where a
test or an agent still finds it. Hide it with `IsVisible`, which a binding, a `False` or WPF's
`Visibility` satisfies and an explicit `True` does not. A slot's size is read as the framework
arranges it: its own `MinHeight` wins, a `MaxHeight` of 0 empties it, a star of weight 0 gets
nothing, and a span has the spacing between its slots.

**`BNXQ1009` asks what names the rows a list generates.** A `ListBoxItem`, `ComboBoxItem` or
`TabItem` generated from `ItemsSource` takes the text of a `TextBlock` at the root of its item
template, else a name that root declares, else `ToString()` on its item: a view model that writes
none names every row with its type's full name, and a record with its type and every property. A
`TextBlock`'s own `AutomationProperties.Name` does not count, because its peer reads its text alone.
A `TreeViewItem` has no fallback, so whatever its template shows, it stays unnamed until a `Style`
for it or its `ItemContainerTheme` sets `AutomationProperties.Name`. With no item template, a row
shows what the data template chosen by its item's type presents, from the list's `DataTemplates`,
an ancestor's or the application's, else its item's `ToString()` in a `TextBlock`. The rule reads
the items' type from the item template's `x:DataType`, else from the `ItemsSource` binding's path,
as the XAML compiler types it through the `x:DataType` in scope, and which container a control
generates, and which peer names it, from compiled types: pass the application's assembly to
`WithAssemblies`. It checks
Avalonia's list, tab and tree rows, whose naming was measured on 12.1.3, and leaves another
framework's alone.

**`BNXQ1010` asks whether a list's rows draw a background of their own.** A control is hit-tested
where it draws, and a null `Background` draws nothing, so a row whose background is null takes a
click only where something it holds draws, and a click elsewhere reaches the list behind. Stock rows
are `Transparent`, which draws. The rule reports two things markup decides about the rows' own
template. First, rows whose `Background` can be null where their template draws it: a row style or
theme that sets `{x:Null}`, or a binding with no non-null `TargetNullValue`, which answers null
wherever its source or its converter does, or a row theme `BasedOn` nothing that sets no background,
where something in the template draws the row's background. Second, a row template that draws no
background at all. What draws the row's background is a `Border`, a panel, a `ContentPresenter` or a
`TextBlock` that takes it by a `TemplateBinding`, or by a binding to the templated parent or to the
row as an ancestor, counted from the nearest ancestor of the type it names, in a `RelativeSource`
that finds an ancestor rather than one whose `Mode` is `Self` or `DataContext`, a shape's `Fill` that
takes it, or a templated control that takes it and whose own
template draws it; a binding with a `TargetNullValue` paints anyway, and one through a converter is
not decided. A theme that sets no template leaves a row the framework's, or `ContentControl`'s
default, both of which draw it, and a template of `{x:Null}` presents nothing. Where nothing draws the
row's background, anything in the template that paints a brush of its own draws one, however little
of the row it covers: a literal brush, a default its type registers, what a control's own theme
paints, or what the markup of a view, or of any type with an `x:Class` of its own or a base's, holds, whose own
`Background` outranks the one a template gives it, though a binding does not. What a
`ContentPresenter` or a `TransitioningContentControl` in the template holds as markup content has no
templated parent, so it is not followed. What the rows present decides nothing, the item template
or what a presenter in the template shows, and neither does what hides an element from the pointer,
nor what a `Popup` holds: a row whose content paints across it is still reported, and the fix,
`TargetNullValue={x:Static Brushes.Transparent}` or a `Transparent` background, costs nothing. A
value the template's markup gives an element ranks below a style with a condition and its own code,
so one they may replace is not decided. A style that paints the rows only for `:pointerover` leaves
the state they start in as it was, and so do ones for `:disabled` and `:selected`, unless markup may
disable or select the rows from the start. Anything in the scan that sets `IsEnabled` to anything but
`True`, or gives a button a `Command`, in any spelling the XAML compiler takes, may disable them,
since a control disabled anywhere disables all it holds; a list, its theme or its type that always
selects a row, as a `TabControl` does, or the selection, or an editable combo box's `Text`, that
markup gives a list, in any spelling, may select one. A `:selected`
style paints only the rows selected, so a null background the rest draw is still reported. One that
clears them in such a state can break a row, so it is named in `Skipped`. A style that holds of every row whatever the data, such as
`ListBox ListBoxItem` or `ListBoxItem:not(.compact)`, sets them all, a `:not()` of a state may not,
and one asking for a property the row theme binds to each item sets only the rows the data picks. It
checks Avalonia's list, combo and tab rows, measured on 12.1.3 under Fluent, Simple and Semi, and
leaves tree rows out, because Fluent's and Simple's give a tree row's header a background of its own.
Pass the application's assembly to `WithAssemblies`.

**`BNXQ1005`, `BNXQ1009` and `BNXQ1010` read a property in every spelling the XAML compiler takes.**
A setter names it alone, after an owner or in parentheses, in its `Property` attribute or a
`Setter.Property` element, and an element sets its own as an attribute or a property element. An
element that gives one property two ways decides nothing, because the framework applies each in turn
and a binding's value arrives once its data does. An owner is the type its prefix names: exactly that
type in a `using:` or `clr-namespace:` namespace, and found by its name in the framework's, Avalonia's,
or WPF's or MAUI's in markup written for that framework; one in any other namespace, which only an
assembly's `XmlnsDefinition` maps to a type, is not decided. On an
element, the control's own type or a base names its own property, and another owner names the one
held by the field the compiler takes for it, which is the control's only where it holds the same
property, as `Border`'s `Background` is a `Panel`'s and a templated control's. After an owner with an
instance property of that name, the compiler takes nothing but a binding, so
`Border.Background="{TemplateBinding Background}"` on a template's `Panel` draws the row's and
`Border.Background="Red"` does not load; after one with static `Set` or `Get` accessors, an attached
property, any value sets it. In a setter, a `TemplateBinding` or a binding path, an owner names the
property its most-derived public field holds, the control's own type included, since a subclass may
hide its base's field with another property, and where two types in the control's chain carry the
owner's name, it is decided only where they hold one property. An attached property of the same name,
such as a consumer's `Paint.Background`, is another property, and only Avalonia's
`AutomationProperties` names a row. A theme applies only where its `TargetType`, the type its prefix
names, is the control's style key or a base of it, and a style key a `StyleKeyOverride` returns other
than by a `typeof` is not read. Where an owner or its fields cannot be read, a value after another
owner may or may not be a binding, or an owner or a theme is not decided, the rule says so in
`Skipped`.

**Read `Skipped` as well as `Inspected`.** Every result also names what the rule saw but could not
check. For `BNXQ1003` that is a control with a `ControlTheme` in the scan and no part found in its
code, a control that declares parts but has no theme in the scan, one the scanned assemblies define
but could not load, one whose code could be read only in part, a name more than one scanned type
carries, where a theme targets it or parts were found in its types' code, and, when the scan was
given no assemblies at all, every themed control. For `BNXQ1004` it is a control whose fit depends on a value
markup cannot evaluate: a bound or resource-based definition, index, span, spacing or size, named
in the reason. For `BNXQ1005` it is every key binding it could not decide: one where a style sets
focus, rows with no item template, rows that present items of their own as a tree's do, and content
whose template the scan does not hold. For `BNXQ1006` it is a themed control that did not load or
whose peer method could not be read, a name more than one scanned type carries, a type with no
`OnCreateAutomationPeer` at all, and, when the scan was given no assemblies, every themed control.
For `BNXQ1008` it is a control that nothing hides, in slots whose size depends on a value markup
cannot evaluate, such as the bound width of a collapsing pane. For `BNXQ1009` it is a list whose
rows markup does not name: one whose items' type neither its item template's `x:DataType` nor its
`ItemsSource` binding reads, because the binding is a reflection one, takes a converter or a source
of its own, or follows a `DataContext` set on its way, or no data type is in scope, or the one that
is names a type the scan does not hold; one with no
item template that a data template may present only some rows of, from this view or another; an
item type that is abstract, an interface or a
control, or has a subclass that writes its own `ToString()`; a template root that names the row
through a peer of its own, and a row whose own peer names it; a style that names the rows only in
some state or some places, or from another view, and one that sets the list's template or the rows'
theme; and, when the scan was given no assemblies, every `ItemsSource`. For `BNXQ1010` it is a list
whose rows' background markup does not decide: a row theme or template the scan does not hold,
styles that disagree, a style that sets the rows' theme or template, or that may paint every row, a
row theme's nested style that may give rows another template, rows whose own code, or their list's,
sets their background or whose type's default cannot be read, a binding of the row's background
through a converter, or to an ancestor that markup cannot count back to the row, an element of the
template that a style with a condition or its own code may
repaint where it draws the row's background, and, where nothing does, one that its theme, a style, a
binding, its code or its type's default may paint, that is given a template of its own, that is a
templated control whose own theme the scan does not hold, or whose type the scan does not resolve,
and what a presenter, or a `TransitioningContentControl`, in the template holds as markup content; a
style that clears the rows in some
state, or picks some of them by a class, a name, a place, a property, the absence of a state, or
`:disabled` or `:selected` where markup may disable or select them; a style that paints,
clears or binds a part of their template, and one in another view, where it could change the answer;
and, when the scan was given no assemblies, every
`ItemsSource`.
None is a violation, and some are correct; all are places where a clean result is not what it seems.

## The theme audit

`theme-audit`, the `Bennewitz.Ninja.XamlQuality.ThemeAudit` dotnet tool, runs the library's analysis from a
`theme-audit.json`, whose every path is relative to the file. A `null` where the audit needs a value is a configuration
error that names the field. A field that has a default, such as `title`, `report` or a list like `themes`, may be left
out but not set to `null`; one that leaves nothing in its place, such as `parts`, a theme's `extends` or a consumer's
`tokens`, may also be `null`.

| Command | What it does |
|---|---|
| `theme-audit report` | Writes the audit report: the resource keys each theme leaves undefined for its consumers, and the colour pairs below a contrast floor |
| `theme-audit compat` | Writes each configured compat dictionary: the keys one theme defines and another lacks, mapped onto the latter's tokens |
| `theme-audit parts` | Writes the parts report: `BNXQ1003`'s reading of the configured controls |
| `theme-audit brand` | Writes each configured brand document: a template filled from a theme's palette |
| `theme-audit inventory <path>` | Lists the resource keys a folder defines, and writes nothing |

**`--check` is the drift check.** Given to a command that writes, it compares instead of writing: it exits 0 when the
committed output is what a fresh run writes, line endings aside, 1 when it is stale or missing or a file cannot be
read, and 2 on a configuration error. On drift it writes the fresh output beside the stale one, as `name.received.ext`,
where the failure can be read or the file taken; a check that finds the output up to date removes it again, and so does
writing the output. The audit never reads a received copy, but ignore `*.received.*` in git, and remove a received
`.axaml` before a build, which compiles it too. A test that generates an output in its own process runs the same
comparison with `OutputDrift.Check`, and writes the output with `OutputDrift.Write`.

**A colour is what Avalonia draws.** A brush's alpha is its colour's times every opacity on the way, truncated to a
whole byte once, as Avalonia's Skia renderer paints it. A brush's `Color` and `Opacity` are read as attributes, as a
whole `{StaticResource}` or `{DynamicResource}`, or as property elements holding a literal or a `<StaticResource>`,
an `Opacity` naming an `x:Double`. A brush whose `Color` names a brush, which Avalonia does not load, and one whose
`Color` or `Opacity` is a binding or anything else the audit cannot read, has no colour rather than a guessed one, so
a contrast pair that names it reads `n/a`. A contrast figure composites a translucent colour over its background
exactly. Skia's CPU renderer, as headless tests run it, blends a translucent colour in integers of its own, the same
on every CPU, from two levels below the exact blend to one above it.

**The parts report is the template parts reference a control library commits.** For each control it lists the
`PART_` constants the code declares, each part some code looks up on the control's template and whose code that is,
and the parts each of its themes declares, with the element and the automation id. A part a theme lacks that the code
declares or looks up reads **missing** in that theme's column, which is what `BNXQ1003` reports, so the report and the
rule cannot disagree. Its format is a contract: an upgrade that changes it is announced, and a committed report is
regenerated once.

```json
"parts": {
  "report": "docs/template-parts.md",
  "markup": ["src/MyControls"],
  "assemblies": ["tests/MyControls.Tests/bin/Release/net10.0/MyControls.dll"]
}
```

The assemblies load apart from the tool's own, each dependency from the folder of an assembly the configuration
names, so name them in a build output that holds their dependencies, as an application's or a test project's does:
a library's own holds none, and its controls would not load. A library that defines a base class of your controls,
such as a text editor's, names its base class's lookups when it is listed too.

**The brand document is a palette's catalogue that cannot drift from the palette.** Its prose is written by hand in a
Markdown template; its placeholders write what nobody should type: each token's colour and where it is declared, the
colour a stack of them draws as, every contrast figure, and a mermaid chart of the swatches in the variant's own
colours. A placeholder can state a claim a sentence rests on. Every claim that no longer holds, and every placeholder
that cannot be filled, is listed together, each with the template's line, and nothing is written.

| Placeholder | Writes |
|---|---|
| `{{tok KEY}}` · `{{at KEY}}` | A token's colour, as `#AARRGGBB` · where it is declared, as its path from the theme's `baseDirectory` and its line |
| `{{val COLOUR}}` | A colour, an opaque one as `#RRGGBB`. It takes no opacity |
| `{{mix STACK}}` · `{{lab STACK}}` | The opaque colour a stack draws as · the same for a mermaid label, `#` as `#35;`. `{{mix STACK =#RRGGBB}}` claims it |
| `{{cr FG on STACK}}` · `{{wc FG on STACK}}` | The WCAG 2.2 contrast of FG drawn over the stack, to one decimal place · its verdict for normal text. Either claims a bound with `<N` or `>=N` |
| `{{near PATH "phrase"}}` | `PATH:LINE` of the one line holding the phrase; a second phrase makes a range |
| `{{pin PACKAGE VERSION}}` | The version the nearest `Directory.Packages.props` pins, which must be the one named |
| `{{swatches}}` | Every token as a swatch, file by file, four to a row, over the frame's ground |

A COLOUR is `#RRGGBB`, `#AARRGGBB` or a token's key, and a token is a keyed colour or solid colour brush of the files
`tokens` names, or a `StaticResource` or `DynamicResource` alias of one. A file of the theme that cannot be read,
missing or not well-formed, is a problem too, since its tokens would be left out unseen; an include of another
assembly's theme, which the audit does not follow, is not. A STACK is colours top first, separated by `/`, ending in one that draws opaque, and each colour in it may carry
`@opacity`, a brush's and then an element's: white at 0.12 inside an element at 0.99 over the window is
`#FFFFFF@0.12@0.99/WindowBackgroundBrush`. Its alpha is Avalonia's, every opacity on the way multiplied and truncated
once. A placeholder the language does not define, `{{HUE_TABLE}}` say, is filled by a block: text your own program
renders from what only its code knows, passed to `BrandRunner.Fill` under the placeholder's inside, which replaces it
verbatim. The tool passes none.

```json
"brand": [
  {
    "template": "docs/Brand.template.md",
    "output": "docs/Brand.md",
    "theme": "App",
    "variant": "Dark",
    "tokens": ["src/App/Themes/Palette.axaml"],
    "near": ["src/App"],
    "frame": { "ground": "WindowBackgroundBrush", "stroke": "#5B5B5B", "text": "#F9F9F9" }
  }
]
```

`variant` is `Default` when omitted, and must be one the theme declares or `Default`, `Light` or `Dark`, which Avalonia
serves from a theme that declares none of them; `tokens` is every file the theme reads when omitted, and `near`, where
a `{{near}}` path is looked for, the configuration's folder; a template with no `{{swatches}}` needs no `frame`. A file-based program that calls
the library sets `#:property PublishAot=false`: the configuration is read with reflection, which a file-based
program's default, native AOT, turns off.

## The runtime theme check

The audit is a model of Avalonia's resource lookup: its theme dictionaries, variants that inherit from others, and
merged and included dictionaries. `Bennewitz.Ninja.XamlQuality.ThemeRuntime` reads that model against the real
lookup. For each consumer, under each theme the audit reads it with and each of that theme's variants, it asks
`Application.TryGetResource` for every key the consumer references and does not define itself, and compares the answer
with the audit's. Each disagreement is a `RuntimeDifference` of one of three kinds: a key the audit calls defined that
Avalonia does not find, one it calls undefined that Avalonia finds, and a lookup that throws, with its message.

The package holds source files, not an assembly: add it to an Avalonia test project that runs headless, and they
compile there against that project's Avalonia, beside `Bennewitz.Ninja.XamlQuality`, which does the planning and the
comparing. Nothing is built from a name: a map gives a factory for each theme, the `avares://` URI of each dictionary
the configuration merges, and the `ThemeVariant` instance of each variant, since a variant built from its name loses
what it inherits. A file that imports both `Avalonia.Styling` and the audit's namespace aliases
`ThemeVariant`, which both declare.

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Bennewitz.Ninja.XamlQuality.ThemeAudit;
using Bennewitz.Ninja.XamlQuality.ThemeRuntime;
using Semi.Avalonia;
using ThemeVariant = Avalonia.Styling.ThemeVariant;

[AvaloniaFact]
public void The_audit_reads_the_themes_as_Avalonia_does()
{
    AuditResult audit = AuditRunner.Run(AuditConfig.Load("theme-audit.json"));
    ThemeRuntimeMap map = new()
    {
        Themes = new Dictionary<string, Func<IStyle>> { ["Fluent"] = () => new FluentTheme(), ["Semi"] = () => new SemiTheme() },
        Merges = new Dictionary<string, Uri> { ["src/MyApp/Themes/SemiCompat.axaml"] = new("avares://MyApp/Themes/SemiCompat.axaml") },
        Variants = new Dictionary<string, IReadOnlyDictionary<string, ThemeVariant>>
        {
            ["Fluent"] = new Dictionary<string, ThemeVariant> { ["Light"] = ThemeVariant.Light, ["Dark"] = ThemeVariant.Dark },
            ["Semi"] = new Dictionary<string, ThemeVariant> { ["Light"] = ThemeVariant.Light, ["Dark"] = ThemeVariant.Dark, ["Aquatic"] = SemiTheme.Aquatic },
        },
    };

    RuntimeCheckResult result = ThemeRuntimeCheck.Run(audit, map);

    Assert.True(result.Differences.Count == 0, string.Join(Environment.NewLine, result.Differences));
    Assert.True(result.Inspected > 0, "The check asked Avalonia nothing.");
    Assert.Equal(["App: {x:Type Button}"], result.Skipped.Select(skip => skip.Subject));
}
```

Assert all three. `Inspected` above zero, since a check that asked nothing agrees with everything; and `Skipped` equal
to the list you expect, so that a theme or a dictionary that stops loading, or a theme, merge path or variant the map
does not name, fails the test instead of passing unchecked. The check builds every theme and merged dictionary afresh
for each theme and variant, sets the application's own styles and resources aside while it asks, and puts everything
back. It runs on Avalonia's UI thread, so run it in a test that nothing runs beside.

## Versioning

Versions come from the release tag (`v1.2.3` → `1.2.3`), passed to the build as `/p:Version=`.
`Bennewitz.Ninja.AutoVersioning` records build metadata into the assemblies; it does not set the
package version. A local build reads `1.0.0`, which is what a local build is.

Publishing uses **NuGet.org Trusted Publishing (OIDC)** — no long-lived API key exists in this
repository.

## Publishing

See [docs/publishing.md](docs/publishing.md) — one-time trusted-publishing setup, the version rule,
and what to check after a release.

## Licence

MIT. See [LICENSE](LICENSE).
