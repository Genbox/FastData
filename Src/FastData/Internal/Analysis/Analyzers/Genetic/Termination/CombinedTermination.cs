using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class CombinedTermination(ITermination[] conditions) : ITermination
{
    public bool Process(int evolutions, double fitness)
    {
        foreach (ITermination condition in conditions)
        {
            if (condition.Process(evolutions, fitness))
                return true;
        }

        return false;
    }
}