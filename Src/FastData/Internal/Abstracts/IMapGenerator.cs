using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IMapGenerator
{
    bool IsAppropriate(StringProperties stringProps);
    IEnumerable<StringSegment> Generate(StringProperties stringProps);
}