using System.Diagnostics;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Structures;

internal sealed class PerfectHashBruteForceStructure : IStructure
{
    public bool TryCreate(object[] data, DataType dataType, DataProperties props, FastDataConfig config, out IContext? context)
    {
        long timestamp = Stopwatch.GetTimestamp();

        //Find the proper seeds
        uint[] seed = PerfectHashHelper.Generate(data, HashHelper.HashObjectSeed, 1, uint.MaxValue, data.Length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 10;
        }).ToArray(); //We call .ToArray() as FirstOrDefault() would return 0 (in the default case), which is a valid seed.

        // If we have 0 seeds, it means either there is no solution, or we hit the exit condition
        if (seed.Length == 0)
        {
            context = null;
            return false;
        }

        KeyValuePair<object, uint>[] pairs = new KeyValuePair<object, uint>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            object value = data[i];

            uint hash = HashHelper.HashObjectSeed(value, seed[0]);
            uint index = (uint)(hash % pairs.Length);
            pairs[index] = new KeyValuePair<object, uint>(value, hash);
        }

        context = new PerfectHashBruteForceContext(pairs, seed[0]);
        return true;
    }
}