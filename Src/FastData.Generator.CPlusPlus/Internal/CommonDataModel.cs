namespace Genbox.FastData.Generator.CPlusPlus.Internal;

public sealed class CommonDataModel
{
    public string MethodAttribute { get; init; }
    public string PostMethodModifier { get; init; }
    public string KeyTypeName { get; init; }
    public string ValueTypeName { get; init; }
    public string InputKeyName { get; init; }
    public string LookupKeyName { get; init; }
    public string ArraySizeType { get; init; }
    public string HashSizeType { get; init; }
    public bool IsPrimitive { get; init; }
}
