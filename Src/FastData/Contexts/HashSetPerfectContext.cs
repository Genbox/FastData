using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public sealed class HashSetPerfectContext<T>(KeyValuePair<T, ulong>[] data) : IContext
{
    public KeyValuePair<T, ulong>[] Data { get; } = data;
}