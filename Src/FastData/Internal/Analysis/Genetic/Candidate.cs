using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[StructLayout(LayoutKind.Auto)]
internal struct Candidate<T>(in T spec) where T : struct, IHashSpec
{
    internal T Spec = spec;
    internal double Fitness = 0;
    internal (string, object)[] Metadata = [];
}