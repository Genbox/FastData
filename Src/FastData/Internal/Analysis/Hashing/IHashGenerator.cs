using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Hashing;

internal interface IHashGenerator
{
    bool IsAppropriate(StringProperties stringProps);
    IEnumerable<StrRange> Generate(StringProperties stringProps);
}