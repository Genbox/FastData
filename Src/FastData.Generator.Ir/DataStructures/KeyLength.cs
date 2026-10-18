using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class KeyLength<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(KeyLengthContext<TValue> context)
    {
        string?[] lengths = context.Lengths;
        IrDataId keys = lowering.Builder.Program.AddData("keys",
            IrClrConverter.FromValue(lengths, IrType.ArrayOf(IrType.String), lowering.Builder.Program.Text.Encoding));
        ReadOnlyMemory<TValue> values = context.Values;
        IrDataId? offsets = null;

        if (values.Length > 0)
        {
            offsets = lowering.Builder.Program.AddData("offsets", IrClrConverter.FromValue(context.ValueOffsets));
            lowering.AddValues(values);
        }

        IrFunctionId next = lowering.Enumeration.AddNext(lengths.Length - 1,
            p => NotEqual(Index(Read(keys), Convert(IrType.I32, p, IrType.I64)), new IrNullLiteral(IrType.String)));
        lowering.Enumeration.AddEnumeration(next, p => Index(Read(keys), Convert(IrType.I32, p, IrType.I64)),
            offsets == null ? null : p => Index(Read(lowering.Builder.Values!.DataId), Index(Read(offsets.Value), Convert(IrType.I32, p, IrType.I64))));

        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);
        IrExpression stored = Index(Read(keys), Read(index));

        return
        [
            Assign(index, Subtract(lowering.Builder.Text.Length(lowering.Key), I32(context.MinLength))),
            If(OrElse(Less(Read(index), I32(0)), GreaterOrEqual(Read(index), I32(lengths.Length))), Return(I32(-1))),
            If(Equal(stored, new IrNullLiteral(IrType.String)), Return(I32(-1))),
            If(lowering.Builder.Keys.Equal(stored, lowering.Key), Return(offsets == null ? Read(index) : Index(Read(offsets.Value), Read(index)))),
            Return(I32(-1))
        ];
    }
}