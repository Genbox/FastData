using System.Diagnostics;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class MinimalPerfectHashCode : IStructure
{
    public IContext Create(object[] data)
    {
        long timestamp = Stopwatch.GetTimestamp();

        //Find the proper seeds
        uint[] seed = MPHHelper.Generate(data, HashHelper.HashObjectSeed, 1, uint.MaxValue, data.Length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 60;
        }).ToArray(); //We call .ToArray() as FirstOrDefault() would return 0 (in the default case), which is a valid seed.

        KeyValuePair<object, uint>[] pairs = new KeyValuePair<object, uint>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            object value = data[i];

            uint hash = HashHelper.HashObjectSeed(value, seed[0]);
            uint index = (uint)(hash % pairs.Length);
            pairs[index] = new KeyValuePair<object, uint>(value, hash);
        }

        return new MinimalPerfectHashContext(pairs, seed[0]);
    }
}