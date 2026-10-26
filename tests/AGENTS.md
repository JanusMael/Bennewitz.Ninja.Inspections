# AGENTS.md — `tests/`

One test project per family, `XamlQuality.Tests` with `XamlQuality.ThemeRuntime.Tests`,
`AssemblyQuality.Tests` and `CodeQuality.Tests`, and under `fixtures/` the assemblies
AssemblyQuality's tests reflect over. `tests/Directory.Build.props` makes each test project an
xUnit v3 test executable on Microsoft.Testing.Platform that is never packed;
`tests/fixtures/Directory.Build.props` keeps the fixtures plain libraries.

## Rules every family keeps

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform, with no VSTest bridge packages | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| Every `Directory.Build.props` here imports the root props explicitly | MSBuild applies only the closest one; without the import a project silently loses the root's target framework and settings | the import line in each |
| A test that scans the shipped assemblies scans its own family's, the `src/` folders named for it | Each family's tests build only what they reference, so another family's assembly is not in their output | `TrimmableTests`, `AnalyzerShapeTests`, XamlQuality's `AssemblyQualityTests` |
| A test finds the repository by walking up to `Inspections.slnx`, and each family's README at `docs/<family>.md` | Test binaries sit at a depth that varies by configuration, and a runner picks its own working directory | each family's `RulesCatalogTests` and `Packaging/` tests |
| A test project may reference another family's project; a packable project may not | Tests gated by another family's head are the point of one repository; a package that did so would depend on a version of that family that need not exist | `repo-conventions check`, `families` |

⛔ **Never weaken a packaging or convention test to make it pass.** When one fails, the workflow,
the project list, the package list or a shipped assembly is wrong, not the test.

## XamlQuality

Two test projects: `XamlQuality.Tests`, and `XamlQuality.ThemeRuntime.Tests`, the one project in the
solution that takes Avalonia. `tests/Directory.Build.props` makes each an xUnit v3 test executable on
Microsoft.Testing.Platform that is never packed, with `Xunit` as a global using, which the second
removes.

| Folder in `XamlQuality.Tests/` | What it covers |
|---|---|
| `Rules/` | One test class per rule in `src/XamlQuality/Rules/`, each writing its markup into a fresh temporary directory; `FocusFakes.cs` and `PeerFakes.cs` are the miniature frameworks `BNXQ1005`'s, `BNXQ1009`'s and `BNXQ1010`'s, and `BNXQ1006`'s, tests read through reflection; in `FocusFakes.cs` a property is the object a type's field holds, as the framework's is, so the types that inherit a field hold one property, a list's selected index is a direct property that another list adds itself to with a copy the framework takes for it, `AutomationProperties` holds attached properties no control does, `BindingBase` is what every binding derives from, two lists borrow `ListBox`'s style key, one by a `typeof` and one through a field, and, as the framework's are, the controls are `AvaloniaObject`s and the properties `AvaloniaProperty`s, each property a rule reads after an owner with an instance CLR property beside its field, which is what the XAML compiler resolves. `ContainerNameFakes.cs` holds the automation peers `BNXQ1009` and `BNXQ1010` recognise a row by, in Avalonia's own namespace, the item types `BNXQ1009`'s templates declare, the view models whose lists its `ItemsSource` bindings read, among them a collection of two item types, one that hides its base's list and an interface that inherits one, and the consumer's controls, views, panels, borders, shapes, rows and lists `BNXQ1010` reads, some painted by a default their static constructor registers or by their own code, some whose static constructor fails, one that draws itself, and one that borrows `Border`'s style key, a border, a panel and a templated control whose own markup a test writes under their `x:Class`, a subclass of the border and of the panel that carries it, a static class whose attached properties a test names after it, one that registers an attached `Background` of its own, which is another property than a control's, and a binding of the consumer's own; and the owners the XAML compiler's routes tell apart: static classes with accessors whose field holds a control's own `Background`, `Text` or `Focusable`, an `AvaloniaObject` that adds itself as an owner of `Background` with accessors and no instance property, a panel, a text block and a list that hide their base's field with a public one of another property, a panel that hides it with a protected one, a static class with an attached `Name` of its own, a list that re-declares the selected index beside an `AddOwner` copy of it, a row whose name a row in another namespace carries too, and, in a namespace of their own, classes named as the framework's `TemplatedControl` and `Panel` are, each with an attached `Background` of its own. `EmittedViews.cs` emits at test time what C# cannot write: a view whose painting code is in an assembly the scan is not given, one with compiled markup's `!XamlIlPopulate` and a nested `XamlClosure_1`, a border with compiled markup of its own and a subclass of it with none, and, in an assembly named as the framework's are, a control that draws itself and a list whose code sets a background, and, in a consumer's assembly of its own, a row named as the framework's `ContentControl` is, which it derives from, that hides its `Background` field, and a list of those rows. `HandedTemplateFakes.cs` holds the controls that hand their template to other code, for `BNXQ1003`, one of them through an interface and one a base class's lambda, which the rule does not follow, and `PartTwins.cs` types of one name in two namespaces, two with constants and two with lookups alone, which it reads as no control's, as it does `PeerFakes.cs`' partless `Twin`s; `TemplatePartReadingTests` reads them through its public reading, beside `EmittedTemplateBases.cs`, which emits at test time a base class whose `OnApplyTemplate` looks a part up and, in an assembly of its own, a control derived from it; `TemplatePartReportTests` holds the parts report's format byte for byte. `UnloadableBuildOutputTests` covers the five rules that read compiled code, over `UnloadableAssembly.cs`: build output emitted at test time whose dependency does not load |
| `ThemeAudit/` | The `ThemeAudit` analysis; the committed inputs are under `ThemeAudit/Fixtures/` (`audit`, `compat`), located through `FixturePaths.Fixture`. `ThemeAuditToolTests` runs the `theme-audit` tool's entry point in this process with the console captured, in the `ConsoleCapture` collection, which runs alone, over copies of those fixtures and the control library `EmittedParts.cs` emits at test time to a folder of its own, since the tool keeps what it loads locked, with its base class in an assembly of its own beside it, which the tool's load context must find there, and a palette and brand template it writes for `brand`. `BrandRunnerTests` writes each palette and template it fills into a temporary folder of its own, and its expected figures were worked out apart from the code |
| `Packaging/` | `AssemblyQualityTests` runs the AssemblyQuality family's rules, `src/AssemblyQuality` referenced as a project, over this family's shipped assemblies; `SourceOnlyPackageTests` packs each source-only project under `src` without a build, as the release does, into a folder of its own, and holds its package; `AvaloniaConfinementTests` reads every other project's restore graph for a package that names Avalonia; `PackagingTests` holds every packable project under `src` to `packages.push` or `packages.local`, and reads `.github/workflows/release.yml` for a glob, an id spelt out, or a step that stops reading `packages.push`; `PluginMarketplaceTests` reads `.claude-plugin/marketplace.json` and the plugin it lists |
| `RulesCatalogTests.cs` | Generates, and checks, the rules table in `docs/xamlquality.md` from the rule types |

`XamlQuality.ThemeRuntime.Tests` compiles `src/XamlQuality.ThemeRuntime/*.cs`, the files the
source-only package ships, with Avalonia's headless platform, and runs them over fixture themes, by
`plans/00003`. It compiles them as a hostile adopting project would: implicit usings off, the language
version a project gets by default, and `HostileAliases.cs`'s global aliases, named after the Avalonia
and library types the files use, so a name they leave unqualified fails to build. Its own files put
their usings inside their namespace, which lookup searches before the aliases.
`Fixtures/theme-audit.json` audits the themes under `Fixtures/Themes`, compiled and loaded by
`avares://`, for the consumer under `Fixtures/Consumers`, which is data and never compiled.
It also holds the audit's colours to what Avalonia draws, by `plans/00004`: `DrawnColourTests` loads
each case's markup at test time with `Avalonia.Markup.Xaml.Loader`, presents it in Simple's window
template, and reads the pixel Skia renders, which is why `TestApp` draws with Skia; the pixel must be
what Skia's CPU raster draws for the colour the audit resolves, or, for a brush painted by an element
at an opacity of its own, at the alpha the brand output makes for that stack.

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| `tests/Directory.Build.props` imports the root props explicitly | MSBuild applies only the closest `Directory.Build.props`; without the import the project silently loses the root's target framework, nullable settings and AutoVersioning reference | the `Import` line itself |
| No VSTest bridge packages | Tests run on Microsoft.Testing.Platform, so `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner`; `tests/Directory.Build.props` comment |
| The test project references `XamlQuality.ThemeAudit.csproj` as well as the library | `AssemblyQualityTests` loads each shipped assembly from the test output, and `ThemeAudit.dll` arrives only through that reference | `XamlQuality.Tests.csproj` comment; `AssemblyQualityTests.Every_shipped_assembly_is_in_the_scan` |
| A test finds the repository by walking up to `Inspections.slnx`, and a fixture by walking up to its own project's csproj | Test binaries sit at a depth that varies by configuration, and a runner picks its own working directory | `RulesCatalogTests.RepositoryRoot`, `PackagingTests.RepoRoot`, `PluginMarketplaceTests.RepoRoot`, `FixturePaths`; `ThemeRuntimeCheckTests`' private `ProjectDirectory` |
| A test that could pass vacuously has a companion proving there was something to check | A discovery that finds nothing agrees perfectly with an empty table or an empty scan | `RulesCatalogTests.RuleDiscovery_FindsTheRulesThatShip`; `PackagingTests.The_release_workflow_still_has_both_publishing_steps` and the `Assert.NotEmpty` in `PackagingTests.Every_packable_project_is_classified`; the `Inspected` and `Skipped` assertions in `AssemblyQualityTests`; `SourceOnlyPackageTests.A_source_only_project_is_found`; `AvaloniaConfinementTests.The_guard_reads_every_other_project_the_solution_holds` and `A_package_whose_id_names_Avalonia_is_found_and_a_project_is_not` |
| Avalonia.Headless builds an application for each test, so `TestApp` holds its style and resources per instance | A style held statically belongs to the first test's application, and adding it to the next one's throws in that test's setup, so every test after the first errors before it runs | `TestApp.OwnStyle` |
| `TestApp`'s own style, its `Styles` collection's resources and its own resources hold keys the consumer references and the audit finds undefined in some setup | Under keys nothing references, a check that left the application's styles or resources in place while it asked would pass every test | `ThemeRuntimeCheckTests.Where_the_audit_and_Avalonia_agree_nothing_is_reported_through_inheritance_an_include_or_an_alias` |
| `DrawnColourTests` predicts each pixel with Skia's CPU raster arithmetic applied to the colour the audit resolves, never with the audit's exact blend, and holds it to the level | The raster blends in integers of its own, alike on every CPU, from two levels below the exact blend to one above, so the exact blend fails some colours, and a tolerance would hide a level the audit got wrong | `DrawnColourTests` (its private `Rastered`) |
| `XamlQuality.ThemeRuntime.Tests` runs its tests one at a time | The check changes the application while it runs, on the one headless dispatcher every test shares | `xunit.runner.json` |
| The rules table in `docs/xamlquality.md` is regenerated with `XQ_UPDATE_DOCS` set, never edited by hand, and the build never writes it | A build that edits a tracked file leaves CI with a dirty tree | `RulesCatalogTests.TheReadmeRulesTable_IsWhatTheRuleTypesSay` |
| No test constructs a type in `Rules/FocusFakes.cs` | Constructing one runs its static constructor, which is `BNXQ1005`'s job; a test that ran it first would hide the cold read the rule has to get right | the file's header comment; test `KeyBindingFocusRuleTests.AListWithStockRows_IsLive_BecauseItsRowsTakeFocus` |
| The unloadable fixture is emitted at test time, and loaded from its file where the assembly it references is not | A checked-in binary cannot be reviewed, a fixture project the tests reference brings its dependency into the test output where it loads, and one loaded from bytes has no metadata file for the rules to read type names from | `Rules/UnloadableAssembly.cs` remarks; the `UnloadableBuildOutputTests` guards, which assert the fixture's control did not load |
| The repository's friend grant to `XamlQuality.Tests` lets the `ThemeAudit/` tests assert on internal types | They were ported unchanged, which is what makes them evidence the move changed no behaviour | `AssemblyInfo.InternalsVisibleTo.cs` |

⛔ **Never weaken a test under `Packaging/` to make it pass.** When one fails, the workflow, the
project list or a shipped assembly is wrong, not the test.

## AssemblyQuality

The test project and the fixture assemblies it reflects over. `tests/Directory.Build.props` makes a
project here an xUnit v3 test executable that is never packed; `tests/fixtures/Directory.Build.props`
keeps the fixtures plain libraries.

| Path | What it holds |
|---|---|
| `AssemblyQuality.Tests/Rules/RuleTests.cs` | Every rule in both directions, against `Fixtures/` and against the library itself |
| `AssemblyQuality.Tests/Rules/CoverageTests.cs` | What a rule did not see: `Skipped`, facade roots, the internal-types opt-in, against the `Orphan` fixture |
| `AssemblyQuality.Tests/Rules/RulesCatalogTests.cs` | The catalogue: ids unique, summaries present, the rules table in `docs/assemblyquality.md` in sync |
| `AssemblyQuality.Tests/Packaging/PackagingTests.cs` | Every packable project classified in `packages.push` or `packages.local`, and `release.yml` naming what it publishes, never globbing |
| `AssemblyQuality.Tests/Fixtures/` | Compiled subjects: `Offender`, `Overloaded`, `Clean` in `Subjects.cs`; namespace shadows in `Shadowed.cs`, `HiddenShadow.cs`, `OwnRoot.cs` |
| `AssemblyQuality.Tests/Rules/FriendReferenceTests.cs` | `BNAQ1006` against `FriendConsumer` beside `friend-v1/` and `friend-v2/` |
| `fixtures/FriendConsumer/`, `fixtures/FriendV1/`, `fixtures/FriendV2/` | `FriendConsumer` is compiled against `FriendV1` (two internals, granted); `FriendV2` has the same assembly name, a newer pinned `BuildTimestamp`, one internal removed and the grant withdrawn. `friend-v1/` and `friend-v2/` each hold the consumer beside one version |
| `AssemblyQuality.Tests/Rules/FriendGrantTests.cs` | `BNAQ1005` against the `Granting` fixture |
| `fixtures/Granting/` | Four `InternalsVisibleTo` grants: to `AssemblyQuality.Tests`, to an external name, to a namespace-form name, and to a name that exists nowhere. Referenced by the test project. Opts out of solution-wide grants once this repository adopts them |
| `AssemblyQuality.Tests/Rules/LoadContextTests.cs` | References resolve in the scanned assembly's own load context, against the `versioned/` fixtures |
| `fixtures/Dependent/`, `fixtures/VersionOne/`, `fixtures/VersionTwo/` | `Dependent` is compiled against `VersionOne`; `VersionTwo` has the same assembly name, a newer pinned `BuildTimestamp` and a different root namespace. Only `Dependent` and `VersionTwo` are copied into `versioned/` |
| `fixtures/Orphan/`, `fixtures/Absent/` | `Orphan` references `Absent` with `Private="false"`, and only `Orphan`'s dll is copied into the test output's `orphan/` folder, so `Absent` really will not load |

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| Both `Directory.Build.props` files here import the root props explicitly | MSBuild applies only the closest one; without the import a project silently loses the root's target framework and settings | the import line in each |
| A fixture project goes under `tests/fixtures/`, never beside the test project | `tests/Directory.Build.props` would make it an xUnit executable | `tests/fixtures/Directory.Build.props` |
| A type added to `AssemblyQuality.Tests/Fixtures/` is seen by every rule scanning `RuleTests.Subjects` | The whole test assembly is the subject, so a stray public type can add or hide a finding | `RuleTests` |
| `OwnRoot.cs` extends the `System` namespace on purpose | It is the only faithful subject for a namespace whose own root is a referenced root | `RuleTests.ANamespaceSharingItsOwnRootWithAReference_IsNotAShadow` |
| A test relying on a runtime condition asserts that condition first | Otherwise it passes on a dependency that loaded after all | `CoverageTests.TheOrphansReference_ReallyWillNotLoad` |
| **Never weaken a packaging test to make it pass** | They stand between a packaging mistake and a permanent release. When one fails, the package list or the workflow is wrong | `PackagingTests` |

## CodeQuality

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

| Path | What it covers |
|---|---|
| `CodeQuality.Tests/AnalyzerConventionTests.cs` | Every analyzer derives from `ScopedAnalyzer<TScope>` and is sealed; the base seals `Initialize` and its gate receives nothing it could register on; the assembly exports no public type of its own |
| `CodeQuality.Tests/RulesCatalogTests.cs` | Every rule's ID, category and help link; its section of `docs/codequality.md` exists; that file's rules table matches the descriptors |
| `CodeQuality.Tests/Rules/RuleTest.cs` | One analyzer run over one test compilation on the .NET 8 reference assemblies, with `{|BNCQ1004:span|}` markup |
| `CodeQuality.Tests/Rules/Plain.cs` | One analyzer over a plain compilation, configured from `.editorconfig` text as the compiler configures it, reporting its findings and how many actions it registered. For what the testing framework cannot show: a rule that is off until enabled, and a gate that stayed closed |
| `CodeQuality.Tests/Rules/Images.cs` | Compiles a source to a real assembly image, whose metadata references only what it uses, as a package's DLL does |
| `CodeQuality.Tests/Rules/*Tests.cs` | One class per rule: firing, silent, and inert outside its scope |
| `CodeQuality.Tests/Packaging/PackagingTests.cs` | Every packable project is classified in exactly one package list; the release workflow globs nothing |
| `CodeQuality.Tests/Packaging/AnalyzerShapeTests.cs` | Read off the compiled assembly: an analyzer targets `netstandard2.0` and references only what the compiler host provides; a library carries the trimmable mark. Read from the project file: an analyzer packs only itself, under `analyzers/dotnet/cs`, with no dependencies |

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| `tests/Directory.Build.props` imports the root props explicitly | Without it, every test project silently loses the root's target framework and nullable settings | the import line itself |
| The tests reference `Microsoft.CodeAnalysis.CSharp.Workspaces` at the analyzer's own pin | The testing package floors Workspaces at 1.0.1; without the reference the tests restore a 2015 Workspaces (NU1701) and prove nothing about the compiler the package supports | `CodeQuality.Tests.csproj`; `Directory.Packages.props`, `RoslynVersion` |
| Every rule is proved firing, silent and inert, and every firing test was watched failing with the gate returning null | A rule whose gate is wrong fails as silence, so a suite of silence tests passes against a rule that does nothing | the rule test classes; the adding-a-rule checklist in the root `AGENTS.md` |
| An inert test counts registrations through `Plain`, beside a control in scope where the count must not be zero, and was watched failing with the gate forced open | Silence is not inertness: every inert test written with `RuleTest` still passed with all three gates forced open, because the test code was quiet anyway | the `*_registers_nothing` tests in each rule's class |
| A disabled-by-default rule's opt-in is tested through `Plain`, not `RuleTest` | `CSharpAnalyzerTest` enables every diagnostic of the analyzer under test, so a rule that is off by default reads as on there | `CancellationTokenTests.Until_enabled_a_defaulted_token_is_not_reported` |
| A test that needs C# 14 syntax, such as an extension block, runs in the preview language through `RuleTest.InPreview` or `RuleTest.RunPreview` | The Roslyn 4.14 pin parses extension blocks only under preview; under the latest language version it reports CS1513 | `SurfaceLeakTests.An_extension_block_whose_receiver_is_covered_fires_at_the_block` |
| A rule that decides from what an assembly references is tested against `Images`, not a project reference | A project reference in the testing framework carries the whole reference pack, so it "references" everything | `SurfaceLeakTests.A_leak_two_packages_away_fires` |
| In a `RuleTest` with additional projects, the analyzer runs over every project, so a leak in the referenced project is expected there too | An expectation written for the primary project alone fails on the extra diagnostic | `SurfaceLeakTests.A_referenced_type_reaching_a_covered_type_fires_and_names_the_path` |
| The tests under `Packaging/` stay as strong as they are | They are the only thing standing between a packaging mistake and a permanent release | `PackagingTests`, `AnalyzerShapeTests` |

⛔ **Never weaken a packaging or convention test to make it pass.** When one fails, the project,
the analyzer or the package list is wrong, not the test.
