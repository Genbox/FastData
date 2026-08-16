using System.Collections;
using System.Collections.Concurrent;
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

/// <summary>Used for handling and rendering templates</summary>
public class TemplateManager
{
    private static readonly ConcurrentDictionary<string, Lazy<Type>> _compiledTypes = new ConcurrentDictionary<string, Lazy<Type>>(StringComparer.Ordinal);

    private readonly string _classNamespace;
    private readonly bool _release;

    public TemplateManager(string language, bool release)
    {
        _classNamespace = "Genbox.FastData.TemplateCache." + language;
        _release = release;
    }

    public string Render(string filePath, string source, Dictionary<string, object?> variables)
    {
        TemplateCompilation template = PrepareTemplate(filePath, source, variables);
        Lazy<Type> lazyType = GetCompiledTemplateType(template);
        Type templateType;

        try
        {
            templateType = lazyType.Value;
        }
        catch
        {
            RemoveCompiledType(template.AssemblyName, lazyType);
            throw;
        }

        return ExecuteCompiledTemplate(templateType, variables);
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

    private TemplateCompilation PrepareTemplate(string filePath, string source, Dictionary<string, object?> variables)
    {
        string className = Path.GetFileNameWithoutExtension(filePath);
        TemplateGenerator generator = CreateGenerator(variables);
        ParsedTemplate parsed = generator.ParseTemplate(filePath, source);

        TemplateSettings settings = TemplatingEngine.GetSettings(generator, parsed);
        settings.Culture = CultureInfo.InvariantCulture;
        settings.Debug = !_release;
        settings.Encoding = Encoding.UTF8;
        settings.Name = className;
        settings.Namespace = _classNamespace;

        string preprocessed = generator.PreprocessTemplate(parsed, filePath, source, settings, out string[] references);
        string[] referencePaths = GetMetadataReferencePaths(generator, settings, references);
        string configuration = _release ? "Release\n" : "Debug\n";
        string assemblyName = className + "." + GetHash(configuration + preprocessed + "\n" + string.Join("\n", referencePaths));

        return new TemplateCompilation(generator, referencePaths, filePath, preprocessed, assemblyName, _classNamespace + "." + className);
    }

    private Type CompileTemplate(TemplateCompilation template)
    {
        if (template.Generator.Errors.HasErrors)
            throw new InvalidOperationException($"Failed to preprocess template '{template.FilePath}':\n{FormatErrors(template.Generator.Errors)}");

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(template.Preprocessed, new CSharpParseOptions(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            template.AssemblyName,
            [syntaxTree],
            template.ReferencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: _release ? OptimizationLevel.Release : OptimizationLevel.Debug));

        using MemoryStream assemblyStream = new MemoryStream();
        EmitResult emitResult = compilation.Emit(assemblyStream);

        if (!emitResult.Success)
            throw new InvalidOperationException($"Failed to compile template '{template.FilePath}':\n{FormatDiagnostics(emitResult.Diagnostics)}");

        Assembly assembly = Assembly.Load(assemblyStream.ToArray());
        return assembly.GetType(template.TypeName, true)!;
    }

    private Lazy<Type> GetCompiledTemplateType(TemplateCompilation template)
    {
        Lazy<Type> candidate = new Lazy<Type>(() => CompileTemplate(template), LazyThreadSafetyMode.ExecutionAndPublication);
        return _compiledTypes.GetOrAdd(template.AssemblyName, candidate);
    }

    private static string ExecuteCompiledTemplate(Type templateType, Dictionary<string, object?> variables)
    {
        string typeName = templateType.FullName ?? templateType.Name;
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

        return str!.Trim();
    }

    private static void RemoveCompiledType(string assemblyName, Lazy<Type> lazyType)
    {
        ICollection<KeyValuePair<string, Lazy<Type>>> entries = _compiledTypes;
        entries.Remove(new KeyValuePair<string, Lazy<Type>>(assemblyName, lazyType));
    }

    private sealed class TemplateCompilation(TemplateGenerator generator, string[] referencePaths, string filePath, string preprocessed, string assemblyName, string typeName)
    {
        public TemplateGenerator Generator { get; } = generator;
        public string[] ReferencePaths { get; } = referencePaths;
        public string FilePath { get; } = filePath;
        public string Preprocessed { get; } = preprocessed;
        public string AssemblyName { get; } = assemblyName;
        public string TypeName { get; } = typeName;
    }

    private static string[] GetMetadataReferencePaths(TemplateGenerator generator, TemplateSettings settings, string[] references)
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

        string[] sortedPaths = paths.ToArray();
        Array.Sort(sortedPaths, StringComparer.OrdinalIgnoreCase);
        return sortedPaths;
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

    private static string GetHash(string source)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        byte[] hash;

        using (SHA256 sha256 = SHA256.Create())
            hash = sha256.ComputeHash(bytes);

        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
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