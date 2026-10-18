using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Operator symbols shared by languages with C-style expressions.</summary>
public static class CStyleOperators
{
    /// <summary>Returns the short-circuit operator symbol.</summary>
    public static string Symbol(LogicalOperation operation) => operation == LogicalOperation.AndAlso ? "&&" : "||";

    /// <summary>Returns the binary operator symbol.</summary>
    public static string Symbol(BinaryOperation operation) => operation switch
    {
        BinaryOperation.Add => "+",
        BinaryOperation.Subtract => "-",
        BinaryOperation.Multiply => "*",
        BinaryOperation.Divide => "/",
        BinaryOperation.Modulo => "%",
        BinaryOperation.Equal => "==",
        BinaryOperation.NotEqual => "!=",
        BinaryOperation.LessThan => "<",
        BinaryOperation.LessThanOrEqual => "<=",
        BinaryOperation.GreaterThan => ">",
        BinaryOperation.GreaterThanOrEqual => ">=",
        BinaryOperation.And => "&",
        BinaryOperation.Or => "|",
        BinaryOperation.ExclusiveOr => "^",
        BinaryOperation.ShiftLeft => "<<",
        BinaryOperation.ShiftRight => ">>",
        _ => throw new NotSupportedException($"Operation '{operation}' is not supported by C-style operators.")
    };
}