namespace Genbox.FastData.Cli.Tests;

[Collection("CliTests")]
public class LanguageOutputTests
{
    [Fact]
    public async Task CSharpStringOutput() => await VerifyOutput("csharp", "Files/Strings.input");

    [Fact]
    public async Task CSharpIntegerOutput() => await VerifyOutput("csharp", "-k UInt8", "Files/Integers.input");

    [Fact]
    public async Task CSharpHashTableOutput() => await VerifyOutput("csharp", "-s HashTable", "Files/Strings.input");

    [Fact]
    public async Task CPlusPlusStringOutput() => await VerifyOutput("cpp", "Files/Strings.input");

    [Fact]
    public async Task CPlusPlusIntegerOutput() => await VerifyOutput("cpp", "-k UInt8", "Files/Integers.input");

    [Fact]
    public async Task CPlusPlusHashTableOutput() => await VerifyOutput("cpp", "-s HashTable", "Files/Strings.input");

    [Fact]
    public async Task RustStringOutput() => await VerifyOutput("rust", "Files/Strings.input");

    [Fact]
    public async Task RustIntegerOutput() => await VerifyOutput("rust", "-k UInt8", "Files/Integers.input");

    [Fact]
    public async Task RustHashTableOutput() => await VerifyOutput("rust", "-s HashTable", "Files/Strings.input");

    private static async Task VerifyOutput(params string[] args)
    {
        string sanitizedFileName = string.Join("_", args);

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            sanitizedFileName = sanitizedFileName.Replace(invalidChar, '_');

        await Verify(await RunAsync(args))
              .UseFileName(sanitizedFileName)
              .UseDirectory("CommandOutputs")
              .DisableDiff();
    }
}