using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Returns segments with offset [0..max-1] and length [1..max-1]</summary>
internal sealed class BruteForceGenerator(BruteForceGeneratorConfig config) : ISegmentGenerator
{
    public bool IsAppropriate(StringKeyProperties props) => true;

    public IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        int max = Math.Min(props.LengthData.MinByteLength, config.MaxSegmentLength); //We cannot segment above the shortest encoded byte string.

        for (uint offset = 0; offset < max; offset++)
        {
            for (int length = 1; length <= max - offset; length++)
            {
                //Generates paired prefix/suffix candidates:
                //[t]est, tes[t]
                //[te]st, te[st]
                //[tes]t, t[est]
                yield return new ArraySegment(offset, length, Alignment.Left);
                yield return new ArraySegment(offset, length, Alignment.Right);
            }
        }
    }
}