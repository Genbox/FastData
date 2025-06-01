using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal abstract class CPlusPlusOutputWriter<T> : OutputWriter<T>
{
    protected string GetFieldModifier(bool value) => value ? FieldModifier : "inline static const ";

    protected string FieldModifier => "static constexpr ";
    protected string MethodModifier => "static ";
    protected string PostMethodModifier => " noexcept";
    protected string MethodAttribute => "[[nodiscard]]";
}