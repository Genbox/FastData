using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Misc;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "The generator search uses non-security-sensitive pseudo-randomness.")]
internal sealed class DefaultRandom(int seed = 0) : IRandom
{
#pragma warning disable S2245 // Pseudo-randomness is intentional for non-security-sensitive generator search.
    private readonly Random _random = seed == 0 ? new Random() : new Random(seed);
#pragma warning restore S2245

    public double NextDouble() => _random.NextDouble();
    public int Next() => _random.Next();
    public int Next(int max) => _random.Next(max);
    public int Next(int min, int max) => _random.Next(min, max);
}