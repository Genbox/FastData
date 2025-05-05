using Genbox.FastData.Specs;

namespace Genbox.FastData.Benchmarks.Code;

public readonly record struct MixSpec(string Name, HashFunc<uint> Function)
{
    public override string ToString() => Name;
}