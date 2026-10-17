using System.Numerics;
using Genbox.FastData.Ir.Extensions;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Execution;

/// <summary>Evaluates scalar binary operations and conversions on IR literals.</summary>
/// <remarks>
/// Implements the IR's numeric, comparison, and conversion rules for operands with validated types.
/// Integral results use the declared bit width and signedness. String comparisons use the program's text encoding,
/// and floating-point comparisons follow IEEE 754 equality and ordering rules.
/// </remarks>
internal static class IrOperations
{
    /// <summary>Evaluates an arithmetic, bitwise, shift, or comparison operation on two literals.</summary>
    /// <param name="operation">The binary operation to evaluate.</param>
    /// <param name="left">The left operand, whose type determines the integral result type.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="encoding">The program's text encoding, used for string comparisons.</param>
    /// <returns>A Boolean literal for comparisons, or an integral literal with the left operand's type.</returns>
    /// <remarks>
    /// Unsigned arithmetic wraps at the operand width. Signed addition, subtraction, and multiplication require
    /// a representable result. Integral division truncates toward zero, and remainder follows the dividend's sign.
    /// Shifts operate on unsigned values with counts in [0, W), where W is the operand width.
    /// </remarks>
    /// <exception cref="DivideByZeroException">An integral divisor is zero.</exception>
    /// <exception cref="IrExecutionException">
    /// Signed addition, subtraction, or multiplication overflows; a shift count is outside [0, W);
    /// or an ordered string comparison has a null operand.
    /// </exception>
    public static IrLiteral Binary(BinaryOperation operation, IrLiteral left, IrLiteral right, IrTextEncoding encoding)
    {
        if (operation.IsComparison())
            return Compare(operation, left, right, encoding);

        // Compute with arbitrary precision, then apply the IR type's overflow or wrapping rules.
        BigInteger x = IntegerValue(left);
        BigInteger y = IntegerValue(right);
        bool signed = left.Type.IsSignedIntegral;

        if (operation is BinaryOperation.ShiftLeft or BinaryOperation.ShiftRight && (y < 0 || y >= left.Type.BitWidth))
            throw new IrExecutionException($"Shift count {y} is outside [0, {left.Type.BitWidth}).");

        BigInteger result = operation switch
        {
            BinaryOperation.Add => x + y,
            BinaryOperation.Subtract => x - y,
            BinaryOperation.Multiply => x * y,
            BinaryOperation.Divide => x / y,
            BinaryOperation.Modulo => x % y,
            BinaryOperation.And => x & y,
            BinaryOperation.Or => x | y,
            BinaryOperation.ExclusiveOr => x ^ y,
            BinaryOperation.ShiftLeft => x << (int)y,
            BinaryOperation.ShiftRight => x >> (int)y,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        if (signed && operation is BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply)
        {
            BigInteger limit = BigInteger.One << (left.Type.BitWidth - 1);
            if (result < -limit || result >= limit)
                throw new IrExecutionException($"Signed {operation} overflows {left.Type}.");
        }

        return Integer(left.Type, result);
    }

    /// <summary>Converts a literal to a target type using the IR's conversion rules.</summary>
    /// <param name="conversion">The numeric conversion or bit reinterpretation policy.</param>
    /// <param name="target">The destination type.</param>
    /// <param name="source">The literal to convert.</param>
    /// <returns>A literal of the target type.</returns>
    /// <remarks>
    /// Integral conversions retain the low W bits at target width W, with signed sources sign-extended when widening.
    /// Character conversions preserve the character's unit value. Floating-point bit casts preserve the complete bit pattern.
    /// </remarks>
    /// <exception cref="IrExecutionException">The source unit value is outside the target character kind.</exception>
    public static IrLiteral Convert(ConversionKind conversion, IrType target, IrLiteral source)
    {
        if (source is IrCharLiteral character)
            return Integer(target, character.Value);

        if (target.CharacterKind is {} characterKind)
        {
            // Unit types are unsigned and at most 16 bits wide, so the value always fits a uint.
            uint unit = (uint)IntegerValue(source);
            if (!IrCharLiteral.IsValid(characterKind, unit))
                throw new IrExecutionException($"Unit value {unit} is not a {characterKind} character.");
            return new IrCharLiteral(characterKind, unit);
        }

        if (conversion == ConversionKind.BitCast)
            return BitCast(source, target);

        BigInteger value = IntegerValue(source);

        // Integral conversions retain the low bits at the target width.
        return Integer(target, value);
    }

    /// <summary>Evaluates equality or ordering for scalar and null literals.</summary>
    /// <remarks>
    /// Strings compare in encoded code-unit order, and characters compare by unit value.
    /// Floating-point signed zeros compare equal; a NaN operand makes only NotEqual true.
    /// Null equality is true when both operands are null, and ordered string comparisons require non-null operands.
    /// </remarks>
    private static IrBoolLiteral Compare(BinaryOperation operation, IrLiteral left, IrLiteral right, IrTextEncoding encoding)
    {
        if (left is IrNullLiteral || right is IrNullLiteral)
        {
            if (operation is not (BinaryOperation.Equal or BinaryOperation.NotEqual))
                throw new IrExecutionException("Ordered string comparisons require non-null operands.");

            bool equal = left is IrNullLiteral && right is IrNullLiteral;
            return new IrBoolLiteral(operation == BinaryOperation.Equal ? equal : !equal);
        }

        int order;

        if (left is IrStringLiteral text)
            order = CompareStrings(text.Value, ((IrStringLiteral)right).Value, encoding);
        else if (left.Type.IsFloatingPoint)
        {
            double a = left is IrF32Literal fa ? fa.Value : ((IrF64Literal)left).Value;
            double b = right is IrF32Literal fb ? fb.Value : ((IrF64Literal)right).Value;

            // Handle NaN before CompareTo to preserve IEEE comparison semantics.
            if (double.IsNaN(a) || double.IsNaN(b))
                return new IrBoolLiteral(operation == BinaryOperation.NotEqual);

            order = a.CompareTo(b);
        }
        else if (left.Type.IsIntegral)
            order = IntegerValue(left).CompareTo(IntegerValue(right));
        else if (left is IrCharLiteral character)
            order = character.Value.CompareTo(((IrCharLiteral)right).Value);
        else if (left is IrBoolLiteral boolean)
            order = boolean.Value.CompareTo(((IrBoolLiteral)right).Value);
        else
            throw new InvalidOperationException("Comparison requires scalar or null operands.");

        return new IrBoolLiteral(operation switch
        {
            BinaryOperation.Equal => order == 0,
            BinaryOperation.NotEqual => order != 0,
            BinaryOperation.LessThan => order < 0,
            BinaryOperation.LessThanOrEqual => order <= 0,
            BinaryOperation.GreaterThan => order > 0,
            BinaryOperation.GreaterThanOrEqual => order >= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        });
    }

    /// <summary>Orders non-null strings by the program's encoded code units.</summary>
    /// <remarks>UTF-8 operands are well-formed UTF-16 strings and compare in Unicode scalar-value order.</remarks>
    private static int CompareStrings(string left, string right, IrTextEncoding encoding)
    {
        if (encoding == IrTextEncoding.Utf16)
            return string.CompareOrdinal(left, right);

        if (ReferenceEquals(left, right))
            return 0;

        int length = Math.Min(left.Length, right.Length);

        for (int i = 0; i < length; i++)
        {
            int a = left[i];
            int b = right[i];
            if (a == b)
                continue;

            // Move surrogate units above BMP units to preserve Unicode scalar-value order.
            // Two differing surrogate units retain their relative order.
            if (a >= 0xD800 && a <= 0xDFFF)
                a += 0x10000;

            if (b >= 0xD800 && b <= 0xDFFF)
                b += 0x10000;

            return a.CompareTo(b);
        }

        return left.Length.CompareTo(right.Length);
    }

    /// <summary>Extracts the mathematical value of an integral literal, preserving its signedness.</summary>
    /// <param name="value">An integral literal of any supported width.</param>
    /// <returns>The literal's value as an arbitrary-precision integer.</returns>
    /// <exception cref="NotSupportedException">The literal has a non-integral type.</exception>
    private static BigInteger IntegerValue(IrLiteral value) => value switch
    {
        IrI8Literal x => x.Value,
        IrU8Literal x => x.Value,
        IrI16Literal x => x.Value,
        IrU16Literal x => x.Value,
        IrI32Literal x => x.Value,
        IrU32Literal x => x.Value,
        IrI64Literal x => x.Value,
        IrU64Literal x => x.Value,
        _ => throw new NotSupportedException("Expected an integral literal.")
    };

    /// <summary>Creates an integral literal from the low bits of a value at the requested type's width.</summary>
    /// <remarks>For signed types, the retained bits are interpreted as a two's-complement value.</remarks>
    private static IrLiteral Integer(IrType type, BigInteger value) => type.Kind switch
    {
        IrTypeKind.I8 => new IrI8Literal(sbyte.CreateTruncating(value)),
        IrTypeKind.U8 => new IrU8Literal(byte.CreateTruncating(value)),
        IrTypeKind.I16 => new IrI16Literal(short.CreateTruncating(value)),
        IrTypeKind.U16 => new IrU16Literal(ushort.CreateTruncating(value)),
        IrTypeKind.I32 => new IrI32Literal(int.CreateTruncating(value)),
        IrTypeKind.U32 => new IrU32Literal(uint.CreateTruncating(value)),
        IrTypeKind.I64 => new IrI64Literal(long.CreateTruncating(value)),
        IrTypeKind.U64 => new IrU64Literal(ulong.CreateTruncating(value)),
        _ => throw new NotSupportedException("Expected an integral type.")
    };

    /// <summary>Creates a literal of the target type from the source literal's bit pattern.</summary>
    /// <remarks>
    /// F32 pairs with I32 or U32, and F64 pairs with I64 or U64. Every bit is preserved,
    /// including signed zero and the sign, payload, and quiet/signaling bit of NaNs.
    /// </remarks>
    /// <param name="source">The floating-point or integral literal to reinterpret.</param>
    /// <param name="target">The destination type in a supported pair.</param>
    /// <returns>A target-typed literal containing the same bits as the source.</returns>
    /// <exception cref="NotSupportedException">The source and target types form an unsupported pair.</exception>
    private static IrLiteral BitCast(IrLiteral source, IrType target) => (source, target.Kind) switch
    {
        (IrI32Literal x, IrTypeKind.F32) => new IrF32Literal(unchecked((uint)x.Value)),
        (IrU32Literal x, IrTypeKind.F32) => new IrF32Literal(x.Value),
        (IrI64Literal x, IrTypeKind.F64) => new IrF64Literal(unchecked((ulong)x.Value)),
        (IrU64Literal x, IrTypeKind.F64) => new IrF64Literal(x.Value),
        (IrF32Literal x, IrTypeKind.I32) => new IrI32Literal(unchecked((int)x.Bits)),
        (IrF32Literal x, IrTypeKind.U32) => new IrU32Literal(x.Bits),
        (IrF64Literal x, IrTypeKind.I64) => new IrI64Literal(unchecked((long)x.Bits)),
        (IrF64Literal x, IrTypeKind.U64) => new IrU64Literal(x.Bits),
        _ => throw new NotSupportedException("Unsupported floating-point bit cast.")
    };
}