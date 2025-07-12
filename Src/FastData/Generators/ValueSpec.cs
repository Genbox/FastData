using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators;

public record ValueSpec<T>(T[] Values) : ValueSpec(typeof(T), Values);

public record ValueSpec(Type Type, Array Array)
{
    private PropertySpec[]? _fields;

    public PropertySpec[]? GetFields(ITypeMap typeMap)
    {
        if (Type.GetTypeCode(Type) != TypeCode.Object)
            throw new InvalidOperationException("GetFields should only be called when the ValueSpec contains a complex type");

        if (_fields == null)
        {
            PropertyInfo[] props = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            _fields = new PropertySpec[props.Length];

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];

                if (!prop.CanRead)
                    continue;

                if (!HasReferenceType && Type.GetTypeCode(prop.PropertyType) == TypeCode.Object)
                    HasReferenceType = true;

                _fields[i] = new PropertySpec(prop.Name, typeMap.GetTypeName(prop.PropertyType));
            }
        }

        return _fields;
    }

    public bool HasReferenceType { get; private set; }
    public bool IsCustomType => Type.GetTypeCode(Type) == TypeCode.Object;
}