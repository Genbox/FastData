using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

internal interface ISelection
{
    void Process(StaticArray<Entity> population, List<int> parents, int maxParents);
}