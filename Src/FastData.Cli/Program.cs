using System.Buffers;
using System.Buffers.Text;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Rust;
using Genbox.FastData.Generators.Abstracts;
using Spectre.Console;

namespace Genbox.FastData.Cli;

internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        //FastData options
        Option<FileInfo?> outputFileOpt = new Option<FileInfo?>("--output-file", "The file to write the generated code to.");
        outputFileOpt.AddAlias("-o");

        Option<KeyType> keyTypeOpt = new Option<KeyType>("--key-type", description: "The type of data in the input file.", getDefaultValue: () => KeyType.String);
        keyTypeOpt.AddAlias("-k");

        Option<StructureType> structureTypeOpt = new Option<StructureType>("--structure-type", description: "The type of data structure to produce.", getDefaultValue: () => StructureType.Auto);
        structureTypeOpt.AddAlias("-s");

        Argument<FileInfo> keysFileArg = new Argument<FileInfo>("keys-file", "The file to read keys from");

        //TODO: Add support for inputting a structure definition + CSV (for simple data) and JSON (for complex data) inputs
        // Argument<FileInfo> valuesFileArg = new Argument<FileInfo>("values-file", "The file to read values from");

        //Options common for all generators
        Option<string> classNameOpt = new Option<string>("--class-name", description: "The class name.", getDefaultValue: () => "MyData");
        classNameOpt.AddAlias("-cn");

        //CSharp section
        Option<string?> namespaceOpt = new Option<string?>("--namespace", "The namespace the generated class resides in.");
        namespaceOpt.AddAlias("-ns");

        Option<ClassVisibility> classVisibilityOpt = new Option<ClassVisibility>("--class-visibility", description: "The visibility of the generated class.", getDefaultValue: () => ClassVisibility.Internal);
        classVisibilityOpt.AddAlias("-cv");

        Option<ClassType> classTypeOpt = new Option<ClassType>("--class-type", description: "The type of the generated class.", getDefaultValue: () => ClassType.Static);
        classTypeOpt.AddAlias("-ct");

        Command csharpCmd = new Command("csharp", "Generate a C# data structure")
        {
            namespaceOpt,
            classVisibilityOpt,
            classTypeOpt,
            classNameOpt,

            keysFileArg
        };

        Command cppCmd = new Command("cpp", "Generate a C++ data structure")
        {
            classNameOpt,

            keysFileArg
        };

        Command rustCmd = new Command("rust", "Generate a Rust data structure")
        {
            classNameOpt,

            keysFileArg
        };

        RootCommand rootCmd = new RootCommand("FastData")
        {
            csharpCmd,
            cppCmd,
            rustCmd
        };

        rootCmd.AddGlobalOption(outputFileOpt);
        rootCmd.AddGlobalOption(keyTypeOpt);
        rootCmd.AddGlobalOption(structureTypeOpt);

        csharpCmd.SetHandler(async (outputFile, keyType, structureType, inputFile, cn, ns, cv, ct) =>
        {
            CSharpCodeGeneratorConfig genCfg = new CSharpCodeGeneratorConfig(cn);
            genCfg.Namespace = ns;
            genCfg.ClassVisibility = cv;
            genCfg.ClassType = ct;

            CSharpCodeGenerator generator = CSharpCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, keyType, structureType, generator, outputFile).ConfigureAwait(false);
        }, outputFileOpt, keyTypeOpt, structureTypeOpt, keysFileArg, classNameOpt, namespaceOpt, classVisibilityOpt, classTypeOpt);

        cppCmd.SetHandler(async (outputFile, keyType, structureType, inputFile, cn) =>
        {
            CPlusPlusCodeGeneratorConfig genCfg = new CPlusPlusCodeGeneratorConfig(cn);
            CPlusPlusCodeGenerator generator = CPlusPlusCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, keyType, structureType, generator, outputFile).ConfigureAwait(false);
        }, outputFileOpt, keyTypeOpt, structureTypeOpt, keysFileArg, classNameOpt);

        rustCmd.SetHandler(async (outputFile, keyType, structureType, inputFile, cn) =>
        {
            RustCodeGeneratorConfig genCfg = new RustCodeGeneratorConfig(cn);
            RustCodeGenerator generator = RustCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, keyType, structureType, generator, outputFile).ConfigureAwait(false);
        }, outputFileOpt, keyTypeOpt, structureTypeOpt, keysFileArg, classNameOpt);

        Parser parser = new CommandLineBuilder(rootCmd)
                        .UseDefaults()
                        .UseHelp(ctx =>
                        {
                            //Manual help output to avoid printing Unknown from the enums
                            ctx.HelpBuilder.CustomizeSymbol(classVisibilityOpt, "-cv, --class-visibility <public|internal>");
                            ctx.HelpBuilder.CustomizeSymbol(classTypeOpt, "-ct, --class-type <instance|static|struct>");
                            ctx.HelpBuilder.CustomizeSymbol(keyTypeOpt, "-k, --key-type <bool|char|double|int16|int32|int64|int8|single|string|uint16|uint32|uint64|uint8>");
                            ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default
                                                                            .GetLayout()
                                                                            .Skip(1) // Skip the default command description section.
                                                                            .Prepend(_ => AnsiConsole.Write(new FigletText(rootCmd.Description!).Color(Color.Red1))));
                        })
                        .Build();

        try
        {
            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync("An error happened: " + e.Message).ConfigureAwait(false);
            return -1;
        }
    }

    private static async Task GenerateAsync(FileInfo inputFile, KeyType keyType, StructureType structureType, ICodeGenerator generator, FileInfo? outputFile)
    {
        string fi = inputFile.FullName;
        FastDataConfig config = new FastDataConfig(structureType);

        string source = keyType switch
        {
            KeyType.String => FastDataGenerator.Generate(await ParseFileAsync<string>(fi, static (x, y) => x.Add(Encoding.UTF8.GetString(y)), CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Int8 => FastDataGenerator.Generate(await ParseFileAsync<sbyte>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out sbyte value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.UInt8 => FastDataGenerator.Generate(await ParseFileAsync<byte>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out byte value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Int16 => FastDataGenerator.Generate(await ParseFileAsync<short>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out short value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.UInt16 => FastDataGenerator.Generate(await ParseFileAsync<ushort>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out ushort value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Int32 => FastDataGenerator.Generate(await ParseFileAsync<int>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out int value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.UInt32 => FastDataGenerator.Generate(await ParseFileAsync<uint>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out uint value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Int64 => FastDataGenerator.Generate(await ParseFileAsync<long>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out long value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.UInt64 => FastDataGenerator.Generate(await ParseFileAsync<ulong>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out ulong value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Single => FastDataGenerator.Generate(await ParseFileAsync<float>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out float value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Double => FastDataGenerator.Generate(await ParseFileAsync<double>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out double value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            KeyType.Char => FastDataGenerator.Generate(await ParseFileAsync<char>(fi, static (x, y) =>
            {
                Span<char> chars = stackalloc char[1];
                Encoding.UTF8.GetChars(y, chars);
                x.Add(chars[0]);
            }, CancellationToken.None).ConfigureAwait(false), config, generator),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };

        if (outputFile == null)
            Console.WriteLine(source);
        else
            await File.WriteAllTextAsync(outputFile.FullName, source).ConfigureAwait(false);
    }

    private sealed class PooledArray<T>(int initialCapacity = 1024)
    {
        private T[] _buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        private int _count;

        public void Add(in T item)
        {
            if (_count == _buffer.Length)
            {
                T[] newBuf = ArrayPool<T>.Shared.Rent(_buffer.Length * 2);
                Array.Copy(_buffer, newBuf, _buffer.Length);
                ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
                _buffer = newBuf;
            }
            _buffer[_count++] = item;
        }

        public T[] ToArray()
        {
            T[] result = new T[_count];
            Array.Copy(_buffer, result, _count);
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            return result;
        }
    }

    private static async Task<T[]> ParseFileAsync<T>(string filePath, Action<PooledArray<T>, ReadOnlySpan<byte>> func, CancellationToken token = default)
    {
        PooledArray<T> pool = new PooledArray<T>();
        await using FileStream stream = File.OpenRead(filePath);
        PipeReader reader = PipeReader.Create(stream);

        while (true)
        {
            ReadResult result = await reader.ReadAsync(token).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;
            SequenceReader<byte> seq = new SequenceReader<byte>(buffer);

            while (seq.TryReadTo(out ReadOnlySequence<byte> line, (byte)'\n'))
            {
                ReadOnlySpan<byte> span = line.IsSingleSegment ? line.First.Span : line.ToArray().AsSpan();

                if (span.Length > 0 && span[^1] == '\r')
                    span = span[..^1];

                func(pool, span);
            }

            // handle final line if no trailing '\n'
            if (result.IsCompleted && !seq.Position.Equals(buffer.End))
            {
                ReadOnlySequence<byte> rem = buffer.Slice(seq.Position);
                ReadOnlySpan<byte> span = rem.IsSingleSegment ? rem.First.Span : rem.ToArray().AsSpan();

                if (span.Length > 0 && span[^1] == '\r')
                    span = span[..^1];

                func(pool, span);
            }

            reader.AdvanceTo(seq.Position, buffer.End);
            if (result.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);
        return pool.ToArray();
    }
}