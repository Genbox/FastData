using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Misc;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "The generator search uses non-security-sensitive pseudo-randomness.")]
internal sealed class SharedRandom : IRandom
{
#pragma warning disable S2245 // Pseudo-randomness is intentional for non-security-sensitive generator search.
    private readonly Random _random = new Random();
#pragma warning restore S2245

    private SharedRandom() {}

    internal static SharedRandom Instance { get; } = new SharedRandom();

    public double NextDouble() => _random.NextDouble();
    public int Next() => _random.Next();
    public int Next(int max) => _random.Next(max);
    public int Next(int min, int max) => _random.Next(min, max);
}