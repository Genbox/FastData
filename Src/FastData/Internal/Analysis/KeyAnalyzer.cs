using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
internal static class KeyAnalyzer
{
    internal static NumericKeyProperties<T> GetNumericProperties<T>(ReadOnlyMemory<T> keys, bool keysAreSorted)
    {
        if (typeof(T) == typeof(char))
            return (NumericKeyProperties<T>)(object)GetCharProperties(((ReadOnlyMemory<char>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(sbyte))
            return (NumericKeyProperties<T>)(object)GetSByteProperties(((ReadOnlyMemory<sbyte>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(byte))
            return (NumericKeyProperties<T>)(object)GetByteProperties(((ReadOnlyMemory<byte>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(short))
            return (NumericKeyProperties<T>)(object)GetInt16Properties(((ReadOnlyMemory<short>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(ushort))
            return (NumericKeyProperties<T>)(object)GetUInt16Properties(((ReadOnlyMemory<ushort>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(int))
            return (NumericKeyProperties<T>)(object)GetInt32Properties(((ReadOnlyMemory<int>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(uint))
            return (NumericKeyProperties<T>)(object)GetUInt32Properties(((ReadOnlyMemory<uint>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(long))
            return (NumericKeyProperties<T>)(object)GetInt64Properties(((ReadOnlyMemory<long>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(ulong))
            return (NumericKeyProperties<T>)(object)GetUInt64Properties(((ReadOnlyMemory<ulong>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(float))
            return (NumericKeyProperties<T>)(object)GetSingleProperties(((ReadOnlyMemory<float>)(object)keys).Span, keysAreSorted);
        if (typeof(T) == typeof(double))
            return (NumericKeyProperties<T>)(object)GetDoubleProperties(((ReadOnlyMemory<double>)(object)keys).Span, keysAreSorted);

        throw new InvalidOperationException($"Unsupported data type: {typeof(T).Name}");
    }

    internal static StringKeyProperties GetStringProperties(ReadOnlySpan<string> keys, bool enableTrimming, bool ignoreCase, GeneratorEncoding encoding)
    {
        //Contains a map of unique lengths
        LengthBitArray lengthMap = new LengthBitArray();

        //We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = keys[0];
        int minByteCount = int.MaxValue;
        int maxByteCount = int.MinValue;
        bool uniqLen = true;
        bool allAscii = true;
        CharacterClass charClass = CharacterClass.Unknown;
        AsciiMap firstCharMap = new AsciiMap();
        AsciiMap lastCharMap = new AsciiMap();
        uint lengthGcd = 0;
        uint byteGcd = 0;

        foreach (string str in keys)
        {
            if (str.Length > maxStr.Length)
                maxStr = str;

            //TODO: Remove branch by rewriting delegate
            int byteCount = encoding switch
            {
                GeneratorEncoding.ASCII => Encoding.ASCII.GetByteCount(str),
                GeneratorEncoding.UTF8 => Encoding.UTF8.GetByteCount(str),
                GeneratorEncoding.UTF16 => Encoding.Unicode.GetByteCount(str),
                GeneratorEncoding.UTF32 => Encoding.UTF32.GetByteCount(str),
                _ => throw new InvalidOperationException($"Unsupported encoding: {encoding}")
            };

            minByteCount = Math.Min(byteCount, minByteCount);
            maxByteCount = Math.Max(byteCount, maxByteCount);

            int length = str.Length;
            uniqLen &= !lengthMap.SetTrue((uint)length);
            lengthGcd = UpdateGcd(lengthGcd, (uint)length);
            byteGcd = UpdateGcd(byteGcd, (uint)byteCount);

            // Code under here is for first/last char analysis
            char firstChar = str[0];
            char lastChar = str[str.Length - 1];

            if (ignoreCase)
            {
                firstChar = char.ToLowerInvariant(firstChar);
                lastChar = char.ToLowerInvariant(lastChar);
            }

            firstCharMap.Add(firstChar);
            lastCharMap.Add(lastChar);

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

        byte stringBitMaskLen = (byte)Math.Min(minByteCount, 8); // Make up to 8 bytes mask, but no longer than the smallest string for perf.
        ulong stringBitMask = GetStringBitMask(keys, encoding, ignoreCase, stringBitMaskLen);

        // The code beneath there calculate entropy maps that cna be used to derive the longest common substrings or longest prefix/suffix strings.
        // It works by adding characters to an accumulator, and then potentially removing the value from it again if the characters are the same.
        // If the accumulator for an offset contains 0 after all strings have been accumulated, it is highly likely that all the characters were the same.
        // However, there is a risk that an accumulator is 0, even if the characters are not the same. So we do a sanity check at the end to ensure we did it right.
        string prefix = string.Empty;
        string suffix = string.Empty;
        int[]? left = null;
        int[]? right = null;

        // Prefix/suffix tracking only makes sense when there are multiple keys, and they are long enough, but not too long!
        if (enableTrimming && keys.Length > 1 && lengthMap.Min > 1 && maxStr.Length <= ushort.MaxValue)
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

            int leftCount = CountZero(left);
            int rightCount = CountZero(right);

            // Make sure that we handle the case where all characters in the inputs are the same
            if (leftCount == lengthMap.Min || rightCount == lengthMap.Min)
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

        uint charDivisor = lengthGcd <= 1 ? 0u : lengthGcd;
        uint byteDivisor = byteGcd <= 1 ? 0u : byteGcd;

        if (charDivisor == 0)
            byteDivisor = 0;

        return new StringKeyProperties(new LengthData((uint)minByteCount, (uint)maxByteCount, uniqLen, lengthMap, charDivisor, byteDivisor), new DeltaData(prefix, left, suffix, right), new CharacterData(allAscii, charClass, stringBitMask, stringBitMaskLen, firstCharMap, lastCharMap));
    }

    private static NumericKeyProperties<char> GetCharProperties(ReadOnlySpan<char> keys, bool keysAreSorted)
    {
        char min = keysAreSorted ? keys[0] : char.MaxValue;
        char max = keysAreSorted ? keys[keys.Length - 1] : char.MinValue;
        ushort mask = 0;

        if (keysAreSorted)
        {
            foreach (char c in keys)
                mask |= c;
        }
        else
        {
            foreach (char c in keys)
            {
                min = c < min ? c : min;
                max = c > max ? c : max;
                mask |= c;
            }
        }

        ulong range = (ulong)(max - min);
        return new NumericKeyProperties<char>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || max - min == keys.Length - 1, mask, static v => v);
    }

    private static NumericKeyProperties<float> GetSingleProperties(ReadOnlySpan<float> keys, bool keysAreSorted)
    {
        float min = keysAreSorted ? keys[0] : float.MaxValue;
        float max = keysAreSorted ? keys[keys.Length - 1] : float.MinValue;

        bool hasZeroOrNaN = false;
        bool hasNaNOrInfinity = false;

        if (keysAreSorted)
        {
            foreach (float c in keys)
            {
                if (!hasZeroOrNaN && (float.IsNaN(c) || c == 0.0f))
                    hasZeroOrNaN = true;

                if (!hasNaNOrInfinity && (float.IsNaN(c) || float.IsInfinity(c)))
                    hasNaNOrInfinity = true;
            }
        }
        else
        {
            foreach (float c in keys)
            {
                if (!hasZeroOrNaN && (float.IsNaN(c) || c == 0.0f))
                    hasZeroOrNaN = true;

                if (!hasNaNOrInfinity && (float.IsNaN(c) || float.IsInfinity(c)))
                    hasNaNOrInfinity = true;

                min = c < min ? c : min;
                max = c > max ? c : max;
            }
        }

        ulong range = ClampRangeToUInt64(max - min);
        return new NumericKeyProperties<float>(min, max, range, CalculateDensity(keys.Length, range), hasZeroOrNaN, IsFloatConsecutive(keys, min, max, hasNaNOrInfinity, keysAreSorted), 0, static v => (long)v);
    }

    private static NumericKeyProperties<double> GetDoubleProperties(ReadOnlySpan<double> keys, bool keysAreSorted)
    {
        double min = keysAreSorted ? keys[0] : double.MaxValue;
        double max = keysAreSorted ? keys[keys.Length - 1] : double.MinValue;

        bool hasZeroOrNaN = false;
        bool hasNaNOrInfinity = false;

        if (keysAreSorted)
        {
            foreach (double c in keys)
            {
                if (!hasZeroOrNaN && (double.IsNaN(c) || c == 0.0d))
                    hasZeroOrNaN = true;

                if (!hasNaNOrInfinity && (double.IsNaN(c) || double.IsInfinity(c)))
                    hasNaNOrInfinity = true;
            }
        }
        else
        {
            foreach (double c in keys)
            {
                if (!hasZeroOrNaN && (double.IsNaN(c) || c == 0.0d))
                    hasZeroOrNaN = true;

                if (!hasNaNOrInfinity && (double.IsNaN(c) || double.IsInfinity(c)))
                    hasNaNOrInfinity = true;

                min = c < min ? c : min;
                max = c > max ? c : max;
            }
        }

        ulong range = ClampRangeToUInt64(max - min);
        return new NumericKeyProperties<double>(min, max, range, CalculateDensity(keys.Length, range), hasZeroOrNaN, IsDoubleConsecutive(keys, min, max, hasNaNOrInfinity, keysAreSorted), 0, static v => (long)v);
    }

    private static NumericKeyProperties<byte> GetByteProperties(ReadOnlySpan<byte> keys, bool keysAreSorted)
    {
        byte min = keysAreSorted ? keys[0] : byte.MaxValue;
        byte max = keysAreSorted ? keys[keys.Length - 1] : byte.MinValue;
        byte mask = 0;

        if (keysAreSorted)
        {
            foreach (byte val in keys)
                mask |= val;
        }
        else
        {
            foreach (byte val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= val;
            }
        }

        ulong range = (ulong)(max - min);
        return new NumericKeyProperties<byte>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || max - min == keys.Length - 1, mask, static v => v);
    }

    private static NumericKeyProperties<sbyte> GetSByteProperties(ReadOnlySpan<sbyte> keys, bool keysAreSorted)
    {
        sbyte min = keysAreSorted ? keys[0] : sbyte.MaxValue;
        sbyte max = keysAreSorted ? keys[keys.Length - 1] : sbyte.MinValue;
        byte mask = 0;

        if (keysAreSorted)
        {
            foreach (sbyte val in keys)
                mask |= unchecked((byte)val);
        }
        else
        {
            foreach (sbyte val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= unchecked((byte)val);
            }
        }

        ulong range = (ulong)(max - min);
        return new NumericKeyProperties<sbyte>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || max - min == keys.Length - 1, mask, static v => v);
    }

    private static NumericKeyProperties<short> GetInt16Properties(ReadOnlySpan<short> keys, bool keysAreSorted)
    {
        short min = keysAreSorted ? keys[0] : short.MaxValue;
        short max = keysAreSorted ? keys[keys.Length - 1] : short.MinValue;
        ushort mask = 0;

        if (keysAreSorted)
        {
            foreach (short val in keys)
                mask |= unchecked((ushort)val);
        }
        else
        {
            foreach (short val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= unchecked((ushort)val);
            }
        }

        ulong range = (ulong)(max - min);
        return new NumericKeyProperties<short>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || max - min == keys.Length - 1, mask, static v => v);
    }

    private static NumericKeyProperties<ushort> GetUInt16Properties(ReadOnlySpan<ushort> keys, bool keysAreSorted)
    {
        ushort min = keysAreSorted ? keys[0] : ushort.MaxValue;
        ushort max = keysAreSorted ? keys[keys.Length - 1] : ushort.MinValue;
        ushort mask = 0;

        if (keysAreSorted)
        {
            foreach (ushort val in keys)
                mask |= val;
        }
        else
        {
            foreach (ushort val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= val;
            }
        }

        ulong range = (ulong)(max - min);
        return new NumericKeyProperties<ushort>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || max - min == keys.Length - 1, mask, static v => v);
    }

    private static NumericKeyProperties<int> GetInt32Properties(ReadOnlySpan<int> keys, bool keysAreSorted)
    {
        int min = keysAreSorted ? keys[0] : int.MaxValue;
        int max = keysAreSorted ? keys[keys.Length - 1] : int.MinValue;
        uint mask = 0;

        if (keysAreSorted)
        {
            foreach (int val in keys)
                mask |= unchecked((uint)val);
        }
        else
        {
            foreach (int val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= unchecked((uint)val);
            }
        }

        ulong range = (ulong)((long)max - min);
        return new NumericKeyProperties<int>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || (long)max - min == keys.Length - 1, mask, static v => v);
    }

    private static NumericKeyProperties<uint> GetUInt32Properties(ReadOnlySpan<uint> keys, bool keysAreSorted)
    {
        uint min = keysAreSorted ? keys[0] : uint.MaxValue;
        uint max = keysAreSorted ? keys[keys.Length - 1] : uint.MinValue;
        uint mask = 0;

        if (keysAreSorted)
        {
            foreach (uint val in keys)
                mask |= val;
        }
        else
        {
            foreach (uint val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= val;
            }
        }

        ulong range = max - min;
        return new NumericKeyProperties<uint>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || (ulong)max - min == (ulong)(keys.Length - 1), mask, static v => v);
    }

    private static NumericKeyProperties<long> GetInt64Properties(ReadOnlySpan<long> keys, bool keysAreSorted)
    {
        long min = keysAreSorted ? keys[0] : long.MaxValue;
        long max = keysAreSorted ? keys[keys.Length - 1] : long.MinValue;
        ulong mask = 0;

        if (keysAreSorted)
        {
            foreach (long val in keys)
                mask |= unchecked((ulong)val);
        }
        else
        {
            foreach (long val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= unchecked((ulong)val);
            }
        }

        ulong range = unchecked((ulong)max - (ulong)min);
        return new NumericKeyProperties<long>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || range == (ulong)(keys.Length - 1), mask, static v => v);
    }

    private static NumericKeyProperties<ulong> GetUInt64Properties(ReadOnlySpan<ulong> keys, bool keysAreSorted)
    {
        ulong min = keysAreSorted ? keys[0] : ulong.MaxValue;
        ulong max = keysAreSorted ? keys[keys.Length - 1] : ulong.MinValue;
        ulong mask = 0;

        if (keysAreSorted)
        {
            foreach (ulong val in keys)
                mask |= val;
        }
        else
        {
            foreach (ulong val in keys)
            {
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                mask |= val;
            }
        }

        ulong range = max - min;
        return new NumericKeyProperties<ulong>(min, max, range, CalculateDensity(keys.Length, range), false, keys.Length <= 1 || max - min == (ulong)(keys.Length - 1), mask, static v => (long)v);
    }

    private static bool IsFloatConsecutive(ReadOnlySpan<float> keys, float min, float max, bool hasNaNOrInfinity, bool keysAreSorted)
    {
        if (hasNaNOrInfinity)
            return false;

        if (keys.Length <= 1)
            return true;

        float expectedRange = keys.Length - 1;
        float range = max - min;

        if (Math.Abs(range - expectedRange) > float.Epsilon)
            return false;

        if (keysAreSorted)
        {
            for (int i = 1; i < keys.Length; i++)
            {
                if (Math.Abs(keys[i] - (keys[i - 1] + 1.0f)) > float.Epsilon)
                    return false;
            }
        }
        else
        {
            float[] sorted = new float[keys.Length];
            keys.CopyTo(sorted);
            Array.Sort(sorted);

            for (int i = 1; i < sorted.Length; i++)
            {
                if (Math.Abs(sorted[i] - (sorted[i - 1] + 1.0f)) > float.Epsilon)
                    return false;
            }
        }

        return true;
    }

    private static bool IsDoubleConsecutive(ReadOnlySpan<double> keys, double min, double max, bool hasNaNOrInfinity, bool keysAreSorted)
    {
        if (hasNaNOrInfinity)
            return false;

        if (keys.Length <= 1)
            return true;

        double expectedRange = keys.Length - 1;
        double range = max - min;

        if (Math.Abs(range - expectedRange) > double.Epsilon)
            return false;

        if (keysAreSorted)
        {
            for (int i = 1; i < keys.Length; i++)
            {
                if (Math.Abs(keys[i] - (keys[i - 1] + 1.0d)) > double.Epsilon)
                    return false;
            }
        }
        else
        {
            double[] sorted = new double[keys.Length];
            keys.CopyTo(sorted);
            Array.Sort(sorted);

            for (int i = 1; i < sorted.Length; i++)
            {
                if (Math.Abs(sorted[i] - (sorted[i - 1] + 1.0d)) > double.Epsilon)
                    return false;
            }
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

    private static double CalculateDensity(int keyCount, ulong range)
    {
        if (keyCount <= 0)
            return 0;

        return keyCount / (range + 1.0d);
    }

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

    private static uint UpdateGcd(uint current, uint value)
    {
        if (current == 0)
            return value;

        while (value != 0)
        {
            uint remainder = current % value;
            current = value;
            value = remainder;
        }

        return current;
    }

    private static ulong GetStringBitMask(ReadOnlySpan<string> keys, GeneratorEncoding encoding, bool ignoreCase, int byteCount)
    {
        ulong union = 0;

        if (encoding == GeneratorEncoding.ASCII)
        {
            foreach (string key in keys)
                union |= GetFirst64BitsAscii(key, ignoreCase, byteCount);
        }
        else if (encoding == GeneratorEncoding.UTF8)
        {
            foreach (string key in keys)
                union |= GetFirst64BitsUtf8(key, ignoreCase, byteCount);
        }
        else if (encoding == GeneratorEncoding.UTF16)
        {
            int charCount = byteCount / 2;
            foreach (string key in keys)
                union |= GetFirst64BitsUtf16(key, ignoreCase, charCount);
        }
        else
            return 0;

        ulong mask = ~union;
        ulong fullMask = byteCount == 8 ? ulong.MaxValue : (1UL << (byteCount * 8)) - 1;
        return mask & fullMask;
    }

    private static ulong GetFirst64BitsAscii(string value, bool ignoreCase, int byteCount)
    {
        ulong result = 0;

        for (int i = 0; i < byteCount; i++)
        {
            uint b = value[i];
            if (ignoreCase && b - 'A' <= 'Z' - 'A')
                b |= 0x20u;

            result |= (ulong)b << (i * 8);
        }

        return result;
    }

    private static ulong GetFirst64BitsUtf8(string value, bool ignoreCase, int byteCount)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        ulong result = 0;

        for (int i = 0; i < byteCount; i++)
        {
            uint b = bytes[i];
            if (ignoreCase && b - 'A' <= 'Z' - 'A')
                b |= 0x20u;

            result |= (ulong)b << (i * 8);
        }

        return result;
    }

    private static ulong GetFirst64BitsUtf16(string value, bool ignoreCase, int charCount)
    {
        ulong result = 0;

        for (int i = 0; i < charCount; i++)
        {
            uint c = value[i];
            if (ignoreCase && c - 'A' <= 'Z' - 'A')
                c |= 0x20u;

            result |= (ulong)c << (i * 16);
        }

        return result;
    }
}