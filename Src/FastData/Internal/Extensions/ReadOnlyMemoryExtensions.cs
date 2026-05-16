namespace Genbox.FastData.Internal.Extensions;

internal static class ReadOnlyMemoryExtensions
{
    internal static bool IsSorted<T>(this ReadOnlyMemory<T> keys)
    {
        ReadOnlySpan<T> span = keys.Span;

        for (int i = 1; i < span.Length; i++)
        {
            if (Comparer<T>.Default.Compare(span[i - 1], span[i]) > 0)
                return false;
        }

        return true;
    }
}