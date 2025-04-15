using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class EytzingerSearchStructure(StructureConfig config) : IStructure
{
    public bool TryCreate(object[] data, out IContext? context)
    {
        //We make a copy to avoid altering the original data
        object[] copy = new object[data.Length];
        data.CopyTo(copy, 0);

        if (config.DataProperties.DataType == DataType.String)
            Array.Sort(copy, config.GetStringComparer());
        else
            Array.Sort(copy);

        object[] output = new object[copy.Length];
        int index = 0;
        EytzingerOrder(ref index, copy, output);

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