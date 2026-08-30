using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Selects a target-language string definition according to the generator encoding.</summary>
/// <param name="types">The string definitions available for each supported encoding.</param>
public class DynamicStringTypeDef(params StringType[] types) : ITypeDef<string>
{
    /// <inheritdoc />
    public TypeCode KeyType => TypeCode.String;

    /// <inheritdoc />
    public string Name => throw new InvalidOperationException("DynamicStringTypeDef does not support Name");

    /// <inheritdoc />
    public Func<TypeMap, object, string> PrintObj => throw new InvalidOperationException("DynamicStringTypeDef does not support PrintObj");

    /// <inheritdoc />
    public Func<TypeMap, string, string> Print => throw new InvalidOperationException("DynamicStringTypeDef does not support Print");

    internal StringType Get(GeneratorEncoding encoding)
    {
        foreach (StringType type in types)
        {
            if (type.Encoding == encoding)
                return type;
        }

        throw new InvalidOperationException($"No string type found for encoding {encoding}");
    }
}