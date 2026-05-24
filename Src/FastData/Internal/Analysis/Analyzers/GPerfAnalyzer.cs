using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

/*
  This is the core algorithm behind GPerf. It takes inspiration from Cichelli's Minimal Perfect Hash function but also supports
  generating nearly perfect hash functions and thus works with larger datasets.

  The algorithm searches for three projections that are injective on the keyword set:
  1. selected positions produce distinct tuples,
  2. alpha increments turn those tuples into distinct multisets,
  3. association values turn the multisets into distinct hash values.

  For case-insensitive hashing, alpha_unify maps each selected character to a canonical association-table index so that changing
  ASCII case does not change asso_values[alpha_unify[c + alpha_inc[i]]].

  I've only focused on the core algorithm, and not all the other application features such as switch-generation, length-table support, etc.
  as they overlap with many of the features in this application.

  FastData intentionally differs from the upstream gperf CLI by validating public configuration strictly: negative numeric settings
  and explicit key positions outside the analyzed byte length throw instead of being warned about, clamped, or ignored. Duplicate
  keyword signatures remain unsupported. The '*' key-position wildcard still means all gperf-supported fixed byte positions present in the dataset.
 */

/// <summary>Finds the least number of positions in a string that hashes to a unique value for all inputs.</summary>
[SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.")]
internal sealed partial class GPerfAnalyzer : IStringHashAnalyzer
{
    private static readonly KeywordSelectedCharsComparer _selectedCharsComparer = new KeywordSelectedCharsComparer(false);
    private static readonly KeywordSelectedCharsComparer _selectedCharsWithLengthComparer = new KeywordSelectedCharsComparer(true);
    private readonly FastSet _emulator;
    private readonly GeneratorEncoding _encoding;
    private readonly bool _hashIncludesLength;
    private readonly bool _ignoreCase;
    private readonly ILogger<GPerfAnalyzer> _logger;
    private readonly GPerfAnalyzerOptions _options;
    private readonly StringKeyProperties _props;
    private readonly Simulator _sim;

    internal GPerfAnalyzer(int dataLength, StringKeyProperties props, GPerfAnalyzerConfig config, Simulator sim, ILogger<GPerfAnalyzer> logger, GeneratorEncoding encoding, bool ignoreCase)
    {
        _props = props;
        _sim = sim;
        _logger = logger;
        _encoding = encoding;
        _ignoreCase = ignoreCase;
        _options = config.CreateOptions();
        _hashIncludesLength = !_options.NoLength && _props.LengthData.LengthRanges.Min != _props.LengthData.LengthRanges.Max;
        _emulator = new FastSet(dataLength, _ignoreCase, _hashIncludesLength);

        if (_options.SizeMultiple > 50)
            LogSizeMultipleExcessive(_logger, _options.SizeMultiple);
        else if (_options.SizeMultiple < 0.01)
            LogSizeMultipleSmall(_logger, _options.SizeMultiple);
    }

    public bool IsAppropriate() => _encoding != GeneratorEncoding.Unknown && (!_ignoreCase || _props.CharacterData.AllAscii) && (_encoding != GeneratorEncoding.AsciiBytes || _props.CharacterData.AllAscii);

    public IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data)
    {
        // We cannot work on empty strings
        if (_props.LengthData.MinCharLength == 0)
            return [];

        Func<string, byte[]> getBytes = StringHelper.GetBytesFunc(_encoding);

        int minLen = int.MaxValue;
        int maxLen = 0;

        List<Keyword> keywords = new List<Keyword>(data.Length);

        foreach (string s in data)
        {
            byte[] bytes = getBytes(s);

            if (bytes.Length == 0)
                return [];

            if (_options.SevenBit && !AllSevenBit(bytes))
                return [];

            minLen = Math.Min(minLen, bytes.Length);
            maxLen = Math.Max(maxLen, bytes.Length);
            keywords.Add(new Keyword(s, bytes));
        }

        // Step1: Find positions
        SelectedPositions positions = GetPositions(keywords, maxLen);
        if (_logger.IsEnabled(LogLevel.Debug))
            LogPositions(_logger, string.Join(", ", positions.Descending));

        // Step 2: Find good alpha increments
        int[] alphaInc = FindAlphaIncrements(keywords, maxLen, positions);
        int alphaSize = ComputeAlphaSize(alphaInc);
        uint[]? alphaUnify = ComputeAlphaUnify(keywords, positions, alphaInc, alphaSize);
        if (_logger.IsEnabled(LogLevel.Debug))
            LogAlphaPositions(_logger, alphaSize, string.Join(", ", alphaInc));

        // Step 3: Finding good association table values
        AssociationTable table = new AssociationTable(_logger, _hashIncludesLength, _options);

        if (!table.TryFindGoodValues(keywords, positions, alphaInc, alphaUnify, alphaSize))
        {
            LogUnableToFindAsso(_logger);
            return [];
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            LogAsso(_logger, string.Join(", ", table.Values));

        // Make one final check, just to make sure nothing weird happened
        table.CollisionDetector.Clear();
        foreach (Keyword keyword in keywords)
        {
            int hashcode = ComputeHash(keyword, table.Values, _hashIncludesLength);

            // This shouldn't happen. proj1, proj2, proj3 must have been computed to be injective on the given keyword set.
            if (table.CollisionDetector.SetBit(hashcode))
                throw new InvalidOperationException("Internal error, unexpected duplicate hash code"); // We throw here instead of returning false, as if the check fails, it is because of buggy code
        }

        // Sort all the things
        keywords.Sort((x, y) => x.HashValue.CompareTo(y.HashValue));

        // Set unused asso[c] to maxHash + 1.
        // This is not necessary but speeds up the lookup function in many cases of lookup failure:
        // no string comparison is needed once the hash value of a string is larger than the hash value of any keyword.
        int maxHash = keywords[keywords.Count - 1].HashValue;

        LogMaxHash(_logger, maxHash);

        for (int i = 0; i < alphaSize; i++)
        {
            if (table.Occurrences[i] == 0)
                table.Values[i] = maxHash + 1;
        }

        ApplyAlphaUnify(table.Values, alphaUnify);

#if DEBUG
        if (_logger.IsEnabled(LogLevel.Trace))
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
            int field_width = 0;
            foreach (Keyword keyword in keywords)
                field_width = Math.Max(field_width, FormatSelChars(keyword).Length);

            foreach (Keyword keyword in keywords)
                sb.AppendLine($"{keyword.HashValue,11},{keyword.Length,11},{"-1",6}, {FormatSelChars(keyword).PadLeft(field_width)}, {keyword.AllChars}");

            sb.AppendLine("End dumping list.\n");

            LogGPerfDebug(_logger, sb.ToString());
        }
#endif

        // We convert keywords to KeyValuePair to keep Keyword internal
        GPerfStringHash stringHash = new GPerfStringHash(table.Values, alphaInc, positions.Descending, minLen, _hashIncludesLength, _encoding, _options.SevenBit, _props.LengthData.LengthRanges.Min);

        Candidate candidate = _sim.Run(data, stringHash);
        LogCandidate(_logger, candidate.Fitness, candidate.Collisions);

        return [candidate];
    }

    private SelectedPositions GetPositions(List<Keyword> data, int maxLen)
    {
        /*
            This is the algorithm for finding positions that results in a good hash. It is based on find_positions() in GPerf.

            It tries to find positions in input strings that give as few projection duplicates as possible. In the event it finds a function with 0 duplicates,
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
            2. Attempt to add each character in the strings and rehash. If we get fewer projection duplicates, we add the new position to the set.
               - result: 0, 1
            3. Attempt to remove each character from the set. If we get the same number of duplicates with a smaller set, we keep the smaller set.
               - result: 0, 1
            4. Attempt to merge two positions into one. If we get the same number of duplicates, we keep the smaller set.
               - result: 0, 1

            It has been reimplemented with the following properties:
            - Uses HashSet for fast lookups. It is a tradeoff as results needs to be sorted. Either we have the overhead of sorting, or we have the overhead of traversing.
              For very large sets, this implementation should be faster)
            - Added additional logging to ensure complete feature parity with GPerf
            - Has the ability to set the max number of characters to check via configuration
            - Uses DirectMap that avoids modulo by directly addressing into an array. Provides better performance.

            Predefined key positions bypass this automatic search, like gperf's -k option.
        */

        if (_options.UseAllKeyPositions)
            return new SelectedPositions(CreateAllDatasetPositions(maxLen));

        int[]? configuredPositions = _options.KeyPositions;
        if (configuredPositions != null)
        {
            ValidateConfiguredPositions(configuredPositions, maxLen);
            return new SelectedPositions(configuredPositions);
        }

        int max = Math.Min(maxLen - 1, _options.MaxConfiguredPosition);

        // Stage 1: Find all positions that are mandatory. If two items are the same length, but differ only on one character, then we must include that character.
        DirectMap mandatory = new DirectMap(maxLen);
        GetMandatory(data, mandatory, _ignoreCase);
        PrintMap("Stage1", data, mandatory);

        // Stage 2: Attempt to use just the mandatory positions first, then add positions as long as they decrease the duplicate count.
        DirectMap current = new DirectMap(mandatory);
        uint currentDuplicates = CountPositionDuplicates(data, current);
        AddWhileBetter(data, max, current, ref currentDuplicates);
        PrintMap("Stage2", data, current);

        // Stage 3: Remove positions, as long as this doesn't increase the duplicate count.
        RemoveSinglePositions(data, max, mandatory, current, ref currentDuplicates);
        PrintMap("Stage3", data, current);

        // Stage 4: Replace two positions by one, as long as this doesn't increase the duplicate count.
        MergePositions(data, max, mandatory, current, ref currentDuplicates);
        PrintMap("Stage4", data, current);

        return new SelectedPositions(current.Positions);
    }

    private static int[] CreateAllDatasetPositions(int maxLen)
    {
        int count = Math.Min(maxLen, 255);
        int[] all = new int[count];
        for (int i = 0; i < all.Length; i++)
            all[i] = i;

        return all;
    }

    private static void ValidateConfiguredPositions(int[] positions, int maxLen)
    {
        foreach (int position in positions)
        {
            if (position >= maxLen)
                throw new InvalidOperationException($"GPerf key position {position + 1} is outside the maximum encoded key length {maxLen}.");
        }
    }

    private static void GetMandatory(List<Keyword> data, DirectMap mandatory, bool ignoreCase)
    {
        for (int i1 = 0; i1 < data.Count - 1; i1++)
        {
            Keyword keyword1 = data[i1];

            for (int i2 = i1 + 1; i2 < data.Count; i2++) // We want to avoid comparing the item to itself
            {
                Keyword keyword2 = data[i2];

                if (keyword1.Length == keyword2.Length) // Same length
                {
                    int i;
                    for (i = 0; i < keyword1.Length - 1; i++)
                    {
                        if (!CharsEqual(keyword1.ByteAt(i), keyword2.ByteAt(i), ignoreCase))
                            break; // Stop on the first char that is different
                    }

                    // If we stopped before the end of the string, find the next offset where the chars differ
                    if (i < keyword1.Length - 1)
                    {
                        int j;
                        for (j = i + 1; j < keyword1.Length; j++)
                        {
                            if (!CharsEqual(keyword1.ByteAt(j), keyword2.ByteAt(j), ignoreCase))
                                break;
                        }

                        if (j >= keyword1.Length)
                            mandatory.Add(i);
                    }
                }
            }
        }
    }

    private static bool CharsEqual(uint left, uint right, bool ignoreCase)
    {
        if (ignoreCase)
        {
            left = ToLowerAscii(left);
            right = ToLowerAscii(right);
        }

        return left == right;
    }

    private static uint ToLowerAscii(uint c) => c >= 'A' && c <= 'Z' ? c + ('a' - 'A') : c;

    private void AddWhileBetter(List<Keyword> data, int max, DirectMap currentSet, ref uint currentDuplicates)
    {
        while (true)
        {
            int bestIndex = int.MinValue;
            uint bestDuplicates = uint.MaxValue;

            for (int i = max; i >= -1; i--)
            {
                if (currentSet.Add(i))
                {
                    uint attemptDuplicates = CountPositionDuplicates(data, currentSet);
                    currentSet.RemoveLast(i);

                    if (attemptDuplicates < bestDuplicates || (attemptDuplicates == bestDuplicates && i >= 0))
                    {
                        bestIndex = i;
                        bestDuplicates = attemptDuplicates;
                    }
                }
            }

            // Stop adding positions when it gives no improvement.
            if (bestDuplicates >= currentDuplicates)
                break;

            currentSet.Add(bestIndex);
            currentDuplicates = bestDuplicates;
            PrintMap("- new best", data, currentSet);
        }
    }

    private void RemoveSinglePositions(List<Keyword> data, int max, DirectMap mandatory, DirectMap currentSet, ref uint currentDuplicates)
    {
        while (true)
        {
            int bestIndex = int.MinValue;
            uint bestDuplicates = uint.MaxValue;

            for (int i = max; i >= -1; i--)
            {
                if (!mandatory.Contains(i) && currentSet.Remove(i))
                {
                    uint attemptDuplicates = CountPositionDuplicates(data, currentSet);
                    currentSet.Add(i);

                    // If the duplicate count is the same, we prefer our new attempt, as it has one instruction less.
                    if (attemptDuplicates < bestDuplicates || (attemptDuplicates == bestDuplicates && i == -1))
                    {
                        bestIndex = i;
                        bestDuplicates = attemptDuplicates;
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestDuplicates > currentDuplicates)
                break;

            currentSet.Remove(bestIndex);
            currentDuplicates = bestDuplicates;
            PrintMap("- new best", data, currentSet);
        }
    }

    private void MergePositions(List<Keyword> data, int max, DirectMap mandatory, DirectMap currentSet, ref uint currentDuplicates)
    {
        while (true)
        {
            int bestI1 = int.MinValue, bestI2 = int.MinValue, bestI3 = int.MinValue;
            uint bestDuplicates = uint.MaxValue;

            for (int i1 = max; i1 >= -1; i1--)
            {
                if (currentSet.Contains(i1) && !mandatory.Contains(i1))
                {
                    for (int i2 = i1 - 1; i2 >= -1; i2--)
                    {
                        if (i2 != i1 && currentSet.Contains(i2) && !mandatory.Contains(i2))
                        {
                            for (int i3 = max; i3 >= -1; i3--)
                            {
                                if (currentSet.Add(i3))
                                {
                                    currentSet.Remove(i1);
                                    currentSet.Remove(i2);
                                    uint attemptDuplicates = CountPositionDuplicates(data, currentSet);
                                    currentSet.Add(i1);
                                    currentSet.Add(i2);
                                    currentSet.Remove(i3);

                                    // If the duplicate count is the same, we prefer our new attempt, as it has two instructions less.
                                    if (attemptDuplicates < bestDuplicates || (attemptDuplicates == bestDuplicates && (i1 == -1 || i2 == -1) && i3 >= 0))
                                    {
                                        bestI1 = i1;
                                        bestI2 = i2;
                                        bestI3 = i3;
                                        bestDuplicates = attemptDuplicates;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Stop removing positions when it gives no improvement.
            if (bestDuplicates > currentDuplicates)
                break;

            currentSet.Remove(bestI1);
            currentSet.Remove(bestI2);
            currentSet.Add(bestI3);
            currentDuplicates = bestDuplicates;
            PrintMap("- new best", data, currentSet);
        }
    }

    private uint CountPositionDuplicates(List<Keyword> data, DirectMap map) => _emulator.CountDups(data, map.Positions);

    private void PrintMap(string stage, List<Keyword> data, DirectMap map)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
            return;

        StringBuilder sb = new StringBuilder();
        sb.Append($"{stage} ({CountPositionDuplicates(data, map)}): ");
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
                sb.Append(i + 1);
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

        LogGPerfDebug(_logger, sb.ToString());
    }

    private int[] FindAlphaIncrements(List<Keyword> keywords, int maxKeyLen, SelectedPositions positions)
    {
        bool trace = _logger.IsEnabled(LogLevel.Trace);
        uint duplicatesGoal = CountDuplicates(keywords, positions, ComputeAlphaUnify(), null, _hashIncludesLength);
        if (trace)
            LogGPerfDebug(_logger, "duplicates_goal: " + duplicatesGoal);

        int alphaIncLength = Math.Max(maxKeyLen, positions.MaxFixedPosition + 1);
        int[] alphaInc = new int[alphaIncLength];
        uint[]? alphaUnify = _ignoreCase ? ComputeAlphaUnify(keywords, positions, alphaInc, ComputeAlphaSize(alphaInc)) : null;
        uint duplicatesCurrent = CountDuplicates(keywords, positions, alphaUnify, alphaInc, _hashIncludesLength);
        if (trace)
            LogGPerfDebug(_logger, "current_duplicates_count: " + duplicatesCurrent);

        if (duplicatesCurrent > duplicatesGoal)
        {
            // Look which alphaInc[i] we are free to increment
            List<int> indices = new List<int>(positions.Count);

            foreach (int keyPos in positions.Descending)
            {
                if (keyPos >= maxKeyLen || keyPos == -1)
                    continue;

                indices.Add(keyPos);
            }

            if (trace)
                LogGPerfDebug(_logger, "nindices: " + indices.Count);

            // Perform several rounds of searching for a good alpha increment.
            // Each round reduces the number of artificial collisions by adding an increment in a single key position.
            int[] best = new int[alphaIncLength];
            int[] attempt = new int[alphaIncLength];

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
                        uint[]? attemptAlphaUnify = _ignoreCase ? ComputeAlphaUnify(keywords, positions, attempt, ComputeAlphaSize(attempt)) : null;
                        uint tryDuplicatesCount = CountDuplicates(keywords, positions, attemptAlphaUnify, attempt, _hashIncludesLength);
                        if (trace)
                            LogGPerfDebug(_logger, "try_duplicates_count: " + tryDuplicatesCount);

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
                        if (trace)
                            LogGPerfDebug(_logger, "new best: " + duplicatesCurrent);
                        break;
                    }
                }
            } while (duplicatesCurrent > duplicatesGoal);

            if (trace)
            {
                LogGPerfDebug(_logger, "\nComputed alpha increments: ");
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
                LogGPerfDebug(_logger, sb.ToString());
            }
        }

        return alphaInc;
    }

    private uint[]? ComputeAlphaUnify()
    {
        if (!_ignoreCase)
            return null;

        uint[] alphaUnify = CreateIdentityAlphaUnify(GetAlphabetSize());
        for (uint c = 'A'; c <= 'Z'; c++)
            alphaUnify[c] = c + ('a' - 'A');

        return alphaUnify;
    }

    private uint[]? ComputeAlphaUnify(List<Keyword> keywords, SelectedPositions positions, int[] alphaInc, int alphaSize)
    {
        if (!_ignoreCase)
            return null;

        // Without alpha increments, case-insensitive mode would simply unify 'A' -> 'a', ..., 'Z' -> 'z'.
        // With increments, selected characters require:
        // asso_values[tolower(c) + alpha_inc[i]] == asso_values[toupper(c) + alpha_inc[i]].
        // These unifications can extend outside the ASCII-letter range, but every chain remains a multiple of 32 apart.
        uint[] alphaUnify = CreateIdentityAlphaUnify(alphaSize);

        foreach (Keyword keyword in keywords)
        {
            foreach (int position in positions.Descending)
            {
                if (!keyword.TryGetSelectedChar(position, out uint c))
                    continue;

                if (c >= 'A' && c <= 'Z')
                    c += 'a' - 'A';

                if (c >= 'a' && c <= 'z')
                {
                    if (position != -1)
                        c += (uint)alphaInc[position];

                    uint target = alphaUnify[c];
                    uint upper = c - ('a' - 'A');

                    for (int current = (int)upper; current >= 0 && alphaUnify[(uint)current] == upper; current -= 'a' - 'A')
                        alphaUnify[(uint)current] = target;
                }
            }
        }

        return alphaUnify;
    }

    private static uint[] CreateIdentityAlphaUnify(int alphaSize)
    {
        uint[] alphaUnify = new uint[alphaSize];
        for (uint c = 0; c < alphaUnify.Length; c++)
            alphaUnify[c] = c;

        return alphaUnify;
    }

    private static void ApplyAlphaUnify(int[] values, uint[]? alphaUnify)
    {
        if (alphaUnify == null)
            return;

        for (uint c = 0; c < alphaUnify.Length; c++)
        {
            if (alphaUnify[c] != c)
                values[c] = values[alphaUnify[c]];
        }
    }

    private static bool AllSevenBit(byte[] bytes)
    {
        foreach (byte b in bytes)
        {
            if (b >= 128)
                return false;
        }

        return true;
    }

    private static uint CountDuplicates(List<Keyword> keywords, SelectedPositions positions, uint[]? alphaUnify, int[]? alphaInc, bool hashIncludesLength)
    {
        // Counts #K - #projection(K). The result is independent of keyword order.
        if (alphaInc == null)
        {
            foreach (Keyword keyword in keywords)
                keyword.InitSelCharsTuple(positions, alphaUnify);
        }
        else
        {
            foreach (Keyword keyword in keywords)
                keyword.InitSelCharsMultiset(positions, alphaInc, alphaUnify);
        }

        HashSet<Keyword> set = new HashSet<Keyword>(GetSelectedCharsComparer(hashIncludesLength));

        uint duplicates = 0;

        foreach (Keyword keyword in keywords)
        {
            if (!set.Add(keyword))
                duplicates++;
        }

        return duplicates;
    }

    private int ComputeAlphaSize(int[] alphaInc)
    {
        int maxAlphaInc = 0;
        for (int i = 0; i < alphaInc.Length; i++)
        {
            if (maxAlphaInc < alphaInc[i])
                maxAlphaInc = alphaInc[i];
        }
        return GetAlphabetSize() + maxAlphaInc;
    }

    private int GetAlphabetSize() => _options.SevenBit ? 128 : 256;

    private static int ComputeHash(Keyword keyword, int[] assoValues, bool hashIncludesLength)
    {
        int sum = hashIncludesLength ? keyword.Length : 0;

        uint[] selChars = keyword.SelChars;
        for (int i = 0; i < keyword.SelCharsLength; i++)
            sum += assoValues[selChars[i]];

        keyword.HashValue = sum;
        return sum;
    }

    private static KeywordSelectedCharsComparer GetSelectedCharsComparer(bool hashIncludesLength) => hashIncludesLength ? _selectedCharsWithLengthComparer : _selectedCharsComparer;

    private static string FormatSelChars(Keyword keyword)
    {
        StringBuilder sb = new StringBuilder(keyword.SelCharsLength);
        uint[] chars = keyword.SelChars;

        for (int i = 0; i < keyword.SelCharsLength; i++)
        {
            uint c = chars[i];
            if (c <= char.MaxValue)
                sb.Append((char)c);
            else
                sb.Append('[').Append(c).Append(']');
        }

        return sb.ToString();
    }

    private sealed class KeywordSelectedCharsComparer(bool hashIncludesLength) : IEqualityComparer<Keyword>
    {
        public bool Equals(Keyword? x, Keyword? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x == null || y == null || x.SelCharsLength != y.SelCharsLength)
                return false;

            if (hashIncludesLength && x.Length != y.Length)
                return false;

            for (int i = 0; i < x.SelCharsLength; i++)
            {
                if (x.SelChars[i] != y.SelChars[i])
                    return false;
            }

            return true;
        }

        public int GetHashCode(Keyword obj)
        {
            int hash = hashIncludesLength ? obj.Length : 0;
            uint[] selChars = obj.SelChars;

            unchecked
            {
                for (int i = 0; i < obj.SelCharsLength; i++)
                    hash = (hash * 397) ^ (int)selChars[i];
            }

            return hash;
        }
    }

    private readonly struct SelectedPositions
    {
        internal SelectedPositions(IReadOnlyList<int> positions)
        {
            Ascending = new int[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                Ascending[i] = positions[i];

            Array.Sort(Ascending);

            for (int i = 1; i < Ascending.Length; i++)
                Debug.Assert(Ascending[i - 1] != Ascending[i], "GPerf positions must be unique.");

            Descending = new int[Ascending.Length];
            for (int i = 0; i < Ascending.Length; i++)
                Descending[i] = Ascending[Ascending.Length - 1 - i];
        }

        internal int[] Ascending { get; }
        internal int[] Descending { get; }
        internal int Count => Ascending.Length;

        internal int MaxFixedPosition
        {
            get
            {
                int max = -1;
                for (int i = 0; i < Ascending.Length; i++)
                {
                    if (Ascending[i] > max)
                        max = Ascending[i];
                }

                return max;
            }
        }
    }

    private sealed class DirectMap
    {
        private readonly bool[] _lookup;

        internal DirectMap(DirectMap input)
        {
            _lookup = new bool[input._lookup.Length];
            Array.Copy(input._lookup, _lookup, _lookup.Length);

            Positions = new List<int>(input.Positions);
        }

        internal DirectMap(int capacity)
        {
            _lookup = new bool[capacity + 1]; //We add space for -1
            Positions = new List<int>(capacity);
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
            Debug.Assert(Positions.Count > 0 && Positions[Positions.Count - 1] == index, "RemoveLast assumes the index was just appended.");
            _lookup[index + 1] = false;
            Positions.RemoveAt(Positions.Count - 1);
        }
    }

    [SuppressMessage("Minor Code Smell", "S1450:Private fields only used as local variables in methods should become local variables")]
    private sealed class AssociationTable(ILogger logger, bool hashIncludesLength, GPerfAnalyzerOptions options)
    {
        private int _maxHash;

        internal int[] Values { get; private set; } = [];
        internal int[] Occurrences { get; private set; } = [];
        internal BoolArray CollisionDetector { get; private set; } = null!;

        internal bool TryFindGoodValues(List<Keyword> keywords, SelectedPositions positions, int[] alphaInc, uint[]? alphaUnify, int alphaSize)
        {
            // Initialize each keyword's SelChars array.
            foreach (Keyword keyword in keywords)
                keyword.InitSelCharsMultiset(positions, alphaInc, alphaUnify);

            int maxSelCharsLength = 0;
            int maxHashBase = 0;
            int totalDuplicates = 0;

            // Duplicate representatives compare selected-character multisets and, when length is part of the hash, keyword length.
            HashSet<Keyword> representatives = new HashSet<Keyword>(GetSelectedCharsComparer(hashIncludesLength));
#if NET6_0_OR_GREATER
            representatives.EnsureCapacity(keywords.Count);
#endif

            // Compute the occurrences of each character in the alphabet.
            Occurrences = new int[alphaSize];

            foreach (Keyword keyword in keywords)
            {
                if (maxSelCharsLength < keyword.SelCharsLength)
                    maxSelCharsLength = keyword.SelCharsLength;
                if (hashIncludesLength && maxHashBase < keyword.Length)
                    maxHashBase = keyword.Length;

                if (!representatives.Add(keyword))
                    totalDuplicates++;

                uint[] selChars = keyword.SelChars;
                for (int i = 0; i < keyword.SelCharsLength; i++)
                    Occurrences[selChars[i]]++;
            }

            // Exit program if duplicates exists and option[DUP] not set, since we don't want to continue in this case.
            if (totalDuplicates > 0)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                    LogGPerfDebug(logger, $"{totalDuplicates} input keys have identical hash values");
                return false;
            }

            Values = new int[alphaSize];
            int assoValueMax = GetInitialAssociationValueMax(keywords.Count, options.SizeMultiple);

            // Round up to the next power of two. This makes it easy to ensure an _asso_value[c] is >= 0 and < asso_value_max. Also, the jump value
            // being odd, it guarantees that Search::try_asso_value() will iterate through different values for _asso_value[c].

            // Given the bound for _asso_values[c], we have a bound for the possible hash values, as computed in compute_hash().
            ReinitializeHashBounds(assoValueMax, maxSelCharsLength, maxHashBase);

#if DEBUG
            if (logger.IsEnabled(LogLevel.Trace))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"total non-linked keys = {keywords.Count}" +
                              $"\nmaximum associated value is {assoValueMax}" +
                              $"\nmaximum size of generated hash table is {_maxHash}");

                sb.AppendLine("\ndumping the keyword list without duplicates");
                sb.AppendLine("keyword #, keysig, keyword");

                int field_width = 0;
                foreach (Keyword keyword in keywords)
                    field_width = Math.Max(field_width, FormatSelChars(keyword).Length);

                int i = 0;
                foreach (Keyword keyword in keywords)
                    sb.AppendLine($"{++i,9}, {FormatSelChars(keyword).PadLeft(field_width)}, {keyword.AllChars}");

                LogGPerfDebug(logger, sb.ToString());
            }
#endif

            FindGoodAssoValues(keywords, assoValueMax, maxSelCharsLength, alphaSize, maxHashBase);
            return true;
        }

        private void FindGoodAssoValues(List<Keyword> keywords, int assoValueMax, int maxSelCharsLength, int alphaSize, int maxHashBase)
        {
            if (options.MultipleIterations == 0)
            {
                Random? random = CreateRandom();
                FindAssoValues(keywords, ref assoValueMax, options.InitialAssociationValue, options.Jump, random, maxSelCharsLength, alphaSize, maxHashBase);
                return;
            }

            int bestCollisions = int.MaxValue;
            int bestMaxHash = int.MaxValue;
            int[] bestAssoValues = new int[alphaSize];
            int initialAssociationValue = 0;
            int jump = 1;

            for (int iteration = 0; iteration < options.MultipleIterations; iteration++)
            {
                FindAssoValues(keywords, ref assoValueMax, initialAssociationValue, jump, null, maxSelCharsLength, alphaSize, maxHashBase);

                int collisions = 0;
                int maxHash = int.MinValue;
                CollisionDetector.Clear();
                foreach (Keyword keyword in keywords)
                {
                    int hashcode = ComputeHash(keyword, Values, hashIncludesLength);
                    if (maxHash < hashcode)
                        maxHash = hashcode;
                    if (CollisionDetector.SetBit(hashcode))
                        collisions++;
                }

                if (collisions < bestCollisions || (collisions == bestCollisions && maxHash < bestMaxHash))
                {
                    Values.CopyTo(bestAssoValues, 0);
                    bestCollisions = collisions;
                    bestMaxHash = maxHash;
                }

                if (initialAssociationValue >= 2)
                {
                    initialAssociationValue -= 2;
                    jump += 2;
                }
                else
                {
                    initialAssociationValue += jump;
                    jump = 1;
                }
            }

            bestAssoValues.CopyTo(Values, 0);
        }

        private Random? CreateRandom()
        {
            if (!options.Random && options.Jump != 0)
                return null;

            return options.RandomSeed.HasValue ? new Random(options.RandomSeed.Value) : new Random();
        }

        private static int GetInitialAssociationValueMax(int keywordCount, double sizeMultiple)
        {
            double scaled = keywordCount * sizeMultiple;
            if (scaled > int.MaxValue)
                throw new InvalidOperationException("GPerf size multiple results in an association table that is too large.");

            int assoValueMax = (int)scaled;
            if (assoValueMax == 0)
                assoValueMax = 1;

            return RoundUpAssociationValueMax(assoValueMax);
        }

        private static int RoundUpAssociationValueMax(int assoValueMax)
        {
            if (assoValueMax >= 1 << 30)
                throw new InvalidOperationException("GPerf association table bound is too large.");

            assoValueMax |= assoValueMax >> 1;
            assoValueMax |= assoValueMax >> 2;
            assoValueMax |= assoValueMax >> 4;
            assoValueMax |= assoValueMax >> 8;
            assoValueMax |= assoValueMax >> 16;
            return assoValueMax + 1;
        }

        private static int ExpandAssociationValueMax(int assoValueMax)
        {
            if (assoValueMax >= 1 << 30)
                throw new InvalidOperationException("GPerf association table bound is too large.");

            return assoValueMax * 2;
        }

        private void ReinitializeHashBounds(int assoValueMax, int maxSelCharsLength, int maxHashBase)
        {
            long maxHash = maxHashBase + (((long)assoValueMax - 1) * maxSelCharsLength);
            if (maxHash >= int.MaxValue)
                throw new InvalidOperationException("GPerf hash range is too large for the collision detector.");

            _maxHash = (int)maxHash;

            // Allocate a sparse bit vector for detection of collisions of hash values.
            CollisionDetector = new BoolArray(_maxHash + 1, logger);
        }

        private void FindAssoValues(List<Keyword> keywords, ref int assoValueMax, int initialAssociationValue, int jump, Random? random, int maxSelCharsLength, int alphaSize, int maxHashBase)
        {
            Array.Clear(Values, 0, Values.Length);
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
                uint[] splitCardinalities = new uint[maxSelCharsLength + 1];
                for (uint c = 0; c < alphaSize; c++)
                {
                    if (Occurrences[c] > 0 && determined[c])
                    {
                        uint possible_collisions = CountPossibleCollisions(partition, c, maxSelCharsLength, splitCardinalities);
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
                            sb.AppendLine("  " + keyword.AllChars);
                    }
                    sb.AppendLine();
                }

                LogGPerfDebug(logger, sb.ToString());
            }
#endif

            uint stepNo = 0;
            for (Step? step = steps; step != null; step = step._next)
            {
                stepNo++;

                int k = step.ChangingCount;

                for (int i = 0; i < k; i++)
                {
                    uint c = step.Changing[i];
                    int value = random != null && options.Random ? random.Next() : initialAssociationValue;
                    Values[c] = value & (step.AssoValueMax - 1);
                }

                uint iterations = 0;
                int[] iter = new int[k];

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
                            int sum = hashIncludesLength ? keyword.Length : 0;
                            int len = keyword.SelCharsLength;
                            int offset = 0;
                            for (; len > 0; offset++, len--)
                            {
                                uint p = keyword.SelChars[offset];
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

                    if (jump != 0)
                    {
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
                            Values[c] = unchecked(Values[c] + jump) & (step.AssoValueMax - 1);
                            if (iter[i] <= bound)
                                goto foundNext;
                            Values[c] = unchecked(Values[c] - (iter[i] * jump)) & (step.AssoValueMax - 1);
                            iter[i] = 0;
                            i++;
                        }
                        i = ii + 1;
                        while (i < k)
                        {
                            uint c = step.Changing[i];
                            iter[i]++;
                            Values[c] = unchecked(Values[c] + jump) & (step.AssoValueMax - 1);
                            if (iter[i] < bound)
                                goto foundNext;
                            Values[c] = unchecked(Values[c] - (iter[i] * jump)) & (step.AssoValueMax - 1);
                            iter[i] = 0;
                            i++;
                        }

                        // Switch from one ii to the next.
                        {
                            uint c = step.Changing[ii];
                            Values[c] = unchecked(Values[c] - (bound * jump)) & (step.AssoValueMax - 1);
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
                                step.AssoValueMax = ExpandAssociationValueMax(step.AssoValueMax);
                                if (step.AssoValueMax > assoValueMax)
                                {
                                    assoValueMax = step.AssoValueMax;

                                    // Reinitialize MaxHash.
                                    ReinitializeHashBounds(assoValueMax, maxSelCharsLength, maxHashBase);
                                }
                            }
                        }
                        {
                            uint c = step.Changing[ii];
                            iter[ii] = bound;
                            Values[c] = unchecked(Values[c] + (bound * jump)) & (step.AssoValueMax - 1);
                        }
                        foundNext: ;
                    }
                    else
                    {
                        uint c = step.Changing[ii];
                        Values[c] = unchecked(Values[c] + random!.Next()) & (step.AssoValueMax - 1);
                        ii++;
                        if (ii == k)
                            ii = 0;
                    }
                }

                if (logger.IsEnabled(LogLevel.Trace))
                    LogGPerfDebug(logger, $"Step {stepNo} chose _asso_values[{string.Join(",", step.Changing.Select(x => "'" + (char)x + "'"))}] in {iterations} iterations.");
            }
        }

        private static EquivalenceClass ComputePartition(List<Keyword> keywords, bool[] undetermined)
        {
            EquivalenceClass? partition = null;
            EquivalenceClass? partitionLast = null;
            List<EquivalenceClass> classes = new List<EquivalenceClass>();

            foreach (Keyword keyword in keywords)
            {
                // Compute the undetermined characters for this keyword.
                uint[] undeterminedChars = new uint[keyword.SelCharsLength];
                int undeterminedCharsLength = 0;

                for (int i = 0; i < keyword.SelCharsLength; i++)
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
                    classes.Add(equClass);

                    // Console.WriteLine("un " + undeterminedCharsLength);
                    if (partition != null)
                        partitionLast.Next = equClass;
                    else
                        partition = equClass;

                    partitionLast = equClass;
                }

                // Add the keyword to the equivalence class.
                equClass.Keywords.Add(keyword);

                equClass.Cardinality++;
            }

            if (classes.Count <= 1)
                return partition!;

            classes.Sort(static (x, y) => y.Cardinality.CompareTo(x.Cardinality));
            for (int i = 0; i < classes.Count - 1; i++)
                classes[i].Next = classes[i + 1];

            classes[classes.Count - 1].Next = null;
            return classes[0];
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

        private static uint CountPossibleCollisions(EquivalenceClass partition, uint c, int maxSelCharsLength, uint[] splitCardinalities)
        {
            /* Every equivalence class p is split according to the frequency of occurrence of c, leading to equivalence classes p1, p2, ...
               This leads to   (|p|^2 - |p1|^2 - |p2|^2 - ...)/2  possible collisions.
               Return the sum of this expression over all equivalence classes.  */

            uint sum = 0;
            for (EquivalenceClass? cls = partition; cls != null; cls = cls.Next)
            {
                for (int i = 0; i <= maxSelCharsLength; i++)
                    splitCardinalities[i] = 0;

                foreach (Keyword keyword in cls.Keywords)
                {
                    int count = 0;
                    for (int i = 0; i < keyword.SelCharsLength; i++)
                    {
                        if (keyword.SelChars[i] == c)
                            count++;
                    }

                    splitCardinalities[count]++;
                }

                sum += cls.Cardinality * cls.Cardinality;
                for (int i = 0; i <= maxSelCharsLength; i++)
                    sum -= splitCardinalities[i] * splitCardinalities[i];
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
                    for (int i = 0; i < keyword.SelCharsLength; i++)
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
        private readonly byte[] _storageArray;
        private byte _iterationNumber = 1;

        public BoolArray(int size, ILogger logger)
        {
            _storageArray = new byte[size];
            if (logger.IsEnabled(LogLevel.Trace))
                LogGPerfDebug(logger, $"\nbool array size = {size}, total bytes = {size}");
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
               occurs once about every 2^8 iterations, so it does not take much time on
               average. */
            _iterationNumber = unchecked((byte)(_iterationNumber + 1));
            if (_iterationNumber == 0)
            {
                _iterationNumber = 1;
                Array.Clear(_storageArray, 0, _storageArray.Length);
            }
        }
    }

    private sealed class EquivalenceClass
    {
        // The keywords in this equivalence class.
        public readonly List<Keyword> Keywords = [];

        // The number of keywords in this equivalence class.
        public uint Cardinality;

        public EquivalenceClass? Next;

        // The undetermined selected characters for the keywords in this equivalence class, as a canonically reordered multiset.
        public uint[] UndeterminedChars = [];
        public int UndeterminedCharsLength;
    }

    private sealed class Step
    {
        public Step? _next;

        // Exclusive upper bound for the _asso_values[c] of this step. A power of 2.
        public int AssoValueMax;
        public uint[] Changing = [];

        // The characters whose values are being determined in this step.
        public int ChangingCount;

        // The keyword set partition after this step.
        public EquivalenceClass Partition = null!;

        // The characters whose values will be determined after this step.
        public bool[] Undetermined = [];

#if DEBUG

        // These two are only used for debug printing
        public double ExpectedLower;
        public double ExpectedUpper;
#endif
    }

    private sealed class Keyword(string allChars, byte[] bytes)
    {
        public string AllChars { get; } = allChars;
        public int Length => bytes.Length;
        public uint[] SelChars { get; private set; } = [];
        public int SelCharsLength { get; private set; }
        public int HashValue { get; set; }

        internal uint ByteAt(int position) => bytes[position];

        internal void InitSelCharsMultiset(SelectedPositions positions, int[] alphaInc, uint[]? alphaUnify)
        {
            int length = InitSelCharsLow(positions, alphaInc, alphaUnify);
            Array.Sort(SelChars, 0, length);
        }

        internal void InitSelCharsTuple(SelectedPositions positions, uint[]? alphaUnify)
        {
            InitSelCharsLow(positions, null, alphaUnify);
        }

        internal bool TryGetSelectedChar(int position, out uint c)
        {
            if (position == -1)
            {
                c = bytes[bytes.Length - 1];
                return true;
            }

            if (position < bytes.Length)
            {
                c = bytes[position];
                return true;
            }

            c = 0;
            return false;
        }

        private int InitSelCharsLow(SelectedPositions positions, int[]? alphaInc, uint[]? alphaUnify)
        {
            // The hash ultimately sums asso_values[selchar]. The selected characters are materialized as a multiset so
            // permutations can be detected and then disambiguated by alpha_inc.
            if (SelChars.Length < positions.Count)
                SelChars = new uint[positions.Count];

            int ptr = 0;
            int[] ascending = positions.Ascending;
            for (int p = 0; p < ascending.Length; p++)
            {
                int i = ascending[p];
                if (!TryGetSelectedChar(i, out uint c))
                    continue;

                if (alphaInc != null && i != -1)
                    c += (uint)alphaInc[i];

                if (alphaUnify != null)
                    c = alphaUnify[c];

                SelChars[ptr++] = c;
            }

            SelCharsLength = ptr;
            return ptr;
        }
    }

    private sealed class FastSet(int dataLength, bool ignoreCase, bool hashIncludesLength)
    {
        private readonly int[] _buckets = new int[dataLength];
        private readonly Entry[] _entries = new Entry[dataLength];
        private readonly int[] _usedBuckets = new int[dataLength];
        private int _count;
        private int _usedBucketCount;

        internal uint CountDups(List<Keyword> data, List<int> positions)
        {
            uint duplicates = 0;

            foreach (Keyword keyword in data)
            {
                if (!Add(keyword, positions))
                    duplicates++;
            }

            // Clear only buckets that were touched; entries are unreachable once buckets are reset.
            for (int i = 0; i < _usedBucketCount; i++)
                _buckets[_usedBuckets[i]] = 0;

            _count = 0;
            _usedBucketCount = 0;

            return duplicates;
        }

        private bool Add(Keyword value, List<int> positions)
        {
            ulong hashCode = HashKeyword(value, positions, ignoreCase, hashIncludesLength);
            int bucketIndex = (int)(hashCode % (ulong)_buckets.Length);
            ref int bucket = ref _buckets[bucketIndex];
            int i = bucket - 1;

            if (bucket == 0)
                _usedBuckets[_usedBucketCount++] = bucketIndex;

            while (i >= 0)
            {
                Entry entry = _entries[i];

                if (entry.Hash == hashCode && Equal(entry.Value, value, positions, ignoreCase, hashIncludesLength))
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

        private static bool Equal(Keyword a, Keyword b, List<int> positions, bool ignoreCase, bool hashIncludesLength)
        {
            if (hashIncludesLength && a.Length != b.Length)
                return false;

            foreach (int pos in positions)
            {
                bool hasA = a.TryGetSelectedChar(pos, out uint aChar);
                bool hasB = b.TryGetSelectedChar(pos, out uint bChar);

                if (hasA != hasB)
                    return false;
                if (hasA && !CharsEqual(aChar, bChar, ignoreCase))
                    return false;
            }

            return true;
        }

        private static uint HashKeyword(Keyword keyword, List<int> positions, bool ignoreCase, bool hashIncludesLength)
        {
            //This hash function is PJW hash

            uint h = hashIncludesLength ? (uint)keyword.Length : 0;
            foreach (int pos in positions)
            {
                if (!keyword.TryGetSelectedChar(pos, out uint c))
                    continue;

                if (ignoreCase)
                    c = ToLowerAscii(c);

                h = (h << 4) + c;

                uint high = h & 0xf0000000;

                if (high != 0)
                    h = h ^ (high >> 24) ^ high;
            }
            return h;
        }

        [StructLayout(LayoutKind.Auto)]
        private record struct Entry(ulong Hash, int Next, Keyword Value);
    }
}