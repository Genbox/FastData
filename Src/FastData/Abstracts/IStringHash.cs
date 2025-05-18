using Genbox.FastData.Specs;

namespace Genbox.FastData.Abstracts;

public interface IStringHash
{
    HashFunc GetHashFunction();
}