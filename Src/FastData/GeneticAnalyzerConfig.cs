using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData;

[PublicAPI]
public sealed class GeneticAnalyzerConfig : IAnalyzerConfig
{
    public bool ShuffleParents { get; set; }

    public int PopulationSize { get; set; } = 32;

    public int MaxGenerations { get; set; } = 10;

    /// <summary>Set to 0 to use a new seed each run</summary>
    public int RandomSeed { get; set; }
}