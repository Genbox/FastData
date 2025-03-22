using System.Collections.Immutable;
using System.Reflection;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.InternalShared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Genbox.FastData.Generator.CSharp.Shared;

public static class CodeGenerator
{
    /// <summary>This is used to compile a FastData and get it as an IFastSet instance</summary>
    public static IFastSet<T> CreateFastSet<T>(string source, bool release)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, release);
        ImmutableArray<Diagnostic> diag = compilation.GetDiagnostics();

        if (diag.Length > 0)
            throw new InvalidOperationException("C# compiler reported errors: " + string.Join('\n', diag.Select(x => x.ToString())));

        if (!CompilationHelper.TryGetAssembly(compilation, out Assembly? assembly, out Diagnostic[] errors))
            throw new InvalidOperationException("Unable to compile set. Errors: " + string.Join('\n', errors.Select(x => x.ToString())));

        Type[] types = assembly.GetTypes();
        Type? type = types[0];

        if (type == null)
            throw new InvalidOperationException("Unable to find type");

        IFastSet<T>? set = (IFastSet<T>?)Activator.CreateInstance(type);

        if (set == null)
            throw new InvalidOperationException("Instance was null");

        return set;
    }
}