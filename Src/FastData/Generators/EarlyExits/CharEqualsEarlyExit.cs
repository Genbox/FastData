using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks that a character position equals a specific value.</summary>
public sealed record CharEqualsEarlyExit(CharPosition Position, char Value) : IEarlyExit;