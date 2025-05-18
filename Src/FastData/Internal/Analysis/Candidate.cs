using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

internal sealed class Candidate<T> where T : IStringHash
{
    internal T Spec { get; set; }
    internal double Fitness { get; set; } = 0;
    internal readonly Dictionary<string, object> Metadata = [];

    public Candidate(T spec) => Spec = spec;
    public Candidate() { }
}