namespace Genbox.FastData.Internal;

internal static class GlobalOptions
{
    // The options in this class are intended for internal debugging and performance measurements

    /// <summary>Enable this to enable the code generator to detect and use faster modulus alternatives.</summary>
    internal const bool OptimizeModulus = true;

    /// <summary>Enable this to generate early exit conditions for methods. Min/max string length and/or hash code tests.</summary>
    internal const bool GenerateEarlyCondition = true;

    /// <summary>Enable this to disable inlining of functions. Only beneficial in benchmarks to give a fairer view of results.</summary>
    internal const bool DisableInlining = false;
}