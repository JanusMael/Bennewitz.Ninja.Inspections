# 00006 — The theme audit resolves an alias where Avalonia resolves it

> Status: **approved 2026-10-20**. Supersedes nothing.

The audit resolves every alias in one map per variant, built by flattening a theme, the files its
configuration merges into it and every dictionary they include, the last definition of a key winning.
Avalonia 12.1.3 does not look a key up in one map. An alias searches the dictionaries around it, out to the
root it was loaded in, then the application, whose resources come before its styles; and it searches them in
the variant of the nearest theme dictionary around it, or in `Default` alone. So the audit can give a colour
Avalonia never draws, and the check plan 00007 builds on this model would inherit the error. By the
developer's decision of 2026-10-20 the model is corrected first, in a plan of its own.

## What was read, and what the runtime check already shows

The port's reading of Avalonia 12.1.3 and the XamlX it pins, recorded in `PROGRESS.md` on 2026-10-20, each claim
marked as the port marked it:

| Claim | Mark |
|---|---|
| A keyed entry that is not a value type, a string or named is deferred, and takes a snapshot of the resource nodes on the XAML parent stack when it is added: its dictionary, those that include it, innermost first, out to its root, then `Application.Current` | read |
| At its first lookup the snapshot is searched in that order, the first hit kept for good. Inside one dictionary: its own entries, its theme dictionary for the variant and those it inherits, `Default`, then its merged dictionaries, last to first | read |
| The variant searched is the nearest keyed theme dictionary's around the alias; with none, only `Default` is searched | read |
| The application's lookup searches its `Resources` before its `Styles` | read |
| An x:Class dictionary created by its element is a root of its own | inferred |

The runtime check of plan 00003 already measures two of these against the audit, in
`tests/XamlQuality.ThemeRuntime.Tests`: an alias in a merged dictionary whose target only another variant
holds, and one outside every theme dictionary whose target only `Dark` holds, both throw under Avalonia, where
the audit counts them defined and, in each variant whose flattened map holds the target, gives them its colour.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **An alias resolves in its own scope first.** Its scope is the dictionaries from the one that holds it out to its root, searched as Avalonia searches them: the root is the theme's entry, an x:Class dictionary the walk entered by its element, a `merge` file, or a consumer's `tokens` entry. It searches in the variant of the nearest keyed theme dictionary around it, that variant's inheritance and `Default`, or in `Default` alone where there is none | What was read. A flattened map lets a key from anywhere answer an alias that Avalonia would never let it reach |
| 2 | **Past its scope, an alias reaches the application:** first the application's resources, which are the consumer's `tokens` and the `merge` files, then its styles, which are the theme | Where the runtime check of plan 00003 puts them, `ThemeRuntimeCheck` setting the merges as the application's `Resources`, and what was read of the application's lookup |
| 3 | **A key looked up from outside, as a consumer's reference or a contrast pair's key, starts at the application.** So a consumer's token that aliases a theme key resolves to the theme's colour, where today the audit resolves the two apart and leaves the token unscored | A control's `DynamicResource` climbs to the application, so the consumer's and the theme's keys meet there, as decision 2 orders them |
| 4 | **The walk keeps each definition's scope.** For every definition, the root it was loaded in, the chain of dictionaries between, and the variant of the nearest keyed theme dictionary; kept internal, as the walk is. `VariantInventory.Resolve` keeps its signature and documents the new rule | Decisions 1 to 3 need it, and so does plan 00007's check. Nothing public changes shape |
| 5 | **An alias Avalonia cannot resolve has no colour.** Where its scope and the application both lack its target in the variant searched, `Resolve` gives `null`, as it does for a dangling alias today; the key still counts as defined | A colour Avalonia never draws is the defect being fixed. Whether a lookup throws stays the runtime check's to report |
| 6 | **Every change the corrected model makes is drawn.** `DrawnColourTests` gains a case for each of decisions 1 to 3: a `merge` file redefining a key a theme alias reads; an alias outside every theme dictionary whose target only a variant holds; an alias inside an x:Class dictionary whose target lies outside it; and a consumer token aliasing a theme key. Each pixel must be what Skia draws for the colour the audit resolves, or the audit must give no colour where nothing is drawn | Decision 1 rests on one inference, and the rest on readings; a drawn colour settles each, as plan 00004's test does |

### Dismissed

- **Fixing only the `merge` files.** The variant rule and the x:Class root bend the same lookups, and plan
  00007 reads all three; a model right about one would be wrong about the others.
- **Predicting a throw.** Whether looking a key up throws is the runtime check's question, and it already
  asks it; the audit's part is to give no colour where Avalonia draws none.
- **Reading the order of an application's own merged dictionaries.** The configuration names no order but
  its own, so `merge` files keep the order they are listed in, the last searched first as Avalonia searches
  merged dictionaries.

## Scope

**In:**
- `ThemeGraphWalker`'s scope for each definition, `ThemeInventory`'s and `VariantInventory`'s resolution, and
  `ContrastAudit`'s lookup of a pair's keys;
- `DrawnColourTests`' new cases, and tests in `tests/XamlQuality.Tests` for each scope, root and variant rule;
- `README.md`'s section on the theme audit, the root `AGENTS.md`'s ThemeAudit section and invariants,
  `src/AGENTS.md`, `tests/AGENTS.md`, `PROGRESS.md`.

**Out:**
- plan 00007's check, which builds on this;
- what the compat generator writes, beyond the colours it reads through `Resolve`;
- the release.

## Steps

One pull request. The approved plan's own commit comes first, before any implementation.

| # | Step | Verified by |
|---|---|---|
| 1 | This plan, approved, in a commit of its own, the pull request's first | The pull request's required checks pass |
| 2 | **The corrected model:** decisions 1 to 6, with the documents and `PROGRESS.md`'s unreleased row | The new `DrawnColourTests` cases draw what the audit resolves on Linux, Windows and macOS in CI; the runtime check's tests of the two throwing aliases still pass, and the audit gives each no colour where Avalonia throws; `XamlQuality.Tests`' theme audit tests pass, each expectation that moves named in the pull request with its reason; `tools/mutate.cs` plants the flattened map back, the variant rule's absence and the order of decision 2 reversed, and each fails a test. In a scratch clone of DiffView at `bf6b734`, never pushed, its report and compat dictionaries regenerated with this tree's audit: what moved summarised in the pull request and recorded under "Effect on a consumer" |

Each step updates `PROGRESS.md` in the same change.
