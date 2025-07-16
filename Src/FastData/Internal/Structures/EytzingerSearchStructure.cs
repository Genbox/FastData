using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class EytzingerSearchStructure<TKey, TValue>(DataType dataType, StringComparison comparison) : IStructure<TKey, TValue, EytzingerSearchContext<TKey, TValue>>
{
    public EytzingerSearchContext<TKey, TValue> Create(TKey[] data, TValue[]? values)
    {
        //We make a copy to avoid altering the original data
        TKey[] copy = new TKey[data.Length];
        data.CopyTo(copy, 0);

        if (dataType == DataType.String)
            Array.Sort(copy, StringHelper.GetStringComparer(comparison));
        else
            Array.Sort(copy);

        TKey[] output = new TKey[copy.Length];
        int index = 0;
        EytzingerOrder(ref index, copy, output);

        return new EytzingerSearchContext<TKey, TValue>(output, values);
    }

    private static void EytzingerOrder(ref int arrIdx, TKey[] data, TKey[] output, int eytIdx = 0)
    {
        if (eytIdx < data.Length)
        {
            EytzingerOrder(ref arrIdx, data, output, (2 * eytIdx) + 1);
            output[eytIdx] = data[arrIdx++];
            EytzingerOrder(ref arrIdx, data, output, (2 * eytIdx) + 2);
        }
    }
}