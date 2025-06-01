using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Genbox.FastData.Generator.CSharp.Tests.Properties;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyDiffPlex.Initialize(OutputType.Compact);
}