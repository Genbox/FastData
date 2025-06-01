using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Abstracts;

internal interface ISegmentGenerator
{
    bool IsAppropriate(StringProperties props);
    IEnumerable<ArraySegment> Generate(StringProperties props);
}