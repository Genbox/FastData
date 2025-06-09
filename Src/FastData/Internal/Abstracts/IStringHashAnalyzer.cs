using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Internal.Abstracts;

/// <summary>String hash analyzers try to find a hash function for a given set of strings.</summary>
internal interface IStringHashAnalyzer
{
    bool IsAppropriate();

    void GetCandidates(ReadOnlySpan<string> data, Func<Candidate, bool> onCandidateFound);
}