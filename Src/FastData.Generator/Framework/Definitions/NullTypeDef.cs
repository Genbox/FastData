using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class NullTypeDef(string nullLabel) : ITypeDef
{
    public KeyType KeyType => KeyType.Null;
    public string Name => throw new InvalidOperationException("Null does not have a name");
    public Func<TypeMap, object, string> PrintObj { get; } = (_, _) => nullLabel;
}