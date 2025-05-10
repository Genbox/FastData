using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IEarlyExitHandler
{
    string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits);
}