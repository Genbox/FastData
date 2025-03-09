using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashAnalyzer<T> where T : struct, IHashSpec
{
    Candidate<T> Run();
}