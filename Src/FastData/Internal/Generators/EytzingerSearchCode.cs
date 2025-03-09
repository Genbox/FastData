using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class EytzingerSearchCode : IStructure
{
    public IContext Create(object[] data)
    {
        Array.Sort(data, StringComparer.Ordinal);

        object[] output = new object[data.Length];
        int index = 0;
        EytzingerOrder(ref index);

        return new EytzingerSearchContext(output);

        void EytzingerOrder(ref int arrIdx, int eytIdx = 0)
        {
            if (eytIdx < data.Length)
            {
                EytzingerOrder(ref arrIdx, (2 * eytIdx) + 1);
                output[eytIdx] = data[arrIdx++];
                EytzingerOrder(ref arrIdx, (2 * eytIdx) + 2);
            }
        }
    }
}