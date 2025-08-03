using Genbox.FastData.Enums;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData;

public static partial class FastDataGenerator
{
    [LoggerMessage(LogLevel.Information, "There are {Count} unique items")]
    internal static partial void LogUniqueItems(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Data consists of {KeyType}")]
    internal static partial void LogKeyType(ILogger logger, KeyType keyType);

    [LoggerMessage(LogLevel.Information, "Min value: {MinValue}, Max value: {MaxValue}")]
    internal static partial void LogMinMaxValues(ILogger logger, object minValue, object maxValue);

    [LoggerMessage(LogLevel.Information, "Min length: {MinLength}, Max length: {MaxLength}")]
    internal static partial void LogMinMaxLength(ILogger logger, uint minLength, uint maxLength);

    [LoggerMessage(LogLevel.Information, "User selected structure type {Type}")]
    internal static partial void LogUserStructureType(ILogger logger, StructureType type);

    [LoggerMessage(LogLevel.Information, "Generated StringHash with fitness {Fitness}")]
    internal static partial void LogStringHashFitness(ILogger logger, double fitness);
}