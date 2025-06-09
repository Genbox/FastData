using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis;

internal static class DataAnalyzer
{
    internal static StringProperties GetStringProperties(ReadOnlySpan<string> data)
    {
        return GetStringProperties<string>(data);
    }

    internal static StringProperties GetStringProperties<T>(ReadOnlySpan<T> data) where T : notnull
    {
        //Contains a map of unique lengths
        LengthBitArray lengthMap = new LengthBitArray();

        //We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = (string)(object)data[0];
        int minLength = int.MaxValue;
        bool uniq = true;

        foreach (T val in data)
        {
            string str = (string)(object)val;

            if (str.Length > maxStr.Length)
                maxStr = str;

            minLength = Math.Min(minLength, str.Length); //Track the smallest string. It might be more than what lengthmap supports
            uniq &= !lengthMap.SetTrue(str.Length);
        }

        //Build a forward and reverse map of merged entropy
        //We can derive common substrings from it, as well as high-entropy substring hash functions
        int[] left = new int[maxStr.Length];
        int[] right = new int[maxStr.Length];
        bool flag = true;
        bool allAscii = true;

        foreach (T val in data)
        {
            string str = (string)(object)val;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                char rc = str[str.Length - 1 - i];

                left[i] += flag ? c : -c;
                right[i] += flag ? rc : -rc;

                if (c > 255)
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

        return new StringProperties(new LengthData((uint)minLength, (uint)maxStr.Length, uniq, lengthMap), new DeltaData(left, right), new CharacterData(allAscii));
    }

    internal static IntegerProperties<T> GetCharProperties<T>(ReadOnlySpan<char> data)
    {
        char min = char.MaxValue;
        char max = char.MinValue;

        foreach (char c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static FloatProperties<T> GetSingleProperties<T>(ReadOnlySpan<float> data)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (float c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new FloatProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static FloatProperties<T> GetDoubleProperties<T>(ReadOnlySpan<double> data)
    {
        double min = double.MaxValue;
        double max = double.MinValue;

        foreach (double c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new FloatProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetByteProperties<T>(ReadOnlySpan<byte> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetSByteProperties<T>(ReadOnlySpan<sbyte> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetInt16Properties<T>(ReadOnlySpan<short> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetUInt16Properties<T>(ReadOnlySpan<ushort> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetInt32Properties<T>(ReadOnlySpan<int> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetUInt32Properties<T>(ReadOnlySpan<uint> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetInt64Properties<T>(ReadOnlySpan<long> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }

    internal static IntegerProperties<T> GetUInt64Properties<T>(ReadOnlySpan<ulong> data)
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

        return new IntegerProperties<T>((T)(object)min, (T)(object)max);
    }
}