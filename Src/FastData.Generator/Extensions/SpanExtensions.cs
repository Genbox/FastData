namespace Genbox.FastData.Generator.Extensions;

public static class SpanExtensions
{
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr) => new ReadOnlySpan<T>(arr);
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr, int start) => new ReadOnlySpan<T>(arr, start, arr.Length - start);
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr, int start, int length) => new ReadOnlySpan<T>(arr, start, length);
}