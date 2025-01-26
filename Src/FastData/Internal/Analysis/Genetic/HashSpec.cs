using System.Runtime.InteropServices;
using System.Text;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[StructLayout(LayoutKind.Auto)]
internal struct HashSpec
{
    internal int ExtractorSeed;

    internal int MixerSeed;
    internal int MixerIterations;

    internal int AvalancheSeed;
    internal int AvalancheIterations;

    internal uint Seed;
    internal StringSegment[] Segments;

    internal StringBuilder? HashString;
}