using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class SingleValueContext<T>(T item) : IContext
{
    public T Item { get; } = item;
}