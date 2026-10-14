// Global aliases named after the Avalonia and library types the runtime check's files use, each for an
// unrelated type, as an adopting project's own global aliases may be: DiffView already aliases
// ThemeVariant in its test files, and a project that makes such an alias global must still compile the
// package. The files name every type from global::, so none of these may take a name in them; a name
// they leave unqualified fails to compile here. This project's own files declare their usings inside
// their namespace, which is searched before these.
global using Application = System.Text.StringBuilder;
global using AuditResult = System.Text.StringBuilder;
global using Dispatcher = System.Text.StringBuilder;
global using IResourceDictionary = System.Text.StringBuilder;
global using IResourceProvider = System.Text.StringBuilder;
global using IStyle = System.Text.StringBuilder;
global using ResourceDictionary = System.Text.StringBuilder;
global using ResourceInclude = System.Text.StringBuilder;
global using RuntimeAnswer = System.Text.StringBuilder;
global using RuntimeCheckPlan = System.Text.StringBuilder;
global using RuntimeCheckResult = System.Text.StringBuilder;
global using RuntimeLookup = System.Text.StringBuilder;
global using RuntimeSetup = System.Text.StringBuilder;
global using Styles = System.Text.StringBuilder;
global using ThemeVariant = System.Text.StringBuilder;
