namespace Genbox.FastData.Generator.CSharp.Enums;

[Flags]
public enum CSharpOptions
{
    None = 0,
    DisableEarlyExits = 1,
    DisableModulusOptimization = 2,
    DisableInlining = 4,
    AggressiveInlining = 8,
    GenerateInterface = 16
}