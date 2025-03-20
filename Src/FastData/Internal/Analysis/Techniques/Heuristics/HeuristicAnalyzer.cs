// #define DebugOutput
using System.Diagnostics;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Techniques.Heuristics;

/// <summary>
/// Finds the least number of positions in a string that hashes to a unique value for all inputs.
/// It is assumed that all inputs are unique - if not, a solution might not exist.
/// </summary>
internal class HeuristicAnalyzer(object[] data, StringProperties props, HeuristicAnalyzerConfig config, Simulation<HeuristicAnalyzerConfig, HeuristicHashSpec> simulation) : IHashAnalyzer<HeuristicHashSpec>
{
    public Candidate<HeuristicHashSpec> Run()
    {
        // Stage 1: Find all positions that are mandatory. If two items are the same length, but differ only on one character, then we must include that character.
        GetMandatory(out HashSet<int> mandatory, out double currentFitness);
        Print("Stage1", currentFitness, mandatory);

        int max = (int)props.LengthData.Max - 1;

        // Stage 2: Attempt using just the mandatory positions first, then add positions as long as it decrease the collision count
        HashSet<int> currentSet = new HashSet<int>(mandatory);
        AddUntilBetter(max, ref currentSet, ref currentFitness);
        Print("Stage2", currentFitness, currentSet);

        // Stage 3: Remove positions, as long as this doesn't increase the collision count.
        RemoveSinglePositions(max, mandatory, ref currentSet, ref currentFitness);
        Print("Stage3", currentFitness, currentSet);

        // Stage 4: Replace two positions by one, as long as this doesn't increase the collision count.
        RemoveDualPositions(max, mandatory, ref currentSet, ref currentFitness);
        Print("Stage4", currentFitness, currentSet);

        //TODO: refactor - it is duplicate of CalculateFitness. We can use just one candidate and pass it around
        HeuristicHashSpec spec = new HeuristicHashSpec(currentSet);
        Candidate<HeuristicHashSpec> candidate = new Candidate<HeuristicHashSpec>(spec);
        simulation(data, config, ref candidate);
        return candidate;
    }

    private void GetMandatory(out HashSet<int> mandatory, out double currentFitness)
    {
        mandatory = new HashSet<int>();

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

        currentFitness = CalculateFitness(data, mandatory);
    }

    private void AddUntilBetter(int max, ref HashSet<int> currentSet, ref double currentFitness)
    {
        while (true)
        {
            HashSet<int>? best = null;
            double bestFitness = double.MinValue;

            for (int i = max; i >= -1; i--)
            {
                if (!currentSet.Contains(i))
                {
                    HashSet<int> attempt = new HashSet<int>(currentSet);
                    attempt.Add(i);
                    double attemptFitness = CalculateFitness(data, attempt);

                    if (attemptFitness > bestFitness || (Math.Abs(attemptFitness - bestFitness) < double.Epsilon && i >= 0))
                    {
                        best = attempt;
                        bestFitness = attemptFitness;
                    }
                }
            }

            // Stop adding positions when it gives no improvement.
            if (bestFitness <= currentFitness)
                break;

            if (best != null)
            {
                currentSet = best;
                currentFitness = bestFitness;
                Print(" - new best", currentFitness, currentSet);
            }
        }
    }

    private void RemoveSinglePositions(int max, HashSet<int> mandatory, ref HashSet<int> currentSet, ref double currentFitness)
    {
        while (true)
        {
            HashSet<int>? best = null;
            double bestFitness = double.MinValue;

            for (int i = max; i >= -1; i--)
            {
                if (currentSet.Contains(i) && !mandatory.Contains(i))
                {
                    HashSet<int> attempt = new HashSet<int>(currentSet);
                    attempt.Remove(i);
                    double attemptFitness = CalculateFitness(data, attempt);

                    // If the number of collisions are the same, we prefer our new attempt, as it has one instruction less, but produce the same number of collisions
                    if (attemptFitness > bestFitness || (Math.Abs(attemptFitness - bestFitness) < double.Epsilon && i == -1))
                    {
                        best = attempt;
                        bestFitness = attemptFitness;
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestFitness < currentFitness)
                break;

            if (best != null)
            {
                currentSet = best;
                currentFitness = bestFitness;
                Print(" - new best", currentFitness, currentSet);
            }
        }
    }

    private void RemoveDualPositions(int max, HashSet<int> mandatory, ref HashSet<int> currentSet, ref double currentFitness)
    {
        while (true)
        {
            HashSet<int>? best = null;
            double bestFitness = double.MinValue;

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
                                    HashSet<int> attempt = new HashSet<int>(currentSet);
                                    attempt.Remove(i1);
                                    attempt.Remove(i2);
                                    attempt.Add(i3);

                                    double attemptFitness = CalculateFitness(data, attempt);

                                    // If the number of collisions are the same, we prefer our new attempt, as it has two instructions less, but produce the same number of collisions
                                    if (attemptFitness > bestFitness || (Math.Abs(attemptFitness - bestFitness) < double.Epsilon && (i1 == -1 || i2 == -1 || i3 != 0))) //IQV: I changed the i3 condition since it was always true
                                    {
                                        best = attempt;
                                        bestFitness = attemptFitness;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestFitness < currentFitness)
                break;

            if (best != null)
            {
                currentSet = best;
                currentFitness = bestFitness;
                Print(" - new best", currentFitness, currentSet);
            }
        }
    }

    private double CalculateFitness(object[] objects, HashSet<int> set)
    {
        HeuristicHashSpec spec = new HeuristicHashSpec(set);
        Candidate<HeuristicHashSpec> candidate = new Candidate<HeuristicHashSpec>(spec);
        simulation(objects, config, ref candidate);
        return candidate.Fitness;
    }

    [Conditional("DebugOutput")]
    private static void Print(string stage, double fitness, HashSet<int> set) => Console.WriteLine($"{stage}: ({fitness:N4}) {string.Join(",", set.OrderBy(i => i).ToArray())}");
}