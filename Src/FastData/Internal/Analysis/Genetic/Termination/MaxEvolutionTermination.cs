using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Termination;

internal sealed class StagnationTermination(int maxGenerations) : ITermination
{
    private int _stagnentGens;
    private double _bestFitness;

    public bool ShouldTerminate(int evolutions, double fitness)
    {
        if (fitness > _bestFitness)
        {
            _bestFitness = fitness;
            _stagnentGens = 0;
        }
        else
            _stagnentGens++;

        return _stagnentGens >= maxGenerations;
    }
}