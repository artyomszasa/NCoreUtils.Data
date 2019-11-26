using System;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class ConstantSource<T> : ConstantSource, IValueSource<T>
    {
        public T Value { get; }

        public override object BoxedValue => Value;

        public override Type Type => typeof(T);

        public ConstantSource(T value) => Value = value;

        T IValueSource<T>.GetValue(object instance) => Value;
    }
}