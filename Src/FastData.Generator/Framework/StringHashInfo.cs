using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.Framework;

public sealed class StringHashInfo(string stringHash, ReaderFunctions functions, StateInfo[]? state)
{
    public ReaderFunctions ReaderFunctions { get; } = functions;
    public string HashSource { get; } = stringHash;
    public StateInfo[]? State { get; } = state;
}