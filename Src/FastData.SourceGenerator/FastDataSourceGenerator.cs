using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.SourceGenerator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Genbox.FastData.SourceGenerator;

[Generator]
[SuppressMessage("ReSharper", "ReplaceWithStringIsNullOrEmpty")]
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
                        object[] data = combinedCfg.Data;
                        FastDataConfig fdCfg = combinedCfg.FDConfig;
                        CSharpCodeGenerator generator = CSharpCodeGenerator.Create(combinedCfg.CSConfig);

                        string source = data[0] switch
                        {
                            char => FastDataGenerator.Generate(Cast<char>(data), fdCfg, generator),
                            sbyte => FastDataGenerator.Generate(Cast<sbyte>(data), fdCfg, generator),
                            byte => FastDataGenerator.Generate(Cast<byte>(data), fdCfg, generator),
                            short => FastDataGenerator.Generate(Cast<short>(data), fdCfg, generator),
                            ushort => FastDataGenerator.Generate(Cast<ushort>(data), fdCfg, generator),
                            int => FastDataGenerator.Generate(Cast<int>(data), fdCfg, generator),
                            uint => FastDataGenerator.Generate(Cast<uint>(data), fdCfg, generator),
                            long => FastDataGenerator.Generate(Cast<long>(data), fdCfg, generator),
                            ulong => FastDataGenerator.Generate(Cast<ulong>(data), fdCfg, generator),
                            float => FastDataGenerator.Generate(Cast<float>(data), fdCfg, generator),
                            double => FastDataGenerator.Generate(Cast<double>(data), fdCfg, generator),
                            string => FastDataGenerator.Generate(Cast<string>(data), fdCfg, generator),
                            _ => throw new InvalidOperationException($"Unsupported data type: {data[0].GetType().Name}")
                        };

                        spc.AddSource(combinedCfg.CSConfig.ClassName + ".g.cs", SourceText.From(source, Encoding.UTF8));
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

    private static T[] Cast<T>(object[] data)
    {
        T[] newArr = new T[data.Length];

        for (int i = 0; i < data.Length; i++)
            newArr[i] = (T)data[i];

        return newArr;
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

            FastDataConfig fdCfg = new FastDataConfig();
            BindValue(() => fdCfg.StructureType, ad.NamedArguments);

            CSharpCodeGeneratorConfig csCfg = new CSharpCodeGeneratorConfig(name);
            BindValue(() => csCfg.Namespace, ad.NamedArguments);
            BindValue(() => csCfg.ClassVisibility, ad.NamedArguments);
            BindValue(() => csCfg.ClassType, ad.NamedArguments);

            //We need logic for analysis level
            object? alArg = ad.NamedArguments.FirstOrDefault(x => x.Key == nameof(FastDataAttribute<int>.AnalysisLevel)).Value.Value;
            if (alArg != null)
            {
                AnalysisLevel al = (AnalysisLevel)alArg;

                fdCfg.StringAnalyzerConfig = al switch
                {
                    AnalysisLevel.Disabled => null,
                    AnalysisLevel.Fast => new StringAnalyzerConfig
                    {
                        BruteForceAnalyzerConfig = new BruteForceAnalyzerConfig
                        {
                            MaxAttempts = 1000
                        },
                        GeneticAnalyzerConfig = new GeneticAnalyzerConfig
                        {
                            PopulationSize = 16,
                            MaxGenerations = 8
                        },
                        GPerfAnalyzerConfig = new GPerfAnalyzerConfig
                        {
                            MaxPositions = 64
                        }
                    },
                    AnalysisLevel.Balanced => new StringAnalyzerConfig(),
                    AnalysisLevel.Aggressive => new StringAnalyzerConfig
                    {
                        BruteForceAnalyzerConfig = new BruteForceAnalyzerConfig
                        {
                            MaxAttempts = 157_464
                        },
                        GeneticAnalyzerConfig = new GeneticAnalyzerConfig
                        {
                            PopulationSize = 64,
                            MaxGenerations = 100
                        },
                        GPerfAnalyzerConfig = new GPerfAnalyzerConfig
                        {
                            MaxPositions = 1024
                        }
                    },
                    _ => throw new ArgumentOutOfRangeException("Unsupported AnalysisLevel: " + al)
                };
            }

            yield return new CombinedConfig(data, fdCfg, csCfg);
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