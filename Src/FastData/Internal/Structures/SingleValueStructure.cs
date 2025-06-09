using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class SingleValueStructure<T> : IStructure<T, SingleValueContext<T>>
{
    public SingleValueContext<T> Create(ReadOnlySpan<T> data)
    {
        if (data.Length != 1)
            throw new InvalidOperationException("SingleValueStructure can only be created from a span with exactly one element.");

        return new SingleValueContext<T>(data[0]);
    }
}