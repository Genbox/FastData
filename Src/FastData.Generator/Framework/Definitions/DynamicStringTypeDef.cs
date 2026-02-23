using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class DynamicStringTypeDef(params StringType[] types) : ITypeDef<string>
{
    internal StringType Get(GeneratorEncoding encoding)
    {
        foreach (StringType type in types)
        {
            if (type.Encoding == encoding)
                return type;
        }

        throw new InvalidOperationException($"No string type found for encoding {encoding}");
    }

    public TypeCode KeyType => TypeCode.String;
    public string Name => throw new InvalidOperationException("DynamicStringTypeDef does not support Name");
    public Func<TypeMap, object, string> PrintObj => throw new InvalidOperationException("DynamicStringTypeDef does not support PrintObj");
    public Func<TypeMap, string, string> Print => throw new InvalidOperationException("DynamicStringTypeDef does not support Print");
}