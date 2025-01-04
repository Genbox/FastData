using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Internal.Abstracts;

internal interface ICode
{
    bool IsAppropriate(DataProperties dataProps);
    bool TryPrepare();
    string Generate(IEnumerable<IEarlyExit> earlyExits);
}