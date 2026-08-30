namespace Genbox.FastData.Generators.Expressions;

/// <summary>Identifies the role of an expression in a generated method.</summary>
public enum ExprKind : byte
{
    /// <summary>The role is not known.</summary>
    Unknown = 0,

    /// <summary>The expression allocates or assigns a value.</summary>
    Assignment,

    /// <summary>The expression returns before the main lookup.</summary>
    EarlyExit
}