using System.Collections;
using System.Reflection;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Models;

namespace Genbox.FastData.Generator.Framework;

public static class ModelBuilder
{
    public static ValuesModel Build(Array values, TypeMap typeMap)
    {
        Dictionary<Type, ITypeReference> typeRefs = new Dictionary<Type, ITypeReference>();
        Dictionary<Type, TypeModel> typeModels = new Dictionary<Type, TypeModel>();
        List<IValueModel> valueModels = new List<IValueModel>();
        Queue<Type> queue = new Queue<Type>();

        ITypeReference GetTypeRef(Type t)
        {
            if (Type.GetTypeCode(t) != TypeCode.Object)
                return typeRefs.GetOrAdd(t, _ => new PrimitiveType(typeMap.GetTypeName(t)));

            if (t.IsArray)
                return new ArrayType(GetTypeRef(t.GetElementType()!));

            return typeRefs.GetOrAdd(t, _ =>
            {
                queue.Enqueue(t);
                return new CustomType(t.Name);
            });
        }

        IValueModel BuildValue(object? obj)
        {
            if (obj == null)
                return new PrimitiveValue(typeMap.GetNull());

            Type type = obj.GetType();
            ITypeDef def = typeMap.Get(type);

            if (Type.GetTypeCode(type) != TypeCode.Object)
                return new PrimitiveValue(def.PrintObj(obj));

            if (obj is IEnumerable ie && obj is not string)
                return new ArrayValue(ie.Cast<object>().Select(BuildValue).ToList());

            Dictionary<string, IValueModel> props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                        .ToDictionary(p => p.Name, p => BuildValue(p.GetValue(obj)), StringComparer.Ordinal);
            GetTypeRef(type);
            return new ObjectValue(type.Name, props);
        }

        foreach (object? o in values)
            valueModels.Add(BuildValue(o));

        while (queue.Count > 0)
        {
            Type t = queue.Dequeue();
            List<PropertyModel> props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Select(p => new PropertyModel(p.Name, GetTypeRef(p.PropertyType)))
                                         .ToList();
            typeModels[t] = new TypeModel(t.Name, props);
        }

        Type elemType = values.GetValue(0).GetType();
        return new ValuesModel(typeModels.Values.ToArray(), valueModels, GetTypeRef(elemType));
    }
#pragma warning disable S1121
    private static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> fac) => dict.TryGetValue(key, out TValue? v) ? v : dict[key] = fac(key);
#pragma warning restore S1121
}