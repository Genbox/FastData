using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal abstract class CPlusPlusOutputWriter<TKey> : OutputWriter<TKey>
{
    protected string PostMethodModifier => " noexcept";
    protected string MethodAttribute => "[[nodiscard]]";
    protected string GetMethodModifier(bool constExpr) => constExpr ? "static constexpr " : "static ";
    protected string GetFieldModifier(bool constExpr) => constExpr ? "static constexpr " : "inline static const ";
    protected string GetValueTypeName(bool customType) => customType ? ValueTypeName + "*" : ValueTypeName;
}