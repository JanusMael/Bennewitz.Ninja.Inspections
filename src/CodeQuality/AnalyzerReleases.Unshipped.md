; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BNCQ1005 | Bennewitz.Ninja.CodeQuality | Disabled | A System.Reactive time operator is given no scheduler
BNCQ1006 | Bennewitz.Ninja.CodeQuality | Warning | An async delegate is given to a System.Reactive operator that does not await it
BNCQ1007 | Bennewitz.Ninja.CodeQuality | Warning | A producer built by Observable.Create catches what OnNext throws
BNCQ1008 | Bennewitz.Ninja.CodeQuality | Warning | A gate taken in a body is released outside a finally clause
BNCQ1009 | Bennewitz.Ninja.CodeQuality | Disabled | A callback no caller catches is not wholly inside a try/catch
BNCQ3001 | Bennewitz.Ninja.CodeQuality | Disabled | An Avalonia DispatcherTimer's Tick handler is not wholly inside a try/catch
