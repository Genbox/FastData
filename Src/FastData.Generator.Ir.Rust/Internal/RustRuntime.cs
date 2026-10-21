using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Rust.Internal;

internal static class RustRuntime
{
    public static string Call(IrBuiltinOperation operation, string[] arguments, RustText text) => operation switch
    {
        IrBuiltinOperation.MultiplyHigh => $"((({arguments[0]} as u128) * ({arguments[1]} as u128) >> 64) as u64)",
        IrBuiltinOperation.PopCount => $"(({arguments[0]}).count_ones() as i32)",
        IrBuiltinOperation.TrailingZeroCount => $"(({arguments[0]}).trailing_zeros() as i32)",
        _ => text.Call(operation, arguments)
    };

    public static string BitCast(IrType sourceType, IrType targetType, string operand)
    {
        string target = RustTypes.ScalarName(targetType.Kind);
        return sourceType.IsFloatingPoint
            ? $"(({operand}).to_bits() as {target})"
            : $"{target}::from_bits({operand} as u{targetType.BitWidth})";
    }
}