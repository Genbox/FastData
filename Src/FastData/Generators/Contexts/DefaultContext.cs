using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public abstract class DefaultContext<TKey, TValue>(TKey[] keys, TValue[]? values) : IContext<TValue>
{
    public TKey[] Keys { get; } = keys;
    public TValue[]? Values { get; } = values;
}