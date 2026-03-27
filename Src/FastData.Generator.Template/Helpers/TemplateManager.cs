using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;

namespace Genbox.FastData.Generator.Template.Helpers;

public class TemplateManager
{
    private readonly string _cacheDirectory;
    private readonly string _classNamespace;
    private readonly bool _release;

    public TemplateManager(string language, string workDir, bool release)
    {
        _classNamespace = "Genbox.FastData.TemplateCache." + language;
        _cacheDirectory = Path.Combine(workDir, "TemplateCache", language);
        _release = release;

        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);
    }

    public string Render(string filePath, string source, Dictionary<string, object?> variables)
    {
        string className = Path.GetFileNameWithoutExtension(filePath);

        TemplateGenerator generator = CreateGenerator(variables);
        string assemblyPath = CompileTemplateAssembly(generator, filePath, source, className);

        return ExecuteCompiledTemplate(className, assemblyPath, variables);
    }

    private static TemplateGenerator CreateGenerator(Dictionary<string, object?> variables)
    {
        TemplateGenerator generator = new TemplateGenerator();
        AddTemplateReference(generator, typeof(TypeCode));
        AddTemplateReference(generator, typeof(FormatHelper));
        AddTemplateReference(generator, typeof(Expression));

        foreach (KeyValuePair<string, object?> pair in variables)
        {
            if (pair.Value == null)
                continue;

            AddTemplateReference(generator, pair.Value.GetType());
        }

        return generator;
    }

    private string CompileTemplateAssembly(TemplateGenerator generator, string filePath, string source, string className)
    {
        ParsedTemplate parsed = generator.ParseTemplate(filePath, source);

        TemplateSettings settings = TemplatingEngine.GetSettings(generator, parsed);
        settings.Culture = CultureInfo.InvariantCulture;
        settings.Debug = !_release;
        settings.Encoding = Encoding.UTF8;
        settings.Name = className;
        settings.Namespace = _classNamespace;

        string preprocessed = generator.PreprocessTemplate(parsed, filePath, source, settings, out string[] references);

        string name = className + "." + GetShortHash(preprocessed) + ".dll";
        string assemblyPath = Path.Combine(_cacheDirectory, name);

        if (File.Exists(assemblyPath))
            return assemblyPath;

        if (generator.Errors.HasErrors)
            throw new InvalidOperationException($"Failed to preprocess template '{filePath}':\n{FormatErrors(generator.Errors)}");

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(preprocessed, new CSharpParseOptions(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            name,
            [syntaxTree],
            GetMetadataReferences(generator, settings, references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: _release ? OptimizationLevel.Release : OptimizationLevel.Debug));

        ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

        if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
            throw new InvalidOperationException($"Failed to compile template '{filePath}':\n{FormatDiagnostics(diagnostics)}");

        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        EmitResult emitResult = compilation.Emit(assemblyPath, pdbPath);

        if (!emitResult.Success)
            throw new InvalidOperationException($"Failed to emit template '{filePath}':\n{FormatDiagnostics(emitResult.Diagnostics)}");

        return assemblyPath;
    }

    private string ExecuteCompiledTemplate(string className, string assemblyPath, Dictionary<string, object?> variables)
    {
        Assembly assembly = Assembly.Load(File.ReadAllBytes(assemblyPath));
        string typeName = _classNamespace + "." + className;
        Type templateType = assembly.GetType(typeName, true);
        object instance = Activator.CreateInstance(templateType) ?? throw new InvalidOperationException($"Failed to create template instance for '{typeName}'.");

        TextTemplatingSession session = new TextTemplatingSession();

        foreach (KeyValuePair<string, object?> pair in variables)
        {
            if (pair.Value == null)
                continue;

            session.Add(pair.Key, pair.Value);
        }

        PropertyInfo? sessionProperty = templateType.GetProperty("Session", BindingFlags.Instance | BindingFlags.Public);

        if (sessionProperty == null)
            throw new InvalidOperationException($"'{typeName}' does not define Session().");

        sessionProperty.SetValue(instance, session);

        MethodInfo? initialize = templateType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (initialize == null)
            throw new InvalidOperationException($"'{typeName}' does not define initialize().");

        initialize.Invoke(instance, null);

        MethodInfo? transformText = templateType.GetMethod("TransformText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (transformText == null)
            throw new InvalidOperationException($"Template '{typeName}' does not define TransformText().");

        object? result = transformText.Invoke(instance, null);

        if (result == null)
            throw new InvalidOperationException("Failed to get output of TransformText()");

        string? str = result.ToString();

        return str.Trim();
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences(TemplateGenerator generator, TemplateSettings settings, string[] references)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddReferencePaths(paths, references);
        AddReferencePaths(paths, generator.Refs);
        AddReferencePaths(paths, settings.Assemblies);

        // Add standard references for .NET
        string? dotNetDir = Path.GetDirectoryName(typeof(object).Assembly.Location);

        if (dotNetDir == null)
            throw new InvalidOperationException("Unable to find .NET runtime");

        paths.Add(Path.Combine(dotNetDir, "System.Runtime.dll"));
        paths.Add(Path.Combine(dotNetDir, "System.Collections.dll"));
        paths.Add(Path.Combine(dotNetDir, "System.Collections.NonGeneric.dll"));
        paths.Add(Path.Combine(dotNetDir, "System.Linq.dll"));
        paths.Add(Path.Combine(dotNetDir, "netstandard.dll"));

        paths.Add(Path.Combine(AppContext.BaseDirectory, "System.CodeDom.dll"));

        return paths.Select(r => MetadataReference.CreateFromFile(r));
    }

    private static void AddReferencePaths(HashSet<string> referencePaths, ICollection<string> references)
    {
        foreach (string reference in references)
        {
            if (string.IsNullOrWhiteSpace(reference))
                continue;

            referencePaths.Add(reference);
        }
    }

    private static string GetShortHash(string source)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        byte[] hash;

        using (SHA256 sha256 = SHA256.Create())
            hash = sha256.ComputeHash(bytes);

        return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 8);
    }

    private static void AddTemplateReference(TemplateGenerator generator, Type type)
    {
        string location = type.Assembly.Location;

        if (!generator.Refs.Exists(x => string.Equals(x, location, StringComparison.OrdinalIgnoreCase)))
            generator.Refs.Add(location);
    }

    private static string FormatErrors(IEnumerable errors)
    {
        StringBuilder sb = new StringBuilder();
        foreach (object error in errors)
        {
            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(error);
        }

        return sb.ToString();
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        StringBuilder sb = new StringBuilder();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Severity != DiagnosticSeverity.Error)
                continue;

            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(diagnostic);
        }

        return sb.ToString();
    }
}