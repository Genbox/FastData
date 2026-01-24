using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>If all strings have a common prefix or suffix, this early exit will check for it.</summary>
public sealed record StringPrefixSuffixEarlyExit(string prefix, string suffix) : IEarlyExit;