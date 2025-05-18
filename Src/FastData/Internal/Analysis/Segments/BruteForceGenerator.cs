using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Internal.Analysis.Segments;

/// <summary>Returns segments with offset [0..max-1] and length [1..max-1]</summary>
internal class BruteForceGenerator : ISegmentGenerator
{
    internal const int MaxLength = 8;

    public bool IsAppropriate(StringProperties props) => true;

    public IEnumerable<StringSegment> Generate(StringProperties props)
    {
        int max = (int)Math.Min(props.LengthData.Min, MaxLength); //We cannot segment above the shortest string.

        //Generates:
        //[t]est
        //[te]st
        //[tes]t

        for (uint offset = 0; offset < max; offset++)
        {
            for (int length = 1; length <= max - offset; length++)
            {
                yield return new StringSegment(offset, length, Alignment.Left);
            }
        }

        //Generates:
        //tes[t]
        //te[st]
        //t[est]

        for (uint offset = 0; offset < max; offset++)
        {
            for (int length = 1; length <= max - offset; length++)
            {
                yield return new StringSegment(offset, length, Alignment.Right);
            }
        }
    }
}