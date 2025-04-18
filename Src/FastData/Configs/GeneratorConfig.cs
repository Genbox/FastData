using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Configs;

public class GeneratorConfig(DataType dataType, IEarlyExit[] earlyExits, IHashSpec? hashSpec, StringComparison? stringComparison, Constants constants, Metadata metadata)
{
    public DataType DataType { get; } = dataType;
    public IEarlyExit[] EarlyExits { get; } = earlyExits;
    public IHashSpec? HashSpec { get; set; } = hashSpec;
    public StringComparison? StringComparison { get; } = stringComparison;
    public Constants Constants { get; } = constants;
    public Metadata Metadata { get; } = metadata;
}