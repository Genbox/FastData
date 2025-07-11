using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public abstract class DefaultContext<T>(T[] data) : IContext<T>
{
    public T[] Data { get; } = data;
}