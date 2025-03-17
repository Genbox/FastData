using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class BinarySearchCode : IStructure
{
    public bool TryCreate(object[] data, KnownDataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
    {
        if (dataType == KnownDataType.String)
            Array.Sort(data, StringHelper.GetStringComparer(config.StringComparison));
        else
            Array.Sort(data);

        context = new BinarySearchContext(data);
        return true;
    }
}