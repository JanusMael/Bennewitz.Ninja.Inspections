# AGENTS.md — `src/`

The shipped projects of the four families, each folder named for its family: `XamlQuality*`,
`AssemblyQuality`, `CodeQuality` and `Testing.*`. Each family's packages are published together from that
family's tag, so everything here is permanent once its family releases.

## Rules every family keeps

| Rule | Why | Guarded by |
|---|---|---|
| `src/Directory.Build.props` imports the root props explicitly, and makes every project here trimmable and trim-analysed | MSBuild applies only the closest `Directory.Build.props`, so without the import a project silently loses the root's settings; an unmarked library is kept whole by a consumer's trimmed publish | the import line itself; `repo-conventions check`, `Trimming` |
| A project that cannot be trimmable says so in its csproj, with the reason: the analyzer, because the SDK supports neither property on `netstandard2.0`, and `XamlQuality`, until it is trim-clean, with its exemption in `.github/repository.json` | A claim the assembly does not keep ships inside the package | `CodeQuality.csproj`; `XamlQuality.csproj`; `repo-conventions check` |
| Each package's csproj sets `AssemblyProduct` and `FamilyReadme` for its family | The root's `AssemblyProduct` is the repository's; a package without `FamilyReadme` packs the root `README.md`, the repository's page instead of its family's | `Directory.Build.props`, the `README` items; `repo-conventions check`; `assert-packages --family` |
| A packable project never references another family's packable project | A family's release packs at its own version, so the dependency would name a version of the other family that need not exist | `repo-conventions check`, `families` |
| No project declares a friend grant of its own | Every project grants every assembly the solution builds, through `AssemblyInfo.InternalsVisibleTo.cs` at the root | `repo-conventions check`, `grants` |

## XamlQuality

The family's three projects, packed and published together from one `xamlquality/v…` tag.

| Project | Assembly | Package | What it ships |
|---|---|---|---|
| `XamlQuality/` | `XamlQuality` | `Bennewitz.Ninja.XamlQuality` | The rules (`Rules/`, each an `IXamlRule`), the scan model (`XamlScanContext`, `XamlFile`, `XamlFinding`, `XamlSkip`), and the whole `ThemeAudit` analysis with its reviewed `ThemeAudit/Mappings/*.json` |
| `XamlQuality.ThemeAudit/` | `ThemeAudit` | `Bennewitz.Ninja.XamlQuality.ThemeAudit` | The `theme-audit` dotnet tool: `Program.cs`, argument parsing and reporting over the library's `ThemeAudit` namespace, and nothing else |
| `XamlQuality.ThemeRuntime/` | none shipped | `Bennewitz.Ninja.XamlQuality.ThemeRuntime` | The runtime theme check's Avalonia half as source, `ThemeRuntimeCheck.cs` and `ThemeRuntimeMap.cs`, packed as compile content that an Avalonia test project compiles against its own Avalonia; it depends on the library at a minimum of its release. The project compiles none of the files |

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| `XamlQuality.csproj` takes no `PackageReference`; `System.CommandLine` belongs to the tool alone | The library's dependencies become every consumer's test-project dependencies | `Directory.Packages.props`, group `Tool`; `AssemblyQualityTests.BNAQ1003_no_assembly_references_a_framework_or_another_family_package`, whose `ForbiddenInTheLibraryOnly` names `System.CommandLine` |
| Analysis goes in the library, never in `Program.cs` | A consumer's test project runs the same analysis without shelling out to the tool | `XamlQuality.ThemeAudit.csproj`, `Description` |
| The tool's `AssemblyName` stays `ThemeAudit` and its `ToolCommandName` stays `theme-audit` | the repository's friend grant to `ThemeAudit` is how the tool reaches `ResourceKeyScanner` and `ResourceKey`; the command name is what consumers' scripts and CI steps invoke | `AssemblyInfo.InternalsVisibleTo.cs`; `XamlQuality.ThemeAudit.csproj` comment on `ToolCommandName` |
| The audit's model of Avalonia's resource lookup lives in two internal places, `ThemeGraphWalker`, which builds each dictionary with the chain it was loaded in, and `ResourceApplication`, which searches them, and `DrawnColourTests` draws each of its rules; the redefinitions check, `UnseenCheck`, reads it through `ResourceApplication.Find` and `ResourceApplication.Locate` and keeps no model of its own | A rule changed in one and not the other, or never drawn, scores a colour Avalonia never draws, and a check with a model of its own disagrees with the colours the report scores | `ResourceNode` and `ResourceApplication` remarks; `UnseenCheck` remarks; `tests/XamlQuality.Tests/ThemeAudit/AliasScopeTests.cs`, `UnseenRedefinitionTests.cs`; `DrawnColourTests`, `UnseenRedefinitionDrawnTests` |
| `Properties/AssemblyInfo.cs` names which `ThemeAudit` types are public and which stay internal | The audit surface shipped public and a consumer binds to it, so narrowing a type is a breaking change | `src/XamlQuality/Properties/AssemblyInfo.cs` comment |
| Every public member carries an XML doc comment | `GenerateDocumentationFile` is on in the library's and the tool's csproj files and warnings are errors, so a missing comment fails the build | `XamlQuality.csproj`, `XamlQuality.ThemeAudit.csproj`; root `Directory.Build.props`, `TreatWarningsAsErrors` |
| The source-only project's files compile in any adopting project: each starts `// <auto-generated/>` and `#nullable enable`, has no using directive, names every type outside it from `global::`, and declares every type `internal` | An adopting project may turn implicit usings off, declare global aliases of its own, and build with warnings as errors and analyzers of its own; and this project, which has no Avalonia, compiles none of them | `SourceOnlyPackageTests`; `tests/XamlQuality.ThemeRuntime.Tests`, which compiles them with implicit usings off and a global alias of each Avalonia and library type name they use |
| The `None` item for `ThemeAudit/Mappings/*.json` keeps `Link`, `PackageCopyToOutput` and `PackagePath` | `CompatMapping.Locate` resolves a bare mapping name against `Mappings/` beside the assembly; without them a `PackageReference` consumer gets no files there | `XamlQuality.csproj`, the comments on that item |
| `theme-audit` exits 0 when clean, 1 on drift, an unreadable file or a brand document that cannot be filled, 2 on a configuration error; every `--check` runs the library's drift check, `OutputDrift.Check`, and every output is written through `OutputDrift.Write`, which removes the received copy a stale check left | CI steps in consuming repositories branch on the code, and a check written again in `Program.cs` would compare differently from a consumer's test | `Program.cs`, the `compat`, `report`, `parts` and `brand` actions; `tests/XamlQuality.Tests/ThemeAudit/ThemeAuditToolTests.cs` |
| A XamlQuality project added here is also referenced from `tests/XamlQuality.Tests/XamlQuality.Tests.csproj`, a source-only one with `ReferenceOutputAssembly` false, and its package id named in `packages.push`, or in `packages.local` if it must never be published | `AssemblyQualityTests` scans every XamlQuality csproj under `src` but a source-only one and fails when its assembly is not in the test output; `SourceOnlyPackageTests` packs a source-only one without a build, which restores nothing, so the reference is what readies it; the release publishes only the packages `packages.push` names, and stops on one that packs and neither list names | `AssemblyQualityTests.Every_shipped_assembly_is_in_the_scan`; `SourceOnlyPackageTests.Each_packs_without_a_build`; `PackagingTests.Every_packable_project_is_classified`; `scripts/assert-packages.cs` |

## AssemblyQuality

The one shipped project, `AssemblyQuality`, packed as `Bennewitz.Ninja.AssemblyQuality`. It holds the
rule contract in the project root and the rules in `Rules/`. The contract every rule keeps is in the
root [AGENTS.md](../AGENTS.md); this section names where each part of it lives.

| File | What it is |
|---|---|
| `IAssemblyRule.cs` | `IAssemblyRule` (`Id`, `Summary`, `Analyze`) and `AssemblyRuleResult` (`Findings`, `Inspected`, `Skipped`, `Clean`) |
| `AssemblyFinding.cs` | One violation: `RuleId`, `AssemblyName`, `Subject`, `Message`, and `Source`, an `init` property, the file and line where a rule that reads method bodies has symbols; null for a rule that reads metadata, which carries none |
| `SourceLine.cs` | `SourceLine`, a file and a line from a portable PDB's sequence points |
| `IL/MethodBodies.cs` | Plan 00011's IL reader, internal: every method body of an assembly as ICSharpCode.Decompiler's transformed ILAst, raw where the transforms fail, each use credited by the compiler's mangled names to the method the source wrote, each kept once by its body's token and IL offset, each given its line. `AssemblyBodies` holds the file open until the rule has matched against it |
| `IL/KeyFlow.cs` | Plan 00011 decision 6's following, internal: every call and delegate the scan makes, each kept from the reading that sees its captured locals, and a key argument followed through locals, ternaries, `??`, tuples and a parameter's callers, to a depth of eight, to a constant, a parameter marked `Bennewitz.Ninja.ResourceKeyAttribute`, or unknown, with why |
| `IL/Ancestry.cs` | Every type a type derives from or implements, read from metadata across its references' closure through the decompiler's resolver, since the decompiler's type system loads the scanned assembly's own references and no further |
| `Rules/ConfinedUse.cs` | `BNAQ1010`'s public surface: `ConfinedUse`, a confinement made by `Constructing`, `AnyMemberOf` or `Member` and shaped by its `init` options, `KeyParameter` among them, which allows a use by its key; `UseKind`; `ConfinedTarget`; `ConfinedUseMatch` and `ConfinedUseReading`, its reading |
| `Rules/ConfinedUseRule.cs` | `BNAQ1010`: a named type or member is used only where its confinement allows it. `Read` lists every matched use, base-constructor chains included; `Analyze` reports the ones not allowed. No default confinement, so unconfigured it inspects nothing |
| `Rules/HelperKey.cs` | `BNAQ1011`'s public surface: `MarkedParameter`, `HelperKeyCall`, `KeyValue` and `KeyValueKind`, and `HelperKeyReading`; `KeyValue` is `BNAQ1010`'s too, each key a match takes |
| `Rules/HelperKeyRule.cs` | `BNAQ1011`: every key a marked parameter is given is one of its key class's constants, and every such constant is given to one. Takes no configuration, only the attribute, matched by its full name; `Read` lists every marked parameter, every call to one with the values its key takes, and every delegate made of one |
| `AssemblyScanContext.cs` | The assemblies a scan covers (`Of`, `ExportedTypes`), plus the internal helpers rules share: `AllTypes`, `ReferencedNamespaces`, `IsLoadFailure`, `Unreadable`, `TrimMessage` |
| `Rules/CancellationTokenRule.cs` | `BNAQ1001`: no public method takes a defaulted `CancellationToken`, nor is a token-less overload of a sibling that takes one. A policy rule, not a defect rule |
| `Rules/SurfaceLeakRule.cs` | `BNAQ1002`: no type from a leak-prone namespace in the public surface, generic arguments unwrapped. Defaults to `LeakProneNamespaces`; `Only(...)` replaces the set |
| `Rules/ForbiddenReferenceRule.cs` | `BNAQ1003`: no direct reference whose simple name starts with a forbidden prefix. No default set, so unconfigured it inspects nothing |
| `Rules/FriendGrantRule.cs` | `BNAQ1005`: every `InternalsVisibleTo` grant names an assembly the caller allows. No default allow-list, so unconfigured it inspects nothing. Reads the assembly's own attributes and never skips |
| `Rules/FriendReferenceRule.cs` | `BNAQ1006`: every reference into an assembly outside the scan and the shared framework resolves, via `Module.ResolveType` / `ResolveMember`, and every one into an internal is still granted. Runs at a provider's release, against published consumers. Reads metadata from the assembly's file, in its own load context |
| `Rules/NamespaceShadowRule.cs` | `BNAQ1004`: no namespace segment after the first shadows a referenced assembly's root. Public types by default; `IncludingInternalTypes()` widens it |

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| A rule is a public, concrete `IAssemblyRule` in this assembly with a parameterless constructor | The catalogue is discovered by reflection and each rule created through `Activator.CreateInstance`; the shared helpers on `AssemblyScanContext` are `internal` | `RulesCatalogTests.DiscoverRules` |
| `Id` is unique, is `BNAQ` and four digits, and never changes; `Summary` states the requirement and ends with a full stop | Consumers suppress by id (`IAssemblyRule.Id`); the summary is the row in `docs/assemblyquality.md` (`IAssemblyRule.Summary`) | `RulesCatalogTests.EveryRuleId_IsUnique`, `EveryRuleId_CarriesTheFamilyPrefix`, `EveryRule_SummarisesWhatItRequires`, `TheCatalogue_IsNotSilentlyEmpty` |
| A rule whose predicate cannot be satisfied returns `Inspected` of zero | Otherwise "nothing checked" reads as "checked and clean" | `RuleTests.EveryRule_GivenNothing_ReportsThatItInspectedNothing`, `ALeakSetNothingInReachExports_ReportsThatItInspectedNothing`, `AForbiddenReferenceRuleWithNoConfiguration_ReportsThatItCheckedNothing`, `AnAssemblyWithNoReferencedRoots_ReportsThatItComparedNothing` |
| A load failure is caught with `AssemblyScanContext.IsLoadFailure`, recorded with `Unreadable` into `Skipped`, and the scan continues | One missing dependency must not take the scan down | `CoverageTests.AReferenceThatWillNotLoad_IsNamedInSkipped`, `ATypeWhoseSignatureCannotBeRead_IsSkippedNotFatal`, `AScanWithEverythingPresent_SkipsNothing` |
| Read a type's whole signature set before counting any of it | A throw part-way through leaves a count matching neither what was examined nor what was not | `CancellationTokenRule.Analyze`, comment |
| Referenced roots come from `AssemblyScanContext.ReferencedNamespaces`, which includes forwarded types | `System.Runtime` is a facade that exports nothing | `CoverageTests.ARootReachableOnlyThroughAFacade_IsComparedAgainst` |
| Every reflection entry point carries `[RequiresUnreferencedCode(AssemblyScanContext.TrimMessage)]` | `IsAotCompatible` turns on the trim analyzers and warnings are errors, so an unmarked one fails the build | `AssemblyQuality.csproj`, `IsAotCompatible` |
| `SurfaceLeakRule.LeakProneNamespaces` grows only deliberately | Adding a namespace changes findings for every consumer; growth belongs at the call site | `SurfaceLeakRule` remarks |
| The library's one package dependency is ICSharpCode.Decompiler, by plans/00011, and no public signature names one of its types | Every consumer takes the package with the library; a decompiler type in the surface would make its version part of this library's contract | `Directory.Packages.props`, group `Reader`; `ConfinedUse.cs`, whose types name strings and the library's own records |
| A rule that reads method bodies reads them through `MethodBodies.Read` and disposes what it returns once it has matched, and follows a key through `KeyFlow` | One reader credits a use one way for every rule, the file is held open only as long as the type system reads from it, and `BNAQ1010`'s key option and `BNAQ1011` cannot follow one key two ways | `ConfinedUseRule.Read`, `HelperKeyRule.Read` |
| The library references neither `System.Text.Json` nor `Newtonsoft.Json`, and none of its own namespaces shadows a referenced root | The tests use this assembly as the clean subject for `BNAQ1002` and `BNAQ1004` | `RuleTests.ALeakSetNothingInReachExports_ReportsThatItInspectedNothing`, `AnAssemblyWithNoShadowingSegment_IsClean` |
| A new or changed `Summary` regenerates the table in `docs/assemblyquality.md` | The table is generated from the rule types | `RulesCatalogTests.TheReadmeTable_MatchesTheRuleTypes`; set `AQ_UPDATE_DOCS=1` and run the tests |
| Every rule has a `### {Id}` section in `docs/assemblyquality.md`, naming its `BNCQ` counterpart where one exists | Bennewitz.Ninja.CodeQuality links each source-side rule to that anchor | `RulesCatalogTests.EveryRule_HasAReadmeSectionOfItsOwn` |

## CodeQuality

The family's one project, the analyzer, packed as `Bennewitz.Ninja.CodeQuality`.

| Path | What it is |
|---|---|
| `CodeQuality/CodeQuality.csproj` | The analyzer: `netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, the DLL packed under `analyzers/dotnet/cs` with `IncludeBuildOutput` off, a `DevelopmentDependency`, trimming opted out with the reason; the tests reach its internals through the repository's friend grants |
| `CodeQuality/ScopedAnalyzer.cs` | `ScopedAnalyzer<TScope>`, the base every rule derives from: a sealed `Initialize`, a gate that receives no context it could register on, and a registration hook that runs only in scope |
| `CodeQuality/Catalog.cs` | What every rule says about where it comes from: the category and the help link into `docs/codequality.md` |
| `CodeQuality/Surface.cs` | `IsVisibleOutside`: public or protected at every level, which is the surface the rules examine |
| `CodeQuality/Rules/NamespaceShadowAnalyzer.cs` | `BNCQ1004`: a namespace segment that shadows a referenced root namespace |
| `CodeQuality/Rules/SurfaceLeakAnalyzer.cs` | `BNCQ1002`: a type from a leak-prone namespace in the visible surface, walked transitively through referenced types |
| `CodeQuality/Rules/CancellationTokenAnalyzer.cs` | `BNCQ1001`: a defaulted cancellation token, or a token-less overload of a tokened sibling; off by default |
| `CodeQuality/LogTemplates.cs` | A message template as Serilog and Microsoft.Extensions.Logging read one, its holes, and the first place it leaves the family's one form for a log line, by plans/00014 decision 5 |
| `CodeQuality/Rules/LogLineFormAnalyzer.cs` | `BNCQ1012`: a log template not in the family's one form, one that is not a constant, a `:l` hole, an enum given to a hole. It analyses generated code and leaves out only a file a tool wrote, by its name or header, since Roslyn takes a partial method for generated where its other half carries `[GeneratedCode]`: a consumer's own `[LoggerMessage]` method, or an MVVM Toolkit hook |
| `CodeQuality/AnalyzerReleases.Unshipped.md`, `AnalyzerReleases.Shipped.md` | Release tracking (RS2007, RS2008): every rule is listed in one of them |

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| The project is packable and its id is declared in `packages.push` | An undeclared package is either published by accident or silently never published | `PackagingTests` |
| The analyzer's csproj sets `TargetFramework` to `netstandard2.0`, overriding the root's `net10.0` | The compiler inside Visual Studio's .NET Framework MSBuild cannot load a `net10.0` analyzer, and says so only as a warning most builds never show | `AnalyzerShapeTests` reads the target off the compiled assembly; `repo-conventions check` holds the analyzer role to `netstandard2.0` |
| The analyzer opts out of `IsTrimmable` and `EnableTrimAnalyzer` in its csproj, with the reason; a library added here gets both from `src/Directory.Build.props` | The SDK does not support either on `netstandard2.0`: it warns `NETSDK1212`, which CI's `-warnaserror` build makes an error. The compiler loads an analyzer, and no consumer publishes it trimmed | `CodeQuality.csproj`; `AnalyzerShapeTests` holds a library to the mark and an analyzer to its target |
| `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers` are `PrivateAssets="all"`, and nothing else is referenced | An analyzer takes no package dependency at run time: a DLL not packed beside it fails to load as `AD0001`, and the rule goes quiet | `AnalyzerShapeTests` lists what the compiled assembly may reference |
| Every analyzer is `internal sealed` and derives from `ScopedAnalyzer<TScope>`; its scope type is `internal` too | The compiler runs every analyzer in the assembly in every compilation; the base is what makes each one decide for itself. A constructed base type may not be less accessible than the class deriving from it: a private scope type fails with `CS9338` | `AnalyzerConventionTests`; the compiler |
| Source stays within what `netstandard2.0` compiles without polyfills: no records, no `init`, no `required`, no nullable attributes such as `[NotNullWhen]` | The root props set `LangVersion` to `preview`, so the syntax is accepted and the missing runtime type is the error | the build |
| An extension block's receiver is read only through `SurfaceLeakAnalyzer.ExtensionBlock`, which reads the members Roslyn 4.14 declares on `ITypeSymbol` from a method that is never inlined | Those members are the older copy Roslyn keeps beside its newer `INamedTypeSymbol` pair. Read directly, a future compiler that dropped them would raise `AD0001` on every type; read there, the rule stops reading receivers instead | `SurfaceLeakAnalyzer.ExtensionBlock`, its remarks |
| Symbols compare through `SymbolEqualityComparer`, never `==` | RS1024; two symbols for one type from different compilations are not reference-equal | `Microsoft.CodeAnalysis.Analyzers` |
| A rule's gate does cheap work only: metadata-name lookups, top-level namespace walks, a read of the options | The gate runs at every compilation start in every project that references the package, in scope or not | review; the remarks on each analyzer say what its gate reads |

XML doc remarks follow the family's house style: one `<para>` per point, each opening with a marker
and a bold claim: ⭐ key idea, ⚠ caution, ⛔ trap. Explain *why*, and say "Measured:" when a claim
comes from an observed failure. Finding messages state the defect, why it bites, and every valid fix.

## Testing

The family's one project, `Testing.Mutate`, packed as the dotnet tool `Bennewitz.Ninja.Testing.Mutate`, whose
command is `mutate`. The root `AGENTS.md`'s Testing section names each file.

### Rules

| Rule | Why | Guarded by |
|---|---|---|
| The `ToolCommandName` stays `mutate` | It is what a repository's tool manifest and its scripts invoke, as `dotnet mutate` | `Testing.Mutate.csproj` |
| Every type is internal, so the project generates no documentation file | The tool has no API; the documentation file would turn the moved code's partial `<param>` tags into errors and describe nothing a consumer reads | `Testing.Mutate.csproj` |
| What the tool kept from DiffView's runner is changed only where `plans/00008`, or a later plan, says, and each file says so in its first line, naming every plan that changed it | DiffView's committed sets and its tests read the runner as it was; a quiet change is one they meet as a wrong verdict | the first line of each moved file; `MutateTests` |
