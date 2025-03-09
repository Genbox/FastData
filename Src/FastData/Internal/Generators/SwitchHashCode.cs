using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchHashCode : IStructure
{
    public IContext Create(object[] data)
    {
        KeyValuePair<uint, object>[] hashCodes = new KeyValuePair<uint, object>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            object value = data[i];
            uint hash = HashHelper.HashObject(value);

            hashCodes[i] = new KeyValuePair<uint, object>(hash, value);
        }

        return new SwitchHashContext(data, hashCodes);
    }
}