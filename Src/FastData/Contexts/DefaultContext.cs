using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public abstract class DefaultContext(object[] data) : IContext
{
    public object[] Data { get; } = data;
}