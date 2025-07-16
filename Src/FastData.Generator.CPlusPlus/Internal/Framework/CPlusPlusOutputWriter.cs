using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal abstract class CPlusPlusOutputWriter<TKey, TValue>(TValue[]? values) : OutputWriter<TKey>
{
    protected ObjectType? ObjectType => values != null ? ObjectType.Create(values, TypeMap) : null;
    protected string ValueString => ObjectType != null ? ObjectType.IsCustomType ? PrintValues(ObjectType) : FormatColumns(values!, ToValueLabel) : FormatColumns(values!, ToValueLabel);
    protected string TypeName => ObjectType != null && ObjectType.IsCustomType ? $"{ValueTypeName}*" : ValueTypeName;
    protected string FieldModifier => "static constexpr ";
    protected string MethodModifier => "static ";
    protected string PostMethodModifier => " noexcept";
    protected string MethodAttribute => "[[nodiscard]]";
    protected string GetFieldModifier(bool value) => value ? FieldModifier : "inline static const ";
}