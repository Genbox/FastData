using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Misc;

internal sealed class SharedRandom : IRandom
{
    private readonly Random _random = new Random();

    private SharedRandom() {}

    internal static SharedRandom Instance { get; } = new SharedRandom();

    public double NextDouble() => _random.NextDouble();
    public int Next() => _random.Next();
    public int Next(int max) => _random.Next(max);
    public int Next(int min, int max) => _random.Next(min, max);
}