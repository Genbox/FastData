using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class HashTablePerfect<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, HashTablePerfectContext<TKey, TValue> context)
    {
        Func<IrExpression, IrExpression, IrExpression> match = lowering.SparseHashEntries(context.Data, context.StoreHashCode, context.Values);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);

        return
        [
            Assign(hash, lowering.Expressions.Hash()),
            Assign(index, Int(Mod(Read(hash), U64((ulong)context.Data.Length)))),
            If(match(Read(index), Read(hash)), Return(Read(index))),
            Return(I32(-1))
        ];
    }
}