using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Internal.Abstracts;

/// <summary>String hash analyzers try to find a hash function for a given set of strings.</summary>
internal interface IStringHashAnalyzer
{
    bool IsAppropriate();

    IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data);
}