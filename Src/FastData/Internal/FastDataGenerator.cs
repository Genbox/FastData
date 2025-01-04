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
        IEarlyExit[] early = [];

        //No matter the StorageMode, if there is only a single item, we will use the same data structure
        if (spec.Data.Length == 1)
        {
            Generate(DataStructure.SingleValue, sb, spec, early);
            return;
        }

        DataProperties dataProps = new DataProperties();

        switch (spec.KnownDataType)
        {
            case KnownDataType.SByte:
                dataProps.IntProps = Analyzer.GetIntegerProperties(spec.Data.Cast<sbyte>());
                break;
            case KnownDataType.Int16:
                dataProps.IntProps = Analyzer.GetIntegerProperties(spec.Data.Cast<short>());
                break;
            case KnownDataType.Int32:
                dataProps.IntProps = Analyzer.GetIntegerProperties(spec.Data.Cast<int>());
                break;
            case KnownDataType.Int64:
                dataProps.IntProps = Analyzer.GetIntegerProperties(spec.Data.Cast<long>());
                break;
            case KnownDataType.Byte:
                dataProps.UIntProps = Analyzer.GetUnsignedIntegerProperties(spec.Data.Cast<byte>());
                break;
            case KnownDataType.UInt16:
                dataProps.UIntProps = Analyzer.GetUnsignedIntegerProperties(spec.Data.Cast<ushort>());
                break;
            case KnownDataType.UInt32:
                dataProps.UIntProps = Analyzer.GetUnsignedIntegerProperties(spec.Data.Cast<uint>());
                break;
            case KnownDataType.UInt64:
                dataProps.UIntProps = Analyzer.GetUnsignedIntegerProperties(spec.Data.Cast<ulong>());
                break;
            case KnownDataType.String:
                dataProps.StringProps = Analyzer.GetStringProperties(spec.Data.Cast<string>());
                break;
            case KnownDataType.Boolean:
                break;
            case KnownDataType.Char:
                dataProps.CharProps = Analyzer.GetCharProperties(spec.Data.Cast<char>());
                break;
            case KnownDataType.Single:
                dataProps.FloatProps = Analyzer.GetFloatProperties(spec.Data.Cast<float>());
                break;
            case KnownDataType.Double:
                dataProps.FloatProps = Analyzer.GetFloatProperties(spec.Data.Cast<double>());
                break;
            case KnownDataType.Unknown:
                //Do nothing
                break;
            default:
                throw new InvalidOperationException("Unknown data type: " + spec.KnownDataType);
        }

        if (dataProps.StringProps.HasValue)
            early = Optimizer.GetEarlyExits(dataProps.StringProps.Value).ToArray();
        else if (dataProps.IntProps.HasValue)
            early = Optimizer.GetEarlyExits(dataProps.IntProps.Value).ToArray();
        else if (dataProps.UIntProps.HasValue)
            early = Optimizer.GetEarlyExits(dataProps.UIntProps.Value).ToArray();
        else if (dataProps.CharProps.HasValue)
            early = Optimizer.GetEarlyExits(dataProps.CharProps.Value).ToArray();
        else if (dataProps.FloatProps.HasValue)
            early = Optimizer.GetEarlyExits(dataProps.FloatProps.Value).ToArray();

        foreach (ICode candidate in GetDataStructureCandidates(spec))
        {
            if (candidate.IsAppropriate(dataProps) && candidate.TryPrepare())
            {
                sb.Append(candidate.Generate(early));
                break;
            }
        }
    }

    private static IEnumerable<ICode> GetDataStructureCandidates(FastDataSpec spec)
    {
        switch (spec.StorageMode)
        {
            case StorageMode.Auto:

                // For small amounts of data, logic is the fastest, so we try that first
                yield return new SwitchCode(spec);

                // We try (unique) key lengths
                yield return new UniqueKeyLengthCode(spec);
                yield return new KeyLengthCode(spec);

                if (spec.StorageOptions.HasFlag(StorageOption.OptimizeForMemory) && spec.StorageOptions.HasFlag(StorageOption.OptimizeForSpeed))
                    yield return new MinimalPerfectHashCode(spec);

                if (spec.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                    yield return new BinarySearchCode(spec);
                else
                    yield return new HashSetCode(spec);

                break;
            case StorageMode.Linear:
                yield return new ArrayCode(spec);
                break;
            case StorageMode.Logic:
                yield return new SwitchCode(spec);
                break;
            case StorageMode.Tree:
                yield return new BinarySearchCode(spec);
                break;
            case StorageMode.Indexed:
                yield return new HashSetCode(spec);
                break;

            default:
                throw new InvalidOperationException($"Unsupported StorageMode {spec.StorageMode}");
        }
    }

    /// <summary>This method is used by tests</summary>
    internal static void Generate(DataStructure ds, StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExit> earlyExits)
    {
        ICode instance = ds switch
        {
            DataStructure.Array => new ArrayCode(spec),
            DataStructure.BinarySearch => new BinarySearchCode(spec),
            DataStructure.EytzingerSearch => new EytzingerSearchCode(spec),
            DataStructure.Switch => new SwitchCode(spec),
            DataStructure.SwitchHashCode => new SwitchHashCode(spec),
            DataStructure.MinimalPerfectHash => new MinimalPerfectHashCode(spec),
            DataStructure.HashSet => new HashSetCode(spec),
            DataStructure.UniqueKeyLength => new UniqueKeyLengthCode(spec),
            DataStructure.UniqueKeyLengthSwitch => new UniqueKeyLengthSwitchCode(spec),
            DataStructure.KeyLength => new KeyLengthCode(spec),
            DataStructure.SingleValue => new SingleValueCode(spec),
            DataStructure.Conditional => new ConditionalCode(spec),
            _ => throw new ArgumentOutOfRangeException(nameof(ds), ds, null)
        };

        if (instance.TryPrepare())
            sb.Append(instance.Generate(earlyExits));
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

            if (!Enums.Enums.KnownDataType.TryParse(genericArg.Name, out KnownDataType dataType))
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

            FastDataSpec spec = TypeHelper.MapData<FastDataSpec>(ad.NamedArguments);

            //Overwrite the values with the unique version
            spec.Name = name;
            spec.Data = ctorArg1.Values.Select(x => x.Value).ToArray()!;
            spec.KnownDataType = dataType;
            spec.DataTypeName = genericArg.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            yield return spec;
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