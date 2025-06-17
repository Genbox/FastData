using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal partial class GeneticAnalyzer<T>
{
    [LoggerMessage(LogLevel.Debug, "Number of segments to explore: {Count}")]
    internal static partial void LogSegmentCount(ILogger logger, int count);

    [LoggerMessage(LogLevel.Debug, "A new best: {Collisions} / {Fitness:0.00000}. Expression: {Expression}")]
    internal static partial void LogBetterCandidate(ILogger logger, double fitness, int collisions, string expression);
}