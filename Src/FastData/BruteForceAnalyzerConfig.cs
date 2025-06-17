using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData;

[PublicAPI]
public sealed class BruteForceAnalyzerConfig : IAnalyzerConfig
{
    public int MaxAttempts { get; set; } = 157_464; //This is the actual number of attempts brute force currently makes. A higher value than this has no effect.
    public int MaxReturned { get; set; } = 10;
}