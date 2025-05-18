using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

internal sealed class Candidate<T> where T : IStringHash
{
    internal T Spec { get; set; }
    internal double Fitness { get; set; } = 0;
    internal readonly Dictionary<string, object> Metadata = [];

    public Candidate(T spec) => Spec = spec;
    public Candidate() { }

    public override bool Equals(object? obj) => obj is Candidate<T> other && Equals(other);
    public bool Equals(Candidate<T> other) => Spec.Equals(other.Spec) && Fitness.Equals(other.Fitness);

    [SuppressMessage("Correctness", "SS008:GetHashCode() refers to mutable or static member")]
    public override int GetHashCode() => Spec.GetHashCode() ^ Fitness.GetHashCode();
}