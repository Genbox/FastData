using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class EytzingerSearchStructure<T>(StructureConfig<T> config) : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        //We make a copy to avoid altering the original data
        T[] copy = new T[data.Length];
        data.CopyTo(copy, 0);

        if (config.DataProperties.DataType == DataType.String)
            Array.Sort(copy, config.GetStringComparer());
        else
            Array.Sort(copy);

        T[] output = new T[copy.Length];
        int index = 0;
        EytzingerOrder(ref index, copy, output);

        context = new EytzingerSearchContext<T>(output);
        return true;
    }

    private static void EytzingerOrder(ref int arrIdx, T[] data, T[] output, int eytIdx = 0)
    {
        if (eytIdx < data.Length)
        {
            EytzingerOrder(ref arrIdx, data, output, (2 * eytIdx) + 1);
            output[eytIdx] = data[arrIdx++];
            EytzingerOrder(ref arrIdx, data, output, (2 * eytIdx) + 2);
        }
    }
}