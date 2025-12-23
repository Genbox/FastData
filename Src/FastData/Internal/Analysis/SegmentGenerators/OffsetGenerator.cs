using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Returns segments with offset [0..n-1] and lengths [offset-n]</summary>
internal sealed class OffsetGenerator : ISegmentGenerator
{
    public bool IsAppropriate(StringKeyProperties props) => true;

    public IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        //Generates:
        //[test]
        //t[est]
        //te[st]
        //tes[t]

        for (uint offset = 0; offset < props.LengthData.LengthMap.Min; offset++)
        {
            yield return new ArraySegment(offset, -1, Alignment.Left);
        }
    }
}