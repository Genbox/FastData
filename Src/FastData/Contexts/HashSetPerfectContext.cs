using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public class HashSetPerfectContext<T>(KeyValuePair<T, ulong>[] data) : IContext
{
    public KeyValuePair<T, ulong>[] Data { get; } = data;
}