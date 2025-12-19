using System.Text;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis;

internal static class KeyAnalyzer
{
    internal static KeyProperties<T> GetProperties<T>(T[] keys) => keys switch
    {
        char[] charArr when typeof(T) == typeof(char) => (KeyProperties<T>)(object)GetCharProperties(charArr),
        sbyte[] sbyteArr when typeof(T) == typeof(sbyte) => (KeyProperties<T>)(object)GetSByteProperties(sbyteArr),
        byte[] byteArr when typeof(T) == typeof(byte) => (KeyProperties<T>)(object)GetByteProperties(byteArr),
        short[] shortArr when typeof(T) == typeof(short) => (KeyProperties<T>)(object)GetInt16Properties(shortArr),
        ushort[] ushortArr when typeof(T) == typeof(ushort) => (KeyProperties<T>)(object)GetUInt16Properties(ushortArr),
        int[] intArr when typeof(T) == typeof(int) => (KeyProperties<T>)(object)GetInt32Properties(intArr),
        uint[] uintArr when typeof(T) == typeof(uint) => (KeyProperties<T>)(object)GetUInt32Properties(uintArr),
        long[] longArr when typeof(T) == typeof(long) => (KeyProperties<T>)(object)GetInt64Properties(longArr),
        ulong[] ulongArr when typeof(T) == typeof(ulong) => (KeyProperties<T>)(object)GetUInt64Properties(ulongArr),
        float[] floatArr when typeof(T) == typeof(float) => (KeyProperties<T>)(object)GetSingleProperties(floatArr),
        double[] doubleArr when typeof(T) == typeof(double) => (KeyProperties<T>)(object)GetDoubleProperties(doubleArr),
        _ => throw new InvalidOperationException($"Unsupported data type: {typeof(T).Name}")
    };

    internal static StringProperties GetStringProperties(string[] keys)
    {
        //Contains a map of unique lengths
        LengthBitArray lengthMap = new LengthBitArray();

        //We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = keys[0];
        int minLength = int.MaxValue;
        int minUtf8ByteLength = int.MaxValue;
        int maxUtf8ByteLength = int.MinValue;
        int minUtf16ByteLength = int.MaxValue;
        int maxUtf16ByteLength = int.MinValue;
        bool uniq = true;

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

            minLength = Math.Min(minLength, str.Length); //Track the smallest string. It might be more than what lengthmap supports
            uniq &= !lengthMap.SetTrue(str.Length);
        }

        //Build a forward and reverse map of merged entropy
        //We can derive common substrings from it, as well as high-entropy substring hash functions
        int[] left = new int[maxStr.Length];
        int[] right = new int[maxStr.Length];
        bool flag = true;
        bool allAscii = true;

        foreach (string str in keys)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                char rc = str[str.Length - 1 - i];

                left[i] += flag ? c : -c;
                right[i] += flag ? rc : -rc;

                if (c > 127)
                    allAscii = false;
            }

            flag = !flag;
        }

        //Odd number of items. We need it to be even
        if (keys.Length % 2 != 0)
        {
            for (int i = 0; i < maxStr.Length; i++)
            {
                //For best mixing, we take the longest string
                char c = maxStr[i];
                char rc = maxStr[maxStr.Length - 1 - i];

                left[i] += flag ? c : -c;
                right[i] += flag ? rc : -rc;

                //We do not add to characterMap here since it does not need the duplicate
            }
        }

        return new StringProperties(new LengthData((uint)minLength, (uint)maxStr.Length, (uint)minUtf8ByteLength, (uint)maxUtf8ByteLength, (uint)minUtf16ByteLength, (uint)maxUtf16ByteLength, uniq, lengthMap), new DeltaData(left, right), new CharacterData(allAscii));
    }

    private static KeyProperties<char> GetCharProperties(char[] keys)
    {
        char min = char.MaxValue;
        char max = char.MinValue;

        foreach (char c in keys)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new KeyProperties<char>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1);
    }

    private static KeyProperties<float> GetSingleProperties(float[] keys)
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
        return new KeyProperties<float>(min, max, range, hasZeroOrNaN, IsFloatContiguous(keys, min, max, hasNaNOrInfinity));
    }

    private static KeyProperties<double> GetDoubleProperties(double[] keys)
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
        return new KeyProperties<double>(min, max, range, hasZeroOrNaN, IsDoubleContiguous(keys, min, max, hasNaNOrInfinity));
    }

    private static KeyProperties<byte> GetByteProperties(byte[] keys)
    {
        byte min = byte.MaxValue;
        byte max = byte.MinValue;

        foreach (byte val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<byte>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1);
    }

    private static KeyProperties<sbyte> GetSByteProperties(sbyte[] keys)
    {
        sbyte min = sbyte.MaxValue;
        sbyte max = sbyte.MinValue;

        foreach (sbyte val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<sbyte>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1);
    }

    private static KeyProperties<short> GetInt16Properties(short[] keys)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        foreach (short val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<short>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1);
    }

    private static KeyProperties<ushort> GetUInt16Properties(ushort[] keys)
    {
        ushort min = ushort.MaxValue;
        ushort max = ushort.MinValue;

        foreach (ushort val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<ushort>(min, max, (ulong)(max - min), false, keys.Length <= 1 || max - min == keys.Length - 1);
    }

    private static KeyProperties<int> GetInt32Properties(int[] keys)
    {
        int min = int.MaxValue;
        int max = int.MinValue;

        foreach (int val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<int>(min, max, (ulong)((long)max - min), false, keys.Length <= 1 || (long)max - min == keys.Length - 1);
    }

    private static KeyProperties<uint> GetUInt32Properties(uint[] keys)
    {
        uint min = uint.MaxValue;
        uint max = uint.MinValue;

        foreach (uint val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<uint>(min, max, max - min, false, keys.Length <= 1 || (ulong)max - min == (ulong)(keys.Length - 1));
    }

    private static KeyProperties<long> GetInt64Properties(long[] keys)
    {
        long min = long.MaxValue;
        long max = long.MinValue;

        foreach (long val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        ulong range = unchecked((ulong)max - (ulong)min);
        return new KeyProperties<long>(min, max, range, false, keys.Length <= 1 || range == (ulong)(keys.Length - 1));
    }

    private static KeyProperties<ulong> GetUInt64Properties(ulong[] keys)
    {
        ulong min = ulong.MaxValue;
        ulong max = ulong.MinValue;

        foreach (ulong val in keys)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new KeyProperties<ulong>(min, max, max - min, false, keys.Length <= 1 || max - min == (ulong)(keys.Length - 1));
    }

    private static bool IsFloatContiguous(float[] keys, float min, float max, bool hasNaNOrInfinity)
    {
        if (hasNaNOrInfinity)
            return false;

        if (keys.Length <= 1)
            return true;

        float expectedRange = keys.Length - 1;
        float range = max - min;

        if (Math.Abs(range - expectedRange) > float.Epsilon)
            return false;

        float[] sorted = (float[])keys.Clone();
        Array.Sort(sorted);

        for (int i = 1; i < sorted.Length; i++)
        {
            if (Math.Abs(sorted[i] - (sorted[i - 1] + 1.0f)) > float.Epsilon)
                return false;
        }

        return true;
    }

    private static bool IsDoubleContiguous(double[] keys, double min, double max, bool hasNaNOrInfinity)
    {
        if (hasNaNOrInfinity)
            return false;

        if (keys.Length <= 1)
            return true;

        double expectedRange = keys.Length - 1;
        double range = max - min;

        if (Math.Abs(range - expectedRange) > double.Epsilon)
            return false;

        double[] sorted = (double[])keys.Clone();
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