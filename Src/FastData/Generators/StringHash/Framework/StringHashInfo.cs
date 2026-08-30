using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generators.StringHash.Framework;

/// <summary>Describes a string hash expression and any arrays it captures.</summary>
/// <param name="expression">The hash expression.</param>
/// <param name="additionalData">Metadata for captured arrays.</param>
public sealed class StringHashInfo(Expression<StringHashFunc> expression, AdditionalData[]? additionalData)
{
    /// <summary>Gets the validated hash expression.</summary>
    public Expression<StringHashFunc> Expression { get; } = Validate(expression, additionalData);

    /// <summary>Gets metadata for arrays captured by the expression.</summary>
    public AdditionalData[]? AdditionalData { get; } = additionalData;

    private static Expression<StringHashFunc> Validate(Expression<StringHashFunc> expression, AdditionalData[]? additionalData)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        Dictionary<string, AdditionalData> dataByName = ValidateAdditionalData(additionalData);
        CapturedExternalDataValidator.ValidateCapturedData(expression, dataByName);
        return expression;
    }

    [SuppressMessage("ReSharper", "CanSimplifyDictionaryLookupWithTryAdd")]
    private static Dictionary<string, AdditionalData> ValidateAdditionalData(AdditionalData[]? additionalData)
    {
        Dictionary<string, AdditionalData> dataByName = new Dictionary<string, AdditionalData>(StringComparer.Ordinal);
        if (additionalData == null)
            return dataByName;

        foreach (AdditionalData? data in additionalData)
        {
            if (data == null)
                throw new ArgumentException("Additional data entries cannot be null.", nameof(additionalData));
            if (string.IsNullOrEmpty(data.Name))
                throw new ArgumentException("Additional data names cannot be empty.", nameof(additionalData));
            if (data.Type == null || data.Values == null || data.Values.Rank != 1 || data.Values.GetType().GetElementType() != data.Type)
                throw new ArgumentException($"Additional data '{data.Name}' must use a one-dimensional value array matching its element type.", nameof(additionalData));
            if (dataByName.ContainsKey(data.Name))
                throw new ArgumentException($"Additional data name '{data.Name}' is duplicated.", nameof(additionalData));
            dataByName.Add(data.Name, data);
        }

        return dataByName;
    }

    private sealed class CapturedExternalDataValidator(Dictionary<string, AdditionalData> dataByName, string parameterName) : ExpressionVisitor
    {
        internal static void ValidateCapturedData(Expression<StringHashFunc> expression, Dictionary<string, AdditionalData> dataByName) =>
            new CapturedExternalDataValidator(dataByName, nameof(expression)).Visit(expression);

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not ConstantExpression constant)
                return base.VisitMember(node);

            if (!dataByName.TryGetValue(node.Member.Name, out AdditionalData? data))
                throw new ArgumentException($"Captured external data '{node.Member.Name}' has no matching {nameof(AdditionalData)} entry.", parameterName);

            Type? elementType = node.Type.IsArray ? node.Type.GetElementType() : null;
            if (elementType != data.Type)
                throw new ArgumentException($"Captured external data '{node.Member.Name}' has element type '{elementType}', but metadata declares '{data.Type}'.", parameterName);

            object? capturedValue = node.Member switch
            {
                FieldInfo field => field.GetValue(constant.Value),
                PropertyInfo property when property.GetIndexParameters().Length == 0 => property.GetValue(constant.Value),
                _ => null
            };
            if (capturedValue is not Array capturedValues || !ReferenceEquals(capturedValues, data.Values))
                throw new ArgumentException($"Captured external data '{node.Member.Name}' must reference the same array instance as its metadata.", parameterName);

            return node;
        }
    }
}