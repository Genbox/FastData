using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// !EndsWith(suffix, inputKey);
public sealed class StringSuffixEarlyExit(string suffix, bool ignoreCase) : StringAffixEarlyExitBase(suffix, ignoreCase ? nameof(EarlyExitFunctions.EndsWithIgnoreCase) : nameof(EarlyExitFunctions.EndsWith));