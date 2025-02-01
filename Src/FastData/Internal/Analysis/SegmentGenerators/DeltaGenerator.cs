using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>
/// This generator uses the delta map from string analysis to provide segments that avoids areas with identical characters and
/// instead try and target areas of high deltas (differences in characters). It is suitable for large strings where brute-force
/// is infeasible.
/// </summary>
internal class DeltaGenerator : ISegmentGenerator
{
    public bool IsAppropriate(StringProperties props) => props.LengthData.Min >= 8;

    /*
    The idea behind this generator is to read the delta maps made during string analysis and derive
    a string segment that uses the characters that change most often (highest delta).

    The goal is to use as few characters as possible, so it outputs small segments from the start of the string
    first, and then expand in length for each iteration. It starts by finding segments of characters that are within
    a certain threshold, then it finds the segment with the highest variance (max(maxVal - minVal)) and starts there

    However, if the segment is too far into the strings, it might bot be a proper offset, so we constrain the algorithm
    to only explore possibilities that are within the shortest string first. Afterward it explore possibilities from the
    end of the string. We prefer start of string first because it avoids an extra operation (str.Length - offset).

    ## Example ##
    Input is as such:
    aaaaaaaaahAj9dDmaaaaaKUAaahd8ad
    aaaaaaaa29kddkaaaaaaa(22aagk90aaatj9
    aaaaaaaaa99xKA82LaaaaFKAaah

    The delta map should indicate (X) the places in the strings that are interesting, and where it is not (-):
    --------XXXXXXXXX----XXX--XXXXXXXXXX

    Now we calculate offset and lengths, and get 3 segments:
    1: 8, 9
    2: 21, 3
    3: 26, 10

    Since the third segment is after the smallest string, we omit it the first time around.
    We find the variance in each segment and use that first. In this example, let's say it is the first segment.
    Now we start returning the shortest segment and increase in length.

    --------[X]XXXXXXXX----XXX--XXXXXXXXXX
    --------[XX]XXXXXXX----XXX--XXXXXXXXXX
    ... repeats ...
    --------[XXXXXXXXX]----XXX--XXXXXXXXXX

    Then we take the second segment and return the smallest string etc.

    --------XXXXXXXXX----[X]XX--XXXXXXXXXX
    --------XXXXXXXXX----[XX]X--XXXXXXXXXX
    --------XXXXXXXXX----[XXX]--XXXXXXXXXX

    So what about the third segment? We use the right aligned delta data instead. Let's do the example for the same data.
    It is right-adjusted to better illustrate what happens. Here is our data:

         aaaaaaaaahAj9dDmaaaaaKUAaahd8ad
    aaaaaaaa29kddkaaaaaaa(22aagk90aaatj9
             aaaaaaaaa99xKA82LaaaaFKAaah

    Create a delta map:
    ------------------------------------

    As we can see, there are no interesting segments for this particular inputs when it is right-aligned.
    */

    public IEnumerable<StringSegment> Generate(StringProperties props)
    {
        // We start from the left, which is faster due to not having to do right-align checks
        foreach (StringSegment stringSegment in CalculateSegments(props, props.DeltaData.Left))
            yield return stringSegment with { Alignment = Alignment.Left };

        foreach (StringSegment stringSegment in CalculateSegments(props, props.DeltaData.Right))
            yield return stringSegment with { Alignment = Alignment.Right };
    }

    /// <summary>Returns segments with increasing length. It starts with the highest variance segment first and then continues to lower ones</summary>
    private static IEnumerable<StringSegment> CalculateSegments(StringProperties stringProps, int[] data)
    {
        StringSegment[] segments = GetSegments(data).ToArray();
        Debug.Assert(segments.Length > 0); // We should always have at least one segment

        // We sort the segments in order of variance
        Array.Sort(segments, (a, b) => ComputeVariance(b, stringProps, data).CompareTo(ComputeVariance(a, stringProps, data)));

        foreach (StringSegment segment in segments)
        {
            for (int i = 1; i <= segment.Length; i++)
                yield return segment with { Length = i };
        }
    }

    /// <summary>Computes the highest variance (difference between min and max of delta)</summary>
    private static int ComputeVariance(StringSegment segment, StringProperties stringProps, int[] data)
    {
        if (segment.Offset > stringProps.LengthData.Min)
            return 0;

        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = segment.Offset; i < Math.Min(stringProps.LengthData.Min, segment.Offset + segment.Length); i++)
        {
            min = Math.Min(data[i], min);
            max = Math.Max(data[i], max);
        }

        int variance = Math.Abs(max - min);

        if (segment.Length == 1)
            Debug.Assert(variance == 0); //Segments with length 1, should have a variance of 0

        return variance;
    }

    /// <summary>Finds segments in the strings using a delta map</summary>
    private static IEnumerable<StringSegment> GetSegments(int[] arr)
    {
        int offset = 0;
        while (offset < arr.Length)
        {
            while (offset < arr.Length && arr[offset] == 0)
                offset++;

            if (offset >= arr.Length)
                break;

            int start = offset;
            while (offset < arr.Length && arr[offset] != 0)
                offset++;

            yield return new StringSegment(start, offset - start, Alignment.Unknown);
        }

        // There were no zeroes in the delta map. Return the entire string.
        if (offset == arr.Length)
            yield return new StringSegment(0, arr.Length, Alignment.Left);
    }
}