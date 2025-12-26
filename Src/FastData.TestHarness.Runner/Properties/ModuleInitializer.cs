using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Genbox.FastData.TestHarness.Runner.Properties;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyDiffPlex.Initialize(OutputType.Compact);
}