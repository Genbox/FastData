using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Models;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class ObjectTypeDef(Func<TypeModel, string> userPrintDeclaration, Func<ITypeReference, IEnumerable<IValueModel>, string> userPrintValues) : ITypeDef<object>
{
    public DataType DataType => DataType.Object;
    public string Name => throw new NotSupportedException("not supported");
    public Func<object, string> PrintObj => throw new NotSupportedException("not supported");
    public Func<object, string> Print => throw new NotSupportedException("not supported");

    public string PrintValues(ObjectType objectType)
    {
        return userPrintValues(objectType.Model.ElementType, objectType.Model.Values);
    }

    public string GetDeclarations(ObjectType objectType)
    {
        StringBuilder sb = new StringBuilder();
        foreach (TypeModel modelType in objectType.Model.Types)
            sb.AppendLine(userPrintDeclaration(modelType));
        return sb.ToString();
    }
}