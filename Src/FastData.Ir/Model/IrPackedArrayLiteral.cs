using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Ir.Internal;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>Stores primitive values in a contiguous typed array.</summary>
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types", Justification = "Metadata is intentionally cached separately for each primitive element type.")]
[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public sealed record IrPackedArrayLiteral<T> : IrArrayLiteralBase where T : struct
{
    private readonly T[] _values;

    /// <summary>
    /// Takes ownership of values whose element type is <typeparamref name="T" />'s default mapping,
    /// or an explicit type with the same payload.
    /// </summary>
    /// <remarks>
    /// Callers must stop modifying the array after construction.
    /// byte also holds Char(Ascii); character ranges are checked during IR validation.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="elementType" /> requires a different payload type, or an inferred type uses an unsupported <typeparamref name="T" />.
    /// </exception>
    public IrPackedArrayLiteral(T[] values, IrType? elementType = null)
    {
        _values = values;
        Values = Array.AsReadOnly(_values);

        Type = elementType switch
        {
            null => DefaultType ?? throw new ArgumentException($"'{typeof(T)}' is not an IR primitive type.", nameof(elementType)),
            _ => IrType.ArrayOf(Check(elementType))
        };
    }

    // Null marks an unsupported T so construction reports the argument error on each use.
    private static IrType? DefaultType { get; } = IrPrimitiveTypes.TryGet(typeof(T), out IrType? type) ? IrType.ArrayOf(type) : null;
    public IReadOnlyList<T> Values { get; }
    public override int Count => _values.Length;
    public override IrType Type { get; }

    private static IrType Check(IrType elementType)
    {
        if (!IrPrimitiveTypes.IsPayloadOf<T>(elementType))
            throw new ArgumentException($"'{typeof(T)}' values cannot hold '{elementType}' elements.", nameof(elementType));

        return elementType;
    }

    public ReadOnlySpan<T> AsSpan() => _values;
    public override IrLiteral ElementAt(int index) => IrClrConverter.FromValue(_values[index], ElementType);
}