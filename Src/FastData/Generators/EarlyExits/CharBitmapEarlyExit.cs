using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks a character position against an ASCII bitmap.</summary>
public sealed record CharBitmapEarlyExit(CharPosition Position, ulong Low, ulong High) : IEarlyExit;