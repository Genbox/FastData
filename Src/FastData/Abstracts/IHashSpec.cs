using Genbox.FastData.Specs;

namespace Genbox.FastData.Abstracts;

public interface IHashSpec
{
    HashFunc<string> GetHashFunction();
    EqualFunc<string> GetEqualFunction();
}