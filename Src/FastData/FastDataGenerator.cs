using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.BruteForce;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Generators;
using Genbox.FastData.Internal.Optimization;

namespace Genbox.FastData;

public static class FastDataGenerator
{
    public static string Generate(FastDataConfig config, IGenerator generator)
    {
        PreProcess(config, out KnownDataType dataType, out DataProperties props, out IEarlyExit[] exits);

        foreach (object candidate in GetDataStructureCandidates(config))
        {
            if (TryProcessCandidate(candidate, props, exits, dataType, generator, config, out string? code))
                return code;
        }

        throw new InvalidOperationException("This should not happen");
    }

    private static void PreProcess(FastDataConfig config, out KnownDataType dataType, out DataProperties props, out IEarlyExit[] exits)
    {
        //Validate that we only have unique data
        HashSet<object> uniq = new HashSet<object>();

        foreach (object o in config.Data)
        {
            if (!uniq.Add(o))
                throw new InvalidOperationException("Duplicate data detected: " + o);
        }

        dataType = config.GetDataType();
        props = GetDataProperties(config.Data, dataType);
        exits = GetEarlyExits(props);
    }

    private static IEarlyExit[] GetEarlyExits(DataProperties props)
    {
        if (props.StringProps.HasValue)
            return Optimizer.GetEarlyExits(props.StringProps.Value).ToArray();
        if (props.IntProps.HasValue)
            return Optimizer.GetEarlyExits(props.IntProps.Value).ToArray();
        if (props.UIntProps.HasValue)
            return Optimizer.GetEarlyExits(props.UIntProps.Value).ToArray();
        if (props.CharProps.HasValue)
            return Optimizer.GetEarlyExits(props.CharProps.Value).ToArray();
        if (props.FloatProps.HasValue)
            return Optimizer.GetEarlyExits(props.FloatProps.Value).ToArray();
        return [];
    }

    private static IEnumerable<object> GetDataStructureCandidates(FastDataConfig config)
    {
        //No matter the StorageMode, if there is only a single item, we will use the same data structure
        if (config.Data.Length == 1)
            yield return new SingleValueCode();
        else
        {
            switch (config.StorageMode)
            {
                case StorageMode.Auto:

                    // For small amounts of data, logic is the fastest, so we try that first
                    yield return new ConditionalCode();

                    // We try (unique) key lengths
                    yield return new UniqueKeyLengthCode();
                    yield return new KeyLengthCode();

                    if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory) && config.StorageOptions.HasFlag(StorageOption.OptimizeForSpeed))
                        yield return new MinimalPerfectHashCode();

                    if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                        yield return new BinarySearchCode();
                    else
                        yield return new HashSetChainCode();

                    break;
                case StorageMode.Linear:
                    yield return new ArrayCode();
                    break;
                case StorageMode.Logic:
                    yield return new ConditionalCode();
                    break;
                case StorageMode.Tree:
                    yield return new BinarySearchCode();
                    break;
                case StorageMode.Indexed:
                    yield return new HashSetChainCode();
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported StorageMode {config.StorageMode}");
            }
        }
    }

    /// <summary>This method is used by tests</summary>
    public static string Generate(DataStructure ds, FastDataConfig config, IGenerator generator)
    {
        PreProcess(config, out KnownDataType dataType, out DataProperties props, out IEarlyExit[] exits);

        object candidate = ds switch
        {
            DataStructure.SingleValue => new SingleValueCode(),
            DataStructure.Array => new ArrayCode(),
            DataStructure.Conditional => new ConditionalCode(),
            DataStructure.BinarySearch => new BinarySearchCode(),
            DataStructure.EytzingerSearch => new EytzingerSearchCode(),
            DataStructure.MinimalPerfectHash => new MinimalPerfectHashCode(),
            DataStructure.HashSetChain => new HashSetChainCode(),
            DataStructure.HashSetLinear => new HashSetLinearCode(),
            DataStructure.KeyLength => new KeyLengthCode(),
            DataStructure.UniqueKeyLength => new UniqueKeyLengthCode(),
            _ => throw new ArgumentOutOfRangeException(nameof(ds), ds, null)
        };

        if (!TryProcessCandidate(candidate, props, exits, dataType, generator, config, out string? code))
            throw new InvalidOperationException("This should not happen");

        return code;
    }

    private static DataProperties GetDataProperties(object[] data, KnownDataType dataType)
    {
        DataProperties props = new DataProperties();

        switch (dataType)
        {
            case KnownDataType.SByte:
                props.IntProps = DataAnalyzer.GetSByteProperties(data);
                break;
            case KnownDataType.Byte:
                props.UIntProps = DataAnalyzer.GetByteProperties(data);
                break;
            case KnownDataType.Int16:
                props.IntProps = DataAnalyzer.GetInt16Properties(data);
                break;
            case KnownDataType.UInt16:
                props.UIntProps = DataAnalyzer.GetUInt16Properties(data);
                break;
            case KnownDataType.Int32:
                props.IntProps = DataAnalyzer.GetInt32Properties(data);
                break;
            case KnownDataType.UInt32:
                props.UIntProps = DataAnalyzer.GetUInt32Properties(data);
                break;
            case KnownDataType.Int64:
                props.IntProps = DataAnalyzer.GetInt64Properties(data);
                break;
            case KnownDataType.UInt64:
                props.UIntProps = DataAnalyzer.GetUInt64Properties(data);
                break;
            case KnownDataType.String:
                props.StringProps = DataAnalyzer.GetStringProperties(data);
                break;
            case KnownDataType.Boolean:
                break;
            case KnownDataType.Char:
                props.CharProps = DataAnalyzer.GetCharProperties(data);
                break;
            case KnownDataType.Single:
                props.FloatProps = DataAnalyzer.GetSingleProperties(data);
                break;
            case KnownDataType.Double:
                props.FloatProps = DataAnalyzer.GetDoubleProperties(data);
                break;
            case KnownDataType.Unknown:
                //Do nothing
                break;
            default:
                throw new InvalidOperationException($"Unknown data type: {dataType}");
        }

        return props;
    }

    private static bool TryProcessCandidate(object candidate, DataProperties props, IEarlyExit[] exits, KnownDataType dataType, IGenerator generator, FastDataConfig config, out string? code)
    {
        if (candidate is IHashStructure hs)
        {
            IHashSpec? spec = null;

            if (props.StringProps != null)
            {
                if (config.AnalyzerConfig is BruteForceAnalyzerConfig bfCfg)
                {
                    BruteForceAnalyzer analyzer = new BruteForceAnalyzer(config.Data, props.StringProps.Value, bfCfg, hs.RunSimulation);
                    spec = analyzer.Run().Spec;
                }
                if (config.AnalyzerConfig is GeneticAnalyzerConfig gaCfg)
                {
                    GeneticAnalyzer analyzer = new GeneticAnalyzer(config.Data, props.StringProps.Value, gaCfg, hs.RunSimulation);
                    spec = analyzer.Run().Spec;
                }
            }
            else
                spec = DefaultHashSpec.Instance;

            code = generator.Generate(new GeneratorConfig(dataType, exits, spec), config, hs.Create(config.Data, HashHelper.HashObject));
            return true;
        }

        if (candidate is IStructure s)
        {
            code = generator.Generate(new GeneratorConfig(dataType, exits, null), config, s.Create(config.Data));
            return true;
        }

        code = null;
        return false;
    }
}