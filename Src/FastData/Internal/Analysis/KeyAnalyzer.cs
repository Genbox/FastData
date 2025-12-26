using System.Text;
using Genbox.FastData.Generators;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis;

internal static class KeyAnalyzer
{
    internal static NumericKeyProperties<T> GetNumericProperties<T>(ReadOnlyMemory<T> keys)
    {
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

    internal static StringKeyProperties GetStringProperties(ReadOnlySpan<string> keys, bool enableTrimming)
    {
        //Contains a map of unique lengths
        LengthBitArray lengthMap = new LengthBitArray();

        //We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = keys[0];
        int minUtf8ByteLength = int.MaxValue;
        int maxUtf8ByteLength = int.MinValue;
        int minUtf16ByteLength = int.MaxValue;
        int maxUtf16ByteLength = int.MinValue;
        bool uniqLen = true;
        bool allAscii = true;
        CharacterClass charClass = CharacterClass.Unknown;

        foreach (string str in keys)
        {
            if (str.Length > maxStr.Length)
                maxStr = str;

            int utf8ByteCount = Encoding.UTF8.GetByteCount(str);
            minUtf8ByteLength = Math.Min(utf8ByteCount, minUtf8ByteLength);
            maxUtf8ByteLength = Math.Max(utf8ByteCount, maxUtf8ByteLength);

            int utf16ByteCount = Encoding.Unicode.GetByteCount(str);
            minUtf16ByteLength = Math.Min(utf16ByteCount, minUtf16ByteLength);
            maxUtf16ByteLength = Math.Max(utf16ByteCount, maxUtf16ByteLength);

            uniqLen &= !lengthMap.SetTrue((uint)str.Length);

            foreach (char c in str)
            {
                if ((uint)c > '\x007f')
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

        // int[]? map = null;
        int[]? left = null;
        int[]? right = null;

        // Prefix/suffix tracking only makes sense when there are multiple keys, and they are long enough
        if (enableTrimming && keys.Length > 1 && lengthMap.Min > 1)
        {
            //Build a forward and reverse map of merged entropy
            //We can derive common prefix/suffix from it that can be used later for high-entropy hash/equality functions
            left = new int[maxStr.Length];
            right = new int[maxStr.Length];

            foreach (string str in keys)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    char lc = str[i];
                    char rc = str[str.Length - 1 - i];

                    left[i] ^= lc;
                    right[i] ^= rc;
                }
            }

            //Odd number of items. We need it to be even
            if (keys.Length % 2 != 0)
            {
                for (int i = 0; i < maxStr.Length; i++)
                {
                    //For best mixing, we take the longest string
                    char lc = maxStr[i];
                    char rc = maxStr[maxStr.Length - 1 - i];

                    left[i] ^= lc;
                    right[i] ^= rc;

                    //We do not add to characterMap here since it does not need the duplicate
                }
            }

            // Make sure that we handle the case where all characters in the inputs are the same
            if (DeltaData.CountZero(left) == lengthMap.Min || DeltaData.CountZero(right) == lengthMap.Min)
            {
                left = null;
                right = null;
            }
        }

        return new StringKeyProperties(new LengthData((uint)minUtf8ByteLength, (uint)maxUtf8ByteLength, (uint)minUtf16ByteLength, (uint)maxUtf16ByteLength, uniqLen, lengthMap), new DeltaData(left, right), new CharacterData(allAscii, charClass));
    }

    private static NumericKeyProperties<char> GetCharProperties(ReadOnlySpan<char> keys)
    {
        char min = char.MaxValue;
        char max = char.MinValue;
        ushort mask = 0;

        foreach (char c in keys)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
            mask |= c;
        }

        return new NumericKeyProperties<char>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1, mask);
    }

    private static NumericKeyProperties<float> GetSingleProperties(ReadOnlySpan<float> keys)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        bool hasZeroOrNaN = false;
        bool hasNaNOrInfinity = false;

        foreach (float c in keys)
        {
#pragma warning disable S1244
            if (!hasZeroOrNaN && (float.IsNaN(c) || c == 0.0f))
#pragma warning restore S1244
                hasZeroOrNaN = true;

            if (!hasNaNOrInfinity && (float.IsNaN(c) || float.IsInfinity(c)))
                hasNaNOrInfinity = true;

            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        ulong range = ClampRangeToUInt64(max - min);
        return new NumericKeyProperties<float>(min, max, range, hasZeroOrNaN, IsFloatConsecutive(keys, min, max, hasNaNOrInfinity), 0);
    }

    private static NumericKeyProperties<double> GetDoubleProperties(ReadOnlySpan<double> keys)
    {
        double min = double.MaxValue;
        double max = double.MinValue;

        bool hasZeroOrNaN = false;
        bool hasNaNOrInfinity = false;

        foreach (double c in keys)
        {
#pragma warning disable S1244
            if (!hasZeroOrNaN && (double.IsNaN(c) || c == 0.0d))
#pragma warning restore S1244
                hasZeroOrNaN = true;

            if (!hasNaNOrInfinity && (double.IsNaN(c) || double.IsInfinity(c)))
                hasNaNOrInfinity = true;

            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        ulong range = ClampRangeToUInt64(max - min);
        return new NumericKeyProperties<double>(min, max, range, hasZeroOrNaN, IsDoubleConsecutive(keys, min, max, hasNaNOrInfinity), 0);
    }

    private static NumericKeyProperties<byte> GetByteProperties(ReadOnlySpan<byte> keys)
    {
        byte min = byte.MaxValue;
        byte max = byte.MinValue;
        byte mask = 0;

        foreach (byte val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= val;
        }

        return new NumericKeyProperties<byte>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1, mask);
    }

    private static NumericKeyProperties<sbyte> GetSByteProperties(ReadOnlySpan<sbyte> keys)
    {
        sbyte min = sbyte.MaxValue;
        sbyte max = sbyte.MinValue;
        byte mask = 0;

        foreach (sbyte val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= unchecked((byte)val);
        }

        return new NumericKeyProperties<sbyte>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1, mask);
    }

    private static NumericKeyProperties<short> GetInt16Properties(ReadOnlySpan<short> keys)
    {
        short min = short.MaxValue;
        short max = short.MinValue;
        ushort mask = 0;

        foreach (short val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= unchecked((ushort)val);
        }

        return new NumericKeyProperties<short>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1, mask);
    }

    private static NumericKeyProperties<ushort> GetUInt16Properties(ReadOnlySpan<ushort> keys)
    {
        ushort min = ushort.MaxValue;
        ushort max = ushort.MinValue;
        ushort mask = 0;

        foreach (ushort val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= val;
        }

        return new NumericKeyProperties<ushort>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1, mask);
    }

    private static NumericKeyProperties<int> GetInt32Properties(ReadOnlySpan<int> keys)
    {
        int min = int.MaxValue;
        int max = int.MinValue;
        uint mask = 0;

        foreach (int val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= unchecked((uint)val);
        }

        return new NumericKeyProperties<int>(min, max, (ulong)((long)max - min), false, keys.Length <= 1 || (long)max - min == keys.Length - 1, mask);
    }

    private static NumericKeyProperties<uint> GetUInt32Properties(ReadOnlySpan<uint> keys)
    {
        uint min = uint.MaxValue;
        uint max = uint.MinValue;
        uint mask = 0;

        foreach (uint val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= val;
        }

        return new NumericKeyProperties<uint>(min, max, max - min, false, keys.Length <= 1 || (ulong)max - min == (ulong)(keys.Length - 1), mask);
    }

    private static NumericKeyProperties<long> GetInt64Properties(ReadOnlySpan<long> keys)
    {
        long min = long.MaxValue;
        long max = long.MinValue;
        ulong mask = 0;

        foreach (long val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= unchecked((ulong)val);
        }

        ulong range = unchecked((ulong)max - (ulong)min);
        return new NumericKeyProperties<long>(min, max, range, false, keys.Length <= 1 || range == (ulong)(keys.Length - 1), mask);
    }

    private static NumericKeyProperties<ulong> GetUInt64Properties(ReadOnlySpan<ulong> keys)
    {
        ulong min = ulong.MaxValue;
        ulong max = ulong.MinValue;
        ulong mask = 0;

        foreach (ulong val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
            mask |= val;
        }

        return new NumericKeyProperties<ulong>(min, max, max - min, false, keys.Length <= 1 || max - min == (ulong)(keys.Length - 1), mask);
    }

    private static bool IsFloatConsecutive(ReadOnlySpan<float> keys, float min, float max, bool hasNaNOrInfinity)
    {
        if (hasNaNOrInfinity)
            return false;

        if (keys.Length <= 1)
            return true;

        float expectedRange = keys.Length - 1;
        float range = max - min;

        if (Math.Abs(range - expectedRange) > float.Epsilon)
            return false;

        float[] sorted = new float[keys.Length];
        keys.CopyTo(sorted);
        Array.Sort(sorted);

        for (int i = 1; i < sorted.Length; i++)
        {
            if (Math.Abs(sorted[i] - (sorted[i - 1] + 1.0f)) > float.Epsilon)
                return false;
        }

        return true;
    }

    private static bool IsDoubleConsecutive(ReadOnlySpan<double> keys, double min, double max, bool hasNaNOrInfinity)
    {
        if (hasNaNOrInfinity)
            return false;

        if (keys.Length <= 1)
            return true;

        double expectedRange = keys.Length - 1;
        double range = max - min;

        if (Math.Abs(range - expectedRange) > double.Epsilon)
            return false;

        double[] sorted = new double[keys.Length];
        keys.CopyTo(sorted);
        Array.Sort(sorted);

        for (int i = 1; i < sorted.Length; i++)
        {
            if (Math.Abs(sorted[i] - (sorted[i - 1] + 1.0d)) > double.Epsilon)
                return false;
        }

        return true;
    }

    private static ulong ClampRangeToUInt64(double range)
    {
        if (double.IsNaN(range) || range <= 0.0d)
            return 0;

        if (range >= ulong.MaxValue)
            return ulong.MaxValue;

        return (ulong)range;
    }
}