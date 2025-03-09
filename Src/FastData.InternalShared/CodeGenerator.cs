using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Genbox.FastData.InternalShared;

public static class CodeGenerator
{
    /// <summary>This is used to wrap a hash function and get it as a delegate</summary>
    public static T GetDelegate<T>(string source, bool release, bool disableWrapper = false) where T : Delegate
    {
        string wrapped = $$"""
                           using System;
                           using Genbox.FastData.Helpers;
                           using System.Runtime.InteropServices;  // MethodImpl
                           using System.Runtime.CompilerServices; // Unsafe

                           public static class Wrapper
                           {
                               {{source}}
                           }
                           """;

        CSharpCompilation compilation = CreateCompilation(disableWrapper ? source : wrapped, release, typeof(T), typeof(DisplayAttribute));

        if (!TryGetAssembly(compilation, out Assembly? assembly, out Diagnostic[] diagnostics))
            throw new InvalidOperationException("Unable to compile delegate. Errors: " + string.Join('\n', diagnostics.Select(x => x.ToString())));

        Type[] types = assembly.GetTypes();
        Type? type = types[0]; //We should only have one type

        if (type == null)
            throw new InvalidOperationException("Unable to find type");

        MethodInfo me = type.GetMethods()[0];
        return me.CreateDelegate<T>();
    }

    /// <summary>This is used to compile a FastData and get it as an IFastSet instance</summary>
    public static IFastSet<T> CreateFastSet<T>(string source, bool release)
    {
        CSharpCompilation compilation = CreateCompilation(source, release);
        ImmutableArray<Diagnostic> diag = compilation.GetDiagnostics();

        if (diag.Length > 0)
            throw new InvalidOperationException("C# compiler reported errors: " + string.Join('\n', diag.Select(x => x.ToString())));

        if (!TryGetAssembly(compilation, out Assembly? assembly, out Diagnostic[] errors))
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
        CSharpCompilation compilation = CreateCompilation(source, release, typeof(T), typeof(DisplayAttribute));

        T generator = new T();

        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outCompilation, out ImmutableArray<Diagnostic> diagnostics);

        codeGenDiagnostics = diagnostics.ToArray();
    }

    public static bool TryGetAssembly(Compilation compilation, [NotNullWhen(true)]out Assembly? assembly, out Diagnostic[] compilerDiagnostics)
    {
        using MemoryStream asmStream = new MemoryStream();
        using MemoryStream symStream = new MemoryStream();
        EmitResult emitResult = compilation.Emit(asmStream, symStream);

        compilerDiagnostics = emitResult.Diagnostics.ToArray();

        if (!emitResult.Success)
        {
            assembly = null;
            return false;
        }

        byte[] assemblyBytes = asmStream.ToArray();
        byte[] symbolsBytes = symStream.ToArray();
        assembly = Assembly.Load(assemblyBytes, symbolsBytes);
        return true;
    }

    public static CSharpCompilation CreateCompilation(string source, bool release, params Type[] types)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        HashSet<string> locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Type type in types)
            locations.Add(type.Assembly.Location);

        foreach (Assembly assembly in AssemblyLoadContext.Default.Assemblies)
        {
            if (assembly.IsDynamic)
                continue;

            if (string.IsNullOrEmpty(assembly.Location))
                continue;

            locations.Add(assembly.Location);
        }

        CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: release ? OptimizationLevel.Release : OptimizationLevel.Debug,
            allowUnsafe: true,
            platform: Platform.X64,
            warningLevel: 0,
            deterministic: true);

        return CSharpCompilation.Create("generator", [syntaxTree], locations.Select(x => MetadataReference.CreateFromFile(x)), options);
    }
}