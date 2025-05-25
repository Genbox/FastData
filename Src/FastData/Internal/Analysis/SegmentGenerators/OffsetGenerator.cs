using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Returns segments with offset [0..n-1] and lengths [offset-n]</summary>
internal class OffsetGenerator : ISegmentGenerator
{
    public bool IsAppropriate(StringProperties props) => true;

    public IEnumerable<ArraySegment> Generate(StringProperties props)
    {
        //Generates:
        //[test]
        //t[est]
        //te[st]
        //tes[t]

        for (uint offset = 0; offset < props.LengthData.Min; offset++)
        {
            yield return new ArraySegment(offset, -1, Alignment.Left);
        }
    }
}