using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class HashTablePerfect<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(HashTablePerfectContext<TKey, TValue> context)
    {
        Func<IrExpression, IrExpression, IrExpression> match = lowering.HashEntries(context.Data.Select(static x => x.Key).ToArray(),
            context.StoreHashCode ? context.Data.Select(static x => x.Value).ToArray() : null,
            context.Values,
            true);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);

        return
        [
            Assign(hash, lowering.Expressions.Hash()),
            Assign(index, Int(Mod(Read(hash), U64((ulong)context.Data.Length)), IrType.U64)),
            If(match(Read(index), Read(hash)), Return(Read(index))),
            Return(I32(-1))
        ];
    }
}