using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure<in TKey, TValue, out TContext> where TContext : IContext<TValue>
{
    TContext Create(TKey[] keys, ValueSpec<TValue>? valueSpec);
}