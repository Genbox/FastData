using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashStructure
{
    IContext Create(object[] data, Func<object, uint> hash);
    void RunSimulation<T>(object[] data, AnalyzerConfig config, ref Candidate<T> candidate) where T : struct, IHashSpec;
}