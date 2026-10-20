using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Internal;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Interop;

/// <summary>Converts CLR primitive values at the IR boundary. Logical records are lowered by the target backend.</summary>
public static class IrClrConverter
{
    /// <summary>Maps a CLR primitive or one-dimensional, zero-based primitive array. <see cref="char" /> maps to a UTF-16 unit character.</summary>
    /// <exception cref="NotSupportedException"><paramref name="type" /> is outside the supported CLR types.</exception>
    public static IrType FromClrType(Type type) => IrPrimitiveTypes.TryGet(type, out IrType? result) ? result : throw new NotSupportedException($"CLR type '{type}' has no built-in IR mapping.");

    /// <summary>Captures a CLR value as an immutable literal. Supply the reference type for null values and the program encoding for string arrays.</summary>
    /// <remarks>
    /// Primitive arrays transfer ownership of their buffers without copying; callers must not modify them through any alias afterward. String arrays are encoded into new storage and are not retained. Aggregate conversions create new element collections; any nested primitive arrays still transfer
    /// ownership of their buffers.
    /// </remarks>
    public static IrLiteral FromValue(object? value, IrType? type = null, IrTextEncoding encoding = IrTextEncoding.Utf16)
    {
        if (value == null)
            return new IrNullLiteral(type ?? throw new ArgumentException("A null value requires an explicit type.", nameof(type)));

        if (value is Array array)
            return FromArray(array, type, encoding);

        IrLiteral literal = value switch
        {
            bool x => new IrBoolLiteral(x),
            char x => new IrCharLiteral(IrCharacterKind.Utf16CodeUnit, x),
            sbyte x => new IrI8Literal(x),
            byte x when type == IrType.Char(IrCharacterKind.Ascii) => new IrCharLiteral(IrCharacterKind.Ascii, x),
            byte x => new IrU8Literal(x),
            short x => new IrI16Literal(x),
            ushort x => new IrU16Literal(x),
            int x => new IrI32Literal(x),
            uint x => new IrU32Literal(x),
            long x => new IrI64Literal(x),
            ulong x => new IrU64Literal(x),
            float x => IrF32Literal.FromValue(x),
            double x => IrF64Literal.FromValue(x),
            string x => new IrStringLiteral(x),
            _ => throw new NotSupportedException($"Unsupported scalar '{value.GetType()}'.")
        };

        if (type != null && type != literal.Type)
            throw new ArgumentException($"Value '{value.GetType()}' does not match IR type '{type}'.", nameof(value));

        return literal;
    }

    private static IrLiteral FromArray(Array array, IrType? type, IrTextEncoding encoding)
    {
        if (array.Rank != 1 || array.GetLowerBound(0) != 0)
            throw new ArgumentException("Array literals require a one-dimensional, zero-based CLR array.", nameof(array));

        type ??= FromClrType(array.GetType());
        if (type.Kind != IrTypeKind.Array)
            throw new ArgumentException($"Value '{array.GetType()}' does not match IR type '{type}'.", nameof(array));

        if (IrPrimitiveTypes.TryGetPackedArrayFactory(type, out Func<Array, IrLiteral>? capture))
            return capture(array);

        if (type.ElementType == IrType.String)
            return new IrText(encoding).PackStrings((string?[])array);

        IrLiteral[] elements = new IrLiteral[array.Length];
        int i = 0;
        foreach (object? element in array)
            elements[i++] = FromValue(element, type.ElementType, encoding);

        return new IrArrayLiteral(type.ElementType!, elements);
    }
}