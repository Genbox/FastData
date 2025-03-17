using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ConditionalCode : IStructure
{
    public bool TryCreate(object[] data, KnownDataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
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