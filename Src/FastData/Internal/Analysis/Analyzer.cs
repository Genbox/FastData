using System.Diagnostics;

namespace Genbox.FastData.Internal.Analysis;

internal static class Analyzer
{
    private const byte _analysisMaxLength = 255;

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

    internal static IntegerProperties GetIntegerProperties(IEnumerable<int> data)
    {
        int min = int.MaxValue;
        int max = int.MinValue;

        using IEnumerator<int> enumerator = data.GetEnumerator();
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