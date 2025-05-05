using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public class PerfectHashBruteForceContext<T>(KeyValuePair<T, uint>[] data, uint seed) : IContext
{
    public KeyValuePair<T, uint>[] Data { get; } = data;
    public uint Seed { get; } = seed;
}