using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ConditionalStructure : IStructure
{
    public bool TryCreate(object[] data, DataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
    {
        if (data.Length > ushort.MaxValue)
        {
            context = null;
            return false;
        }

        context = new ConditionalContext(data);
        return true;
    }
}