namespace Genbox.FastData.Generator.Lowered;

/// <summary>A statement that advances the enclosing loop.</summary>
public sealed class LoweredContinueStatement : LoweredStatement
{
    private LoweredContinueStatement() {}

    /// <summary>Gets the shared statement instance.</summary>
    public static LoweredContinueStatement Instance { get; } = new LoweredContinueStatement();
}