using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>An immutable array of array or record values. Elements are available through indexed access.</summary>
/// <remarks>Scalar arrays use <see cref="IrPackedArrayLiteral{T}" /> or <see cref="IrPackedStringArrayLiteral" />; construction rejects them here.</remarks>
public sealed record IrArrayLiteral : IrArrayLiteralBase
{
    /// <summary>Takes ownership of the element collection without copying it.</summary>
    /// <remarks>Callers must not modify the collection through any alias after construction; immutable literals may be shared.</remarks>
    public IrArrayLiteral(IrType elementType, IReadOnlyList<IrLiteral> elements)
    {
        Require(elementType.Kind is IrTypeKind.Array or IrTypeKind.Record, "Scalar and String arrays must use packed literals.");
        foreach (IrLiteral element in elements)
            Require(element.Type == elementType, "Array literal element type does not match its array.", code: IrDiagnosticCode.TypeMismatch);

        Type = IrType.ArrayOf(elementType);
        Elements = elements;
    }

    public override int Count => Elements.Count;
    public IReadOnlyList<IrLiteral> Elements { get; }
    public override IrType Type { get; }
    public override IrLiteral ElementAt(int index) => Elements[index];
}