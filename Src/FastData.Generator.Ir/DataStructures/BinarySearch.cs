using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class BinarySearch<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, ReadOnlyMemory<TKey> keyMemory, ReadOnlyMemory<TValue> valueMemory)
    {
        TKey[] keys = GetArray(keyMemory);

        if (lowering.Builder.Keys.FoldFunctionId != null && lowering.Config is StringGeneratorConfig { Encoding: not GeneratorEncoding.Utf8Bytes })
        {
            // Only Utf8Bytes frontends sort with the lowercase ASCII fold. The others use OrdinalIgnoreCase, which orders uppercase ASCII
            // and places punctuation differently, so the keys are re-sorted to match the IR fold.
            string[] folded = keys.Select(key => ((string)(object)key!).ToLowerInvariant()).ToArray();
            int[] order = Enumerable.Range(0, keys.Length).ToArray();
            Array.Sort(order, (a, b) => string.CompareOrdinal(folded[a], folded[b]));
            keys = order.Select(index => keys[index]).ToArray();

            if (!valueMemory.IsEmpty)
            {
                TValue[] values = GetArray(valueMemory);
                valueMemory = order.Select(index => values[index]).ToArray();
            }
        }

        IrDataId data = lowering.ArrayLookup(keys, valueMemory);

        // Only a folded comparison needs the comparison helper; plain keys order with the core operators.

        IrExpression KeyAt(IrExpression index) => Index(Read(data), index);

        IrExpression Before(IrExpression index) => lowering.Builder.Keys.FoldFunctionId != null
            ? Less(lowering.Builder.Keys.Compare(KeyAt(index), lowering.Key), I32(0))
            : Less(KeyAt(index), lowering.Key);

        IrVariableId low = lowering.Builder.FindLocal("low", IrType.I32);
        List<IrStatement> statements = LowerBound(lowering.Builder.FindLocal, low, keys.Length, Before);
        statements.Add(If(AndAlso(Less(Read(low), I32(keys.Length)), lowering.Builder.Keys.Equal(KeyAt(Read(low)), lowering.Key)), Return(Read(low))));
        statements.Add(Return(I32(-1)));
        return statements;
    }
}