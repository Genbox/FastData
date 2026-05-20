namespace Genbox.FastData.Config.Analysis;

internal readonly struct GPerfAnalyzerOptions
{
    internal GPerfAnalyzerOptions(bool sevenBit, bool noLength, int maxConfiguredPosition, int[]? keyPositions, bool useAllKeyPositions, int initialAssociationValue, int jump, int multipleIterations, bool random, int? randomSeed, double sizeMultiple)
    {
        SevenBit = sevenBit;
        NoLength = noLength;
        MaxConfiguredPosition = maxConfiguredPosition;
        KeyPositions = keyPositions;
        UseAllKeyPositions = useAllKeyPositions;
        InitialAssociationValue = initialAssociationValue;
        Jump = jump;
        MultipleIterations = multipleIterations;
        Random = random;
        RandomSeed = randomSeed;
        SizeMultiple = sizeMultiple;
    }

    internal bool SevenBit { get; }
    internal bool NoLength { get; }
    internal int MaxConfiguredPosition { get; }
    internal int[]? KeyPositions { get; }
    internal bool UseAllKeyPositions { get; }
    internal int InitialAssociationValue { get; }
    internal int Jump { get; }
    internal int MultipleIterations { get; }
    internal bool Random { get; }
    internal int? RandomSeed { get; }
    internal double SizeMultiple { get; }
}