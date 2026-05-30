using System.Diagnostics;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

internal sealed class TimeBasedTermination(TimeSpan maxDuration) : ITermination
{
    private readonly long _startTime = Stopwatch.GetTimestamp();

    public bool Process(int evolutions, double fitness) => (Stopwatch.GetTimestamp() - _startTime) / (double)Stopwatch.Frequency >= maxDuration.TotalSeconds;
}