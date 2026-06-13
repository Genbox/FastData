using System.CommandLine;
using System.Text;
using Genbox.FastData.BenchmarkHarness.Runner.Catalog;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        BenchmarkCatalog catalog = new BenchmarkCatalog();
        Application application = new Application(catalog);
        RootCommand rootCommand = new CommandLine(catalog.LanguageNames).CreateRootCommand(application.RunAsync);

        try
        {
            ParseResult parseResult = rootCommand.Parse(args, new ParserConfiguration());
            InvocationConfiguration invocationConfig = new InvocationConfiguration { EnableDefaultExceptionHandler = false };
            return await parseResult.InvokeAsync(invocationConfig, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ConsoleOutput.WriteError("An error happened: " + ex.Message);
            return 1;
        }
    }
}