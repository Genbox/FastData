namespace Genbox.FastData.Generator.CPlusPlus;

[Flags]
public enum CPlusPlusOptions
{
    None = 0,
    DisableEarlyExits = 1,
    DisableModulusOptimization = 2,
    DisableInlining = 4,
    AggressiveInlining = 8
}