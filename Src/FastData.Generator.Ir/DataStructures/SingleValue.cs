using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class SingleValue<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, SingleValueContext<TKey, TValue> context)
    {
        IrLiteral key = IrClrConverter.FromValue(context.Key);
        lowering.AddValues(context.Values);
        lowering.Enumeration.AddCursor(1, _ => key, lowering.Builder.Values == null ? null : lowering.ValueAt);

        return
        [
            If(lowering.Builder.Keys.Equal(lowering.Key, key), Return(I32(0))),
            Return(I32(-1))
        ];
    }
}