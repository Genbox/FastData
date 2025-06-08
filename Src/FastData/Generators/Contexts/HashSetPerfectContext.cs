using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class HashSetPerfectContext<T>(KeyValuePair<T, ulong>[] data) : IContext
{
    public KeyValuePair<T, ulong>[] Data { get; } = data;
}