using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal abstract class CPlusPlusOutputWriter<TKey> : OutputWriter<TKey>
{
    protected string FieldModifier => "static constexpr ";
    protected string MethodModifier => "static constexpr ";
    protected string PostMethodModifier => " noexcept";
    protected string MethodAttribute => "[[nodiscard]]";
    protected string GetFieldModifier(bool value) => value ? FieldModifier : "inline static const ";
}