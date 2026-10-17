namespace Genbox.FastData.Ir.Model.Abstracts;

/// <summary>An immutable array value with typed element access, independent of its storage representation.</summary>
public abstract record IrArrayLiteralBase : IrLiteral
{
    private protected IrArrayLiteralBase() {}
    public IrType ElementType => Type.ElementType!;
    public abstract int Count { get; }

    /// <summary>Returns one element as a literal of the array's element type.</summary>
    public abstract IrLiteral ElementAt(int index);
}