using System;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public abstract class ConstantSource : IValueSource
    {
        public static ConstantSource FromValue(object value, Type type = null)
        {
            Type ty;
            object v;
            if (type is null)
            {
                if (value is null)
                {
                    throw new InvalidOperationException("When creating constant source for null value type must be specified explicitly.");
                }
                ty = value.GetType();
                v = value;
            }
            else
            {
                if (value is null)
                {
                    ty = type;
                    v = ty.IsValueType ? Activator.CreateInstance(ty) : null;
                }
                else
                {
                    ty = value.GetType();
                    if (!type.IsAssignableFrom(ty))
                    {
                        throw new InvalidOperationException($"Value of type {ty} is not compatible with the specified type {type}.");
                    }
                    v = value;
                }
            }
            return (ConstantSource)Activator.CreateInstance(typeof(ConstantSource<>).MakeGenericType(ty), v);
        }

        public abstract object BoxedValue { get; }

        public abstract Type Type { get; }

        object IValueSource.GetValue(object instance) => BoxedValue;
    }
}