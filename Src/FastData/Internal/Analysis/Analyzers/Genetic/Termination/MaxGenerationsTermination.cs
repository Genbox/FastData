using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class MaxGenerationsTermination(int maxGenerations) : ITermination
{
    public bool Process(int evolutions, double fitness) => evolutions >= maxGenerations;
}