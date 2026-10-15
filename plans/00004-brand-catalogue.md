# 00004 — The brand catalogue as a theme-audit output, and the audit's colours as Avalonia draws them

> Status: **approved 2026-10-15**. Supersedes nothing.

The WPF-to-Avalonia port builds its brand and theme catalogue with a generator of its own: a .NET 10
file-based program that fills a Markdown template with what nobody should type by hand, every palette
value and the line declaring it, every composited colour, every contrast figure, and mermaid swatch
charts, and that stops the build when a sentence's claim stops holding. It sent the generator and its
exporter here on 2026-10-14 for a home other repositories can use. The developer decided on 2026-10-15
that the generator's general half becomes a `theme-audit` output, for XAML palettes only, that the
exporter goes to the mermaid-charts skill, which covers swatch charts, and that this plan asks whether
a brush based on or read from another resource can be read.

It can. Every spelling of one that Avalonia draws can be read, and the audit already reads the common
ones; the one spelling it should refuse, a brush whose `Color` names another brush, does not load in
Avalonia at all. Measuring that showed something larger: the audit's colours are not always the ones
Avalonia draws. So this plan corrects the audit's colours first, and builds the brand output on them.

## What was measured

Headless on Avalonia 12.1.3 with Skia, on 2026-10-15: twenty cases, each a cell painted over a known
ground and read back from the rendered frame.

| What | Avalonia drew | The audit says | The port's generator says |
|---|---|---|---|
| White at an opacity of 0.12, over black | 30/255 | 31 | 30 |
| The same over `#454545` | `#5B5B5B` | `#5C5C5C` | `#5B5B5B` |
| A brush at 0.6 inside an element at 0.56, over black | 85 | 86 | 85 |
| A brush at 0.12 inside an element at 0.99, over black | 30 | 30 | 29 |
| A brush at 0.5 inside an element at 0.99, over black | 126 | 126 | 125 |
| `<StaticResource x:Key="A" ResourceKey="B"/>`, B a white brush at 0.12, used as `{StaticResource A}`; and at 0.5, used as `{DynamicResource A}` | 30; 127 | 31; 128 | refused |
| A brush at 0.12 whose `Color` is `{StaticResource}` or `{DynamicResource}` of a `Color` | 30 | 31 | refused |
| A brush whose `Color` is `{StaticResource}` or `{DynamicResource}` of a brush | does not load: `InvalidCastException` | a colour, both brushes' opacities folded in | refused |
| A brush whose `Color` is a `<StaticResource>` property element, or whose `Color` and `Opacity` are both property elements | 30 | no colour | refused |
| A white brush whose `Opacity` is `{StaticResource}` of an `x:Double` of 0.12 | 30 | opaque white | refused |
| A `Color` resource of `#1EFFFFFF`, through a brush or as a `Background` itself | 30 | 30 | refused |

Every pixel matched one arithmetic, the one `DrawingContextImpl.CreatePaint` writes at 12.1.3: the
brush's opacity times the opacity its element pushes, then `A * opacity` cast to a byte, so truncated
once; `SkiaOptions.UseOpacitySaveLayer`, which would draw an element's opacity through a layer instead,
is off by default. Blending then rounds to the nearest level, as the audit's `CompositeOver` does.
Applied to every case, the audit's arithmetic, which rounds the alpha, is one level off in 14 of the
18 that load, and the port's, which truncates after each opacity in turn, in two. The port's generator
refuses every resource here but a brush with a literal colour and no other attribute.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Two pull requests, the audit's colours first.** The first changes how the audit reads and composites colours; the second adds the brand output on top of it | The audit's report and contrast check share that reading, so changing it moves every consumer's committed report. Reviewed on its own, its effect on them is recorded apart from a feature that changes nothing they have |
| 2 | **The audit's alpha is Avalonia's: every opacity on the way multiplied, then truncated once to a whole byte.** `VariantInventory.Resolve` multiplies a brush's own opacity and those along an alias chain, as it does now, and truncates where it rounds. `AuditColor.CompositeOver` is left as it is | What was measured, and what the source at 12.1.3 says. A blend in steps of 1/255 never lands on a half, so the way the audit rounds a blend cannot differ from the renderer's |
| 3 | **The audit reads every spelling of a brush based on another resource that Avalonia draws, and refuses the one it does not load.** It already reads a `StaticResource` or `DynamicResource` element that aliases a brush or a colour, a brush whose `Color` is either markup extension of a `Color`, and a `Color` resource. It now also reads a brush's `Color` or `Opacity` written as a property element, holding a literal or a `<StaticResource>`, and an `Opacity` written as either markup extension of an `x:Double`, which it read as 1. A brush whose `Color` names a brush does not load at 12.1.3, so the audit stops giving it a colour, whether the brush is named directly or through an alias. What it still cannot read, a binding or a value from code, leaves the key without a colour, never with a guess | The developer's question of 2026-10-15. An `Opacity` read as 1 makes a translucent token score as opaque, the worst of the measured misses; a brush that never loads is not a colour anyone sees |
| 4 | **A test holds the audit's colours to what Avalonia draws.** A test class in `tests/XamlQuality.ThemeRuntime.Tests` draws every case of the measurement, and every spelling decision 3 reads or refuses, headless with Skia, and asserts that each pixel is what the audit resolves and composites, and that each case that does not load is given no colour. The project gains `Avalonia.Skia`, which `Directory.Packages.props` already pins at 12.1.3 | The arithmetic is Avalonia's, and a version that changes it would change what is drawn under a figure the audit still prints. In the solution the test runs in every pull request on Linux, Windows and macOS; the probe under `tools/` runs in none. The cases paint brushes, which no theme changes, so one theme measures them |
| 5 | **The brand output is a `theme-audit brand` action, a `brand` section of the configuration, and the library's half.** Each entry of `brand` is one document: its `template` and `output`; the `theme`, from the configuration's `themes`, that its palette is, and the `variant` it is read in, `Default` when omitted; `tokens`, the files whose keyed brushes and colours are the palette's tokens, every file the theme reads when omitted; `near`, the directories a `{{near}}` path is looked for in, in order, the configuration's directory when omitted; and `frame`, the keys of the ground, the stroke and the text its charts are drawn in. `theme-audit brand` fills each document and writes it through `OutputDrift.Write`; with `--check` it compares through `OutputDrift.Check` instead; and it exits 0, 1 or 2 as `report`, `compat` and `parts` do | The audit's own actions and drift check are the contract consumers' CI steps already branch on. Analysis stays in the library, by `src/AGENTS.md`, so a consumer's own program or test fills the document the tool does |
| 6 | **The template language is the port's general half, as the port writes it.** `{{tok Key}}` writes the colour the key resolves to in the document's variant, as `#AARRGGBB`; `{{at Key}}` writes where it is declared, as its path from the theme's `baseDirectory` and its line; `{{val COLOUR}}`, `{{mix STACK}}`, `{{mix STACK =#RRGGBB}}`, `{{lab STACK}}`, `{{cr FG on STACK}}` with `<N` or `>=N`, `{{wc FG on STACK}}` and `{{near PATH "phrase"}}` with an optional second phrase, as the port defines them; `{{pin PACKAGE VERSION}}`, which holds where the `Directory.Packages.props` nearest the configuration's directory, at or above it, pins that version, for the port's `{{SEMI …}}`; and `{{swatches}}` for its `{{PALETTE_SWATCHES}}`. A COLOUR is `#RRGGBB`, `#AARRGGBB` or a token's key, aliases followed; in a STACK each may carry `@opacity` as often as it has opacities, composited by decision 2 | The developer's decision of 2026-10-15 moves the general half, so the port's template moves with it, two of its placeholders renamed. The port truncates each opacity in turn, which the measurement refutes |
| 7 | **Every claim and every guard that fails is listed, and nothing is written.** A claim that no longer holds; a placeholder the language does not know; a key that names no token or resolves to no colour; a `{{near}}` phrase on no line or on more than one; and an `@` in a `{{val}}`. The tool lists them all and exits 1, never with an unhandled exception | The port's fixes BC-2, BC-5 and BC-6: one run shows every problem, and a failure has an exit code a script can read |
| 8 | **Charts follow the mermaid-charts skill's lessons, in the variant's own colours.** `{{swatches}}` draws every token, file by file in the order the theme reads them and in declaration order, four to a row, each filled with what it draws as over the frame's ground and labelled with its key and value, its text black or white, whichever contrasts more. Labels write `#` as `#35;` and `"` as `#quot;`, front matter sets `titleColor`, and rows are linked with `~~~`. The ground, the stroke and the title's colour are the `frame`'s keys, resolved in the document's variant | The port's lessons, which the mermaid-charts skill takes too, by the developer's decision of 2026-10-15; and BC-4, a frame written in the dark variant's colours, which a light ground hides |
| 9 | **The document's format is a contract, as the parts report's is.** LF, invariant culture, no timestamp, no absolute path, and a first line naming the template by its path from the configuration's directory. A change to it is recorded under "Effect on a consumer" | A committed document is checked by the drift check on Windows, Linux and macOS |
| 10 | **The port's own half stays in the port, and reaches a document as named blocks.** Its search palette, level words, escape codes, colour names and accents, and the counts it draws from them, come from its own C#. Its own program renders them and passes each to the library's half as a block, named by the placeholder it fills, as `HUE_TABLE` or `huename dark`, which replaces that placeholder verbatim after everything else is filled. A placeholder the language does not define and no block fills, a block the template never names, and a block named as one of the language's own are problems by decision 7. The tool passes no blocks | They are read from C#, which the developer's decision of 2026-10-15 keeps out. Named blocks are the smallest surface that lets the port's program and the tool fill one document under one drift check |
| 11 | **The public surface, permanent from the release that carries it.** `ResourceDefinition` gains `Line`; `ResourceValue.ColorLiteral` and `ResourceValue.Alias` gain `OpacityKey`, the key an `Opacity` names; `ResourceValue.Alias` gains `FromColor`, set where the alias is a brush's `Color`; each is an `init` property. `AuditConfig` gains `Brand`. New are `BrandConfig` and `BrandFrame`, its configuration; `BrandRunner.Fill(AuditConfig config, BrandConfig document, IReadOnlyDictionary<string, string>? blocks = null)`; and `BrandResult(string Markdown, IReadOnlyList<string> Problems)`. `src/XamlQuality/Properties/AssemblyInfo.cs` records them | The audit surface is public, by `AssemblyInfo.cs`, and a member added to a public record later is an `init` property, so code compiled against an earlier release keeps compiling |

### Dismissed

- **Rounding the alpha, as the audit does today, or truncating after each opacity, as the port's
  generator does.** Both refuted by the measurement.
- **Reading a brush whose `Color` names a brush.** Avalonia 12.1.3 does not load it.
- **Palettes that are not XAML.** Out, by the developer's decision of 2026-10-15.
- **The port's own placeholders in the library.** They read the port's C#, by decision 10.
- **Two tools filling one document in turn.** The drift check cannot compare a half-filled document
  with the one committed.
- **A data file of colour sets the brand output reads.** A second input language, and the port would
  still need a program to write it from its C#.
- **A placeholder naming its own variant, as `{{tok Light:Key}}`.** No document needs one; two
  variants are two entries of `brand`.
- **Making the port's arithmetic and chart helpers public.** The port renders its own tables today and
  keeps doing so; a helper goes public when a second program needs it.
- **The measurement as a case of the probe under `tools/`.** The probe measures the rules' verdicts,
  and no pull request runs it.

## Scope

**In:**
- the audit's colour reading and arithmetic, in `ThemeDefinitionScanner`, `ThemeGraphWalker` and `ThemeInventory`;
- the test class of decision 4, and `Avalonia.Skia` in its project;
- the brand output: `BrandRunner`, its configuration, the tool's `brand` action, and its tests in `XamlQuality.Tests` over fixture palettes and templates;
- `src/XamlQuality/Properties/AssemblyInfo.cs`, `README.md`'s section on the theme audit, the root `AGENTS.md`'s ThemeAudit section and invariants, `src/AGENTS.md`, `tests/AGENTS.md`;
- `PROGRESS.md`.

**Out:**
- the port's adoption: its program calling the library, which it hears of when a release carries the brand output, in text the developer approves;
- the exporter and the swatch lessons in the mermaid-charts skill, whose exact text the developer approves before it lands in bb-skills;
- the port's capture scripts, BC-7 and BC-8, which stay with the port;
- the release.

## Steps

Two pull requests. The approved plan's own commit comes first, before any implementation.

| # | Step | Verified by |
|---|---|---|
| 1 | This plan, approved, in a commit of its own, the first pull request's first | The pull request's required checks pass |
| 2 | **The audit's colours as drawn:** decisions 2 to 4, with the root `AGENTS.md`'s invariant, `README.md` and `PROGRESS.md`'s unreleased row | The new test class draws every measured case and finds the audit's colour in each, to the level, on all three operating systems, and finds the two that do not load given no colour. `XamlQuality.Tests`' theme audit tests pass, each expectation that moves named in the pull request with its reason. `tools/mutate.cs` plants the old rounding, drops each newly read spelling, and drops the refusal of a brush named by a `Color`, and each fails a test. In a scratch clone of DiffView at `bf6b734`, the commit `tools/real-code/inputs.json` pins, never pushed, its report regenerated with this tree's audit, and what moved summarised in the pull request and recorded under "Effect on a consumer" |
| 3 | **The brand output:** decisions 5 to 11, with `AssemblyInfo.cs`, the documents and `PROGRESS.md` | Tests for each placeholder, and for each kind of claim holding and failing; every problem listed together and nothing written; a palette with light and dark variants filled in each (BC-3); a chart framed in the variant's own colours (BC-4); an `@` in a `{{val}}` refused (BC-2); blocks given and missing; the tool's exit codes and its drift check, as `ThemeAuditToolTests` holds the other actions. `tools/mutate.cs` over the new classes. Then, in a scratch copy of the port's catalogue as it arrived on 2026-10-14, its template's `{{SEMI …}}` and `{{PALETTE_SWATCHES}}` renamed, a file-based program passing the port's own blocks: the document it fills is the port's `Brand.md` but for the first line and each figure the arithmetic of decision 2 moves, each named in the pull request |

Each step updates `PROGRESS.md` in the same change.
