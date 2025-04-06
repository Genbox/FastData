namespace Genbox.FastData.Internal.Structures;

internal ref struct AssociationTable
{
    internal int[] Values { get; set; }
    internal int MaxHash { get; set; }
    internal int[] Occurrences { get; set; }
    internal BoolArray CollisionDetector { get; set; }

    internal bool TryFindGoodValues(List<Keyword> keywords, int[] positions, int[] alphaInc, int alphaSize)
    {
        // Initialize each keyword's SelChars array.
        foreach (Keyword keyword in keywords)
            keyword.InitSelCharsMultiset(positions, alphaInc);

        // Compute the maximum SelChars.Length over all keywords.
        int maxSelCharsLength = keywords.Max(x => x.SelChars.Length);
        // Console.WriteLine("_max_selchars_length: " + maxSelCharsLength);

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
            DebugHelper.Print($"{totalDuplicates} input keys have identical hash values");
            return false;
        }

        // Compute the occurrences of each character in the alphabet.
        Occurrences = new int[alphaSize];

        foreach (Keyword keyword in keywords)
            foreach (char c in keyword.SelChars)
                Occurrences[c]++;

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
        MaxHash = (assoValueMax - 1) * maxSelCharsLength;

        // Allocate a sparse bit vector for detection of collisions of hash values.
        CollisionDetector = new BoolArray(MaxHash + 1);

#if DebugPrint
        Console.WriteLine($"total non-linked keys = {keywords.Count}" +
                          $"\nmaximum associated value is {assoValueMax}" +
                          $"\nmaximum size of generated hash table is {MaxHash}");

        Console.WriteLine("\ndumping the keyword list without duplicates");
        Console.WriteLine("keyword #, keysig, keyword");

        int field_width = keywords.Max(x => x.SelChars.Length);
        int i = 0;
        foreach (Keyword keyword in keywords)
            Console.WriteLine($"{++i,9}, {keyword.SelChars.PadLeft(field_width)}, {keyword.AllChars}");
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
            determined[c] = true;

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
                if (Occurrences[c] > 0 && determined[c])
                {
                    uint possible_collisions = CountPossibleCollisions(partition, c, maxSelCharsLength);
                    if (possible_collisions < bestPossibleCollisions)
                    {
                        bestC = c;
                        bestPossibleCollisions = possible_collisions;
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
                if (Occurrences[c] > 0 && determined[c] && UnchangedPartition(partition, c))
                {
                    undetermined[c] = true;
                    determined[c] = false;
                }

            // main_c must be one of these.
            if (determined[bestC])
                throw new InvalidOperationException("main_c");

            // Now the set of changing characters of this step.
            int changing_count = 0;
            for (uint c = 0; c < alphaSize; c++)
                if (undetermined[c] && !step.Undetermined[c])
                    changing_count++;

            uint[] changing = new uint[changing_count];
            changing_count = 0;
            for (uint c = 0; c < alphaSize; c++)
                if (undetermined[c] && !step.Undetermined[c])
                    changing[changing_count++] = c;

            step.Changing = changing;
            step.ChangingCount = changing_count;
            step.AssoValueMax = assoValueMax;
            step.ExpectedLower = Math.Exp(bestPossibleCollisions / (double)MaxHash);
            step.ExpectedUpper = Math.Exp(bestPossibleCollisions / (double)assoValueMax);

            step._next = steps;
            steps = step;
        }

#if DebugPrint
        uint stepNum = 0;
        for (Step? step = steps; step != null; step = step._next)
        {
            stepNum++;
            Console.WriteLine($"Step {stepNum} chooses _asso_values[{string.Join(",", step.Changing.Select(x => "'" + (char)x + "'"))}], expected number of iterations between {step.ExpectedLower:F6} and {step.ExpectedUpper:F6}.");

            Console.WriteLine("Keyword equivalence classes:");
            for (EquivalenceClass? cls = step.Partition; cls != null; cls = cls.Next)
            {
                foreach (Keyword keyword in cls.Keywords)
                    Console.WriteLine("  " + keyword.AllChars);
            }
            Console.WriteLine();
        }
#endif

        // Initialize _asso_values[]. (The value given here matters only for those c which occur in all keywords with equal multiplicity.)
        for (uint c = 0; c < alphaSize; c++)
            Values[c] = 0;

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
                iter[i] = 0;

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
                            MaxHash = (assoValueMax - 1) * maxSelCharsLength;

                            // Reinitialize CollisionDetector.
                            CollisionDetector = new BoolArray(MaxHash + 1);
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

#if DebugPrint
            Console.WriteLine($"Step {stepNo} chose _asso_values[{string.Join(",", step.Changing.Select(x => "'" + (char)x + "'"))}] in {iterations} iterations.");
#endif
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
                if (undetermined[keyword.SelChars[i]])
                    undeterminedChars[undeterminedCharsLength++] = keyword.SelChars[i];

            // Look up the equivalence class to which this keyword belongs.
            EquivalenceClass? equClass;
            for (equClass = partition; equClass != null; equClass = equClass.Next)
                if (equClass.UndeterminedCharsLength == undeterminedCharsLength && Equals(equClass.UndeterminedChars, undeterminedChars, undeterminedCharsLength))
                    break;

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
            if (a[offset] != b[offset])
                return false;

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
                split_cardinalities[i] = 0;

            foreach (Keyword keyword in cls.Keywords)
            {
                int count = 0;
                for (int i = 0; i < keyword.SelChars.Length; i++)
                    if (keyword.SelChars[i] == c)
                        count++;

                split_cardinalities[count]++;
            }

            sum += cls.Cardinality * cls.Cardinality;
            for (int i = 0; i <= maxSelCharsLength; i++)
                sum -= split_cardinalities[i] * split_cardinalities[i];
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
                    if (keyword.SelChars[i] == c)
                        count++;

                if (idx == 0)
                    firstCount = count;
                else if (count != firstCount)
                    return false; // c would split this equivalence class.
            }
        }
        return true;
    }
}