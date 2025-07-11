using System.Text;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis;

internal static class DataAnalyzer
{
    internal static ValueProperties<T> GetValueProperties<T>(T[] data) => data switch
    {
        char[] charArr when typeof(T) == typeof(char) => (ValueProperties<T>)(object)GetCharProperties(charArr),
        sbyte[] sbyteArr when typeof(T) == typeof(sbyte) => (ValueProperties<T>)(object)GetSByteProperties(sbyteArr),
        byte[] byteArr when typeof(T) == typeof(byte) => (ValueProperties<T>)(object)GetByteProperties(byteArr),
        short[] shortArr when typeof(T) == typeof(short) => (ValueProperties<T>)(object)GetInt16Properties(shortArr),
        ushort[] ushortArr when typeof(T) == typeof(ushort) => (ValueProperties<T>)(object)GetUInt16Properties(ushortArr),
        int[] intArr when typeof(T) == typeof(int) => (ValueProperties<T>)(object)GetInt32Properties(intArr),
        uint[] uintArr when typeof(T) == typeof(uint) => (ValueProperties<T>)(object)GetUInt32Properties(uintArr),
        long[] longArr when typeof(T) == typeof(long) => (ValueProperties<T>)(object)GetInt64Properties(longArr),
        ulong[] ulongArr when typeof(T) == typeof(ulong) => (ValueProperties<T>)(object)GetUInt64Properties(ulongArr),
        float[] floatArr when typeof(T) == typeof(float) => (ValueProperties<T>)(object)GetSingleProperties(floatArr),
        double[] doubleArr when typeof(T) == typeof(double) => (ValueProperties<T>)(object)GetDoubleProperties(doubleArr),
        _ => throw new InvalidOperationException($"Unsupported data type: {typeof(T).Name}")
    };

    internal static StringProperties GetStringProperties(string[] data)
    {
        //Contains a map of unique lengths
        LengthBitArray lengthMap = new LengthBitArray();

        //We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = data[0];
        int minLength = int.MaxValue;
        int minUtf8ByteLength = int.MaxValue;
        int maxUtf8ByteLength = int.MinValue;
        int minUtf16ByteLength = int.MaxValue;
        int maxUtf16ByteLength = int.MinValue;
        bool uniq = true;

        foreach (string str in data)
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

        foreach (string str in data)
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
        if (data.Length % 2 != 0)
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

    private static ValueProperties<char> GetCharProperties(char[] data)
    {
        char min = char.MaxValue;
        char max = char.MinValue;

        foreach (char c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new ValueProperties<char>(min, max, false);
    }

    private static ValueProperties<float> GetSingleProperties(float[] data)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        bool hasZeroOrNaN = false;
        foreach (float c in data)
        {
#pragma warning disable S1244
            if (!hasZeroOrNaN && (float.IsNaN(c) || c == 0.0f))
#pragma warning restore S1244
                hasZeroOrNaN = true;

            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new ValueProperties<float>(min, max, hasZeroOrNaN);
    }

    private static ValueProperties<double> GetDoubleProperties(double[] data)
    {
        double min = double.MaxValue;
        double max = double.MinValue;

        bool hasZeroOrNaN = false;
        foreach (double c in data)
        {
#pragma warning disable S1244
            if (!hasZeroOrNaN && (double.IsNaN(c) || c == 0.0d))
#pragma warning restore S1244
                hasZeroOrNaN = true;

            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new ValueProperties<double>(min, max, hasZeroOrNaN);
    }

    private static ValueProperties<byte> GetByteProperties(byte[] data)
    {
        byte min = byte.MaxValue;
        byte max = byte.MinValue;

        byte lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (byte val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<byte>(min, max, false);
    }

    private static ValueProperties<sbyte> GetSByteProperties(sbyte[] data)
    {
        sbyte min = sbyte.MaxValue;
        sbyte max = sbyte.MinValue;

        sbyte lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (sbyte val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<sbyte>(min, max, false);
    }

    private static ValueProperties<short> GetInt16Properties(short[] data)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        short lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (short val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<short>(min, max, false);
    }

    private static ValueProperties<ushort> GetUInt16Properties(ushort[] data)
    {
        ushort min = ushort.MaxValue;
        ushort max = ushort.MinValue;

        ushort lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (ushort val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<ushort>(min, max, false);
    }

    private static ValueProperties<int> GetInt32Properties(int[] data)
    {
        int min = int.MaxValue;
        int max = int.MinValue;

        int lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (int val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<int>(min, max, false);
    }

    private static ValueProperties<uint> GetUInt32Properties(uint[] data)
    {
        uint min = uint.MaxValue;
        uint max = uint.MinValue;

        uint lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (uint val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<uint>(min, max, false);
    }

    private static ValueProperties<long> GetInt64Properties(long[] data)
    {
        long min = long.MaxValue;
        long max = long.MinValue;

        long lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (long val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<long>(min, max, false);
    }

    private static ValueProperties<ulong> GetUInt64Properties(ulong[] data)
    {
        ulong min = ulong.MaxValue;
        ulong max = ulong.MinValue;

        ulong lastValue = data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        foreach (ulong val in data)
        {
            min = Math.Min(min, val);
            max = Math.Max(max, val);
        }

        return new ValueProperties<ulong>(min, max, false);
    }
}