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

    protected string GetCompareFunction(string var1, string var2)
    {
        if (GeneratorConfig.KeyType == KeyType.String && GeneratorConfig.IgnoreCase)
            return $"case_insensitive_compare({var1}, {var2})";

        return $"if {var1} < {var2} {{ -1 }} else if {var1} > {var2} {{ 1 }} else {{ 0 }}";
    }

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
        string pre = GeneratorConfig.IgnoreCase ? $"case_insensitive_starts_with(key, {ToValueLabel(TrimPrefix)})" : $"key.starts_with({ToValueLabel(TrimPrefix)})";
        string suf = GeneratorConfig.IgnoreCase ? $"case_insensitive_ends_with(key, {ToValueLabel(TrimSuffix)})" : $"key.ends_with({ToValueLabel(TrimSuffix)})";

        if (TrimPrefix.Length == 0)
            return suf;

        if (TrimSuffix.Length == 0)
            return pre;

        return $"{pre} && {suf}";
    }

    protected override string GetEqualFunctionInternal(string value1, string value2, KeyType keyType)
    {
        if (keyType == KeyType.String && GeneratorConfig.IgnoreCase)
            return $"case_insensitive_equals({value1}, {value2})";

        return $"{value1} == {value2}";
    }

    protected override void RegisterSharedCode()
    {
        if (GeneratorConfig.KeyType != KeyType.String || !GeneratorConfig.IgnoreCase)
            return;

        Shared.Add(CodePlacement.Before, """
                                         #[inline(always)]
                                         fn to_lower_ascii(value: u8) -> u8 {
                                             let upper = value.wrapping_sub(b'A');
                                             if upper <= (b'Z' - b'A') { value | 0x20 } else { value }
                                         }

                                         #[inline(always)]
                                         fn case_insensitive_equals(a: &str, b: &str) -> bool {
                                             let a_bytes = a.as_bytes();
                                             let b_bytes = b.as_bytes();

                                             if a_bytes.len() != b_bytes.len() {
                                                 return false;
                                             }

                                             let len = a_bytes.len();
                                             let mut i = 0;
                                             while i < len {
                                                 if to_lower_ascii(a_bytes[i]) != to_lower_ascii(b_bytes[i]) {
                                                     return false;
                                                 }

                                                 i += 1;
                                             }

                                             true
                                         }

                                         #[inline(always)]
                                         fn case_insensitive_compare(a: &str, b: &str) -> i32 {
                                             let a_bytes = a.as_bytes();
                                             let b_bytes = b.as_bytes();
                                             let a_len = a_bytes.len();
                                             let b_len = b_bytes.len();
                                             let len = if a_len < b_len { a_len } else { b_len };

                                             let mut i = 0;
                                             while i < len {
                                                 let ca = to_lower_ascii(a_bytes[i]);
                                                 let cb = to_lower_ascii(b_bytes[i]);

                                                 if ca != cb {
                                                     return if ca < cb { -1 } else { 1 };
                                                 }

                                                 i += 1;
                                             }

                                             if a_len == b_len { 0 } else if a_len < b_len { -1 } else { 1 }
                                         }

                                         #[inline(always)]
                                         fn case_insensitive_starts_with(value: &str, prefix: &str) -> bool {
                                             let value_bytes = value.as_bytes();
                                             let prefix_bytes = prefix.as_bytes();
                                             let prefix_len = prefix_bytes.len();

                                             if prefix_len > value_bytes.len() {
                                                 return false;
                                             }

                                             let mut i = 0;
                                             while i < prefix_len {
                                                 if to_lower_ascii(value_bytes[i]) != to_lower_ascii(prefix_bytes[i]) {
                                                     return false;
                                                 }

                                                 i += 1;
                                             }

                                             true
                                         }

                                         #[inline(always)]
                                         fn case_insensitive_ends_with(value: &str, suffix: &str) -> bool {
                                             let value_bytes = value.as_bytes();
                                             let suffix_bytes = suffix.as_bytes();
                                             let suffix_len = suffix_bytes.len();
                                             let value_len = value_bytes.len();

                                             if suffix_len > value_len {
                                                 return false;
                                             }

                                             let offset = value_len - suffix_len;
                                             let mut i = 0;
                                             while i < suffix_len {
                                                 if to_lower_ascii(value_bytes[offset + i]) != to_lower_ascii(suffix_bytes[i]) {
                                                     return false;
                                                 }

                                                 i += 1;
                                             }

                                             true
                                         }
                                         """);
    }
}