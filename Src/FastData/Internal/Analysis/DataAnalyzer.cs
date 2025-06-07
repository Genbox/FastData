using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis;

internal static class DataAnalyzer
{
    internal static StringProperties GetStringProperties(string[] data)
    {
        //Contains a map of unique lengths
        LengthBitArray lengthMap = new LengthBitArray();

        //We need to know the longest string for optimal mixing. Probably not 100% necessary.
        string maxStr = data[0];
        int minLength = int.MaxValue;
        bool uniq = true;

        foreach (string val in data)
        {
            if (val.Length > maxStr.Length)
                maxStr = val;

            minLength = Math.Min(minLength, val.Length); //Track the smallest string. It might be more than what lengthmap supports
            uniq &= !lengthMap.SetTrue(val.Length);
        }

        //Build a forward and reverse map of merged entropy
        //We can derive common substrings from it, as well as high-entropy substring hash functions
        int[] left = new int[maxStr.Length];
        int[] right = new int[maxStr.Length];
        bool flag = true;
        bool allAscii = true;

        foreach (string val in data)
        {
            for (int i = 0; i < val.Length; i++)
            {
                char c = val[i];
                char rc = val[val.Length - 1 - i];

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

    internal static IntegerProperties<T> GetCharProperties<T>(char[] data)
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

    internal static FloatProperties<T> GetSingleProperties<T>(float[] data)
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

    internal static FloatProperties<T> GetDoubleProperties<T>(double[] data)
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

    internal static IntegerProperties<T> GetByteProperties<T>(byte[] data)
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

    internal static IntegerProperties<T> GetSByteProperties<T>(sbyte[] data)
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

    internal static IntegerProperties<T> GetInt16Properties<T>(short[] data)
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

    internal static IntegerProperties<T> GetUInt16Properties<T>(ushort[] data)
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

    internal static IntegerProperties<T> GetInt32Properties<T>(int[] data)
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

    internal static IntegerProperties<T> GetUInt32Properties<T>(uint[] data)
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

    internal static IntegerProperties<T> GetInt64Properties<T>(long[] data)
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

    internal static IntegerProperties<T> GetUInt64Properties<T>(ulong[] data)
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