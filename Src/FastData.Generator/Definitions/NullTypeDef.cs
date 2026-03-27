using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

public class NullTypeDef(string nullLabel) : ITypeDef
{
    public TypeCode KeyType => TypeCode.Empty;
    public string Name => throw new InvalidOperationException("Null does not have a name");
    public Func<TypeMap, object, string> PrintObj { get; } = (_, _) => nullLabel;
}