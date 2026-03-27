using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !StartsWith(prefix, inputKey);
public sealed record StringPrefixEarlyExit(string Prefix, bool IgnoreCase) : StringAffixEarlyExitBase(Prefix, IgnoreCase ? nameof(StringFunctions.StartsWithIgnoreCase) : nameof(StringFunctions.StartsWith));