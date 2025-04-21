using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Genbox.FastData.Generator.CPlusPlus.Tests.Properties;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyDiffPlex.Initialize(OutputType.Compact);
}