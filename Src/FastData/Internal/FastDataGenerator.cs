using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
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

    [SuppressMessage("Major Bug", "S2583:Conditionally executed code should be reachable")]
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<FastDataSpec>> p = context.CompilationProvider
                                                                            .SelectMany(Transform)
                                                                            .Collect();

        context.RegisterSourceOutput(p, (spc, specs) =>
        {
            if (spc.CancellationToken.IsCancellationRequested)
                return;

            try
            {
                StringBuilder sb = new StringBuilder();

                foreach (FastDataSpec spec in specs)
                {
                    if (spec.Data.Length == 0)
                        throw new InvalidOperationException("There are no values");

                    //Analyze the data to determine properties we can optimize for
                    AnalysisResult analysis = Analyzer.Analyze(spec.Data);

                    IEnumerable<IEarlyExitSpec> earlyExitSpecs = Optimizer.GetEarlyExitSpecs(analysis);

                    sb.Clear();
                    AppendHeader(sb, spec);

                    switch (spec.StorageMode)
                    {
                        case StorageMode.Auto:
                        {
                            if (spec.Data.Length == 1)
                                goto case StorageMode.SingleValue;

                            if (spec.Data.Length <= 64)
                                goto case StorageMode.Switch;

                            //If the lengths are unique, we can use the length as hash key
                            if (analysis.StringProperties.UniqLength)
                                goto case StorageMode.UniqueKeyLength;

                            //Fallback to hashset
                            goto case StorageMode.HashSet;
                        }
                        case StorageMode.Array:
                            ArrayCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.BinarySearch:
                            BinarySearchCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.EytzingerSearch:
                            EytzingerSearchCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.Switch:
                            SwitchCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.SwitchHashCode:
                            SwitchHashCode.Generate(sb, spec);
                            break;
                        case StorageMode.MinimalPerfectHash:
                            MinimalPerfectHashCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.HashSet:
                            HashSetCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.UniqueKeyLength:
                            UniqueKeyLengthCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.UniqueKeyLengthSwitch:
                            UniqueKeyLengthSwitchCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.KeyLength:
                            KeyLengthCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        case StorageMode.SingleValue:
                            SingleValueCode.Generate(sb, spec);
                            break;
                        case StorageMode.Conditional:
                            ConditionalCode.Generate(sb, spec, earlyExitSpecs);
                            break;
                        default:
                            throw new InvalidOperationException("Value outside of supported values: " + spec.StorageMode);
                    }

                    AppendFooter(sb, spec);

                    spc.AddSource(spec.Name + ".g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
                }
            }
            catch (Exception e)
            {
                spc.ReportDiagnostic(Diagnostic.Create(_generationError, null, e.Message));
            }
        });
    }

    private static IEnumerable<FastDataSpec> Transform(Compilation c, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            yield break;

        ISymbol? symbol = c.GetTypeByMetadataName(FastLookupAttr);

        if (symbol == null)
            yield break;

        ImmutableArray<AttributeData> ads = c.Assembly.GetAttributes();

        foreach (AttributeData ad in ads)
        {
            if (ad.AttributeClass == null)
                continue;

            //If it is not one of our Attributes, skip it.
            if (!AreEqualSymbols(ad.AttributeClass, symbol))
                continue;

            TypedConstant name = ad.ConstructorArguments[0];

            if (name.Value == null)
                throw new InvalidOperationException("Name was null");

            TypedConstant values = ad.ConstructorArguments[1];

            //We can always unique the values
            HashSet<string> uniqueValues = new HashSet<string>(StringComparer.Ordinal);

            foreach (TypedConstant value in values.Values)
            {
                if (value.Value == null)
                    continue;

                uniqueValues.Add(value.Value.ToString());
            }

            FastDataSpec spec = TypeHelper.MapData<FastDataSpec>(ad.NamedArguments);

            //Overwrite the values with the unique version
            spec.Name = (string)name.Value;
            spec.Data = uniqueValues.ToArray();

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
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        sb.Append($$"""


                        public{{staticStr}} int Length => {{spec.Data.Length}};
                    }
                    """);
    }
}