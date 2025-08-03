namespace Genbox.FastData.Cli.Tests;

public class GenericTests
{
    [Fact] public async Task CSharpProduceStringOutput() => await Do(["csharp", "Files/Strings.input"]);
    [Fact] public async Task CSharpProduceÍntegerOutput() => await Do(["csharp", "-k UInt8", "Files/Integers.input"]);
    [Fact] public async Task CSharpCustomStructure() => await Do(["csharp", "-s HashTable", "Files/Strings.input"]);

    [Fact] public async Task CPlusPlusProduceStringOutput() => await Do(["cpp", "Files/Strings.input"]);
    [Fact] public async Task CPlusPlusProduceIntegerOutput() => await Do(["cpp", "-k UInt8", "Files/Integers.input"]);
    [Fact] public async Task CPlusPlusCustomStructure() => await Do(["cpp", "-s HashTable", "Files/Strings.input"]);

    [Fact] public async Task RustProduceStringOutput() => await Do(["rust", "Files/Strings.input"]);
    [Fact] public async Task RustProduceIntegerOutput() => await Do(["rust", "-k UInt8", "Files/Integers.input"]);
    [Fact] public async Task RustCustomStructure() => await Do(["rust", "-s HashTable", "Files/Strings.input"]);

    private static async Task Do(string[] args)
    {
        string sanitizedFileName = string.Join("_", args);

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            sanitizedFileName = sanitizedFileName.Replace(invalidChar, '_');

        await Verify(await ExecuteProgramMainAsync(args))
              .UseFileName(sanitizedFileName)
              .UseDirectory("CommandOutputs")
              .DisableDiff();
    }

    private static async Task<(string, string)> ExecuteProgramMainAsync(params string[] args)
    {
        // Save original console writers
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        try
        {
            await using StringWriter outputWriter = new StringWriter();
            await using StringWriter errorWriter = new StringWriter();

            Console.SetOut(outputWriter);
            Console.SetError(errorWriter);

            await Program.Main(args);

            return (outputWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}