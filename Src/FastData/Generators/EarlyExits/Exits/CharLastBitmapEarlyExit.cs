using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey => ((uint)GetLastChar(inputKey) < 64u)
//    ? ((Low & (1UL << (int)(uint)GetLastChar(inputKey))) == 0UL)
//    : ((High & (1UL << (int)((uint)GetLastChar(inputKey) - 64u))) == 0UL);
public sealed record CharLastBitmapEarlyExit(ulong Low, ulong High, bool IgnoreCase) : CharBitmapEarlyExitBase(Low, High, IgnoreCase ? nameof(EarlyExitFunctions.GetLastCharLower) : nameof(EarlyExitFunctions.GetLastChar));