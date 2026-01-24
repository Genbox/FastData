using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks that a character position falls within an observed range.</summary>
public sealed record CharRangeEarlyExit(CharPosition Position, char Min, char Max) : IEarlyExit;
