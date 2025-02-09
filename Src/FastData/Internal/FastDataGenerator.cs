using System.Collections.Immutable;
using System.Text;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
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
                        Generator.Generate(sb, spec);
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
}