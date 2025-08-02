using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class ObjectTypeDef(Func<TypeMap, Type, string> userPrintDeclaration, Func<TypeMap, object, string> userPrintValue) : ITypeDef<object>
{
    public DataType DataType => DataType.Object;
    public string Name => throw new NotSupportedException("not supported");
    public Func<TypeMap, object, string> PrintObj => userPrintValue;
    public Func<TypeMap, object, string> Print => userPrintValue;

    public string PrintDeclaration(TypeMap map, Type type) => userPrintDeclaration(map, type);
}