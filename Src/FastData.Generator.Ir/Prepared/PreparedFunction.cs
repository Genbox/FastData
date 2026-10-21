using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>An original IR function with names and rendering metadata.</summary>
public sealed class PreparedFunction
{
    public PreparedFunction(string name, IrFunction function, IReadOnlyList<PreparedCall> calls)
    {
        Name = name;
        Function = function;
        VariableNames = PreparedNames.Create("fdVariable", function.VariableCount);
        Calls = calls;
    }

    public string Name { get; }
    public IrFunction Function { get; }
    public IReadOnlyList<string> VariableNames { get; }
    public IReadOnlyList<PreparedCall> Calls { get; }
}