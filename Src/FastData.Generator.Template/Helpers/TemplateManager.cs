using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;

namespace Genbox.FastData.Generator.Template.Helpers;

/// <summary>Compiles, caches, and renders text templates.</summary>
public class TemplateManager
{
    private static readonly ConcurrentDictionary<string, Lazy<Type>> _compiledTypes = new ConcurrentDictionary<string, Lazy<Type>>(StringComparer.Ordinal);

    private readonly string _classNamespace;
    private readonly bool _release;

    /// <summary>Initializes a new instance of the <see cref="TemplateManager"/> class.</summary>
    /// <param name="language">The target language name used to isolate compiled template types.</param>
    /// <param name="release">Whether templates should be compiled with release optimizations.</param>
    public TemplateManager(string language, bool release)
    {
        _classNamespace = "Genbox.FastData.TemplateCache." + language;
        _release = release;
    }

    /// <summary>Compiles, caches, and renders a template.</summary>
    /// <param name="filePath">The path used to identify and preprocess the template.</param>
    /// <param name="source">The template source.</param>
    /// <param name="variables">The variables exposed to the template session.</param>
    /// <returns>The rendered template output.</returns>
    [SuppressMessage("Maintainability", "MA0016:Prefer using collection abstraction instead of implementation", Justification = "The concrete dictionary type is part of the established public template API.")]
    public string Render(string filePath, string source, Dictionary<string, object?> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables), "The template variables cannot be null.");

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

    [SuppressMessage("Performance", "MA0089:Use an overload with char instead of string", Justification = ".NET Standard 2.0 does not provide the char overload.")]
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

#if NET10_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026:Calling members annotated with RequiresUnreferencedCodeAttribute", Justification = "The method loads a template assembly emitted moments earlier and resolves its known generated type name.")]
#endif
    private Type CompileTemplate(TemplateCompilation template)
    {
        if (template.Generator.Errors.HasErrors)
            throw new InvalidOperationException($"Failed to preprocess template '{template.FilePath}':\n{FormatErrors(template.Generator.Errors)}");

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(template.Preprocessed, new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: CancellationToken.None);

        CSharpCompilation compilation = CSharpCompilation.Create(
            template.AssemblyName,
            [syntaxTree],
            template.ReferencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: _release ? OptimizationLevel.Release : OptimizationLevel.Debug));

        using MemoryStream assemblyStream = new MemoryStream();
        EmitResult emitResult = compilation.Emit(assemblyStream, cancellationToken: CancellationToken.None);

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

#if NET10_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2067:Unrecognized value passed to a parameter annotated with DynamicallyAccessedMembersAttribute", Justification = "Runtime-generated T4 types are deliberately activated and cannot be described statically to the trimmer.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Unrecognized value passed to a parameter annotated with DynamicallyAccessedMembersAttribute", Justification = "Runtime-generated T4 types expose a known T4 contract that is deliberately invoked through reflection.")]
#endif
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

        MethodInfo? initialize = templateType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);

        if (initialize == null)
            throw new InvalidOperationException($"'{typeName}' does not define initialize().");

        initialize.Invoke(instance, null);

        MethodInfo? transformText = templateType.GetMethod("TransformText", BindingFlags.Instance | BindingFlags.Public);

        if (transformText == null)
            throw new InvalidOperationException($"Template '{typeName}' does not define TransformText().");

        object? result = transformText.Invoke(instance, null);

        if (result == null)
            throw new InvalidOperationException("Failed to get output of TransformText()");

        string? str = result.ToString();

        if (str == null)
            throw new InvalidOperationException("TransformText() returned a value with no string representation.");

        return str.Trim();
    }

    private static void RemoveCompiledType(string assemblyName, Lazy<Type> lazyType)
    {
        ICollection<KeyValuePair<string, Lazy<Type>>> entries = _compiledTypes;
        entries.Remove(new KeyValuePair<string, Lazy<Type>>(assemblyName, lazyType));
    }

    private static string[] GetMetadataReferencePaths(TemplateGenerator generator, TemplateSettings settings, string[] references)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddReferencePaths(paths, references);
        AddReferencePaths(paths, generator.Refs);
        AddReferencePaths(paths, settings.Assemblies);

        // Add standard references for .NET
        string dotNetDir = RuntimeEnvironment.GetRuntimeDirectory();

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
#if NET10_0_OR_GREATER
        byte[] hash = SHA256.HashData(bytes);
#else
        byte[] hash;

        using (SHA256 sha256 = SHA256.Create())
            hash = sha256.ComputeHash(bytes);
#endif

        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

#if NET10_0_OR_GREATER
    [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Runtime template compilation requires physical metadata references for each participating assembly.")]
#endif
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

    private sealed class TemplateCompilation(TemplateGenerator generator, string[] referencePaths, string filePath, string preprocessed, string assemblyName, string typeName)
    {
        public TemplateGenerator Generator { get; } = generator;
        public string[] ReferencePaths { get; } = referencePaths;
        public string FilePath { get; } = filePath;
        public string Preprocessed { get; } = preprocessed;
        public string AssemblyName { get; } = assemblyName;
        public string TypeName { get; } = typeName;
    }
}