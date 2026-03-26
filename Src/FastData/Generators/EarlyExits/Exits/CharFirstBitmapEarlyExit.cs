using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey => ((uint)GetFirstChar(inputKey) < 64u)
//     ? ((Low & (1UL << (int)(uint)GetFirstChar(inputKey))) == 0UL)
//     : ((High & (1UL << (int)((uint)GetFirstChar(inputKey) - 64u))) == 0UL);
public sealed record CharFirstBitmapEarlyExit(ulong Low, ulong High, bool IgnoreCase) : CharBitmapEarlyExitBase(Low, High, IgnoreCase ? nameof(EarlyExitFunctions.GetFirstCharLower) : nameof(EarlyExitFunctions.GetFirstChar));