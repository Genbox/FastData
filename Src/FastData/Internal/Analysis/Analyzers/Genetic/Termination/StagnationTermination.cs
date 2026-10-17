using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class StagnationTermination(int maxGenerations) : ITermination
{
    private double _bestFitness;
    private bool _hasFitness;
    private int _stagnentGens;

    public bool Process(int evolutions, double fitness)
    {
        if (!_hasFitness || fitness > _bestFitness)
        {
            _hasFitness = true;
            _bestFitness = fitness;
            _stagnentGens = 0;
        }
        else
            _stagnentGens++;

        return _stagnentGens >= maxGenerations;
    }
}