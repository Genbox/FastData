using Genbox.FastData.Generator.Template.Misc;

namespace Genbox.FastData.Generator.Template.Extensions;

public static class MemoryExtensions
{
    public static MemoryObjectEnumerable<T> ToObjects<T>(this ReadOnlyMemory<T> keys) => new MemoryObjectEnumerable<T>(keys);
}