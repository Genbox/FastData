using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashExpressionDef
{
    string RenderAdditionalData(AdditionalData[] info);
    string RenderFunctions(ReaderFunctions functions);
}