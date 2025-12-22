using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal abstract class RustOutputWriter<T> : OutputWriter<T>
{
    protected string MethodModifier => "pub ";
    protected string MethodAttribute => "#[must_use]";
    protected string FieldModifier => "const ";
    protected string GetKeyTypeName(bool customType) => customType ? $"&'static {KeyTypeName}" : KeyTypeName;
    protected string GetValueTypeName(bool customType) => customType ? $"&'static {ValueTypeName}" : ValueTypeName;

    protected override string GetMethodHeader(MethodType methodType)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(base.GetMethodHeader(methodType));

        if (TotalTrimLength != 0)
            sb.Append($$"""

                                if !({{GetTrimMatchCondition()}}) {
                                    {{RustEarlyExitDef.RenderExit(methodType)}}
                                }

                                let trimmedKey = &key[{{TrimPrefix.Length.ToStringInvariant()}}..key.len() - {{TrimSuffix.Length.ToStringInvariant()}}];
                        """);

        return sb.ToString();
    }

    private string GetTrimMatchCondition()
    {
        string prefixCheck = $"key.starts_with({ToValueLabel(TrimPrefix)})";
        string suffixCheck = $"key.ends_with({ToValueLabel(TrimSuffix)})";

        if (TrimPrefix.Length == 0)
            return suffixCheck;

        if (TrimSuffix.Length == 0)
            return prefixCheck;

        return $"{prefixCheck} && {suffixCheck}";
    }
}