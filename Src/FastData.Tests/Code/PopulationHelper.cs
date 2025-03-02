using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;

namespace Genbox.FastData.Tests.Code;

internal static class GeneticHelper
{
    internal static Candidate<GeneticHashSpec>[] GeneratePopulation(int size, double minFitness, double maxFitness)
    {
        var rand = new Random(42);
        return Enumerable.Range(0, size)
                         .Select(i => new Candidate<GeneticHashSpec> { Fitness = rand.NextDouble() * (maxFitness - minFitness) + minFitness })
                         .ToArray();
    }
}