using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashStructure
{
    bool TryCreate(object[] data, KnownDataType dataType, DataProperties props, FastDataConfig config, Func<object, uint> hash, out IContext? context);
    void RunSimulation<T>(object[] data, AnalyzerConfig config, ref Candidate<T> candidate) where T : struct, IHashSpec;
}