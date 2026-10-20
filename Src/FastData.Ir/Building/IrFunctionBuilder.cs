using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Ir.Building;

/// <summary>Owns one function's parameters, local declarations, and body.</summary>
public sealed class IrFunctionBuilder
{
    private readonly string _name;
    private readonly IrFunctionVariables _variables = new IrFunctionVariables();
    private IrBlock? _body;

    internal IrFunctionBuilder(IrFunctionId id, string name)
    {
        Id = id;
        _name = name;
    }

    /// <summary>The function's program-local identity, available before defining its body.</summary>
    public IrFunctionId Id { get; }

    /// <summary>Adds a parameter in signature order. Parameters must precede locals, because variable identities are positions.</summary>
    public IrVariableId Parameter(string name, IrType type) => _variables.Parameter(name, type);

    /// <summary>Declares a function-scoped local, initially unassigned. Assign it explicitly before reading it.</summary>
    public IrVariableId Local(string name, IrType type) => _variables.Local(name, type);

    /// <summary>Defines the body once.</summary>
    /// <remarks>The body is retained as immutable IR; its statement collection and descendants must not be modified through any alias.</remarks>
    public void Define(IrBlock body)
    {
        if (_body != null)
            throw new InvalidOperationException($"'{_name}' is already defined.");

        _body = body;
    }

    internal IrFunction Build() => _variables.Build(_name, Id.ReturnType, _body ?? throw new InvalidOperationException($"'{_name}' has no body."));
}