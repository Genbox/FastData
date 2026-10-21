using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>Complete, dependency-ordered storage and functions for one emitted scope.</summary>
public sealed class PreparedEmissionPlan
{
    private readonly Dictionary<IrLiteral, string> _references;

    internal PreparedEmissionPlan(IReadOnlyList<PreparedData> data, IReadOnlyList<PreparedFunction> functions, Dictionary<IrLiteral, string> references)
    {
        Data = data;
        Functions = functions;
        _references = references;
    }

    public IReadOnlyList<PreparedData> Data { get; }
    public IReadOnlyList<PreparedFunction> Functions { get; }
    public string? Reference(IrLiteral literal) => _references.TryGetValue(literal, out string? name) ? name : null;

    public static PreparedEmissionPlan Create(IReadOnlyList<PreparedData> data, IReadOnlyList<PreparedFunction> functions) => DataPreparation.Build(data, functions);
}