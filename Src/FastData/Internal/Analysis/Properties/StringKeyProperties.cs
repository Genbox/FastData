using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record StringKeyProperties(LengthData LengthData, DeltaData DeltaData, CharacterData CharacterData) : IProperties;