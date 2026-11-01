# Bennewitz.Ninja.CodeQuality

Roslyn analyzers for the shape of a .NET library's source: a namespace segment that shadows a
referenced root namespace, an implementation type leaking into the public surface, a cancellation
token a caller can leave out, and the System.Reactive calls where one exception ends the process or
vanishes. Three rules are the compile-time counterparts of
[Bennewitz.Ninja.AssemblyQuality](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/assemblyquality.md)
rules that read the shipped assembly, and carry their numbers: `BNCQ1004` finds while you type what
`BNAQ1004` finds after the build. A rule with no counterpart takes the next free number, and a rule
about Avalonia the next from `BNCQ3001`, so the rules about a framework you may not use stand apart.
A rule's section names its counterpart, if it has one.

## Install

```bash
dotnet add package Bennewitz.Ninja.CodeQuality
```

A development dependency: the analyzer runs inside the compiler, ships nothing at run time, and is
not passed on to whoever references your package. It needs a compiler from Roslyn 4.14 on, which is
Visual Studio 2022 17.14 or a .NET 9.0.3xx SDK or later. An older compiler, such as the .NET 8
SDK's, reports `CS9057` and skips the analyzer: every rule goes quiet, and the build fails only
where warnings are errors.

## Rules

<!-- BEGIN GENERATED RULES -->
| Id | Default | Reports |
|---|---|---|
| [`BNCQ1001`](#bncq1001) | off | A visible method lets the caller leave the cancellation token out |
| [`BNCQ1002`](#bncq1002) | on | A type from a leak-prone namespace appears in the visible surface |
| [`BNCQ1004`](#bncq1004) | on | A namespace segment shadows the root namespace of a referenced assembly |
| [`BNCQ1005`](#bncq1005) | off | A System.Reactive time operator is given no scheduler |
| [`BNCQ1006`](#bncq1006) | on | An async delegate is given to a System.Reactive operator that does not await it |
| [`BNCQ1007`](#bncq1007) | on | A producer built by Observable.Create catches what OnNext throws |
<!-- END GENERATED RULES -->

The table is rendered from the analyzers' own descriptors, and a test fails if it drifts.
`BNCQ1003` is reserved: its counterpart
[`BNAQ1003`](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/assemblyquality.md#bnaq1003) forbids a
reference, and a forbidden use site at compile time is already
[Microsoft.CodeAnalysis.BannedApiAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)'
job.

Where a rule comes from is shown by its help link (its section below), its category
(`Bennewitz.Ninja.CodeQuality`) and the package id, not by a longer ID. `BNCQ` is `BN` plus the
product's initials, the scheme across the Bennewitz.Ninja quality packages, and it will not change.

**Every rule decides once per compilation whether it applies**, and registers nothing where it
does not: a project the rule has nothing to compare against pays a few lookups at compilation
start and nothing per file. A build with no findings cannot tell that from "checked and clean",
which is why each rule's tests show it firing on a real violation as well as staying silent.

### BNCQ1001

**A visible method lets the caller leave the cancellation token out.** Off by default. Counterpart:
[`BNAQ1001`](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/assemblyquality.md#bnaq1001).

A `CancellationToken` parameter with a default value hands every caller who forgets it
`CancellationToken.None`: an operation that cannot be cancelled, chosen by nobody, visible nowhere.
The call compiles, runs, and hangs on shutdown. An overload that leaves the token out while a
same-named sibling takes it is the same default, moved one overload over, and is reported too:
when its parameters are the sibling's with the token removed, or a leading run of them.

The fix that holds is to require the token **ahead of any optional parameter**, so that a caller
who means `CancellationToken.None` has to write it. Dropping the token's default alone is CS1737
when an optional parameter follows it.

This is a policy, not a defect: the BCL defaults its own tokens everywhere, and a finding is not
evidence of a bug. It is the rule a library adopts when its cancellation contract is load-bearing.
Turn it on in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.BNCQ1001.severity = warning
```

Visible means public or protected at every level, so a protected method counts, and so does a
constructor; `BNAQ1001` reads neither. Compiler-generated members, accessors, operators and
explicit interface implementations are skipped: a caller cannot pass them a token by name. Inert in
a compilation that cannot name `CancellationToken`.

### BNCQ1002

**A type from a leak-prone namespace appears in the visible surface.** Counterpart:
[`BNAQ1002`](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/assemblyquality.md#bnaq1002).

Return a `JsonNode` from one public method and every consumer now binds against
`System.Text.Json`, its version, its behaviour and its breaking changes, whether or not they
serialize anything. Swapping the serializer later is then a breaking change to an API that was
never about serialization. The rule reads every visible member's signature, what a type derives
from and implements, and generic constraints. It unwraps generic arguments, including a containing
type's, and array elements, so a `Task<JsonNode>`, a `List<JsonNode>.Enumerator` and a
`where T : JsonNode` leak exactly as much as a bare `JsonNode`.

Unlike `BNAQ1002`, the walk is **transitive through referenced types**: a public `Envelope` from
another package whose `Payload` is a `JObject` binds the consumer to Newtonsoft one hop further,
and the message names the hop. A covered type the signature names itself is reported before any
longer path. Your own types are not opened, because their members are reported where they are
declared. A referenced type is opened only if its assembly can reach a covered namespace, directly
or through its own references, which keeps the walk off `Task<T>`, `List<T>` and most of the
framework.

A C# 14 `extension(...)` block exposes its receiver: calling `node.Touch()` binds the consumer to
`JsonNode` as surely as a classic `this JsonNode` parameter does. So a covered receiver, or a
covered constraint on a generic block, is reported at the block, and the members inside are checked
like any other.

The default namespaces are the ones that leak in practice: `System.Text.Json.Nodes` and
`Newtonsoft.Json.Linq`. The type most likely to leak from your library is one no general list
will name, so `.editorconfig` **replaces** the list with yours, comma or semicolon separated. A
prefix covers the namespace and everything beneath it on a segment boundary.

```ini
[*.cs]
bennewitz_ninja_codequality.BNCQ1002.namespaces = System.Text.Json.Nodes, Newtonsoft.Json.Linq, Acme.Internal
```

The list is read once per compilation, from a global config first and then from the first source
file whose options carry it, so it belongs in the repository's root `.editorconfig`. An empty list
forbids nothing. Inert where no covered namespace is in reach. A library whose surface is JSON on
purpose, such as a JSON editor, exposes these types by design: set the list to the namespaces it
does not mean to expose, or empty it.

### BNCQ1004

**A namespace segment shadows the root namespace of a referenced assembly.** Counterpart:
[`BNAQ1004`](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/assemblyquality.md#bnaq1004).

C# resolves the first identifier of a qualified name by walking outward from the current
namespace. Inside `Acme.Widgets.Avalonia`, the name `Avalonia` finds your namespace first, and
`Avalonia.Media.Color` fails to compile looking for `Acme.Widgets.Avalonia.Media.Color`: CS0234,
reported at the use site, about a type that plainly exists, possibly years after the namespace was
declared. The rule reports the segment itself, in every declaration that carries it.

Only segments after the first can shadow. A namespace's own root is the tree it already lives in,
so `Acme.Widgets` beside a referenced `Acme.Other` resolves perfectly. A root is a top-level
namespace a referenced assembly declares a type you can see in; a namespace holding only internal
types counts only where that assembly makes its internals visible to yours, and a reference behind
an `extern alias` counts only when one of its aliases is `global`.

Two fixes, not one. Where namespaces follow the assembly name, renaming only the namespace breaks
that convention, so the fix there renames assembly and namespace together, `.Avalonia` to
`.AvaloniaUI`. The package id takes no part in name resolution and may keep the word. Inert in a
compilation whose references declare no visible namespace.

### BNCQ1005

**A System.Reactive time operator is given no scheduler.** Off by default. No counterpart.

`Throttle`, `Sample`, `Interval`, `Timer`, `Delay`, `Buffer` and `Window` by time, and `Timeout`,
deliver on Rx's default scheduler when they are given none. A throw there has no caller to go back
to, so it ends the process, and an `onError` does not stop that. `Timeout` with no `onError` ends it
with no code of yours throwing at all. The rule reports the call, at the operator's name, when its
overload takes a `TimeSpan` or a `DateTimeOffset` and no `IScheduler`. What was measured is in
[the time operator's entry](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/exception-resilience.md#a-time-operator-with-no-scheduler-ends-the-process-on-a-throw-even-with-an-onerror)
of `docs/exception-resilience.md`.

The fix is to pass a scheduler: the UI scheduler for work that touches the view, a pool scheduler
otherwise. Then guard the handler, because a pool scheduler turns the ending into a subscription
that dies silently.

Only the operators measured to deliver on the scheduler are named: `Replay`, `Skip` and `Take` by
time are not. A scheduler you chose, `Scheduler.Default` included, is not reported either. It ends
the process on a throw too, but choosing one is not a defect the rule can see.

The finding is the shape, and whether a throw can follow is decided by what runs downstream. On a
real application's code, three of its five findings had nothing downstream that could throw on real
input, and two ended the process. So it is off until you turn it on in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.BNCQ1005.severity = warning
```

Inert in a compilation that cannot name `System.Reactive.Linq.Observable`.

### BNCQ1006

**An async delegate is given to a System.Reactive operator that does not await it.** No counterpart.

`Subscribe`, `Do` and `Finally` take an `Action`, so an async lambda given to one is async void: its
exception is rethrown on the thread it resumed on, and off a UI thread that ends the process. An
async `Select` makes each value a task nobody awaits, and a failure in one goes unobserved. The rule
reports an async lambda or anonymous method given to a void delegate parameter of System.Reactive's
`Observable` or `ObservableExtensions`, at its `async` keyword, and a `Select` whose selector returns
`Task` or `Task<T>`, at its name. What was measured is in
[the async lambda's entry](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/exception-resilience.md#an-async-lambda-given-where-a-void-delegate-is-expected-is-async-void)
of `docs/exception-resilience.md`.

The fix is `SelectMany` with `Observable.FromAsync`, which awaits the work and carries its failure to
`onError`, or a synchronous handler:

```csharp
source.SelectMany(value => Observable.FromAsync(() => SaveAsync(value)))
    .Subscribe(_ => { }, error => log.Error(error));
```

A `Select` whose tasks go straight to `Concat`, `Merge` or `Switch` is not reported: those take an
observable of tasks and await each. An async lambda given to any other void delegate is
`Microsoft.VisualStudio.Threading.Analyzers`' VSTHRD101, and an async void method passed as a method
group is its VSTHRD100, at the method, so neither is reported here. Inert in a compilation that
cannot name `System.Reactive.Linq.Observable`.

### BNCQ1007

**A producer built by Observable.Create catches what OnNext throws.** No counterpart.

A subscriber's throw comes out of `OnNext` after Rx has disposed the producer's subscription. A catch
around the call swallows that failure, and the producer carries on for nobody: every subscriber of a
shared source stops, and nothing says why. The rule reports the `OnNext` of the observer
`Observable.Create` hands its delegate, followed into the lambdas and local functions written inside
it, when the call sits in a try block whose catch can carry on: its end can be reached, or it
continues a loop. What was measured is in
[the producer's entry](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/exception-resilience.md#a-producer-that-catches-its-subscribers-throws-talks-to-a-dead-observer)
of `docs/exception-resilience.md`.

The fix is to catch only the producer's own work, and call `OnNext` outside the try:

```csharp
string text;
try
{
    text = File.ReadAllText(path);
}
catch (IOException error)
{
    observer.OnError(error);
    return Disposable.Empty;
}

observer.OnNext(text);
```

A catch that rethrows, returns or breaks out is not reported. Neither is a catch around a
`Subject`'s `OnNext`: there it is a guard, and the subject's other subscribers kept every value. Inert
in a compilation that cannot name `System.Reactive.Linq.Observable`.

## Licence

MIT. See [LICENSE](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/LICENSE).
