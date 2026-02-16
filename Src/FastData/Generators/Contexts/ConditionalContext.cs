using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class ConditionalContext<TKey,TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) : IContext
{
    public ReadOnlyMemory<TKey> Keys { get; } = keys;
    public ReadOnlyMemory<TValue> Values { get; } = values;
}