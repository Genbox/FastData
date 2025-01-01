namespace Genbox.FastData.Benchmarks.Code;

public readonly record struct MixSpec(string Name, Func<uint, uint, uint> Function)
{
    public override string ToString() => Name;
}