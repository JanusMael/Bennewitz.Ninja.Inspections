using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using Bennewitz.Ninja.XamlQuality;

// Runs the rules over a consumer's markup and build output, and prints every result on a line of its
// own: each rule's counts, then its findings and skips, sorted, so that two runs compare as text. The
// first line names the library by its hash, which differs from build to build, so tools/real-code.cs
// keeps it out of the record.
//
// args: <rules> <markup root> <bin folder> [<assembly file in the bin folder>...]
//       <rules> --each <folder> <bin folder> [<assembly file in the bin folder>...]
//   <rules> is "all", every rule the library defines, or a comma-separated list of rule ids.
//   --each runs the rules over every folder in <folder> as a markup root of its own, in ordinal order,
//   each under a line naming it, "[<name>]", all in this one process: tools/probe.cs reads each of the
//   probe's cases so.

bool each = args.Length > 1 && args[1] == "--each";
if (args.Length < (each ? 4 : 3))
{
    Console.Error.WriteLine("usage: Runner <all|BNXQnnnn,...> <markup root | --each <folder>> <bin folder> [<assembly file>...]");
    return 2;
}

// ⚠ A redirected console on Windows writes in its code page, which mangles any message that is not
// ASCII, so a record written there would differ from one written on Linux.
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

Assembly library = typeof(IXamlRule).Assembly;
Console.WriteLine($"library {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(library.Location)))[..16]}");

string markup = each ? args[2] : args[1];
string bin = Path.GetFullPath(each ? args[3] : args[2]);
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    string candidate = Path.Combine(bin, name.Name + ".dll");
    return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
};

// Found as RulesCatalogTests finds them, so a new rule is run without anyone adding it here.
IXamlRule[] every =
[
    .. library.GetTypes()
        .Where(type => typeof(IXamlRule).IsAssignableFrom(type))
        .Where(type => type is { IsAbstract: false, IsInterface: false })
        .Select(type => (IXamlRule)Activator.CreateInstance(type)!)
        .OrderBy(rule => rule.Id, StringComparer.Ordinal),
];

IXamlRule[] chosen = args[0] == "all"
    ? every
    : [.. args[0].Split(',').Select(id => every.FirstOrDefault(rule => rule.Id == id)
        ?? throw new ArgumentException($"no rule has the id '{id}'"))];

Assembly[] assemblies = [.. args.Skip(each ? 4 : 3).Select(file => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, file)))];
if (!each)
{
    Report(XamlScanContext.Load(markup).WithAssemblies(assemblies));
    return 0;
}

foreach (string root in Directory.GetDirectories(markup).Order(StringComparer.Ordinal))
{
    Console.WriteLine($"[{Path.GetFileName(root)}]");
    Report(XamlScanContext.Load(root).WithAssemblies(assemblies));
}

return 0;

void Report(XamlScanContext scan)
{
    foreach (IXamlRule rule in chosen)
    {
        XamlRuleResult result = rule.Analyze(scan);

        Console.WriteLine($"{rule.Id}: inspected {result.Inspected}, findings {result.Findings.Count}, skipped {result.Skipped.Count}");
        foreach (string line in result.Findings
                     .Select(finding => $"  finding {finding.RelativePath}:{finding.Line} {finding.Message}")
                     .Concat(result.Skipped.Select(skip => $"  skipped {skip.RelativePath}:{skip.Line} {skip.Subject}: {skip.Reason}"))
                     .Order(StringComparer.Ordinal))
        {
            Console.WriteLine(line);
        }
    }
}
