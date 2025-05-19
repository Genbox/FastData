using Genbox.FastData.Specs;

namespace Genbox.FastData.Abstracts;

public interface IArrayHash
{
    HashFunc GetHashFunction();
}