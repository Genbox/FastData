using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public class HashSetPerfectContext<T>(KeyValuePair<T, ulong>[] data, uint seed) : IContext
{
    public KeyValuePair<T, ulong>[] Data { get; } = data;
    public uint Seed { get; } = seed;
}