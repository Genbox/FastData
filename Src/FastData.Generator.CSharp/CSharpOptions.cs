namespace Genbox.FastData.Generator.CSharp;

[Flags]
public enum CSharpOptions
{
    None = 0,
    DisableEarlyExits = 1,
    DisableModulusOptimization = 2,
    DisableInlining = 4,
    AggressiveInlining = 8
}