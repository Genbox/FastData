namespace Genbox.FastData.Internal.Encodings;

internal interface IIntegerEncoding
{
    int MaxEncodedLength { get; }

    int GetEncodedLength(ulong value);

    int Encode(ulong value, Span<byte> destination);

    bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead);
}