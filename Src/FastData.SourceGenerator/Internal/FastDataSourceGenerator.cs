using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;
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

                    if (obj is CombinedConfig combinedCfg)
                    {
                        if (!FastDataGenerator.TryGenerate(combinedCfg.Data, combinedCfg.FastDataConfig, new CSharpCodeGenerator(combinedCfg.CSharpGeneratorConfig), out string? source))
                        {
                            StructureType ds = combinedCfg.FastDataConfig.StructureType;

                            if (ds != StructureType.Auto)
                                throw new InvalidOperationException($"Failed to generate code with '{ds}'. Try setting DataStructure to Auto.");

                            throw new InvalidOperationException("Failed to generate code.");
                        }

                        spc.AddSource(combinedCfg.CSharpGeneratorConfig.ClassName + ".g.cs", SourceText.From(source, Encoding.UTF8));
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

            if (!Enum.TryParse<DataType>(genericArg.Name, out _))
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

            FastDataConfig config = new FastDataConfig();
            BindValue(() => config.StructureType, ad.NamedArguments);
            BindValue(() => config.StorageOptions, ad.NamedArguments);

            CSharpGeneratorConfig config2 = new CSharpGeneratorConfig(name);
            BindValue(() => config2.Namespace, ad.NamedArguments);
            BindValue(() => config2.ClassVisibility, ad.NamedArguments);
            BindValue(() => config2.ClassType, ad.NamedArguments);

            yield return new CombinedConfig(data, config, config2);
        }
    }

    private static void BindValue<T>(Expression<Func<T?>> property, ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs)
    {
        MemberExpression memberExpr = (MemberExpression)property.Body;
        MemberExpression? fieldInfo = memberExpr.Expression as MemberExpression;
        ConstantExpression? constExpr = fieldInfo?.Expression as ConstantExpression;
        PropertyInfo prop = (PropertyInfo)memberExpr.Member;

        foreach (KeyValuePair<string, TypedConstant> pair in namedArgs)
        {
            if (pair.Key == prop.Name)
            {
                if (pair.Value.Kind == TypedConstantKind.Error)
                    throw new InvalidOperationException($"Unable to map value on '{prop.Name}' due to invalid value");

                object target = fieldInfo?.Member is FieldInfo fi ? fi.GetValue(constExpr?.Value) : throw new InvalidOperationException("Cannot find target for: " + prop.Name);

                prop.SetValue(target, pair.Value.Value);
                break;
            }
        }
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