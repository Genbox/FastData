namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Represents structure-specific data passed from a selected structure to a code generator.</summary>
public interface IContext
{
    /// <summary>
    /// Gets the fixed-width logical size in bytes of persisted structural data. Excludes keys, values, object and array headers, alignment, embedded constants and
    /// code, and generator-specific type reduction.
    /// </summary>
    long GetOverheadBytes();
}