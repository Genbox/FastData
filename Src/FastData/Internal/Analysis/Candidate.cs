using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal struct Candidate<T>(in T spec) where T : struct, IHashSpec
{
    internal T Spec = spec;
    internal double Fitness = 0;
    internal Dictionary<string, object> Metadata = [];
}