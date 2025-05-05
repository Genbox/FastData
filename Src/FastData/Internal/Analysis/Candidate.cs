using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal struct Candidate<T>(in T spec) where T : struct, IHashSpec
{
    internal T Spec = spec;
    internal double Fitness = 0;
    internal readonly Dictionary<string, object> Metadata = [];

    public readonly override bool Equals(object? obj) => obj is Candidate<T> other && Equals(other);
    public readonly bool Equals(Candidate<T> other) => Spec.Equals(other.Spec) && Fitness.Equals(other.Fitness);

    [SuppressMessage("Correctness", "SS008:GetHashCode() refers to mutable or static member")]
    public readonly override int GetHashCode() => Spec.GetHashCode() ^ Fitness.GetHashCode();
}