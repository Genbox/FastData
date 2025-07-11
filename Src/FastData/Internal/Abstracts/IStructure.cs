using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure<in TKey, in TValue, out TContext> where TContext : IContext
{
    TContext Create(TKey[] keys, TValue[]? values);
}