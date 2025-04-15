using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Internal.Analysis.Segments;

/// <summary>Creates edge-grams with offsets of 0 and lengths [1..max]</summary>
internal class EdgeGramGenerator : ISegmentGenerator
{
    internal const int MaxLength = 8;

    public bool IsAppropriate(StringProperties props) => true;

    public IEnumerable<StringSegment> Generate(StringProperties props)
    {
        int len;

        int max = (int)Math.Min(props.LengthData.Min, MaxLength); //We cannot segment above the shortest string.

        for (len = 1; len <= max; len++)
            yield return new StringSegment(0, len, Alignment.Left);

        for (len = 1; len <= max; len++)
            yield return new StringSegment(0, len, Alignment.Right);
    }
}