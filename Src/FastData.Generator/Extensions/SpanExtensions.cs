using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Extensions;

/// <summary>Provides explicit read-only span views over arrays.</summary>
[PublicAPI]
public static class SpanExtensions
{
    /// <summary>Creates a read-only span over an entire array.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="arr">The source array.</param>
    /// <returns>A read-only view over the array.</returns>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr) => new ReadOnlySpan<T>(arr);

    /// <summary>Creates a read-only span over an array starting at the specified index.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="arr">The source array.</param>
    /// <param name="start">The zero-based starting index.</param>
    /// <returns>A read-only view from <paramref name="start"/> to the end of the array.</returns>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr, int start)
    {
        if (arr == null)
            throw new ArgumentNullException(nameof(arr), "The source array cannot be null.");

        return new ReadOnlySpan<T>(arr, start, arr.Length - start);
    }

    /// <summary>Creates a read-only span over the specified range of an array.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="arr">The source array.</param>
    /// <param name="start">The zero-based starting index.</param>
    /// <param name="length">The number of elements in the span.</param>
    /// <returns>A read-only view over the requested range.</returns>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr, int start, int length) => new ReadOnlySpan<T>(arr, start, length);
}