using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A record declaration with generated field names shared by declarations and accesses.</summary>
public sealed class PreparedRecord
{
    internal PreparedRecord(string name, IrRecord record)
    {
        Name = name;
        Record = record;
        FieldNames = PreparedNames.Create("fdField", record.Fields.Count);
    }

    public string Name { get; }

    public IrRecord Record { get; }

    /// <summary>Generated names in original field order, indexed by field ID.</summary>
    public IReadOnlyList<string> FieldNames { get; }
}