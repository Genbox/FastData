using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class Hyble<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(HybleContext<TKey, TValue> context)
    {
        Func<IrExpression, IrExpression, IrExpression> match = lowering.HashEntries(context.Data.Select(static x => x.Key).ToArray(),
            context.StoreHashCode ? context.Data.Select(static x => x.Value).ToArray() : null,
            context.Values,
            true);
        (IrDataId displacements, IrType type) = lowering.Storage.Unsigned("displacements", context.Displacements, IrType.U16);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);
        IrExpression bucket = Int(And(Read(hash), U64(context.BucketMask)), IrType.U64);

        return
        [
            Assign(hash, Multiply(lowering.Expressions.Hash(), U64(context.Seed))),
            Assign(index,
                Int(Add(lowering.MultiplyHigh(Read(hash), U64(context.ApproxRange)), Unsigned(Index(Read(displacements), bucket), type)), IrType.U64)),
            If(match(Read(index), Read(hash)), Return(Read(index))),
            Return(I32(-1))
        ];
    }
}