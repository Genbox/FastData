using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

public interface ISelection
{
    void Process(StaticArray<Entity> population, List<int> parents, int maxParents);
}