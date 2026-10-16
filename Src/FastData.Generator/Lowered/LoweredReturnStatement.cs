namespace Genbox.FastData.Generator.Lowered;

/// <summary>A return statement with an optional result.</summary>
public sealed class LoweredReturnStatement(ValidatedExpression? value) : LoweredStatement
{
    /// <summary>Gets the optional return value.</summary>
    public ValidatedExpression? Value { get; } = value;
}