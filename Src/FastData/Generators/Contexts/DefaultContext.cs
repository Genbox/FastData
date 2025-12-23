using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public abstract class DefaultContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) : IContext<TValue>
{
    public ReadOnlyMemory<TKey> Keys { get; } = keys;
    public ReadOnlyMemory<TValue> Values { get; } = values;
}