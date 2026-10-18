using Genbox.FastData.Enums;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

internal interface ITestVector : IXunitSerializable
{
    StructureType StructureType { get; }
}