using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashStructure
{
    bool TryCreate(object[] data, KnownDataType dataType, DataProperties props, FastDataConfig config, Func<object, uint> hash, out IContext? context);

    double[] Emulate(object[] data, uint capacity, HashFunc hashFunc, EqualFunc equalFunc);
}