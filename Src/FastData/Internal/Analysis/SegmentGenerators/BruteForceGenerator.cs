using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Returns segments with offset [0..max-1] and length [1..max-1]</summary>
internal sealed class BruteForceGenerator(int maxLength) : ISegmentGenerator
{
    public bool IsAppropriate(StringKeyProperties props) => true;

    public IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        int max = (int)Math.Min(props.LengthData.LengthMap.Min, maxLength); //We cannot segment above the shortest string.

        //Generates:
        //[t]est
        //[te]st
        //[tes]t

        for (uint offset = 0; offset < max; offset++)
        {
            for (int length = 1; length <= max - offset; length++)
            {
                yield return new ArraySegment(offset, length, Alignment.Left);
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
                yield return new ArraySegment(offset, length, Alignment.Right);
            }
        }
    }
}