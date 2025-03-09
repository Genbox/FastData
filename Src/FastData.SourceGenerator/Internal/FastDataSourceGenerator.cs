using System.Collections.Immutable;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Genbox.FastData.SourceGenerator.Internal;

[Generator]
internal class FastDataSourceGenerator : IIncrementalGenerator
{
    private static readonly string FastLookupAttr = typeof(FastDataAttribute<>).FullName!;

    private static readonly SymbolDisplayFormat Format = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private readonly DiagnosticDescriptor _generationError = new DiagnosticDescriptor("FD001", "Exception while generating code", "{0}", "TODO", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<object>> p = context.CompilationProvider
                                                                    .SelectMany((comp, token) =>
                                                                    {
                                                                        try
                                                                        {
                                                                            return Transform(comp, token).ToArray();
                                                                        }
                                                                        catch (Exception e)
                                                                        {
                                                                            return [e];
                                                                        }
                                                                    })
                                                                    .Collect();

        context.RegisterSourceOutput(p, (spc, specs) =>
        {
            if (spc.CancellationToken.IsCancellationRequested)
                return;

            try
            {
                foreach (object obj in specs)
                {
                    if (obj is Exception ex)
                        throw ex;

                    if (obj is CombinedConfig combinedConfig)
                    {
                        string source = FastDataGenerator.Generate(combinedConfig.FastDataConfig, new CSharpCodeGenerator(combinedConfig.CSharpGeneratorConfig));
                        spc.AddSource(combinedConfig.FastDataConfig.Name + ".g.cs", SourceText.From(source, Encoding.UTF8));
                    }
                    else
                        throw new InvalidOperationException("Unknown object type: " + obj.GetType().Name);
                }
            }
            catch (Exception e)
            {
                spc.ReportDiagnostic(Diagnostic.Create(_generationError, null, e));
            }
        });
    }

    private static IEnumerable<object> Transform(Compilation c, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            yield break;

        ISymbol? symbol = c.GetTypeByMetadataName(FastLookupAttr);

        if (symbol == null)
            yield break;

        ImmutableArray<AttributeData> ads = c.Assembly.GetAttributes();

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

        foreach (AttributeData ad in ads)
        {
            if (ad.AttributeClass == null)
                continue;

            //If it is not one of our Attributes, skip it.
            if (!AreEqualSymbols(ad.AttributeClass, symbol))
                continue;

            if (ad.ConstructorArguments.Length != 2)
                throw new InvalidOperationException("Expected 2 constructor arguments");

            TypedConstant nameType = ad.ConstructorArguments[0];

            string? name = (string?)nameType.Value;

            if (name == null || name.Length == 0)
                throw new InvalidOperationException("Name is null or empty");

            if (!names.Add(name))
                throw new InvalidOperationException($"The name '{name}' is duplicated elsewhere");

            TypedConstant ctorArg1 = ad.ConstructorArguments[1];

            if (ctorArg1.Values.Length == 0)
                throw new InvalidOperationException($"There are no values in '{name}'");

            ITypeSymbol genericArg = ad.AttributeClass.TypeArguments[0];

            if (!Enum.TryParse<KnownDataType>(genericArg.Name, out _))
                throw new InvalidOperationException($"FastData does not support '{genericArg.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' as generic argument for '{name}'");

            //We uniq the values and throw on duplicates
            HashSet<object> uniqueValues = new HashSet<object>();

            foreach (TypedConstant value in ctorArg1.Values)
            {
                if (value.Value == null)
                    throw new InvalidOperationException("Null value in dataset");

                if (value.Value is string str && str.Length == 0)
                    throw new InvalidOperationException("Empty string values are not supported");

                if (!uniqueValues.Add(value.Value))
                    throw new InvalidOperationException($"Duplicate value: {value.Value}");
            }

            object[] data = new object[ctorArg1.Values.Length];

            for (int i = 0; i < data.Length; i++)
            {
                object? value = ctorArg1.Values[i].Value;

                if (value == null)
                    continue;

                data[i] = value;
            }

            FastDataConfig config = new FastDataConfig(name, data);
            config.StorageMode = GetValueOrDefault<StorageMode>(nameof(config.StorageMode), ad.NamedArguments);
            config.StorageOptions = GetValueOrDefault<StorageOption>(nameof(config.StorageOptions), ad.NamedArguments);

            CSharpGeneratorConfig config2 = new CSharpGeneratorConfig();
            config2.Namespace = GetValueOrDefault<string>(nameof(config2.Namespace), ad.NamedArguments);
            config2.ClassVisibility = GetValueOrDefault<ClassVisibility>(nameof(config2.ClassVisibility), ad.NamedArguments);
            config2.ClassType = GetValueOrDefault<ClassType>(nameof(config2.ClassType), ad.NamedArguments);

            yield return new CombinedConfig(config, config2);
        }
    }

    private static T? GetValueOrDefault<T>(string name, ImmutableArray<KeyValuePair<string, TypedConstant>> args)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in args)
        {
            if (pair.Key == name)
            {
                if (pair.Value.Value == null)
                    throw new InvalidOperationException($"Unable to read {name}");

                return (T?)pair.Value.Value;
            }
        }

        return default;
    }

    private static bool AreEqualSymbols(ISymbol a, ISymbol b)
    {
        ImmutableArray<SymbolDisplayPart> aParts = a.ToDisplayParts(Format);
        ImmutableArray<SymbolDisplayPart> bParts = b.ToDisplayParts(Format);

        if (aParts.Length != bParts.Length)
            return false;

        for (int i = 0; i < aParts.Length; i++)
        {
            SymbolDisplayPart aPart = aParts[i];
            SymbolDisplayPart bPart = bParts[i];

            if (aPart.Kind != bPart.Kind)
                return false;

            if (aPart.Symbol?.Name != bPart.Symbol?.Name)
                return false;
        }

        return true;
    }
}