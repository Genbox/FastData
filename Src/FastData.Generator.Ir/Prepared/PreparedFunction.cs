using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A function signature, generated variable names, and a backend-prepared body.</summary>
public sealed class PreparedFunction
{
    public PreparedFunction(string name, IrFunction function, IrBlock body)
    {
        Name = name;
        ReturnType = function.ReturnType;
        Body = body;
        VariableNames = PreparedNames.Create("fdVariable", function.VariableCount);
        Parameters = function.Parameters.Select((parameter, i) => new PreparedDeclaration(VariableNames[i], parameter.Type)).ToArray();
        Locals = function.Locals.Select((local, i) => new PreparedDeclaration(VariableNames[function.Parameters.Count + i], local.Type)).ToArray();
    }

    public string Name { get; }
    public IrBlock Body { get; }

    public IrType ReturnType { get; }
    public IReadOnlyList<PreparedDeclaration> Parameters { get; }
    public IReadOnlyList<PreparedDeclaration> Locals { get; }

    /// <summary>Unique parameter and local names indexed by variable ID.</summary>
    public IReadOnlyList<string> VariableNames { get; }
}