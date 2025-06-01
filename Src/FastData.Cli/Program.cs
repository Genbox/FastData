using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Rust;
using Spectre.Console;

namespace Genbox.FastData.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        //Common options
        Option<FileInfo?> outputFileOpt = new Option<FileInfo?>("--output-file", "The file to write the generated code to.");
        outputFileOpt.AddAlias("-o");

        Option<DataType> dataTypeOpt = new Option<DataType>("--data-type", description: "The type of data in the input file.", getDefaultValue: () => DataType.String);
        dataTypeOpt.AddAlias("-d");

        Option<StructureType> structureTypeOpt = new Option<StructureType>("--structure-type", description: "The type of data structure to produce.", getDefaultValue: () => StructureType.Auto);
        structureTypeOpt.AddAlias("-s");

        Argument<FileInfo> inputFileArg = new Argument<FileInfo>("input-file", "The file to read data from");

        //CSharp section
        Option<string?> namespaceOpt = new Option<string?>("--namespace", "The namespace the generated class resides in.");
        namespaceOpt.AddAlias("-ns");

        Option<ClassVisibility> classVisibilityOpt = new Option<ClassVisibility>("--class-visibility", description: "The visibility of the generated class.", getDefaultValue: () => ClassVisibility.Internal);
        classVisibilityOpt.AddAlias("-cv");

        Option<ClassType> classTypeOpt = new Option<ClassType>("--class-type", description: "The type of the generated class.", getDefaultValue: () => ClassType.Static);
        classTypeOpt.AddAlias("-ct");

        Option<string> classNameOpt = new Option<string>("--class-name", description: "The class name.", getDefaultValue: () => "MyData");
        classNameOpt.AddAlias("-cn");

        Command csharpCmd = new Command("csharp", "Generate a C# data structure")
        {
            namespaceOpt,
            classVisibilityOpt,
            classTypeOpt,
            classNameOpt,

            inputFileArg
        };

        Command cppCmd = new Command("cpp", "Generate a C++ data structure")
        {
            classNameOpt,

            inputFileArg
        };

        Command rustCmd = new Command("rust", "Generate a Rust data structure")
        {
            classNameOpt,

            inputFileArg
        };

        RootCommand rootCmd = new RootCommand("FastData")
        {
            csharpCmd,
            cppCmd,
            rustCmd
        };

        rootCmd.AddGlobalOption(outputFileOpt);
        rootCmd.AddGlobalOption(dataTypeOpt);
        rootCmd.AddGlobalOption(structureTypeOpt);

        csharpCmd.SetHandler(async (outputFile, dataType, structureType, inputFile, cn, ns, cv, ct) =>
        {
            CSharpCodeGeneratorConfig genCfg = new CSharpCodeGeneratorConfig(cn);
            genCfg.Namespace = ns;
            genCfg.ClassVisibility = cv;
            genCfg.ClassType = ct;

            CSharpCodeGenerator generator = CSharpCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, dataType, structureType, generator, outputFile);
        }, outputFileOpt, dataTypeOpt, structureTypeOpt, inputFileArg, classNameOpt, namespaceOpt, classVisibilityOpt, classTypeOpt);

        cppCmd.SetHandler(async (outputFile, dataType, structureType, inputFile, cn) =>
        {
            CPlusPlusCodeGeneratorConfig genCfg = new CPlusPlusCodeGeneratorConfig(cn);
            CPlusPlusCodeGenerator generator = CPlusPlusCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, dataType, structureType, generator, outputFile);
        }, outputFileOpt, dataTypeOpt, structureTypeOpt, inputFileArg, classNameOpt);

        rustCmd.SetHandler(async (outputFile, dataType, structureType, inputFile, cn) =>
        {
            RustCodeGeneratorConfig genCfg = new RustCodeGeneratorConfig(cn);
            RustCodeGenerator generator = RustCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, dataType, structureType, generator, outputFile);
        }, outputFileOpt, dataTypeOpt, structureTypeOpt, inputFileArg, classNameOpt);

        Parser parser = new CommandLineBuilder(rootCmd)
                        .UseDefaults()
                        .UseHelp(ctx =>
                        {
                            //Manual help output to avoid printing Unknown from the enums
                            ctx.HelpBuilder.CustomizeSymbol(classVisibilityOpt, "-cv, --class-visibility <public|internal>");
                            ctx.HelpBuilder.CustomizeSymbol(classTypeOpt, "-ct, --class-type <instance|static|struct>");
                            ctx.HelpBuilder.CustomizeSymbol(dataTypeOpt, "-d, --data-type <bool|char|double|int16|int32|int64|int8|single|string|uint16|uint32|uint64|uint8>");
                            ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default
                                                                            .GetLayout()
                                                                            .Skip(1) // Skip the default command description section.
                                                                            .Prepend(_ => AnsiConsole.Write(new FigletText(rootCmd.Description!).Color(Color.Red1))));
                        })
                        .Build();

        return await parser.InvokeAsync(args);
    }

    private static async Task GenerateAsync(FileInfo inputFile, DataType dataType, StructureType structureType, ICodeGenerator generator, FileInfo? outputFile)
    {
        object[] data = await ReadFile(inputFile.FullName, dataType).ToArrayAsync();
        FastDataConfig config = new FastDataConfig(structureType);

        if (!FastDataGenerator.TryGenerate(data, config, generator, out string? source))
            throw new InvalidOperationException("Unable to generate code");

        if (outputFile == null)
            Console.WriteLine(source);
        else
            await File.WriteAllTextAsync(outputFile.FullName, source);
    }

    private static async IAsyncEnumerable<object> ReadFile(string file, DataType dataType)
    {
        Func<string, object> func = GetTypeFunc(dataType);

        await foreach (string line in File.ReadLinesAsync(file))
        {
            yield return func(line);
        }
    }

    private static Func<string, object> GetTypeFunc(DataType dataType) => dataType switch
    {
        DataType.String => str => str,
        DataType.Bool => str => bool.Parse(str),
        DataType.Int8 => str => sbyte.Parse(str, CultureInfo.InvariantCulture),
        DataType.UInt8 => str => byte.Parse(str, CultureInfo.InvariantCulture),
        DataType.Char => str => char.Parse(str),
        DataType.Int16 => str => short.Parse(str, CultureInfo.InvariantCulture),
        DataType.UInt16 => str => ushort.Parse(str, CultureInfo.InvariantCulture),
        DataType.Int32 => str => int.Parse(str, CultureInfo.InvariantCulture),
        DataType.UInt32 => str => uint.Parse(str, CultureInfo.InvariantCulture),
        DataType.Int64 => str => long.Parse(str, CultureInfo.InvariantCulture),
        DataType.UInt64 => str => ulong.Parse(str, CultureInfo.InvariantCulture),
        DataType.Single => str => float.Parse(str, CultureInfo.InvariantCulture),
        DataType.Double => str => double.Parse(str, CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
}