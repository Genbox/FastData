using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure<T, out TContext> where TContext : IContext<T>
{
    TContext Create(ref ReadOnlySpan<T> data);
}