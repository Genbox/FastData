using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Analyzers.Heuristics;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;
using static Genbox.FastData.Internal.Structures.DebugHelper;

namespace Genbox.FastData.Internal.Structures;

/*
  This is the core algorithm behind GPerf. It takes inspiration from Cichelli's Minimal Perfect Hash function, but also supports
  generating nearly-perfect hash functions, and thus works with larger datasets.

  I've only focused on the core algorithm, and not all the other application features such as switch-generation, length-table support, etc.
  as they overlap with many of the features in this application.

  From a purely algorithmic perspective, this implementation does not support:
  - Case-sensitivity (alpha_unify)
  - Predefined key positions
  - Length hashing
  - 7-Bit ASCII
  - Duplicates (it fails on duplicates)
  - Multiple iterations (decreases the generated table size)
  - Setting initial_asso or jump_value
  - Random associated values tables
 */

//TODO: Convert to a IHashStructure

[SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'")]
internal sealed class PerfectHashGPerfStructure : IStructure
{
    public bool TryCreate(object[] data, DataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
    {
        context = null;

        // It won't work with zero or one item. The case where we get 1 item should be handled by SingleItem anyway.
        if (data.Length < 2)
            return false;

        // GPerf only works on strings
        if (dataType != DataType.String)
            return false;

        // We cannot work on empty strings
        if (props.StringProps!.Value.LengthData.Min == 0)
            return false;

        // Step 1: Finding good positions
        Simulator sim = new Simulator(new SimulatorConfig { TimeWeight = 0 }, data, HashSetChainStructure.EmulateInternal);
        HeuristicAnalyzer analyzer = new HeuristicAnalyzer(data, props.StringProps.Value, new HeuristicAnalyzerConfig(), sim);
        Candidate<HeuristicHashSpec> candidate = analyzer.Run();

        // If we didn't get any positions, we don't want to move any further
        if (candidate.Spec.Positions.Length == 0)
            return false;

        int[] positions = candidate.Spec.Positions;
        int maxLen = (int)props.StringProps.Value.LengthData.Max;

        // TODO: For now, we keep regenerating state within Keyword. In the future, I hope to do this more efficiently
        List<Keyword> keywords = new List<Keyword>(data.Length);
        keywords.AddRange(data.Select(x => new Keyword((string)x)));

        // Step 2: Find good alpha increments
        int[] alphaInc = FindAlphaIncrements(keywords, maxLen, positions);
        int alphaSize = ComputeAlphaSize(alphaInc);

        // Step 3: Finding good association table values
        AssociationTable table = new AssociationTable();
        if (!table.TryFindGoodValues(keywords, positions, alphaInc, alphaSize))
        {
            context = null;
            return false;
        }

        // Make one final check, just to make sure nothing weird happened
        table.CollisionDetector.Clear();
        foreach (Keyword keyword in keywords)
        {
            int hashcode = ComputeHash(keyword, table.Values);

            // This shouldn't happen. proj1, proj2, proj3 must have been computed to be injective on the given keyword set.
            if (table.CollisionDetector.SetBit(hashcode))
            {
                // We throw here instead of returning false, as if the check fails, it is because of buggy code
                throw new InvalidOperationException("Internal error, unexpected duplicate hash code");
            }
        }

        // Sort all the things
        keywords.Sort((x, y) => x.HashValue.CompareTo(y.HashValue));

        // Set unused asso[c] to maxHash + 1.
        // This is not absolutely necessary, but speeds up the lookup function in many cases of lookup failure:
        // no string comparison is needed once the hash value of a string is larger than the hash value of any keyword.
        table.MaxHash = keywords[keywords.Count - 1].HashValue;

        for (int i = 0; i < alphaSize; i++)
            if (table.Occurrences[i] == 0)
                table.Values[i] = table.MaxHash + 1;

#if DebugPrint
        Print("\ndumping occurrence and associated values tables");

        for (int i = 0; i < alphaSize; i++)
            if (table.Occurrences[i] != 0)
                Print($"asso_values[{(char)i}] = {table.Values[i],6}, occurrences[{(char)i}] = {table.Occurrences[i],6}");

        Print("end table dumping");

        Print("\nDumping key list information:\n" +
              $"total non-static linked keywords = {keywords.Count}\n" +
              $"total keywords = {keywords.Count}\n" +
              "total duplicates = 0\n" +
              $"maximum key length = {maxLen}");

        Print("\nList contents are:\n(hash value, key length, index, selchars, keyword):");
        int field_width = keywords.Max(x => x.SelChars.Length);

        foreach (Keyword keyword in keywords)
            Print($"{keyword.HashValue,11},{keyword.AllChars.Length,11},{keyword.FinalIndex,6}, {keyword.SelChars.PadLeft(field_width)}, {keyword.AllChars}");

        Print("End dumping list.\n");
#endif

        // We convert keywords to KeyValuePair to keep Keyword internal
        context = new PerfectHashGPerfContext(table.Values, alphaInc, keywords.Select(x => new KeyValuePair<string, uint>(x.AllChars, (uint)x.HashValue)).ToArray(), positions.OrderByDescending(x => x).ToArray(), table.MaxHash);
        return true;
    }

    private static int[] FindAlphaIncrements(List<Keyword> keywords, int maxKeyLen, int[] positions)
    {
        uint duplicatesGoal = CountDuplicates(keywords, positions);
        Print("duplicates_goal: " + duplicatesGoal);

        int[] alphaInc = new int[maxKeyLen];
        uint duplicatesCurrent = CountDuplicates(keywords, positions, alphaInc);
        Print("current_duplicates_count: " + duplicatesCurrent);

        if (duplicatesCurrent > duplicatesGoal)
        {
            // Look which alphaInc[i] we are free to increment
            List<int> indices = new List<int>();

            foreach (int keyPos in positions.OrderByDescending(x => x))
            {
                if (keyPos >= maxKeyLen || keyPos == -1)
                    continue;

                indices.Add(keyPos);
            }

            Print("nindices: " + indices.Count);

            // Perform several rounds of searching for a good alpha increment.
            // Each round reduces the number of artificial collisions by adding an increment in a single key position.
            int[] best = new int[maxKeyLen];
            int[] attempt = new int[maxKeyLen];

            do
            {
                // An increment of 1 is not always enough. Try higher increments also.
                for (int inc = 1;; inc++)
                {
                    uint bestDuplicatesCount = uint.MaxValue;

                    for (int j = 0; j < indices.Count; j++)
                    {
                        alphaInc.CopyTo(attempt, 0);
                        attempt[indices[j]] += inc;
                        uint tryDuplicatesCount = CountDuplicates(keywords, positions, attempt);
                        Print("try_duplicates_count: " + tryDuplicatesCount);

                        // We prefer 'try' to 'best' if it produces fewer duplicates.
                        if (tryDuplicatesCount < bestDuplicatesCount)
                        {
                            attempt.CopyTo(best, 0);
                            bestDuplicatesCount = tryDuplicatesCount;
                        }
                    }

                    // Stop this round when we got an improvement.
                    if (bestDuplicatesCount < duplicatesCurrent)
                    {
                        best.CopyTo(alphaInc, 0);
                        duplicatesCurrent = bestDuplicatesCount;
                        Print("new best: " + duplicatesCurrent);
                        break;
                    }
                }
            } while (duplicatesCurrent > duplicatesGoal);

#if DebugPrint
            Console.Write("\nComputed alpha increments: ");
            bool first = true;
            for (int j = indices.Count; j-- > 0;)
                if (alphaInc[indices[j]] != 0)
                {
                    if (!first)
                        Console.Write(", ");
                    Console.Write($"{indices[j] + 1}:+{alphaInc[indices[j]]}");
                    first = false;
                }
            Console.WriteLine();
#endif
        }

        return alphaInc;
    }

    private static uint CountDuplicates(List<Keyword> keywords, int[] positions, int[]? alphaInc = null)
    {
        if (alphaInc == null)
        {
            foreach (Keyword keyword in keywords)
                keyword.InitSelCharsTuple(positions);
        }
        else
        {
            foreach (Keyword keyword in keywords)
                keyword.InitSelCharsMultiset(positions, alphaInc);
        }

        HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

        uint collisions = 0;

        foreach (Keyword keyword in keywords)
            if (!set.Add(keyword.SelChars))
                collisions++;

        return collisions;
    }

    private static int ComputeAlphaSize(int[] alphaInc)
    {
        int maxAlphaInc = 0;
        for (int i = 0; i < alphaInc.Length; i++)
            if (maxAlphaInc < alphaInc[i])
                maxAlphaInc = alphaInc[i];
        return 256 + maxAlphaInc;
    }

    private static int ComputeHash(Keyword keyword, int[] assoValues)
    {
        int sum = keyword.SelChars.Sum(c => assoValues[c]);
        keyword.HashValue = sum;
        return sum;
    }
}