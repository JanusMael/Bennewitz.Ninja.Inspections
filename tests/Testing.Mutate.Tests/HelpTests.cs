namespace Testing.Mutate.Tests;

/// <summary>What <c>mutate --help</c> answers, which is what a person runs first after installing the tool.</summary>
public sealed class HelpTests
{
    [Fact]
    public void Help_answers_outside_a_checkout_and_names_every_command()
    {
        string nowhere = Directory.CreateTempSubdirectory("mutate-help-").FullName;
        try
        {
            (int exitCode, string output) = Tool.Run(nowhere, ["--help"]);

            Assert.True(exitCode == 0, output);
            foreach (string command in (string[])["--list", "--check", "--only", "--owed", "--parallel", "--judge", "--kills", "--merge", "--budget", "--swap"])
            {
                Assert.Contains(command, output, StringComparison.Ordinal);
            }
        }
        finally
        {
            Directory.Delete(nowhere, recursive: true);
        }
    }

    [Fact]
    public void Anything_else_outside_a_checkout_is_refused_and_says_why()
    {
        string nowhere = Directory.CreateTempSubdirectory("mutate-nowhere-").FullName;
        try
        {
            (int exitCode, string output) = Tool.Run(nowhere, ["--list"]);

            Assert.True(exitCode == 2, output);
            Assert.Contains("not inside a git checkout", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(nowhere, recursive: true);
        }
    }
}
