using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>A value-returning function.</summary>
/// <remarks>
/// Variables are numbered within the function: parameters first, then locals. An <see cref="IrVariableId" /> is therefore a
/// position scoped to its owning function.
/// Locals are visible throughout the function, initially unassigned, and retain their values across block and loop entries.
/// </remarks>
public sealed class IrFunction : IrCallable
{
    public IrFunction(string name, IrType returnType, IReadOnlyList<IrVariable> parameters, IReadOnlyList<IrVariable> locals, IrBlock body)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
        Locals = locals;
        Body = body;
        Signature = new IrFunctionSignature(ReturnType, Parameters.Select(static parameter => parameter.Type).ToArray());
    }

    public string Name { get; }
    public IrType ReturnType { get; }
    public IReadOnlyList<IrVariable> Parameters { get; }
    public IReadOnlyList<IrVariable> Locals { get; }
    public IrBlock Body { get; }

    /// <summary>The number of variables: parameters followed by locals.</summary>
    public int VariableCount => Parameters.Count + Locals.Count;

    /// <summary>The signature derived from this function's parameters.</summary>
    public IrFunctionSignature Signature { get; }

    /// <summary>Resolves a variable by its position among the parameters and locals.</summary>
    public IrVariable GetVariable(IrVariableId id) => TryGetVariable(id, out IrVariable? variable) ? variable : throw new ArgumentOutOfRangeException(nameof(id), $"Function '{Name}' has no variable {id.Value}.");

    internal bool TryGetVariable(IrVariableId id, [NotNullWhen(true)]out IrVariable? variable)
    {
        int index = id.Value;
        if (index < 0 || index >= VariableCount)
            variable = null;
        else if (index < Parameters.Count)
            variable = Parameters[index];
        else
            variable = Locals[index - Parameters.Count];

        return variable != null;
    }
}