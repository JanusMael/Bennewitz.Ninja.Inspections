namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>What <c>mutate --help</c> prints.</summary>
internal static class Usage
{
    public const string Text = """
        mutate — proves tests able to fail, by breaking what they guard, one declared edit at a time, in a git worktree
        of this checkout, which it never edits.

          mutate --set <file.json> --list                 each mutation, its expectation and what its run filters to
          mutate --set <file.json> --check                every edit applies once, every killer is declared; runs nothing
          mutate --set <file.json>                        run the set
          mutate --set <file.json> --only <text>          the mutations whose name holds the text
          mutate --set <a.json> --set <b.json>            several sets, one after another, each judged on its own
          mutate --owed <ref>                             the committed sets a change since <ref> owes a run
          mutate --parallel <n> [--set … | --owed <ref>]  every mutation in n worktrees at once
          mutate --judge <log> red|green [--killer <test>] [--says <text>]   judge a captured test log
          mutate --set <file.json> --kills <kills.json>   judge the set's tests against recorded kills
          mutate --set <file.json> --merge <file.tsv> …   judge shards' results as one run of the set
          mutate --moved <before> <after>                 the packages a change moved between two pins files

          --configuration <name>   every build and test run's configuration, Debug unless given
          --property <name=value>  an MSBuild property every build and test run is given; may be repeated
          --budget <minutes>       how long a run may be estimated to take before it is refused, 30 unless given
          --swap <project>         a mutation under the project's folder builds it alone and swaps its assembly into
                                   the test output, rather than building the test project; may be repeated

        Exit codes: 0 every mutation as declared and every declared test accounted for; 1 one was not, a setup fault,
        or a baseline that does not build or is red; 2 a usage error, an unreadable file, a refused budget, a stray
        write, or a merge refused.

        https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/testing.md
        """;
}
