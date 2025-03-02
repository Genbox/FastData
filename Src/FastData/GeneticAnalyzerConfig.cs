using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData;

public class GeneticAnalyzerConfig : AnalyzerConfig
{
    public int PopulationSize { get; set; } = 10;

    public bool CrossEliteOnly { get; set; } = true;
    public float CrossPercent { get; set; } = 0.25f;

    internal IReinsertion Reinsertion { get; set; }
    internal IMutation Mutation { get; set; }
    internal ICrossOver CrossOver { get; set; }
    internal ISelection Selection { get; set; }
    internal ITermination Termination { get; set; }
}