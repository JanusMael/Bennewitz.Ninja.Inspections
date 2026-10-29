#!/usr/bin/env dotnet
// Post-pack guard: the packages actually produced must be exactly the ones declared.
//
// PackagingTests checks the same thing from the PROJECT FILES, before anything is packed. This
// checks it from the PACKED OUTPUT, which is the only place a surprise can still appear — an SDK
// default, a transitive target, or a project that packs something its csproj does not obviously
// say it does. Both guards, because they fail on different mistakes.
//
// ⛔ Ids are read from the <id> element inside each .nuspec, NEVER parsed from the filename.
// "<id>.<version>.nupkg" is not decidable lexically: nothing distinguishes an id ending in
// "Widget" from one ending in "Widget.2026". A guard that splits on dots is wrong in exactly the
// cases it exists to catch.
//
// ⭐ A FAMILY'S RELEASE packs only that family's projects, so with --family the declaration is that
// family's entry in .github/repository.json's "families", and nothing else may have been packed: an
// id of another family, or one of packages.local, is as unexpected there as an undeclared one. Each of
// its packages must also carry the family's README, docs/<family>.md, byte for byte, as the README its
// .nuspec names: what nuget.org will show, read from what pack produced.
//
// Usage:  dotnet run scripts/assert-packages.cs -- <packages-directory> [--family <family>]

using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

string? packagesDirectory = null;
string? family = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--family" && i + 1 < args.Length)
    {
        family = args[++i].Trim().ToLowerInvariant();
    }
    else if (packagesDirectory is null && !args[i].StartsWith("--", StringComparison.Ordinal))
    {
        packagesDirectory = args[i];
    }
    else
    {
        return Fail($"Unknown or incomplete argument '{args[i]}'. Usage: assert-packages <packages-directory> [--family <family>]");
    }
}
packagesDirectory ??= Path.Combine("packages", "Release");
string repoRoot = Directory.GetCurrentDirectory();

HashSet<string> declared;
string declaredWhere;
if (family is null)
{
    declared =
    [
        .. ReadList(Path.Combine(repoRoot, "packages.push")),
        .. ReadList(Path.Combine(repoRoot, "packages.local")),
    ];
    declaredWhere = "packages.push or packages.local";
}
else
{
    string[]? ids = FamilyIds(Path.Combine(repoRoot, ".github", "repository.json"), family, out string? problem);
    if (ids is null)
    {
        return Fail(problem!);
    }
    declared = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    declaredWhere = $"the {family} family in .github/repository.json";
}

if (!Directory.Exists(packagesDirectory))
{
    return Fail($"No packages directory at '{packagesDirectory}'. Nothing was packed, so nothing can be checked.");
}

// .snupkg is a symbol package alongside its .nupkg, not a package of its own.
string[] produced = [.. Directory
    .EnumerateFiles(packagesDirectory, "*.nupkg", SearchOption.TopDirectoryOnly)
    .Where(path => !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))];

if (produced.Length == 0)
{
    return Fail($"No .nupkg files in '{packagesDirectory}'. A release that publishes nothing is a failure, not a no-op.");
}

HashSet<string> packed = new(declared.Comparer);
List<(string Id, byte[]? Readme)> readmes = [];

foreach (string package in produced)
{
    (string? id, byte[]? readme) = Read(package);

    if (id is null)
    {
        return Fail($"'{Path.GetFileName(package)}' has no readable .nuspec id.");
    }

    packed.Add(id);
    readmes.Add((id, readme));
}

string[] unexpected = [.. packed.Except(declared, declared.Comparer).Order()];
string[] missing = [.. declared.Except(packed, declared.Comparer).Order()];

if (unexpected.Length > 0)
{
    return Fail(family is null
        ? "Packed but declared nowhere: " + string.Join(", ", unexpected) +
          ". Add it to packages.push or packages.local before this reaches a push — a published id cannot be withdrawn."
        : $"Packed but not in {declaredWhere}: " + string.Join(", ", unexpected) +
          ". A family's release packs only that family's projects.");
}

if (missing.Length > 0)
{
    return Fail(
        $"Declared in {declaredWhere} but not packed: " + string.Join(", ", missing) +
        ". The push names it explicitly, so the release would fail on a file that does not exist.");
}

if (family is not null)
{
    string readmePath = Path.Combine(repoRoot, "docs", family + ".md");
    if (!File.Exists(readmePath))
    {
        return Fail($"docs/{family}.md is missing: it is the {family} family's README, which each of its packages carries.");
    }

    byte[] expected = File.ReadAllBytes(readmePath);
    string[] wrong = [.. readmes
        .Where(r => r.Readme is null || !r.Readme.AsSpan().SequenceEqual(expected))
        .OrderBy(r => r.Id, StringComparer.Ordinal)
        .Select(r => r.Readme is null ? $"{r.Id} packs none" : $"{r.Id}'s is another file")];
    if (wrong.Length > 0)
    {
        return Fail(
            $"The {family} family's packages carry docs/{family}.md as their README.md, byte for byte: " + string.Join("; ", wrong) +
            $". Set FamilyReadme to docs/{family}.md in each one's project.");
    }
}

Console.WriteLine($"Packages check out: {packed.Count} packed, all declared in {declaredWhere}.");
foreach (string id in packed.Order())
{
    Console.WriteLine($"  {id}");
}
if (family is not null)
{
    Console.WriteLine($"Each carries docs/{family}.md as its README.md.");
}

return 0;

static string[] ReadList(string path) =>
    File.Exists(path)
        ? [.. File.ReadLines(path).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#'))]
        : [];

// The family's ids, matching its stem without regard to case, as the release resolves it.
static string[]? FamilyIds(string path, string family, out string? problem)
{
    problem = null;
    JsonObject? families;
    try
    {
        families = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path))?["families"] as JsonObject : null;
    }
    catch (JsonException error)
    {
        problem = ".github/repository.json is not valid JSON: " + error.Message;
        return null;
    }
    if (families is null)
    {
        problem = $"--family {family}, but .github/repository.json declares no families.";
        return null;
    }

    foreach ((string stem, JsonNode? ids) in families)
    {
        if (stem.Equals(family, StringComparison.OrdinalIgnoreCase))
        {
            string[] list = ids is JsonArray array
                ? [.. array.Select(n => n is JsonValue value && value.TryGetValue(out string? id) ? id.Trim() : "").Where(id => id.Length > 0)]
                : [];
            if (list.Length == 0)
            {
                problem = $"The {family} family in .github/repository.json lists no ids.";
                return null;
            }
            return list;
        }
    }

    problem = $"--family {family}, which .github/repository.json does not declare; it declares {string.Join(", ", families.Select(f => f.Key))}.";
    return null;
}

// The package's id, and the bytes of the README its .nuspec names: null when it names none, or the
// file it names is not in the package.
static (string? Id, byte[]? Readme) Read(string packagePath)
{
    using ZipArchive archive = ZipFile.OpenRead(packagePath);

    ZipArchiveEntry? nuspec = archive.Entries.FirstOrDefault(entry =>
        entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
        !entry.FullName.Contains('/'));

    if (nuspec is null)
    {
        return (null, null);
    }

    XDocument document;
    using (Stream stream = nuspec.Open())
    {
        document = XDocument.Load(stream);
    }

    // The nuspec namespace varies by schema version, so match on local name.
    string? id = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "id")?.Value.Trim();
    string? readme = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "readme")?.Value.Trim().Replace('\\', '/');
    ZipArchiveEntry? entry = readme is { Length: > 0 } ? archive.GetEntry(readme) : null;
    if (entry is null)
    {
        return (id, null);
    }

    using Stream content = entry.Open();
    using MemoryStream bytes = new();
    content.CopyTo(bytes);
    return (id, bytes.ToArray());
}

static int Fail(string message)
{
    Console.Error.WriteLine("::error::" + message);
    return 1;
}
