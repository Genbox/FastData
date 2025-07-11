using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ConditionalStructure<TKey, TValue> : IStructure<TKey, TValue, ConditionalContext<TKey, TValue>>
{
    public ConditionalContext<TKey, TValue> Create(TKey[] data, TValue[]? values)
    {
        return new ConditionalContext<TKey, TValue>(data, values);
    }
}