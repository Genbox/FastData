using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Genbox.FastData.SourceGenerator.Tests;

public static class SourceGenHelper
{
    public static string RunSourceGenerator<T>(string source, bool release, out Diagnostic[] compilerDiagnostics, out Diagnostic[] codeGenDiagnostics) where T : IIncrementalGenerator, new()
    {
        RunSourceGenerator<T>(source, release, out codeGenDiagnostics, out Compilation compilation);
        compilerDiagnostics = compilation.GetDiagnostics().ToArray();

        StringBuilder sb = new StringBuilder();

        foreach (SyntaxTree tree in compilation.SyntaxTrees.Skip(1))
            sb.AppendLine(tree.ToString());

        return sb.ToString();
    }

    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"")]
    private static void RunSourceGenerator<T>(string source, bool release, out Diagnostic[] codeGenDiagnostics, out Compilation outCompilation) where T : IIncrementalGenerator, new()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, release, typeof(T), typeof(DisplayAttribute));

        T generator = new T();

        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outCompilation, out ImmutableArray<Diagnostic> diagnostics);

        codeGenDiagnostics = diagnostics.ToArray();
    }
}