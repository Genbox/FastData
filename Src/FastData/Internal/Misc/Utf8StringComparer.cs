using System.Collections.Generic;

namespace Genbox.FastData.Internal.Misc;

internal sealed class Utf8StringComparer(bool ignoreCase) : IComparer<string>
{
    internal static Utf8StringComparer Ordinal { get; } = new Utf8StringComparer(false);

    internal static Utf8StringComparer AsciiIgnoreCase { get; } = new Utf8StringComparer(true);

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x == null)
            return -1;

        if (y == null)
            return 1;

        int length = Math.Min(x.Length, y.Length);

        for (int i = 0; i < length; i++)
        {
            int a = x[i];
            int b = y[i];

            if (ignoreCase)
            {
                if (a >= 'A' && a <= 'Z')
                    a += 'a' - 'A';

                if (b >= 'A' && b <= 'Z')
                    b += 'a' - 'A';
            }

            if (a == b)
                continue;

            // For well-formed UTF-16, UTF-8 order differs at surrogate/upper-BMP boundaries.
            // Move surrogate units above BMP units while preserving their relative order.
            if (a >= 0xD800 && a <= 0xDFFF)
                a += 0x10000;

            if (b >= 0xD800 && b <= 0xDFFF)
                b += 0x10000;

            return a.CompareTo(b);
        }

        return x.Length.CompareTo(y.Length);
    }
}