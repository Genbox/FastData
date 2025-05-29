using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

internal abstract class Gene<T>(string name, T value) : IGene
{
    internal T Value { get; set; } = value;
    public string Name { get; } = name;
    public abstract void Mutate(IRandom rng);
    public abstract IGene Clone();

    public override string ToString() => $"{Name}: {Value}";
}