using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Harness;

internal sealed class HarnessFunction(string name, IrType result)
{
    private readonly IrFunctionVariables _variables = new IrFunctionVariables();

    public IrVariableId Parameter(string variable, IrType type) => _variables.Parameter(variable, type);

    public IrVariableId Local(string variable, IrType type) => _variables.Local(variable, type);

    public PreparedFunction Build(IrBlock body) => new PreparedFunction(name, _variables.Build(name, result, body), []);
}