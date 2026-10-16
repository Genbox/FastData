namespace Genbox.FastData.Generator.Lowered;

/// <summary>A loop whose condition is evaluated before each iteration.</summary>
public sealed class LoweredWhileStatement(ValidatedExpression test, LoweredBlock body) : LoweredStatement
{
    /// <summary>Gets the validated condition.</summary>
    public ValidatedExpression Test { get; } = test;
    /// <summary>Gets the statement body.</summary>
    public LoweredBlock Body { get; } = body;
}