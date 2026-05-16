using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

public interface IStructure
{
    IEnumerable<IEarlyExit> GetMandatoryExits();
}

internal interface IStructure<TKey, TValue, out TContext> : IStructure where TContext : class, IContext
{
    TContext? Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values);
}