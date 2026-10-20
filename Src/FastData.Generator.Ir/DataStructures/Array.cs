using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class Array<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, ReadOnlyMemory<TKey> keyMemory, ReadOnlyMemory<TValue> values)
    {
        TKey[] keys = GetArray(keyMemory);
        IrDataId data = lowering.ArrayLookup(keys, values);

        IrVariableId index = lowering.Builder.FindLocal("i", IrType.I32);

        return
        [
            Assign(index, I32(0)),
            While(Less(Read(index), I32(keys.Length)),
                If(lowering.Builder.Keys.Equal(Index(Read(data), Read(index)), lowering.Key), Return(Read(index))),
                Increment(index)),
            Return(I32(-1))
        ];
    }
}