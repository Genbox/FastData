using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests.DiffPlex;

namespace Genbox.FastData.Tests.Properties;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        DiffRunner.Disabled = true;
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}