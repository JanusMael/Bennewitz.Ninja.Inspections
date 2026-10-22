# AGENTS.md — `scripts/`

File-based C# apps, run with `dotnet run --file`. Each is compiled with this repository's root
`Directory.Build.props`, so warnings are errors here too.

| Script | What it does | Run by |
|---|---|---|
| `repo-conventions.cs` | Checks, and applies, the family's repository conventions: the AI-facing documents, GitHub settings and rulesets, read against `.github/repository.json` | CI's `conventions` job (`check`), and the maintainer (`check --admin`, `apply`) |
| `assert-packages.cs` | Checks that the packages `dotnet pack` actually produced in a folder are exactly the ones `packages.push` and `packages.local` declare. A copy of `templates/bbpkg/scripts/assert-packages.cs` in Bennewitz.Ninja.Templates | CI's `pack` job, and the release before it pushes |
| `fetch-reference.cs` | Fetches the real themes' sources that `fetch-reference.json` lists, Avalonia's Fluent and Simple themes and Semi.Avalonia, into `reference/`, which git ignores: each at the tag made of the version `Directory.Packages.props` pins for its packages, shallow, sparse and LF. `--status` reports without fetching, and exits 1 when a source is missing or off its pin; `--update` fetches one off its pin again | The developer, for work by hand, such as pointing the theme audit at the real themes' markup |

## Rules

| Rule | Why |
|---|---|
| **`repo-conventions.cs` is never edited here** | It is a copy of `templates/bbpkg/scripts/repo-conventions.cs` in Bennewitz.Ninja.Templates, identical in every family repository. Change it there and copy it back; run from that repository, `check --repo` reports a copy that differs |
| `assert-packages.cs` reads each id from the `.nuspec` inside the package, never from the file name | `<id>.<version>.nupkg` cannot be split reliably: nothing separates an id ending in `.ThemeAudit` from one ending in `.ThemeAudit.2026` |
| What this repository varies goes in `.github/repository.json`, not in the script | The family baseline lives in the script's `Baseline` and must not differ between repositories |
| A file-based app builds with everything the root props set, including the `Bennewitz.Ninja.AutoVersioning` reference and central package management | Nothing scopes `Directory.Build.props` to projects under `src`, so a script that fails to build may be failing on a root setting rather than on its own code |
| Nothing automated reads `reference/`: no test, build or CI job, by plans/00003 | Every test run on three operating systems stays off the network, and the repository republishes no other project's code |
| A reference source's tag is made from the pins, never written into the manifest | A source at a version apart from the package a measurement runs reads as the audit's error |
