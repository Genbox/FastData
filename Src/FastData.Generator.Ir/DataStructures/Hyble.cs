using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class Hyble<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, HybleContext<TKey, TValue> context)
    {
        Func<IrExpression, IrExpression, IrExpression> match = lowering.SparseHashEntries(context.Data, context.StoreHashCode, context.Values);
        IrDataId displacements = lowering.Storage.Unsigned("displacements", context.Displacements);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);
        IrExpression bucket = Int(And(Read(hash), U64(context.BucketMask)));

        return
        [
            Assign(hash, Multiply(lowering.Expressions.Hash(), U64(context.Seed))),
            Assign(index,
                Int(Add(lowering.MultiplyHigh(Read(hash), U64(context.ApproxRange)), Unsigned(Index(Read(displacements), bucket))))),
            If(match(Read(index), Read(hash)), Return(Read(index))),
            Return(I32(-1))
        ];
    }
}