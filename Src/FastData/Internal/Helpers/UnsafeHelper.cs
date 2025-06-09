namespace Genbox.FastData.Internal.Helpers;

internal static class UnsafeHelper
{
    internal static unsafe ReadOnlySpan<TTo> ConvertSpan<TFrom, TTo>(ReadOnlySpan<TFrom> span)
    {
        // Can't do this yet as it requires struct generic constraints.
        // return MemoryMarshal.Cast<TFrom, TTo>(span);

        fixed (void* ptr = span)
            return new ReadOnlySpan<TTo>(ptr, span.Length);
    }
}