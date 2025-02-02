using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genbox.FastData.Internal.Analysis.Genetic.Operations;
using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
internal static class HashHelper
{
    internal static Func<string, uint> GetHashFunction(ref HashSpec hashSpec)
    {
        // We create a hash function dynamically that runs A times different mix functions and B times avalanche functions.
        // Which functions that are run are determined on two seeds.

        //Select an extraction function. <accumulator, input, output>
        Func<uint, ushort, StringBuilder?, uint> extractFunc = Extractors.Ops[hashSpec.ExtractorSeed % Extractors.Ops.Length];

        //Now build the mixer
        Func<uint, StringBuilder?, uint> mixerFunc = Mixers.Ops[hashSpec.MixerSeed % Mixers.Ops.Length];
        Random mixerRng = new Random(hashSpec.MixerSeed);

        for (int i = 0; i < hashSpec.MixerIterations; i++)
        {
            int idx = mixerRng.Next(0, Mixers.Ops.Length);
            var currentFunc = Mixers.Ops[idx];
            var previousFunc = mixerFunc;
            mixerFunc = (x, s) => currentFunc(previousFunc(x, s), s);
        }

        Func<uint, StringBuilder?, uint> avalancheFunc = Mixers.Ops[hashSpec.AvalancheSeed % Mixers.Ops.Length];
        Random avalancheRng = new Random(hashSpec.AvalancheSeed);

        for (int i = 0; i < hashSpec.AvalancheIterations; i++)
        {
            int idx = avalancheRng.Next(0, Mixers.Ops.Length);
            var currentFunc = Mixers.Ops[idx];
            var previousFunc = avalancheFunc;
            avalancheFunc = (x, s) => currentFunc(previousFunc(x, s), s);
        }

        StringBuilder? s = hashSpec.HashString;
        uint seed = hashSpec.Seed;
        StringSegment[] segments = hashSpec.Segments;

        return x => avalancheFunc(Hash(x, (acc, aChar) => mixerFunc(extractFunc(acc, aChar, s), s), seed, segments), s);
    }

    private static uint Hash(string x, Func<uint, ushort, uint> round, uint seed, StringSegment[] segments)
    {
        uint acc = seed;
        foreach (char c in GetCharacters(x, segments))
            acc = round(acc, c);
        return acc;
    }

    private static IEnumerable<char> GetCharacters(string str, StringSegment[] segments)
    {
        foreach (StringSegment segment in segments)
        {
            for (int i = segment.Offset; i < /*segment*/str.Length; i++)
                yield return str[i];
        }
    }
}