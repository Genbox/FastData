using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !StartsWith(prefix, inputKey);
public sealed class StringPrefixEarlyExit(string prefix, bool ignoreCase) : StringAffixEarlyExitBase(prefix, ignoreCase ? nameof(EarlyExitFunctions.StartsWithIgnoreCase) : nameof(EarlyExitFunctions.StartsWith));