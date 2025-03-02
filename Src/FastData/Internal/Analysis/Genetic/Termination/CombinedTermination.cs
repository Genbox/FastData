using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Termination;

internal sealed class CombinedTermination(ITermination[] conditions) : ITermination
{
    public bool ShouldTerminate(int evolutions, double fitness) => Array.Exists(conditions, x => x.ShouldTerminate(evolutions, fitness));
}