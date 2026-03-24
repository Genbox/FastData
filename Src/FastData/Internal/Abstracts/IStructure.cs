using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

public interface IStructure;

internal interface IStructure<TKey, TValue, out TContext> : IStructure where TContext : IContext
{
    TContext Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values);

    IEnumerable<IEarlyExit> GetMandatoryExits();
}