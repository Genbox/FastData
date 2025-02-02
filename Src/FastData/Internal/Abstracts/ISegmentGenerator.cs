using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Abstracts;

internal interface ISegmentGenerator
{
    bool IsAppropriate(StringProperties props);
    IEnumerable<StringSegment> Generate(StringProperties props);
}