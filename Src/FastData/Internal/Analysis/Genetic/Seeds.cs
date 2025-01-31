namespace Genbox.FastData.Internal.Analysis.Genetic;

internal static class Seeds
{
    // A good seed has the following properties:
    // - Odd: Avoids getting the mixer stuck in a loop of 0.
    // - Large: Means we push a lot of the lower bits into higher bits. Gives better avalanche.
    // - Low bias: No correlation between input bits and output bits
    internal static readonly uint[] GoodSeeds =
    [
        0x85EBCA6B, 0xC2B2AE35, //Murmur
        0x45D9F3B, // Degski
        0x9E3779B9, // FP32
        0x7FEB352D, 0x846CA68B, // Lowbias
        0xED5AD4BB, 0xAC4C1B51, 0x31848BAB, //Triple
        0x85EBCA77, 0xC2B2AE3D, // XXHash2
    ];
}