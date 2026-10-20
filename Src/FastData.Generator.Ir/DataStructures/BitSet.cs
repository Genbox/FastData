using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class BitSet<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, BitSetContext<TValue> context)
    {
        NumericGeneratorConfig numeric = (NumericGeneratorConfig)lowering.Config;
        TKey min = (TKey)numeric.Constants.MinValue;
        ulong span = unchecked(ToUnsigned((TKey)numeric.Constants.MaxValue) - ToUnsigned(min));
        IrDataId? bits = context.HasOccupancy ? lowering.Builder.Program.AddData("bitset", IrClrConverter.FromValue(context.BitSet)) : null;
        lowering.AddValues(context.Values, !context.HasOccupancy);

        IrExpression BitIsSet(IrExpression bit) => NotEqual(Binary(BinaryOperation.And, Index(Read(bits.Value),
                    Convert(IrType.I32, Binary(BinaryOperation.ShiftRight, bit, I32(6)))),
                Binary(BinaryOperation.ShiftLeft, U64(1), Convert(IrType.I32, Binary(BinaryOperation.And, bit, U64(63))))),
            U64(0));

        // Positions are key offsets from the minimum key.
        IrFunctionId next = lowering.Enumeration.AddNext((long)span, bits == null ? null : p => BitIsSet(Convert(IrType.U64, p)));
        IrLiteral minimum = IrClrConverter.FromValue(min);
        lowering.Enumeration.AddEnumeration(next, p => Convert(lowering.KeyType,
                Add(Convert(IrType.U64, minimum), Convert(IrType.U64, p))),
            lowering.Builder.Values == null ? null : p => lowering.ValueAt(Convert(IrType.I32, p)));

        IrVariableId offset = lowering.Builder.FindLocal("offset", IrType.U64);
        List<IrStatement> statements =
        [
            Assign(offset, Subtract(Convert(IrType.U64, lowering.Key), Convert(IrType.U64, minimum))),
            If(Greater(Read(offset), U64(span)), Return(I32(-1)))
        ];
        if (bits != null)
            statements.Add(If(Not(BitIsSet(Read(offset))), Return(I32(-1))));
        statements.Add(Return(Convert(IrType.I32, Read(offset))));
        return statements;
    }
}