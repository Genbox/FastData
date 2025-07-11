using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure<in T, out TContext> where TContext : IContext<T>
{
    TContext Create(T[] data);
}