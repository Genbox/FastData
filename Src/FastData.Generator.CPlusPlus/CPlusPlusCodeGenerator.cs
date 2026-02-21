using System.Collections;
using System.Linq;
using Genbox.FastData.Generator.CPlusPlus.Internal;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.Internal.Generators;
using Genbox.FastData.Generator.CPlusPlus.Internal.TemplateData;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;

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

    protected override OutputWriter<TKey>? GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context) => context switch
    {
        SingleValueContext<TKey, TValue> x => new SingleValueCode<TKey, TValue>(x, Shared),
        RangeContext<TKey> x => new RangeCode<TKey, TValue>(x),
        BitSetContext<TValue> x => new BitSetCode<TKey, TValue>(x, Shared),
        BloomFilterContext x => new BloomFilterCode<TKey>(x),
        ArrayContext<TKey, TValue> x => new ArrayCode<TKey, TValue>(x, Shared),
        BinarySearchContext<TKey, TValue> x => new BinarySearchCode<TKey, TValue>(x, Shared),
        ConditionalContext<TKey, TValue> x => new ConditionalCode<TKey, TValue>(x, Shared),
        HashTableContext<TKey, TValue> x => new HashTableCode<TKey, TValue>(x, Shared),
        HashTableCompactContext<TKey, TValue> x => new HashTableCompactCode<TKey, TValue>(x, Shared),
        HashTablePerfectContext<TKey, TValue> x => new HashTablePerfectCode<TKey, TValue>(x, Shared),
        KeyLengthContext<TValue> x => new KeyLengthCode<TKey, TValue>(x, Shared),
        EliasFanoContext<TKey> x => new EliasFanoCode<TKey>(x),
        RrrBitVectorContext x => new RrrBitVectorCode<TKey>(x),
        _ => null
    };

    private sealed class TemplateBasedOutputWriter<TKey, TValue>(IContext context) : CPlusPlusOutputWriter<TKey>
    {
        public override string Generate()
        {
            ITemplateData? dataModel = CreateContextModel();

            CommonDataModel common = new CommonDataModel
            {
                MethodAttribute = MethodAttribute,
                PostMethodModifier = PostMethodModifier,
                KeyTypeName = KeyTypeName,
                ValueTypeName = ValueTypeName,
                InputKeyName = InputKeyName,
                LookupKeyName = LookupKeyName,
                ArraySizeType = ArraySizeType,
                HashSizeType = HashSizeType,
                IsPrimitive = typeof(TValue).IsPrimitive
            };

            TemplateModel model = new TemplateModel
            {
                KeyType = KeyType,
                HashSource = HashSource,
                GetMethodHeader = GetMethodHeader,
                GetEqualFunction = (a, b) => GetEqualFunction(a, b),
                GetEqualFunctionByType = GetEqualFunction,
                GetCompareFunction = GetCompareFunction,
                GetModFunction = GetModFunction,
                GetSmallestSignedType = GetSmallestSignedType,
                GetSmallestUnsignedType = GetSmallestUnsignedType,
                ToValueLabel = ToValueLabel,
                ValueObjectDeclarations = GetObjectDeclarations<TValue>(),
                GetMethodModifier = GetMethodModifier,
                GetFieldModifier = GetFieldModifier,
                GetValueTypeName = GetValueTypeName
            };

            string raw = context.GetType().Name;
            int idx = raw.IndexOf("Context", StringComparison.Ordinal);
            string name = raw.Substring(0, idx) + "Code.t4";
            string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates", name));

            TemplateGenerator generator = new TemplateGenerator();
            AddTemplateReference(generator, typeof(TemplateModel));
            AddTemplateReference(generator, typeof(CommonDataModel));
            AddTemplateReference(generator, typeof(SharedCode));
            AddTemplateReference(generator, typeof(MethodType));
            AddTemplateReference(generator, typeof(KeyType));
            AddTemplateReference(generator, typeof(FormatHelper));
            AddTemplateReference(generator, typeof(CPlusPlusCodeGeneratorConfig));

            ITextTemplatingSession session = generator.GetOrCreateSession();
            session["Model"] = model;
            session["Context"] = context;
            session["Common"] = common;
            session["Shared"] = Shared;
            if (dataModel != null)
                session["Data"] = dataModel;

            ParsedTemplate parsed = generator.ParseTemplate(name, text);
            TemplateSettings settings = TemplatingEngine.GetSettings(generator, parsed);
            ValueTuple<string, string> result = generator.ProcessTemplateAsync(parsed, name, text, name, settings).GetAwaiter().GetResult();

            if (generator.Errors.HasErrors)
            {
                string errors = string.Join("\n", generator.Errors.Cast<object>().Select(x => x.ToString()));
                throw new InvalidOperationException("Failed to process template '" + name + "':\n" + errors);
            }

            return result.Item2;
        }

        private ITemplateData? CreateContextModel()
        {
            switch (context)
            {
                case ArrayContext<TKey, TValue> arrayCtx:
                    return new ArrayTemplateData
                    {
                        Keys = ToObjects(arrayCtx.Keys),
                        KeyCount = arrayCtx.Keys.Length,
                        Values = ToObjects(arrayCtx.Values),
                        ValueCount = arrayCtx.Values.Length
                    };

                case BinarySearchContext<TKey, TValue> bsCtx:
                    return new BinarySearchTemplateData
                    {
                        Keys = ToObjects(bsCtx.Keys),
                        KeyCount = bsCtx.Keys.Length,
                        Values = ToObjects(bsCtx.Values),
                        ValueCount = bsCtx.Values.Length
                    };

                case ConditionalContext<TKey, TValue> conCtx:
                    return new ArrayTemplateData
                    {
                        Keys = ToObjects(conCtx.Keys),
                        KeyCount = conCtx.Keys.Length,
                        Values = ToObjects(conCtx.Values),
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
                        Values = ToObjects(klCtx.Values),
                        ValueCount = klCtx.Values.Length
                    };

                case BloomFilterContext:
                    return null;

                case BitSetContext<TValue> bsCtx:
                    return new BitSetTemplateData
                    {
                        Values = ToObjects(bsCtx.Values),
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
                        Values = ToObjects(hashCtx.Values),
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
                        Values = ToObjects(compactCtx.Values),
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
                        Values = ToObjects(perfectCtx.Values),
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

        private static IEnumerable<object> ToObjects(ReadOnlyMemory<TKey> keys) => new MemoryObjectEnumerable<TKey>(keys);

        private static IEnumerable<object> ToObjects(ReadOnlyMemory<TValue> values) => new MemoryObjectEnumerable<TValue>(values);

        private sealed class MemoryObjectEnumerable<T>(ReadOnlyMemory<T> memory) : IEnumerable<object>
        {
            public IEnumerator<object> GetEnumerator() => new Enumerator(memory);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator(ReadOnlyMemory<T> memory) : IEnumerator<object>
            {
                private int _index = -1;

                public object Current => memory.Span[_index];

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    return _index < memory.Length;
                }

                public void Dispose() {}

                public void Reset() => throw new NotSupportedException("not supported");
            }
        }

        private static void AddTemplateReference(TemplateGenerator generator, Type type)
        {
            string location = type.Assembly.Location;

            if (!string.IsNullOrEmpty(location) && !generator.Refs.Exists(x => string.Equals(x, location, StringComparison.OrdinalIgnoreCase)))
                generator.Refs.Add(location);
        }
    }
}