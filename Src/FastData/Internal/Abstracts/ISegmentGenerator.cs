using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Abstracts;

internal interface ISegmentGenerator
{
    bool IsAppropriate(StringKeyProperties props);
    IEnumerable<ArraySegment> Generate(StringKeyProperties props);
}