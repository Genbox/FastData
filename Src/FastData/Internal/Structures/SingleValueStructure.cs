using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Structures;

internal sealed class SingleValueStructure : IStructure
{
    public bool TryCreate(object[] data, DataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
    {
        if (data.Length != 1)
        {
            context = null;
            return false;
        }

        context = new SingleValueContext(data[0]);
        return true;
    }
}