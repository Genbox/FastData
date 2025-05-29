using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

internal partial class GeneticEngine
{
    [LoggerMessage(LogLevel.Debug, "A new best: {Collisions} / {Fitness:0.00000}. Genes: {Genes}")]
    internal static partial void LogBetterCandidate(ILogger logger, double fitness, int collisions, string genes);

    [LoggerMessage(LogLevel.Trace, "Generation: {Generation}")]
    internal static partial void LogGeneration(ILogger logger, int generation);
}