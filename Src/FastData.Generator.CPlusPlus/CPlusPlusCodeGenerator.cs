using Genbox.FastData.Generator.CPlusPlus.Internal;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.TemplateData;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Template.Extensions;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusCodeGenerator : CodeGenerator
{
    private readonly CPlusPlusCodeGeneratorConfig _cfg;

    private CPlusPlusCodeGenerator(CPlusPlusCodeGeneratorConfig cfg, ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef, TypeMap helper)
        : base(langDef, constDef, earlyExitDef, hashDef, helper, null) => _cfg = cfg;

    public static CPlusPlusCodeGenerator Create(CPlusPlusCodeGeneratorConfig userCfg)
    {
        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);

        return new CPlusPlusCodeGenerator(userCfg, langDef, new CPlusPlusConstantsDef(), new CPlusPlusEarlyExitDef(map, userCfg.GeneratorOptions), new CPlusPlusHashDef(), map);
    }

    public override GeneratorEncoding Encoding => GeneratorEncoding.UTF8;

    public override string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context)
    {
        //C++ generator does not support chars outside ASCII
        if (genCfg.KeyType == KeyType.Char && (char)(object)genCfg.Constants.MaxValue > 127)
            throw new InvalidOperationException("C++ generator does not support chars outside ASCII. Please use a different data type or reduce the max value to 127 or lower.");

        return base.Generate<TKey, TValue>(genCfg, context);
    }

    protected override void AppendHeader<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, IContext context)
    {
        base.AppendHeader<TKey, TValue>(sb, genCfg, context);

        sb.AppendLine("""
                      #pragma once
                      #include <array>
                      #include <cstring>
                      #include <cstdint>
                      #include <limits>
                      #include <string_view>

                      """);
    }

    protected override void AppendBody<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, string keyTypeName, string valueTypeName, IContext context)
    {
        sb.AppendLine($$"""
                        class {{_cfg.ClassName}} final {
                        """);

        base.AppendBody<TKey, TValue>(sb, genCfg, keyTypeName, valueTypeName, context);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);
        sb.Append("};");
    }

    protected override OutputWriter<TKey> GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context) => new TemplateBasedOutputWriter<TKey, TValue>(context);

    private sealed class TemplateBasedOutputWriter<TKey, TValue>(IContext context) : OutputWriter<TKey>
    {
        public override string Generate()
        {
            string raw = context.GetType().Name;
            int idx = raw.IndexOf("Context", StringComparison.Ordinal);
            string name = raw.Substring(0, idx) + "Code.t4";
            string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates", "CPlusPlus", name));

            return TemplateHelper.Render(this, name, source, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Model", new TemplateModel
                    {
                        KeyType = KeyType,
                        HashSource = HashSource,
                        MethodAttribute = "[[nodiscard]]",
                        PostMethodModifier = " noexcept",
                        KeyTypeName = KeyTypeName,
                        ValueTypeName = ValueTypeName,
                        IsPrimitive = typeof(TValue).IsPrimitive,
                        GetMethodHeader = GetMethodHeader,
                        GetEqualFunction = (a, b) => GetEqualFunction(a, b),
                        GetEqualFunctionByType = GetEqualFunction,
                        GetCompareFunction = GetCompareFunction,
                        GetModFunction = GetModFunction,
                        GetSmallestSignedType = GetSmallestSignedType,
                        GetSmallestUnsignedType = GetSmallestUnsignedType,
                        ToValueLabel = ToValueLabel,
                        ValueObjectDeclarations = GetObjectDeclarations<TValue>(),
                        GetMethodModifier = constExpr => constExpr ? "static constexpr " : "static ",
                        GetFieldModifier = constExpr => constExpr ? "static constexpr " : "inline static const ",
                        GetValueTypeName = customType => customType ? ValueTypeName + "*" : ValueTypeName
                    }
                },
                { "Context", context },
                { "Shared", Shared },
                { "Data", CreateContextModel() }
            });
        }

        private ITemplateData? CreateContextModel()
        {
            switch (context)
            {
                case ArrayContext<TKey, TValue> arrayCtx:
                    return new ArrayTemplateData
                    {
                        Keys = (arrayCtx.Keys).ToObjects(),
                        KeyCount = arrayCtx.Keys.Length,
                        Values = (arrayCtx.Values).ToObjects(),
                        ValueCount = arrayCtx.Values.Length
                    };

                case BinarySearchContext<TKey, TValue> bsCtx:
                    return new BinarySearchTemplateData
                    {
                        Keys = (bsCtx.Keys).ToObjects(),
                        KeyCount = bsCtx.Keys.Length,
                        Values = (bsCtx.Values).ToObjects(),
                        ValueCount = bsCtx.Values.Length
                    };

                case ConditionalContext<TKey, TValue> conCtx:
                    return new ArrayTemplateData
                    {
                        Keys = (conCtx.Keys).ToObjects(),
                        KeyCount = conCtx.Keys.Length,
                        Values = (conCtx.Values).ToObjects(),
                        ValueCount = conCtx.Values.Length
                    };

                case SingleValueContext<TKey, TValue> singleCtx:
                    return new SingleValueTemplateData
                    {
                        Item = singleCtx.Key,
                        Value = singleCtx.Values.IsEmpty ? null : singleCtx.Values.Span[0]
                    };

                case RangeContext<TKey> rangeCtx:
                    return new RangeTemplateData
                    {
                        Min = rangeCtx.Min,
                        Max = rangeCtx.Max
                    };

                case KeyLengthContext<TValue> klCtx:
                    return new KeyLengthTemplateData
                    {
                        Keys = klCtx.Lengths,
                        KeyCount = klCtx.Lengths.Length,
                        Values = (klCtx.Values).ToObjects(),
                        ValueCount = klCtx.Values.Length
                    };

                case BloomFilterContext:
                    return null;

                case BitSetContext<TValue> bsCtx:
                    return new BitSetTemplateData
                    {
                        Values = (bsCtx.Values).ToObjects(),
                        ValueCount = bsCtx.Values.Length
                    };

                case HashTableContext<TKey, TValue> hashCtx:
                    HashTableEntryTemplateData[] hashEntries = new HashTableEntryTemplateData[hashCtx.Entries.Length];

                    for (int i = 0; i < hashCtx.Entries.Length; i++)
                    {
                        hashEntries[i] = new HashTableEntryTemplateData
                        {
                            Key = hashCtx.Entries[i].Key,
                            Hash = hashCtx.Entries[i].Hash,
                            Next = hashCtx.Entries[i].Next
                        };
                    }

                    return new HashTableTemplateData
                    {
                        Entries = hashEntries,
                        Values = (hashCtx.Values).ToObjects(),
                        ValueCount = hashCtx.Values.Length
                    };

                case HashTableCompactContext<TKey, TValue> compactCtx:
                    HashTableCompactEntryTemplateData[] compactEntries = new HashTableCompactEntryTemplateData[compactCtx.Entries.Length];

                    for (int i = 0; i < compactCtx.Entries.Length; i++)
                    {
                        compactEntries[i] = new HashTableCompactEntryTemplateData
                        {
                            Key = compactCtx.Entries[i].Key,
                            Hash = compactCtx.Entries[i].Hash
                        };
                    }

                    return new HashTableCompactTemplateData
                    {
                        Entries = compactEntries,
                        Values = (compactCtx.Values).ToObjects(),
                        ValueCount = compactCtx.Values.Length
                    };

                case HashTablePerfectContext<TKey, TValue> perfectCtx:
                    HashTablePerfectEntryTemplateData[] perfectEntries = new HashTablePerfectEntryTemplateData[perfectCtx.Data.Length];

                    for (int i = 0; i < perfectCtx.Data.Length; i++)
                    {
                        perfectEntries[i] = new HashTablePerfectEntryTemplateData
                        {
                            Key = perfectCtx.Data[i].Key,
                            Hash = perfectCtx.Data[i].Value
                        };
                    }

                    return new HashTablePerfectTemplateData
                    {
                        Entries = perfectEntries,
                        Values = (perfectCtx.Values).ToObjects(),
                        ValueCount = perfectCtx.Values.Length
                    };

                case RrrBitVectorContext:
                    return null;

                case EliasFanoContext<TKey>:
                    return null;

                default:
                    throw new InvalidOperationException("No template mapping found for context type: " + context.GetType().FullName);
            }
        }

        private string GetCompareFunction(string var1, string var2)
        {
            if (KeyType == KeyType.String)
            {
                if (IgnoreCase)
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
                sb.Append($"        const auto {TrimmedKeyName} = {InputKeyName}.substr({TrimPrefix.Length.ToStringInvariant()}, {InputKeyName}.length() - {TotalTrimLength.ToStringInvariant()});");

            return sb.ToString();
        }

        protected override string GetEqualFunctionInternal(string value1, string value2, KeyType keyType)
        {
            if (keyType == KeyType.String && IgnoreCase)
                return $"case_insensitive_equals({value1}, {value2})";

            return $"{value1} == {value2}";
        }

        protected override void RegisterSharedCode()
        {
            if (KeyType != KeyType.String || !IgnoreCase)
                return;

            string helpers = Encoding switch
            {
                GeneratorEncoding.UTF16 => GetUtf16CaseInsensitiveHelpers(),
                GeneratorEncoding.UTF32 => GetUtf32CaseInsensitiveHelpers(),
                GeneratorEncoding.UTF8 or GeneratorEncoding.ASCII => GetAsciiCaseInsensitiveHelpers(),
                _ => throw new InvalidOperationException($"Unsupported encoding: {Encoding}")
            };

            Shared.Add(CodePlacement.Before, helpers);
        }

        private string GetAsciiCaseInsensitiveHelpers() => $$"""
                                                             static constexpr uint32_t to_lower_ascii(uint32_t c) noexcept
                                                             {
                                                                 if (c - 'A' <= 'Z' - 'A')
                                                                     c |= 0x20u;

                                                                 return c;
                                                             }

                                                             static constexpr bool case_insensitive_equals({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
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

                                                             static constexpr int case_insensitive_compare({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
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

                                                             static constexpr bool case_insensitive_starts_with({{KeyTypeName}} value, {{KeyTypeName}} prefix) noexcept
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

                                                             static constexpr bool case_insensitive_ends_with({{KeyTypeName}} value, {{KeyTypeName}} suffix) noexcept
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
                                                             """;

        private string GetUtf16CaseInsensitiveHelpers() => $$"""
                                                             static constexpr char16_t to_lower_ascii(char16_t c) noexcept
                                                             {
                                                                 if (c - u'A' <= u'Z' - u'A')
                                                                     c = static_cast<char16_t>(c | u'\x20');

                                                                 return c;
                                                             }

                                                             static constexpr bool case_insensitive_equals({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
                                                             {
                                                                 if (a.size() != b.size())
                                                                     return false;

                                                                 size_t len = a.size();
                                                                 for (size_t i = 0; i < len; i++)
                                                                 {
                                                                     if (to_lower_ascii(a[i]) != to_lower_ascii(b[i]))
                                                                         return false;
                                                                 }

                                                                 return true;
                                                             }

                                                             static constexpr int case_insensitive_compare({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
                                                             {
                                                                 size_t a_len = a.size();
                                                                 size_t b_len = b.size();
                                                                 size_t len = a_len < b_len ? a_len : b_len;

                                                                 for (size_t i = 0; i < len; i++)
                                                                 {
                                                                     auto ca = to_lower_ascii(a[i]);
                                                                     auto cb = to_lower_ascii(b[i]);

                                                                     if (ca != cb)
                                                                         return ca < cb ? -1 : 1;
                                                                 }

                                                                 if (a_len == b_len)
                                                                     return 0;

                                                                 return a_len < b_len ? -1 : 1;
                                                             }

                                                             static constexpr bool case_insensitive_starts_with({{KeyTypeName}} value, {{KeyTypeName}} prefix) noexcept
                                                             {
                                                                 size_t prefix_len = prefix.size();
                                                                 if (prefix_len > value.size())
                                                                     return false;

                                                                 for (size_t i = 0; i < prefix_len; i++)
                                                                 {
                                                                     if (to_lower_ascii(value[i]) != to_lower_ascii(prefix[i]))
                                                                         return false;
                                                                 }

                                                                 return true;
                                                             }

                                                             static constexpr bool case_insensitive_ends_with({{KeyTypeName}} value, {{KeyTypeName}} suffix) noexcept
                                                             {
                                                                 size_t suffix_len = suffix.size();
                                                                 size_t value_len = value.size();

                                                                 if (suffix_len > value_len)
                                                                     return false;

                                                                 size_t offset = value_len - suffix_len;

                                                                 for (size_t i = 0; i < suffix_len; i++)
                                                                 {
                                                                     if (to_lower_ascii(value[offset + i]) != to_lower_ascii(suffix[i]))
                                                                         return false;
                                                                 }

                                                                 return true;
                                                             }
                                                             """;

        private string GetUtf32CaseInsensitiveHelpers() => $$"""
                                                             static constexpr char32_t to_lower_ascii(char32_t c) noexcept
                                                             {
                                                                 if (c - U'A' <= U'Z' - U'A')
                                                                     c = static_cast<char32_t>(c | U'\x20');

                                                                 return c;
                                                             }

                                                             static constexpr bool case_insensitive_equals({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
                                                             {
                                                                 if (a.size() != b.size())
                                                                     return false;

                                                                 size_t len = a.size();
                                                                 for (size_t i = 0; i < len; i++)
                                                                 {
                                                                     if (to_lower_ascii(a[i]) != to_lower_ascii(b[i]))
                                                                         return false;
                                                                 }

                                                                 return true;
                                                             }

                                                             static constexpr int case_insensitive_compare({{KeyTypeName}} a, {{KeyTypeName}} b) noexcept
                                                             {
                                                                 size_t a_len = a.size();
                                                                 size_t b_len = b.size();
                                                                 size_t len = a_len < b_len ? a_len : b_len;

                                                                 for (size_t i = 0; i < len; i++)
                                                                 {
                                                                     auto ca = to_lower_ascii(a[i]);
                                                                     auto cb = to_lower_ascii(b[i]);

                                                                     if (ca != cb)
                                                                         return ca < cb ? -1 : 1;
                                                                 }

                                                                 if (a_len == b_len)
                                                                     return 0;

                                                                 return a_len < b_len ? -1 : 1;
                                                             }

                                                             static constexpr bool case_insensitive_starts_with({{KeyTypeName}} value, {{KeyTypeName}} prefix) noexcept
                                                             {
                                                                 size_t prefix_len = prefix.size();
                                                                 if (prefix_len > value.size())
                                                                     return false;

                                                                 for (size_t i = 0; i < prefix_len; i++)
                                                                 {
                                                                     if (to_lower_ascii(value[i]) != to_lower_ascii(prefix[i]))
                                                                         return false;
                                                                 }

                                                                 return true;
                                                             }

                                                             static constexpr bool case_insensitive_ends_with({{KeyTypeName}} value, {{KeyTypeName}} suffix) noexcept
                                                             {
                                                                 size_t suffix_len = suffix.size();
                                                                 size_t value_len = value.size();

                                                                 if (suffix_len > value_len)
                                                                     return false;

                                                                 size_t offset = value_len - suffix_len;

                                                                 for (size_t i = 0; i < suffix_len; i++)
                                                                 {
                                                                     if (to_lower_ascii(value[offset + i]) != to_lower_ascii(suffix[i]))
                                                                         return false;
                                                                 }

                                                                 return true;
                                                             }
                                                             """;
    }
}