using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

[DebuggerDisplay("{Value}")]
internal sealed class IntGene(string name, int value, int min = int.MinValue, int max = int.MaxValue) : Gene<int>(name, value)
{
    public override void Mutate(IRandom rng) => Value = rng.Next(min, max);
    public override IGene Clone() => new IntGene(Name, Value, min, max);
}