using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

/// <summary>Returns segments with offset [0..n-1] and lengths [offset-n]</summary>
internal class OffsetGenerator : ISegmentGenerator
{
    public bool IsAppropriate(StringProperties props) => true;

    public IEnumerable<StringSegment> Generate(StringProperties props)
    {
        for (int offset = 0; offset < props.LengthData.Min; offset++)
            yield return new StringSegment(offset, -1, Alignment.Left);
    }
}