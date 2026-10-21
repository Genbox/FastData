using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.CSharp.Internal;

internal static class CSharpRuntime
{
    public static string Call(IrBuiltinOperation operation, string[] arguments) => operation switch
    {
        IrBuiltinOperation.MultiplyHigh => $"Math.BigMul({arguments[0]}, {arguments[1]}, out _)",
        IrBuiltinOperation.PopCount => $"BitOperations.PopCount({arguments[0]})",
        IrBuiltinOperation.TrailingZeroCount => $"BitOperations.TrailingZeroCount({arguments[0]})",
        _ => CSharpText.Call(operation, arguments)
    };

    public static string BitCast(IrType sourceType, IrType targetType, string operand)
    {
        string method = (sourceType.Kind, targetType.Kind) switch
        {
            (IrTypeKind.F32, IrTypeKind.U32) => "SingleToUInt32Bits",
            (IrTypeKind.F32, IrTypeKind.I32) => "SingleToInt32Bits",
            (IrTypeKind.U32, IrTypeKind.F32) => "UInt32BitsToSingle",
            (IrTypeKind.I32, IrTypeKind.F32) => "Int32BitsToSingle",
            (IrTypeKind.F64, IrTypeKind.U64) => "DoubleToUInt64Bits",
            (IrTypeKind.F64, IrTypeKind.I64) => "DoubleToInt64Bits",
            (IrTypeKind.U64, IrTypeKind.F64) => "UInt64BitsToDouble",
            (IrTypeKind.I64, IrTypeKind.F64) => "Int64BitsToDouble",
            _ => throw new NotSupportedException($"Bit cast from {sourceType} to {targetType} is not supported.")
        };

        return $"BitConverter.{method}({operand})";
    }
}