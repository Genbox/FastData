using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData;

public class GeneratorConfig(KnownDataType dataType, IEarlyExit[] earlyExits, IHashSpec? hashSpec)
{
    public KnownDataType DataType { get; set; } = dataType;
    public IEarlyExit[] EarlyExits { get; set; } = earlyExits;
    public IHashSpec? HashSpec { get; set; } = hashSpec;
}