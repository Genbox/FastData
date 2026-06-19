using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Generates string segments from XOR-based delta maps produced during string analysis.</summary>
/// <remarks>
/// Delta maps are fast entropy approximations: a non-zero value indicates that encoded bytes differ at that position across the analyzed keys.
/// This generator converts contiguous non-zero runs into prefix segments for both left- and right-aligned maps.
/// </remarks>
internal sealed class DeltaGenerator(DeltaGeneratorConfig config) : ISegmentGenerator
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

    /// <summary>Generates progressive prefix segments from non-zero delta-map runs.</summary>
    /// <remarks>
    /// When <see cref="DeltaGeneratorConfig.MaxSegmentLength"/> is <c>-1</c>, segments are kept within the shortest encoded key length.
    /// Positive values cap the emitted segment length and allow segments beyond the shortest encoded key length.
    /// </remarks>
    public IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        // We start from the left, which is faster due to not having to do right-align checks
        if (props.DeltaData.LeftMap != null)
        {
            foreach (ArraySegment segment in GenerateAligned(props.DeltaData.LeftMap, props.LengthData.MinByteLength, config.MaxSegmentLength, Alignment.Left))
                yield return segment;
        }

        // Process right-aligned segments
        if (props.DeltaData.RightMap != null)
        {
            foreach (ArraySegment segment in GenerateAligned(props.DeltaData.RightMap, props.LengthData.MinByteLength, config.MaxSegmentLength, Alignment.Right))
                yield return segment;
        }
    }

    private static IEnumerable<ArraySegment> GenerateAligned(int[] deltaMap, int minByteLength, int maxSegmentLength, Alignment alignment)
    {
        foreach ((uint start, uint offset) in GetSegments(deltaMap))
        {
            int runLength = (int)(offset - start);
            int lengthLimit;

            if (maxSegmentLength == -1)
            {
                int remainingMinLength = minByteLength - (int)start;
                if (remainingMinLength <= 0)
                    continue;

                lengthLimit = Math.Min(runLength, remainingMinLength);
            }
            else
            {
                lengthLimit = Math.Min(runLength, maxSegmentLength);
            }

            for (int length = 1; length <= lengthLimit; length++)
                yield return new ArraySegment(start, length, alignment);
        }
    }

    /// <summary>Finds contiguous non-zero ranges in a delta map.</summary>
    /// <returns>Ranges with an inclusive start and exclusive offset.</returns>
    private static IEnumerable<(uint start, uint offset)> GetSegments(int[] arr)
    {
        uint offset = 0;
        while (offset < arr.Length)
        {
            while (offset < arr.Length && arr[offset] == 0)
                offset++;

            if (offset >= arr.Length)
                break;

            uint start = offset;
            while (offset < arr.Length && arr[offset] != 0)
                offset++;

            yield return (start, offset);
        }
    }
}