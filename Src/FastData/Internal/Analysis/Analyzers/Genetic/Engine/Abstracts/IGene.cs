using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

internal interface IGene
{
    internal string Name { get; }
    void Mutate(IRandom rng);
    IGene Clone();
}