using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Generators.EarlyExits;

internal static class StringEarlyExits
{
    internal static IEarlyExit[] GetExits(Type structureType, StringKeyProperties props, EarlyExitConfig config, bool ignoreCase, int lengthOffset = 0, int firstCharOffset = 0, int lastCharOffset = 0)
    {
        IEarlyExit[] candidates = ProduceCandidates(structureType, props, config, ignoreCase, lengthOffset, firstCharOffset, lastCharOffset).ToArray();

        if (candidates.Length == 0)
            return [];

        float threshold = config.MinRejectionRatio;

        if (threshold > 0f)
            candidates = FilterByRejection(candidates, props, threshold, lengthOffset).ToArray();

        if (candidates.Length == 0)
            return [];

        return GetTopExits(candidates, config.MaxCandidates).ToArray();
    }

    private static IEnumerable<IEarlyExit> ProduceCandidates(Type structureType, StringKeyProperties props, EarlyExitConfig config, bool ignoreCase, int lengthOffset, int firstCharOffset, int lastCharOffset)
    {
        if (config.Disabled)
            yield break;

        if (!config.IsEnabledForStructure(structureType))
            yield break;

        // Length early exits are great for languages that provide constant time access to the length of a string (C#, Java, Python, Go, Rust)
        // For languages that has to iterate the string (C, Swift, Haskell), it is slower than comparing the first char.
        {
            DataRanges<int> ranges = props.LengthData.LengthRanges;
            int min = ranges.Min + lengthOffset;
            int max = ranges.Max + lengthOffset;

            if (config.IsEarlyExitEnabled(typeof(LengthNotEqualEarlyExit)) && min == max)
                yield return new LengthNotEqualEarlyExit(min);

            if (config.IsEarlyExitEnabled(typeof(LengthLessThanEarlyExit)) && min > 0)
                yield return new LengthLessThanEarlyExit(min);

            if (config.IsEarlyExitEnabled(typeof(LengthGreaterThanEarlyExit)) && max < int.MaxValue)
                yield return new LengthGreaterThanEarlyExit(max);

            for (int i = 0; i < ranges.Ranges.Count - 1; i++)
            {
                (int Start, int End) current = ranges.Ranges[i];
                (int Start, int End) next = ranges.Ranges[i + 1];
                yield return new StringLengthRangeEarlyExit(current.End + lengthOffset, next.Start + lengthOffset);
            }

            int bitCount = GetRangeCount(ranges);
            float density = (float)bitCount / ((max - min) + 1);
            if (config.IsEarlyExitEnabled(typeof(LengthBitmapEarlyExit)) && config.CheckDensityLimits(typeof(LengthBitmapEarlyExit), density))
            {
                ulong bitSet = BuildLengthBitmap(ranges, lengthOffset);
                if (bitSet != 0)
                    yield return new LengthBitmapEarlyExit(bitSet);
            }
        }

        {
            // Accessing a char by index in a string is always constant time.
            AsciiMap firstMap = props.CharacterData.FirstCharMap;
            if (config.IsEarlyExitEnabled(typeof(CharFirstNotEqualEarlyExit)) && firstMap.BitCount == 1) //1 bit means all first characters are the same
                yield return new CharFirstNotEqualEarlyExit(firstMap.Min, ignoreCase, firstCharOffset); //We can use min or max. They are the same.

            if (config.IsEarlyExitEnabled(typeof(CharFirstLessThanEarlyExit)) && firstMap.Min > 0)
                yield return new CharFirstLessThanEarlyExit(firstMap.Min, firstCharOffset);

            if (config.IsEarlyExitEnabled(typeof(CharFirstGreaterThanEarlyExit)) && firstMap.Max < char.MaxValue)
                yield return new CharFirstGreaterThanEarlyExit(firstMap.Max, firstCharOffset);

            if (config.IsEarlyExitEnabled(typeof(CharFirstBitmapEarlyExit)) && config.CheckDensityLimits(typeof(CharFirstBitmapEarlyExit), firstMap.Density))
                yield return new CharFirstBitmapEarlyExit(firstMap.Low, firstMap.High, ignoreCase, firstCharOffset);

            // Accessing the last char/byte is slow in languages that don't cache string length
            AsciiMap lastMap = props.CharacterData.LastCharMap;
            if (config.IsEarlyExitEnabled(typeof(CharLastNotEqualEarlyExit)) && lastMap.BitCount == 1) //1 bit means all last characters are the same
                yield return new CharLastNotEqualEarlyExit(lastMap.Min, ignoreCase, lastCharOffset); //We can use min or max. They are the same.

            if (config.IsEarlyExitEnabled(typeof(CharLastLessThanEarlyExit)) && lastMap.Min > 0)
                yield return new CharLastLessThanEarlyExit(lastMap.Min, lastCharOffset);

            if (config.IsEarlyExitEnabled(typeof(CharLastGreaterThanEarlyExit)) && lastMap.Max < char.MaxValue)
                yield return new CharLastGreaterThanEarlyExit(lastMap.Max, lastCharOffset);

            if (config.IsEarlyExitEnabled(typeof(CharLastBitmapEarlyExit)) && config.CheckDensityLimits(typeof(CharLastBitmapEarlyExit), lastMap.Density))
                yield return new CharLastBitmapEarlyExit(lastMap.Low, lastMap.High, ignoreCase, lastCharOffset);
        }

        // If prefix/suffix trimming is disabled, we can use them as early exits instead.
        {
            if (config.IsEarlyExitEnabled(typeof(StringPrefixEarlyExit)) && props.DeltaData.Prefix.Length != 0)
                yield return new StringPrefixEarlyExit(props.DeltaData.Prefix, ignoreCase);

            if (config.IsEarlyExitEnabled(typeof(StringSuffixEarlyExit)) && props.DeltaData.Suffix.Length != 0)
                yield return new StringSuffixEarlyExit(props.DeltaData.Suffix, ignoreCase);
        }
    }

    private static IEnumerable<IEarlyExit> GetTopExits(IEarlyExit[] candidates, int maxCandidates)
    {
        if (maxCandidates <= 0 || candidates.Length == 0)
            return [];

        return candidates.OrderByDescending(x => x.KeyspaceSize).Take(maxCandidates);
    }

    private static IEnumerable<IEarlyExit> FilterByRejection(IEarlyExit[] candidates, StringKeyProperties props, float threshold, int lengthOffset)
    {
        int minLength = props.LengthData.LengthRanges.Min + lengthOffset;
        int maxLength = props.LengthData.LengthRanges.Max + lengthOffset;
        double lengthSpan = maxLength >= minLength ? (maxLength - minLength) + 1d : 0d;
        double firstSpan = GetAsciiSpan(props.CharacterData.FirstCharMap);
        double lastSpan = GetAsciiSpan(props.CharacterData.LastCharMap);

        foreach (IEarlyExit exit in candidates)
        {
            double span = GetObservedSpan(exit, lengthSpan, firstSpan, lastSpan);
            double ratio = span <= 0d ? 0d : exit.KeyspaceSize / span;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio > 1d)
                ratio = 1d;
            else if (ratio < 0d)
                ratio = 0d;

            if (ratio >= threshold)
                yield return exit;
        }
    }

    private static double GetObservedSpan(IEarlyExit exit, double lengthSpan, double firstSpan, double lastSpan)
    {
        if (exit is CharFirstLessThanEarlyExit or CharFirstGreaterThanEarlyExit or CharFirstNotEqualEarlyExit or CharFirstBitmapEarlyExit)
            return firstSpan;

        if (exit is CharLastLessThanEarlyExit or CharLastGreaterThanEarlyExit or CharLastNotEqualEarlyExit or CharLastBitmapEarlyExit)
            return lastSpan;

        return lengthSpan;
    }

    private static double GetAsciiSpan(AsciiMap map)
    {
        if (map.BitCount == 0 || map.Max < map.Min)
            return 0d;

        return (map.Max - map.Min) + 1d;
    }

    private static int GetRangeCount(DataRanges<int> ranges)
    {
        int count = 0;

        foreach ((int Start, int End) range in ranges.Ranges)
            count += (range.End - range.Start) + 1;

        return count;
    }

    private static ulong BuildLengthBitmap(DataRanges<int> ranges, int offset)
    {
        const int maxIndex = 63;
        ulong bitSet = 0;

        foreach ((int Start, int End) range in ranges.Ranges)
        {
            int start = range.Start + offset;
            int end = range.End + offset;

            if (start > maxIndex)
                continue;

            end = Math.Min(end, maxIndex);

            for (int value = start; value <= end; value++)
            {
                int bitIndex = (value - 1) & 63;
                bitSet |= 1UL << bitIndex;
            }
        }

        return bitSet;
    }
}