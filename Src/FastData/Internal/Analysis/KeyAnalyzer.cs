using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
internal static class KeyAnalyzer
{
    internal static NumericKeyProperties<T> GetNumericProperties<T>(ReadOnlyMemory<T> keys, bool keysAreSorted)
    {
        if (!keysAreSorted)
        {
            T[] sorted = new T[keys.Length];
            keys.Span.CopyTo(sorted);
            Array.Sort(sorted, Comparer<T>.Default);
            keys = sorted;
        }

        if (typeof(T) == typeof(char))
            return (NumericKeyProperties<T>)(object)GetCharProperties(((ReadOnlyMemory<char>)(object)keys).Span);
        if (typeof(T) == typeof(sbyte))
            return (NumericKeyProperties<T>)(object)GetSByteProperties(((ReadOnlyMemory<sbyte>)(object)keys).Span);
        if (typeof(T) == typeof(byte))
            return (NumericKeyProperties<T>)(object)GetByteProperties(((ReadOnlyMemory<byte>)(object)keys).Span);
        if (typeof(T) == typeof(short))
            return (NumericKeyProperties<T>)(object)GetInt16Properties(((ReadOnlyMemory<short>)(object)keys).Span);
        if (typeof(T) == typeof(ushort))
            return (NumericKeyProperties<T>)(object)GetUInt16Properties(((ReadOnlyMemory<ushort>)(object)keys).Span);
        if (typeof(T) == typeof(int))
            return (NumericKeyProperties<T>)(object)GetInt32Properties(((ReadOnlyMemory<int>)(object)keys).Span);
        if (typeof(T) == typeof(uint))
            return (NumericKeyProperties<T>)(object)GetUInt32Properties(((ReadOnlyMemory<uint>)(object)keys).Span);
        if (typeof(T) == typeof(long))
            return (NumericKeyProperties<T>)(object)GetInt64Properties(((ReadOnlyMemory<long>)(object)keys).Span);
        if (typeof(T) == typeof(ulong))
            return (NumericKeyProperties<T>)(object)GetUInt64Properties(((ReadOnlyMemory<ulong>)(object)keys).Span);
        if (typeof(T) == typeof(float))
            return (NumericKeyProperties<T>)(object)GetSingleProperties(((ReadOnlyMemory<float>)(object)keys).Span);
        if (typeof(T) == typeof(double))
            return (NumericKeyProperties<T>)(object)GetDoubleProperties(((ReadOnlyMemory<double>)(object)keys).Span);

        throw new InvalidOperationException($"Unsupported data type: {typeof(T).Name}");
    }

    internal static StringKeyProperties GetStringProperties(ReadOnlySpan<string> keys, bool enableTrimming, bool ignoreCase, GeneratorEncoding encoding)
    {
        // We need to create a length map for generators
        HashSet<int> uniqLengths = new HashSet<int>();

        // We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = keys[0];

        // We separately need to keep track of the smallest string, but only the length of it.
        int minLength = int.MaxValue;

        bool allAscii = true;
        CharacterClass charClass = CharacterClass.Unknown;
        AsciiMap firstCharMap = new AsciiMap();
        AsciiMap lastCharMap = new AsciiMap();

        // We wire up the delegates here to avoid branching in the foreach below

        // Get the generator-specific string length.
        Func<string, int> getLength = StringHelper.GetLengthFunc(encoding);
        Func<char, char> getChar = ignoreCase ? char.ToLowerInvariant : static c => c;

        for (int i = 0; i < keys.Length; i++)
        {
            string str = keys[i];
            if (str.Length > maxStr.Length)
                maxStr = str;

            minLength = Math.Min(minLength, str.Length);
            uniqLengths.Add(getLength(str));

            firstCharMap.Add(getChar(str[0]));
            lastCharMap.Add(getChar(str[str.Length - 1]));

            foreach (char c in str)
            {
                if (allAscii && (uint)c > '\x007f')
                    allAscii = false;

                if (char.IsLower(c))
                    charClass |= CharacterClass.Lowercase;
                else if (char.IsUpper(c))
                    charClass |= CharacterClass.Uppercase;
                else if (char.IsNumber(c))
                    charClass |= CharacterClass.Number;
                else if (char.IsSymbol(c))
                    charClass |= CharacterClass.Symbol;
                else if (char.IsWhiteSpace(c))
                    charClass |= CharacterClass.Whitespace;
            }
        }

        // The code beneath there calculate entropy maps that cna be used to derive the longest common substrings or longest prefix/suffix strings.
        // It works by adding characters to an accumulator, and then potentially removing the value from it again if the characters are the same.
        // If the accumulator for an offset contains 0 after all strings have been accumulated, it is highly likely that all the characters were the same.
        // However, there is a risk that an accumulator is 0, even if the characters are not the same. So we do a sanity check at the end to ensure we did it right.
        string prefix = string.Empty;
        string suffix = string.Empty;
        int[]? left = null;
        int[]? right = null;

        // Convert lengths to an array and sort it so we can make it into ranges.
        int[] lengthArr = uniqLengths.ToArray();
        Array.Sort(lengthArr);

        DataRanges<int> ranges = new DataRanges<int>(lengthArr.Length);
        int rangeStart = lengthArr[0];
        int previous = lengthArr[0];

        for (int i = 1; i < lengthArr.Length; i++)
        {
            int current = lengthArr[i];

            if (current - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = current;
            }

            previous = current;
        }

        ranges.Add(rangeStart, previous);

        // Prefix/suffix tracking only makes sense when there are multiple keys, and they are long enough
        if (enableTrimming && keys.Length > 1 && minLength > 1)
        {
            //Build a forward and reverse map of merged entropy
            //We can derive common prefix/suffix from it that can be used later for high-entropy hash/equality functions
            left = new int[maxStr.Length];
            right = new int[maxStr.Length];

            foreach (string str in keys)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    left[i] ^= getChar(str[i]);
                    right[i] ^= getChar(str[str.Length - 1 - i]);
                }
            }

            //Odd number of items. We need it to be even
            if (keys.Length % 2 != 0)
            {
                for (int i = 0; i < maxStr.Length; i++)
                {
                    //For best mixing, we take the longest string

                    left[i] ^= getChar(maxStr[i]);
                    right[i] ^= getChar(maxStr[maxStr.Length - 1 - i]);

                    //We do not add to characterMap here since it does not need the duplicate
                }
            }

            int leftCount = CountZero(left);
            int rightCount = CountZero(right);

            // Make sure that we handle the case where all characters in the inputs are the same
            if (leftCount == minLength || rightCount == minLength)
            {
                prefix = string.Empty;
                suffix = string.Empty;
            }
            else
            {
                prefix = keys[0].Substring(0, leftCount);
                suffix = keys[0].Substring(keys[0].Length - rightCount, rightCount);
            }
        }

        return new StringKeyProperties(new LengthData(keys.Length == uniqLengths.Count, ranges, minLength, maxStr.Length), new DeltaData(prefix, left, suffix, right), new CharacterData(allAscii, charClass, firstCharMap, lastCharMap));
    }

    private static NumericKeyProperties<char> GetCharProperties(ReadOnlySpan<char> keys)
    {
        ushort mask = 0;
        DataRanges<char> ranges = new DataRanges<char>(keys.Length);
        char rangeStart = keys[0];
        char previous = keys[0];
        mask |= previous;

        for (int i = 1; i < keys.Length; i++)
        {
            char val = keys[i];
            mask |= val;

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= char.MaxValue;
        ulong range = (ulong)(ranges.Max - ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<char>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<byte> GetByteProperties(ReadOnlySpan<byte> keys)
    {
        byte mask = 0;
        DataRanges<byte> ranges = new DataRanges<byte>(keys.Length);
        byte rangeStart = keys[0];
        byte previous = keys[0];
        mask |= previous;

        for (int i = 1; i < keys.Length; i++)
        {
            byte val = keys[i];
            mask |= val;

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= byte.MaxValue;
        ulong range = (ulong)(ranges.Max - ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<byte>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<sbyte> GetSByteProperties(ReadOnlySpan<sbyte> keys)
    {
        byte mask = 0;
        DataRanges<sbyte> ranges = new DataRanges<sbyte>(keys.Length);
        sbyte rangeStart = keys[0];
        sbyte previous = keys[0];
        mask |= unchecked((byte)previous);

        for (int i = 1; i < keys.Length; i++)
        {
            sbyte val = keys[i];
            mask |= unchecked((byte)val);

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= byte.MaxValue;
        ulong range = (ulong)(ranges.Max - ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<sbyte>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<short> GetInt16Properties(ReadOnlySpan<short> keys)
    {
        ushort mask = 0;
        DataRanges<short> ranges = new DataRanges<short>(keys.Length);
        short rangeStart = keys[0];
        short previous = keys[0];
        mask |= unchecked((ushort)previous);

        for (int i = 1; i < keys.Length; i++)
        {
            short val = keys[i];
            mask |= unchecked((ushort)val);

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= ushort.MaxValue;
        ulong range = (ulong)(ranges.Max - ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<short>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<ushort> GetUInt16Properties(ReadOnlySpan<ushort> keys)
    {
        ushort mask = 0;
        DataRanges<ushort> ranges = new DataRanges<ushort>(keys.Length);
        ushort rangeStart = keys[0];
        ushort previous = keys[0];
        mask |= previous;

        for (int i = 1; i < keys.Length; i++)
        {
            ushort val = keys[i];
            mask |= val;

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= ushort.MaxValue;
        ulong range = (ulong)(ranges.Max - ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<ushort>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<int> GetInt32Properties(ReadOnlySpan<int> keys)
    {
        uint mask = 0;
        DataRanges<int> ranges = new DataRanges<int>(keys.Length);
        int rangeStart = keys[0];
        int previous = keys[0];
        mask |= unchecked((uint)previous);

        for (int i = 1; i < keys.Length; i++)
        {
            int val = keys[i];
            mask |= unchecked((uint)val);

            if ((long)val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= uint.MaxValue;
        ulong range = (ulong)((long)ranges.Max - ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<int>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<uint> GetUInt32Properties(ReadOnlySpan<uint> keys)
    {
        uint mask = 0;
        DataRanges<uint> ranges = new DataRanges<uint>(keys.Length);
        uint rangeStart = keys[0];
        uint previous = keys[0];
        mask |= previous;

        for (int i = 1; i < keys.Length; i++)
        {
            uint val = keys[i];
            mask |= val;

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= uint.MaxValue;
        ulong range = ranges.Max - ranges.Min;
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<uint>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<long> GetInt64Properties(ReadOnlySpan<long> keys)
    {
        ulong mask = 0;
        DataRanges<long> ranges = new DataRanges<long>(keys.Length);
        long rangeStart = keys[0];
        long previous = keys[0];
        mask |= unchecked((ulong)previous);

        for (int i = 1; i < keys.Length; i++)
        {
            long val = keys[i];
            mask |= unchecked((ulong)val);

            if (previous < long.MaxValue && val > previous + 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= ulong.MaxValue;
        ulong range = unchecked((ulong)ranges.Max - (ulong)ranges.Min);
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<long>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<ulong> GetUInt64Properties(ReadOnlySpan<ulong> keys)
    {
        ulong mask = 0;
        DataRanges<ulong> ranges = new DataRanges<ulong>(keys.Length);
        ulong rangeStart = keys[0];
        ulong previous = keys[0];
        mask |= previous;

        for (int i = 1; i < keys.Length; i++)
        {
            ulong val = keys[i];
            mask |= val;

            if (val - previous > 1)
            {
                ranges.Add(rangeStart, previous);
                rangeStart = val;
            }

            previous = val;
        }

        ranges.Add(rangeStart, previous);

        mask ^= ulong.MaxValue;
        ulong range = ranges.Max - ranges.Min;
        bool isConsecutive = range == (ulong)(keys.Length - 1);
        return new NumericKeyProperties<ulong>(ranges, range, CalculateDensity(keys.Length, range), false, isConsecutive, mask);
    }

    private static NumericKeyProperties<float> GetSingleProperties(ReadOnlySpan<float> keys)
    {
        bool hasZero = false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == 0.0f) // Covers -0 and +0
            {
                hasZero = true;
                break;
            }
        }

        float min = keys[0];
        float max = keys[keys.Length - 1];

        DataRanges<float> ranges = new DataRanges<float>(min, max);
        float actualRange = max - min;
        ulong range = ClampRangeToUInt64(actualRange);

        return new NumericKeyProperties<float>(ranges, range, CalculateDensity(keys.Length, range), hasZero, false, 0);
    }

    private static NumericKeyProperties<double> GetDoubleProperties(ReadOnlySpan<double> keys)
    {
        bool hasZero = false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == 0.0d) // Covers -0 and +0
            {
                hasZero = true;
                break;
            }
        }

        double min = keys[0];
        double max = keys[keys.Length - 1];

        DataRanges<double> ranges = new DataRanges<double>(min, max);
        double actualRange = max - min;
        ulong range = ClampRangeToUInt64(actualRange);

        return new NumericKeyProperties<double>(ranges, range, CalculateDensity(keys.Length, range), hasZero, false, 0);
    }

    private static ulong ClampRangeToUInt64(double range) => range switch
    {
        <= 0.0d => 0,
        >= ulong.MaxValue => ulong.MaxValue,
        _ => (ulong)range
    };

    private static float CalculateDensity(int keyCount, ulong range) => keyCount / (range + 1.0f);

    private static int CountZero(int[] data)
    {
        int count;
        for (count = 0; count < data.Length; count++)
        {
            if (data[count] != 0)
                break;
        }
        return count;
    }
}