namespace Genbox.FastData.Generator.Lowered;

/// <summary>A conditional with true and optional false branches.</summary>
public sealed class LoweredIfStatement(ValidatedExpression test, LoweredBlock ifTrue, LoweredBlock? ifFalse) : LoweredStatement
{
    /// <summary>Gets the validated condition.</summary>
    public ValidatedExpression Test { get; } = test;

    /// <summary>Gets the branch executed when the condition is true.</summary>
    public LoweredBlock IfTrue { get; } = ifTrue;

    /// <summary>Gets the optional branch executed when the condition is false.</summary>
    public LoweredBlock? IfFalse { get; } = ifFalse;
}