using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Extensions;

/// <summary>Classifies <see cref="BinaryOperation" /> values.</summary>
public static class BinaryOperationExtensions
{
    /// <summary>Whether the operation compares its operands and yields Bool.</summary>
    public static bool IsComparison(this BinaryOperation op) => op is BinaryOperation.Equal or BinaryOperation.NotEqual or BinaryOperation.LessThan or BinaryOperation.LessThanOrEqual or BinaryOperation.GreaterThan or BinaryOperation.GreaterThanOrEqual;
}