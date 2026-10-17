using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Internal;

internal static class IrPrimitiveTypes
{
    private static readonly Dictionary<Type, IrType> _types = new Dictionary<Type, IrType>(26);
    private static readonly Dictionary<IrType, Func<Array, IrLiteral>> _packedArrays = new Dictionary<IrType, Func<Array, IrLiteral>>(13);

    // Closed generic calls keep array factories visible to trimming/AOT.
    static IrPrimitiveTypes()
    {
        AddScalar<string>(IrType.String);
        AddValueType<bool>(IrType.Bool);
        AddValueType<char>(IrType.Char(IrCharacterKind.Utf16CodeUnit));
        AddValueType<sbyte>(IrType.I8);
        AddValueType<byte>(IrType.U8);
        AddValueType<short>(IrType.I16);
        AddValueType<ushort>(IrType.U16);
        AddValueType<int>(IrType.I32);
        AddValueType<uint>(IrType.U32);
        AddValueType<long>(IrType.I64);
        AddValueType<ulong>(IrType.U64);
        AddValueType<float>(IrType.F32);
        AddValueType<double>(IrType.F64);

        // Characters whose payload is shared with an unsigned type are always typed explicitly.
        AddPackedArray<byte>(IrType.Char(IrCharacterKind.Ascii));
    }

    public static bool TryGet(Type type, [NotNullWhen(true)]out IrType? result) => _types.TryGetValue(type, out result);

    public static bool TryGetPackedArrayFactory(IrType type, [NotNullWhen(true)]out Func<Array, IrLiteral>? factory) => _packedArrays.TryGetValue(type, out factory);

    private static void AddValueType<T>(IrType type) where T : struct
    {
        AddScalar<T>(type);
        AddPackedArray<T>(type);
    }

    private static void AddPackedArray<T>(IrType elementType) where T : struct => _packedArrays.Add(IrType.ArrayOf(elementType), x =>
    {
        if (x.GetType() == typeof(T[]))
            return new IrPackedArrayLiteral<T>((T[])x, elementType);

        throw new ArgumentException($"Packed values require a '{typeof(T[])}' array.", nameof(x));
    });

    /// <summary>Whether <typeparamref name="T" /> is the packed payload of an element type: its default CLR mapping, or byte for ASCII characters.</summary>
    public static bool IsPayloadOf<T>(IrType elementType) => (_types.TryGetValue(typeof(T), out IrType? type) && type == elementType) || (typeof(T) == typeof(byte) && elementType == IrType.Char(IrCharacterKind.Ascii));

    private static void AddScalar<T>(IrType type)
    {
        _types.Add(typeof(T), type);
        _types.Add(typeof(T[]), IrType.ArrayOf(type));
    }
}