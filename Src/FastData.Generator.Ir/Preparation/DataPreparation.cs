using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Visiting;

namespace Genbox.FastData.Generator.Ir.Preparation;

/// <summary>Names aggregate literals and orders their storage before the declarations that use them.</summary>
internal sealed class DataPreparation : IrWalker
{
    private readonly List<PreparedData> _declarations = [];
    private readonly Dictionary<IrLiteral, string> _names = new Dictionary<IrLiteral, string>();
    private int _next;

    private DataPreparation() {}

    private void Add(PreparedData data)
    {
        base.VisitLiteral(data.Value);

        // The first name is the canonical reference; each explicitly named declaration is still emitted.
        if (data.Value is IrArrayLiteralBase or IrRecordLiteral)
            _names.TryAdd(data.Value, data.Name);
        _declarations.Add(data);
    }

    public static PreparedEmissionPlan Build(IReadOnlyList<PreparedData> data, IReadOnlyList<PreparedFunction> functions)
    {
        DataPreparation preparation = new DataPreparation();
        foreach (PreparedData declaration in data)
            preparation.Add(declaration);
        foreach (PreparedFunction function in functions)
            preparation.Visit(function.Function);
        return new PreparedEmissionPlan(preparation._declarations, functions, preparation._names);
    }

    public override void VisitLiteral(IrLiteral literal)
    {
        if (literal is IrArrayLiteralBase or IrRecordLiteral && !_names.ContainsKey(literal))
            Add(new PreparedData(PreparedNames.Name("fdLiteral", _next++), literal));
    }
}