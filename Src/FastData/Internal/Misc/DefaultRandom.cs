using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Misc;

internal sealed class DefaultRandom(int seed = 0) : IRandom
{
    private readonly Random _random = seed == 0 ? new Random() : new Random(seed);

    public double NextDouble() => _random.NextDouble();
    public int Next() => _random.Next();
    public int Next(int max) => _random.Next(max);
    public int Next(int min, int max) => _random.Next(min, max);
}