using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Termination;

internal sealed class FitnessThresholdTermination(double fitnessThreshold) : ITermination
{
    public bool ShouldTerminate(int evolutions, double fitness) => fitness >= fitnessThreshold;
}