using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

/*
  This is the core algorithm behind GPerf. It takes inspiration from Cichelli's Minimal Perfect Hash function but also supports
  generating nearly perfect hash functions and thus works with larger datasets.

  I've only focused on the core algorithm, and not all the other application features such as switch-generation, length-table support, etc.
  as they overlap with many of the features in this application.

  From a purely algorithmic perspective, this implementation does not support:
  - Case sensitivity (alpha_unify)
  - Predefined key positions
  - Length hashing
  - 7-Bit ASCII
  - Duplicates (it fails on duplicates)
  - Multiple iterations (decreases the generated table size)
  - Setting initial_asso or jump_value
  - Random associated values tables
 */

/// <summary>Finds the least number of positions in a string that hashes to a unique value for all inputs.</summary>
internal sealed partial class GPerfAnalyzer(int dataLength, StringProperties props, GPerfAnalyzerConfig config, Simulator sim, ILogger<GPerfAnalyzer> logger) : IStringHashAnalyzer
{
    private readonly FastSet _emulator = new FastSet(dataLength);
    public bool IsAppropriate() => props.CharacterData.AllAscii;

    [SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'")]
    public IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data)
    {
        // We cannot work on empty strings
        if (props.LengthData.Min == 0)
            return [];

        // Step1: Find positions
        int[] positions = GetPositions(data);
        LogPositions(logger, string.Join(", ", positions));

        // If we didn't get any positions, we don't want to move any further
        if (positions.Length == 0)
            return [];

        int maxLen = (int)props.LengthData.Max;

        // TODO: For now, we keep regenerating state within Keyword. In the future, I hope to do this more efficiently
        List<Keyword> keywords = new List<Keyword>(data.Length);

        foreach (string s in data)
            keywords.Add(new Keyword(s));

        // Step 2: Find good alpha increments
        int[] alphaInc = FindAlphaIncrements(keywords, maxLen, positions);
        int alphaSize = ComputeAlphaSize(alphaInc);
        LogAlphaPositions(logger, alphaSize, string.Join(", ", alphaInc));

        // Step 3: Finding good association table values
        AssociationTable table = new AssociationTable(logger);

        if (!table.TryFindGoodValues(keywords, positions, alphaInc, alphaSize))
        {
            LogUnableToFindAsso(logger);
            return [];
        }

        LogAsso(logger, string.Join(", ", table.Values));

#if DEBUG

        // Make one final check, just to make sure nothing weird happened
        table.CollisionDetector.Clear();
        foreach (Keyword keyword in keywords)
        {
            int hashcode = ComputeHash(keyword, table.Values);

            // This shouldn't happen. proj1, proj2, proj3 must have been computed to be injective on the given keyword set.
            if (table.CollisionDetector.SetBit(hashcode))
                throw new InvalidOperationException("Internal error, unexpected duplicate hash code"); // We throw here instead of returning false, as if the check fails, it is because of buggy code
        }
#endif

        // Sort all the things
        keywords.Sort((x, y) => x.HashValue.CompareTo(y.HashValue));

        // Set unused asso[c] to maxHash + 1.
        // This is not necessary but speeds up the lookup function in many cases of lookup failure:
        // no string comparison is needed once the hash value of a string is larger than the hash value of any keyword.
        int maxHash = keywords[keywords.Count - 1].HashValue;

        LogMaxHash(logger, maxHash);

        for (int i = 0; i < alphaSize; i++)
        {
            if (table.Occurrences[i] == 0)
                table.Values[i] = maxHash + 1;
        }

#if DEBUG
        if (logger.IsEnabled(LogLevel.Trace))
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\ndumping occurrence and associated values tables");

            for (int i = 0; i < alphaSize; i++)
            {
                if (table.Occurrences[i] != 0)
                    sb.AppendLine($"asso_values[{(char)i}] = {table.Values[i],6}, occurrences[{(char)i}] = {table.Occurrences[i],6}");
            }

            sb.AppendLine("end table dumping");

            sb.AppendLine("\nDumping key list information:\n" +
                          $"total non-static linked keywords = {keywords.Count}\n" +
                          $"total keywords = {keywords.Count}\n" +
                          "total duplicates = 0\n" +
                          $"maximum key length = {maxLen}");

            sb.AppendLine("\nList contents are:\n(hash value, key length, index, selchars, keyword):");
            int field_width = keywords.Max(x => x.SelChars.Length);

            foreach (Keyword keyword in keywords)
            {
                sb.AppendLine($"{keyword.HashValue,11},{keyword.AllChars.Length,11},{"-1",6}, {keyword.SelChars.PadLeft(field_width)}, {keyword.AllChars}");
            }

            sb.AppendLine("End dumping list.\n");

            LogGPerfDebug(logger, sb.ToString());
        }
#endif

        // We convert keywords to KeyValuePair to keep Keyword internal
        GPerfStringHash stringHash = new GPerfStringHash(table.Values, alphaInc, positions.OrderByDescending(x => x).ToArray(), props.LengthData.Min);

        Candidate candidate = sim.Run(data, stringHash);
        LogCandidate(logger, candidate.Fitness, candidate.Collisions);

        return [candidate];
    }

    private int[] GetPositions(ReadOnlySpan<string> data)
    {
        /*
            This is the algorithm for finding positions that results in a good hash. It is based on find_positions() in GPerf.

            It tries to find positions in input strings that give as few collisions as possible. In the event it finds a function with 0 collisions,
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
            - Added additional logging to ensure complete feature parity with GPerf
            - Has the ability to set the max number of characters to check via configuration
            - Uses DirectMap that avoids modulo by directly addressing into an array. Provides better performance.

            This implementation does not support:
            - Case-sensitivity (alpha_unify)
        */

        int max = (int)Math.Min(props.LengthData.Max - 1, config.MaxPositions - 1);

        // Stage 1: Find all positions that are mandatory. If two items are the same length, but differ only on one character, then we must include that character.
        DirectMap mandatory = new DirectMap(max + 1);
        GetMandatory(data, mandatory);
        PrintMap("Stage1", data, mandatory);

        // Stage 2: Attempt to use just the mandatory positions first, then add positions as long as it decrease the collision count
        DirectMap current = new DirectMap(mandatory);
        uint currentCollisions = GetCollisions(data, current);
        AddWhileBetter(data, max, current, ref currentCollisions);
        PrintMap("Stage2", data, current);

        // Stage 3: Remove positions, as long as this doesn't increase the collision count.
        RemoveSinglePositions(data, max, mandatory, current, ref currentCollisions);
        PrintMap("Stage3", data, current);

        // Stage 4: Replace two positions by one, as long as this doesn't increase the collision count.
        MergePositions(data, max, mandatory, current, ref currentCollisions);
        PrintMap("Stage4", data, current);

        return current.Positions.ToArray();
    }

    private static void GetMandatory(ReadOnlySpan<string> data, DirectMap mandatory)
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

    private void AddWhileBetter(ReadOnlySpan<string> data, int max, DirectMap currentSet, ref uint currentCollisions)
    {
        while (true)
        {
            int bestIndex = int.MinValue;
            uint bestCollisions = uint.MaxValue;

            for (int i = max; i >= -1; i--)
            {
                if (currentSet.Add(i))
                {
                    uint attemptCollisions = GetCollisions(data, currentSet);
                    currentSet.RemoveLast(i);

                    if (attemptCollisions < bestCollisions || (attemptCollisions == bestCollisions && i >= 0))
                    {
                        bestIndex = i;
                        bestCollisions = attemptCollisions;
                    }
                }
            }

            // Stop adding positions when it gives no improvement.
            if (bestCollisions >= currentCollisions)
                break;

            currentSet.Add(bestIndex);
            currentCollisions = bestCollisions;
            PrintMap("- new best", data, currentSet);
        }
    }

    private void RemoveSinglePositions(ReadOnlySpan<string> data, int max, DirectMap mandatory, DirectMap currentSet, ref uint currentFitness)
    {
        while (true)
        {
            int bestIndex = int.MinValue;
            uint bestFitness = uint.MaxValue;

            for (int i = max; i >= -1; i--)
            {
                if (!mandatory.Contains(i) && currentSet.Remove(i))
                {
                    uint attemptCollisions = GetCollisions(data, currentSet);
                    currentSet.Add(i);

                    // If the number of collisions are the same, we prefer our new attempt, as it has one instruction less, but produce the same number of collisions
                    if (attemptCollisions < bestFitness || (attemptCollisions == bestFitness && i == -1))
                    {
                        bestIndex = i;
                        bestFitness = attemptCollisions;
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestFitness > currentFitness)
                break;

            currentSet.Remove(bestIndex);
            currentFitness = bestFitness;
            PrintMap("- new best", data, currentSet);
        }
    }

    private void MergePositions(ReadOnlySpan<string> data, int max, DirectMap mandatory, DirectMap currentSet, ref uint currentCollisions)
    {
        while (true)
        {
            int bestI1 = int.MinValue, bestI2 = int.MinValue, bestI3 = int.MinValue;
            uint bestCollisions = uint.MaxValue;

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
                                    uint attemptCollisions = GetCollisions(data, currentSet);
                                    currentSet.Add(i1);
                                    currentSet.Add(i2);
                                    currentSet.Remove(i3);

                                    // If the number of collisions are the same, we prefer our new attempt, as it has two instructions less, but produce the same number of collisions
                                    if (attemptCollisions < bestCollisions || (attemptCollisions == bestCollisions && (i1 == -1 || i2 == -1)))
                                    {
                                        bestI1 = i1;
                                        bestI2 = i2;
                                        bestI3 = i3;
                                        bestCollisions = attemptCollisions;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestCollisions > currentCollisions)
                break;

            currentSet.Remove(bestI1);
            currentSet.Remove(bestI2);
            currentSet.Add(bestI3);
            currentCollisions = bestCollisions;
            PrintMap("- new best", data, currentSet);
        }
    }

    private uint GetCollisions(ReadOnlySpan<string> data, DirectMap map) => _emulator.GetCollisions(data, map.Positions);

    private void PrintMap(string stage, ReadOnlySpan<string> data, DirectMap map)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{stage} ({GetCollisions(data, map)}): ");
        bool lastChar = false;
        bool first = true;
        foreach (int i in map.Positions)
        {
            if (!first)
                sb.Append(", ");
            if (i == -1)
                lastChar = true;
            else
            {
                sb.Append(i);
                first = false;
            }
        }

        if (lastChar)
        {
            if (!first)
                sb.Append(", ");
            sb.Append('$');
        }
        sb.AppendLine();

        LogGPerfDebug(logger, sb.ToString());
    }

    [SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'")]
    private int[] FindAlphaIncrements(List<Keyword> keywords, int maxKeyLen, int[] positions)
    {
        uint duplicatesGoal = CountDuplicates(keywords, positions);
        LogGPerfDebug(logger, "duplicates_goal: " + duplicatesGoal);

        int[] alphaInc = new int[maxKeyLen];
        uint duplicatesCurrent = CountDuplicates(keywords, positions, alphaInc);
        LogGPerfDebug(logger, "current_duplicates_count: " + duplicatesCurrent);

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

            LogGPerfDebug(logger, "nindices: " + indices.Count);

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
                        LogGPerfDebug(logger, "try_duplicates_count: " + tryDuplicatesCount);

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
                        LogGPerfDebug(logger, "new best: " + duplicatesCurrent);
                        break;
                    }
                }
            } while (duplicatesCurrent > duplicatesGoal);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                LogGPerfDebug(logger, "\nComputed alpha increments: ");
                bool first = true;

                StringBuilder sb = new StringBuilder();
                for (int j = indices.Count; j-- > 0;)
                {
                    if (alphaInc[indices[j]] != 0)
                    {
                        if (!first)
                            sb.Append(", ");
                        sb.Append($"{indices[j] + 1}:+{alphaInc[indices[j]]}");
                        first = false;
                    }
                }

                sb.AppendLine();
                LogGPerfDebug(logger, sb.ToString());
            }
        }

        return alphaInc;
    }

    private static uint CountDuplicates(List<Keyword> keywords, int[] positions, int[]? alphaInc = null)
    {
        if (alphaInc == null)
        {
            foreach (Keyword keyword in keywords)
            {
                keyword.InitSelCharsTuple(positions);
            }
        }
        else
        {
            foreach (Keyword keyword in keywords)
            {
                keyword.InitSelCharsMultiset(positions, alphaInc);
            }
        }

        HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

        uint collisions = 0;

        foreach (Keyword keyword in keywords)
        {
            if (!set.Add(keyword.SelChars))
                collisions++;
        }

        return collisions;
    }

    private static int ComputeAlphaSize(int[] alphaInc)
    {
        int maxAlphaInc = 0;
        for (int i = 0; i < alphaInc.Length; i++)
        {
            if (maxAlphaInc < alphaInc[i])
                maxAlphaInc = alphaInc[i];
        }
        return 256 + maxAlphaInc;
    }

    private static int ComputeHash(Keyword keyword, int[] assoValues)
    {
        int sum = keyword.SelChars.Sum(c => assoValues[c]);
        keyword.HashValue = sum;
        return sum;
    }

    private sealed class DirectMap
    {
        private readonly int _capacity;
        private readonly bool[] _lookup;

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

    [SuppressMessage("Minor Code Smell", "S1450:Private fields only used as local variables in methods should become local variables")]
    private sealed class AssociationTable(ILogger logger)
    {
        private int _maxHash;

        internal int[] Values { get; private set; }
        internal int[] Occurrences { get; private set; }
        internal BoolArray CollisionDetector { get; private set; }

        internal bool TryFindGoodValues(List<Keyword> keywords, int[] positions, int[] alphaInc, int alphaSize)
        {
            // Initialize each keyword's SelChars array.
            foreach (Keyword keyword in keywords)
            {
                keyword.InitSelCharsMultiset(positions, alphaInc);
            }

            // Compute the maximum SelChars.Length over all keywords.
            int maxSelCharsLength = keywords.Max(x => x.SelChars.Length);

            int totalDuplicates = 0;

            // Make a hash set for efficiency.
            HashSet<string> representatives = new HashSet<string>(StringComparer.Ordinal);

            foreach (Keyword keyword in keywords)
            {
                if (!representatives.Add(keyword.SelChars))
                    totalDuplicates++;
            }

            // Exit program if duplicates exists and option[DUP] not set, since we don't want to continue in this case.
            if (totalDuplicates > 0)
            {
                LogGPerfDebug(logger, $"{totalDuplicates} input keys have identical hash values");
                return false;
            }

            // Compute the occurrences of each character in the alphabet.
            Occurrences = new int[alphaSize];

            foreach (Keyword keyword in keywords)
            {
                foreach (char c in keyword.SelChars)
                {
                    Occurrences[c]++;
                }
            }

            Values = new int[alphaSize];
            int assoValueMax = keywords.Count;

            // Round up to the next power of two. This makes it easy to ensure an _asso_value[c] is >= 0 and < asso_value_max. Also, the jump value
            // being odd, it guarantees that Search::try_asso_value() will iterate through different values for _asso_value[c].

            if (assoValueMax == 0)
                assoValueMax = 1;

            assoValueMax |= assoValueMax >> 1;
            assoValueMax |= assoValueMax >> 2;
            assoValueMax |= assoValueMax >> 4;
            assoValueMax |= assoValueMax >> 8;
            assoValueMax |= assoValueMax >> 16;
            assoValueMax++;

            // Given the bound for _asso_values[c], we have a bound for the possible hash values, as computed in compute_hash().
            _maxHash = (assoValueMax - 1) * maxSelCharsLength;

            // Allocate a sparse bit vector for detection of collisions of hash values.
            CollisionDetector = new BoolArray(_maxHash + 1, logger);

#if DEBUG
            if (logger.IsEnabled(LogLevel.Trace))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"total non-linked keys = {keywords.Count}" +
                              $"\nmaximum associated value is {assoValueMax}" +
                              $"\nmaximum size of generated hash table is {_maxHash}");

                sb.AppendLine("\ndumping the keyword list without duplicates");
                sb.AppendLine("keyword #, keysig, keyword");

                int field_width = keywords.Max(x => x.SelChars.Length);
                int i = 0;
                foreach (Keyword keyword in keywords)
                {
                    sb.AppendLine($"{++i,9}, {keyword.SelChars.PadLeft(field_width)}, {keyword.AllChars}");
                }

                LogGPerfDebug(logger, sb.ToString());
            }
#endif

            FindAssoValues(keywords, assoValueMax, 5, maxSelCharsLength, alphaSize);
            return true;
        }

        private void FindAssoValues(List<Keyword> keywords, int assoValueMax, int jump, int maxSelCharsLength, int alphaSize)
        {
            Step? steps = null;

            // Determine the steps, starting with the last one.
            bool[] undetermined = new bool[alphaSize];

            bool[] determined = new bool[alphaSize];
            for (uint c = 0; c < alphaSize; c++)
            {
                determined[c] = true;
            }

            while (true)
            {
                // Compute the partition that needs to be refined.
                EquivalenceClass partition = ComputePartition(keywords, undetermined);

                // Console.WriteLine("_cardinality: " + partition.Cardinality);
                // Console.WriteLine("_undetermined_chars_length: " + partition.UndeterminedCharsLength);

                /* Determine the main character to be chosen in this step.
                   Choosing such a character c has the effect of splitting every equivalence class (according to the frequency of occurrence of c).
                   We choose the c with the minimum number of possible collisions, so that characters which lead to a large number of collisions get
                   handled early during the search.  */
                uint bestC = 0;
                uint bestPossibleCollisions = uint.MaxValue;
                for (uint c = 0; c < alphaSize; c++)
                {
                    if (Occurrences[c] > 0 && determined[c])
                    {
                        uint possible_collisions = CountPossibleCollisions(partition, c, maxSelCharsLength);
                        if (possible_collisions < bestPossibleCollisions)
                        {
                            bestC = c;
                            bestPossibleCollisions = possible_collisions;
                        }
                    }
                }

                if (bestPossibleCollisions == uint.MaxValue)
                    break;

                // We need one more step.
                Step step = new Step();

                step.Undetermined = new bool[alphaSize];
                undetermined.CopyTo(step.Undetermined, 0);

                step.Partition = partition;

                // Now determine how the equivalence classes will be before this step.
                undetermined[bestC] = true;
                partition = ComputePartition(keywords, undetermined);

                // Console.WriteLine("_cardinality: " + partition.Cardinality);
                // Console.WriteLine("chosen_possible_collisions: " + bestPossibleCollisions);

                /* Now determine which other characters should be determined in this step, because they will not change the equivalence classes at
                   this point.  It is the set of all c which, for all equivalence classes, have the same frequency of occurrence in every keyword
                   of the equivalence class.  */
                for (uint c = 0; c < alphaSize; c++)
                {
                    if (Occurrences[c] > 0 && determined[c] && UnchangedPartition(partition, c))
                    {
                        undetermined[c] = true;
                        determined[c] = false;
                    }
                }

                // main_c must be one of these.
                if (determined[bestC])
                    throw new InvalidOperationException("main_c");

                // Now the set of changing characters of this step.
                int changing_count = 0;
                for (uint c = 0; c < alphaSize; c++)
                {
                    if (undetermined[c] && !step.Undetermined[c])
                        changing_count++;
                }

                uint[] changing = new uint[changing_count];
                changing_count = 0;
                for (uint c = 0; c < alphaSize; c++)
                {
                    if (undetermined[c] && !step.Undetermined[c])
                        changing[changing_count++] = c;
                }

                step.Changing = changing;
                step.ChangingCount = changing_count;
                step.AssoValueMax = assoValueMax;

#if DEBUG
                step.ExpectedLower = Math.Exp((double)bestPossibleCollisions / _maxHash);
                step.ExpectedUpper = Math.Exp((double)bestPossibleCollisions / assoValueMax);
#endif

                step._next = steps;
                steps = step;
            }

#if DEBUG
            if (logger.IsEnabled(LogLevel.Trace))
            {
                StringBuilder sb = new StringBuilder();
                uint stepNum = 0;
                for (Step? step = steps; step != null; step = step._next)
                {
                    stepNum++;
                    sb.AppendLine($"Step {stepNum} chooses _asso_values[{string.Join(",", step.Changing.Select(x => "'" + (char)x + "'"))}], expected number of iterations between {step.ExpectedLower:F6} and {step.ExpectedUpper:F6}.");
                    sb.AppendLine("Keyword equivalence classes:");
                    for (EquivalenceClass? cls = step.Partition; cls != null; cls = cls.Next)
                    {
                        foreach (Keyword keyword in cls.Keywords)
                        {
                            sb.AppendLine("  " + keyword.AllChars);
                        }
                    }
                    sb.AppendLine();
                }

                LogGPerfDebug(logger, sb.ToString());
            }
#endif

            // Initialize _asso_values[]. (The value given here matters only for those c which occur in all keywords with equal multiplicity.)
            for (uint c = 0; c < alphaSize; c++)
            {
                Values[c] = 0;
            }

            uint stepNo = 0;
            for (Step? step = steps; step != null; step = step._next)
            {
                stepNo++;

                // Initialize the asso_values[].
                int k = step.ChangingCount;
                for (uint i = 0; i < k; i++)
                {
                    uint c = step.Changing[i];
                    Values[c] = 0 /*rng.Next()*/ & (step.AssoValueMax - 1);
                }

                uint iterations = 0;
                int[] iter = new int[k];

                for (int i = 0; i < k; i++)
                {
                    iter[i] = 0;
                }

                int ii = jump != 0 ? k - 1 : 0;

                while (true)
                {
                    // Test whether these asso_values[] lead to collisions among the equivalence classes that should be collision-free.
                    bool hasCollision = false;
                    for (EquivalenceClass? cls = step.Partition; cls != null; cls = cls.Next)
                    {
                        // Iteration Number array is a win, O(1) initialization time!
                        CollisionDetector.Clear();

                        foreach (Keyword keyword in cls.Keywords)
                        {
                            // Compute the new hash code for the keyword, leaving apart the yet undetermined asso_values[].
                            int sum = 0;
                            int len = keyword.SelChars.Length;
                            int offset = 0;
                            for (; len > 0; offset++, len--)
                            {
                                char p = keyword.SelChars[offset];
                                if (!step.Undetermined[p])
                                    sum += Values[p];
                            }

                            // See whether it collides with another keyword's hash code, from the same equivalence class.
                            if (CollisionDetector.SetBit(sum))
                            {
                                hasCollision = true;
                                break;
                            }
                        }

                        // Don't need to continue looking at the other equivalence classes if we already have found a collision.
                        if (hasCollision)
                            break;
                    }

                    iterations++;
                    if (!hasCollision)
                        break;

                    /* The way we try various values for
                         asso_values[step->_changing[0],...step->_changing[k-1]]
                       is like this:
                       for (bound = 0,1,...)
                         for (ii = 0,...,k-1)
                           iter[ii] := bound
                           iter[0..ii-1] := values <= bound
                           iter[ii+1..k-1] := values < bound
                       and
                         asso_values[step->_changing[i]] =
                           _initial_asso_value + iter[i] * _jump.
                       This makes it more likely to find small asso_values[].
                     */
                    int bound = iter[ii];
                    int i = 0;
                    while (i < ii)
                    {
                        uint c = step.Changing[i];
                        iter[i]++;
                        Values[c] = (Values[c] + jump) & (step.AssoValueMax - 1);
                        if (iter[i] <= bound)
                            goto foundNext;
                        Values[c] = (Values[c] - (iter[i] * jump)) & (step.AssoValueMax - 1);
                        iter[i] = 0;
                        i++;
                    }
                    i = ii + 1;
                    while (i < k)
                    {
                        uint c = step.Changing[i];
                        iter[i]++;
                        Values[c] = (Values[c] + jump) & (step.AssoValueMax - 1);
                        if (iter[i] < bound)
                            goto foundNext;
                        Values[c] = (Values[c] - (iter[i] * jump)) & (step.AssoValueMax - 1);
                        iter[i] = 0;
                        i++;
                    }

                    // Switch from one ii to the next.
                    {
                        uint c = step.Changing[ii];
                        Values[c] = (Values[c] - (bound * jump)) & (step.AssoValueMax - 1);
                        iter[ii] = 0;
                    }

                    // Here all iter[i] == 0.
                    ii++;
                    if (ii == k)
                    {
                        ii = 0;
                        bound++;
                        if (bound == step.AssoValueMax)
                        {
                            /* Out of search space!  We can either backtrack, or increase the available search space of this step.
                               It seems simpler to choose the latter solution.  */
                            step.AssoValueMax = 2 * step.AssoValueMax;
                            if (step.AssoValueMax > assoValueMax)
                            {
                                assoValueMax = step.AssoValueMax;

                                // Reinitialize MaxHash.
                                _maxHash = (assoValueMax - 1) * maxSelCharsLength;

                                // Reinitialize CollisionDetector.
                                CollisionDetector = new BoolArray(_maxHash + 1, logger);
                            }
                        }
                    }
                    {
                        uint c = step.Changing[ii];
                        iter[ii] = bound;
                        Values[c] = (Values[c] + (bound * jump)) & (step.AssoValueMax - 1);
                    }
                    foundNext: ;
                }

                LogGPerfDebug(logger, $"Step {stepNo} chose _asso_values[{string.Join(",", step.Changing.Select(x => "'" + (char)x + "'"))}] in {iterations} iterations.");
            }
        }

        private static EquivalenceClass ComputePartition(List<Keyword> keywords, bool[] undetermined)
        {
            EquivalenceClass? partition = null;
            EquivalenceClass? partitionLast = null;

            foreach (Keyword keyword in keywords)
            {
                // Compute the undetermined characters for this keyword.
                uint[] undeterminedChars = new uint[keyword.SelChars.Length];
                int undeterminedCharsLength = 0;

                for (int i = 0; i < keyword.SelChars.Length; i++)
                {
                    if (undetermined[keyword.SelChars[i]])
                        undeterminedChars[undeterminedCharsLength++] = keyword.SelChars[i];
                }

                // Look up the equivalence class to which this keyword belongs.
                EquivalenceClass? equClass;
                for (equClass = partition; equClass != null; equClass = equClass.Next)
                {
                    if (equClass.UndeterminedCharsLength == undeterminedCharsLength && Equals(equClass.UndeterminedChars, undeterminedChars, undeterminedCharsLength))
                        break;
                }

                if (equClass == null)
                {
                    equClass = new EquivalenceClass();
                    equClass.UndeterminedChars = undeterminedChars;
                    equClass.UndeterminedCharsLength = undeterminedCharsLength;

                    // Console.WriteLine("un " + undeterminedCharsLength);
                    if (partition != null)
                        partitionLast.Next = equClass;
                    else
                        partition = equClass;

                    partitionLast = equClass;
                }

                // Add the keyword to the equivalence class.
                if (equClass.Keywords == null)
                    equClass.Keywords = new List<Keyword> { keyword };
                else
                    equClass.Keywords.Add(keyword);

                equClass.Cardinality++;
            }

            return partition;
        }

        private static bool Equals(uint[] a, uint[] b, int len)
        {
            for (int offset = 0; offset != len; offset++)
            {
                if (a[offset] != b[offset])
                    return false;
            }

            return true;
        }

        private static uint CountPossibleCollisions(EquivalenceClass partition, uint c, int maxSelCharsLength)
        {
            /* Every equivalence class p is split according to the frequency of occurrence of c, leading to equivalence classes p1, p2, ...
               This leads to   (|p|^2 - |p1|^2 - |p2|^2 - ...)/2  possible collisions.
               Return the sum of this expression over all equivalence classes.  */

            uint sum = 0;
            uint[] split_cardinalities = new uint[maxSelCharsLength + 1];
            for (EquivalenceClass? cls = partition; cls != null; cls = cls.Next)
            {
                for (int i = 0; i <= maxSelCharsLength; i++)
                {
                    split_cardinalities[i] = 0;
                }

                foreach (Keyword keyword in cls.Keywords)
                {
                    int count = 0;
                    for (int i = 0; i < keyword.SelChars.Length; i++)
                    {
                        if (keyword.SelChars[i] == c)
                            count++;
                    }

                    split_cardinalities[count]++;
                }

                sum += cls.Cardinality * cls.Cardinality;
                for (int i = 0; i <= maxSelCharsLength; i++)
                {
                    sum -= split_cardinalities[i] * split_cardinalities[i];
                }
            }
            return sum;
        }

        private static bool UnchangedPartition(EquivalenceClass partition, uint c)
        {
            for (EquivalenceClass? cls = partition; cls != null; cls = cls.Next)
            {
                uint firstCount = uint.MaxValue;

                for (int idx = 0; idx < cls.Keywords.Count; idx++)
                {
                    Keyword keyword = cls.Keywords[idx];

                    uint count = 0;
                    for (int i = 0; i < keyword.SelChars.Length; i++)
                    {
                        if (keyword.SelChars[i] == c)
                            count++;
                    }

                    if (idx == 0)
                        firstCount = count;
                    else if (count != firstCount)
                        return false; // c would split this equivalence class.
                }
            }
            return true;
        }
    }

    private sealed class BoolArray
    {
        private readonly uint[] _storageArray;
        private uint _iterationNumber = 1;

        public BoolArray(int size, ILogger logger)
        {
            _storageArray = new uint[size];
            LogGPerfDebug(logger, $"\nbool array size = {size}, total bytes = {size * sizeof(uint)}");
        }

        public bool SetBit(int index)
        {
            if (_storageArray[index] == _iterationNumber)
                return true;

            _storageArray[index] = _iterationNumber;
            return false;
        }

        public void Clear()
        {
            /* If we wrap around it's time to zero things out again!  However, this only
               occurs once about every 2^32 iterations, so it will not happen more
               frequently than once per second. */
            if (++_iterationNumber == 0)
            {
                _iterationNumber = 1;
                Array.Clear(_storageArray, 0, _storageArray.Length);
            }
        }
    }

    private sealed class EquivalenceClass
    {
        // The number of keywords in this equivalence class.
        public uint Cardinality;

        // The keywords in this equivalence class.
        public List<Keyword>? Keywords;

        public EquivalenceClass? Next;

        // The undetermined selected characters for the keywords in this equivalence class, as a canonically reordered multiset.
        public uint[] UndeterminedChars;
        public int UndeterminedCharsLength;
    }

    private sealed class Step
    {
        public Step? _next;

        // Exclusive upper bound for the _asso_values[c] of this step. A power of 2.
        public int AssoValueMax;
        public uint[] Changing;

        // The characters whose values are being determined in this step.
        public int ChangingCount;

        // The keyword set partition after this step.
        public EquivalenceClass Partition;

        // The characters whose values will be determined after this step.
        public bool[] Undetermined;

#if DEBUG

        // These two are only used for debug printing
        public double ExpectedLower;
        public double ExpectedUpper;
#endif
    }

    private sealed class Keyword(string allChars)
    {
        public string AllChars { get; } = allChars;
        public string SelChars { get; private set; }
        public int HashValue { get; set; }

        internal void InitSelCharsMultiset(int[] positions, int[] alpha_inc)
        {
            char[] chars = InitSelCharsLow(positions, alpha_inc);
            Array.Sort(chars);
            SelChars = new string(chars);
        }

        internal void InitSelCharsTuple(int[] positions)
        {
            char[] chars = InitSelCharsLow(positions);
            SelChars = new string(chars);
        }

        [SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'")]
        private char[] InitSelCharsLow(int[] positions, int[]? alpha_inc = null)
        {
            Span<char> keySet = stackalloc char[positions.Length];

            int ptr = 0;
            foreach (int i in positions.OrderBy(x => x))
            {
                if (i >= AllChars.Length)
                    continue;

                int c;

                if (i == -1)
                    c = AllChars[AllChars.Length - 1];
                else if (i < AllChars.Length)
                {
                    c = AllChars[i];

                    if (alpha_inc != null)
                        c += alpha_inc[i];
                }
                else
                    continue;

                keySet[ptr++] = (char)c;
            }

            return keySet.Slice(0, ptr).ToArray();
        }
    }

    private sealed class FastSet(int dataLength)
    {
        private readonly int[] _buckets = new int[dataLength];
        private readonly Entry[] _entries = new Entry[dataLength];
        private int _count;

        internal uint GetCollisions(ReadOnlySpan<string> data, List<int> positions)
        {
            if (positions.Count == 0)
                return (uint)(data.Length - 1);

            uint collisions = 0;

            foreach (string s in data)
            {
                if (!Add(s, positions))
                    collisions++;
            }

            //Clear the set
            Array.Clear(_buckets, 0, _buckets.Length);
            Array.Clear(_entries, 0, _entries.Length);
            _count = 0;

            return collisions;
        }

        private bool Add(string value, List<int> positions)
        {
            ulong hashCode = HashString(value, positions);
            ref int bucket = ref _buckets[hashCode % (ulong)_buckets.Length];
            int i = bucket - 1;

            while (i >= 0)
            {
                Entry entry = _entries[i];

                if (entry.Hash == hashCode && Equal(entry.Value, value, positions))
                    return false;

                i = entry.Next;
            }

            ref Entry newEntry = ref _entries[_count];
            newEntry.Hash = hashCode;
            newEntry.Next = bucket - 1;
            newEntry.Value = value;
            bucket = ++_count;
            return true;
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

        private static uint HashString(string str, List<int> positions)
        {
            //This hash function is PJW hash

            uint h = 0;
            foreach (int pos in positions)
            {
                char c;

                if (pos == -1)
                    c = str[str.Length - 1];
                else if (pos <= str.Length - 1)
                    c = str[pos];
                else
                    continue;

                h = (h << 4) + c;

                uint high = h & 0xf0000000;

                if (high != 0)
                    h = h ^ (high >> 24) ^ high;
            }
            return h;
        }

        [StructLayout(LayoutKind.Auto)]
        private record struct Entry(ulong Hash, int Next, string Value);
    }
}