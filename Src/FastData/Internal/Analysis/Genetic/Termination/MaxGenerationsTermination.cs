using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Termination;

internal sealed class MaxGenerationsTermination(int maxGenerations) : ITermination
{
    public bool ShouldTerminate(int evolutions, double fitness) => evolutions >= maxGenerations;
}