using System.Buffers;
using System.Text;
using System.Text.Json;
using Genbox.FastData.InternalShared.Harness;

namespace Genbox.FastData.BenchmarkHarness.Runner.Results;

internal sealed class ResultStore(string resultsDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static readonly SearchValues<char> InvalidFileNameChars = SearchValues.Create(Path.GetInvalidFileNameChars());

    public ResultEntry[] ReadHistory(string benchmarkName)
    {
        string path = GetResultPath(benchmarkName);

        if (!File.Exists(path))
            return [];

        List<ResultEntry> entries = [];

        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            ResultEntry? entry = DeserializeEntry(line);
            if (entry != null)
                entries.Add(entry);
        }

        return entries.ToArray();
    }

    public async Task<ResultEntry?> ReadPreviousResultAsync(string benchmarkName, CancellationToken cancellationToken)
    {
        string path = GetResultPath(benchmarkName);

        if (!File.Exists(path))
            return null;

        string? lastLine = await Utf8LastLineReader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(lastLine))
            return null;

        return DeserializeEntry(lastLine);
    }

    public async Task AppendResultAsync(string benchmarkName, BenchmarkResult result, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(resultsDirectory);
        ResultEntry entry = new ResultEntry(benchmarkName, result.Min, result.Median, result.Max, result.Avg, result.Error, result.StdDev, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(entry, JsonOptions);
        await File.AppendAllTextAsync(GetResultPath(benchmarkName), json + System.Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }

    private static ResultEntry? DeserializeEntry(string json) => JsonSerializer.Deserialize<ResultEntry>(json, JsonOptions);

    private string GetResultPath(string benchmarkName) => Path.Combine(resultsDirectory, SanitizeFileName(benchmarkName) + ".jsonl");

    private static string SanitizeFileName(string benchmarkName)
    {
        if (!benchmarkName.AsSpan().ContainsAny(InvalidFileNameChars))
            return benchmarkName;

        StringBuilder builder = new StringBuilder(benchmarkName.Length);

        foreach (char ch in benchmarkName)
            builder.Append(InvalidFileNameChars.Contains(ch) ? '_' : ch);

        return builder.ToString();
    }
}