using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class EytzingerSearchStructure<T>(DataType dataType, StringComparison comparison) : IStructure<T, EytzingerSearchContext<T>>
{
    public EytzingerSearchContext<T> Create(ReadOnlySpan<T> data)
    {
        //We make a copy to avoid altering the original data
        T[] copy = new T[data.Length];
        data.CopyTo(copy);

        if (dataType == DataType.String)
            Array.Sort(copy, StringHelper.GetStringComparer(comparison));
        else
            Array.Sort(copy);

        T[] output = new T[copy.Length];
        int index = 0;
        EytzingerOrder(ref index, copy, output);

        return new EytzingerSearchContext<T>(output);
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