using System.Buffers;
using System.Buffers.Text;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Genbox.FastData.Config;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Generator.CPlusPlus;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Rust;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Structures;
using Spectre.Console;

namespace Genbox.FastData.Cli;

internal static class Program
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static async Task<int> Main(string[] args)
    {
        // Common options
        Option<FileInfo?> outputFileOpt = new Option<FileInfo?>("--output-file", "-o") { Description = "The file to write the generated code to.", Recursive = true };
        Option<KeyType> keyTypeOpt = new Option<KeyType>("--key-type", "-k") { Description = "The type of data in the input file.", DefaultValueFactory = _ => KeyType.String, HelpName = "char|double|int16|int32|int64|int8|single|string|uint16|uint32|uint64|uint8", Recursive = true };
        Option<StructureType> structureTypeOpt = new Option<StructureType>("--structure-type", "-s") { Description = "The type of data structure to produce.", DefaultValueFactory = _ => StructureType.Auto, Recursive = true };
        Option<bool> ignoreCaseOpt = new Option<bool>("--ignore-case", "-ic") { Description = "Use case-insensitive lookups for ASCII string keys.", Recursive = true };
        Option<FileInfo?> valuesFileOpt = new Option<FileInfo?>("--values-file", "-v") { Description = "The file to read values from. The number of values must match the number of keys.", Recursive = true };
        Option<KeyType> valueTypeOpt = new Option<KeyType>("--value-type") { Description = "The type of data in the values file.", DefaultValueFactory = _ => KeyType.String, HelpName = "char|double|int16|int32|int64|int8|single|string|uint16|uint32|uint64|uint8", Recursive = true };
        Option<AnalysisLevel> analysisLevelOpt = new Option<AnalysisLevel>("--analysis-level") { Description = "The amount of string-hash analysis to perform.", DefaultValueFactory = _ => AnalysisLevel.Disabled, HelpName = "disabled|fast|balanced|aggressive", Recursive = true };
        Option<bool> allowApproximateOpt = new Option<bool>("--allow-approximate") { Description = "Allow approximate membership lookups, such as Bloom filters, that may return false positives.", Recursive = true };
        Argument<FileInfo> keysFileArg = new Argument<FileInfo>("keys-file") { Description = "The file to read keys from" };
        Option<string> classNameOpt = new Option<string>("--class-name", "-cn") { Description = "The class name.", DefaultValueFactory = _ => "MyData" };

        // C#-only options
        Option<string?> namespaceOpt = new Option<string?>("--namespace", "-ns") { Description = "The namespace the generated class resides in." };
        Option<ClassVisibility> classVisibilityOpt = new Option<ClassVisibility>("--class-visibility", "-cv") { Description = "The visibility of the generated class.", DefaultValueFactory = _ => ClassVisibility.Internal, HelpName = "public|internal" };
        Option<ClassType> classTypeOpt = new Option<ClassType>("--class-type", "-ct") { Description = "The type of the generated class.", DefaultValueFactory = _ => ClassType.Static, HelpName = "instance|static|struct" };

        // Helper to read common options from a ParseResult
        CommonOptions ReadCommon(ParseResult pr) => new CommonOptions(
            pr.GetValue(keysFileArg),
            pr.GetValue(outputFileOpt),
            pr.GetValue(valuesFileOpt),
            pr.GetValue(keyTypeOpt),
            pr.GetValue(valueTypeOpt),
            pr.GetValue(structureTypeOpt),
            pr.GetValue(ignoreCaseOpt),
            pr.GetValue(allowApproximateOpt),
            pr.GetValue(analysisLevelOpt));

        // Commands
        Command csharpCmd = new Command("csharp", "Generate a C# data structure") { namespaceOpt, classVisibilityOpt, classTypeOpt, classNameOpt, keysFileArg };
        Command cppCmd = new Command("cpp", "Generate a C++ data structure") { classNameOpt, keysFileArg };
        Command rustCmd = new Command("rust", "Generate a Rust data structure") { classNameOpt, keysFileArg };

        RootCommand rootCmd = new RootCommand("FastData") { csharpCmd, cppCmd, rustCmd };
        rootCmd.Options.Add(outputFileOpt);
        rootCmd.Options.Add(keyTypeOpt);
        rootCmd.Options.Add(structureTypeOpt);
        rootCmd.Options.Add(ignoreCaseOpt);
        rootCmd.Options.Add(valuesFileOpt);
        rootCmd.Options.Add(valueTypeOpt);
        rootCmd.Options.Add(analysisLevelOpt);
        rootCmd.Options.Add(allowApproximateOpt);

        HelpOption helpOption = rootCmd.Options.OfType<HelpOption>().First();
        helpOption.Action = new CustomHelpAction(rootCmd);

        csharpCmd.SetAction(async (pr, token) =>
        {
            CSharpCodeGeneratorConfig genCfg = new CSharpCodeGeneratorConfig(pr.GetValue(classNameOpt))
            {
                Namespace = pr.GetValue(namespaceOpt),
                ClassVisibility = pr.GetValue(classVisibilityOpt),
                ClassType = pr.GetValue(classTypeOpt)
            };
            await RunAsync(ReadCommon(pr), new CSharpCodeGenerator(genCfg), token).ConfigureAwait(false);
        });

        cppCmd.SetAction(async (pr, token) =>
        {
            await RunAsync(ReadCommon(pr), new CPlusPlusCodeGenerator(new CPlusPlusCodeGeneratorConfig(pr.GetValue(classNameOpt))), token).ConfigureAwait(false);
        });

        rustCmd.SetAction(async (pr, token) =>
        {
            await RunAsync(ReadCommon(pr), new RustCodeGenerator(new RustCodeGeneratorConfig(pr.GetValue(classNameOpt))), token).ConfigureAwait(false);
        });

        try
        {
            ParseResult parseResult = rootCmd.Parse(args, new ParserConfiguration());
            InvocationConfiguration invocationConfig = new InvocationConfiguration { EnableDefaultExceptionHandler = false };
            return await parseResult.InvokeAsync(invocationConfig, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync("An error happened: " + e.Message).ConfigureAwait(false);
            return -1;
        }
    }

    private static async Task RunAsync(CommonOptions opts, ICodeGenerator generator, CancellationToken token)
    {
        bool hasValues = opts.ValuesFile != null;
        ValidateOptions(opts.KeyType, hasValues, opts.StructureType, opts.IgnoreCase, opts.AnalysisLevel, opts.AllowApproximate);

        Type? structureTypeOverride = MapStructureType(opts.StructureType);

        StringDataConfig stringConfig = new StringDataConfig
        {
            IgnoreCase = opts.IgnoreCase,
            AllowApproximateMatching = opts.AllowApproximate,
            StringAnalyzerConfig = CreateStringAnalyzerConfig(opts.AnalysisLevel)
        };
        NumericDataConfig numericConfig = new NumericDataConfig
        {
            AllowApproximateMatching = opts.AllowApproximate
        };

        if (structureTypeOverride != null)
        {
            stringConfig.StructureTypeOverride = structureTypeOverride;
            numericConfig.StructureTypeOverride = structureTypeOverride;
        }

        Array keys = await ParseFileAsync(opts.InputFile.FullName, opts.KeyType, token).ConfigureAwait(false);
        Array? values = opts.ValuesFile == null ? null : await ParseFileAsync(opts.ValuesFile.FullName, opts.ValueType, token).ConfigureAwait(false);
        DataConfig config = opts.KeyType == KeyType.String ? stringConfig : numericConfig;
        string source = GenerateSource(keys, values, config, generator);

        if (opts.OutputFile == null)
            Console.WriteLine(source);
        else
            await File.WriteAllTextAsync(opts.OutputFile.FullName, source, token).ConfigureAwait(false);
    }

    private static void ValidateOptions(KeyType keyType, bool hasValues, StructureType structureType, bool ignoreCase, AnalysisLevel analysisLevel, bool allowApproximate)
    {
        if (ignoreCase && keyType != KeyType.String)
            throw new InvalidOperationException("IgnoreCase is only supported for string keys.");

        if (analysisLevel != AnalysisLevel.Disabled && keyType != KeyType.String)
            throw new InvalidOperationException("AnalysisLevel is only supported for string keys.");

        if (allowApproximate && hasValues)
            throw new InvalidOperationException("Approximate matching is only supported for membership lookups.");

        if (structureType == StructureType.BloomFilter && !allowApproximate)
            throw new InvalidOperationException("BloomFilter is approximate and requires --allow-approximate.");

        if (keyType == KeyType.String)
        {
            if (!IsStringStructureSupported(structureType))
                throw new InvalidOperationException($"Structure '{structureType}' is not supported for string keys.");
        }
        else if (!IsNumericStructureSupported(structureType))
            throw new InvalidOperationException($"Structure '{structureType}' is not supported for numeric keys.");

        if (hasValues && !IsValueStructureSupported(structureType))
            throw new InvalidOperationException($"Structure '{structureType}' does not support key/value lookups.");
    }

    private static bool IsStringStructureSupported(StructureType structureType) => structureType is
        StructureType.Auto or StructureType.Array or StructureType.BinarySearch or
        StructureType.BloomFilter or StructureType.Conditional or StructureType.HashTableCompact or
        StructureType.HashTablePerfect or StructureType.HashTable or StructureType.KeyLength or
        StructureType.SingleValue;

    private static bool IsNumericStructureSupported(StructureType structureType) => structureType is not StructureType.KeyLength;

    private static bool IsValueStructureSupported(StructureType structureType) => structureType is
        StructureType.Auto or StructureType.Array or StructureType.BinarySearch or
        StructureType.BinarySearchInterpolation or StructureType.BitSet or StructureType.Conditional or
        StructureType.HashTableCompact or StructureType.HashTablePerfect or StructureType.HashTable or
        StructureType.KeyLength or StructureType.SingleValue;

    private static Type? MapStructureType(StructureType structureType) => structureType switch
    {
        StructureType.Auto => null,
        StructureType.Array => typeof(ArrayStructure<,>),
        StructureType.BinarySearch => typeof(BinarySearchStructure<,>),
        StructureType.BinarySearchInterpolation => typeof(BinarySearchInterpolationStructure<,>),
        StructureType.BitSet => typeof(BitSetStructure<,>),
        StructureType.BloomFilter => typeof(BloomFilterStructure<,>),
        StructureType.Conditional => typeof(ConditionalStructure<,>),
        StructureType.EliasFano => typeof(EliasFanoStructure<,>),
        StructureType.HashTableCompact => typeof(HashTableCompactStructure<,>),
        StructureType.HashTablePerfect => typeof(HashTablePerfectStructure<,>),
        StructureType.HashTable => typeof(HashTableStructure<,>),
        StructureType.KeyLength => typeof(KeyLengthStructure<,>),
        StructureType.Range => typeof(RangeStructure<,>),
        StructureType.RrrBitVector => typeof(RrrBitVectorStructure<,>),
        StructureType.SingleValue => typeof(SingleValueStructure<,>),
        _ => throw new ArgumentOutOfRangeException(nameof(structureType), structureType, "Unsupported structure type.")
    };

    private static StringAnalyzerConfig? CreateStringAnalyzerConfig(AnalysisLevel analysisLevel) => analysisLevel switch
    {
        AnalysisLevel.Disabled => null,
        AnalysisLevel.Fast => new StringAnalyzerConfig
        {
            BruteForceAnalyzerConfig = new BruteForceAnalyzerConfig { MaxAttempts = 1000 },
            GeneticAnalyzerConfig = new GeneticAnalyzerConfig { PopulationSize = 16, MaxGenerations = 8 },
            GPerfAnalyzerConfig = new GPerfAnalyzerConfig { MaxPositions = 64 }
        },
        AnalysisLevel.Balanced => new StringAnalyzerConfig(),
        AnalysisLevel.Aggressive => new StringAnalyzerConfig
        {
            BruteForceAnalyzerConfig = new BruteForceAnalyzerConfig { MaxAttempts = 157_464 },
            GeneticAnalyzerConfig = new GeneticAnalyzerConfig { PopulationSize = 64, MaxGenerations = 100 },
            GPerfAnalyzerConfig = new GPerfAnalyzerConfig { MaxPositions = 1024 }
        },
        _ => throw new ArgumentOutOfRangeException(nameof(analysisLevel), analysisLevel, "Unsupported analysis level.")
    };

    private static string GenerateSource(Array keys, Array? values, DataConfig config, ICodeGenerator generator)
    {
        Type keyType = keys.GetType().GetElementType() ?? throw new InvalidOperationException("Key array element type is missing.");

        if (values == null)
        {
            if (keyType == typeof(string))
                return FastDataGenerator.Generate((string[])keys, (StringDataConfig)config, generator);

            MethodInfo mi = GetGenerateMethod(nameof(FastDataGenerator.Generate), 1, 4, 0).MakeGenericMethod(keyType);
            return (string)mi.Invoke(null, [keys, config, generator, null])!;
        }

        Type valueType = values.GetType().GetElementType() ?? throw new InvalidOperationException("Value array element type is missing.");

        if (keyType == typeof(string))
        {
            MethodInfo mi = GetGenerateMethod(nameof(FastDataGenerator.GenerateKeyed), 1, 5, 0).MakeGenericMethod(valueType);
            return (string)mi.Invoke(null, [keys, values, config, generator, null])!;
        }

        MethodInfo keyed = GetGenerateMethod(nameof(FastDataGenerator.GenerateKeyed), 2, 5, 0).MakeGenericMethod(keyType, valueType);
        return (string)keyed.Invoke(null, [keys, values, config, generator, null])!;
    }

    private static MethodInfo GetGenerateMethod(string name, int genericArgCount, int paramCount, int memoryParamCount)
    {
        MethodInfo? method = Array.Find(typeof(FastDataGenerator).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly), m =>
        {
            if (m.Name != name || !m.IsGenericMethodDefinition)
                return false;

            if (m.GetGenericArguments().Length != genericArgCount)
                return false;

            ParameterInfo[] parms = m.GetParameters();
            if (parms.Length != paramCount)
                return false;

            int memoryParams = 0;
            for (int i = 0; i < parms.Length; i++)
            {
                Type type = parms[i].ParameterType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>))
                    memoryParams++;
            }

            return memoryParams == memoryParamCount;
        });

        if (method == null)
            throw new InvalidOperationException($"Unable to find '{name}' overload.");

        return method;
    }

    private static async Task<Array> ParseFileAsync(string filePath, KeyType keyType, CancellationToken token) => keyType switch
    {
        KeyType.String => await ParseFileAsync<string>(filePath, static (x, y) => x.Add(StrictUtf8.GetString(y)), token).ConfigureAwait(false),
        KeyType.Int8 => await ParseFileAsync<sbyte>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.UInt8 => await ParseFileAsync<byte>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.Int16 => await ParseFileAsync<short>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.UInt16 => await ParseFileAsync<ushort>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.Int32 => await ParseFileAsync<int>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.UInt32 => await ParseFileAsync<uint>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.Int64 => await ParseFileAsync<long>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.UInt64 => await ParseFileAsync<ulong>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.Single => await ParseFileAsync<float>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.Double => await ParseFileAsync<double>(filePath, static (x, y) => AddParsedNumber(x, y, static (source, out value, out bytesConsumed) => Utf8Parser.TryParse(source, out value, out bytesConsumed)), token).ConfigureAwait(false),
        KeyType.Char => await ParseFileAsync<char>(filePath, AddParsedChar, token).ConfigureAwait(false),
        _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
    };

    private static void AddParsedNumber<T>(PooledArray<T> values, ReadOnlySpan<byte> source, Utf8NumberParser<T> parser)
    {
        if (parser(source, out T value, out int bytesConsumed) && bytesConsumed == source.Length)
        {
            values.Add(value);
            return;
        }

        throw new InvalidOperationException($"Invalid {typeof(T).Name} value: '{GetDisplayValue(source)}'.");
    }

    private static void AddParsedChar(PooledArray<char> values, ReadOnlySpan<byte> source)
    {
        int charCount = StrictUtf8.GetCharCount(source);
        if (charCount != 1)
            throw new InvalidOperationException($"Invalid Char value: '{GetDisplayValue(source)}'. Expected exactly one UTF-16 character.");

        Span<char> chars = stackalloc char[1];
        StrictUtf8.GetChars(source, chars);
        values.Add(chars[0]);
    }

    private static string GetDisplayValue(ReadOnlySpan<byte> source) => Encoding.UTF8.GetString(source);

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

    private readonly record struct CommonOptions(
        FileInfo InputFile,
        FileInfo? OutputFile,
        FileInfo? ValuesFile,
        KeyType KeyType,
        KeyType ValueType,
        StructureType StructureType,
        bool IgnoreCase,
        bool AllowApproximate,
        AnalysisLevel AnalysisLevel);

    private delegate bool Utf8NumberParser<T>(ReadOnlySpan<byte> source, out T value, out int bytesConsumed);

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
                ArrayPool<T>.Shared.Return(_buffer, true);
                _buffer = newBuf;
            }
            _buffer[_count++] = item;
        }

        public T[] ToArray()
        {
            T[] result = new T[_count];
            Array.Copy(_buffer, result, _count);
            ArrayPool<T>.Shared.Return(_buffer, true);
            return result;
        }
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