namespace Genbox.FastData.Ir.Model;

/// <summary>A return type and ordered parameter types.</summary>
/// <remarks>Takes ownership of the parameter collection without copying it. Callers must not modify it through any alias; immutable types may be shared.</remarks>
public sealed class IrFunctionSignature
{
    public IrFunctionSignature(IrType returnType, IReadOnlyList<IrType> parameters)
    {
        ReturnType = returnType ?? throw Fail("A type is required.");
        foreach (IrType parameter in parameters)
            Require(parameter != null, "A type is required.");
        Parameters = parameters;
    }

    public IrType ReturnType { get; }
    public IReadOnlyList<IrType> Parameters { get; }
}