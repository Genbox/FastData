using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusCodeSpec : CodeSpec
{
    public override string GetFieldModifier() => "static constexpr ";
    public override string GetMethodModifier() => "static ";
    public override string GetMethodAttributes() => "[[nodiscard]]";
}