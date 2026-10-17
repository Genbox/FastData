using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Creates prefix and suffix edge-grams with lengths [1..max], where max is the smaller of the shortest encoded key length and the configured maximum.</summary>
internal sealed class EdgeGramGenerator(EdgeGramGeneratorConfig config) : ISegmentGenerator
{
    public bool IsAppropriate(StringKeyProperties props) => true;

    public IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        int len;

        int max = Math.Min(props.LengthData.MinByteLength, config.MaxSegmentLength); //We cannot segment above the shortest encoded byte string.

        //Generates:
        //[t]est
        //[te]st
        //[tes]t
        //[test]

        for (len = 1; len <= max; len++)
            yield return new ArraySegment(0, len, Alignment.Left);

        for (len = 1; len <= max; len++)
            yield return new ArraySegment(0, len, Alignment.Right);
    }
}