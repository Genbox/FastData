using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.CPlusPlus.Shared;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

// ReSharper disable once UnusedType.Global
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public sealed class CPlusPlusContext
{
    public CPlusPlusContext()
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
        Directory.CreateDirectory(rootDir);
        Compiler = new CPlusPlusCompiler(false, rootDir);
    }

    public CPlusPlusCompiler Compiler { get; }
}