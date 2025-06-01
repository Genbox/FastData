using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Creates edge-grams with offsets of 0 and lengths [1..max]</summary>
internal sealed class EdgeGramGenerator(int maxLength) : ISegmentGenerator
{
    public bool IsAppropriate(StringProperties props) => true;

    public IEnumerable<ArraySegment> Generate(StringProperties props)
    {
        int len;

        int max = (int)Math.Min(props.LengthData.Min, maxLength); //We cannot segment above the shortest string.

        //Generates:
        //[t]est
        //[te]st
        //[tes]t
        //[test]

        for (len = 1; len <= max; len++)
        {
            yield return new ArraySegment(0, len, Alignment.Left);
        }

        for (len = 1; len <= max; len++)
        {
            yield return new ArraySegment(0, len, Alignment.Right);
        }
    }
}