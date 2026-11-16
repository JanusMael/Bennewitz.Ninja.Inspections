# Bennewitz.Ninja.Testing

Tools that test the tests. One package so far:

| Package | What it is |
|---|---|
| `Bennewitz.Ninja.Testing.Mutate` | `mutate`, a dotnet tool: mutation testing driven by committed sets |

## `mutate`

A test that cannot fail proves nothing. `mutate` breaks the code a test guards, one declared edit at a time, and
checks that the test goes red, and that it goes red in the right place: the test that must fail, with the words its
failure must carry. A change that should not matter is declared too, and must leave every test green.

```bash
dotnet new tool-manifest            # once per repository
dotnet tool install Bennewitz.Ninja.Testing.Mutate
dotnet mutate --set tests/MyApp.Tests/Mutations/parser.json
```

It needs a git checkout and tests on Microsoft.Testing.Platform, which `dotnet test --project` and `--filter-class`
assume, as xUnit v3's do.

### A set

A set is a JSON file committed beside the tests it proves, `tests/<project>/Mutations/<subject>.json`. Comments and
trailing commas are allowed. A key the reader does not know is refused, never ignored: a misspelt `killer` would
otherwise drop the one check its mutation was written to make.

```json
{
  "project": "tests/MyApp.Tests/MyApp.Tests.csproj",
  "tests": ["tests/MyApp.Tests/ParserTests.cs"],
  "mutations": [
    {
      "name": "the parser forgets the last token",
      "file": "src/MyApp/Parser.cs",
      "old": "for (int i = 0; i < tokens.Count; i++)",
      "new": "for (int i = 0; i < tokens.Count - 1; i++)",
      "killer": "The_last_token_is_read",
      "says": "Assert.Equal() Failure"
    },
    {
      "name": "a loop written the other way round",
      "file": "src/MyApp/Parser.cs",
      "old": "i < tokens.Count; i++)",
      "new": "tokens.Count > i; i++)",
      "expect": "green",
      "because": "the same loop, so every test must stay green"
    }
  ]
}
```

| Key | Where | Meaning |
|---|---|---|
| `project` | set, required | The test project every mutation is built and judged in |
| `class` | set or mutation | A test class every run is filtered to |
| `tests` | set | Test files whose every test some mutation must kill |
| `pins` | set | The props file `inert-at-pin` markers are judged against; `Directory.Packages.props` unless given |
| `name`, `file`, `old`, `new` | mutation, required | The edit: `old` must occur exactly once in `file`, matched as written |
| `expect` | mutation | `red`, the default, or `green` |
| `killer` | red mutation | The test that must be among the failures |
| `says` | red mutation | Text the run must contain |
| `because` | mutation | Why the expectation is right; a green mutation must give one, and a red one gives one or a `killer` |

A set with red mutations and no green one is refused: a green control is the only run in which a test failing
under every mutation shows. A mutation that edits its own set is refused too.

### Verdicts

Every mutation's run is compared with its declaration, and anything but `ok` fails the run.

| Verdict | When |
|---|---|
| `ok` | As declared |
| `survived` | Declared red, and every test passed |
| `aborted` | Red, but no test was named as failing: the host died |
| `wrong-killer` | Red, but the declared killer was not among the failures |
| `wrong-assertion` | The killer failed without the declared words |
| `unexpected-red` | Declared green, and a test failed |
| `no-op` | The edit no longer matches exactly once: the code moved |
| `build` | The mutation does not compile, which proves nothing about the tests |

Only an explicit `Test run summary: Passed!` reads as green: an aborted host prints `Failed!` with `failed: 0`, which
every reading by counts calls healthy.

### Completeness

A set that names its test files is held to every test in them: a full run fails while any of them was killed by no
mutation. A test no mutation of yours can break, one that asks about a dependency, is excused by a line in its body,
`// inert-at-pin: <package> <version>`, while the set's pins hold that package at that version. A moved pin, a
marker in no test, and a marked test some mutation killed anyway each fail the run.

### How it runs

- ⭐ **The checkout is never edited.** A run cuts a detached git worktree, loads it with the checkout's state,
  uncommitted work and untracked files included, written as a git tree through a temporary index, checks that it
  holds that tree, and plants its mutations there. A run killed partway leaves only the worktree, which the next run
  removes.
- **A clean baseline first.** The worktree's clean tree must build and pass, or nothing is mutated.
- **A budget.** A run is estimated after its baseline and refused past 30 minutes, or past `--budget <minutes>`.
- **Builds stay incremental.** Every build and test run is given one `BuildTimestamp` for the run, MSBuild's node
  reuse off and Avalonia's build telemetry off. A mutation's build skips the restore unless its edit, or the one
  before it, is to what restore reads.
- **A swap, where you ask for one.** With `--swap <project>`, a mutation in that project's folder builds the project
  alone and copies its assembly over the one in the test project's output, which took one library's mutations from
  17.9 s to 14.0 s on average on a busy machine. Only the project's own build checks the mutation, so swap one whose mutations stay inside it; a
  mutation to what restore reads, and the one after it, build the test project as usual.
- **Narrowed where it is safe.** A red mutation that names its killer runs only the classes of the set's test files;
  a green one, and a red one with no killer, run the set's whole filter.

### Commands

| Command | What it does |
|---|---|
| `mutate --set <file> --list` | Each mutation, its expectation, and what its run filters to and restores; runs nothing |
| `mutate --set <file> --check` | Every edit applies once, every killer is declared, every marker holds; runs nothing |
| `mutate --set <file>` | Runs the set in one worktree |
| `mutate --set <file> --only <text>` | The mutations whose name holds the text; a narrowed run judges no completeness |
| `mutate --set <a> --set <b>` | Several sets, one after another, each judged on its own |
| `mutate --owed <ref>` | The committed sets a change since `<ref>` owes a run: its own file, a test file or an edited file changed, or a pin moved that its project resolves |
| `mutate --parallel <n> [--set … \| --owed <ref>]` | Every mutation dealt into `n` worktrees at once, each set judged as one run |
| `mutate --judge <log> red\|green [--killer <test>] [--says <text>]` | Judges a captured test log |
| `mutate --set <file> --kills <kills.json>` | Judges the set's completeness against recorded kills |

`--configuration <name>` (default `Debug`), `--property <name>=<value>`, `--swap <project>` and `--budget <minutes>`
apply to every build and test run, in every worktree.

Exit codes: `0` every mutation as declared and every declared test accounted for; `1` a mutation not as declared, a
test killed by none, a setup fault, or a baseline that does not build or is red; `2` a usage error, an unreadable
file, a refused budget, a write outside the declared file, or a merge refused.
