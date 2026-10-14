#!/usr/bin/env dotnet
// The real themes' sources, by plans/00003: fetches what scripts/fetch-reference.json lists into
// reference/, each at the tag its entry makes of the version Directory.Packages.props pins for its
// packages, for work by hand, such as pointing the theme audit at the real themes' markup. Nothing
// automated reads them: no test, build or CI job, and .gitignore excludes reference/ whole. Modelled on
// DiffView's script of the same name.
//
// ⭐ READ FROM THE PINS. A source's tag is made from the version its packages are pinned at, so the
// sources cannot drift from the packages a measurement runs, where a version apart would read as the
// audit's error. An entry's packages must be pinned alike: Avalonia's Fluent and Simple themes are one
// checkout.
//
// ⛔ LF, SHALLOW, SPARSE. Every checkout is LF on every platform, core.autocrlf=false and core.eol=lf,
// since the audit hashes the bytes it reads and a CRLF checkout of the same tag hashes differently. It is
// shallow at its tag, sparse to the folders its entry lists, and takes core.longpaths, since Windows
// refuses a path past 260 characters otherwise.
//
// Usage:
//   dotnet run --file scripts/fetch-reference.cs                 fetch each source that is missing
//   dotnet run --file scripts/fetch-reference.cs -- --update     also fetch again each source off its pin
//   dotnet run --file scripts/fetch-reference.cs -- --status     report only, fetching nothing
//   dotnet run --file scripts/fetch-reference.cs -- <name> ...   only the sources named
//
// Exit codes: 0 every source asked for is at its pin; 1 one is missing, off its pin, or failed to fetch;
// 2 a usage error, or a manifest or a pin that cannot be read.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

bool update = false, statusOnly = false;
List<string> only = [];
foreach (string argument in args)
{
    if (argument == "--update")
    {
        update = true;
    }
    else if (argument == "--status")
    {
        statusOnly = true;
    }
    else if (argument.StartsWith("--", StringComparison.Ordinal))
    {
        return Usage($"unknown argument '{argument}'.");
    }
    else
    {
        only.Add(argument);
    }
}

if (update && statusOnly)
{
    return Usage("--update fetches and --status does not: pass one of them.");
}

(int topExit, string top) = Git.Run(Directory.GetCurrentDirectory(), ["rev-parse", "--show-toplevel"]);
if (topExit != 0)
{
    return Usage("run it from inside the repository.");
}

string root = Path.GetFullPath(top.Trim());
Source[] sources;
try
{
    sources = Source.ReadAll(Path.Combine(root, "scripts", "fetch-reference.json"), Path.Combine(root, "Directory.Packages.props"));
}
catch (Exception error) when (error is IOException or JsonException or XmlException or InvalidDataException)
{
    Console.Error.WriteLine("fetch-reference: " + error.Message);
    return 2;
}

string[] unknown = [.. only.Where(name => !sources.Any(source => string.Equals(source.Name, name, StringComparison.OrdinalIgnoreCase)))];
if (unknown.Length > 0)
{
    return Usage($"the manifest lists no source called {string.Join(", ", unknown)}.");
}

int failures = 0;
foreach (Source source in sources.Where(source => only.Count == 0 || only.Contains(source.Name, StringComparer.OrdinalIgnoreCase)))
{
    string folder = source.PathUnder(root);
    string pin = Path.Combine(folder, Source.PinFile);
    bool present = Directory.Exists(Path.Combine(folder, ".git"));
    string? fetched = present && File.Exists(pin) ? File.ReadAllText(pin).Trim() : null;
    if (present && fetched == source.Tag)
    {
        Report(source, "present", $"{source.Tag} in {source.Folder}");
        continue;
    }

    if (statusOnly || (present && !update))
    {
        Report(source, present ? "off-pin" : "missing", present
            ? $"{source.Folder} holds {fetched ?? "an unknown ref"}, and the pin makes {source.Tag}: run with --update"
            : "run: dotnet run --file scripts/fetch-reference.cs");
        failures++;
        continue;
    }

    try
    {
        // A checkout off its pin, or a folder an interrupted fetch left, is replaced whole.
        if (Directory.Exists(folder))
        {
            DeleteTree(folder);
        }

        source.Fetch(root);
        File.WriteAllText(pin, source.Tag + "\n");
        Report(source, "fetched", $"{source.Tag} in {source.Folder}");
    }
    catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException)
    {
        Report(source, "failed", error.Message);
        failures++;
        if (Directory.Exists(folder))
        {
            try
            {
                DeleteTree(folder);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                // A partial clone that will not go is replaced by the next fetch, which deletes it first.
            }
        }
    }
}

return failures == 0 ? 0 : 1;

static void Report(Source source, string state, string detail) => Console.WriteLine($"{source.Name,-16} {state,-8} {detail}");

static void DeleteTree(string path)
{
    // git's object files are read-only, so the attribute goes first, or a delete fails on Windows.
    foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
    {
        File.SetAttributes(file, FileAttributes.Normal);
    }

    Directory.Delete(path, recursive: true);
}

static int Usage(string problem)
{
    Console.Error.WriteLine("fetch-reference: " + problem);
    Console.Error.WriteLine("usage: dotnet run --file scripts/fetch-reference.cs -- [--update | --status] [<name> ...]");
    return 2;
}

/// <summary>One source the manifest lists, with the tag the version its packages are pinned at makes.</summary>
sealed record Source(string Name, string Folder, string Url, string Tag, string[] Sparse)
{
    /// <summary>The file in a checkout that records the tag it was fetched at.</summary>
    public const string PinFile = ".fetched-ref";

    /// <summary>The manifest's sources, their tags made from the pins.</summary>
    /// <remarks>
    /// ⚠ Read through JsonDocument, not the serializer: a file-based app is analysed for native AOT, which
    /// rejects reflection-based deserialization, and warnings are errors here.
    /// </remarks>
    public static Source[] ReadAll(string manifestPath, string pinsPath)
    {
        Dictionary<string, string> pins = new(StringComparer.OrdinalIgnoreCase);
        foreach (XElement pinned in XDocument.Load(pinsPath).Descendants("PackageVersion"))
        {
            if (pinned.Attribute("Include")?.Value is { } package && pinned.Attribute("Version")?.Value is { } version)
            {
                pins[package] = version;
            }
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifestPath),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (!document.RootElement.TryGetProperty("sources", out JsonElement entries) || entries.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("the manifest holds no \"sources\" array.");
        }

        List<Source> sources = [];
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            string name = Text(entry, "name") ?? throw new InvalidDataException("a source has no name.");
            string folder = (Text(entry, "dir") ?? throw new InvalidDataException($"{name} has no dir.")).Replace('\\', '/');
            string url = Text(entry, "url") ?? throw new InvalidDataException($"{name} has no url.");
            string tag = Text(entry, "tag") ?? throw new InvalidDataException($"{name} has no tag.");
            string[] packages = Texts(entry, "packages");
            if (!folder.StartsWith("reference/", StringComparison.Ordinal) || folder.Split('/').Any(part => part is "" or "." or ".."))
            {
                throw new InvalidDataException($"{name}'s dir, {folder}, is not a folder under reference/, the one folder this script writes.");
            }

            if (!tag.Contains("{version}", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"{name}'s tag, {tag}, holds no {{version}}, so it would not follow the pin.");
            }

            if (packages.Length == 0)
            {
                throw new InvalidDataException($"{name} names no package whose pinned version makes its tag.");
            }

            string[] versions =
            [
                .. packages.Select(package => pins.TryGetValue(package, out string? version)
                        ? version
                        : throw new InvalidDataException($"{name}: Directory.Packages.props pins no version of {package}."))
                    .Distinct(StringComparer.Ordinal),
            ];
            if (versions.Length > 1)
            {
                throw new InvalidDataException(
                    $"{name}: {string.Join(" and ", packages)} are pinned at {string.Join(" and ", versions)}, and one checkout cannot match them all.");
            }

            sources.Add(new Source(name, folder, url, tag.Replace("{version}", versions[0], StringComparison.Ordinal), Texts(entry, "sparse")));
        }

        return [.. sources];
    }

    /// <summary>The checkout's folder under <paramref name="root"/>.</summary>
    public string PathUnder(string root) => Path.Combine([root, .. Folder.Split('/')]);

    /// <summary>Clones the source shallow at its tag, LF, and sparse where its entry lists folders.</summary>
    public void Fetch(string root)
    {
        string folder = PathUnder(root);
        Directory.CreateDirectory(Path.GetDirectoryName(folder)!);

        // The --config settings go into the checkout's own config, so a git command run in it later keeps
        // them; Git.Run passes them for this command too.
        List<string> arguments =
        [
            "clone", "--quiet", "--depth", "1", "--branch", Tag,
            "--config", "core.autocrlf=false", "--config", "core.eol=lf", "--config", "core.longpaths=true",
        ];
        if (Sparse.Length > 0)
        {
            arguments.AddRange(["--filter=blob:none", "--sparse"]);
        }

        arguments.AddRange([Url, folder]);
        Git.Check(root, arguments);
        if (Sparse.Length > 0)
        {
            Git.Check(folder, ["sparse-checkout", "set", .. Sparse]);
        }
    }

    private static string? Text(JsonElement entry, string property) =>
        entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string[] Texts(JsonElement entry, string property) =>
        entry.TryGetProperty(property, out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? [.. values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString() ?? "").Where(value => value.Length > 0)]
            : [];
}

/// <summary>git, run with the settings every checkout here takes.</summary>
static class Git
{
    public static (int Exit, string Output) Run(string directory, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo start = new("git")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        // ⛔ LF on every platform, and paths past 260 characters on Windows.
        foreach (string argument in (string[])["-c", "core.autocrlf=false", "-c", "core.eol=lf", "-c", "core.longpaths=true", .. arguments])
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("git could not be started: is it installed and on PATH?");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, output.Result + errors.Result);
    }

    public static void Check(string directory, IReadOnlyList<string> arguments)
    {
        (int exit, string output) = Run(directory, arguments);
        if (exit != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed with exit {exit}: {output.Trim()}");
        }
    }
}
