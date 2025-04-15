using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Contexts;

public class SingleValueContext(object item) : IContext
{
    public object Item { get; } = item;
}