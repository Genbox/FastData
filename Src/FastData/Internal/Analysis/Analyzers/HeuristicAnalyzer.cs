using System.Diagnostics;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

/*
   This is the algorithm for finding positions that results in a good hash. It is based on find_positions() in GPerf.

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
internal class HeuristicAnalyzer(string[] data, StringProperties props, HeuristicAnalyzerConfig config, Simulator simulator) : IHashAnalyzer<HeuristicStringHash>
{
    private const double _epsilon = 1e-6;

    public Candidate<HeuristicStringHash> Run()
    {
        int max = (int)Math.Min(props.LengthData.Max - 1, config.MaxPositions - 1);

        // Stage 1: Find all positions that are mandatory. If two items are the same length, but differ only on one character, then we must include that character.
        DirectMap mandatory = new DirectMap(max + 1);
        GetMandatory(mandatory);
        Print("Stage1", mandatory);

        // Stage 2: Attempt to use just the mandatory positions first, then add positions as long as it decrease the collision count
        DirectMap current = new DirectMap(mandatory);
        double currentFitness = CalculateFitness(current);
        AddWhileBetter(max, current, ref currentFitness);
        Print("Stage2", current);

        // Stage 3: Remove positions, as long as this doesn't increase the collision count.
        RemoveSinglePositions(max, mandatory, current, ref currentFitness);
        Print("Stage3", current);

        // Stage 4: Replace two positions by one, as long as this doesn't increase the collision count.
        MergePositions(max, mandatory, current, ref currentFitness);
        Print("Stage4", current);

        return CalculateFitnessInternal(current);
    }

    private void GetMandatory(DirectMap mandatory)
    {
        for (int i1 = 0; i1 < data.Length - 1; i1++)
        {
            string str1 = data[i1];

            for (int i2 = i1 + 1; i2 < data.Length; i2++) // We want to avoid comparing the item to itself
            {
                string str2 = data[i2];

                if (str1.Length == str2.Length) // Same length
                {
                    int i;
                    for (i = 0; i < str1.Length - 1; i++)
                    {
                        if (str1[i] != str2[i])
                            break; // Stop on the first char that is different
                    }

                    // If we stopped before the end of the string, find the next offset where the chars differ
                    if (i < str1.Length - 1)
                    {
                        int j;
                        for (j = i + 1; j < str1.Length; j++)
                        {
                            if (str1[j] != str2[j])
                                break;
                        }

                        if (j >= str1.Length)
                            mandatory.Add(i);
                    }
                }
            }
        }
    }

    private void AddWhileBetter(int max, DirectMap currentSet, ref double currentFitness)
    {
        while (true)
        {
            int bestIndex = int.MinValue;
            double bestFitness = double.MinValue;

            for (int i = max; i >= -1; i--)
            {
                if (currentSet.Add(i))
                {
                    double attemptFitness = CalculateFitness(currentSet);
                    currentSet.RemoveLast(i);

                    if (attemptFitness > bestFitness || (Math.Abs(attemptFitness - bestFitness) < _epsilon && i >= 0))
                    {
                        bestIndex = i;
                        bestFitness = attemptFitness;
                    }
                }
            }

            // Stop adding positions when it gives no improvement.
            if (bestFitness <= currentFitness)
                break;

            currentSet.Add(bestIndex);
            currentFitness = bestFitness;
            Print("- new best", currentSet);
        }
    }

    private void RemoveSinglePositions(int max, DirectMap mandatory, DirectMap currentSet, ref double currentFitness)
    {
        while (true)
        {
            int bestIndex = int.MinValue;
            double bestFitness = double.MinValue;

            for (int i = max; i >= -1; i--)
            {
                if (!mandatory.Contains(i) && currentSet.Remove(i))
                {
                    double attemptFitness = CalculateFitness(currentSet);
                    currentSet.Add(i);

                    // If the number of collisions are the same, we prefer our new attempt, as it has one instruction less, but produce the same number of collisions
                    if (attemptFitness > bestFitness || (Math.Abs(attemptFitness - bestFitness) < _epsilon && i == -1))
                    {
                        bestIndex = i;
                        bestFitness = attemptFitness;
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestFitness < currentFitness)
                break;

            currentSet.Remove(bestIndex);
            currentFitness = bestFitness;
            Print("- new best", currentSet);
        }
    }

    private void MergePositions(int max, DirectMap mandatory, DirectMap currentSet, ref double currentFitness)
    {
        while (true)
        {
            int bestI1 = int.MinValue, bestI2 = int.MinValue, bestI3 = int.MinValue;
            double bestFitness = double.MinValue;

            for (int i1 = max; i1 >= -1; i1--)
            {
                if (currentSet.Contains(i1) && !mandatory.Contains(i1))
                {
                    for (int i2 = max; i2 >= -1; i2--)
                    {
                        if (i2 != i1 && currentSet.Contains(i2) && !mandatory.Contains(i2))
                        {
                            for (int i3 = max; i3 >= 0; i3--)
                            {
                                if (currentSet.Add(i3))
                                {
                                    currentSet.Remove(i1);
                                    currentSet.Remove(i2);
                                    double attemptFitness = CalculateFitness(currentSet);
                                    currentSet.Add(i1);
                                    currentSet.Add(i2);
                                    currentSet.Remove(i3);

                                    // If the number of collisions are the same, we prefer our new attempt, as it has two instructions less, but produce the same number of collisions
                                    if (attemptFitness > bestFitness || (Math.Abs(attemptFitness - bestFitness) < _epsilon && (i1 == -1 || i2 == -1)))
                                    {
                                        bestI1 = i1;
                                        bestI2 = i2;
                                        bestI3 = i3;
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

            currentSet.Remove(bestI1);
            currentSet.Remove(bestI2);
            currentSet.Add(bestI3);
            currentFitness = bestFitness;
            Print("- new best", currentSet);
        }
    }

    private double CalculateFitness(DirectMap set) => CalculateFitnessInternal(set).Fitness; //The algorithm here works with less fitness = better. At least for now.

    private Candidate<HeuristicStringHash> CalculateFitnessInternal(DirectMap map)
    {
        Candidate<HeuristicStringHash> _cand = new Candidate<HeuristicStringHash>();

        _cand.Spec = new HeuristicStringHash(map.Positions);
        simulator.Run(ref _cand);
        return _cand;
    }

    private static bool Equal(string a, string b, List<int> positions)
    {
        foreach (int pos in positions)
        {
            if (pos == -1) //This if-case should come first, or else it will overlap with the next
            {
                if (a[a.Length - 1] != b[b.Length - 1])
                    return false;
            }
            else if (pos <= a.Length - 1 && pos <= b.Length - 1)
            {
                if (a[pos] != b[pos])
                    return false;
            }
        }

        return true;
    }

    [Conditional("DebugPrint")]
    private void Print(string stage, DirectMap map)
    {
        Candidate<HeuristicStringHash> cand = CalculateFitnessInternal(map);

        Console.Write($"{stage} ({cand.Metadata["Collisions"]}): ");
        bool lastChar = false;
        bool first = true;
        foreach (int i in map.Positions)
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

    private sealed class DirectMap
    {
        private readonly bool[] _lookup;
        private readonly int _capacity;

        internal DirectMap(DirectMap input)
        {
            _lookup = new bool[input._lookup.Length];
            Array.Copy(input._lookup, _lookup, _lookup.Length);

            Positions = new List<int>(input._capacity);
            Positions.AddRange(input.Positions);
            _capacity = input._capacity;
        }

        internal DirectMap(int capacity)
        {
            _lookup = new bool[capacity + 1]; //We add space for -1
            Positions = new List<int>(capacity);
            _capacity = capacity;
        }

        public List<int> Positions { get; }

        public bool Add(int index)
        {
            ref bool lookup = ref _lookup[index + 1];

            //The item is not in the lookup. Add it and return true.
            if (!lookup)
            {
                lookup = true;
                Positions.Add(index);
                return true;
            }

            return false;
        }

        public bool Contains(int index) => _lookup[index + 1];

        public bool Remove(int index)
        {
            ref bool lookup = ref _lookup[index + 1];

            if (lookup)
            {
                lookup = false;
                Positions.Remove(index);
                return true;
            }

            return false;
        }

        public void RemoveLast(int index)
        {
            _lookup[index + 1] = false;
            Positions.RemoveAt(Positions.Count - 1);
        }
    }
}