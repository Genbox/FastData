using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ConditionalStructure<T> : IStructure<T, ConditionalContext<T>>
{
    public ConditionalContext<T> Create(ref ReadOnlySpan<T> data)
    {
        return new ConditionalContext<T>();
    }
}