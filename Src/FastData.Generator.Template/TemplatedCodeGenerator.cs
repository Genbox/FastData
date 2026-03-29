using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Template.Abstracts;
using Genbox.FastData.Generator.Template.Extensions;
using Genbox.FastData.Generator.Template.Helpers;
using Genbox.FastData.Generator.Template.TemplateData;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Template;

public abstract class TemplatedCodeGenerator : ICodeGenerator
{
    private readonly string _languageName;
    private readonly TemplateManager _manager;
    private readonly TypeMap _map;

    protected TemplatedCodeGenerator(ILanguageDef languageDef, GeneratorEncoding encoding)
    {
        Encoding = encoding;

        _map = new TypeMap(languageDef.TypeDefinitions, Encoding);

        string typeName = GetType().Name;
        _languageName = typeName.Substring(0, typeName.Length - 13);

#if RELEASE
        const bool release = true;
#else
        const bool release = false;
#endif

        _manager = new TemplateManager(_languageName, Path.Combine(Path.GetTempPath(), "FastData"), release);
    }

    protected string TemplateDir => Path.Combine(AppContext.BaseDirectory, "Templates", _languageName);

    public GeneratorEncoding Encoding { get; }

    public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
    {
        Dictionary<string, object?> variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Model", new TemplateModel
                {
                    KeyType = typeof(TKey),
                    ValueType = typeof(TValue)
                }
            },
            { "Context", context },
            { "TypeMap", _map },
            { "GeneratorConfig", genCfg },
            { "Data", CreateContextModel<TKey, TValue>(context) }
        };

        return GenerateTemplated<TKey, TValue>(genCfg, _manager, variables);
    }

    protected abstract string GenerateTemplated<TKey, TValue>(GeneratorConfigBase genCfg, TemplateManager manager, Dictionary<string, object?> variables);

    protected static ITemplateData? CreateContextModel<TKey, TValue>(IContext context)
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

            case BinarySearchInterpolationContext<TKey, TValue> ibsCtx:
                return new BinarySearchTemplateData
                {
                    Keys = ibsCtx.Keys.ToObjects(),
                    KeyCount = ibsCtx.Keys.Length,
                    Values = ibsCtx.Values.ToObjects(),
                    ValueCount = ibsCtx.Values.Length
                };

            case ConditionalContext<TKey, TValue> conCtx:
                return new ConditionalTemplateData
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
                //TODO
                return null;

            default:
                throw new InvalidOperationException("No template mapping found for context type: " + context.GetType().FullName);
        }
    }
}