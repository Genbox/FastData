using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Internal.Abstracts;

/// <summary>
/// The job of an analyzer is to reduce O(n) operations to something simpler in hashing operations.
/// It does so by analyzing a array for entropy and selecting enough of the array, such that a hash of the resulting
/// segments is unique (or only has a few collisions).
/// </summary>
internal interface IHashAnalyzer<T> where T : IStringHash
{
    /// <summary>
    /// Runs the analyzer. Data is usually provided in the ctor.
    /// </summary>
    /// <returns>A candidate that specifies the fitness of the analysis. A fitness of 1 means no collisions. A fitness of 0 means all items collide.</returns>
    Candidate<T> Run();
}