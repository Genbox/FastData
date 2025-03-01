using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal struct Candidate<T>(in T spec) where T : struct, IHashSpec
{
    internal T Spec = spec;
    internal double Fitness = 0;
    internal (string, object)[] Metadata = [];
}