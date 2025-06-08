using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
internal sealed class BruteForceAnalyzerConfig : IAnalyzerConfig
{
    public int MaxAttempts { get; set; } = 157_464; //This is the actual number of attempts brute force currently makes. A higher value than this has no effect.
    public int MaxReturned { get; set; } = 10;
}