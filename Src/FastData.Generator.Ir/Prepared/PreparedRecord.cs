using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A record declaration with generated field names shared by declarations and accesses.</summary>
public sealed class PreparedRecord
{
    internal PreparedRecord(string name, IrRecord record)
    {
        Name = name;
        Fields = record.Fields.Select((field, i) => new PreparedDeclaration(PreparedNames.Name("fdField", i), field.Type)).ToArray();
    }

    public string Name { get; }

    /// <summary>Fields in original declaration order, indexed by field ID.</summary>
    public IReadOnlyList<PreparedDeclaration> Fields { get; }
}