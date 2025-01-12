namespace Genbox.FastData.Internal.Analysis;

internal static class Analyzer
{
    internal static StringProperties GetStringProperties(object[] data)
    {
        IntegerBitSet lengthMap = new IntegerBitSet();
        CharacterMap characterMap = new CharacterMap();

        string maxStr = (string)data[0];

        foreach (string val in data)
        {
            if (val.Length > maxStr.Length)
                maxStr = val;

            lengthMap.Set(val.Length);
        }

        int[] left = new int[maxStr.Length];
        int[] right = new int[maxStr.Length];
        bool flag = true;

        foreach (string val in data)
        {
            for (int i = 0; i < val.Length; i++)
            {
                char c = val[i];
                char rc = val[val.Length - 1 - i];

                left[i] += flag ? c : -c;
                right[i] += flag ? rc : -rc;

                characterMap.Add(c);
            }

            flag = !flag;
        }

        //Odd number of items. We need it to be even
        //For best mixing, we take the longest string
        if (data.Length % 2 != 0)
        {
            for (int i = 0; i < maxStr.Length; i++)
            {
                char c = maxStr[i];
                char rc = maxStr[maxStr.Length - 1 - i];

                left[i] += flag ? c : -c;
                right[i] += flag ? rc : -rc;

                //We do not add to characterMap here since it does not need the duplicate
            }
        }

        return new StringProperties(lengthMap, new EntropyData(left, right), characterMap);
    }

    internal static CharProperties GetCharProperties(object[] data)
    {
        char min = char.MaxValue;
        char max = char.MinValue;

        foreach (char c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new CharProperties(min, max);
    }

    internal static FloatProperties GetSingleProperties(object[] data)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (float c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new FloatProperties(min, max);
    }

    internal static FloatProperties GetDoubleProperties(object[] data)
    {
        double min = double.MaxValue;
        double max = double.MinValue;

        foreach (double c in data)
        {
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return new FloatProperties(min, max);
    }

    internal static UnsignedIntegerProperties GetByteProperties(object[] data)
    {
        byte min = byte.MaxValue;
        byte max = byte.MinValue;

        byte lastValue = (byte)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (byte val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetSByteProperties(object[] data)
    {
        sbyte min = sbyte.MaxValue;
        sbyte max = sbyte.MinValue;

        sbyte lastValue = (sbyte)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (sbyte val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetInt16Properties(object[] data)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        short lastValue = (short)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (short val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static UnsignedIntegerProperties GetUInt16Properties(object[] data)
    {
        ushort min = ushort.MaxValue;
        ushort max = ushort.MinValue;

        ushort lastValue = (ushort)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (ushort val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetInt32Properties(object[] data)
    {
        int min = int.MaxValue;
        int max = int.MinValue;

        int lastValue = (int)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (int val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static UnsignedIntegerProperties GetUInt32Properties(object[] data)
    {
        uint min = uint.MaxValue;
        uint max = uint.MinValue;

        uint lastValue = (uint)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (uint val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetInt64Properties(object[] data)
    {
        long min = long.MaxValue;
        long max = long.MinValue;

        long lastValue = (long)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (long val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static UnsignedIntegerProperties GetUInt64Properties(object[] data)
    {
        ulong min = ulong.MaxValue;
        ulong max = ulong.MinValue;

        ulong lastValue = (ulong)data[0];

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        foreach (ulong val in data)
        {
            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }
}