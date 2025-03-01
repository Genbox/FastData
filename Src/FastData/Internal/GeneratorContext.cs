using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Optimization;

namespace Genbox.FastData.Internal;

internal class GeneratorContext(FastDataConfig config)
{
    private IHashSpec? _hashSpec;
    private IEarlyExit[]? _earlyExits;
    private DataProperties? _dataProperties;

    internal DataProperties GetDataProperties()
    {
        if (_dataProperties == null)
        {
            _dataProperties = new DataProperties();

            switch (config.DataType)
            {
                case KnownDataType.SByte:
                    _dataProperties.IntProps = DataAnalyzer.GetSByteProperties(config.Data);
                    break;
                case KnownDataType.Byte:
                    _dataProperties.UIntProps = DataAnalyzer.GetByteProperties(config.Data);
                    break;
                case KnownDataType.Int16:
                    _dataProperties.IntProps = DataAnalyzer.GetInt16Properties(config.Data);
                    break;
                case KnownDataType.UInt16:
                    _dataProperties.UIntProps = DataAnalyzer.GetUInt16Properties(config.Data);
                    break;
                case KnownDataType.Int32:
                    _dataProperties.IntProps = DataAnalyzer.GetInt32Properties(config.Data);
                    break;
                case KnownDataType.UInt32:
                    _dataProperties.UIntProps = DataAnalyzer.GetUInt32Properties(config.Data);
                    break;
                case KnownDataType.Int64:
                    _dataProperties.IntProps = DataAnalyzer.GetInt64Properties(config.Data);
                    break;
                case KnownDataType.UInt64:
                    _dataProperties.UIntProps = DataAnalyzer.GetUInt64Properties(config.Data);
                    break;
                case KnownDataType.String:
                    _dataProperties.StringProps = DataAnalyzer.GetStringProperties(config.Data);
                    break;
                case KnownDataType.Boolean:
                    break;
                case KnownDataType.Char:
                    _dataProperties.CharProps = DataAnalyzer.GetCharProperties(config.Data);
                    break;
                case KnownDataType.Single:
                    _dataProperties.FloatProps = DataAnalyzer.GetSingleProperties(config.Data);
                    break;
                case KnownDataType.Double:
                    _dataProperties.FloatProps = DataAnalyzer.GetDoubleProperties(config.Data);
                    break;
                case KnownDataType.Unknown:
                    //Do nothing
                    break;
                default:
                    throw new InvalidOperationException($"Unknown data type: {config.DataType}");
            }
        }

        return _dataProperties;
    }

    internal IEarlyExit[] GetEarlyExits()
    {
        if (_earlyExits == null)
        {
            DataProperties props = GetDataProperties();

            if (props.StringProps.HasValue)
                _earlyExits = Optimizer.GetEarlyExits(props.StringProps.Value).ToArray();
            else if (props.IntProps.HasValue)
                _earlyExits = Optimizer.GetEarlyExits(props.IntProps.Value).ToArray();
            else if (props.UIntProps.HasValue)
                _earlyExits = Optimizer.GetEarlyExits(props.UIntProps.Value).ToArray();
            else if (props.CharProps.HasValue)
                _earlyExits = Optimizer.GetEarlyExits(props.CharProps.Value).ToArray();
            else if (props.FloatProps.HasValue)
                _earlyExits = Optimizer.GetEarlyExits(props.FloatProps.Value).ToArray();
            else
                _earlyExits = [];
        }

        return _earlyExits;
    }

    internal IHashSpec GetHashSpec()
    {
        if (_hashSpec == null)
        {
            //TODO: Generate
            _hashSpec = null;
        }

        return _hashSpec;
    }
}