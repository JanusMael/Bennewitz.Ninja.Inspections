# AGENTS.md — `.github/`

The workflows and the repository's GitHub settings.

| File | What it is |
|---|---|
| `workflows/ci.yml` | On every push and pull request, every job with Avalonia's build telemetry off, since the solution builds a project that takes Avalonia: build with `-warnaserror` and test on Linux, Windows and macOS; pack every packable project and check what was packed against `packages.push` and `packages.local` with `scripts/assert-packages.cs`; the `conventions` job, which runs `scripts/repo-conventions.cs check`; and `Verification tools`, which runs the real-code comparison, `tools/real-code.cs`, and the mutation runner's smoke catalog, `tools/mutate/smoke.json`, and builds the headless probe, `tools/probe/`, and `tools/probe.cs`, on Linux |
| `workflows/release.yml` | One `publish` job. On a `v*.*.*` tag, or a dispatch with `version` filled in, it refuses to run without `NUGET_USER`, then builds, tests, packs at the tag's version, checks what was packed against `packages.push` and `packages.local`, logs in through `NuGet/login`, pushes the packages `packages.push` names with `--skip-duplicate`, and creates the GitHub Release with the same files. Dispatched with `version` blank, it only logs in and reports the preflight in the run summary |
| `repository.json` | What this repository's GitHub settings vary by: description, topics, required checks, shipped content paths and exemptions. `scripts/repo-conventions.cs` applies and checks it |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` for tools that look here |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **A job's check name is part of the `main` ruleset.** `Build & Test (ubuntu-latest)`, `Build & Test (windows-latest)`, `Build & Test (macos-latest)`, `Pack (verify the packages build)`, `conventions` and `Verification tools` are required checks, listed under `requiredChecks`. The matrix job reports one check per OS, so renaming the job or changing its matrix means updating the list and running `repo-conventions apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why | `repository.json`; `repo-conventions check` |
| **`release.yml` keeps its file name**, and the credential preflight stays inside it | The trusted-publishing policy on nuget.org names this file, and the OIDC token is bound to the file name; renamed, every login fails | `docs/publishing.md`, step 2 |
| The preflight's list, the push and `gh release create` each read their ids from `packages.push`, the workflow spells out none, and nothing globs `*.nupkg` | A glob publishes whatever is in the folder, permanently; a policy is checked by eye against the preflight's list; and a second list of ids in the workflow drifts from the first | `PackagingTests` |
| The release runs `scripts/assert-packages.cs` on what it packed before it pushes | A package that packed and is declared nowhere would otherwise reach the push, and one declared and not packed would fail it halfway | `PackagingTests.The_release_workflow_checks_what_was_packed_before_it_pushes` |
| Every publishing step is gated on `RELEASING`, and the push also on `NUGET_USER` | A blank-version dispatch must never publish, and a tag with the variable unset must fail rather than create a release with no package | `release.yml`, the job-level `env` and each step's `if` |
| `NUGET_USER` is a repository variable, never a secret | A masked value hides the one fact that diagnoses a failed login | `release.yml` comment; `docs/publishing.md`, step 1 |
| `release.yml` holds `id-token: write`, and the login stays immediately before the push | That permission replaces a stored API key; the token it yields is short-lived | `release.yml` comments |
