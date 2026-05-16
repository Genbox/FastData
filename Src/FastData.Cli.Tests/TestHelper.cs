using System.Text;

namespace Genbox.FastData.Cli.Tests;

internal static class TestHelper
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    internal static async Task<(string Output, string Error)> RunAsync(params string[] args)
    {
        (int _, string output, string error) = await RunWithExitCodeAsync(args);
        return (output, error);
    }

    internal static async Task<(int ExitCode, string Output, string Error)> RunWithExitCodeAsync(params string[] args)
    {
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        try
        {
            await using StringWriter outputWriter = new StringWriter();
            await using StringWriter errorWriter = new StringWriter();

            Console.SetOut(outputWriter);
            Console.SetError(errorWriter);

            int exitCode = await Program.Main(args);

            return (exitCode, outputWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    internal static async Task<string> WriteTempFileAsync(List<string> tracker, string content, string extension = ".input")
    {
        string path = GetTempFilePath(tracker, extension);
        await File.WriteAllTextAsync(path, content + "\n", Utf8NoBom);
        return path;
    }

    internal static string GetTempFilePath(List<string> tracker, string extension)
    {
        string directory = Path.Combine(Path.GetTempPath(), "FastData.Cli.Tests");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Guid.NewGuid() + extension);
        tracker.Add(path);
        return path;
    }

    internal static void CleanupTempFiles(List<string> files)
    {
        foreach (string path in files)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
        }
    }
}