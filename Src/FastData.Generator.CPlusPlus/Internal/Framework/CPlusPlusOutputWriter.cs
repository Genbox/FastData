using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal abstract class CPlusPlusOutputWriter<TKey> : OutputWriter<TKey>
{
    protected string PostMethodModifier => " noexcept";
    protected string MethodAttribute => "[[nodiscard]]";
    protected string GetMethodModifier(bool constExpr) => constExpr ? "static constexpr " : "static ";
    protected string GetFieldModifier(bool constExpr) => constExpr ? "static constexpr " : "inline static const ";
    protected string GetValueTypeName(bool customType) => customType ? ValueTypeName + "*" : ValueTypeName;

    protected override string GetMethodHeader(MethodType methodType)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(base.GetMethodHeader(methodType));

        if (TotalTrimLength != 0)
            sb.Append($"""

                               if (!({GetTrimMatchCondition()}))
                                   {CPlusPlusEarlyExitDef.RenderExit(methodType)}

                               const auto trimmedKey = key.substr({TrimPrefix.Length.ToStringInvariant()}, key.length() - {TotalTrimLength.ToStringInvariant()});
                       """);

        return sb.ToString();
    }

    private string GetTrimMatchCondition()
    {
        string prefixCheck = $"key.compare(0, {TrimPrefix.Length.ToStringInvariant()}, {ToValueLabel(TrimPrefix)}) == 0";
        string suffixCheck = $"key.compare(key.length() - {TrimSuffix.Length.ToStringInvariant()}, {TrimSuffix.Length.ToStringInvariant()}, {ToValueLabel(TrimSuffix)}) == 0";

        if (TrimPrefix.Length == 0)
            return suffixCheck;

        if (TrimSuffix.Length == 0)
            return prefixCheck;

        return $"{prefixCheck} && {suffixCheck}";
    }
}