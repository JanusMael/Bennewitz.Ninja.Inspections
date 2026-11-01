; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BNCQ1005 | Bennewitz.Ninja.CodeQuality | Disabled | A System.Reactive time operator is given no scheduler
BNCQ1006 | Bennewitz.Ninja.CodeQuality | Warning | An async delegate is given to a System.Reactive operator that does not await it
BNCQ1007 | Bennewitz.Ninja.CodeQuality | Warning | A producer built by Observable.Create catches what OnNext throws
