using Genbox.FastData.Internal.Analysis.Genetic;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashAnalyzer<T> where T : struct, IHashSpec
{
    Candidate<T> Run();
}