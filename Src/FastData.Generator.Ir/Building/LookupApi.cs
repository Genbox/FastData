using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Building;

/// <summary>Expressions shared by target-language lookup API wrappers.</summary>
public static class LookupApi
{
    /// <summary>Searches for the key and returns its slot or the miss sentinel.</summary>
    public static IrFunctionCallExpression Find(LookupContract contract, IrExpression key) => Call(contract.FindFunctionId, key);

    /// <summary>Tests membership from a search result.</summary>
    public static IrBinaryExpression Found(IrExpression slot) => GreaterOrEqual(slot, I32(0));

    /// <summary>Tests whether the key is present.</summary>
    public static IrBinaryExpression Contains(LookupContract contract, IrExpression key) => Found(Find(contract, key));

    /// <summary>Reads the payload associated with a successful search slot.</summary>
    public static IrExpression Value(LookupValues values, IrExpression slot)
    {
        IrIndexExpression element = Index(Read(values.DataId), slot);
        return values.FieldId is {} field ? Field(element, field) : element;
    }
}