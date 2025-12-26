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

    protected string GetCompareFunction(string var1, string var2)
    {
        if (GeneratorConfig.KeyType == KeyType.String)
        {
            if (GeneratorConfig.IgnoreCase)
                return $"case_insensitive_compare({var1}, {var2})";

            return $"{var1}.compare({var2})";
        }

        return $"{var1} < {var2} ? -1 : ({var1} > {var2} ? 1 : 0)";
    }

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
        string prefixCheck = GeneratorConfig.IgnoreCase ? $"case_insensitive_starts_with(key, {ToValueLabel(TrimPrefix)})" : $"key.compare(0, {TrimPrefix.Length.ToStringInvariant()}, {ToValueLabel(TrimPrefix)}) == 0";
        string suffixCheck = GeneratorConfig.IgnoreCase ? $"case_insensitive_ends_with(key, {ToValueLabel(TrimSuffix)})" : $"key.compare(key.length() - {TrimSuffix.Length.ToStringInvariant()}, {TrimSuffix.Length.ToStringInvariant()}, {ToValueLabel(TrimSuffix)}) == 0";

        if (TrimPrefix.Length == 0)
            return suffixCheck;

        if (TrimSuffix.Length == 0)
            return prefixCheck;

        return $"{prefixCheck} && {suffixCheck}";
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

        Shared.Add(CodePlacement.Before, $$"""
                                           static inline uint32_t to_lower_ascii(uint32_t c) noexcept
                                           {
                                               if (c - 'A' <= 'Z' - 'A')
                                                   c |= 0x20u;

                                               return c;
                                           }

                                           static inline bool case_insensitive_equals({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
                                           {
                                               if (a.size() != b.size())
                                                   return false;

                                               size_t len = a.size();
                                               for (size_t i = 0; i < len; i++)
                                               {
                                                   if (to_lower_ascii(static_cast<uint32_t>(a[i])) != to_lower_ascii(static_cast<uint32_t>(b[i])))
                                                       return false;
                                               }

                                               return true;
                                           }

                                           static inline int case_insensitive_compare({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
                                           {
                                               size_t a_len = a.size();
                                               size_t b_len = b.size();
                                               size_t len = a_len < b_len ? a_len : b_len;

                                               for (size_t i = 0; i < len; i++)
                                               {
                                                   uint32_t ca = to_lower_ascii(static_cast<uint32_t>(a[i]));
                                                   uint32_t cb = to_lower_ascii(static_cast<uint32_t>(b[i]));

                                                   if (ca != cb)
                                                       return ca < cb ? -1 : 1;
                                               }

                                               if (a_len == b_len)
                                                   return 0;

                                               return a_len < b_len ? -1 : 1;
                                           }

                                           static inline bool case_insensitive_starts_with({{KeyTypeName}} value, {{KeyTypeName}} prefix) noexcept
                                           {
                                               size_t prefix_len = prefix.size();
                                               if (prefix_len > value.size())
                                                   return false;

                                               for (size_t i = 0; i < prefix_len; i++)
                                               {
                                                   if (to_lower_ascii(static_cast<uint32_t>(value[i])) != to_lower_ascii(static_cast<uint32_t>(prefix[i])))
                                                       return false;
                                               }

                                               return true;
                                           }

                                           static inline bool case_insensitive_ends_with({{KeyTypeName}} value, {{KeyTypeName}} suffix) noexcept
                                           {
                                               size_t suffix_len = suffix.size();
                                               size_t value_len = value.size();

                                               if (suffix_len > value_len)
                                                   return false;

                                               size_t offset = value_len - suffix_len;

                                               for (size_t i = 0; i < suffix_len; i++)
                                               {
                                                   if (to_lower_ascii(static_cast<uint32_t>(value[offset + i])) != to_lower_ascii(static_cast<uint32_t>(suffix[i])))
                                                       return false;
                                               }

                                               return true;
                                           }
                                           """);
    }
}