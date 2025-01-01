using System.Diagnostics;

namespace Genbox.FastData.Internal.Analysis;

internal static class Analyzer
{
    private const byte _analysisMaxLength = 255;

    /// <summary>
    /// Analyze a set of strings and return properties about the strings which can be used to determine the optimal way to compare the strings.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static AnalysisResult Analyze(string[] data)
    {
        if (data.Length == 0)
            throw new InvalidOperationException("This method will produce invalid results given an empty array");

        StringProperties strProps = GetProperties(data);

        byte longestLeft = 0;

        if (data.Length > 1)
            longestLeft = LongestLeft(data);

        byte longestRight = 0;

        if (data.Length > 2)
            longestRight = LongestRight(data);

        return new AnalysisResult(strProps, data.Length, longestLeft, longestRight);
    }

    /// <summary>
    /// Finds the longest left-aligned common substring length in a set of strings
    /// </summary>
    /// <param name="data">The strings to compare</param>
    /// <returns>Returns the length of the longest common left-aligned substring. Returns 0 if all the string are different.</returns>
    internal static byte LongestLeft(string[] data)
    {
        Debug.Assert(data.Length > 1);

        string reference = data[0];
        byte length = (byte)reference.Length;

        for (int i = 1; i < data.Length; i++)
        {
            byte currentLength = 0;
            int minLength = Math.Min(length, data[i].Length);
            for (int j = 0; j < minLength; j++)
            {
                if (reference[j] == data[i][j])
                    currentLength++;
                else
                    break;
            }
            length = currentLength;
        }

        return length;
    }

    /// <summary>
    /// Finds the longest right-aligned common substring length in a set of strings
    /// </summary>
    /// <param name="data">The strings to compare</param>
    /// <returns>Returns the length of the longest common right-aligned substring. Returns 0 if all the string are different.</returns>
    internal static byte LongestRight(string[] data)
    {
        Debug.Assert(data.Length > 1);

        string reference = data[0];
        byte length = (byte)reference.Length;

        for (int i = 1; i < data.Length; i++)
        {
            byte currentLength = 0;
            int minLength = Math.Min(length, data[i].Length);
            for (int j = 0; j < minLength; j++)
            {
                if (reference[reference.Length - 1 - j] == data[i][data[i].Length - 1 - j])
                    currentLength++;
                else
                    break;
            }
            length = currentLength;
        }

        return length;
    }

    /// <summary>
    /// Gets a few generic properties of a set of strings.
    /// </summary>
    internal static StringProperties GetProperties(string[] data)
    {
        uint minStrLen = uint.MaxValue;
        uint maxStrLen = 0;

        ushort minChar = ushort.MaxValue;
        ushort maxChar = 0;

        bool[] lengths = new bool[_analysisMaxLength];
        bool uniqLength = true;

        foreach (string val in data)
        {
            uint len = (uint)val.Length;

            if (len == 0)
                continue;

            GetStrMinMax(len, ref minStrLen, ref maxStrLen);
            GetStrUniq(len, lengths, ref uniqLength);

            (ushort minCharVal, ushort maxCharVal) = GetCharMinMax(val);
            minChar = Math.Min(minCharVal, minChar);
            maxChar = Math.Max(maxCharVal, maxChar);
        }

        return new StringProperties(minStrLen, maxStrLen, minChar, maxChar, uniqLength);
    }

    /// <summary>
    /// Given a string length, it is compared <paramref name="minStrLen"/> and <paramref name="maxStrLen"/>.
    /// It is built using refs to support incremental updates.
    /// </summary>
    internal static void GetStrMinMax(uint strLen, ref uint minStrLen, ref uint maxStrLen)
    {
        minStrLen = Math.Min(minStrLen, strLen);
        maxStrLen = Math.Max(maxStrLen, strLen);
    }

    /// <summary>
    /// Updates an index with lengths and report back if the length collided with a previous length. Can be called incrementally.
    /// </summary>
    internal static void GetStrUniq(uint strLen, bool[] lengthIndex, ref bool uniqLength)
    {
        Debug.Assert(strLen > 0);

        if (uniqLength && strLen <= _analysisMaxLength)
        {
            if (lengthIndex[strLen - 1])
                uniqLength = false;

            lengthIndex[strLen - 1] = true;
        }
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