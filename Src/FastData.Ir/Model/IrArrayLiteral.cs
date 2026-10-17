using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>An immutable array of array or record values. Elements are available through indexed access.</summary>
/// <remarks>Scalar arrays use <see cref="IrPackedArrayLiteral{T}" /> or <see cref="IrPackedStringArrayLiteral" />; validation rejects them here.</remarks>
public sealed record IrArrayLiteral : IrArrayLiteralBase
{
    public IrArrayLiteral(IrType elementType, IReadOnlyList<IrLiteral> elements)
    {
        Type = IrType.ArrayOf(elementType);
        Elements = elements;
    }

    public override int Count => Elements.Count;
    public IReadOnlyList<IrLiteral> Elements { get; }
    public override IrType Type { get; }
    public override IrLiteral ElementAt(int index) => Elements[index];
}