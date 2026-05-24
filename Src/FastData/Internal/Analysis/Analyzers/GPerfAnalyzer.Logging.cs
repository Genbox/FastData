using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal partial class GPerfAnalyzer
{
    [LoggerMessage(LogLevel.Trace, "{Message}", EventId = 42)]
    internal static partial void LogGPerfDebug(ILogger logger, string message);

    [LoggerMessage(LogLevel.Debug, "Positions: {Positions}")]
    internal static partial void LogPositions(ILogger logger, string positions);

    [LoggerMessage(LogLevel.Debug, "Alpha size: {Size}, Alpha increments: {Positions}")]
    internal static partial void LogAlphaPositions(ILogger logger, int size, string positions);

    [LoggerMessage(LogLevel.Debug, "Unable to find association table values")]
    internal static partial void LogUnableToFindAsso(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Generated association table values: {Values}")]
    internal static partial void LogAsso(ILogger logger, string values);

    [LoggerMessage(LogLevel.Debug, "Max hash: {MaxHash}")]
    internal static partial void LogMaxHash(ILogger logger, int maxHash);

    [LoggerMessage(LogLevel.Debug, "A new best: {Collisions} / {Fitness:0.00000}")]
    internal static partial void LogCandidate(ILogger logger, double fitness, int collisions);

    [LoggerMessage(LogLevel.Warning, "Size multiple {SizeMultiple} is excessive; this can create very large GPerf association tables.")]
    internal static partial void LogSizeMultipleExcessive(ILogger logger, double sizeMultiple);

    [LoggerMessage(LogLevel.Warning, "Size multiple {SizeMultiple} is extremely small; GPerf may need to expand the association table during search.")]
    internal static partial void LogSizeMultipleSmall(ILogger logger, double sizeMultiple);
}