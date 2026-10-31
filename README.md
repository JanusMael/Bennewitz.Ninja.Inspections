# Bennewitz.Ninja.Inspections

Static analysis for .NET, read three ways: the XAML markup, the assembly that ships, and the source
as it is typed. Each family has its own packages, its own version line and its own releases.

| Family | Reads | Packages | Its README |
|---|---|---|---|
| XamlQuality | XAML markup, and the compiled controls it names | [`Bennewitz.Ninja.XamlQuality`](https://www.nuget.org/packages/Bennewitz.Ninja.XamlQuality), [`.ThemeAudit`](https://www.nuget.org/packages/Bennewitz.Ninja.XamlQuality.ThemeAudit), [`.ThemeRuntime`](https://www.nuget.org/packages/Bennewitz.Ninja.XamlQuality.ThemeRuntime) | [docs/xamlquality.md](docs/xamlquality.md) |
| AssemblyQuality | The shipped assembly, by reflection | [`Bennewitz.Ninja.AssemblyQuality`](https://www.nuget.org/packages/Bennewitz.Ninja.AssemblyQuality) | [docs/assemblyquality.md](docs/assemblyquality.md) |
| CodeQuality | The source, as a Roslyn analyzer | [`Bennewitz.Ninja.CodeQuality`](https://www.nuget.org/packages/Bennewitz.Ninja.CodeQuality) | [docs/codequality.md](docs/codequality.md) |

Each family's README is its rules catalog, every rule with its own section, and its packages'
nuget.org page.

## XamlQuality

Accessibility names, theme-token discipline, resource keys a theme leaves undefined, contrast
floors, and the conventions a XAML codebase drifts away from silently. The rules read markup as
text or XML, so they work on WPF and MAUI XAML as well as Avalonia's, and nothing published
references Avalonia.

```bash
dotnet add package Bennewitz.Ninja.XamlQuality
```

The library's rules run under any test framework. `theme-audit`, the ThemeAudit dotnet tool, runs
the same analysis in CI and writes theme-compat dictionaries. ThemeRuntime ships as source files an
Avalonia test project compiles, to read the audit's answers against Avalonia's own resource lookup.

## AssemblyQuality

Rules for the shape a .NET assembly ships: cancellation tokens a caller can leave out, types that
leak into the public surface, forbidden references, shadowed namespaces, friend grants, and
references that no longer resolve. It reads the compiled output, the thing a consumer binds
against.

```bash
dotnet add package Bennewitz.Ninja.AssemblyQuality
```

Run it in a test project against the untrimmed build: the rules read by reflection and are not
trim-safe.

## CodeQuality

Roslyn analyzers for the same defects in the source, reported at the line while it is typed. Each
`BNCQ` rule is the compile-time counterpart of the `BNAQ` rule with its number, so `BNCQ1004` finds
what `BNAQ1004` would find after the build.

```bash
dotnet add package Bennewitz.Ninja.CodeQuality
```

A development dependency: it runs inside the compiler and ships nothing at run time. It needs a
compiler from Roslyn 4.14 on: Visual Studio 2022 17.14, or a .NET 9.0.3xx SDK or later.

## Every clean result says what it inspected

The XamlQuality and AssemblyQuality rules report and never assert: the consumer chooses the runner
and the severity. Each result also counts what the rule inspected, because a rule whose selector
stopped matching returns no findings and reads exactly like a clean codebase. Assert on both.

## Avalonia and drivable UI

Two guides live here alongside XamlQuality, each the only copy of itself:

- [docs/avalonia-gotchas.md](docs/avalonia-gotchas.md): measured Avalonia foot-guns, each a
  symptom, a cause and a fix.
- [docs/ai-drivable-ui.md](docs/ai-drivable-ui.md): how to build a desktop UI that an agent can
  drive and verify through its automation tree.

The guide is also a Claude Code plugin:

```bash
claude plugin marketplace add JanusMael/Bennewitz.Ninja.Inspections
```

```bash
claude plugin install drivable-ui@xamlquality
```

## Exception resilience

[docs/exception-resilience.md](docs/exception-resilience.md): where one exception from ordinary
input silently kills a feature, vanishes, or ends the process, in plain .NET, System.Reactive and
DynamicData, each shape measured, with its fix.

## Releases

Each family releases alone, from a tag of its own: `xamlquality/vYYYY.Q.MMDD`,
`assemblyquality/vYYYY.Q.MMDD` or `codequality/vYYYY.Q.MMDD`. The version is the tag's, and a
release publishes only that family's packages, to nuget.org through trusted publishing; no API key
exists in this repository. XamlQuality's releases before it was joined by the other two carry plain
`vYYYY.Q.MMDD` tags.

[docs/publishing.md](docs/publishing.md) is the runbook: the one-time setup, the preflight,
releasing, and checking the result against the feed.

## Contributing

[AGENTS.md](AGENTS.md) holds the invariants, the commands and the checklists, for people and agents
alike, and [PROGRESS.md](PROGRESS.md) the work in hand.

## Licence

MIT. See [LICENSE](LICENSE).
