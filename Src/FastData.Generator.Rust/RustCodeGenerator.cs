using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generator.Rust.TemplateData;
using Genbox.FastData.Generator.Template.Extensions;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust;

public sealed class RustCodeGenerator : CodeGenerator
{
    private readonly RustCodeGeneratorConfig _cfg;

    private RustCodeGenerator(RustCodeGeneratorConfig cfg, ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef, TypeMap map)
        : base(langDef, constDef, earlyExitDef, hashDef, map, null) => _cfg = cfg;

    public static RustCodeGenerator Create(RustCodeGeneratorConfig userCfg)
    {
        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);

        return new RustCodeGenerator(userCfg, langDef, new RustConstantsDef(), new RustEarlyExitDef(map, userCfg.GeneratorOptions), new RustHashDef(), map);
    }

    public override GeneratorEncoding Encoding => GeneratorEncoding.UTF8;

    protected override void AppendHeader<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, IContext context)
    {
        base.AppendHeader<TKey, TValue>(sb, genCfg, context);

        sb.Append($"""
                   #![allow(unused_parens)]
                   #![allow(missing_docs)]
                   #![allow(unused_imports)]
                   #![allow(unused_unsafe)]
                   use std::ptr;

                   pub struct {_cfg.ClassName};

                   """);
    }

    protected override void AppendBody<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, string keyTypeName, string valueTypeName, IContext context)
    {
        sb.Append($$"""

                    impl {{_cfg.ClassName}} {

                    """);

        base.AppendBody<TKey, TValue>(sb, genCfg, keyTypeName, valueTypeName, context);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);

        sb.Append('}');
    }

    protected override OutputWriter<TKey> GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context) => new TemplateBasedOutputWriter<TKey, TValue>(context);

    private sealed class TemplateBasedOutputWriter<TKey, TValue>(IContext context) : OutputWriter<TKey>
    {
        public override string Generate()
        {
            string raw = context.GetType().Name;
            int idx = raw.IndexOf("Context", StringComparison.Ordinal);
            string name = raw.Substring(0, idx) + "Code.t4";
            string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates", "Rust", name));

            return TemplateHelper.Render(this, name, source, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Model", new TemplateModel
                    {
                        HashSource = HashSource,
                        MethodAttribute = "#[must_use]",
                        MethodModifier = "pub ",
                        FieldModifier = "const ",
                        KeyTypeName = KeyTypeName,
                        KeyTypeCode = Type.GetTypeCode(typeof(TKey)),
                        ValueTypeName = ValueTypeName,
                        GetMethodHeader = GetMethodHeader,
                        GetEqualFunction = (a, b) => GetEqualFunction(a, b),
                        GetEqualFunctionByType = GetEqualFunction,
                        GetCompareFunction = GetCompareFunction,
                        GetModFunction = GetModFunction,
                        GetSmallestSignedType = GetSmallestSignedType,
                        GetSmallestUnsignedType = GetSmallestUnsignedType,
                        ToValueLabel = ToValueLabel,
                        ValueObjectDeclarations = GetObjectDeclarations<TValue>(),
                        GetKeyTypeName = () => typeof(TKey) == typeof(string) ? $"&'static {KeyTypeName}" : KeyTypeName,
                        GetValueTypeName = () => typeof(TValue) == typeof(string) || !typeof(TValue).IsPrimitive ? $"&'static {ValueTypeName}" : ValueTypeName
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
                        Keys = arrayCtx.Keys.ToObjects(),
                        KeyCount = arrayCtx.Keys.Length,
                        Values = arrayCtx.Values.ToObjects(),
                        ValueCount = arrayCtx.Values.Length
                    };

                case BinarySearchContext<TKey, TValue> bsCtx:
                    return new BinarySearchTemplateData
                    {
                        Keys = bsCtx.Keys.ToObjects(),
                        KeyCount = bsCtx.Keys.Length,
                        Values = bsCtx.Values.ToObjects(),
                        ValueCount = bsCtx.Values.Length
                    };

                case InterpolatedBinarySearchContext<TKey, TValue> ibsCtx:
                    return new BinarySearchTemplateData
                    {
                        Keys = ibsCtx.Keys.ToObjects(),
                        KeyCount = ibsCtx.Keys.Length,
                        Values = ibsCtx.Values.ToObjects(),
                        ValueCount = ibsCtx.Values.Length
                    };

                case ConditionalContext<TKey, TValue> conCtx:
                    return new ArrayTemplateData
                    {
                        Keys = conCtx.Keys.ToObjects(),
                        KeyCount = conCtx.Keys.Length,
                        Values = conCtx.Values.ToObjects(),
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
                        Values = klCtx.Values.ToObjects(),
                        ValueCount = klCtx.Values.Length
                    };

                case BloomFilterContext:
                    return null;

                case BitSetContext<TValue> bsCtx:
                    return new BitSetTemplateData
                    {
                        Values = bsCtx.Values.ToObjects(),
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
                        Values = hashCtx.Values.ToObjects(),
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
                        Values = compactCtx.Values.ToObjects(),
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
                        Values = perfectCtx.Values.ToObjects(),
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
            if (typeof(TKey) == typeof(string) && IgnoreCase)
                return $"case_insensitive_compare({var1}, {var2})";

            return $"if {var1} < {var2} {{ -1 }} else if {var1} > {var2} {{ 1 }} else {{ 0 }}";
        }

        protected override string GetMethodHeader(MethodType methodType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.GetMethodHeader(methodType));

            if (TotalTrimLength != 0)
                sb.Append($"    let {TrimmedKeyName} = &{InputKeyName}[{TrimPrefix.Length.ToStringInvariant()}..{InputKeyName}.len() - {TrimSuffix.Length.ToStringInvariant()}];");

            return sb.ToString();
        }

        protected override string GetEqualFunctionInternal(string value1, string value2, TypeCode keyType)
        {
            if (keyType == TypeCode.String && IgnoreCase)
                return $"case_insensitive_equals({value1}, {value2})";

            return $"{value1} == {value2}";
        }

        protected override void RegisterSharedCode()
        {
            if (typeof(TKey) != typeof(string) || !IgnoreCase)
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
}