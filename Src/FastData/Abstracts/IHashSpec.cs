using Genbox.FastData.Internal.Analysis.Analyzers;

namespace Genbox.FastData.Abstracts;

public interface IHashSpec
{
    HashFunc GetHashFunction();
    EqualFunc GetEqualFunction();
}