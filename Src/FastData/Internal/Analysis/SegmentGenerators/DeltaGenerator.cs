using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>This generator uses the delta map from string analysis to provide segments that avoids areas with identical characters and instead try and target areas of high deltas (differences in characters). It is suitable for large strings where brute-force is infeasible.</summary>
internal sealed class DeltaGenerator : ISegmentGenerator
{
    public bool IsAppropriate(StringKeyProperties props) => true;

    /*
    The idea behind this generator is to read the delta maps made during string analysis and derive
    a string segment that uses the characters that change most often (the highest delta).

    The goal is to use as few characters as possible, so it outputs small segments from the start of the string
    first and then expands in length for each iteration. It starts by finding segments of characters that are within
    a certain threshold, then it finds the segment with the highest variance (max(maxVal - minVal)) and starts there

    However, if the segment is too far into the strings, it might bot be a proper offset, so we constrain the algorithm
    to only explore possibilities that are within the shortest string first. Afterward it explores possibilities from the
    end of the string. We prefer the start of string first because it avoids an extra operation (str.Length - offset).

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

    So what about the third segment? We use the right-aligned delta data instead. Let's do the example for the same data.
    It is right-adjusted to better illustrate what happens. Here is our data:

         aaaaaaaaahAj9dDmaaaaaKUAaahd8ad
    aaaaaaaa29kddkaaaaaaa(22aagk90aaatj9
             aaaaaaaaa99xKA82LaaaaFKAaah

    Create a delta map:
    ------------------------------------

    As we can see, there are no interesting segments for this particular input when it is right-aligned.
    */

    public IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        if (props.DeltaData.LeftMap != null)
        {
            // We start from the left, which is faster due to not having to do right-align checks
            foreach (ArraySegment segment in CalculateSegments(props.DeltaData.LeftMap))
            {
                // Left Alignment: offset + length <= Min
                int maxLength = (int)(props.LengthData.LengthMap.Min - segment.Offset);
                int length = maxLength < 0 ? 0 : Math.Min(segment.Length, maxLength);

                if (length > 0)
                    yield return new ArraySegment(segment.Offset, length, Alignment.Left);
            }
        }

        if (props.DeltaData.RightMap != null)
        {
            // Process right-aligned segments
            foreach (ArraySegment segment in CalculateSegments(props.DeltaData.RightMap))
            {
                // Right Alignment: offset + length <= Min
                int maxLength = (int)(props.LengthData.LengthMap.Min - segment.Offset);
                int length = maxLength < 0 ? 0 : Math.Min(segment.Length, maxLength);

                if (length > 0)
                    yield return new ArraySegment(segment.Offset, length, Alignment.Right);
            }
        }
    }

    /// <summary>Returns segments with increasing length. It starts with the highest variance segment first and then continues to lower ones</summary>
    private static IEnumerable<ArraySegment> CalculateSegments(int[] deltaMap)
    {
        // Use the delta map to generate segments.
        IEnumerable<ArraySegment> segments = GetSegments(deltaMap);

        // We sort the segments in order of variance
        return segments.OrderByDescending(segment => ComputeVariance(segment, deltaMap));
    }

    /// <summary>Computes the highest variance (difference between min and max of delta)</summary>
    private static int ComputeVariance(ArraySegment segment, int[] data)
    {
        if (segment.Offset > data.Length)
            return 0;

        int min = int.MaxValue;
        int max = int.MinValue;
        for (uint i = segment.Offset; i < Math.Min(data.Length, segment.Offset + segment.Length); i++)
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
    private static IEnumerable<ArraySegment> GetSegments(int[] arr)
    {
        uint offset = 0;
        while (offset < arr.Length)
        {
            while (offset < arr.Length && arr[offset] == 0)
            {
                offset++;
            }

            if (offset >= arr.Length)
                break;

            uint start = offset;
            while (offset < arr.Length && arr[offset] != 0)
            {
                offset++;
            }

            yield return new ArraySegment(start, (int)(offset - start), Alignment.Unknown);
        }
    }
}