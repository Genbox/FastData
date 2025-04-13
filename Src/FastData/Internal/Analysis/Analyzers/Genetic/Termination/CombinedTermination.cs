using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class CombinedTermination(ITermination[] conditions) : ITermination
{
    public bool Process(int evolutions, double fitness) => Array.Exists(conditions, x => x.Process(evolutions, fitness));
}