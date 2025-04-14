using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

internal interface IReinsertion
{
    void Process(StaticArray<Entity> population, StaticArray<Entity> newPopulation);
}