namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

internal class GeneticEngineConfig
{
    public int PopulationSize { get; set; } = 100;
    public bool ShuffleParents { get; set; } = false;
    public int MaxReturned { get; set; } = 10;
}