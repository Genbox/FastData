using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal partial class GeneticAnalyzer
{
    [LoggerMessage(LogLevel.Debug, "Number of segments to explore: {Count}")]
    internal static partial void LogSegmentCount(ILogger logger, int count);
}