using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public sealed class SingleValueContext<T>(T item) : IContext
{
    public T Item { get; } = item;
}