; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
;
; Every release moves the rules it ships from AnalyzerReleases.Unshipped.md into a "## Release <version>"
; section here, in the same change as the tag.

## Release 2026.3.925

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BNCQ1001 | Bennewitz.Ninja.CodeQuality | Disabled | A visible method lets the caller leave the cancellation token out
BNCQ1002 | Bennewitz.Ninja.CodeQuality | Warning | A type from a leak-prone namespace appears in the visible surface
BNCQ1004 | Bennewitz.Ninja.CodeQuality | Warning | A namespace segment shadows the root namespace of a referenced assembly

## Release 2026.4.1104

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BNCQ1005 | Bennewitz.Ninja.CodeQuality | Warning | A System.Reactive time operator is given no scheduler
BNCQ1006 | Bennewitz.Ninja.CodeQuality | Warning | An async delegate is given to a System.Reactive operator that does not await it
BNCQ1007 | Bennewitz.Ninja.CodeQuality | Warning | A producer built by Observable.Create catches what OnNext throws
BNCQ1008 | Bennewitz.Ninja.CodeQuality | Warning | A gate taken in a body is released outside a finally clause
BNCQ1009 | Bennewitz.Ninja.CodeQuality | Warning | A callback no caller catches is not wholly inside a try/catch
BNCQ3001 | Bennewitz.Ninja.CodeQuality | Warning | An Avalonia DispatcherTimer's Tick handler is not wholly inside a try/catch
