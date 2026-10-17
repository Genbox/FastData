namespace Genbox.FastData.Ir.Model;

public sealed class IrFunctionSignature(IrType returnType, IReadOnlyList<IrType> parameters)
{
    public IrType ReturnType { get; } = returnType;
    public IReadOnlyList<IrType> Parameters { get; } = parameters;
}