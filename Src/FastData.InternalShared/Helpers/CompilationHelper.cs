using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Genbox.FastData.InternalShared.Helpers;

public static class CompilationHelper
{
    public static T GetDelegate<T>(string source, bool release) where T : Delegate
    {
        return GetDelegate<T>(source, static types => types[0], release);
    }

    /// <summary>This is used to wrap a hash function and get it as a delegate</summary>
    public static T GetDelegate<T>(string source, Func<Type[], Type> typeFilter, bool release) where T : Delegate
    {
        CSharpCompilation compilation = CreateCompilation(source, release, typeof(T), typeof(DisplayAttribute));

        if (!TryGetAssembly(compilation, out Assembly? assembly, out Diagnostic[] diagnostics))
            throw new InvalidOperationException("Unable to compile delegate. Errors: " + string.Join('\n', diagnostics.Select(x => x.ToString())));

        Type[] types = assembly.GetTypes();
        Type type = typeFilter(types);

        MethodInfo me = type.GetMethods()[0];
        return me.CreateDelegate<T>();
    }

    public static CSharpCompilation CreateCompilation(string source, bool release, params Type[] types)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        HashSet<string> locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Type type in types)
        {
            locations.Add(type.Assembly.Location);
        }

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

    private static bool TryGetAssembly(Compilation compilation, [NotNullWhen(true)]out Assembly? assembly, out Diagnostic[] compilerDiagnostics)
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
}