using Genbox.FastData.Configs;

namespace Genbox.FastData;

public class GeneticAnalyzerConfig : IAnalyzerConfig
{
    public bool ShuffleParents { get; set; }

    public int PopulationSize { get; set; } = 100;

    /// <summary>
    /// Set to 0 to use a new seed each run
    /// </summary>
    public int RandomSeed { get; set; }
}