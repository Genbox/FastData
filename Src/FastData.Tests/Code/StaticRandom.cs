using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Tests.Code;

/// <summary>Always returns 1 in all calls. Used in tests.</summary>
internal sealed class StaticRandom : IRandom
{
    private StaticRandom() {}

    internal static StaticRandom Instance { get; } = new StaticRandom();

    public double NextDouble() => 1;
    public int Next() => 1;
    public int Next(int max) => 1;
    public int Next(int min, int max) => 1;
}