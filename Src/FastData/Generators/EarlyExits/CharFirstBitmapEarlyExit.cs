using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// inputKey => ((uint)GetFirstChar(inputKey) < 64u)
//     ? ((Low & (1UL << (int)(uint)GetFirstChar(inputKey))) == 0UL)
//     : ((High & (1UL << (int)((uint)GetFirstChar(inputKey) - 64u))) == 0UL);
public sealed class CharFirstBitmapEarlyExit(ulong low, ulong high, bool ignoreCase) : CharBitmapEarlyExitBase(low, high, ignoreCase ? nameof(EarlyExitFunctions.GetFirstCharLower) : nameof(EarlyExitFunctions.GetFirstChar));