using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Heuristics;

/*
   This is the algorithm for finding positions that results in a good hash from GPerf. See find_positions().

   It tries to find positions in input strings that gives as few collisions as possible. In the event it finds a function with 0 collisions,
   the hash is a perfect hash.

   Input:
   - cat
   - dog
   - horse
   - pig
   - cow

   Algorithm example:
   1. If all strings are the same length, find where they differ. This is a mandatory position to hash.
      - result: [nothing]
   2. Attempt to add each character in the strings and rehash. If we get fewer collisions, we add the new position to the set.
      - result: 0, 1
   3. Attempt to remove each character from the set. If we get the same number of collisions with a smaller set, we keep the smaller set.
      - result: 0, 1
   4. Attempt to merge two positions into one. If we get the same number of collisions, we keep the smaller set.
      - result: 0, 1

   It has been reimplemented with the following properties:
   - Uses HashSet for fast lookups. It is a tradeoff as results needs to be sorted. Either we have the overhead of sorting, or we have the overhead of traversing.
     For very large sets, this implementation should be faster)
   - Uses a generic fitness parameter to align with other analyzers in the library
   - Added additional logging to ensure complete feature parity with GPerf
   - Has the ability to set the max number of characters to check via configuration

   This implementation does not support:
   - Case-sensitivity (alpha_unify)
 */

/// <summary>Finds the least number of positions in a string that hashes to a unique value for all inputs.</summary>
[SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'")]
internal class HeuristicAnalyzer(object[] data, StringProperties props, HeuristicAnalyzerConfig config, Simulator simulator) : IHashAnalyzer<HeuristicHashSpec>
{
    public Candidate<HeuristicHashSpec> Run()
    {
        // Stage 1: Find all positions that are mandatory. If two items are the same length, but differ only on one character, then we must include that character.
        HashSet<int> mandatory = GetMandatory();
        Print("Stage1", mandatory);

        int max = (int)(props.LengthData.Max - 1 < config.MaxPositions - 1 ? props.LengthData.Max - 1 : config.MaxPositions - 1);

        // Stage 2: Attempt using just the mandatory positions first, then add positions as long as it decrease the collision count
        HashSet<int> current = new HashSet<int>(mandatory);
        double currentFitness = CalculateFitness(current);
        AddWhileBetter(max, ref current, ref currentFitness);
        Print("Stage2", current);

        // Stage 3: Remove positions, as long as this doesn't increase the collision count.
        RemoveSinglePositions(max, mandatory, ref current, ref currentFitness);
        Print("Stage3", current);

        // Stage 4: Replace two positions by one, as long as this doesn't increase the collision count.
        MergePositions(max, mandatory, ref current, ref currentFitness);
        Print("Stage4", current);

        return CalculateFitnessInternal(current);
    }

    private HashSet<int> GetMandatory()
    {
        HashSet<int> mandatory = new HashSet<int>();

        for (int i1 = 0; i1 < data.Length - 1; i1++)
        {
            string str1 = (string)data[i1];

            for (int i2 = i1 + 1; i2 < data.Length; i2++) // We want to avoid comparing the item to itself
            {
                string str2 = (string)data[i2];

                if (str1.Length == str2.Length) // Same length
                {
                    int i;
                    for (i = 0; i < str1.Length - 1; i++)
                        if (str1[i] != str2[i])
                            break; // Stop on the first char that is different

                    // If we stopped before the end of the string, find the next offset where the chars differ
                    if (i < str1.Length - 1)
                    {
                        int j;
                        for (j = i + 1; j < str1.Length; j++)
                            if (str1[j] != str2[j])
                                break;

                        if (j >= str1.Length)
                            mandatory.Add(i);
                    }
                }
            }
        }

        return mandatory;
    }

    private void AddWhileBetter(int max, ref HashSet<int> currentSet, ref double currentFitness)
    {
        while (true)
        {
            HashSet<int>? bestSet = null;
            double bestFitness = double.MaxValue;

            for (int i = max; i >= -1; i--)
            {
                if (!currentSet.Contains(i))
                {
                    HashSet<int> attemptSet = new HashSet<int>(currentSet);
                    attemptSet.Add(i);
                    double attemptFitness = CalculateFitness(attemptSet);

                    if (attemptFitness < bestFitness || (Math.Abs(attemptFitness - bestFitness) < double.Epsilon && i >= 0))
                    {
                        bestSet = attemptSet;
                        bestFitness = attemptFitness;
                    }
                }
            }

            // Stop adding positions when it gives no improvement.
            if (bestFitness >= currentFitness)
                break;

            if (bestSet != null)
            {
                currentSet = bestSet;
                currentFitness = bestFitness;
                Print("- new best", currentSet);
            }
        }
    }

    private void RemoveSinglePositions(int max, HashSet<int> mandatory, ref HashSet<int> currentSet, ref double currentFitness)
    {
        while (true)
        {
            HashSet<int>? bestSet = null;
            double bestFitness = double.MaxValue;

            for (int i = max; i >= -1; i--)
            {
                if (currentSet.Contains(i) && !mandatory.Contains(i))
                {
                    HashSet<int> attemptSet = new HashSet<int>(currentSet);
                    attemptSet.Remove(i);
                    double attemptFitness = CalculateFitness(attemptSet);

                    // If the number of collisions are the same, we prefer our new attempt, as it has one instruction less, but produce the same number of collisions
                    if (attemptFitness < bestFitness || (Math.Abs(attemptFitness - bestFitness) < double.Epsilon && i == -1))
                    {
                        bestSet = attemptSet;
                        bestFitness = attemptFitness;
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestFitness > currentFitness)
                break;

            if (bestSet != null)
            {
                currentSet = bestSet;
                currentFitness = bestFitness;
                Print("- new best", currentSet);
            }
        }
    }

    private void MergePositions(int max, HashSet<int> mandatory, ref HashSet<int> currentSet, ref double currentFitness)
    {
        while (true)
        {
            HashSet<int>? bestSet = null;
            double bestFitness = double.MaxValue;

            for (int i1 = max; i1 >= -1; i1--)
            {
                if (currentSet.Contains(i1) && !mandatory.Contains(i1))
                {
                    for (int i2 = max; i2 >= -1; i2--)
                    {
                        if (currentSet.Contains(i2) && !mandatory.Contains(i2) && i2 != i1)
                        {
                            for (int i3 = max; i3 >= 0; i3--)
                            {
                                if (!currentSet.Contains(i3))
                                {
                                    HashSet<int> attemptSet = new HashSet<int>(currentSet);
                                    attemptSet.Remove(i1);
                                    attemptSet.Remove(i2);
                                    attemptSet.Add(i3);

                                    double attemptFitness = CalculateFitness(attemptSet);

                                    // If the number of collisions are the same, we prefer our new attempt, as it has two instructions less, but produce the same number of collisions
                                    if (attemptFitness < bestFitness || (Math.Abs(attemptFitness - bestFitness) < double.Epsilon && (i1 == -1 || i2 == -1 || i3 >= 0)))
                                    {
                                        bestSet = attemptSet;
                                        bestFitness = attemptFitness;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestFitness > currentFitness)
                break;

            if (bestSet != null)
            {
                currentSet = bestSet;
                currentFitness = bestFitness;
                Print("- new best", currentSet);
            }
        }
    }

    private double CalculateFitness(HashSet<int> set) => CalculateFitnessInternal(set).Fitness * -1; //The algorithm here works with less fitness = better. At least for now.

    private Candidate<HeuristicHashSpec> CalculateFitnessInternal(HashSet<int> set)
    {
        Candidate<HeuristicHashSpec> cand = new Candidate<HeuristicHashSpec>(new HeuristicHashSpec(set.OrderBy(x => x).ToArray()));
        simulator.Run(ref cand);
        return cand;
    }

    [Conditional("DebugPrint")]
    private void Print(string stage, HashSet<int> set)
    {
        Candidate<HeuristicHashSpec> cand = CalculateFitnessInternal(set);

        Console.Write($"{stage} ({cand.Metadata["Collisions"]}): ");
        bool lastChar = false;
        bool first = true;
        foreach (int i in set.OrderBy(x => x))
        {
            if (!first)
                Console.Write(", ");
            if (i == -1)
                lastChar = true;
            else
            {
                Console.Write(i);
                first = false;
            }
        }

        if (lastChar)
        {
            if (!first)
                Console.Write(", ");
            Console.Write("$");
        }
        Console.WriteLine();
    }
}