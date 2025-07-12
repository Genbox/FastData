using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public abstract class DefaultContext<TKey, TValue>(TKey[] keys, ValueSpec<TValue>? valueSpec) : IContext<TValue>
{
    public TKey[] Keys { get; } = keys;
    public ValueSpec<TValue>? ValueSpec { get; } = valueSpec;
}