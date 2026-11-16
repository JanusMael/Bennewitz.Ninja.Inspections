// Moved from DiffView's scripts/mutate-behaviour.cs at 9b36789 by plans/00008; changed only where the plan says.
namespace Bennewitz.Ninja.Testing.Mutate;

/// <summary>How a set's run is judged, once it has run — by this process or by shards that each ran a part of it.</summary>
internal static class Verdict
{
    /// <summary>
    /// The one place a kill is recorded, for a run's own kills, recorded ones and merged ones alike: each
    /// test a mutation failed, credited to the first mutation that failed it.
    /// </summary>
    public static void RecordKills(Dictionary<string, string> killedBy, string mutation, IEnumerable<string> failed)
    {
        foreach (string test in failed)
        {
            killedBy.TryAdd(test, mutation);
        }
    }

    /// <summary>Prints each mutation against its declaration and the set's tests against the kills, and answers the exit code.</summary>
    public static int Report(
        IReadOnlyList<Mutation> chosen,
        IReadOnlyDictionary<string, (Reading Reading, string Outcome)> results,
        Declarations declared,
        IReadOnlyDictionary<string, string> killedBy,
        bool narrowed,
        bool restoresHeld)
    {
        Console.WriteLine();
        bool ok = true;
        foreach (Mutation mutation in chosen)
        {
            if (!results.TryGetValue(mutation.Name, out (Reading Reading, string Outcome) r))
            {
                continue;
            }

            bool agree = r.Outcome == Judge.Ok;
            ok &= agree;
            Console.WriteLine($"{(agree ? "  ok  " : " WRONG")}  {mutation.Name}");
            if (agree)
            {
                continue;
            }

            Console.WriteLine($"          expected {mutation.Describe()}, got {r.Outcome}");
            if (r.Reading.Failed.Count > 0)
            {
                Console.WriteLine($"          failed: {string.Join(", ", r.Reading.Failed)}");
            }

            if (r.Reading.Message.Length > 0)
            {
                Console.WriteLine($"          {r.Reading.Message}");
            }
        }

        // ⭐ The half a set adds: every test it declares must have been broken by something, or be marked
        // inert at a pin the set still holds. A test no mutation reddened is a test nothing has shown able
        // to fail.
        Completeness judgement = Completeness.Of(declared.Tests, killedBy, narrowed);
        judgement.Print();

        Console.WriteLine();
        Console.WriteLine(
            $"mutations: {results.Count.ToString(CultureInfo.InvariantCulture)}, as declared: "
            + $"{results.Values.Count(v => v.Outcome == Judge.Ok).ToString(CultureInfo.InvariantCulture)}");
        if (declared.Tests.Count > 0)
        {
            Console.WriteLine(judgement.Summary);
        }

        Console.WriteLine(ok && judgement.Holds && restoresHeld
            ? "every mutation matched its declaration"
            : "A MUTATION DID NOT MATCH ITS DECLARATION, A DECLARED TEST WAS NEVER BROKEN, OR ONE MARKED INERT WAS — the tests do not guard what they claim");
        return ok && judgement.Holds && restoresHeld ? 0 : 1;
    }
}
