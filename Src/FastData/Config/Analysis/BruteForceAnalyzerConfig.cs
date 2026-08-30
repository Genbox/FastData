using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

/// <summary>Configures exhaustive search for string-hash candidates.</summary>
[PublicAPI]
public sealed class BruteForceAnalyzerConfig : IAnalyzerConfig
{
    /// <summary>Gets or sets the maximum number of candidates to evaluate. Values above 157,464 currently have no additional effect.</summary>
    public int MaxAttempts { get; set; } = 157_464; //This is the actual number of attempts brute force currently makes. A higher value than this has no effect.

    /// <summary>Gets or sets the maximum number of highest-scoring candidates to return.</summary>
    public int MaxReturned { get; set; } = 10;
}