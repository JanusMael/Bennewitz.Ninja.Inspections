using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Testing.Mutate.Tests;

/// <summary>
/// Whole runs over a repository of the test's own: what the tool promises about the checkout it is run in
/// (plans/00008, decisions 2 and 3), which only a run that builds and tests can show.
/// </summary>
/// <remarks>
/// <para>⭐ <b>The repository is real and small:</b> a calculator library and an xUnit v3 project holding its one test,
/// a set with a red mutation that names its killer and a green control, committed but for the test file, which stays
/// untracked, and one uncommitted edit. A run can only kill the mutation if the untracked file reached its worktree,
/// and the checkout's bytes and status, read before and after, show whether the run edited it.</para>
/// <para>⚠ <b>Each run restores and builds,</b> about half a minute, so this class holds four runs and no more.</para>
/// </remarks>
public sealed class WorktreeRunTests(WorktreeRunTests.Subject subject) : IClassFixture<WorktreeRunTests.Subject>
{
    [Fact]
    public void A_run_kills_as_declared_from_a_worktree_and_leaves_the_checkout_as_it_found_it()
    {
        string before = subject.State();

        // A budget of its own, which the run is held to and allowed by.
        (int exitCode, string output) = Tool.Run(subject.Root, ["--set", "Mutations/calculator.json", "--budget", "10"]);

        Assert.True(exitCode == 0, output);
        Assert.Contains("against a budget of 10.0 min", output, StringComparison.Ordinal);
        Assert.Contains("this checkout is not edited", output, StringComparison.Ordinal);
        Assert.Contains("ok     the calculator subtracts", output, StringComparison.Ordinal);
        Assert.Contains("ok     the sum is taken the other way round", output, StringComparison.Ordinal);

        // The killer is in the untracked test file: it was killed, so that file reached the worktree.
        Assert.Contains("1 killed by a mutation", output, StringComparison.Ordinal);
        Assert.Equal(before, subject.State());
        Assert.DoesNotContain(Subject.Home, subject.Worktrees(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_run_estimated_past_its_budget_is_refused_after_its_baseline_and_edits_nothing()
    {
        string before = subject.State();

        (int exitCode, string output) = Tool.Run(subject.Root, ["--set", "Mutations/calculator.json", "--budget", "0.001"]);

        Assert.True(exitCode == 2, output);
        Assert.Contains("baseline: green", output, StringComparison.Ordinal);
        Assert.Contains("past its budget", output, StringComparison.Ordinal);
        Assert.DoesNotContain("[1/2]", output, StringComparison.Ordinal);
        Assert.Equal(before, subject.State());
    }

    [Fact]
    public void A_worktree_a_dead_run_left_is_removed_by_the_next_run()
    {
        // A run named for a process that is not running: no system hands out the largest pid there is. Its start time is
        // this test's own, so two runs of the test never meet.
        string left = Path.Combine(Subject.Home, $"{int.MaxValue}-{DateTime.UtcNow.Ticks}", "1");
        subject.Git("worktree", "add", "--detach", left, "HEAD");
        Assert.Contains(left, subject.Worktrees(), StringComparison.OrdinalIgnoreCase);

        (int exitCode, string output) = Tool.Run(subject.Root, ["--set", "Mutations/calculator.json", "--only", "other way round"]);

        Assert.True(exitCode == 0, output);
        Assert.Contains("left by a run that is gone", output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(left), output);
        Assert.DoesNotContain(left, subject.Worktrees(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_mutation_in_a_swapped_project_builds_it_alone_and_is_judged_by_the_assembly_it_swaps_in()
    {
        (int exitCode, string output) = Tool.Run(subject.Root, ["--set", "Mutations/calculator.json", "--swap", "Calculator/Calculator.csproj"]);

        // The red mutation is killed only if the assembly it built reached the tests' output.
        Assert.True(exitCode == 0, output);
        Assert.Contains("swap: a mutation under Calculator/Calculator.csproj's folder", output, StringComparison.Ordinal);
        Assert.Contains("ok     the calculator subtracts (swapped, ", output, StringComparison.Ordinal);
        Assert.Contains("ok     the sum is taken the other way round (swapped, ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_swap_that_names_no_project_is_refused_before_anything_builds()
    {
        (int exitCode, string output) = Tool.Run(subject.Root, ["--set", "Mutations/calculator.json", "--swap", "Calculator/Missing.csproj"]);

        Assert.True(exitCode == 2, output);
        Assert.Contains("--swap takes a project file", output, StringComparison.Ordinal);
        Assert.DoesNotContain("baseline", output, StringComparison.Ordinal);
    }

    /// <summary>The repository the runs are over, made once for the class and removed after it.</summary>
    public sealed class Subject : IDisposable
    {
        public Subject()
        {
            Root = Directory.CreateTempSubdirectory("mutate-subject-").FullName;
            string repo = RepoPaths.Root;
            File.Copy(Path.Combine(repo, "global.json"), Path.Combine(Root, "global.json"));
            File.Copy(Path.Combine(repo, "NuGet.config"), Path.Combine(Root, "NuGet.config"));
            string xunit = XDocument.Load(Path.Combine(repo, "Directory.Packages.props"))
                .Descendants("PackageVersion").Single(e => (string?)e.Attribute("Include") == "xunit.v3").Attribute("Version")!.Value;

            Write("Calculator.Tests/Calculator.Tests.csproj", $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="xunit.v3" Version="{xunit}" />
                    <ProjectReference Include="../Calculator/Calculator.csproj" />
                  </ItemGroup>
                </Project>
                """);
            Write("Calculator/Calculator.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                </Project>
                """);
            Write("Calculator/Calculator.cs", """
                namespace Arithmetic;

                public static class Calculator
                {
                    public static int Add(int a, int b) => a + b;
                }
                """);
            // The attribute is interpolated so that this file, which the tool's own set holds to its tests and reads by
            // lines, holds no attribute line above the subject's test: the subject's file gets one, and this file none.
            Write("Calculator.Tests/CalculatorTests.cs", $$"""
                using Xunit;

                namespace Arithmetic.Tests;

                public sealed class CalculatorTests
                {
                    {{Fact}}
                    public void Two_and_three_make_five() => Assert.Equal(5, Calculator.Add(2, 3));
                }
                """);
            Write("Mutations/calculator.json", """
                {
                  "project": "Calculator.Tests/Calculator.Tests.csproj",
                  "tests": ["Calculator.Tests/CalculatorTests.cs"],
                  "mutations": [
                    {
                      "name": "the calculator subtracts",
                      "file": "Calculator/Calculator.cs",
                      "old": "=> a + b;",
                      "new": "=> a - b;",
                      "killer": "Two_and_three_make_five",
                      "says": "Assert.Equal() Failure"
                    },
                    {
                      "name": "the sum is taken the other way round",
                      "file": "Calculator/Calculator.cs",
                      "old": "=> a + b;",
                      "new": "=> b + a;",
                      "expect": "green",
                      "because": "addition commutes"
                    }
                  ]
                }
                """);
            Write(".gitignore", "bin/\nobj/\n");
            Write("notes.txt", "committed\n");

            Git("init", "--quiet");
            Git("config", "user.name", "mutate tests");
            Git("config", "user.email", "mutate-tests@invalid");
            Git("config", "core.autocrlf", "false");
            Git("add", "--", ".gitignore", "global.json", "NuGet.config", "notes.txt", "Calculator.Tests/Calculator.Tests.csproj", "Calculator/Calculator.csproj", "Calculator/Calculator.cs", "Mutations/calculator.json");
            Git("commit", "--quiet", "-m", "subject");

            // Uncommitted work the runs must carry and must not touch.
            Write("notes.txt", "committed, then edited\n");
        }

        private const string Fact = "[Fact]";

        /// <summary>Where the tool keeps every run's worktrees, as it names it.</summary>
        public static string Home => Path.Combine(Path.GetTempPath(), "bennewitz-mutate", "worktrees");

        public string Root { get; }

        /// <summary>Every file of the checkout outside its build output and .git, with a hash of its bytes, and git's status.</summary>
        public string State()
        {
            IEnumerable<string> files = Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(Root, f).Replace('\\', '/'))
                .Where(f => !f.StartsWith(".git/", StringComparison.Ordinal) && !f.Split('/').Any(p => p is "bin" or "obj"))
                .Order(StringComparer.Ordinal)
                .Select(f => f + " " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Root, f)))));
            return string.Join('\n', files) + "\n--\n" + Git("status", "--porcelain", "--untracked-files=all");
        }

        /// <summary>The worktrees git lists for the checkout.</summary>
        public string Worktrees() => Git("worktree", "list", "--porcelain").Replace('/', Path.DirectorySeparatorChar);

        public string Git(params string[] arguments)
        {
            ProcessStartInfo start = new("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(start) ?? throw new InvalidOperationException("could not start git");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            string output = stdout.GetAwaiter().GetResult();
            return process.ExitCode == 0
                ? output
                : throw new InvalidOperationException($"git {string.Join(' ', arguments)}: {stderr.GetAwaiter().GetResult()}");
        }

        public void Dispose()
        {
            try
            {
                Git("worktree", "prune");
            }
            catch (InvalidOperationException)
            {
                // Removed below either way.
            }

            foreach (string file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(Root, recursive: true);
        }

        private void Write(string relative, string text)
        {
            string path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text.ReplaceLineEndings("\n"));
        }
    }
}
