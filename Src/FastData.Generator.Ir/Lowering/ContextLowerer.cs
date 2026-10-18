using Genbox.FastData.Generator.Ir.DataStructures;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.Lowering;

/// <summary>Lowers frontend structure contexts into portable IR lookup programs.</summary>
internal static class ContextLowerer
{
    public static LoweredLookup Lower<TKey, TValue>(GeneratorConfigBase config, IContext context, IrBackendCapabilities capabilities)
    {
        LoweringContext<TKey, TValue> lowering = new LoweringContext<TKey, TValue>(config, capabilities);
        List<IrStatement> body = lowering.Expressions.EarlyExits();
        body.AddRange(context switch
        {
            SingleValueContext<TKey, TValue> single => new SingleValue<TKey, TValue>(lowering).Lower(single),
            ArrayContext<TKey, TValue> array => new Array<TKey, TValue>(lowering).Lower(array.Keys, array.Values),
            BinarySearchContext<TKey, TValue> search => new BinarySearch<TKey, TValue>(lowering).Lower(search.Keys, search.Values),
            ConditionalContext<TKey, TValue> conditional => new Conditional<TKey, TValue>(lowering).Lower(conditional.Keys, conditional.Values),
            HashTableContext<TKey, TValue> table => new HashTable<TKey, TValue>(lowering).Lower(table),
            HashTableCompactContext<TKey, TValue> compact => new HashTableCompact<TKey, TValue>(lowering).Lower(compact),
            HashTablePerfectContext<TKey, TValue> perfect => new HashTablePerfect<TKey, TValue>(lowering).Lower(perfect),
            BinarySearchInterpolationContext<TKey, TValue> interpolation => new BinarySearchInterpolation<TKey, TValue>(lowering).Lower(interpolation),
            BloomFilterContext bloom => new BloomFilter<TKey, TValue>(lowering).Lower(bloom),
            HybleContext<TKey, TValue> hyble => new Hyble<TKey, TValue>(lowering).Lower(hyble),
            ConstMapContext<TKey, TValue> map => new ConstMap<TKey, TValue>(lowering).Lower(map),
            EliasFanoContext elias => new EliasFano<TKey, TValue>(lowering).Lower(elias),
            RrrBitVectorContext rrr => new RrrBitVector<TKey, TValue>(lowering).Lower(rrr),
            PgmContext<TKey, TValue> pgm => new Pgm<TKey, TValue>(lowering).Lower(pgm),
            RangeContext<TKey> range => new Range<TKey, TValue>(lowering).Lower(GetArray(range.Ranges)),
            BitSetContext<TValue> bitSet => new BitSet<TKey, TValue>(lowering).Lower(bitSet),
            KeyLengthContext<TValue> keyLength => new KeyLength<TKey, TValue>(lowering).Lower(keyLength),
            _ => throw new NotSupportedException($"IR frontend lowering does not support the {config.StructureType} structure yet.")
        });

        lowering.ExportConstants();
        return new LoweredLookup(lowering.Builder.Build(new IrBlock(body)), lowering.KeyStorage, lowering.ValueStorage);
    }
}