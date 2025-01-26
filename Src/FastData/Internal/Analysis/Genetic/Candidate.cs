using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[StructLayout(LayoutKind.Auto)]
internal struct Candidate(HashSpec spec)
{
    internal HashSpec Spec = spec;
    internal double Fitness = 0;
    internal (string, object)[] Metadata = [];
}