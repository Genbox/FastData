using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// inputKey => ((uint)GetLastChar(inputKey) < 64u)
//    ? ((Low & (1UL << (int)(uint)GetLastChar(inputKey))) == 0UL)
//    : ((High & (1UL << (int)((uint)GetLastChar(inputKey) - 64u))) == 0UL);
public sealed class CharLastBitmapEarlyExit(ulong low, ulong high, bool ignoreCase) : CharBitmapEarlyExitBase(low, high, ignoreCase ? nameof(EarlyExitFunctions.GetLastCharLower) : nameof(EarlyExitFunctions.GetLastChar));