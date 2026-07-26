using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

public class ObjectTypeDef(Func<TypeMap, Type, string> userPrintDeclaration, Func<TypeMap, object, string> userPrintValue) : IObjectTypeDef
{
    public TypeCode KeyType => TypeCode.Object;
    public string Name => throw new NotSupportedException("not supported");
    public Func<TypeMap, object, string> PrintObj => userPrintValue;
    public Func<TypeMap, object, string> Print => userPrintValue;

    public string PrintDeclaration(TypeMap map, Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "The object type cannot be null.");

        return userPrintDeclaration(map, type);
    }
}