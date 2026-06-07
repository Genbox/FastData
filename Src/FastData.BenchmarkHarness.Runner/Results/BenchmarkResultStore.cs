using System.Text;
using System.Text.Json;
using Genbox.FastData.InternalShared.Harness;

namespace Genbox.FastData.BenchmarkHarness.Runner.Results;

internal sealed class BenchmarkResultStore(string resultsDirectory)
{
    private const int ResultReadBufferSize = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false };

    public BenchmarkResultEntry[] ReadHistory(string benchmarkName)
    {
        string path = GetResultPath(benchmarkName);

        if (!File.Exists(path))
            return [];

        List<BenchmarkResultEntry> entries = [];

        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            BenchmarkResultEntry? entry = JsonSerializer.Deserialize<BenchmarkResultEntry>(line, JsonOptions);
            if (entry != null)
                entries.Add(entry);
        }

        return entries.ToArray();
    }

    public async Task<BenchmarkResultEntry?> ReadPreviousResultAsync(string benchmarkName, CancellationToken cancellationToken)
    {
        string path = GetResultPath(benchmarkName);

        if (!File.Exists(path))
            return null;

        string? lastLine = await ReadLastLineAsync(path, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(lastLine))
            return null;

        return JsonSerializer.Deserialize<BenchmarkResultEntry>(lastLine, JsonOptions);
    }

    public async Task AppendResultAsync(string benchmarkName, BenchmarkResult result, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(resultsDirectory);
        BenchmarkResultEntry entry = new BenchmarkResultEntry(benchmarkName, result.Min, result.Median, result.Max, result.Avg, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(entry, JsonOptions);
        await File.AppendAllTextAsync(GetResultPath(benchmarkName), json + System.Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }

    private string GetResultPath(string benchmarkName) => Path.Combine(resultsDirectory, SanitizeFileName(benchmarkName) + ".jsonl");

    private static string SanitizeFileName(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        char[] invalidChars = Path.GetInvalidFileNameChars();

        foreach (char ch in value)
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);

        return builder.ToString();
    }

    private static async Task<string?> ReadLastLineAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, ResultReadBufferSize, FileOptions.Asynchronous);

        if (stream.Length == 0)
            return null;

        long remaining = stream.Length;
        long? lineEnd = null;
        byte[] buffer = new byte[ResultReadBufferSize];

        while (remaining > 0)
        {
            int readLength = (int)Math.Min(buffer.Length, remaining);
            remaining -= readLength;

            stream.Seek(remaining, SeekOrigin.Begin);
            await stream.ReadExactlyAsync(buffer.AsMemory(0, readLength), cancellationToken).ConfigureAwait(false);

            ReadOnlySpan<byte> span = buffer.AsSpan(0, readLength);

            if (lineEnd is null)
            {
                int contentEnd = span.Length;
                while (contentEnd > 0 && IsNewLine(span[contentEnd - 1]))
                    contentEnd--;

                if (contentEnd == 0)
                    continue;

                lineEnd = remaining + contentEnd;
                span = span[..contentEnd];
            }

            int lineBreakIndex = span.LastIndexOfAny((byte)'\r', (byte)'\n');
            if (lineBreakIndex >= 0)
            {
                long lineStart = remaining + lineBreakIndex + 1;
                return await ReadUtf8RangeAsync(stream, lineStart, lineEnd.Value - lineStart, cancellationToken).ConfigureAwait(false);
            }

            if (remaining == 0)
                return await ReadUtf8RangeAsync(stream, 0, lineEnd.Value, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<string?> ReadUtf8RangeAsync(FileStream stream, long start, long length, CancellationToken cancellationToken)
    {
        if (length <= 0)
            return null;

        if (length > int.MaxValue)
            throw new InvalidOperationException("The last benchmark result line is too large to read.");

        byte[] line = new byte[(int)length];
        stream.Seek(start, SeekOrigin.Begin);
        await stream.ReadExactlyAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(line);
    }

    private static bool IsNewLine(byte value) => value is (byte)'\r' or (byte)'\n';
}