using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

public class ObjectTypeDef(Func<TypeMap, Type, string> userPrintDeclaration, Func<TypeMap, object, string> userPrintValue) : ITypeDef<object>
{
    public TypeCode KeyType => TypeCode.Object;
    public string Name => throw new NotSupportedException("not supported");
    public Func<TypeMap, object, string> PrintObj => userPrintValue;
    public Func<TypeMap, object, string> Print => userPrintValue;

    public string PrintDeclaration(TypeMap map, Type type) => userPrintDeclaration(map, type);
}