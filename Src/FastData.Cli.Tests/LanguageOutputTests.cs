namespace Genbox.FastData.Cli.Tests;

[Collection("CliTests")]
public class LanguageOutputTests
{
    [Fact]
    public async Task CSharpStringOutput() => await VerifyOutputAsync("csharp", "Files/Strings.input");

    [Fact]
    public async Task CSharpIntegerOutput() => await VerifyOutputAsync("csharp", "-k UInt8", "Files/Integers.input");

    [Fact]
    public async Task CSharpHashTableOutput() => await VerifyOutputAsync("csharp", "-s HashTable", "Files/Strings.input");

    [Fact]
    public async Task CPlusPlusStringOutput() => await VerifyOutputAsync("cpp", "Files/Strings.input");

    [Fact]
    public async Task CPlusPlusIntegerOutput() => await VerifyOutputAsync("cpp", "-k UInt8", "Files/Integers.input");

    [Fact]
    public async Task CPlusPlusHashTableOutput() => await VerifyOutputAsync("cpp", "-s HashTable", "Files/Strings.input");

    [Fact]
    public async Task RustStringOutput() => await VerifyOutputAsync("rust", "Files/Strings.input");

    [Fact]
    public async Task RustIntegerOutput() => await VerifyOutputAsync("rust", "-k UInt8", "Files/Integers.input");

    [Fact]
    public async Task RustHashTableOutput() => await VerifyOutputAsync("rust", "-s HashTable", "Files/Strings.input");

    private static async Task VerifyOutputAsync(params string[] args)
    {
        string sanitizedFileName = string.Join('_', args);

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            sanitizedFileName = sanitizedFileName.Replace(invalidChar, '_');

        (string output, string error) = await RunAsync(args);
        await Verify((output, error))
              .UseFileName(sanitizedFileName)
              .UseDirectory("CommandOutputs")
              .DisableDiff();
    }
}