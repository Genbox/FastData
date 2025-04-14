using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class ConvergenceTermination(int maxTopResults, double cutoffPercent) : ITermination
{
    private readonly double[] _recent = new double[maxTopResults];
    private int _count;

    public bool Process(int evolutions, double fitness)
    {
        _recent[evolutions % _recent.Length] = fitness;

        if (_count++ < _recent.Length)
            return false;

        double min = _recent.Min();
        double max = _recent.Max();

        return max - min <= cutoffPercent;
    }
}