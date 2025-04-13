using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure
{
    bool TryCreate(object[] data, DataType dataType, DataProperties props, FastDataConfig config, out IContext? context);
}