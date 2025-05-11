using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IEarlyExitDef
{
    string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits);
}