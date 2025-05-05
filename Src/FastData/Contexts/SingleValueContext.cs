using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public class SingleValueContext<T>(T item) : IContext
{
    public T Item { get; } = item;
}