using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

internal class BruteForceGenerator : IMapGenerator
{
    public bool IsAppropriate(StringProperties stringProps) => true;

    public IEnumerable<StringSegment> Generate(StringProperties stringProps)
    {
        uint max = Math.Max(stringProps.LengthData.Max, 8);

        for (int i = 0; i < max - 1; i++)
        {
            for (int j = 1; j < max; j++)
                yield return new StringSegment(i, j, Alignment.Unknown);
        }
    }
}