using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator.Rust;

public sealed class TemplateModel
{
    public required KeyType KeyType { get; init; }
    public required string HashSource { get; init; }
    public required string MethodAttribute { get; init; }
    public required string MethodModifier { get; init; }
    public required string FieldModifier { get; init; }
    public required string KeyTypeName { get; init; }
    public required string ValueTypeName { get; init; }
    public required bool IsPrimitive { get; init; }
    public required Func<MethodType, string> GetMethodHeader { get; init; }
    public required Func<string, string, string> GetEqualFunction { get; init; }
    public required Func<string, string, KeyType, string> GetEqualFunctionByType { get; init; }
    public required Func<string, string, string> GetCompareFunction { get; init; }
    public required Func<string, ulong, string> GetModFunction { get; init; }
    public required Func<long, string> GetSmallestSignedType { get; init; }
    public required Func<long, string> GetSmallestUnsignedType { get; init; }
    public required Func<object?, string> ToValueLabel { get; init; }
    public required string ValueObjectDeclarations { get; init; }
    public required Func<bool, string> GetKeyTypeName { get; init; }
    public required Func<bool, string> GetValueTypeName { get; init; }
}