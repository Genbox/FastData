namespace Genbox.FastData.Ir.Model.Enums;

/// <summary>Short-circuit Boolean operations.</summary>
public enum LogicalOperation
{
    Unknown = 0,

    /// <summary>Evaluates Right only when Left is true.</summary>
    AndAlso,

    /// <summary>Evaluates Right only when Left is false.</summary>
    OrElse
}