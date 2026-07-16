using System.Numerics;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Generators.EarlyExits;

internal static class StringEarlyExits
{
    internal static IEarlyExit[] GetExits(StructureType structureType, StringKeyProperties props, EarlyExitConfig config, bool ignoreCase, uint itemCount)
    {
        if (config.Disabled)
            return [];

        if (!config.IsEnabledForStructure(structureType))
            return [];

        if (itemCount <= config.MinItemCount)
            return [];

        IEarlyExit[] candidates = ProduceCandidates(props, config, ignoreCase).ToArray();

        if (candidates.Length == 0)
            return [];

        float threshold = config.MinRejectionRatio;

        if (threshold > 0f)
            candidates = FilterByRejection(candidates, props, threshold).ToArray();

        if (candidates.Length == 0)
            return [];

        IEarlyExit[] exits = GetTopExits(candidates, props, config.MaxCandidates);
        return EnsureUnitAtLengthGuard(exits, props, config);
    }

    private static IEnumerable<IEarlyExit> ProduceCandidates(StringKeyProperties props, EarlyExitConfig config, bool ignoreCase)
    {
        // Length early exits are great for languages that provide constant time access to the length of a string (C#, Java, Python, Go, Rust)
        // For languages that has to iterate the string (C, Swift, Haskell), it is slower than comparing the first char.
        {
            DataRanges<int> ranges = props.LengthData.LengthRanges;
            int min = ranges.Min;
            int max = ranges.Max;

            if (config.IsEarlyExitEnabled(typeof(LengthNotEqualEarlyExit)) && min == max)
                yield return new LengthNotEqualEarlyExit(min);

            if (config.IsEarlyExitEnabled(typeof(LengthLessThanEarlyExit)) && min > 0)
                yield return new LengthLessThanEarlyExit(min);

            if (config.IsEarlyExitEnabled(typeof(LengthGreaterThanEarlyExit)) && max < int.MaxValue)
                yield return new LengthGreaterThanEarlyExit(max);

            if (config.IsEarlyExitEnabled(typeof(LengthInRangeEarlyExit)))
            {
                for (int i = 0; i < ranges.Ranges.Count - 1; i++)
                {
                    (int Start, int End) current = ranges.Ranges[i];
                    (int Start, int End) next = ranges.Ranges[i + 1];
                    yield return new LengthInRangeEarlyExit(current.End, next.Start);
                }
            }

            int bitCount = GetRangeCount(ranges);
            float density = (float)bitCount / ((max - min) + 1);
            if (config.IsEarlyExitEnabled(typeof(LengthBitmapEarlyExit)) && min >= 1 && max <= 64 && config.CheckDensityLimits(typeof(LengthBitmapEarlyExit), density))
            {
                ulong bitSet = BuildLengthBitmap(ranges);
                if (bitSet != 0)
                    yield return new LengthBitmapEarlyExit(bitSet);
            }
        }

        if (props.CharacterData.AllAscii)
        {
            AsciiMap firstMap = props.CharacterData.FirstCharMap;
            if (config.IsEarlyExitEnabled(typeof(UnitAtNotEqualEarlyExit)) && firstMap.BitCount == 1) //1 bit means all first units are the same
                yield return new UnitAtNotEqualEarlyExit(firstMap.Min, ignoreCase); //We can use min or max. They are the same.

            if (!ignoreCase)
            {
                if (config.IsEarlyExitEnabled(typeof(UnitAtLessThanEarlyExit)) && firstMap.Min > 0)
                    yield return new UnitAtLessThanEarlyExit(firstMap.Min);

                if (config.IsEarlyExitEnabled(typeof(UnitAtGreaterThanEarlyExit)) && firstMap.Max < char.MaxValue)
                    yield return new UnitAtGreaterThanEarlyExit(firstMap.Max);
            }

            if (config.IsEarlyExitEnabled(typeof(UnitAtBitmapEarlyExit)) && config.CheckDensityLimits(typeof(UnitAtBitmapEarlyExit), firstMap.Density))
                yield return new UnitAtBitmapEarlyExit(firstMap.Low, firstMap.High, ignoreCase);

            if (!ignoreCase && config.IsEarlyExitEnabled(typeof(UnitAtInRangeEarlyExit)))
            {
                foreach (IEarlyExit exit in GetUnitAtGaps(firstMap, 0))
                    yield return exit;
            }

            // Accessing the last char/byte is slow in languages that don't cache string length
            AsciiMap lastMap = props.CharacterData.LastCharMap;
            if (config.IsEarlyExitEnabled(typeof(UnitAtNotEqualEarlyExit)) && lastMap.BitCount == 1) //1 bit means all last units are the same
                yield return new UnitAtNotEqualEarlyExit(lastMap.Min, ignoreCase, -1); //We can use min or max. They are the same.

            if (!ignoreCase)
            {
                if (config.IsEarlyExitEnabled(typeof(UnitAtLessThanEarlyExit)) && lastMap.Min > 0)
                    yield return new UnitAtLessThanEarlyExit(lastMap.Min, -1);

                if (config.IsEarlyExitEnabled(typeof(UnitAtGreaterThanEarlyExit)) && lastMap.Max < char.MaxValue)
                    yield return new UnitAtGreaterThanEarlyExit(lastMap.Max, -1);
            }

            if (config.IsEarlyExitEnabled(typeof(UnitAtBitmapEarlyExit)) && config.CheckDensityLimits(typeof(UnitAtBitmapEarlyExit), lastMap.Density))
                yield return new UnitAtBitmapEarlyExit(lastMap.Low, lastMap.High, ignoreCase, -1);

            if (!ignoreCase && config.IsEarlyExitEnabled(typeof(UnitAtInRangeEarlyExit)))
            {
                foreach (IEarlyExit exit in GetUnitAtGaps(lastMap, -1))
                    yield return exit;
            }
        }
    }

    // Walks the observed-character bitmap and yields one UnitAtInRangeEarlyExit per gap between adjacent runs of observed characters.
    private static IEnumerable<IEarlyExit> GetUnitAtGaps(AsciiMap map, int offset)
    {
        (int Start, int End)? previous = null;

        foreach ((int Start, int End) range in GetSetRanges(map))
        {
            if (previous is { } prev)
                yield return new UnitAtInRangeEarlyExit((char)prev.End, (char)range.Start, offset);

            previous = range;
        }
    }

    // Returns contiguous runs of observed (set) characters in the map's bitmap, in ascending order.
    private static IEnumerable<(int Start, int End)> GetSetRanges(AsciiMap map)
    {
        if (map.BitCount == 0)
            yield break;

        int start = -1;

        for (int value = map.Min; value <= map.Max; value++)
        {
            bool set = value < 64 ? (map.Low & (1UL << value)) != 0 : (map.High & (1UL << (value - 64))) != 0;

            if (set)
                start = start == -1 ? value : start;
            else if (start != -1)
            {
                yield return (start, value - 1);
                start = -1;
            }
        }

        if (start != -1)
            yield return (start, map.Max);
    }

    private static IEarlyExit[] GetTopExits(IEarlyExit[] candidates, StringKeyProperties props, int maxCandidates)
    {
        if (maxCandidates <= 0 || candidates.Length == 0)
            return [];

        return candidates.OrderByDescending(x => GetScore(x, props)).Take(maxCandidates).ToArray();
    }

    private static IEarlyExit[] EnsureUnitAtLengthGuard(IEarlyExit[] exits, StringKeyProperties props, EarlyExitConfig config)
    {
        // This function ensures we have a length check BEFORE all others when a fixed-offset check (UnitAt/EqualsAt) is being used.

        int requiredLength = 0;
        foreach (IEarlyExit exit in exits)
            requiredLength = Math.Max(requiredLength, GetRequiredLength(exit));

        if (requiredLength == 0)
            return exits;

        IEarlyExit? guard = Array.Find(exits, x => GuardsLength(x, requiredLength));

        if (guard == null)
        {
            if (!config.IsEarlyExitEnabled(typeof(LengthLessThanEarlyExit)))
                return exits.Where(x => GetRequiredLength(x) == 0).ToArray();

            // The observed minimum is normally >= requiredLength already (the offset/fragment came from real keys),
            // but Max() keeps this correct even if that assumption is ever violated by a future candidate source.
            guard = new LengthLessThanEarlyExit(Math.Max(requiredLength, props.LengthData.LengthRanges.Min));
        }

        return exits.Prepend(guard).Distinct().ToArray();
    }

    // Returns the minimum string length a fixed-offset check needs to read safely, or 0 if the exit has no such requirement.
    private static int GetRequiredLength(IEarlyExit exit) => exit switch
    {
        UnitAtLessThanEarlyExit u => RequiredLengthForOffset(u.Offset),
        UnitAtGreaterThanEarlyExit u => RequiredLengthForOffset(u.Offset),
        UnitAtNotEqualEarlyExit u => RequiredLengthForOffset(u.Offset),
        UnitAtBitmapEarlyExit u => RequiredLengthForOffset(u.Offset),
        UnitAtInRangeEarlyExit u => RequiredLengthForOffset(u.Offset),
        EqualsAtEarlyExit e => e.Offset >= 0 ? e.Offset + e.Fragment.Length : -e.Offset,
        _ => 0
    };

    private static int RequiredLengthForOffset(int offset) => offset >= 0 ? offset + 1 : -offset;

    private static bool GuardsLength(IEarlyExit exit, int requiredLength) => exit switch
    {
        LengthLessThanEarlyExit lessThan => lessThan.Value >= requiredLength,
        LengthNotEqualEarlyExit notEqual => notEqual.Value >= requiredLength,
        _ => false
    };

    private static IEnumerable<IEarlyExit> FilterByRejection(IEarlyExit[] candidates, StringKeyProperties props, float threshold)
    {
        int minLength = props.LengthData.LengthRanges.Min;
        int maxLength = props.LengthData.LengthRanges.Max;
        double lengthSpan = maxLength >= minLength ? (maxLength - minLength) + 1d : 0d;
        double firstSpan = GetAsciiSpan(props.CharacterData.FirstCharMap);
        double lastSpan = GetAsciiSpan(props.CharacterData.LastCharMap);

        foreach (IEarlyExit exit in candidates)
        {
            double ratio = GetRejectionRatio(exit, lengthSpan, firstSpan, lastSpan);

            if (ratio >= threshold)
                yield return exit;
        }
    }

    private static double GetScore(IEarlyExit exit, StringKeyProperties props)
    {
        int minLength = props.LengthData.LengthRanges.Min;
        int maxLength = props.LengthData.LengthRanges.Max;
        double lengthSpan = maxLength >= minLength ? (maxLength - minLength) + 1d : 0d;
        double firstSpan = GetAsciiSpan(props.CharacterData.FirstCharMap);
        double lastSpan = GetAsciiSpan(props.CharacterData.LastCharMap);

        return GetRejectionRatio(exit, lengthSpan, firstSpan, lastSpan) / GetEstimatedCost(exit);
    }

    private static double GetRejectionRatio(IEarlyExit exit, double lengthSpan, double firstSpan, double lastSpan)
    {
        double span = GetObservedSpan(exit, lengthSpan, firstSpan, lastSpan);
        double ratio;

        if (exit is LengthBitmapEarlyExit lengthBitmap)
            ratio = span <= 0d ? 0d : (span - BitOperations.PopCount(lengthBitmap.BitSet)) / span;
        else if (exit is UnitAtBitmapEarlyExit unitBitmap)
            ratio = span <= 0d ? 0d : (span - BitOperations.PopCount(unitBitmap.Low) - BitOperations.PopCount(unitBitmap.High)) / span;
        else
            ratio = span <= 0d ? 0d : exit.KeyspaceSize / span;

        return ClampRatio(ratio);
    }

    private static double GetEstimatedCost(IEarlyExit exit) => exit switch
    {
        LengthLessThanEarlyExit or LengthGreaterThanEarlyExit or LengthNotEqualEarlyExit => 1d,
        LengthInRangeEarlyExit => 2d,
        LengthBitmapEarlyExit => 2d,
        UnitAtLessThanEarlyExit or UnitAtGreaterThanEarlyExit or UnitAtNotEqualEarlyExit => 2d,
        UnitAtInRangeEarlyExit => 3d,
        UnitAtBitmapEarlyExit => 4d,
        EqualsAtEarlyExit equalsAt => Math.Max(2d, equalsAt.Fragment.Length),
        _ => 1d
    };

    private static double GetObservedSpan(IEarlyExit exit, double lengthSpan, double firstSpan, double lastSpan)
    {
        if (exit is UnitAtLessThanEarlyExit e1)
            return e1.Offset >= 0 ? firstSpan : lastSpan;

        if (exit is UnitAtGreaterThanEarlyExit e2)
            return e2.Offset >= 0 ? firstSpan : lastSpan;

        if (exit is UnitAtNotEqualEarlyExit e3)
            return e3.Offset >= 0 ? firstSpan : lastSpan;

        if (exit is UnitAtBitmapEarlyExit e4)
            return e4.Offset >= 0 ? firstSpan : lastSpan;

        if (exit is UnitAtInRangeEarlyExit e5)
            return e5.Offset >= 0 ? firstSpan : lastSpan;

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

    private static ulong BuildLengthBitmap(DataRanges<int> ranges)
    {
        const int maxIndex = 64;
        ulong bitSet = 0;

        foreach ((int Start, int End) range in ranges.Ranges)
        {
            int start = range.Start;
            int end = range.End;

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

    private static double ClampRatio(double ratio)
    {
        if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio > 1d)
            return 1d;

        if (ratio < 0d)
            return 0d;

        return ratio;
    }
}