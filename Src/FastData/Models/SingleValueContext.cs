using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Models;

public class SingleValueContext(object item) : IContext
{
    public object Item { get; } = item;
}