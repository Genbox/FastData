namespace Genbox.FastData.Ir.Model.Enums;

/// <summary>Eager binary operations. Arithmetic operates on integers; unsigned operations wrap and signed operations require representable results.</summary>
public enum BinaryOperation
{
    Unknown = 0,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    And,
    Or,
    ExclusiveOr,
    ShiftLeft,
    ShiftRight
}