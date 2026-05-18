using System.Text;

namespace Genbox.FastData.Cli.Tests;

[Collection("CliTests")]
public sealed class ValidationTests : IDisposable
{
    private readonly List<string> _tempFiles = new List<string>();

    public void Dispose() => CleanupTempFiles(_tempFiles);

    [Fact]
    public async Task RejectsPartialIntegerInput()
    {
        string inputFile = await WriteTempAsync("123abc");

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--key-type", "Int32", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("Invalid Int32 value: '123abc'.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcceptsUtf8BomForNumericInput()
    {
        string inputFile = await WriteTempAsync("1\n2", Encoding.UTF8);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--key-type", "Int32", inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.NotEmpty(output);
    }

    [Fact]
    public async Task StripsUtf8BomFromFirstStringInput()
    {
        string inputFile = await WriteTempAsync("one\ntwo", Encoding.UTF8);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.DoesNotContain("\ufeff", output, StringComparison.Ordinal);
        Assert.Contains("\"one\"", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task RejectsInvalidCharInput(string value)
    {
        string inputFile = await WriteTempAsync(value);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--key-type", "Char", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("Invalid Char value", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsIgnoreCaseForNumericKeys()
    {
        string inputFile = await WriteTempAsync("1");

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--key-type", "Int32", "--ignore-case", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("IgnoreCase is only supported for string keys.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WritesOutputFile()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);
        string outputFile = GetTempFilePath(_tempFiles, ".cs");

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--output-file", outputFile, inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(output);
        Assert.Empty(error);
        Assert.True(File.Exists(outputFile));
        string generated = await File.ReadAllTextAsync(outputFile);
        Assert.Contains("internal static class MyData", generated, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AppliesCSharpClassOptions()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--namespace", "Custom.Namespace", "--class-visibility", "Public", "--class-type", "Struct", "--class-name", "CustomData", inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.Contains("namespace Custom.Namespace;", output, StringComparison.Ordinal);
        Assert.Contains("public partial struct CustomData", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GeneratesKeyValueLookup()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);
        string valuesFile = await WriteTempAsync("""
                                                 1
                                                 2
                                                 """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--values-file", valuesFile, "--value-type", "Int32", inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.Contains("TryLookup", output, StringComparison.Ordinal);
        Assert.Contains("int", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcceptsStringAnalysisLevel()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--analysis-level", "Fast", inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.NotEmpty(output);
    }

    [Fact]
    public async Task RejectsAnalysisLevelForNumericKeys()
    {
        string inputFile = await WriteTempAsync("1");

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--key-type", "Int32", "--analysis-level", "Fast", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("AnalysisLevel is only supported for string keys.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsBloomFilterWithoutAllowApproximate()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--structure-type", "BloomFilter", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("BloomFilter is approximate and requires --allow-approximate.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GeneratesBloomFilterWithAllowApproximate()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--structure-type", "BloomFilter", "--allow-approximate", inputFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.Contains("// Structure: BloomFilter", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsStringIncompatibleStructure()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--structure-type", "BitSet", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("Structure 'BitSet' is not supported for string keys.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsNumericIncompatibleStructure()
    {
        string inputFile = await WriteTempAsync("""
                                                1
                                                2
                                                """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--key-type", "Int32", "--structure-type", "KeyLength", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("Structure 'KeyLength' is not supported for numeric keys.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsApproximateKeyValueLookup()
    {
        string inputFile = await WriteTempAsync("""
                                                one
                                                two
                                                """);
        string valuesFile = await WriteTempAsync("""
                                                 1
                                                 2
                                                 """);

        (int exitCode, string output, string error) = await RunWithExitCodeAsync("csharp", "--allow-approximate", "--values-file", valuesFile, "--value-type", "Int32", inputFile);

        Assert.Equal(-1, exitCode);
        Assert.Empty(output);
        Assert.Contains("Approximate matching is only supported for membership lookups.", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HelpDoesNotAdvertiseBoolKeyType()
    {
        (int exitCode, string output, string error) = await RunWithExitCodeAsync("--help");

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.DoesNotContain("bool|", output, StringComparison.OrdinalIgnoreCase);
    }

    private Task<string> WriteTempAsync(string content) => WriteTempFileAsync(_tempFiles, content);

    private async Task<string> WriteTempAsync(string content, Encoding? encoding)
    {
        encoding ??= new UTF8Encoding(false);

        string path = GetTempFilePath(_tempFiles, ".input");
        await File.WriteAllTextAsync(path, content + "\n", encoding);
        return path;
    }
}