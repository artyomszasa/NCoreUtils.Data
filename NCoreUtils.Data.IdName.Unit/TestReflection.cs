using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NCoreUtils.Data
{
    public class TestReflection
    {
        private sealed class MyDictionary : IDictionary<string, int>
        {
            public int this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ICollection<string> Keys => throw new NotImplementedException();

            public ICollection<int> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public void Add(string key, int value)
            {
                throw new NotImplementedException();
            }

            public void Add(KeyValuePair<string, int> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, int> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, int> item)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out int value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Nullable()
        {
            Assert.True(typeof(int?).IsNullable(out var elementType));
            Assert.False(typeof(string).IsNullable(out var nullType));
            Assert.Equal(typeof(int), elementType);
            Assert.Null(nullType);
            Assert.True(typeof(int?).IsNullable());
            Assert.False(typeof(string).IsNullable());
        }

        [Fact]
        public void Dictionary()
        {
            Assert.True(typeof(Dictionary<string, int>).IsDictionaryType(out var keyType, out var valueType));
            Assert.Equal(typeof(string), keyType);
            Assert.Equal(typeof(int), valueType);
            Assert.True(typeof(IDictionary<string, int>).IsDictionaryType(out keyType, out valueType));
            Assert.Equal(typeof(string), keyType);
            Assert.Equal(typeof(int), valueType);
            Assert.True(typeof(MyDictionary).IsDictionaryType(out keyType, out valueType));
            Assert.Equal(typeof(string), keyType);
            Assert.Equal(typeof(int), valueType);
            Assert.True(typeof(MyDictionary).IsDictionaryType());
            Assert.False(typeof(string).IsDictionaryType());
        }
    }
}