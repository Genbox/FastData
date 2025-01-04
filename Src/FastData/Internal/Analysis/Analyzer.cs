using System.Diagnostics;

namespace Genbox.FastData.Internal.Analysis;

internal static class Analyzer
{
    private const byte _analysisMaxLength = 255;

    internal static FloatProperties GetFloatProperties(IEnumerable<float> data)
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

    internal static FloatProperties GetFloatProperties(IEnumerable<double> data)
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

    internal static CharProperties GetCharProperties(IEnumerable<char> data)
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

    internal static StringProperties GetStringProperties(IEnumerable<string> data)
    {
        uint minStrLen = uint.MaxValue;
        uint maxStrLen = 0;

        ushort minChar = ushort.MaxValue;
        ushort maxChar = 0;

        bool[] lengthIndex = new bool[_analysisMaxLength];

        using IEnumerator<string> enumerator = data.GetEnumerator();
        enumerator.MoveNext();

        string reference = enumerator.Current!;
        byte longestLeft = (byte)reference.Length;
        byte longestRight = (byte)reference.Length;

        uint len = (uint)reference.Length;

        SetMinMax(len, ref minStrLen, ref maxStrLen);
        SetLengthIndex(len, lengthIndex);

        (ushort minCharVal, ushort maxCharVal) = GetCharMinMax(reference);
        minChar = Math.Min(minCharVal, minChar);
        maxChar = Math.Max(maxCharVal, maxChar);

        while (enumerator.MoveNext())
        {
            string val = enumerator.Current!;

            len = (uint)val.Length;

            if (len == 0)
                continue;

            SetMinMax(len, ref minStrLen, ref maxStrLen);
            SetLengthIndex(len, lengthIndex);

            (minCharVal, maxCharVal) = GetCharMinMax(val);
            minChar = Math.Min(minCharVal, minChar);
            maxChar = Math.Max(maxCharVal, maxChar);

            byte currentLength = 0;
            int minLength = Math.Min(longestLeft, val.Length);
            for (int j = 0; j < minLength; j++)
            {
                if (reference[j] == val[j])
                    currentLength++;
                else
                    break;
            }
            longestLeft = currentLength;

            currentLength = 0;
            minLength = Math.Min(longestRight, val.Length);
            for (int j = 0; j < minLength; j++)
            {
                if (reference[reference.Length - 1 - j] == val[val.Length - 1 - j])
                    currentLength++;
                else
                    break;
            }
            longestRight = currentLength;
        }

        return new StringProperties(minStrLen, maxStrLen, minChar, maxChar, longestLeft, longestRight, (uint)lengthIndex.Count(x => x));
    }

    internal static UnsignedIntegerProperties GetUnsignedIntegerProperties(IEnumerable<byte> data)
    {
        byte min = byte.MaxValue;
        byte max = byte.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        byte lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            byte val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetIntegerProperties(IEnumerable<sbyte> data)
    {
        sbyte min = sbyte.MaxValue;
        sbyte max = sbyte.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        sbyte lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            sbyte val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetIntegerProperties(IEnumerable<short> data)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        short lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            short val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static UnsignedIntegerProperties GetUnsignedIntegerProperties(IEnumerable<ushort> data)
    {
        ushort min = ushort.MaxValue;
        ushort max = ushort.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        ushort lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            ushort val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetIntegerProperties(IEnumerable<int> data)
    {
        int min = int.MaxValue;
        int max = int.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        int lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            int val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static UnsignedIntegerProperties GetUnsignedIntegerProperties(IEnumerable<uint> data)
    {
        uint min = uint.MaxValue;
        uint max = uint.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        uint lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            uint val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    internal static IntegerProperties GetIntegerProperties(IEnumerable<long> data)
    {
        long min = long.MaxValue;
        long max = long.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        long lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            long val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new IntegerProperties(min, max, consecutive);
    }

    internal static UnsignedIntegerProperties GetUnsignedIntegerProperties(IEnumerable<ulong> data)
    {
        ulong min = ulong.MaxValue;
        ulong max = ulong.MinValue;

        using var enumerator = data.GetEnumerator();
        enumerator.MoveNext();
        ulong lastValue = enumerator.Current;

        min = Math.Min(min, lastValue);
        max = Math.Max(max, lastValue);

        bool consecutive = true;
        while (enumerator.MoveNext())
        {
            ulong val = enumerator.Current;

            if (consecutive && lastValue + 1 != val)
                consecutive = false;

            min = Math.Min(min, val);
            max = Math.Max(max, val);
            lastValue = val;
        }

        return new UnsignedIntegerProperties(min, max, consecutive);
    }

    /// <summary>
    /// Updates the minimum and maximum length.
    /// It is built using refs to support incremental updates.
    /// </summary>
    internal static void SetMinMax(uint strLen, ref uint minStrLen, ref uint maxStrLen)
    {
        minStrLen = Math.Min(minStrLen, strLen);
        maxStrLen = Math.Max(maxStrLen, strLen);
    }

    /// <summary>
    /// Updates an index with lengths and report back if the length collided with a previous length. Can be called incrementally.
    /// </summary>
    internal static void SetLengthIndex(uint strLen, bool[] lengthIndex)
    {
        Debug.Assert(strLen > 0);

        if (strLen <= _analysisMaxLength)
            lengthIndex[strLen - 1] = true;
    }

    /// <summary>
    /// Gets the minimum and maximum char value of the string.
    /// </summary>
    internal static (ushort minCharVal, ushort maxCharVal) GetCharMinMax(string val)
    {
        ushort minVal = ushort.MaxValue;
        ushort maxVal = 0;

        foreach (ushort c in val)
        {
            minVal = Math.Min(c, minVal);
            maxVal = Math.Max(c, maxVal);
        }

        return (minVal, maxVal);
    }
}