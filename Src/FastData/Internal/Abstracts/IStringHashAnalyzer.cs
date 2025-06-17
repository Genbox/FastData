using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;

namespace Genbox.FastData.Internal.Abstracts;

/// <summary>String hash analyzers try to find a hash function for a given set of strings.</summary>
internal interface IStringHashAnalyzer<T> where T : notnull
{
    bool IsAppropriate();

    IEnumerable<Candidate> GetCandidates(ReadOnlySpan<T> data);
}