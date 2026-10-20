using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Ir.Building;

/// <summary>Allocates typed function-scoped variable IDs with parameters before locals.</summary>
public sealed class IrFunctionVariables
{
    private readonly List<IrVariable> _locals = new List<IrVariable>();
    private readonly List<IrVariable> _parameters = new List<IrVariable>();

    /// <summary>Adds a parameter in signature order, before any locals.</summary>
    public IrVariableId Parameter(string name, IrType type)
    {
        if (_locals.Count != 0)
            throw new InvalidOperationException("Parameters must be declared before locals.");

        _parameters.Add(new IrVariable(name, type));
        return new IrVariableId(_parameters.Count - 1, type);
    }

    /// <summary>Adds a local after the function's parameters and previous locals.</summary>
    public IrVariableId Local(string name, IrType type)
    {
        _locals.Add(new IrVariable(name, type));
        return new IrVariableId((_parameters.Count + _locals.Count) - 1, type);
    }

    /// <summary>Copies the mutable parameter and local lists into a function with the supplied body.</summary>
    /// <remarks>Later variable declarations do not change previous results. The immutable body and its descendants are shared without copying.</remarks>
    public IrFunction Build(string name, IrType returnType, IrBlock body) =>
        new IrFunction(name, returnType, _parameters.ToArray(), _locals.ToArray(), body);
}