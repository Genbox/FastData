using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Internal.Abstracts;

internal interface ISegmentGenerator
{
    bool IsAppropriate(StringProperties props);
    IEnumerable<StringSegment> Generate(StringProperties props);
}