using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class ConditionalStructure<TKey, TValue> : IStructure<TKey, TValue, ConditionalContext<TKey, TValue>>
{
    public ConditionalContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) => new ConditionalContext<TKey, TValue>(keys, values);

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}