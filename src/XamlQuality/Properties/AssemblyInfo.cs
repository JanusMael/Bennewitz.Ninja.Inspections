using System.Runtime.CompilerServices;

// The CLI face of this library, which reaches the seven genuinely internal helpers below.
//
// ⛔ THE AUDIT SURFACE IS PUBLIC, AND HAS BEEN SINCE 2026.3.920. Whatever the intent, that is what
// shipped: 53 public top-level types under ThemeAudit/ against 7 internal. AuditRunner,
// AuditConfig, MarkdownReport, AuditResult, CompatOutcome, CompatHow, CompatEntry,
// ConsumerThemeFindings, UndefinedKeyFinding, ContrastFinding, ContrastStatus and the whole config
// surface are all reachable by any consumer, and Bennewitz.Ninja.DiffView binds to eleven of them.
//
// ⚠ NARROWING IT IS A BREAKING CHANGE. A published surface is a promise once someone binds to it,
// and one already has. Curating a smaller, deliberate audit API is still worth doing — but it is a
// major-version decision with a migration path, not a tidy-up, and the version to beat is
// 2026.3.920.
//
// Internal, and staying that way: NamedColors, XamlFiles, ResourceKeyScanner, ThemeGraphWalker,
// ResourceKey, VariantId, WalkResult.
[assembly: InternalsVisibleTo("ThemeAudit")]

// The test project asserts against the ported analysis directly, exactly as it did before the
// move — which is what makes those fourteen files evidence that the move changed no behaviour.
[assembly: InternalsVisibleTo("XamlQuality.Tests")]

// The rules' surface is public as well, and as permanent: each IXamlRule, the scan model, and
// BNXQ1003's reading, TemplatePartRule.ReadParts with its six types, TemplatePartReading,
// ControlTemplateParts, TemplatePartLookup, TemplatePartOrigin, ThemeParts and DeclaredPart. The
// reading is public by plans/00002, so that a consumer's parts reference is the reading the rule
// checks rather than a second one; its names bind once a release carries them. Its step B adds four
// public ThemeAudit types, as permanent: OutputDrift and DriftState, the drift check a consumer's
// test shares with the tool, and PartsConfig and PartsRunner, the parts report's configuration and
// reading. Plans/00003 adds seven more, as permanent: RuntimeCheckPlan, RuntimeSetup, RuntimeLookup,
// RuntimeAnswer, RuntimeCheckResult, RuntimeDifference and RuntimeDifferenceKind, the runtime theme
// check's library half, which the source package Bennewitz.Ninja.XamlQuality.ThemeRuntime calls. A
// member added to them later is an init property, never a new positional parameter: an adopting
// project may compile an older source half against a newer library. Plans/00004 adds three init
// properties to the audit's ResourceValue records, as permanent: ColorLiteral.OpacityKey, and
// Alias.OpacityKey and Alias.FromColor, which record a brush's Opacity written as a resource's key
// and a brush's Color written as an alias. Its brand output adds, as permanent, the init property
// ResourceDefinition.Line, AuditConfig.Brand, and four types: BrandConfig and BrandFrame, a brand
// document's configuration, and BrandRunner and BrandResult, which fill one; a consumer's own program
// calls BrandRunner.Fill with the blocks only its code can render.
