using System.Text;

namespace Genbox.FastData.BenchmarkHarness.Runner.Results;

internal static class Utf8LastLineReader
{
    private const int BufferSize = 4096;

    public static async Task<string?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, FileOptions.Asynchronous);

        if (stream.Length == 0)
            return null;

        long remaining = stream.Length;
        long? lineEnd = null;
        byte[] buffer = new byte[BufferSize];

        while (remaining > 0)
        {
            int readLength = (int)Math.Min(buffer.Length, remaining);
            remaining -= readLength;

            stream.Seek(remaining, SeekOrigin.Begin);
            await stream.ReadExactlyAsync(buffer.AsMemory(0, readLength), cancellationToken).ConfigureAwait(false);

            ReadOnlySpan<byte> span = buffer.AsSpan(0, readLength);
            if (lineEnd is null)
            {
                span = TrimTrailingLineBreaks(span, remaining, out lineEnd);
                if (lineEnd is null)
                    continue;
            }

            int lineBreakIndex = span.LastIndexOfAny((byte)'\r', (byte)'\n');
            if (lineBreakIndex >= 0)
                return await ReadRangeAsync(stream, remaining + lineBreakIndex + 1, lineEnd.Value, cancellationToken).ConfigureAwait(false);

            if (remaining == 0)
                return await ReadRangeAsync(stream, 0, lineEnd.Value, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static ReadOnlySpan<byte> TrimTrailingLineBreaks(ReadOnlySpan<byte> span, long position, out long? lineEnd)
    {
        int contentEnd = span.Length;
        while (contentEnd > 0 && span[contentEnd - 1] is (byte)'\r' or (byte)'\n')
            contentEnd--;

        if (contentEnd == 0)
        {
            lineEnd = null;
            return span;
        }

        lineEnd = position + contentEnd;
        return span[..contentEnd];
    }

    private static async Task<string?> ReadRangeAsync(FileStream stream, long start, long end, CancellationToken cancellationToken)
    {
        long length = end - start;

        if (length <= 0)
            return null;

        if (length > int.MaxValue)
            throw new InvalidOperationException("The last benchmark result line is too large to read.");

        byte[] line = new byte[(int)length];
        stream.Seek(start, SeekOrigin.Begin);
        await stream.ReadExactlyAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(line);
    }
}