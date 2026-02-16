using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure<TKey, TValue, out TContext> where TContext : IContext
{
    TContext Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values);
}