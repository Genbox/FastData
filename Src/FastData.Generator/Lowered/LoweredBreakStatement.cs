namespace Genbox.FastData.Generator.Lowered;

/// <summary>A statement that exits the enclosing loop.</summary>
public sealed class LoweredBreakStatement : LoweredStatement
{
    private LoweredBreakStatement() {}

    /// <summary>Gets the shared statement instance.</summary>
    public static LoweredBreakStatement Instance { get; } = new LoweredBreakStatement();
}