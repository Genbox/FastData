using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

internal interface IMutation
{
    void Process(StaticArray<Entity> population);
}