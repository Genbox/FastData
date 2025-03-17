using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Models;

public class MinimalPerfectHashContext(KeyValuePair<object, uint>[] data, uint seed) : IContext
{
    public KeyValuePair<object, uint>[] Data { get; } = data;
    public uint Seed { get; } = seed;
}