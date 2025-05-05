using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public abstract class DefaultContext<T>(T[] data) : IContext
{
    public T[] Data { get; } = data;
}