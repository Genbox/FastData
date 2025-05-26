using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Abstracts;

/// <summary>
/// String hash analyzers try to find a hash function for a given set of strings.
/// </summary>
internal interface IStringHashAnalyzer
{
    bool IsAppropriate();

    /// <summary>Runs the analyzer</summary>
    /// <returns>A set of good candidates.</returns>
    IEnumerable<Candidate> GetCandidates();
}