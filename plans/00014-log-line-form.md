# 00014 — BNCQ1012, a log line in the family's one form

> Status: **approved 2026-11-23**. Supersedes nothing.

Bennewitz.Ninja.Templates' `plans/00014`, approved by the developer on 2026-11-23, sets one form for every log line
the family's code writes, and gives its grammar to a CodeQuality rule (its decision 7), planned and released here
(its decision 8). Its session brought the request the same day. DiffView's `plans/00037` waits on the rule's id and
release, which Templates passes on.

The form, from Templates' decisions 1 to 3:

```text
[Backup.Tab] index={Index} tab="{Tab}" elapsedMs={ElapsedMs}
```

- **A tag first**: `[Area.Kind]`, both parts PascalCase, one dot, nothing before it. `[Area]` alone is not the form.
- **Then key=value pairs only**, one space before each, camelCase keys, no sentence. Free text is quoted in the
  template; numbers, booleans and identifiers are bare.
- **No hole carries `:l`**, and an enum is passed as its name, a string, since a `{Message:lj}` sink prints an enum
  quoted, `level="Warning"`.

Whether a line is one per user action, and what it must never hold, are review's (Templates' decisions 4 and 6).
AppKit read 17 calls outside the form, in 9 files, at `a0f43fe`, three of which pass on a line that already exists.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **One rule, `BNCQ1012`, "A log template is not in the family's one form", a warning on by default.** One id with a message for each way a template leaves the form | Templates' decision 7 asks for one id, on at warning. `BNCQ1010` and `BNCQ1011` would read as the counterparts of plan 00011's `BNAQ1010` and `BNAQ1011`, and `BNCQ1011` is kept for that counterpart; a template exists only in source, so this rule has no `BNAQ` counterpart, and `BNCQ1012` is the next number free in both families |
| 2 | **Its gate opens where the compilation can see `Microsoft.Extensions.Logging.ILogger` or `Serilog.ILogger`**, and returns null otherwise | A project that logs through neither registers nothing, as every rule's gate does |
| 3 | **The calls it reads:** <ul><li>`LoggerExtensions.LogTrace` to `LogCritical` and `Log`;</li><li>`LoggerMessage.Define` and its generic forms, and the `Message` of `[LoggerMessage]`;</li><li>Serilog's `Verbose` to `Fatal` and `Write`, on `Serilog.Log`, on `Serilog.ILogger`, and on every type that implements it, `Serilog.Core.Logger` among them.</li></ul> Each by its template parameter, `message`, `formatString` or `messageTemplate`, so an overload added later is read too. Not read: `BeginScope`, which writes no line, and `ILogger.Log<TState>` with a formatter, which takes no template | Templates' decision 7's calls. A call binds to the member on the type the compiler chose, so Serilog's interface alone would miss a call through a `Logger`, as plan 00011's review found of `Application.TryGetResource` |
| 4 | **The template must be a constant**, a literal or a `const`, or a concatenation of them; any other template is reported, as one whose form cannot be read | A template built at run time is the one a rule that reads constants cannot judge, AppKit's `UiThreadExceptions.cs:444` among them, and an interpolated string is no template at all to a structured sink. Leaving it unreported would make the rule's own blind spot the way around it |
| 5 | **The grammar.** The template is the tag and then nothing or pairs: <ul><li>the tag: `[`, a PascalCase word, `.`, a PascalCase word, `]`, at the very start, a PascalCase word being an upper-case letter and then letters or digits;</li><li>each pair: one space, a camelCase key, a lower-case letter and then letters or digits, `=`, and a value;</li><li>a value: either `"`, anything but `"`, `"`, or a run with no space and no `"`, which may be a hole, a literal or both, as `elapsedMs={Ms}`.</li></ul> Nothing may follow the last pair. `{{` and `}}` are literal braces. A hole is read as Serilog and Microsoft.Extensions.Logging read one, its `@` or `$`, alignment and format included. The finding names the first place the template leaves the form and what is there: no tag, `[Area]` alone, a part not PascalCase, a sentence, a key not camelCase, two spaces, text after the last pair | Templates' decisions 1 to 3, written so a test can hold each clause. Whether a bare hole holds free text the rule cannot know, since a string may be an identifier: that is review's, as quoting is |
| 6 | **A hole whose format is `l` is reported**, one finding for each | Templates' decision 3: under the family's `{Message:lj}` sinks `:l` does nothing, and on an enum it throws or drops the line |
| 7 | **An enum passed to a hole is reported**, an enum or a nullable enum, seen through its conversion to `object`. A hole is matched to its argument by position for the extension methods and Serilog's calls, by number for a numbered hole, by parameter name, ignoring case, for `[LoggerMessage]`, as its generator matches them, and to its type argument for `LoggerMessage.Define<…>`. A template with fewer arguments than holes, or more, leaves that mapping to the compiler's own analyzers and reports no enum it cannot place | Templates' decision 3: an enum is passed as its name, `mode.ToString()`, because the sinks print it quoted |
| 8 | **A line that passes on one that already exists is suppressed where it is written, with its reason**, `#pragma warning disable BNCQ1012 // …` or `[SuppressMessage]` with a `Justification`, and `docs/codequality.md` gives AppKit's `BucketedRollingFileSink`, which writes an already rendered line as `"{RawLine}"`, as the example. No template is exempt by its shape. Forwarded text that is not yet a line, `AvaloniaDiagnostics`' `EventListener` text, is a value of a line in the form, as Templates' decision 1 writes Avalonia's | A suppression is visible, greppable and reviewed at the one call that needs it. An exempt shape, `"{X}"` alone, would let any sentence through as `"{X}"` with the sentence as its argument |
| 9 | **A finding is reported on the template argument**, or on the attribute's `Message`; generated code is not analysed | Where the fix is typed. The `[LoggerMessage]` generator's output repeats the template |
| 10 | **The rule is proved on real code before it lands**: it runs in scratch over ClaudeForge, DiffView, AppKit, AutoVersioning, FileServer, and an app of each template, injected as plan 00010 ran `BNCQ3002`, and its findings are counted by message. AppKit's are its 17 calls in 9 files at `a0f43fe`, with decision 4's run-time template among them, or the difference is explained | Templates' count is a reading of AppKit by eye. The repositories it names are the ones whose builds the release turns red |
| 11 | **A mutation set of the rule's own, `tests/CodeQuality.Tests/Mutations/bncq1012.json`**, run with `mutate --swap src/CodeQuality/CodeQuality.csproj` | Each test must be shown to fail |

### Dismissed

- **A pass-through shape, a template that is one hole alone.** Two call sites need it, and it opens the rule to every
  sentence passed as an argument.
- **An attribute marking a pass-through method**, declared by the consumer as plan 00011's key attribute is. Public
  surface for two call sites a suppression already covers.
- **Leaving run-time templates unreported**, as Templates' decision 7 reads constants. The way around the rule would
  be to build the template.
- **Judging whether a bare hole holds free text.** A string hole may hold a name or an id; the rule would guess.
- **A code fix.** Converting a sentence into pairs is a choice of keys, which a fix cannot make.
- **`Microsoft.Extensions.Logging`'s and Serilog's own analyzers**, CA2254 and the like. They hold that a template is
  constant, not its form, and Serilog's are a third-party package nobody here pins.

## Scope

**In:** the analyzer and its line in `AnalyzerReleases.Unshipped.md`; its tests, with test-only references to
Microsoft.Extensions.Logging.Abstractions and Serilog at the versions step 2 reads AppKit and ClaudeForge pinning; its
mutation set; its section and the regenerated table in `docs/codequality.md`; `PROGRESS.md`; the message to Templates
with the id once this plan is approved, and with the release once it is out, whose text the developer approves.

**Out:** converting any repository's lines, which each does at the upgrade that brings the rule, AppKit before the
rule reaches it (Templates' decision 8); one line per action, and what a value may hold; the release itself, which
happens on the developer's word.

## Steps

Three pull requests. The approved plan's own commit comes first, before any implementation.

| # | Step | Verified by |
|---|---|---|
| 1 | This plan, approved, in a commit of its own; Templates is told the id | The pull request's required checks pass |
| 2 | **Measure, in scratch.** A scratch analyzer with decisions 3 to 7 runs over the repositories of decision 10 at commits recorded then, and reports, by message: <ul><li>which logging calls, overloads and receivers each repository uses, `[LoggerMessage]` and `Define` among them;</li><li>every template outside the form, and why;</li><li>every `:l` hole and every enum argument;</li><li>every run-time template</li></ul> | The counts recorded in `PROGRESS.md` for each repository. AppKit's findings set against its 17 calls in 9 files, each difference explained. A decision that a measurement refutes is put to the developer before step 3 |
| 3 | **The rule**: the analyzer, its release line, tests, the mutation set, the docs and `PROGRESS.md` | **Tests:** each call of decision 3 read, through an interface and a class alike; each clause of decision 5 both ways, the form and each way out of it, `{{` included; a `:l` hole; an enum, a nullable enum and an enum's name, through each mapping of decision 7; a run-time and an interpolated template; a suppressed call silent; generated code silent; inert without either logger beside a control with one. <br><br>**Mutation set:** kills as declared, within 30 minutes. <br><br>**Real code:** in scratch, the findings over decision 10's repositories are step 2's counts, and the analyzer reaches each repository's compiler |

Each step updates `PROGRESS.md` in the same change. CodeQuality releases on the developer's word, after which
Templates is told the version, with text the developer approves.
