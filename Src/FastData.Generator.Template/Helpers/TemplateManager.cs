using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Generator.Framework;
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

    public string Render<TKey>(OutputWriter<TKey> writer, string name, string source, Dictionary<string, object?> variables)
    {
        string className = Path.GetFileNameWithoutExtension(name);
        string shortHash = GetShortHash(source);
        string assemblyPath = Path.Combine(_cacheDirectory, className + "." + shortHash + ".dll");

        if (!File.Exists(assemblyPath))
        {
            TemplateGenerator generator = CreateGenerator(variables);
            CompileTemplateAssembly(generator, name, source, className, assemblyPath);
        }

        return ExecuteCompiledTemplate(writer, className, assemblyPath, variables);
    }

    private static TemplateGenerator CreateGenerator(Dictionary<string, object?> variables)
    {
        TemplateGenerator generator = new TemplateGenerator();
        AddTemplateReference(generator, typeof(CommonDataModel));
        AddTemplateReference(generator, typeof(TypeCode));
        AddTemplateReference(generator, typeof(FormatHelper));

        foreach (KeyValuePair<string, object?> pair in variables)
        {
            if (pair.Value == null)
                continue;

            AddTemplateReference(generator, pair.Value.GetType());
        }

        return generator;
    }

    private void CompileTemplateAssembly(TemplateGenerator generator, string name, string source, string className, string assemblyPath)
    {
        ParsedTemplate parsed = generator.ParseTemplate(name, source);

        TemplateSettings settings = TemplatingEngine.GetSettings(generator, parsed);
        settings.Culture = CultureInfo.InvariantCulture;
        settings.Debug = !_release;
        settings.Encoding = Encoding.UTF8;
        settings.Name = className;
        settings.Namespace = _classNamespace;

        string preprocessed = generator.PreprocessTemplate(parsed, name, source, settings, out string[] references);

        if (generator.Errors.HasErrors)
            throw new InvalidOperationException($"Failed to preprocess template '{name}':\n{FormatErrors(generator.Errors)}");

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(preprocessed, new CSharpParseOptions(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(assemblyPath),
            [syntaxTree],
            GetMetadataReferences(generator, settings, references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: _release ? OptimizationLevel.Release : OptimizationLevel.Debug));

        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        EmitResult emitResult = compilation.Emit(assemblyPath, pdbPath);

        if (!emitResult.Success)
            throw new InvalidOperationException($"Failed to compile template '{name}':\n{FormatDiagnostics(emitResult.Diagnostics)}");
    }

    private string ExecuteCompiledTemplate<TKey>(OutputWriter<TKey> writer, string className, string assemblyPath, Dictionary<string, object?> variables)
    {
        Assembly assembly = Assembly.Load(File.ReadAllBytes(assemblyPath));
        string typeName = _classNamespace + "." + className;
        Type templateType = assembly.GetType(typeName, true);
        object instance = Activator.CreateInstance(templateType) ?? throw new InvalidOperationException($"Failed to create template instance for '{typeName}'.");

        TextTemplatingSession session = new TextTemplatingSession();
        session["Common"] = new CommonDataModel
        {
            InputKeyName = writer.InputKeyName,
            LookupKeyName = writer.LookupKeyName,
            ArraySizeType = writer.ArraySizeType,
            HashSizeType = writer.HashSizeType
        };

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

        return result.ToString();
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences(TemplateGenerator generator, TemplateSettings settings, string[] references)
    {
        HashSet<string> referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddReferencePaths(referencePaths, references);
        AddReferencePaths(referencePaths, generator.Refs);
        AddReferencePaths(referencePaths, settings.Assemblies);

        return referencePaths.Select(r => MetadataReference.CreateFromFile(r));
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
            if (diagnostic.Severity != DiagnosticSeverity.Error && diagnostic.Severity != DiagnosticSeverity.Warning)
                continue;

            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(diagnostic);
        }

        return sb.ToString();
    }
}