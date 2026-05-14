namespace Genbox.FastData.Generator.CSharp.Enums;

/// <summary>Specifies C#-specific code generation options.</summary>
[Flags]
public enum CSharpOptions
{
    /// <summary>Use default C# generation behavior.</summary>
    None = 0,

    /// <summary>Do not emit optimized replacements for modulus operations.</summary>
    DisableModulusOptimization = 1,

    /// <summary>Do not emit inlining hints.</summary>
    DisableInlining = 2,

    /// <summary>Emit aggressive inlining hints where supported by the template.</summary>
    AggressiveInlining = 4
}