using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class EytzingerSearchCode : IStructure
{
    public bool TryCreate(object[] data, KnownDataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
    {
        if (dataType == KnownDataType.String)
            Array.Sort(data, StringHelper.GetStringComparer(config.StringComparison));
        else
            Array.Sort(data);

        object[] output = new object[data.Length];
        int index = 0;
        EytzingerOrder(ref index, data, output);

        context = new EytzingerSearchContext(output);
        return true;
    }

    private static void EytzingerOrder(ref int arrIdx, object[] data, object[] output, int eytIdx = 0)
    {
        if (eytIdx < data.Length)
        {
            EytzingerOrder(ref arrIdx, data, output, (2 * eytIdx) + 1);
            output[eytIdx] = data[arrIdx++];
            EytzingerOrder(ref arrIdx, data, output, (2 * eytIdx) + 2);
        }
    }
}