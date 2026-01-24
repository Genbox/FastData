using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks that the first and last characters fall within observed ranges.</summary>
public sealed record CharRangeEarlyExit(char FirstMin, char FirstMax, char LastMin, char LastMax) : IEarlyExit;
