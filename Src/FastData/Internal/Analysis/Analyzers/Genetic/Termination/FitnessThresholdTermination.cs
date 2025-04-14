using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class FitnessThresholdTermination(double fitnessThreshold) : ITermination
{
    public bool Process(int evolutions, double fitness) => fitness >= fitnessThreshold;
}