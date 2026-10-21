namespace Genbox.FastData.Generator.Ir.Harness;

public enum HarnessCheck
{
    Success = 0,
    MissingKey = 1,
    LookupValueMismatch = 2,
    UnexpectedKey = 3,
    UnexpectedLookup = 4,
    KeyCountMismatch = 5,
    ValueCountMismatch = 6,
    EnumerationValueMismatch = 7,
    EnumerationKeyMismatch = 8,
    KeySpanCountMismatch = 9,
    KeySpanContentMismatch = 10,
    ValueSpanCountMismatch = 11,
    ValueSpanContentMismatch = 12,
    SpanPairMismatch = 13,
    AlternateKeyMissing = 14,
    AlternateLookupValueMismatch = 15
}