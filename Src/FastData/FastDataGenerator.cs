using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.Techniques.BruteForce;
using Genbox.FastData.Internal.Analysis.Techniques.Genetic;
using Genbox.FastData.Internal.Optimization;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData;

public static class FastDataGenerator
{
    public static bool TryGenerate(FastDataConfig config, IGenerator generator, out string? source)
    {
        PreProcess(config, out KnownDataType dataType, out DataProperties props, out IEarlyExit[] exits);

        Constants constants = new Constants((uint)config.Data.Length);

        if (props.StringProps.HasValue)
        {
            constants.MinValue = props.StringProps.Value.LengthData.Min;
            constants.MaxValue = props.StringProps.Value.LengthData.Max;
        }
        else if (props.IntProps.HasValue)
        {
            constants.MinValue = props.IntProps.Value.MinValue;
            constants.MaxValue = props.IntProps.Value.MaxValue;
        }
        else if (props.UIntProps.HasValue)
        {
            constants.MinValue = props.UIntProps.Value.MinValue;
            constants.MaxValue = props.UIntProps.Value.MaxValue;
        }
        else if (props.FloatProps.HasValue)
        {
            constants.MinValue = props.FloatProps.Value.MinValue;
            constants.MaxValue = props.FloatProps.Value.MaxValue;
        }
        else if (props.CharProps.HasValue)
        {
            constants.MinValue = props.CharProps.Value.MinValue;
            constants.MaxValue = props.CharProps.Value.MaxValue;
        }

        Metadata metadata = new Metadata(typeof(FastDataGenerator).Assembly.GetName().Version!, DateTimeOffset.Now);
        GeneratorConfig genCfg = new GeneratorConfig(dataType, exits, null, config.StringComparison, constants, metadata);

        foreach (object candidate in GetDataStructureCandidates(config))
        {
            if (TryProcessCandidate(candidate, props, genCfg, dataType, generator, config, out source))
                return true;
        }

        source = null;
        return false;
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
        DataStructure ds = config.DataStructure;

        if (ds == DataStructure.Auto)
        {
            if (config.Data.Length == 1)
                yield return new SingleValueStructure();
            else
            {
                // For small amounts of data, logic is the fastest, so we try that first
                yield return new ConditionalStructure();

                // We try key lengths
                yield return new KeyLengthStructure();

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory) && config.StorageOptions.HasFlag(StorageOption.OptimizeForSpeed))
                    yield return new PerfectHashBruteForceStructure();

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                    yield return new BinarySearchStructure();
                else
                    yield return new HashSetChainStructure();
            }
        }
        else if (ds == DataStructure.SingleValue)
            yield return new SingleValueStructure();
        else if (ds == DataStructure.Array)
            yield return new ArrayStructure();
        else if (ds == DataStructure.Conditional)
            yield return new ConditionalStructure();
        else if (ds == DataStructure.BinarySearch)
            yield return new BinarySearchStructure();
        else if (ds == DataStructure.EytzingerSearch)
            yield return new EytzingerSearchStructure();
        else if (ds == DataStructure.PerfectHashGPerf)
            yield return new PerfectHashGPerfStructure();
        else if (ds == DataStructure.PerfectHashBruteForce)
            yield return new PerfectHashBruteForceStructure();
        else if (ds == DataStructure.HashSetChain)
            yield return new HashSetChainStructure();
        else if (ds == DataStructure.HashSetLinear)
            yield return new HashSetLinearStructure();
        else if (ds == DataStructure.KeyLength)
            yield return new KeyLengthStructure();
        else
            throw new InvalidOperationException($"Unsupported DataStructure {ds}");
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

    private static bool TryProcessCandidate(object candidate, DataProperties props, GeneratorConfig genCfg, KnownDataType dataType, IGenerator generator, FastDataConfig config, out string? code)
    {
        if (candidate is IHashStructure hs)
        {
            if (props.StringProps != null)
            {
                if (config.AnalyzerConfig is BruteForceAnalyzerConfig bfCfg)
                {
                    BruteForceAnalyzer analyzer = new BruteForceAnalyzer(config.Data, props.StringProps.Value, bfCfg, hs.RunSimulation);
                    genCfg.HashSpec = analyzer.Run().Spec;
                }
                if (config.AnalyzerConfig is GeneticAnalyzerConfig gaCfg)
                {
                    GeneticAnalyzer analyzer = new GeneticAnalyzer(config.Data, props.StringProps.Value, gaCfg, hs.RunSimulation);
                    genCfg.HashSpec = analyzer.Run().Spec;
                }
            }
            else
                genCfg.HashSpec = DefaultHashSpec.Instance;

            if (!hs.TryCreate(config.Data, dataType, props, config, HashHelper.HashObject, out IContext? context))
            {
                code = null;
                return false;
            }

            code = generator.Generate(genCfg, config, context!);
            return true;
        }

        if (candidate is IStructure s)
        {
            if (!s.TryCreate(config.Data, dataType, props, config, out IContext? context))
            {
                code = null;
                return false;
            }

            code = generator.Generate(genCfg, config, context!);
            return true;
        }

        code = null;
        return false;
    }
}