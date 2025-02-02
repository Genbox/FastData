using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Internal.Analysis.Genetic.Operations;
using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[StructLayout(LayoutKind.Auto)]
internal struct GeneticHashSpec : IHashSpec
{
    internal int ExtractorSeed;

    internal int MixerSeed;
    internal int MixerIterations;

    internal int AvalancheSeed;
    internal int AvalancheIterations;

    internal uint Seed;
    internal StringSegment[] Segments;

    internal StringBuilder? HashString;

    public Func<string, uint> GetFunction()
    {
        // We create a hash function dynamically that runs A times different mix functions and B times avalanche functions.
        // Which functions that are run are determined on two seeds.

        //Select an extraction function. <accumulator, input, output>
        Func<uint, ushort, StringBuilder?, uint> extractFunc = Extractors.Ops[ExtractorSeed % Extractors.Ops.Length];

        //Now build the mixer
        Func<uint, StringBuilder?, uint> mixerFunc = Mixers.Ops[MixerSeed % Mixers.Ops.Length];
        Random mixerRng = new Random(MixerSeed);

        for (int i = 0; i < MixerIterations; i++)
        {
            int idx = mixerRng.Next(0, Mixers.Ops.Length);
            var currentFunc = Mixers.Ops[idx];
            var previousFunc = mixerFunc;
            mixerFunc = (x, s) => currentFunc(previousFunc(x, s), s);
        }

        Func<uint, StringBuilder?, uint> avalancheFunc = Mixers.Ops[AvalancheSeed % Mixers.Ops.Length];
        Random avalancheRng = new Random(AvalancheSeed);

        for (int i = 0; i < AvalancheIterations; i++)
        {
            int idx = avalancheRng.Next(0, Mixers.Ops.Length);
            var currentFunc = Mixers.Ops[idx];
            var previousFunc = avalancheFunc;
            avalancheFunc = (x, s) => currentFunc(previousFunc(x, s), s);
        }

        StringBuilder? s = HashString;
        uint seed = Seed;
        StringSegment[] segments = Segments;

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

    public string Construct() => throw new NotSupportedException("This method is not supported.");
}