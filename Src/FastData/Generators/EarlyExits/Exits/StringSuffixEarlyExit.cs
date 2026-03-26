using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !EndsWith(suffix, inputKey);
public sealed record StringSuffixEarlyExit(string Suffix, bool IgnoreCase) : StringAffixEarlyExitBase(Suffix, IgnoreCase ? nameof(EarlyExitFunctions.EndsWithIgnoreCase) : nameof(EarlyExitFunctions.EndsWith));