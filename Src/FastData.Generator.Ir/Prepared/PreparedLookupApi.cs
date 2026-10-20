using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>Available public lookup members and their shared implementation expressions.</summary>
public sealed class PreparedLookupApi
{
    internal PreparedLookupApi(FastDataProgram lookup, IrDataId? keyStorage, IrDataId? valueStorage, IReadOnlyList<string> names)
    {
        KeyType = lookup.KeyType;
        IrVariableExpression key = Read(new IrVariableId(0, lookup.KeyType));
        IrVariableExpression slot = Read(new IrVariableId(1, IrType.I32));
        Find = new PreparedFunctionCall(names[lookup.Contract.FindFunctionId.Value], [key], IrType.I32);
        Contains = LookupApi.Found(Find);
        Found = LookupApi.Found(slot);
        if (lookup.Contract.Values is {} values)
            Value = new PreparedLookupValue(lookup.ValueType!, LookupApi.Value(values, slot));
        if (keyStorage is {} keys)
            KeySlice = new PreparedLookupValue(IrType.ArrayOf(lookup.KeyType), Read(keys));
        if (valueStorage is {} data)
            ValueSlice = new PreparedLookupValue(IrType.ArrayOf(lookup.ValueType!), Read(data));

        if (lookup.Contract.Enumeration is {} enumeration)
        {
            PreparedEnumeration keyEnumeration = new PreparedEnumeration("Keys", lookup.KeyType, names[enumeration.NextFunctionId.Value], names[enumeration.KeyFunctionId.Value]);
            Enumerations = enumeration.ValueFunctionId is {} value
                ? [keyEnumeration, new PreparedEnumeration("Values", lookup.ValueType!, names[enumeration.NextFunctionId.Value], names[value.Value])]
                : [keyEnumeration];
        }
    }

    public IrType KeyType { get; }
    public IReadOnlyList<string> VariableNames { get; } = ["key", "slot"];
    public IrExpression Contains { get; }
    public IrExpression Find { get; }
    public IrExpression Found { get; }
    public PreparedLookupValue? Value { get; }
    public PreparedLookupValue? KeySlice { get; }
    public PreparedLookupValue? ValueSlice { get; }
    public IReadOnlyList<PreparedEnumeration> Enumerations { get; } = [];
}