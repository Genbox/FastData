using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Ir.Building;

/// <summary>Owns one function's parameters, local declarations, and body.</summary>
public sealed class IrFunctionBuilder
{
    private readonly List<IrVariable> _locals = new List<IrVariable>();
    private readonly string _name;
    private readonly List<IrVariable> _parameters = new List<IrVariable>();
    private readonly IrType _returnType;
    private IrBlock? _body;

    internal IrFunctionBuilder(IrFunctionId id, string name, IrType returnType)
    {
        Id = id;
        _name = name;
        _returnType = returnType;
    }

    /// <summary>The function's program-local identity, available before defining its body.</summary>
    public IrFunctionId Id { get; }

    /// <summary>Adds a parameter in signature order. Parameters must precede locals, because variable identities are positions.</summary>
    public IrVariableId Parameter(string name, IrType type)
    {
        if (_locals.Count != 0)
            throw new InvalidOperationException($"Function '{_name}' must declare its parameters before its locals.");

        _parameters.Add(new IrVariable(name, type));
        return new IrVariableId(_parameters.Count - 1);
    }

    /// <summary>Declares a function-scoped local, initially unassigned. Assign it explicitly before reading it.</summary>
    public IrVariableId Local(string name, IrType type)
    {
        _locals.Add(new IrVariable(name, type));
        return new IrVariableId((_parameters.Count + _locals.Count) - 1);
    }

    /// <summary>Defines the body once.</summary>
    public void Define(IrBlock body)
    {
        if (_body != null)
            throw new InvalidOperationException($"'{_name}' is already defined.");

        _body = body;
    }

    internal IrFunction Build() => new IrFunction(_name, _returnType, _parameters.ToArray(), _locals.ToArray(), _body ?? throw new InvalidOperationException($"'{_name}' has no body."));
}