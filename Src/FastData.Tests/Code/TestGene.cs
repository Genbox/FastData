using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Tests.Code;

internal class TestGene(string name) : Gene<string?>(name, null)
{
    public override void Mutate(IRandom rng) => throw new NotSupportedException();
    public override IGene Clone() => new TestGene(Name);
}