using Genbox.FastData.Enums;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData;

public static partial class FastDataGenerator
{
    [LoggerMessage(LogLevel.Information, "There are {Count} unique items")]
    internal static partial void LogUniqueItems(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Data consists of {DataType}")]
    internal static partial void LogDataType(ILogger logger, DataType dataType);

    [LoggerMessage(LogLevel.Information, "Min value: {MinValue}, Max value: {MaxValue}")]
    internal static partial void LogMinMaxValues(ILogger logger, object minValue, object maxValue);

    [LoggerMessage(LogLevel.Information, "Min length: {MinLength}, Max length: {MaxLength}")]
    internal static partial void LogMinMaxLength(ILogger logger, uint minLength, uint maxLength);

    [LoggerMessage(LogLevel.Information, "User selected structure type {Type}")]
    internal static partial void LogUserStructureType(ILogger logger, StructureType type);

    [LoggerMessage(LogLevel.Information, "Trying data structure {Name}")]
    internal static partial void LogCandidateAttempt(ILogger logger, string name);

    [LoggerMessage(LogLevel.Information, "Data structure {Name} succeeded")]
    internal static partial void LogCandidateSuccess(ILogger logger, string name);

    [LoggerMessage(LogLevel.Information, "Data structure {Name} failed")]
    internal static partial void LogCandidateFailed(ILogger logger, string name);
}