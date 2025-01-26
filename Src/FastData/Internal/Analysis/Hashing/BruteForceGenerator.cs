using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Hashing;

internal class BruteForceGenerator : IHashGenerator
{
    public bool IsAppropriate(StringProperties stringProps) => true;

    public IEnumerable<StrRange> Generate(StringProperties stringProps)
    {
        uint max = Math.Max(stringProps.LengthData.Max, 8);

        for (uint i = 0; i < max - 1; i++)
        {
            for (uint j = 1; j < max; j++)
                yield return new StrRange(i, j);
        }
    }
}