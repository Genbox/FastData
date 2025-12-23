namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the interface for a context used during code generation. A context represents a self-contained container of data for a data structure.</summary>
public interface IContext<TValue>
{
    ReadOnlyMemory<TValue> Values { get; }
}