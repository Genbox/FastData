using System.Collections.Immutable;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Generators;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Optimization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Genbox.FastData.Internal;

[Generator]
internal class FastDataGenerator : IIncrementalGenerator
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
                StringBuilder sb = new StringBuilder();

                foreach (object obj in specs)
                {
                    if (obj is Exception ex)
                        throw ex;

                    if (obj is FastDataSpec spec)
                    {
                        sb.Clear();
                        AppendHeader(sb, spec);
                        AppendDataStructure(sb, spec);
                        AppendFooter(sb, spec);

                        spc.AddSource(spec.Name + ".g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
                    }
                }
            }
            catch (Exception e)
            {
                spc.ReportDiagnostic(Diagnostic.Create(_generationError, null, e.Message));
            }
        });
    }

    private static void AppendDataStructure(StringBuilder sb, FastDataSpec spec)
    {
        IEnumerable<IEarlyExitSpec> early = [];

        //No matter the StorageMode, if there is only a single item, we will use the same data structure
        if (spec.Data.Length == 1)
        {
            Generate(DataStructure.SingleValue, sb, spec, early);
            return;
        }

        // If we know the data type, we might be able to get specialized early exits
        if (spec.KnownDataType == KnownDataType.String)
        {
            StringProperties props = Analyzer.GetStringProperties(spec.Data.Cast<string>());
            early = Optimizer.GetEarlyExits(props);
        }
        else if (spec.KnownDataType == KnownDataType.Int32)
        {
            IntegerProperties props = Analyzer.GetIntegerProperties(spec.Data.Cast<int>());
            early = Optimizer.GetEarlyExits(props);
        }

        switch (spec.StorageMode)
        {
            case StorageMode.Auto:

                // If the type is array, we prefer to use lenght indexes if possible
                if (spec.IsArray)
                {
                    ArrayProperties props = Analyzer.GetArrayProperties(spec.Data.Cast<Array>());

                    // We use both the early exits from arrays (if any) and the ones from the known data types.
                    // The array ones should come first as they are more specific. TODO: correct?
                    IEnumerable<IEarlyExitSpec> combined = Optimizer.GetEarlyExits(props).Concat(early);

                    //TODO: Calculate fragmentation factor. If it is too high, and user wants to optimize for memory, use a slower data structure

                    if (props.NumLengths == spec.Data.Length) //If the lengths are all unique, we use a special data structure
                        Generate(DataStructure.UniqueKeyLength, sb, spec, combined);
                    else
                        Generate(DataStructure.KeyLength, sb, spec, combined);
                }
                else
                {
                    //TODO: Add support for length indexed structures for strings

                    // For small amounts of data, switch is the fastest
                    if (spec.Data.Length <= 64)
                        Generate(DataStructure.Switch, sb, spec, early);
                    else // more than 64 items
                    {
                        if (spec.StorageOptions.HasFlag(StorageOption.OptimizeForMemory) && spec.StorageOptions.HasFlag(StorageOption.OptimizeForSpeed))
                        {
                            //TODO: Minimal perfect hash
                            //TODO: Set timeout for minimal perfect hash according to aggressiveness
                            break;
                        }

                        if (spec.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                        {
                            Generate(DataStructure.EytzingerSearch, sb, spec, early);
                            break;
                        }

                        Generate(DataStructure.HashSet, sb, spec, early);
                    }
                }

                break;
            case StorageMode.Linear:
                Generate(DataStructure.Array, sb, spec, early);
                break;
            case StorageMode.Logic:
                Generate(DataStructure.Switch, sb, spec, early);
                break;
            case StorageMode.Tree:
                if (spec.StorageOptions.HasFlag(StorageOption.AggressiveOptimization))
                    Generate(DataStructure.EytzingerSearch, sb, spec, early);
                else
                    Generate(DataStructure.BinarySearch, sb, spec, early);
                break;
            case StorageMode.Indexed:
                Generate(DataStructure.HashSet, sb, spec, early);
                break;

            default:
                throw new InvalidOperationException($"Unsupported StorageMode {spec.StorageMode}");
        }
    }

    /// <summary>
    /// This method is used by tests as well
    /// </summary>
    internal static void Generate(DataStructure ds, StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        switch (ds)
        {
            case DataStructure.Array:
                ArrayCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.BinarySearch:
                BinarySearchCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.EytzingerSearch:
                EytzingerSearchCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.Switch:
                SwitchCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.SwitchHashCode:
                SwitchHashCode.Generate(sb, spec);
                break;
            case DataStructure.MinimalPerfectHash:
                MinimalPerfectHashCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.HashSet:
                HashSetCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.UniqueKeyLength:
                UniqueKeyLengthCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.UniqueKeyLengthSwitch:
                UniqueKeyLengthSwitchCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.KeyLength:
                KeyLengthCode.Generate(sb, spec, earlyExitSpecs);
                break;
            case DataStructure.SingleValue:
                SingleValueCode.Generate(sb, spec);
                break;
            case DataStructure.Conditional:
                ConditionalCode.Generate(sb, spec, earlyExitSpecs);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ds), ds, null);
        }
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

            if (!IsSupported(genericArg))
                throw new InvalidOperationException($"FastData does not support '{genericArg.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' as generic argument for '{name}'");

            //We uniq the values and throw on duplicates
            HashSet<object> uniqueValues = new HashSet<object>();

            foreach (TypedConstant value in ctorArg1.Values)
            {
                if (value.Value == null)
                    continue;

                if (!uniqueValues.Add(value.Value))
                    throw new InvalidOperationException($"Duplicate value: {value.Value}");
            }

            FastDataSpec spec = TypeHelper.MapData<FastDataSpec>(ad.NamedArguments);

            //Overwrite the values with the unique version
            spec.Name = name;
            spec.Data = ctorArg1.Values.Select(x => x.Value).ToArray()!;
            spec.IsArray = ctorArg1.Values[0].Type!.TypeKind == TypeKind.Array;

            spec.DataTypeName = genericArg.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            //If we know the type, set it. Otherwise, it defaults to unknown.
            if (!Enums.Enums.KnownDataType.TryParse(genericArg.Name, out KnownDataType dataType))
                spec.KnownDataType = dataType;

            yield return spec;
        }
    }

    private static bool IsSupported(ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Boolean => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Char => true,
            SpecialType.System_String => true,
            _ => false
        };
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

    private static void AppendHeader(StringBuilder sb, FastDataSpec spec)
    {
        string? ns = spec.Namespace != null ? $"namespace {spec.Namespace};\n" : null;
        string cn = spec.Name;
        string visibility = spec.ClassVisibility.ToString().ToLowerInvariant();

        string type = spec.ClassType switch
        {
            ClassType.Static => " static class",
            ClassType.Instance => " class",
            ClassType.Struct => " struct",
            _ => throw new InvalidOperationException("Invalid type: " + spec.ClassType)
        };

        // = spec.Mode.HasFlag(StorageFlags.Struct) ? "struct" : "class";
        string? attr = spec.ClassType == ClassType.Struct ? "[StructLayout(LayoutKind.Auto)]" : null;
        string? iface = spec.ClassType != ClassType.Static ? " : IFastSet" : null;
        string? partial = spec.ClassType != ClassType.Static ? " partial" : null;

        // AssemblyName name = typeof(FastDataGenerator).Assembly.GetName();
        sb.AppendLine("// <auto-generated />");

#if RELEASE
        System.Reflection.AssemblyName name = typeof(FastDataGenerator).Assembly.GetName();
        sb.Append("// Generated by ").Append(name.Name).Append(' ').AppendLine(name.Version.ToString());
        sb.Append("// Generated on: ").AppendFormat(System.Globalization.DateTimeFormatInfo.InvariantInfo, "{0:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow).AppendLine(" UTC");
#endif

        sb.Append($$"""
                    #nullable enable
                    using Genbox.FastData;
                    using Genbox.FastData.Abstracts;
                    using Genbox.FastData.Helpers;
                    using System.Runtime.InteropServices;
                    using System.Runtime.CompilerServices;
                    using System.Text;
                    using System;

                    {{ns}}
                    {{attr}}{{visibility}}{{partial}}{{type}} {{cn}} {{iface}}
                    {

                    """);
    }

    private static void AppendFooter(StringBuilder sb, FastDataSpec spec)
    {
        sb.Append($"""


                       public const int ItemCount = {spec.Data.Length};
                   """);

        if (spec.ClassType == ClassType.Instance)
        {
            sb.Append($"""

                           public int Length => {spec.Data.Length};
                       """);
        }

        sb.AppendLine();
        sb.Append('}');
    }
}