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
    private static readonly string FastDataAttr = typeof(FastDataAttribute<>).FullName!;
    private static readonly string FastDataKeyValueAttr = typeof(FastDataKeyValueAttribute<,>).FullName!;

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
                        Array keys = combinedCfg.Keys;
                        Array? values = combinedCfg.Values;
                        FastDataConfig fdCfg = combinedCfg.FDConfig;
                        CSharpCodeGenerator generator = CSharpCodeGenerator.Create(combinedCfg.CSConfig);

                        Type genType = typeof(FastDataGenerator);

                        string source;
                        if (values == null)
                        {
                            MethodInfo mi = genType.GetMethod(nameof(FastDataGenerator.Generate), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                                                   .MakeGenericMethod(keys.GetValue(0).GetType());
                            source = (string)mi.Invoke(null, [keys, fdCfg, generator, null])!;
                        }
                        else
                        {
                            MethodInfo mi = genType.GetMethod(nameof(FastDataGenerator.GenerateKeyed), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                                                   .MakeGenericMethod(keys.GetValue(0).GetType(), values.GetValue(0).GetType());
                            source = (string)mi.Invoke(null, [keys, values, fdCfg, generator, null])!;
                        }

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

    private static IEnumerable<object> Transform(Compilation c, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            yield break;

        ISymbol? fdAttr = c.GetTypeByMetadataName(FastDataAttr);
        ISymbol? fdKvAttr = c.GetTypeByMetadataName(FastDataKeyValueAttr);

        if (fdAttr == null && fdKvAttr == null)
            yield break;

        ImmutableArray<AttributeData> ads = c.Assembly.GetAttributes();

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

        foreach (AttributeData ad in ads)
        {
            if (ad.AttributeClass == null)
                continue;

            //If it is not one of our attributes, skip it.
            if (!AreEqualSymbols(ad.AttributeClass, fdAttr) && !AreEqualSymbols(ad.AttributeClass, fdKvAttr))
                continue;

            if (ad.ConstructorArguments.Length is not (2 or 3))
                throw new InvalidOperationException("Expected 2 constructor arguments");

            TypedConstant nameType = ad.ConstructorArguments[0];

            string? name = (string?)nameType.Value;

            if (name == null || name.Length == 0)
                throw new InvalidOperationException("Name is null or empty");

            if (!names.Add(name))
                throw new InvalidOperationException($"The name '{name}' is duplicated elsewhere");

            ImmutableArray<TypedConstant> keys = ad.ConstructorArguments[1].Values;

            if (keys.Length == 0)
                throw new InvalidOperationException($"There are no keys in '{name}'");

            ITypeSymbol genericArg0 = ad.AttributeClass.TypeArguments[0];

            if (!Enum.TryParse<KeyType>(genericArg0.Name, true, out _))
                throw new InvalidOperationException($"FastData does not support '{genericArg0.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' as generic argument for '{name}'");

            //We uniq the keys and throw on duplicates
            HashSet<object> uniqueKeys = new HashSet<object>();

            foreach (TypedConstant value in keys)
            {
                if (value.Value == null)
                    throw new InvalidOperationException("Null value in dataset");

                if (value.Value is string str && str.Length == 0)
                    throw new InvalidOperationException("Empty string values are not supported");

                if (!uniqueKeys.Add(value.Value))
                    throw new InvalidOperationException($"Duplicate value: {value.Value}");
            }

            //Copy out the values to avoid hanging on to Roslyn references later on
            Array keysArr = Array.CreateInstance(ToRuntimeType(genericArg0), keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                keysArr.SetValue(keys[i].Value ?? throw new InvalidOperationException("Null key in dataset"), i);
            }

            Array? valueArr = null;

            if (ad.ConstructorArguments.Length == 3) //Lazy check for key/value attribute
            {
                ImmutableArray<TypedConstant> values = ad.ConstructorArguments[2].Values;
                ITypeSymbol genericArg1 = ad.AttributeClass.TypeArguments[1];
                valueArr = Array.CreateInstance(ToRuntimeType(genericArg1), values.Length);

                for (int i = 0; i < valueArr.Length; i++)
                {
                    valueArr.SetValue(values[i].Value ?? throw new InvalidOperationException("Null value in dataset"), i);
                }
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

            yield return new CombinedConfig(keysArr, valueArr, fdCfg, csCfg);
        }
    }

    public static Type? ToRuntimeType(ITypeSymbol symbol)
    {
        // handle primitives & string
        if (symbol.SpecialType != SpecialType.None)
        {
            return symbol.SpecialType switch
            {
                SpecialType.System_Boolean => typeof(bool),
                SpecialType.System_Char => typeof(char),
                SpecialType.System_SByte => typeof(sbyte),
                SpecialType.System_Byte => typeof(byte),
                SpecialType.System_Int16 => typeof(short),
                SpecialType.System_UInt16 => typeof(ushort),
                SpecialType.System_Int32 => typeof(int),
                SpecialType.System_UInt32 => typeof(uint),
                SpecialType.System_Int64 => typeof(long),
                SpecialType.System_UInt64 => typeof(ulong),
                SpecialType.System_Single => typeof(float),
                SpecialType.System_Double => typeof(double),
                SpecialType.System_String => typeof(string),
                SpecialType.System_Object => typeof(object),
                _ => null
            };
        }

        // FQN, remove "global::"
        string metadataName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                    .Replace("global::", "");

        // fallback: any loaded assembly
        return AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(metadataName, false))
                        .FirstOrDefault(t => t != null);
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