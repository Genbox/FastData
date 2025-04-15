using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Abstracts;

public interface IHashSpec
{
    HashFunc GetHashFunction();
    EqualFunc GetEqualFunction();
}