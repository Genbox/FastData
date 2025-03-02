using System.Diagnostics;
using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Termination;

internal sealed class TimeBasedTermination(TimeSpan maxDuration) : ITermination
{
    private readonly long _startTime = Stopwatch.GetTimestamp();

    public bool ShouldTerminate(int evolutions, double fitness)
    {
        return new TimeSpan((Stopwatch.GetTimestamp() - _startTime) * Stopwatch.Frequency) >= maxDuration;
    }
}