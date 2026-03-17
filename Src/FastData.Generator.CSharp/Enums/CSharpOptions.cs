namespace Genbox.FastData.Generator.CSharp.Enums;

[Flags]
public enum CSharpOptions
{
    None = 0,
    DisableModulusOptimization = 1,
    DisableInlining = 2,
    AggressiveInlining = 4
}