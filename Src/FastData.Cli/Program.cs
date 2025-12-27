using System.Buffers;
using System.Buffers.Text;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
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
        Option<FileInfo?> outputFileOpt = new Option<FileInfo?>("--output-file", "-o")
        {
            Description = "The file to write the generated code to.",
            Recursive = true
        };

        Option<KeyType> keyTypeOpt = new Option<KeyType>("--key-type", "-k")
        {
            Description = "The type of data in the input file.",
            DefaultValueFactory = _ => KeyType.String,
            HelpName = "bool|char|double|int16|int32|int64|int8|single|string|uint16|uint32|uint64|uint8",
            Recursive = true
        };

        Option<StructureType> structureTypeOpt = new Option<StructureType>("--structure-type", "-s")
        {
            Description = "The type of data structure to produce.",
            DefaultValueFactory = _ => StructureType.Auto,
            Recursive = true
        };

        Option<bool> ignoreCaseOpt = new Option<bool>("--ignore-case", "-ic")
        {
            Description = "Use case-insensitive lookups for ASCII string keys.",
            Recursive = true
        };

        Argument<FileInfo> keysFileArg = new Argument<FileInfo>("keys-file")
        {
            Description = "The file to read keys from"
        };

        //TODO: Add support for inputting a structure definition + CSV (for simple data) and JSON (for complex data) inputs
        // Argument<FileInfo> valuesFileArg = new Argument<FileInfo>("values-file", "The file to read values from");

        //Options common for all generators
        Option<string> classNameOpt = new Option<string>("--class-name", "-cn")
        {
            Description = "The class name.",
            DefaultValueFactory = _ => "MyData"
        };

        //CSharp section
        Option<string?> namespaceOpt = new Option<string?>("--namespace", "-ns")
        {
            Description = "The namespace the generated class resides in."
        };

        Option<ClassVisibility> classVisibilityOpt = new Option<ClassVisibility>("--class-visibility", "-cv")
        {
            Description = "The visibility of the generated class.",
            DefaultValueFactory = _ => ClassVisibility.Internal,
            HelpName = "public|internal"
        };

        Option<ClassType> classTypeOpt = new Option<ClassType>("--class-type", "-ct")
        {
            Description = "The type of the generated class.",
            DefaultValueFactory = _ => ClassType.Static,
            HelpName = "instance|static|struct"
        };

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

        rootCmd.Options.Add(outputFileOpt);
        rootCmd.Options.Add(keyTypeOpt);
        rootCmd.Options.Add(structureTypeOpt);
        rootCmd.Options.Add(ignoreCaseOpt);

        HelpOption helpOption = rootCmd.Options.OfType<HelpOption>().First();
        helpOption.Action = new CustomHelpAction(rootCmd);

        csharpCmd.SetAction(async (pr, token) =>
        {
            FileInfo? outputFile = pr.GetValue(outputFileOpt);
            KeyType keyType = pr.GetValue(keyTypeOpt);
            StructureType structureType = pr.GetValue(structureTypeOpt);
            bool ignoreCase = pr.GetValue(ignoreCaseOpt);
            FileInfo inputFile = pr.GetValue(keysFileArg);
            string? cn = pr.GetValue(classNameOpt);
            string? ns = pr.GetValue(namespaceOpt);
            ClassVisibility cv = pr.GetValue(classVisibilityOpt);
            ClassType ct = pr.GetValue(classTypeOpt);

            CSharpCodeGeneratorConfig genCfg = new CSharpCodeGeneratorConfig(cn);
            genCfg.Namespace = ns;
            genCfg.ClassVisibility = cv;
            genCfg.ClassType = ct;

            CSharpCodeGenerator generator = CSharpCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, keyType, structureType, ignoreCase, generator, outputFile, token).ConfigureAwait(false);
        });

        cppCmd.SetAction(async (pr, token) =>
        {
            FileInfo? outputFile = pr.GetValue(outputFileOpt);
            KeyType keyType = pr.GetValue(keyTypeOpt);
            StructureType structureType = pr.GetValue(structureTypeOpt);
            bool ignoreCase = pr.GetValue(ignoreCaseOpt);
            FileInfo inputFile = pr.GetValue(keysFileArg);
            string? cn = pr.GetValue(classNameOpt);

            CPlusPlusCodeGeneratorConfig genCfg = new CPlusPlusCodeGeneratorConfig(cn);
            CPlusPlusCodeGenerator generator = CPlusPlusCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, keyType, structureType, ignoreCase, generator, outputFile, token).ConfigureAwait(false);
        });

        rustCmd.SetAction(async (pr, token) =>
        {
            FileInfo? outputFile = pr.GetValue(outputFileOpt);
            KeyType keyType = pr.GetValue(keyTypeOpt);
            StructureType structureType = pr.GetValue(structureTypeOpt);
            bool ignoreCase = pr.GetValue(ignoreCaseOpt);
            FileInfo inputFile = pr.GetValue(keysFileArg);
            string? cn = pr.GetValue(classNameOpt);

            RustCodeGeneratorConfig genCfg = new RustCodeGeneratorConfig(cn);
            RustCodeGenerator generator = RustCodeGenerator.Create(genCfg);

            await GenerateAsync(inputFile, keyType, structureType, ignoreCase, generator, outputFile, token).ConfigureAwait(false);
        });

        try
        {
            ParserConfiguration parserConfig = new ParserConfiguration();
            InvocationConfiguration invocationConfig = new InvocationConfiguration
            {
                EnableDefaultExceptionHandler = false
            };
            ParseResult parseResult = rootCmd.Parse(args, parserConfig);
            return await parseResult.InvokeAsync(invocationConfig, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync("An error happened: " + e.Message).ConfigureAwait(false);
            return -1;
        }
    }

    private static async Task GenerateAsync(FileInfo inputFile, KeyType keyType, StructureType structureType, bool ignoreCase, ICodeGenerator generator, FileInfo? outputFile, CancellationToken token)
    {
        string fi = inputFile.FullName;
        FastDataConfig config = new FastDataConfig(structureType)
        {
            IgnoreCase = ignoreCase
        };

        if (ignoreCase && keyType != KeyType.String)
            throw new InvalidOperationException("IgnoreCase is only supported for string keys.");

        string source = keyType switch
        {
            KeyType.String => FastDataGenerator.Generate(await ParseFileAsync<string>(fi, static (x, y) => x.Add(Encoding.UTF8.GetString(y)), token).ConfigureAwait(false), config, generator),
            KeyType.Int8 => FastDataGenerator.Generate(await ParseFileAsync<sbyte>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out sbyte value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.UInt8 => FastDataGenerator.Generate(await ParseFileAsync<byte>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out byte value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.Int16 => FastDataGenerator.Generate(await ParseFileAsync<short>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out short value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.UInt16 => FastDataGenerator.Generate(await ParseFileAsync<ushort>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out ushort value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.Int32 => FastDataGenerator.Generate(await ParseFileAsync<int>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out int value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.UInt32 => FastDataGenerator.Generate(await ParseFileAsync<uint>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out uint value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.Int64 => FastDataGenerator.Generate(await ParseFileAsync<long>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out long value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.UInt64 => FastDataGenerator.Generate(await ParseFileAsync<ulong>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out ulong value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.Single => FastDataGenerator.Generate(await ParseFileAsync<float>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out float value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.Double => FastDataGenerator.Generate(await ParseFileAsync<double>(fi, static (x, y) =>
            {
                if (Utf8Parser.TryParse(y, out double value, out _))
                    x.Add(value);
                else
                    throw new InvalidOperationException("Invalid value");
            }, token).ConfigureAwait(false), config, generator),
            KeyType.Char => FastDataGenerator.Generate(await ParseFileAsync<char>(fi, static (x, y) =>
            {
                Span<char> chars = stackalloc char[1];
                Encoding.UTF8.GetChars(y, chars);
                x.Add(chars[0]);
            }, token).ConfigureAwait(false), config, generator),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };

        if (outputFile == null)
            Console.WriteLine(source);
        else
            await File.WriteAllTextAsync(outputFile.FullName, source, token).ConfigureAwait(false);
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
        FileStream stream = File.OpenRead(filePath);
        await using ConfiguredAsyncDisposable stream1 = stream.ConfigureAwait(false);
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

    private sealed class CustomHelpAction(RootCommand rootCmd) : SynchronousCommandLineAction
    {
        private readonly RootCommand _rootCmd = rootCmd;

        public override int Invoke(ParseResult parseResult)
        {
            AnsiConsole.Write(new FigletText(_rootCmd.Description!).Color(Color.Red1));
            return new HelpAction().Invoke(parseResult);
        }
    }
}