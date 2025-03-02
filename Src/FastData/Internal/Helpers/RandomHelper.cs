namespace Genbox.FastData.Internal.Helpers;

internal static class RandomHelper
{
    private static readonly Random _rng = new Random();

    internal static double NextDouble() => _rng.NextDouble();

    public static int Next(int max) => _rng.Next(max);
}