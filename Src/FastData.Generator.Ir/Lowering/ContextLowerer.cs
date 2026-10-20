using Genbox.FastData.Generator.Ir.DataStructures;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

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
            SingleValueContext<TKey, TValue> single => SingleValue<TKey, TValue>.Lower(lowering, single),
            ArrayContext<TKey, TValue> array => Array<TKey, TValue>.Lower(lowering, array.Keys, array.Values),
            BinarySearchContext<TKey, TValue> search => BinarySearch<TKey, TValue>.Lower(lowering, search.Keys, search.Values),
            ConditionalContext<TKey, TValue> conditional => Conditional<TKey, TValue>.Lower(lowering, conditional.Keys, conditional.Values),
            HashTableContext<TKey, TValue> table => HashTable<TKey, TValue>.Lower(lowering, table),
            HashTableCompactContext<TKey, TValue> compact => HashTableCompact<TKey, TValue>.Lower(lowering, compact),
            HashTablePerfectContext<TKey, TValue> perfect => HashTablePerfect<TKey, TValue>.Lower(lowering, perfect),
            BinarySearchInterpolationContext<TKey, TValue> interpolation => BinarySearchInterpolation<TKey, TValue>.Lower(lowering, interpolation),
            BloomFilterContext bloom => BloomFilter<TKey, TValue>.Lower(lowering, bloom),
            HybleContext<TKey, TValue> hyble => Hyble<TKey, TValue>.Lower(lowering, hyble),
            ConstMapContext<TKey, TValue> map => ConstMap<TKey, TValue>.Lower(lowering, map),
            EliasFanoContext elias => EliasFano<TKey, TValue>.Lower(lowering, elias),
            RrrBitVectorContext rrr => RrrBitVector<TKey, TValue>.Lower(lowering, rrr),
            PgmContext<TKey, TValue> pgm => Pgm<TKey, TValue>.Lower(lowering, pgm),
            RangeContext<TKey> range => Range<TKey, TValue>.Lower(lowering, GetArray(range.Ranges)),
            BitSetContext<TValue> bitSet => BitSet<TKey, TValue>.Lower(lowering, bitSet),
            KeyLengthContext<TValue> keyLength => KeyLength<TKey, TValue>.Lower(lowering, keyLength),
            _ => throw new NotSupportedException($"IR frontend lowering does not support the {config.StructureType} structure yet.")
        });

        lowering.ExportConstants();
        return new LoweredLookup(lowering.Builder.Build(new IrBlock(body)), lowering.KeyStorage, lowering.ValueStorage);
    }
}