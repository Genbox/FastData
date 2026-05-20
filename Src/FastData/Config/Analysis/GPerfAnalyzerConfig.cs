using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
[SuppressMessage("Exceptions usages", "EX005:Use parameter related exception only for method parameters")]
[SuppressMessage("Major Code Smell", "S3928:Parameter names used into ArgumentException constructors should match an existing one ")]
[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
public sealed class GPerfAnalyzerConfig : IAnalyzerConfig
{
    private const int MaxKeyPosition = 255;

    /// <summary>Maximum one-based automatic key position to consider. FastData requires a value in the range 1 through 255; mandatory positions may exceed this limit.</summary>
    public uint MaxPositions { get; set; } = 255;

    /// <summary>Optional gperf-style key positions, for example <c>1,4,$</c>, <c>1-8</c>, or <c>*</c>. Positions are one-based; <c>$</c> means the last byte and <c>*</c> means every supported fixed byte position present in the dataset.</summary>
    public string? KeyPositions { get; set; }

    /// <summary>Restricts the algorithm to 7-bit input bytes, matching gperf's <c>--seven-bit</c> option.</summary>
    public bool SevenBit { get; set; }

    /// <summary>Disables the keyword length contribution to the hash, matching gperf's <c>--no-strlen</c> option.</summary>
    public bool NoLength { get; set; }

    /// <summary>Initial value used for associated values. The gperf default is 0. Ignored when <see cref="Random"/> is enabled or <see cref="MultipleIterations"/> is greater than 0.</summary>
    public int InitialAssociationValue { get; set; }

    /// <summary>Jump value used while resolving associated-value collisions. The gperf default is 5; even non-zero values are rounded up by the analyzer. Ignored when <see cref="MultipleIterations"/> is greater than 0.</summary>
    public int Jump { get; set; } = 5;

    /// <summary>Number of alternative initial/jump pairs to try, matching gperf's <c>--multiple-iterations</c> option. A value of 0 uses the configured initial and jump values once; values greater than 0 use gperf's generated initial/jump sequence.</summary>
    public int MultipleIterations { get; set; }

    /// <summary>Randomly initializes associated values, matching gperf's <c>--random</c> option. Supersedes <see cref="InitialAssociationValue"/> unless <see cref="MultipleIterations"/> is greater than 0.</summary>
    public bool Random { get; set; }

    /// <summary>Optional seed for deterministic random-mode tests. gperf seeds from the current time.</summary>
    public int? RandomSeed { get; set; }

    /// <summary>Multiplier for the associated-value range. FastData requires a finite positive value and does not support gperf CLI compatibility forms such as zero clamping or negative reciprocals.</summary>
    public double SizeMultiple { get; set; } = 1.0;

    internal GPerfAnalyzerOptions CreateOptions()
    {
        (int[]? Positions, bool UseAll) keyPositions = ParseKeyPositions(KeyPositions);
        return new GPerfAnalyzerOptions(
            SevenBit,
            NoLength,
            GetMaxConfiguredPosition(MaxPositions),
            keyPositions.Positions,
            keyPositions.UseAll,
            NormalizeInitialAssociationValue(InitialAssociationValue),
            NormalizeJump(Jump),
            ValidateMultipleIterations(MultipleIterations),
            Random,
            RandomSeed,
            NormalizeSizeMultiple(SizeMultiple));
    }

    private static int GetMaxConfiguredPosition(uint maxPositions)
    {
        if (maxPositions is 0 or > MaxKeyPosition)
            throw new ArgumentOutOfRangeException(nameof(MaxPositions), maxPositions, "Max positions must be in the range 1 through 255.");

        return (int)maxPositions - 1;
    }

    private static (int[]? Positions, bool UseAll) ParseKeyPositions(string? keyPositions)
    {
        if (string.IsNullOrWhiteSpace(keyPositions))
            return (null, false);

        string text = keyPositions.Trim();
        if (text == "*")
            return (null, true);

        if (text.IndexOf('*') >= 0)
            throw new ArgumentException("The all-positions wildcard must be the entire key-position expression.", nameof(KeyPositions));

        List<int> positions = new List<int>();
        bool[] seen = new bool[MaxKeyPosition + 1];

        foreach (string rawItem in text.Split(','))
        {
            string item = rawItem.Trim();
            if (item.Length == 0)
                continue;

            AddKeyPositionItem(positions, seen, item);
        }

        if (positions.Count == 0)
            throw new ArgumentException("No key positions selected.", nameof(KeyPositions));

        return (positions.ToArray(), false);
    }

    private static void AddKeyPositionItem(List<int> positions, bool[] seen, string item)
    {
        int rangeSeparator = item.IndexOf('-');
        if (rangeSeparator < 0)
        {
            AddKeyPosition(positions, seen, ParseKeyPosition(item));
            return;
        }

        int start = ParseKeyPosition(item.Substring(0, rangeSeparator).Trim());
        if (start == -1)
            throw new ArgumentException("The last-byte position cannot start a key-position range.", nameof(KeyPositions));

        int end = ParseKeyPosition(item.Substring(rangeSeparator + 1).Trim());
        if (end == -1 || end <= start)
            throw new ArgumentException("Invalid key-position range.", nameof(KeyPositions));

        for (int position = start; position <= end; position++)
            AddKeyPosition(positions, seen, position);
    }

    private static int ParseKeyPosition(string text)
    {
        if (text.Length == 0)
            throw new ArgumentException("Missing key position.", nameof(KeyPositions));

        if (text == "$" || text == "'$'")
            return -1;

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            throw new ArgumentException("Invalid key position.", nameof(KeyPositions));

        if (value < 1 || value > MaxKeyPosition)
            throw new ArgumentOutOfRangeException(nameof(KeyPositions), value, "Key positions must be in the range 1 through 255.");

        return value - 1;
    }

    private static void AddKeyPosition(List<int> positions, bool[] seen, int position)
    {
        int seenIndex = position + 1;
        if (seen[seenIndex])
            throw new ArgumentException("Duplicate key positions selected.", nameof(KeyPositions));

        seen[seenIndex] = true;
        positions.Add(position);
    }

    private static double NormalizeSizeMultiple(double sizeMultiple)
    {
        if (double.IsNaN(sizeMultiple) || double.IsInfinity(sizeMultiple))
            throw new ArgumentOutOfRangeException(nameof(SizeMultiple), sizeMultiple, "Size multiple must be a finite number.");

        if (sizeMultiple <= 0)
            throw new ArgumentOutOfRangeException(nameof(SizeMultiple), sizeMultiple, "Size multiple must be greater than zero.");

        return sizeMultiple;
    }

    private static int NormalizeInitialAssociationValue(int initialAssociationValue)
    {
        if (initialAssociationValue < 0)
            throw new ArgumentOutOfRangeException(nameof(InitialAssociationValue), initialAssociationValue, "Initial association value must not be negative.");

        return initialAssociationValue;
    }

    private static int NormalizeJump(int jump)
    {
        if (jump < 0)
            throw new ArgumentOutOfRangeException(nameof(Jump), jump, "Jump must not be negative.");

        return jump != 0 && (jump & 1) == 0 ? jump + 1 : jump;
    }

    private static int ValidateMultipleIterations(int multipleIterations)
    {
        if (multipleIterations < 0)
            throw new ArgumentOutOfRangeException(nameof(MultipleIterations), multipleIterations, "Multiple iterations must not be negative.");

        return multipleIterations;
    }
}