using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal partial class BruteForceAnalyzer<T>
{
    [LoggerMessage(LogLevel.Trace, "Mixer: {Mixer}")]
    internal static partial void LogMixer(ILogger logger, string mixer);

    [LoggerMessage(LogLevel.Trace, "Avalanche: {Avalanche}")]
    internal static partial void LogAvalanche(ILogger logger, string avalanche);

    [LoggerMessage(LogLevel.Debug, "A new best: {Collisions} / {Fitness:0.00000}. Mixer: {Mixer}. Avalanche: {Avalanche}")]
    internal static partial void LogBetterCandidate(ILogger logger, double fitness, int collisions, string mixer, string avalanche);
}